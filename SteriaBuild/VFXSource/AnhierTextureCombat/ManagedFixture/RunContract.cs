using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Battle.DiceAttackEffect;
using UnityEngine;
using Steria;
class RunContract
{
 static string Here,Reference,Output,Ship;static JsonNode Skins,Natives;static GameObject stage;static List<string> checks=new List<string>();static List<object> evidence=new List<object>();
 static BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static string[] profiles={"SeaPierce","SeaFarHit","MemorySlash","MemoryPierce","MemoryHit","MemoryGuard"};
 static JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
 static float F(JsonNode n,string k)=>n[k].GetValue<float>();static long L(JsonNode n,string k)=>n[k].GetValue<long>();static string S(JsonNode n,string k)=>n[k].GetValue<string>();
 static Vector3 V(JsonNode n)=>new Vector3(F(n,"x"),F(n,"y"),F(n,"z"));static Quaternion Q(JsonNode n)=>new Quaternion(F(n,"x"),F(n,"y"),F(n,"z"),F(n,"w"));
 static ActionDetail Action(string p)=>p=="MemorySlash"?ActionDetail.Slash:p=="MemoryHit"?ActionDetail.Hit:p=="MemoryGuard"?ActionDetail.Guard:p=="SeaFarHit"?ActionDetail.Fire:ActionDetail.Penetrate;
 static string Suffix(string p)=>p=="MemorySlash"?"J":p=="MemoryHit"?"H":p=="MemoryGuard"?"G":"Z";
 static void Main(string[] args)
 {
  Here=Path.GetFullPath(args[0]);Reference=Path.Combine(Here,"UnityProject/Assets/Reference");Output=Path.Combine(Here,"previews");Ship=Path.GetFullPath(Path.Combine(Here,"../../SteriaModFolder/Resource/Effects/AnhierTextureCombat"));
  Skins=JsonNode.Parse(File.ReadAllText(Path.Combine(Reference,"native-skins.json")));Natives=JsonNode.Parse(File.ReadAllText(Path.Combine(Reference,"native-effects.json")));
  var cases=new List<object>();
  foreach(string profile in profiles)foreach(bool left in new[]{true,false})
  {
   var variants=new Dictionary<string,object>();
   foreach(string variant in new[]{"candidate","native"})
   {
    Scene();string skin=profile=="SeaFarHit"?"Fire":"Bada";var self=Actor(skin,Action(profile),left,3,left?5:-5);var target=Actor("Bada",ActionDetail.Guard,!left,3,left?-6.5f:6.5f);
    var fx=variant=="candidate"?Candidate(profile,self,target,1):Native(profile,self,target);float life=Life(fx);var core=fx.GetComponent<AnhierTextureTimeline>();
    if(variant=="candidate")
    {
     bool far=profile=="SeaFarHit";var intended=far?SingletonBehavior<BattleSceneRoot>.Instance.transform:self.charAppearance.GetAtkEffectPivot(Action(profile));
     Check(fx.transform.parent==intended,profile+" "+left+" correct self/neutral-scene parent");
     Check(Math.Abs(fx.transform.localScale.x-(profile=="MemoryGuard"?2:4))<.0001f,profile+" native 2*(1+roll), guard half");
     Check(!fx.HasDamagedEffect,profile+" engine controls victory-only damaged spark");
     Tick(fx,.25f,false);Check(core.Alpha>.999f,profile+" full native alpha through .25 seconds");
     fx.SetDestroyTime(2);Tick(fx,.5f,false);Check(core.Alpha<.4f && Alive(fx.gameObject),profile+" SetDestroyTime changes death, preserves initialized visual clock");fx.SetDestroyTime(life);Tick(fx,0,false);
     var renderers=fx.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name=="Pigment"||s.name=="FoamOrFire"||s.name=="TargetWaterSplash").ToArray();
     Check(renderers.Length==(far?3:2),profile+" only intended disjoint layers");
     foreach(var sr in renderers)Check(sr.gameObject.layer==8&&sr.sortingLayerID==-1396713383,profile+" native Effect / Particle layer");
     if(far){var contact=renderers.Single(s=>s.name=="TargetWaterSplash");Check(contact.enabled&&contact.color.a>.999f,"far event-frame hit alpha");Check(Vector3.Distance(contact.transform.position,target.atkEffectRoot.position)<.0001f,"far Update reanchors target after native SetScale");Check(contact.transform.parent==fx.transform,"far contact keeps attacker facing and neutral scale");}
     evidence.Add(new{profile,left,mapScale=3,roll=1,lifetime=life,parent=fx.transform.parent.gameObject.name,pivot=Matrix(self.charAppearance.GetAtkEffectPivot(Action(profile))),self=Matrix(self.transform),target=Matrix(target.atkEffectRoot),effect=Matrix(fx.transform)});
    }
    var frames=new List<object>();for(int i=0;i<=60;i++){float t=i/60f;Tick(fx,t,variant=="native");frames.Add(new{time=t,alive=Alive(fx.gameObject),layers=Layers(fx)});}
    Check(!Alive(fx.gameObject),variant+" "+profile+" native lifecycle destruction");
    variants[variant]=frames;
   }
   cases.Add(new{profile,left,mapScale=3,roll=1,skin=profile=="SeaFarHit"?"Fire":"Bada",action=(int)Action(profile),variants});
  }
  // Risk-directed scalar, mirror and full-matrix contracts across native map sizes.
  foreach(float map in new[]{1.5f,2.25f,3f})foreach(bool left in new[]{true,false})foreach(float power in new[]{0f,1f,2f})
  {
   Scene();var self=Actor("Bada",ActionDetail.Penetrate,left,map,5);var target=Actor("Bada",ActionDetail.Guard,!left,map,-6.5f);
   Transform z=new GameObject("ExplicitZRotated").transform;z.SetParent(self.charAppearance.transform,false);z.localPosition=new Vector3(-2.1f,2.4f,0);z.localRotation=Quaternion.Euler(0,0,18);z.localScale=new Vector3(1.15f,.9f,1);self.charAppearance.atkEffectPivot_Z=z;
   var fx=Candidate("MemoryPierce",self,target,power);float expected=2*(1+power);Check(Vector3.Distance(fx.transform.TransformPoint(Vector3.right),z.TransformPoint(Vector3.right*expected))<.0001f,"full pivot matrix map="+map+" left="+left+" roll="+power);
   Tick(fx,.667f,false);Check(fx.GetComponent<AnhierTextureTimeline>().Complete&&Alive(fx.gameObject),"native .667 visuals end while 1s object alive");Tick(fx,1f,false);Check(!Alive(fx.gameObject),"native incoming lifetime destroys at 1s");
   var far=Candidate("SeaFarHit",self,target,power);Tick(far,0,false);Check(Math.Abs(far.transform.lossyScale.x-expected)<.0001,"far neutral root excludes actor mapScale="+map+" roll="+power);
  }
  // Exact lifecycle discrepancy regression: native's initialized Animator.speed
  // stays fixed after SetDestroyTime; compare candidate with original native method.
  foreach(float incoming in new[]{.5f,1f,2f})
  {
   Scene();var self=Neutral(ActionDetail.Slash,true,3,5);var target=Neutral(ActionDetail.Guard,false,3,-6.5f);
   var candidate=Candidate("MemorySlash",self,target,1,incoming);var native=Native("MemorySlash",self,target,incoming);
   candidate.SetDestroyTime(3);native.SetDestroyTime(3);
   foreach(float t in new[]{.5f*incoming,.667f*incoming})
   {
    Tick(candidate,t,false);Tick(native,t,true);float na=native.GetComponent<SpriteRenderer>().color.a;
    Check(Math.Abs(candidate.GetComponent<AnhierTextureTimeline>().Alpha-na)<.0001f,"native visual clock unchanged by SetDestroyTime at incoming="+incoming+" t="+t);
    Check(Alive(candidate.gameObject)&&Alive(native.gameObject),"extended native/candidate objects stay alive after visual end");
   }
   var shortCandidate=Candidate("MemorySlash",self,target,1,incoming);var shortNative=Native("MemorySlash",self,target,incoming);shortCandidate.SetDestroyTime(.2f);shortNative.SetDestroyTime(.2f);Tick(shortCandidate,.2f,false);Tick(shortNative,.2f,true);Check(!Alive(shortCandidate.gameObject)&&!Alive(shortNative.gameObject),"shorter SetDestroyTime kills at .2 without retiming clip");
  }
  var neutralCases=new List<object>();
  foreach(string profile in profiles)foreach(float actorScale in new[]{3f,2.25f})
  {
   bool left=actorScale==3;bool far=profile=="SeaFarHit";float selfX=far?5f:3f;float targetX=far?6.5f:3f;
   Scene();var self=Neutral(Action(profile),left,actorScale,left?selfX:-selfX);var target=Neutral(ActionDetail.Guard,!left,actorScale,left?-targetX:targetX);
   var fx=Candidate(profile,self,target,1);var frames=new List<object>();
   for(int i=0;i<=60;i++){Tick(fx,i/60f,false);frames.Add(new{time=i/60f,alive=Alive(fx.gameObject),layers=Layers(fx)});}
   neutralCases.Add(new{profile,left,actorScale,action=(int)Action(profile),selfMatrix=Matrix(self.characterRotationCenter),targetMatrix=Matrix(target.characterRotationCenter),pivot=Matrix(self.charAppearance.GetAtkEffectPivot(Action(profile))),frames});
  }
  File.WriteAllText(Path.Combine(Output,"independent-frames.json"),JsonSerializer.Serialize(new{kind="independent-neutral-mannequin-fit-simulation",fps=60,cases=neutralCases},Json));
  File.WriteAllText(Path.Combine(Output,"managed-frames.json"),JsonSerializer.Serialize(new{kind="managed-native-contract-simulation",fps=60,cases},Json));
  File.WriteAllText(Path.Combine(Output,"runtime-evidence.json"),JsonSerializer.Serialize(new{kind="managed-native-contract-simulation",rows=evidence},Json));
  File.WriteAllText(Path.Combine(Output,"checks.txt"),string.Join("\n",checks));Console.WriteLine("MANAGED_NATIVE_CONTRACT_PASS "+checks.Count+" checks");
 }
 static void Scene(){stage=new GameObject("NeutralBattleSceneRoot");SingletonBehavior<BattleSceneRoot>.Instance=stage.AddComponent<BattleSceneRoot>();}
 static BattleUnitView Actor(string skin,ActionDetail action,bool left,float scale,float x)
 {
  var data=Skins["skins"].AsArray().Single(n=>S(n,"name")==skin);var go=new GameObject(skin+"View");go.transform.SetParent(stage.transform,false);go.transform.localPosition=new Vector3(x*scale,0,0);var view=go.AddComponent<BattleUnitView>();
  var rotation=new GameObject("characterRotationCenter").transform;rotation.SetParent(go.transform,false);rotation.localScale=Vector3.one*scale;rotation.localRotation=Quaternion.Euler(0,left?0:180,0);view.characterRotationCenter=rotation;
  var trans=new Dictionary<long,Transform>();foreach(var t in data["transforms"].AsArray())trans[L(t,"id")]=new GameObject(S(t,"name")).transform;
  foreach(var t in data["transforms"].AsArray()){var tr=trans[L(t,"id")];long parent=L(t,"parent");tr.SetParent(parent==0?rotation:trans[parent],false);tr.localPosition=V(t["position"]);tr.localRotation=Q(t["rotation"]);tr.localScale=V(t["scale"]);}
  var appearance=trans[L(data,"appearanceTransform")].gameObject.AddComponent<CharacterAppearance>();appearance.atkEffectRoot=trans[L(data,"root")];
  foreach(var kv in data["pivots"].AsObject()){long id=kv.Value.GetValue<long>();if(id!=0)typeof(CharacterAppearance).GetField("atkEffectPivot_"+kv.Key).SetValue(appearance,trans[id]);}
  view.charAppearance=appearance;view.atkEffectRoot=appearance.atkEffectRoot;view.model=new BattleUnitModel{view=view};return view;
 }
 static BattleUnitView Neutral(ActionDetail action,bool left,float scale,float x)
 {
  var root=new GameObject("IndependentMannequinView");root.transform.SetParent(stage.transform,false);root.transform.position=new Vector3(x*scale,0,0);var view=root.AddComponent<BattleUnitView>();
  var rotation=new GameObject("characterRotationCenter").transform;rotation.SetParent(root.transform,false);rotation.localScale=Vector3.one*scale;rotation.localRotation=Quaternion.Euler(0,left?0:180,0);view.characterRotationCenter=rotation;
  var appearance=new GameObject("IndependentGenericAppearance").AddComponent<CharacterAppearance>();appearance.transform.SetParent(rotation,false);
  var contact=new GameObject("GenericTorsoContact").transform;contact.SetParent(appearance.transform,false);contact.localPosition=new Vector3(-.25f,2.25f,0);appearance.atkEffectRoot=contact;
  foreach(var pair in new[]{new{letter="J",point=new Vector3(-1.6f,2.45f,0)},new{letter="Z",point=new Vector3(-1.65f,2.55f,0)},new{letter="H",point=new Vector3(-1.5f,2.3f,0)},new{letter="G",point=new Vector3(-1.4f,2.25f,0)},new{letter="F",point=new Vector3(-1.65f,2.55f,0)}})
  {var hand=new GameObject("AuthoredHand_"+pair.letter).transform;hand.SetParent(appearance.transform,false);hand.localPosition=pair.point;typeof(CharacterAppearance).GetField("atkEffectPivot_"+pair.letter).SetValue(appearance,hand);}
  view.model=new BattleUnitModel{view=view};view.charAppearance=appearance;view.atkEffectRoot=contact;return view;
 }
 static DiceAttackEffect Candidate(string profile,BattleUnitView self,BattleUnitView target,float roll,float incoming=1f)
 {
  var go=new GameObject(profile);go.transform.SetParent(stage.transform,false);var type=typeof(DiceAttackEffect_Steria_AnhierTextureBase).Assembly.GetType("DiceAttackEffect_Steria_AnhierTexture"+profile);var effect=(DiceAttackEffect)go.AddComponent(type);effect.Initialize(self,target,incoming);effect.SetScale(roll);effect.SetLayer("Effect");return effect;
 }
 static DiceAttackEffect Native(string profile,BattleUnitView self,BattleUnitView target,float incoming=1f)
 {
  bool far=profile=="SeaFarHit";var data=Natives["effects"].AsArray().Single(n=>S(n,"profile")==profile);var go=new GameObject("Native_"+Suffix(profile));go.transform.SetParent(stage.transform,false);var effect=far?(DiceAttackEffect)go.AddComponent<DiceAttackEffect_RedHoodMercenary_Gun>():go.AddComponent<DiceAttackEffect>();
  foreach(var item in data["sprites"].AsArray())
  {
   string name=S(item,"name");if(name=="Trail")continue;
   bool root=!far||name=="RedHoodMercenary_Gun";var sr=root?go.GetComponent<SpriteRenderer>():new GameObject(name).AddComponent<SpriteRenderer>();if(!root){sr.transform.SetParent(go.transform,false);sr.transform.localPosition=V(item["position"]);sr.transform.localRotation=Q(item["rotation"]);sr.transform.localScale=V(item["scale"]);}
   var tex=new Texture2D(2,2){name=S(item,"file")};ImageConversion.LoadImage(tex,File.ReadAllBytes(Path.Combine(Reference,tex.name)),true);sr.sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(F(item,"pivotX"),F(item,"pivotY")),F(item,"ppu"));sr.sprite.name=tex.name;sr.sortingLayerID=item["sortingLayer"].GetValue<int>();sr.sortingOrder=item["sortingOrder"].GetValue<int>();
   if(far&&name=="Hit")typeof(DiceAttackEffect_RedHoodMercenary_Gun).GetField("_targetEffect",Fields).SetValue(effect,sr);if(far&&name=="Bullet"){typeof(DiceAttackEffect_RedHoodMercenary_Gun).GetField("_bullet",Fields).SetValue(effect,sr);sr.gameObject.SetActive(false);}
  }
  typeof(DiceAttackEffect).GetField("additionalScale",Fields).SetValue(effect,Vector3.one*(far?1:profile=="MemoryGuard"?-.5f:0));effect.Initialize(self,target,incoming);effect.SetScale(1);effect.SetLayer("Effect");
  if(far)effect.transform.rotation=self.characterRotationCenter.rotation;
  return effect;
 }
 static float Life(DiceAttackEffect e)=>(float)typeof(DiceAttackEffect).GetField("_destroyTime",Fields).GetValue(e);
 static void Tick(DiceAttackEffect e,float time,bool native)
 {
  if(!Alive(e.gameObject))return;typeof(DiceAttackEffect).GetField("_elapsed",Fields).SetValue(e,time-Time.deltaTime);
  e.GetType().GetMethod("Update",Fields).Invoke(e,null);
  if(native){bool far=e is DiceAttackEffect_RedHoodMercenary_Gun;float clipTime=time*e.GetComponent<Animator>().speed;float d=Math.Max(0,clipTime-(far?2f/3:.25f));float u=Mathf.Clamp01(d*3);float alpha=far?1-u*u*(3-2*u):Mathf.Clamp01(27.647995f*d*d*d-17.279999f*d*d+1);if(clipTime>=(far?1f:2f/3))alpha=0;foreach(var sr in e.GetComponentsInChildren<SpriteRenderer>()){sr.color=new Color(1,1,1,alpha);sr.enabled=alpha>0;}}
 }
 static bool Alive(GameObject go)=>go.activeInHierarchy&&!go.destroyed&&(go.transform.parent==null||Alive(go.transform.parent.gameObject));
 static float[] Matrix(Transform tr){var m=tr.matrix;return new[]{m.M11,m.M12,m.M13,m.M14,m.M21,m.M22,m.M23,m.M24,m.M31,m.M32,m.M33,m.M34,m.M41,m.M42,m.M43,m.M44};}
 static object[] Layers(DiceAttackEffect e)=>e.GetComponentsInChildren<SpriteRenderer>().Where(sr=>sr.sprite!=null).Select(sr=>(object)new{name=sr.gameObject.name,file=sr.sprite.texture.name,ppu=sr.sprite.pixelsPerUnit,pivot=new[]{sr.sprite.pivot.x,sr.sprite.pivot.y},size=new[]{sr.sprite.texture.width,sr.sprite.texture.height},alpha=sr.color.a,enabled=sr.enabled&&Alive(sr.gameObject),sortingLayer=sr.sortingLayerID,order=sr.sortingOrder,matrix=Matrix(sr.transform)}).ToArray();
 static void Check(bool pass,string name){if(!pass)throw new Exception("FAIL "+name);checks.Add("PASS "+name);}
}
