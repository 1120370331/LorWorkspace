using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using HarmonyLib;
using LOR_DiceSystem;
using Steria;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class FlowBorderUiChecks {
 static BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static string Out=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
 static List<string> Checks=new List<string>();
 public static Font FixtureFont;
 static TMP_FontAsset font;
 static float MeshRadius(FlowPrismGraphic prism){using(var vh=new VertexHelper()){typeof(FlowPrismGraphic).GetMethod("OnPopulateMesh",F,null,new[]{typeof(VertexHelper)},null).Invoke(prism,new object[]{vh});float radius=0;var vertex=new UIVertex();for(int i=0;i<vh.currentVertCount;i++){vh.PopulateUIVertex(ref vertex,i);radius=Mathf.Max(radius,((Vector2)vertex.position-prism.rectTransform.rect.center).magnitude);}return radius;}}
 static FlowCardPreviewStamp Stamp(BattleDiceCardUI ui){return ui.GetComponent<FlowCardPreviewStamp>();}
 static int applySounds,lockSounds,vibes;
 static void SoundObserved(EffectSoundType t){if(t==EffectSoundType.CARD_APPLY)applySounds++;if(t==EffectSoundType.CARD_LOCK)lockSounds++;}
 static void VibeObserved(){vibes++;}
 static BattleDiceCardBufUI FlowSlot(BattleDiceCardUI ui){return ui.bufIconListUI.FirstOrDefault(x=>x.gameObject.activeSelf&&x.txt_bufIconStack.text.StartsWith("×"));}
 class OtherVisibleBuf:BattleDiceCardBuf {protected override string keywordId=>"FlowFixtureOther";public OtherVisibleBuf(Sprite sprite){Set(this,"_bufIcon",sprite);Set(this,"_iconInit",true);}}

 static void Check(bool ok,string text){if(!ok)throw new Exception(text);Checks.Add("PASS "+text);}
 static void Set(object o,string field,object value){AccessTools.Field(o.GetType(),field).SetValue(o,value);}
 static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
 static TextMeshProUGUI Text(Transform parent,string value,Vector2 pos,Vector2 size,int points){var r=Rect("Text",parent,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.rectTransform.sizeDelta=size;t.rectTransform.anchoredPosition=pos;t.font=font;t.text=value;t.fontSize=points;t.color=new Color(.8f,.86f,.89f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
 static BattleDiceCardModel Card(int id){var xml=new DiceCardXmlInfo(new LorId("SteriaBuilding",id));xml.workshopName="洋流，听我的号令";for(int i=0;i<3;i++)xml.DiceBehaviourList.Add(new DiceBehaviour{Min=4+i,Dice=7+i,Type=BehaviourType.Atk,Detail=BehaviourDetail.Slash});var c=new BattleDiceCardModel();Set(c,"_xmlData",xml);return c;}
 public static void Run(){
  FlowPlanning.Reset();BattleObjectManager.instance.Init_only();
  Singleton<BattleEffectTextsXmlList>.Instance.Init(new Dictionary<string,LOR_XML.BattleEffectText>{
   {"CardBuf_FlowFixtureOther",new LOR_XML.BattleEffectText{Desc="Ordinary buff tooltip"}},
   {"CardBuf_SteriaManualFlow",new LOR_XML.BattleEffectText{Desc="Flow enhancement {0}"}}});
  font=TMP_FontAsset.CreateFontAsset(FixtureFont);
  var camera=new GameObject("Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=360;camera.transform.position=new Vector3(0,0,-100);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.06f);camera.farClipPlane=1000;
  var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;
  var canvas=Rect("Canvas",null,Vector2.zero,new Vector2(1280,720));var cv=canvas.gameObject.AddComponent<Canvas>();cv.renderMode=RenderMode.WorldSpace;cv.worldCamera=camera;canvas.gameObject.AddComponent<GraphicRaycaster>();
  Text(canvas,"洋流 · 流强化书页边框",new Vector2(0,280),new Vector2(1000,65),34);
  Text(canvas,"真实候选 DLL / 原版手牌组件与点击入口 · 简化场景布局",new Vector2(0,228),new Vector2(1000,35),18);
  var handRoot=Rect("Hand",canvas,new Vector2(0,-65),new Vector2(900,400));
  var hand=handRoot.gameObject.AddComponent<BattleUnitCardsInHandUI>();hand.enabled=false;Set(hand,"_rootObj",handRoot.gameObject);
  var list=new List<BattleDiceCardUI>();Set(hand,"_cardList",list);
  var manager=new GameObject("BattleManagerUI").AddComponent<BattleManagerUI>();manager.enabled=false;manager.ui_unitCardsInHand=hand;
  var u=new BattleUnitModel(1);u.breakDetail.breakLife=1;u.faction=Faction.Enemy;u.bufListDetail.AddBuf(new BattleUnitBuf_Flow{stack=16});u.faction=Faction.Player;
  // Real player buff bookkeeping reads these native records even in an otherwise minimal battle.
  var book=new BookModel();Set(book,"_classInfo",new BookXmlInfo());u.equipment.book=book;
  var unitData=(UnitDataModel)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnitDataModel));Set(unitData,"_bookItem",book);
  var battleData=(UnitBattleDataModel)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnitBattleDataModel));
  battleData.unitData=unitData;battleData.historyInStage=new UnitBattleStageHistory();battleData.historyInWave=new UnitBattleWaveHistory();battleData.emotionDetail=new BattleUnitEmotionDetail();Set(u,"_unitData",battleData);
  BattleObjectManager.instance.GetList().Add(u);Set(hand,"_selectedUnit",u);
  var passive=new PassiveAbility_9002001();passive.Init(u);u.passiveDetail.PassiveList.Add(passive);
  var stage=Singleton<StageController>.Instance;Set(stage,"_phase",StageController.StagePhase.ApplyLibrarianCardPhase);
  var sound=new GameObject("BattleSoundManager").AddComponent<BattleSoundManager>();sound.enabled=false;
  var cards=new List<BattleDiceCardModel>();
  for(int i=0;i<3;i++){
   var model=Card(9002002);cards.Add(model);
   var face=Rect("Card"+i,handRoot,new Vector2(-290+i*240,0),new Vector2(208,360));face.gameObject.SetActive(false);
   var ui=face.gameObject.AddComponent<BattleDiceCardUI>();ui.enabled=false;Set(ui,"_cardModel",model);
   ui.ui_behaviourDescList=new List<BattleDiceCard_BehaviourDescUI>();
   var image=face.gameObject.AddComponent<Image>();image.color=new Color(.11f,.16f,.18f,.96f);
   Text(face,"川流不息",new Vector2(0,120),new Vector2(195,40),20);
   var art=Rect("Artwork",face,new Vector2(0,52),new Vector2(184,90));var artImage=art.gameObject.AddComponent<Image>();artImage.color=new Color(.17f,.28f,.31f);artImage.raycastTarget=false;Set(ui,"img_artwork",artImage);
   if(i==2){art.sizeDelta*=2;art.localScale=Vector3.one*.5f;}
   Text(face,"立绘区域",new Vector2(0,64),new Vector2(175,30),15);
   for(int d=0;d<3;d++){var rowRect=Rect("Die"+d,face,new Vector2(0,-22-d*42),new Vector2(190,40));rowRect.gameObject.SetActive(false);var row=rowRect.gameObject.AddComponent<BattleDiceCard_BehaviourDescUI>();row.enabled=false;row.txt_range=Text(rowRect,"4-7",Vector2.zero,new Vector2(190,40),24);ui.ui_behaviourDescList.Add(row);rowRect.gameObject.SetActive(true);}
   var vibe=Rect("Vibe",face,Vector2.zero,new Vector2(208,360));Set(ui,"vibeRect",vibe);
   var frame=Rect("NativeLinearDodge",face,Vector2.zero,new Vector2(208,360));var frameImage=frame.gameObject.AddComponent<Image>();
   var frameTex=new Texture2D(16,16);for(int y=0;y<16;y++)for(int x=0;x<16;x++)frameTex.SetPixel(x,y,x<1||y<1||x>14||y>14?Color.white:Color.clear);frameTex.Apply();
   frameImage.sprite=Sprite.Create(frameTex,new UnityEngine.Rect(0,0,16,16),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(2,2,2,2));frameImage.type=Image.Type.Sliced;frameImage.fillCenter=false;frameImage.color=new Color(.2f,.35f,.4f,.6f);frameImage.raycastTarget=false;ui.img_linearDodges=new[]{frameImage};
   var slotRoot=Rect("NativeBufIcons",face,new Vector2(0,156),new Vector2(180,24));var slot=Rect("NativeSlot",slotRoot,new Vector2(-60,0),new Vector2(52,24));var bufUI=slot.gameObject.AddComponent<BattleDiceCardBufUI>();
   bufUI.img_bufIcon=Rect("Icon",slot,new Vector2(-4,0),new Vector2(20,20)).gameObject.AddComponent<Image>();bufUI.overlay=bufUI.img_bufIcon.gameObject.AddComponent<UI.UICardBufOverlay>();bufUI.txt_bufIconStack=Text(slot,"",new Vector2(15,0),new Vector2(32,24),16);ui.bufIconListUI=new[]{bufUI};
   face.gameObject.SetActive(true);list.Add(ui);FlowCardPreview.Apply(ui);
  }
  Set(u.allyCardDetail,"_cardInHand",cards);
  var iconPath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../../SteriaBuild/SteriaModFolder/Resource/ArtWork/SteriaFlow.png"));
  var iconTex=new Texture2D(2,2);iconTex.LoadImage(File.ReadAllBytes(iconPath));BattleUnitBuf._bufIconDictionary["SteriaFlow"]=Sprite.Create(iconTex,new UnityEngine.Rect(0,0,iconTex.width,iconTex.height),new Vector2(.5f,.5f));
  FlowHandButtonPatch.Postfix(hand);var controller=hand.GetComponent<FlowHandController>();
  Check(handRoot.Find("SteriaFlowButton").gameObject.activeSelf&&!controller.Armed,"binding shows flow button synchronously before any Update or click");
  controller.enabled=false;FlowHandButtonPatch.Postfix(hand);
  Check(handRoot.Find("SteriaFlowButton").gameObject.activeSelf,"rebinding immediately refreshes an existing controller");
  foreach(var text in handRoot.GetComponentsInChildren<TextMeshProUGUI>(true))text.font=font;
  var update=typeof(FlowHandController).GetMethod("Update",F);update.Invoke(controller,null);
  Check(handRoot.Find("SteriaFlowButton").gameObject.activeSelf,"Flow button is visible in planning with active Flow");
  var harmony=new Harmony("steria.flow.fixture");
  foreach(var t in typeof(FlowHandController).Assembly.GetTypes().Where(t=>t.Name.StartsWith("Flow")&&t.GetCustomAttributes(typeof(HarmonyPatch),false).Length>0))harmony.CreateClassProcessor(t).Patch();
  harmony.Patch(AccessTools.Method(typeof(BattleSoundManager),"PlaySound",new[]{typeof(EffectSoundType),typeof(bool)}),postfix:new HarmonyMethod(typeof(FlowBorderUiChecks),"SoundObserved"));
  harmony.Patch(AccessTools.Method(typeof(BattleDiceCardUI),"PlayVibeCard"),prefix:new HarmonyMethod(typeof(FlowBorderUiChecks),"VibeObserved"));
  // Fill the only original icon slot: Flow must extend native capacity without replacing this independent buff.
  cards[1].AddBuf(new OtherVisibleBuf(BattleUnitBuf._bufIconDictionary["SteriaFlow"]));
  var eventSystem=new GameObject("EventSystem").AddComponent<EventSystem>();var click=new PointerEventData(eventSystem){button=PointerEventData.InputButton.Left};
  handRoot.Find("SteriaFlowButton").GetComponent<Button>().onClick.Invoke();
  Check(controller.Armed,"button click arms hand selection");
  var prism=handRoot.Find("SteriaFlowButton").GetComponent<FlowPrismGraphic>();
  Check(prism.Armed&&MeshRadius(prism)>34&&prism.rectTransform.rect.width==62,"armed rim includes five-pixel exterior halo while hit rect remains 62px");
  update.Invoke(controller,null);Save(camera,rt,"01-unenhanced.png");

  Check(list.All(x=>Stamp(x)==null||Stamp(x).CurrentBorder==null),"unstrengthened cards have no border objects");
  list[1].OnPdSubmit(click);
  var center=list[1];var stamp=Stamp(center);var border=stamp.CurrentBorder;
  Check(FlowPlanning.Get(cards[1]).Levels==1&&border!=null&&border.enabled,"real OnPdSubmit enhancement creates active border");
  Check(list[0].GetComponentsInChildren<FlowCardCurrentBorder>(true).Length==0&&list[2].GetComponentsInChildren<FlowCardCurrentBorder>(true).Length==0,"same-name same-ID card instances stay undecorated");
  Check(FlowSlot(center).txt_bufIconStack.text=="×1"&&applySounds==1,"native Flow count and CARD_APPLY preserved");
  Check(stamp.FlashImages.Count==1&&stamp.FlashImages[0].gameObject.activeSelf,"native success flash still present");
  typeof(FlowCardPreviewStamp).GetMethod("UpdateFlash",F).Invoke(stamp,new object[]{stamp.FlashStart+.31f});
  Check(!stamp.FlashImages[0].gameObject.activeSelf&&border.enabled,"persistent current remains after success flash expires");
  Check(!border.raycastTarget&&FlowSlot(center).GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget),"decoration never intercepts page input");
  Directory.CreateDirectory(Path.Combine(Out,"frames"));
  for(int frame=0;frame<144;frame++){
   border.Tick(frame/30f);Save(camera,rt,"frames/current-"+frame.ToString("D3")+".png");
   if(frame==0||frame==9||frame==18||frame==27)Save(camera,rt,"02-enhanced-t"+(frame/30f).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png");
  }
  int descendants=center.GetComponentsInChildren<Transform>(true).Length;
  for(int repeat=0;repeat<20;repeat++)FlowCardPreview.Apply(center);
  Check(center.GetComponentsInChildren<FlowCardCurrentBorder>(true).Length==1&&center.GetComponentsInChildren<Transform>(true).Length==descendants,"twenty repeated Apply calls reuse one mesh and stable child objects");
  list[1].OnPdSubmit(click);border.Tick(.6f);Save(camera,rt,"03-enhanced-twice.png");
  Check(FlowPlanning.Get(cards[1]).Levels==2&&FlowSlot(center).txt_bufIconStack.text=="×2"&&stamp.CurrentBorder==border,"second enhancement preserves the mesh and native count");
  controller.HandleCardClick(center,new PointerEventData(eventSystem){button=PointerEventData.InputButton.Right});
  border.Tick(.6f);Check(!controller.Armed&&border.enabled,"canceling selection leaves actual enhanced-page current intact");
  FlowPlanning.Commit();FlowPlanning.BeginRound();FlowCardPreview.Apply(center);border.Tick(.6f);
  Check(FlowPlanning.Get(cards[1]).Levels==2&&border.enabled,"unused enhancement current survives scene boundary");
  center.gameObject.SetActive(false);Check(!border.enabled,"disabling pooled UI hides current immediately");
  center.gameObject.SetActive(true);Check(border.enabled,"reenabling same enhanced model resumes true enhancement state");
  var native=center.img_linearDodges[0].rectTransform;
  ((RectTransform)center.transform).sizeDelta=Vector2.zero;
  native.sizeDelta=new Vector2(260,300);native.localScale=new Vector3(.8f,1.2f,1);
  center.transform.localScale=Vector3.one*.72f;border.Tick(.6f);
  Check(Mathf.Abs(border.rectTransform.rect.width-208)<.01f&&Mathf.Abs(border.rectTransform.rect.height-360)<.01f,"zero-sized card root and independently scaled native frame use native frame geometry");
  Save(camera,rt,"04-scaled-native-frame.png");
  center.transform.localScale=Vector3.one;native.sizeDelta=new Vector2(208,360);native.localScale=Vector3.one;
  var rightFrame=Rect("NativeRightHalf",center.transform,new Vector2(52,0),new Vector2(104,360)).gameObject.AddComponent<Image>();rightFrame.raycastTarget=false;rightFrame.color=Color.clear;
  native.sizeDelta=new Vector2(104,360);native.anchoredPosition=new Vector2(-52,0);
  center.img_linearDodges=new[]{center.img_linearDodges[0],rightFrame};border.Tick(.6f);
  Check(Mathf.Abs(border.rectTransform.rect.width-208)<.01f&&border.rectTransform.localPosition.sqrMagnitude<.01f,"multiple native frame parts produce their actual geometric union");
  native.sizeDelta=new Vector2(208,360);native.anchoredPosition=Vector2.zero;center.img_linearDodges=new[]{center.img_linearDodges[0]};
  rightFrame.gameObject.SetActive(false);((RectTransform)center.transform).sizeDelta=new Vector2(208,360);
  FlowCardPreview.Restore(center);Set(center,"_cardModel",cards[0]);FlowCardPreview.Apply(center);
  Check(!border.enabled&&FlowSlot(center)==null,"pooled UI rebound to same-ID unenhanced model clears border and native marker");
  Save(camera,rt,"05-reused-unenhanced.png");
  Set(center,"_cardModel",cards[1]);FlowCardPreview.Apply(center);border.Tick(.6f);
  Check(border.enabled&&center.GetComponentsInChildren<FlowCardCurrentBorder>(true).Length==1,"rebinding original enhanced instance restores one border");
  FlowPlanning.Commit();HarmonyHelpers.RegisterCardUsage(new BattlePlayingCardDataInUnitModel{owner=u,card=cards[1]});
  border.Tick(.9f);
  Check(FlowPlanning.Get(cards[1])==null&&!border.enabled,"consumed page disappears on next visual tick even without Apply");
  FlowCardPreview.Apply(center);Save(camera,rt,"06-consumed.png");
  FlowPlanning.Reset();border.Tick(1f);Check(!border.enabled,"wave reset leaves no current");
  File.WriteAllLines(Path.Combine(Out,"ui-checks.txt"),Checks);
  harmony.UnpatchSelf();
 }
 static void Save(Camera camera,RenderTexture rt,string file){foreach(var text in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>())text.font=font;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGBA32,false);tex.ReadPixels(new UnityEngine.Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Out,file),tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=null;}
}
