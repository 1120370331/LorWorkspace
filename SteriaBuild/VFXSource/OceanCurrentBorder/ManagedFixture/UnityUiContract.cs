// Bounded managed UI adapter. It records production-generated vertices; it is not Unity rendering.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using N = System.Numerics;
namespace UnityEngine {
 public class Object { public string name; }
 public class Component:Object { public GameObject gameObject; public Transform transform=>gameObject.transform; public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>(); }
 public class MonoBehaviour:Component { public bool enabled=true; }
 public class GameObject:Object {
  public Transform transform; public bool activeSelf=true; public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);
  public List<Component> components=new List<Component>();
  public GameObject(string name,params Type[] types){this.name=name;transform=new RectTransform{gameObject=this};components.Add(transform);foreach(var type in types)if(type!=typeof(RectTransform))AddComponent(type);}
  public Component AddComponent(Type type){var c=(Component)Activator.CreateInstance(type);c.gameObject=this;components.Add(c);if(activeInHierarchy)Invoke(c,"OnEnable");return c;}
  public T AddComponent<T>() where T:Component=>(T)AddComponent(typeof(T));
  public T GetComponent<T>() where T:Component=>components.OfType<T>().FirstOrDefault();
  public T[] GetComponentsInChildren<T>() where T:Component=>components.OfType<T>().Concat(transform.children.SelectMany(x=>x.gameObject.GetComponentsInChildren<T>())).ToArray();
  public void SetActive(bool value){if(value==activeSelf)return;activeSelf=value;foreach(var component in GetComponentsInChildren<Component>())Invoke(component,value?"OnEnable":"OnDisable");}
  private static void Invoke(Component c,string method){c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.Invoke(c,null);}
 }
 public struct Vector2 {
  public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);
  public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);public static Vector2 up=>new Vector2(0,1);public static Vector2 down=>new Vector2(0,-1);public static Vector2 left=>new Vector2(-1,0);public static Vector2 right=>new Vector2(1,0);
  public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public static Vector2 operator*(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);public static Vector2 operator/(Vector2 a,float b)=>new Vector2(a.x/b,a.y/b);
  public static Vector2 Min(Vector2 a,Vector2 b)=>new Vector2(Math.Min(a.x,b.x),Math.Min(a.y,b.y));public static Vector2 Max(Vector2 a,Vector2 b)=>new Vector2(Math.Max(a.x,b.x),Math.Max(a.y,b.y));
  public static implicit operator Vector3(Vector2 a)=>new Vector3(a.x,a.y,0);public static implicit operator Vector2(Vector3 a)=>new Vector2(a.x,a.y);
 }
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
  public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);
  public N.Vector3 Native=>new N.Vector3(x,y,z);public static Vector3 From(N.Vector3 a)=>new Vector3(a.X,a.Y,a.Z);
  public static Vector3 operator+(Vector3 a,Vector3 b)=>From(a.Native+b.Native);public static Vector3 operator-(Vector3 a,Vector3 b)=>From(a.Native-b.Native);public static Vector3 operator*(Vector3 a,float b)=>From(a.Native*b);
 }
 public struct Quaternion {public N.Quaternion Native;public static Quaternion identity=>new Quaternion{Native=N.Quaternion.Identity};public static Quaternion Euler(float z)=>new Quaternion{Native=N.Quaternion.CreateFromAxisAngle(N.Vector3.UnitZ,z*(float)Math.PI/180)};}
 public class Transform:Component {
  public Transform parent;public List<Transform> children=new List<Transform>();public Vector3 localPosition,localScale=Vector3.one;public Quaternion localRotation=Quaternion.identity;
  public N.Matrix4x4 Matrix=>N.Matrix4x4.CreateScale(localScale.Native)*N.Matrix4x4.CreateFromQuaternion(localRotation.Native)*N.Matrix4x4.CreateTranslation(localPosition.Native)*(parent==null?N.Matrix4x4.Identity:parent.Matrix);
  public void SetParent(Transform value,bool worldPositionStays){parent?.children.Remove(this);parent=value;parent?.children.Add(this);}
  public Vector3 TransformPoint(Vector3 value)=>Vector3.From(N.Vector3.Transform(value.Native,Matrix));
  public Vector3 InverseTransformPoint(Vector3 value){N.Matrix4x4.Invert(Matrix,out var inv);return Vector3.From(N.Vector3.Transform(value.Native,inv));}
  public Vector3 TransformVector(Vector3 value)=>Vector3.From(N.Vector3.TransformNormal(value.Native,Matrix));
  public Vector3 InverseTransformVector(Vector3 value){N.Matrix4x4.Invert(Matrix,out var inv);return Vector3.From(N.Vector3.TransformNormal(value.Native,inv));}
  public Vector3 position {get=>TransformPoint(Vector3.zero);set=>localPosition=parent==null?value:parent.InverseTransformPoint(value);}
 }
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;}public float xMin=>x;public float yMin=>y;public float xMax=>x+width;public float yMax=>y+height;}
 public class RectTransform:Transform {
  public Vector2 anchorMin,anchorMax,pivot=new Vector2(.5f,.5f),sizeDelta,offsetMin,offsetMax;
  public Vector2 anchoredPosition {get=>localPosition;set=>localPosition=value;}
  public Rect rect=>new Rect(-sizeDelta.x*pivot.x,-sizeDelta.y*pivot.y,sizeDelta.x,sizeDelta.y);
  public enum Axis{Horizontal,Vertical}public void SetSizeWithCurrentAnchors(Axis axis,float value){if(axis==Axis.Horizontal)sizeDelta.x=value;else sizeDelta.y=value;}
  public void GetWorldCorners(Vector3[] corners){var r=rect;corners[0]=TransformPoint(new Vector3(r.xMin,r.yMin,0));corners[1]=TransformPoint(new Vector3(r.xMin,r.yMax,0));corners[2]=TransformPoint(new Vector3(r.xMax,r.yMax,0));corners[3]=TransformPoint(new Vector3(r.xMax,r.yMin,0));}
 }
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public struct Bounds{public Vector3 min,max;public Vector3 size=>max-min;}
 public class Material {}public class Sprite {}
 public static class Mathf{public const float PI=(float)Math.PI;public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Abs(float a)=>Math.Abs(a);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float Sin(float a)=>(float)Math.Sin(a);public static float Cos(float a)=>(float)Math.Cos(a);public static float Repeat(float a,float b)=>a-(float)Math.Floor(a/b)*b;public static float Lerp(float a,float b,float t)=>a+(b-a)*Math.Max(0,Math.Min(t,1));}
 public static class Time {public static float unscaledTime;}
 public class Canvas{public static event Action willRenderCanvases;}
 public class CanvasRenderer{public void Clear(){}}
}
namespace UnityEngine.UI {
 using UnityEngine;
 public class Graphic:MonoBehaviour{public bool raycastTarget=true;public Color color;public Material material;public RectTransform rectTransform=>(RectTransform)transform;}
 public class MaskableGraphic:Graphic {public CanvasRenderer canvasRenderer=new CanvasRenderer();public void SetVerticesDirty(){}protected virtual void OnPopulateMesh(VertexHelper vh){} }
 public class Image:Graphic {public Sprite sprite;public int type;public bool preserveAspect,fillCenter;}
 public class VertexHelper {public List<Vector3> vertices=new List<Vector3>();public List<Color> colors=new List<Color>();public List<int> triangles=new List<int>();public int currentVertCount=>vertices.Count;public void Clear(){vertices.Clear();colors.Clear();triangles.Clear();}public void AddVert(Vector3 point,Color color,Vector2 uv){vertices.Add(point);colors.Add(color);}public void AddTriangle(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}}
 public static class LayoutRebuilder{public static void ForceRebuildLayoutImmediate(RectTransform rect){}}
}
namespace TMPro {
 using UnityEngine;
 public enum TextAlignmentOptions{TopLeft=257}public enum TextOverflowModes{Overflow}
 public class TextMeshProUGUI:UnityEngine.UI.Graphic{public string text;public float fontSize,lineSpacing;public bool enableAutoSizing,enableWordWrapping;public TextAlignmentOptions alignment;public TextOverflowModes overflowMode;public Bounds textBounds;public Vector2 GetPreferredValues(string s)=>Vector2.zero;public void ForceMeshUpdate(bool x){}}
}
namespace HarmonyLib {public static class AccessTools{public static MethodInfo Method(Type type,string name)=>type.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);}}
public enum BehaviourType {Atk,Standby}
public class DiceBehaviour {public BehaviourType Type;public int Min=4,Dice=7;}
public class BattleDiceCardModel {public int Id;public List<DiceBehaviour> GetBehaviourList()=>new List<DiceBehaviour>();}
public class BattleDiceCardUI:UnityEngine.MonoBehaviour {public BattleDiceCardModel CardModel;public UnityEngine.UI.Image[] img_linearDodges;public object[] bufIconListUI=new object[0];public List<BattleDiceCard_BehaviourDescUI> ui_behaviourDescList=new List<BattleDiceCard_BehaviourDescUI>();public void EnableAddedIcons(){} }
public class BattleDiceCard_BehaviourDescUI:UnityEngine.MonoBehaviour{public TMPro.TextMeshProUGUI txt_range;}
namespace Steria {
 public class Plan{public int Levels,Amount;public bool Mass,Proxy;public List<int> DiceIndices=new List<int>();}
 public static class FlowPlanning{public static Dictionary<BattleDiceCardModel,Plan> Plans=new Dictionary<BattleDiceCardModel,Plan>();public static Plan Get(BattleDiceCardModel card)=>card!=null&&Plans.TryGetValue(card,out var plan)?plan:null;}
}
