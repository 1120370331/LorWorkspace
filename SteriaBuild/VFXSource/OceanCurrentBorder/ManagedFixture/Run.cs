using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.Json;
using Steria;
using UnityEngine;
using UnityEngine.UI;
class Run {
 static string Out="output/ocean-current-vfx/ui/managed";
 static List<string> checks=new List<string>();static List<object> shots=new List<object>();
 static BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool condition,string label){if(!condition)throw new Exception(label);checks.Add("PASS "+label);}
 static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var go=new GameObject(name,typeof(RectTransform));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.localPosition=pos;rect.sizeDelta=size;return rect;}
 static BattleDiceCardUI Card(Transform parent,float x){var root=Rect("Card",parent,new Vector2(x,0),Vector2.zero);var ui=root.gameObject.AddComponent<BattleDiceCardUI>();ui.CardModel=new BattleDiceCardModel{Id=9002002};var frame=Rect("NativeLinearDodge",root,Vector2.zero,new Vector2(208,360));ui.img_linearDodges=new[]{frame.gameObject.AddComponent<Image>()};return ui;}
 static FlowCardPreviewStamp Stamp(BattleDiceCardUI ui)=>ui.GetComponent<FlowCardPreviewStamp>();
 static void Main(){try{Work();Console.WriteLine("BORDER_PRODUCTION_MESH_PASS "+checks.Count);}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Work(){
  Directory.CreateDirectory(Out);
  var trace=File.ReadAllLines(Out+"/real-mechanics-trace.csv").Select(x=>x.Split(',')).ToDictionary(x=>x[0],x=>int.Parse(x[1]));
  var hand=Rect("Hand",null,new Vector2(0,0),Vector2.zero);
  var left=Card(hand,-260);var center=Card(hand,0);var right=Card(hand,260);var cards=new[]{left,center,right};
  foreach(var ui in cards)FlowCardPreview.Apply(ui);
  Check(cards.All(x=>Stamp(x)==null),"plain card instances never allocate a border");Save(cards,"plain",0);
  FlowPlanning.Plans[center.CardModel]=new Plan{Levels=trace["enhanced"]};FlowCardPreview.Apply(center);
  var stamp=Stamp(center);var border=stamp.CurrentBorder;
  Check(border.enabled&&!border.raycastTarget,"production Apply binds one noninteractive persistent graphic");
  Check(Stamp(left)==null&&Stamp(right)==null,"same XML ID is not treated as the same enhanced card instance");
  FlowCardPreview.Flash(center);typeof(FlowCardPreviewStamp).GetMethod("UpdateFlash",Flags).Invoke(stamp,new object[]{.31f});
  Check(!stamp.FlashImages[0].gameObject.activeSelf&&border.enabled,"success flash expiration leaves persistent border active");
  for(int frame=0;frame<144;frame++)Save(cards,"animation",frame/30f);
  int count=center.gameObject.GetComponentsInChildren<Component>().Length;
  for(int repeat=0;repeat<20;repeat++)FlowCardPreview.Apply(center);
  Check(center.gameObject.GetComponentsInChildren<Component>().Length==count&&center.gameObject.GetComponentsInChildren<FlowCardCurrentBorder>().Length==1,"twenty production Apply calls preserve exact component and object counts");
  FlowPlanning.Plans[center.CardModel].Levels=trace["twice"];FlowCardPreview.Apply(center);Save(cards,"twice",.6f);
  Check(stamp.CurrentBorder==border,"second enhancement reuses existing mesh");
  FlowPlanning.Plans[center.CardModel].Levels=trace["next-round"];FlowCardPreview.Apply(center);
  Check(border.enabled,"real-mechanics next-round state keeps current visible");
  center.gameObject.SetActive(false);Check(!border.enabled,"actual stamp OnDisable hides pooled current");
  center.gameObject.SetActive(true);Check(border.enabled,"actual stamp OnEnable restores same-card enhancement");
  var native=center.img_linearDodges[0].rectTransform;native.sizeDelta=new Vector2(260,300);native.localScale=new Vector3(.8f,1.2f,1);center.transform.localScale=Vector3.one*.72f;
  border.Tick(.6f);Check(Math.Abs(border.rectTransform.rect.width-208)<.01f&&Math.Abs(border.rectTransform.rect.height-360)<.01f,"zero-size card root and independently scaled native frame produce 208x360 local perimeter");
  Save(cards,"scaled-native-frame",.6f);center.transform.localScale=Vector3.one;native.sizeDelta=new Vector2(208,360);native.localScale=Vector3.one;
  var half=Rect("NativeRightHalf",center.transform,new Vector2(52,0),new Vector2(104,360)).gameObject.AddComponent<Image>();
  native.sizeDelta=new Vector2(104,360);native.localPosition=new Vector2(-52,0);center.img_linearDodges=new[]{center.img_linearDodges[0],half};border.Tick(.6f);
  Check(Math.Abs(border.rectTransform.rect.width-208)<.01f&&border.rectTransform.localPosition.magnitude<.01f,"split native image rectangles use transformed geometric union");Save(cards,"split-frame-union",.6f);
  center.img_linearDodges=new[]{center.img_linearDodges[0]};native.sizeDelta=new Vector2(208,360);native.localPosition=Vector3.zero;
  var old=center.CardModel;FlowCardPreview.Restore(center);center.CardModel=left.CardModel;FlowCardPreview.Apply(center);
  Check(!border.enabled,"production Restore and Apply remove border on same-ID unenhanced UI reuse");Save(cards,"reused-plain",.6f);
  center.CardModel=old;FlowCardPreview.Apply(center);Check(border.enabled&&center.gameObject.GetComponentsInChildren<FlowCardCurrentBorder>().Length==1,"rebinding original enhanced model restores existing component");
  FlowPlanning.Plans.Remove(old);border.Tick(.9f);Check(!border.enabled&&trace["consumed"]==0,"actual consumption trace removes mesh on Tick without Apply");
  FlowCardPreview.Apply(center);Save(cards,"consumed",.9f);
  FlowPlanning.Plans.Clear();border.Tick(1);Check(!border.enabled&&trace["reset"]==0,"reset leaves no border");
  FlowPlanning.Plans[old]=new Plan{Levels=1};FlowCardPreview.Apply(center);
  typeof(FlowCardPreviewStamp).GetMethod("OnDestroy",Flags).Invoke(stamp,null);Check(!border.enabled,"destroying owner stamp clears the persistent graphic");
  FlowCardPreview.Apply(center);center.CardModel=null;FlowCardPreview.Apply(center);Check(!border.enabled,"null-model pooled UI clears before Apply early return");
  File.WriteAllLines(Out+"/mesh-checks.txt",checks);File.WriteAllText(Out+"/production-meshes.json",JsonSerializer.Serialize(shots));
 }
 static void Save(BattleDiceCardUI[] cards,string state,float time){
  var views=new List<object>();
  foreach(var ui in cards){
   var stamp=Stamp(ui);var border=stamp?.CurrentBorder;border?.Tick(time);
   var vh=new VertexHelper();if(border!=null&&border.enabled)typeof(FlowCardCurrentBorder).GetMethod("OnPopulateMesh",Flags).Invoke(border,new object[]{vh});
   var vertices=new List<float[]>();for(int i=0;i<vh.vertices.Count;i++){var p=border.transform.TransformPoint(vh.vertices[i]);var c=vh.colors[i];vertices.Add(new[]{p.x,p.y,c.r,c.g,c.b,c.a});}
   var corners=new Vector3[4];ui.img_linearDodges[0].rectTransform.GetWorldCorners(corners);
   views.Add(new{levels=FlowPlanning.Get(ui.CardModel)?.Levels??0,position=new[]{ui.transform.position.x,ui.transform.position.y},scale=ui.transform.localScale.x,frame=corners.Select(p=>new[]{p.x,p.y}).ToArray(),vertices,triangles=vh.triangles});
  }
  shots.Add(new{state,time,cards=views});
 }
}
