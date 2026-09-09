// Bounded managed stand-in for Unity's component/transform and PNG metadata APIs.
// No GPU rendering claims. Production adapter + timeline and original native
// DiceAttackEffect/RedHood methods are compiled unchanged beside this implementation.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using N=System.Numerics;
using UnityEngine;
namespace UnityEngine
{
 [AttributeUsage(AttributeTargets.Class,Inherited=true)] public class RequireComponent:Attribute { public Type[] types; public RequireComponent(params Type[] t){types=t;} }
 [AttributeUsage(AttributeTargets.Field)] public class SerializeField:Attribute{}
 public class Object
 {
  public string name; public bool destroyed;
  public static void Destroy(Object o){if(o!=null)o.destroyed=true;}
  public static implicit operator bool(Object o){return !ReferenceEquals(o,null)&&!o.destroyed;}
 }
 public class Component:Object
 {
  public GameObject gameObject; public Transform transform=>gameObject.transform;
  public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
  public T[] GetComponentsInChildren<T>() where T:Component=>gameObject.GetComponentsInChildren<T>();
 }
 public class MonoBehaviour:Component{}
 public class GameObject:Object
 {
  public Transform transform; public int layer; public bool activeSelf=true; public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);
  public readonly List<Component> components=new List<Component>();
  public GameObject(string n="GameObject"){name=n;transform=new Transform{gameObject=this};components.Add(transform);}
  public T AddComponent<T>() where T:Component => (T)AddComponent(typeof(T));
  public Component AddComponent(Type type)
  {
   foreach(RequireComponent req in type.GetCustomAttributes(typeof(RequireComponent),true))foreach(Type t in req.types)if(!components.Any(x=>t.IsInstanceOfType(x)))AddComponent(t);
   var c=(Component)Activator.CreateInstance(type);c.gameObject=this;c.name=name;components.Add(c);
   var awake=type.GetMethod("Awake",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(awake!=null)awake.Invoke(c,null);return c;
  }
  public T GetComponent<T>() where T:Component=>components.OfType<T>().FirstOrDefault();
  public T[] GetComponentsInChildren<T>() where T:Component=>components.OfType<T>().Concat(transform.children.SelectMany(c=>c.gameObject.GetComponentsInChildren<T>())).ToArray();
  public void SetActive(bool v){activeSelf=v;}
 }
 public struct Vector2
 {
  public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero=>new Vector2(0,0);
 }
 public struct Vector3
 {
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public static Vector3 zero=>new Vector3(0,0,0);public static Vector3 one=>new Vector3(1,1,1);public static Vector3 right=>new Vector3(1,0,0);
  public N.Vector3 N=>new N.Vector3(x,y,z); public static Vector3 From(N.Vector3 v)=>new Vector3(v.X,v.Y,v.Z);
  public static Vector3 operator+(Vector3 a,Vector3 b)=>From(a.N+b.N);public static Vector3 operator-(Vector3 a,Vector3 b)=>From(a.N-b.N);public static Vector3 operator*(Vector3 a,float f)=>From(a.N*f);
  public static float Distance(Vector3 a,Vector3 b)=>System.Numerics.Vector3.Distance(a.N,b.N);
 }
 public struct Quaternion
 {
  public float x,y,z,w;public Quaternion(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}
  public N.Quaternion N=>new N.Quaternion(x,y,z,w);public static Quaternion From(N.Quaternion q)=>new Quaternion(q.X,q.Y,q.Z,q.W);public static Quaternion identity=>new Quaternion(0,0,0,1);
  public static Quaternion Euler(float x,float y,float z)=>From(System.Numerics.Quaternion.CreateFromYawPitchRoll(y*(float)Math.PI/180,x*(float)Math.PI/180,z*(float)Math.PI/180));
 }
 public class Transform:Component
 {
  private Transform par;public List<Transform> children=new List<Transform>();public Vector3 localPosition=Vector3.zero,localScale=Vector3.one;public Quaternion localRotation=Quaternion.identity;
  public Transform parent{get=>par;set=>SetParent(value,true);}
  public N.Matrix4x4 matrix=>N.Matrix4x4.CreateScale(localScale.N)*N.Matrix4x4.CreateFromQuaternion(localRotation.N)*N.Matrix4x4.CreateTranslation(localPosition.N)*(par==null?N.Matrix4x4.Identity:par.matrix);
  public Vector3 position{get=>TransformPoint(Vector3.zero);set{if(par==null)localPosition=value;else{N.Matrix4x4.Invert(par.matrix,out var inv);localPosition=Vector3.From(N.Vector3.Transform(value.N,inv));}}}
  public Quaternion rotation{get{N.Matrix4x4.Decompose(matrix,out var s,out var q,out var p);return Quaternion.From(q);}set{localRotation=par==null?value:Quaternion.From(value.N*N.Quaternion.Inverse(par.rotation.N));}}
  public Vector3 lossyScale{get{N.Matrix4x4.Decompose(matrix,out var s,out var q,out var p);return Vector3.From(s);}}
  public void SetParent(Transform p,bool worldPositionStays=true){var old=matrix;if(par!=null)par.children.Remove(this);par=p;if(par!=null)par.children.Add(this);if(worldPositionStays){var local=old;if(p!=null){N.Matrix4x4.Invert(p.matrix,out var inv);local*=inv;}N.Matrix4x4.Decompose(local,out var s,out var q,out var pos);localScale=Vector3.From(s);localRotation=Quaternion.From(q);localPosition=Vector3.From(pos);}}
  public Vector3 TransformPoint(Vector3 v)=>Vector3.From(N.Vector3.Transform(v.N,matrix));
  public Transform Find(string n)=>children.FirstOrDefault(c=>c.gameObject.name==n);
 }
 public struct Color { public float r,g,b,a;public Color(float rr,float gg,float bb,float aa=1){r=rr;g=gg;b=bb;a=aa;}public static Color white=>new Color(1,1,1,1); }
 public class Animator:Component { public float speed;public bool enabled; }
 public class Shader:Object{ public static Shader Find(string n)=>new Shader{name=n}; }
 public class Material:Object{public Material(Shader s){} }
 public class SpriteRenderer:Component
 {
  public Sprite sprite;public Color color=Color.white;public bool enabled=true;public Material sharedMaterial;public int sortingOrder;private int sort;
  public int sortingLayerID{get=>sort;set=>sort=value;}public string sortingLayerName{get=>sort==-1396713383?"Particle":"Default";set=>sort=SortingLayer.NameToID(value);}
 }
 public struct SortingLayer
 {
  public string name;public int id;public static SortingLayer[] layers=>new[]{new SortingLayer{name="Default",id=0},new SortingLayer{name="Particle",id=-1396713383}};
  public static int NameToID(string n)=>n=="Particle"?-1396713383:0;
 }
 public static class LayerMask{public static int NameToLayer(string n)=>n=="Effect"?8:n=="Character"?20:n=="Default"?0:-1;}
 public class Texture2D:Object
 {
  public int width,height;public FilterMode filterMode;public TextureWrapMode wrapMode;public Texture2D(int w,int h,TextureFormat f=TextureFormat.RGBA32,bool mip=false){width=w;height=h;}
 }
 public class Sprite:Object
 {
  public Texture2D texture;public Vector2 pivot;public float pixelsPerUnit;public static Sprite Create(Texture2D t,Rect rect,Vector2 p,float ppu,uint extrusion=0,SpriteMeshType mesh=SpriteMeshType.FullRect)=>new Sprite{texture=t,pivot=p,pixelsPerUnit=ppu};
 }
 public struct Rect { public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;} }
 public enum TextureFormat{RGBA32}public enum FilterMode{Bilinear}public enum TextureWrapMode{Clamp}public enum SpriteMeshType{FullRect}
 public static class ImageConversion{public static bool LoadImage(Texture2D t,byte[] b,bool n){if(b.Length<24)return false;t.width=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(16,4));t.height=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(20,4));return true;}}
 public static class JsonUtility{public static T FromJson<T>(string s)=>JsonSerializer.Deserialize<T>(s,new JsonSerializerOptions{IncludeFields=true});}
 public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Abs(float a)=>Math.Abs(a);public static float Clamp01(float a)=>Math.Min(1,Math.Max(0,a));}
 public static class Time{public static float deltaTime=1f/60;}
}
public enum ActionDetail{Default,Guard,Evade,Damaged,Slash,Penetrate,Hit,Move,Standing,NONE,Fire,Aim,Special,Slash2,Penetrate2,Hit2}
public class BattleUnitModel {public BattleUnitView view;}
public class BattleUnitView:UnityEngine.MonoBehaviour {public BattleUnitModel model;public CharacterAppearance charAppearance;public UnityEngine.Transform atkEffectRoot,characterRotationCenter;}
public class CharacterAppearance:UnityEngine.MonoBehaviour
{
 public UnityEngine.Transform atkEffectRoot,atkEffectPivot_G,atkEffectPivot_E,atkEffectPivot_J,atkEffectPivot_Z,atkEffectPivot_H,atkEffectPivot_F;
 			public Transform GetAtkEffectPivot(ActionDetail action)
	{
		switch (action)
		{
		case ActionDetail.Guard:
			if (this.atkEffectPivot_G == null)
			{
				return this.atkEffectRoot;
			}
			return this.atkEffectPivot_G;
		case ActionDetail.Evade:
			if (this.atkEffectPivot_E == null)
			{
				return this.atkEffectRoot;
			}
			return this.atkEffectPivot_E;
		case ActionDetail.Slash:
			if (this.atkEffectPivot_J == null)
			{
				return this.atkEffectRoot;
			}
			return this.atkEffectPivot_J;
		case ActionDetail.Penetrate:
			if (this.atkEffectPivot_Z == null)
			{
				return this.atkEffectRoot;
			}
			return this.atkEffectPivot_Z;
		case ActionDetail.Hit:
			if (this.atkEffectPivot_H == null)
			{
				return this.atkEffectRoot;
			}
			return this.atkEffectPivot_H;
		case ActionDetail.Fire:
			if (this.atkEffectPivot_F == null)
			{
				return this.atkEffectRoot;
			}
			return this.atkEffectPivot_F;
		}
		return this.atkEffectRoot;
	}

}
public class BattleSceneRoot:UnityEngine.MonoBehaviour{}
public class SingletonBehavior<T> {public static T Instance;}
public class AutoDestruct:UnityEngine.MonoBehaviour{public float time;public void DestroyWhenDisable(){} }
namespace Steria {public static class SteriaLogger{public static void LogError(string s){throw new Exception(s);} } }
