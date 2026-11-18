// Bounded managed Unity stand-in adapted from the existing Anhier fixture.
// Only adapter ownership/transform contracts are exercised, never GPU rendering.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using N=System.Numerics;
using UnityEngine;
namespace UnityEngine
{
 [AttributeUsage(AttributeTargets.Class,Inherited=true)] public class RequireComponent:Attribute { public Type[] types; public RequireComponent(params Type[] t){types=t;} }
 [AttributeUsage(AttributeTargets.Field)] public class SerializeField:Attribute{}
 public class Object
 {
  public string name; public bool destroyed;
  public static void Destroy(Object o){if(ReferenceEquals(o,null)||o.destroyed)return;o.destroyed=true;
   if(o is GameObject go){foreach(var c in go.components.ToArray()){Invoke(c,"OnDisable");Invoke(c,"OnDestroy");c.destroyed=true;}foreach(var child in go.transform.children.ToArray())Destroy(child.gameObject);}}
  static void Invoke(object o,string name){o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.Invoke(o,null);}
  public static bool operator==(Object a,Object b){bool an=ReferenceEquals(a,null)||a.destroyed,bn=ReferenceEquals(b,null)||b.destroyed;return an&&bn||!an&&!bn&&ReferenceEquals(a,b);}
  public static bool operator!=(Object a,Object b)=>!(a==b);public override bool Equals(object o)=>ReferenceEquals(this,o);public override int GetHashCode()=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
  public static T Instantiate<T>(T source) where T:Object {var original=(GameObject)(Object)source;var clone=new GameObject(original.name);clone.AddComponent<ParticleSystemRenderer>();return (T)(Object)clone;}
  public static implicit operator bool(Object o){return !ReferenceEquals(o,null)&&!o.destroyed;}
 }
 public class Component:Object
 {
  public GameObject gameObject; public Transform transform=>gameObject.transform;
  public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
  public T[] GetComponentsInChildren<T>(bool includeInactive=false) where T:Component=>gameObject.GetComponentsInChildren<T>();
 }
 public class MonoBehaviour:Component{}
 public class GameObject:Object
 {
  public static readonly List<GameObject> Created=new List<GameObject>();
  public Transform transform; public int layer; public bool activeSelf=true; public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);
  public readonly List<Component> components=new List<Component>();
  public GameObject(string n="GameObject"){name=n;transform=new Transform{gameObject=this};components.Add(transform);Created.Add(this);}
  public T AddComponent<T>() where T:Component => (T)AddComponent(typeof(T));
  public Component AddComponent(Type type)
  {
   foreach(RequireComponent req in type.GetCustomAttributes(typeof(RequireComponent),true))foreach(Type t in req.types)if(!components.Any(x=>t.IsInstanceOfType(x)))AddComponent(t);
   var c=(Component)Activator.CreateInstance(type);c.gameObject=this;c.name=name;components.Add(c);
   var awake=type.GetMethod("Awake",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(awake!=null)awake.Invoke(c,null);return c;
  }
  public T GetComponent<T>() where T:Component=>components.OfType<T>().FirstOrDefault();
  public T[] GetComponentsInChildren<T>(bool includeInactive=false) where T:Component=>components.OfType<T>().Concat(transform.children.SelectMany(c=>c.gameObject.GetComponentsInChildren<T>())).ToArray();
  public void SetActive(bool v){if(activeSelf==v)return;activeSelf=v;if(!v)foreach(var c in components.ToArray())c.GetType().GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.Invoke(c,null);}
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
  public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
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
  public Vector3 InverseTransformPoint(Vector3 v){N.Matrix4x4.Invert(matrix,out var inv);return Vector3.From(N.Vector3.Transform(v.N,inv));}
  public Transform Find(string n)=>children.FirstOrDefault(c=>c.gameObject.name==n);
 }
 public class Animator:Component { public float speed;public bool enabled; }
 public class Renderer:Component {public string sortingLayerName; public bool enabled=true; public int sortingOrder;}
 public class ParticleSystemRenderer:Renderer {}
 public class SpriteRenderer:Renderer {}
 public static class LayerMask{public static int NameToLayer(string n)=>n=="Effect"?8:n=="Character"?20:n=="Default"?0:-1;}
 public static class Mathf{public const float Rad2Deg=57.2957795f;public const float Epsilon=float.Epsilon;public static float Atan2(float a,float b)=>(float)Math.Atan2(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Abs(float a)=>Math.Abs(a);public static float Clamp01(float a)=>Math.Min(1,Math.Max(0,a));}
 public static class Time{public static float deltaTime=1f/60;public static int frameCount;}
}

namespace UnityEngine
{
 public enum TextureFormat { RGBA32 }
 public class Shader:Object { public bool isSupported=true; }
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1f){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color clear=>new Color(0,0,0,0);}
 public enum CameraClearFlags { SolidColor }
 public enum RenderTextureFormat { ARGB32 }
 public class RenderTexture:Object
 {
  public static RenderTexture active;public bool released;public RenderTexture(int w,int h,int depth,RenderTextureFormat format){}
  public void Create(){}public void Release(){released=true;}
 }
 public class Camera:Component
 {
  public static Camera main { get { return null; } }
  public static int Renders;public bool enabled,orthographic;public float orthographicSize,nearClipPlane,farClipPlane;
  public int cullingMask;public Color backgroundColor;public CameraClearFlags clearFlags;public RenderTexture targetTexture;
  public void Render(){Renders++;}
 }
 public class AssetBundle:Object
 {
  public static AssetBundle LoadFromFile(string path){return new AssetBundle();}
  public T LoadAsset<T>(string path) where T:Object,new(){return new T();}
  public void Unload(bool unloadAllLoadedObjects){}
 }
 public enum FilterMode { Bilinear }
 public enum TextureWrapMode { Clamp }
 public class Texture2D:Object
 {
  public readonly bool linear,mipChain;public bool readable=true;public FilterMode filterMode;public TextureWrapMode wrapMode;
  public Texture2D(int w,int h,TextureFormat format,bool mipChain,bool linear){this.mipChain=mipChain;this.linear=linear;}
 }
 public static class ImageConversion
 {
  public static bool LoadImage(Texture2D texture,byte[] bytes,bool markNonReadable){texture.readable=!markNonReadable;return bytes.Length>0;}
 }
 public static class Resources
 {
  public static bool Missing;public static readonly List<string> Requests=new List<string>();
  public static T Load<T>(string path) where T:Object {Requests.Add(path);return Missing?null:(T)(Object)new GameObject(path);}
 }
 public static class Debug { public static void LogError(string text){} }
}
