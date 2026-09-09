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
public static class FlowUiChecks {
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
  Text(canvas,"流 · 手动强化",new Vector2(0,280),new Vector2(1000,65),34);
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
   var model=Card(i==1?9002006:9002002);cards.Add(model);
   var face=Rect("Card"+i,handRoot,new Vector2(-290+i*240,0),new Vector2(208,360));face.gameObject.SetActive(false);
   var ui=face.gameObject.AddComponent<BattleDiceCardUI>();ui.enabled=false;Set(ui,"_cardModel",model);
   ui.ui_behaviourDescList=new List<BattleDiceCard_BehaviourDescUI>();
   var image=face.gameObject.AddComponent<Image>();image.color=new Color(.11f,.16f,.18f,.96f);
   Text(face,i==1?"洋流，听我的号令":"川流不息",new Vector2(0,120),new Vector2(195,40),20);
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
  var iconPath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../SteriaBuild/SteriaModFolder/Resource/ArtWork/SteriaFlow.png"));
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
  harmony.Patch(AccessTools.Method(typeof(BattleSoundManager),"PlaySound",new[]{typeof(EffectSoundType),typeof(bool)}),postfix:new HarmonyMethod(typeof(FlowUiChecks),"SoundObserved"));
  harmony.Patch(AccessTools.Method(typeof(BattleDiceCardUI),"PlayVibeCard"),prefix:new HarmonyMethod(typeof(FlowUiChecks),"VibeObserved"));
  // Fill the only original icon slot: Flow must extend native capacity without replacing this independent buff.
  cards[1].AddBuf(new OtherVisibleBuf(BattleUnitBuf._bufIconDictionary["SteriaFlow"]));
  var eventSystem=new GameObject("EventSystem").AddComponent<EventSystem>();var click=new PointerEventData(eventSystem){button=PointerEventData.InputButton.Left};
  handRoot.Find("SteriaFlowButton").GetComponent<Button>().onClick.Invoke();
  Check(controller.Armed,"button click arms hand selection");
  var prism=handRoot.Find("SteriaFlowButton").GetComponent<FlowPrismGraphic>();
  Check(prism.Armed&&MeshRadius(prism)>34&&prism.rectTransform.rect.width==62,"armed rim includes five-pixel exterior halo while hit rect remains 62px");
  update.Invoke(controller,null);Save(camera,rt,"01-select.png");
  list[1].OnPdSubmit(click);
  Check(controller.Armed&&FlowPlanning.Get(cards[1]).Levels==1&&FlowPlanning.Available(u)==13,"successful card submit enhances once and preserves armed mode");
  Check(hand.GetSelectedCard()==null,"enhancement click never selects page for combat");
  Check(list[1].ui_behaviourDescList[0].txt_range.text=="5-8\n(+1)"&&list[1].ui_behaviourDescList[0].txt_range.fontSize==24,"preview uses original font size on two separate lines");
  Check(cards[1].GetBehaviourList()[0].Min==4,"shared XML remains unchanged");
  Check(FlowSlot(list[1])!=null&&FlowSlot(list[1]).txt_bufIconStack.text=="×1"&&FlowSlot(list[1]).img_bufIcon.sprite!=null,"original SetBufIcon displays Flow icon and first count");
  Check(list[1].bufIconListUI.Length==2&&list[1].bufIconListUI[0].gameObject.activeSelf,"full native icon area expands without displacing existing buff");
  Check(applySounds==1&&lockSounds==0,"success plays one native CARD_APPLY sound only");
  Check(prism.Armed&&Stamp(list[1]).FlashImages.Count==1&&Stamp(list[1]).FlashImages[0].gameObject.activeSelf,"successful enhancement retains armed rim and starts reused card frame flash");
  var stampUpdate=typeof(FlowCardPreviewStamp).GetMethod("UpdateFlash",F);stampUpdate.Invoke(Stamp(list[1]),new object[]{Stamp(list[1]).FlashStart+.05f});
  Save(camera,rt,"02-enhanced-once.png");
  list[1].OnPdSubmit(click);
  Check(controller.Armed&&FlowPlanning.Get(cards[1]).Levels==2&&FlowPlanning.Available(u)==10,"second card click without button gives exactly second enhancement");
  Check(FlowSlot(list[1]).txt_bufIconStack.text=="×2"&&list[1].bufIconListUI.Length==2,"second enhancement updates original native counter without duplicating slots");
  Check(FlowSlot(list[1]).GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget)&&Stamp(list[1]).FlashImages.All(g=>!g.raycastTarget),"Flow native decoration and flash do not intercept page input");
  Check(applySounds==2&&Stamp(list[1]).FlashImages.Count==1,"repeat success plays once and reuses flash geometry");
  stampUpdate.Invoke(Stamp(list[1]),new object[]{Stamp(list[1]).FlashStart+.05f});
  update.Invoke(controller,null);Save(camera,rt,"02-enhanced.png");
  stampUpdate.Invoke(Stamp(list[1]),new object[]{Stamp(list[1]).FlashStart+.31f});
  Check(!Stamp(list[1]).FlashImages[0].gameObject.activeSelf,"success border finishes after 0.3 seconds");
  controller.HandleCardClick(list[1],new PointerEventData(eventSystem){button=PointerEventData.InputButton.Right});
  Check(!controller.Armed&&FlowPlanning.Available(u)==10&&!prism.Armed,"right click cancels selection and rim without spending");
  controller.Toggle();list[2].OnPdSubmit(click);list[2].OnPdSubmit(click);
  Check(controller.Armed&&FlowPlanning.Get(cards[2]).Levels==2,"continuous selection also strengthens a second page");
  list[2].OnPdSubmit(click);
  Check(!controller.Armed&&FlowPlanning.Get(cards[2]).Levels==2&&FlowPlanning.Available(u)==4,"failed enhancement exits mode without changing successful levels");
  Check(vibes==1&&lockSounds==1&&applySounds==4,"failure invokes one original vibration and one CARD_LOCK sound");
  list[2].StopAllCoroutines();Set(list[2],"isRunningVibeCard",false);
  Check(FlowSlot(list[0])==null,"same-name unenhanced card has no native Flow marker");
  var reusedSlot=list[2].bufIconListUI[0];reusedSlot.SetBufIcon(new OtherVisibleBuf(BattleUnitBuf._bufIconDictionary["SteriaFlow"]));
  Check(reusedSlot.overlay.enabled&&reusedSlot.overlay.GetDescription()=="Ordinary buff tooltip"&&reusedSlot.img_bufIcon.raycastTarget&&!reusedSlot.txt_bufIconStack.raycastTarget,"native Flow slot reused for ordinary buff restores tooltip and original raycast states");
  FlowCardPreview.Apply(list[2]);
  Save(camera,rt,"03-native-icons.png");
  FlowCardPreview.Restore(list[1]);Set(list[1],"_cardModel",cards[0]);FlowCardPreview.Apply(list[1]);
  Check(FlowSlot(list[1])==null&&list[1].ui_behaviourDescList[0].txt_range.text=="4-7"&&list[1].ui_behaviourDescList[0].txt_range.rectTransform.sizeDelta.y==40,"pooled UI restores original text geometry and removes previous native marker");
  Set(list[1],"_cardModel",cards[1]);FlowCardPreview.Apply(list[1]);
  FlowPlanning.Commit();FlowPlanning.BeginRound();FlowCardPreview.Apply(list[1]);FlowCardPreview.Apply(list[2]);
  Check(FlowSlot(list[1])!=null&&FlowSlot(list[2])!=null&&FlowPlanning.Get(cards[1]).Levels==2,"unused enhancements and native markers survive next round");
  FlowPlanning.Commit();var action=new BattlePlayingCardDataInUnitModel{owner=u,card=cards[1]};HarmonyHelpers.RegisterCardUsage(action);FlowCardPreview.Apply(list[1]);
  Check(FlowPlanning.Get(cards[1])==null&&FlowSlot(list[1])==null&&FlowSlot(list[2])!=null,"actual card usage removes only its consumed enhancement marker");
  Save(camera,rt,"04-used.png");
  FlowPlanning.Reset();FlowCardPreview.Apply(list[2]);
  Check(FlowSlot(list[2])==null,"wave reset clears remaining enhancement marker");
  controller.Toggle();Set(hand,"_selectedUnit",null);update.Invoke(controller,null);
  Check(!controller.Armed&&!prism.Armed&&!handRoot.Find("SteriaFlowButton").gameObject.activeSelf,"switching off character hides button and clears selection and glow");
  Set(hand,"_selectedUnit",u);Set(stage,"_phase",StageController.StagePhase.SetCurrentDiceAction);update.Invoke(controller,null);
  var flowButton=handRoot.Find("SteriaFlowButton").GetComponent<Button>();
  Check(flowButton.gameObject.activeSelf&&!flowButton.interactable,"unit with flow remains visible outside planning but cannot edit combat allocations");
  controller.Toggle();Check(!controller.Armed,"visible combat button cannot arm enhancement");
  Set(hand,"_selectedUnit",null);Set(hand,"_hOveredUnit",u);FlowHandButtonPatch.Postfix(hand);
  Check(flowButton.gameObject.activeSelf&&!flowButton.interactable,"hovered hand with Flow shows the button without selecting a unit");
  Set(hand,"_hOveredUnit",null);Set(hand,"_selectedUnit",u);
  var flow=u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_Flow>().First(b=>!b.IsDestroyed());flow.stack=0;FlowHandButtonPatch.Postfix(hand);
  Check(!flowButton.gameObject.activeSelf,"zero Flow hides button synchronously");
  typeof(CardAbilityHelper).GetMethod("ApplyFlowStacksNow",F).Invoke(null,new object[]{u,1});
  Check(flowButton.gameObject.activeSelf&&flowButton.transform.Find("AvailableFlow").GetComponent<TextMeshProUGUI>().text=="流 1","settled Flow gain refreshes visible hand without Update or click");
  flow.Destroy();u.faction=Faction.Enemy;FlowHandButtonPatch.Postfix(hand);
  typeof(CardAbilityHelper).GetMethod("ApplyFlowStacksNow",F).Invoke(null,new object[]{u,2});
  Check(flowButton.gameObject.activeSelf&&!flowButton.interactable,"new active Flow on enemy also immediately shows a noninteractive button");
  u.breakDetail.breakLife=0;FlowHandButtonPatch.Postfix(hand);
  Check(flowButton.gameObject.activeSelf,"stagger does not hide existing Flow indicator");
  var bootRoot=Rect("BootHand",canvas,Vector2.zero,new Vector2(900,400));var bootHand=bootRoot.gameObject.AddComponent<BattleUnitCardsInHandUI>();bootHand.enabled=false;
  Set(bootHand,"_rootObj",bootRoot.gameObject);Set(bootHand,"_cardList",new List<BattleDiceCardUI>());Set(bootHand,"_selectedUnit",u);Set(bootHand,"_initialized",true);
  typeof(BattleUnitCardsInHandUI).GetMethod("Initialize",F).Invoke(bootHand,null);
  Check(bootHand.GetComponent<FlowHandController>()!=null&&bootRoot.Find("SteriaFlowButton").gameObject.activeSelf,"hand initialization creates visible button without SetCardsObject");
  u.breakDetail.breakLife=1;
  foreach(var active in u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_Flow>())active.Destroy();
  Set(hand,"_selectedUnit",u);Set(hand,"_hOveredUnit",null);Set(stage,"_phase",StageController.StagePhase.ApplyLibrarianCardPhase);FlowHandButtonPatch.Postfix(hand);
  Check(!flowButton.gameObject.activeSelf,"no active or pending Flow hides indicator");
  CardAbilityHelper.AddFlowStacks(u,5,false);
  Check(flowButton.gameObject.activeSelf&&!flowButton.interactable&&FlowPlanning.Available(u)==0,"ordinary deferred gain immediately shows pending indicator without granting current Flow");
  Check(flowButton.transform.Find("AvailableFlow").GetComponent<TextMeshProUGUI>().text=="可用 0\n下幕 +5","pending indicator explicitly separates available zero from next-round five");
  Save(camera,rt,"05-pending-flow.png");
  FlowGainTiming.BeginRound();FlowGainTiming.EndRoundStart();controller.RefreshNow();
  Check(FlowPlanning.Available(u)==5&&flowButton.transform.Find("AvailableFlow").GetComponent<TextMeshProUGUI>().text=="流 5","pending amount becomes available exactly at next round start");
  foreach(var active in u.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_Flow>())active.Destroy();
  u.faction=Faction.Player;FlowPlanning.BeginRound();controller.RefreshNow();
  CardAbilityHelper.AddFlowStacksThisRound(u,3,false);
  Check(FlowPlanning.Available(u)==3&&flowButton.gameObject.activeSelf&&flowButton.interactable&&flowButton.transform.Find("AvailableFlow").GetComponent<TextMeshProUGUI>().text=="流 3","Anhier immediate-flow API refreshes currently usable planning button");
  controller.Toggle();list[0].OnPdSubmit(click);
  Check(FlowPlanning.Get(cards[0]).Levels==1&&FlowPlanning.Available(u)==0&&!controller.Armed,"exact-balance enhancement succeeds and exits continuous mode at zero");
  // Reproduce the real pooled two-die panel: an inactive row retains a position very near a live row.
  var spacingUI=list[0];FlowCardPreview.Restore(spacingUI);
  var rangeRows=spacingUI.ui_behaviourDescList;
  rangeRows[2].gameObject.SetActive(false);
  rangeRows[2].transform.localPosition=rangeRows[0].transform.localPosition+Vector3.down*2;
  rangeRows[1].transform.localPosition=rangeRows[0].transform.localPosition+Vector3.down*18;
  var secondOriginal=rangeRows[1].transform.localPosition;var hiddenOriginal=rangeRows[2].transform.localPosition;
  var originalAlignment=rangeRows[0].txt_range.alignment;var originalTextPosition=rangeRows[0].txt_range.rectTransform.anchoredPosition;
  FlowCardPreview.Apply(spacingUI);Canvas.ForceUpdateCanvases();
  var firstBounds=RangeBounds(rangeRows[0].txt_range,spacingUI.transform);var secondBounds=RangeBounds(rangeRows[1].txt_range,spacingUI.transform);
  Check(rangeRows[0].txt_range.lineSpacing>=0&&rangeRows[1].txt_range.lineSpacing>=0,"hidden nearby dice never cause negative line spacing");
  Check(rangeRows[1].txt_range.textInfo.lineInfo[0].baseline>rangeRows[1].txt_range.textInfo.lineInfo[1].baseline,"bonus line remains below its own range in tight pooled layout");
  Check(firstBounds.x-secondBounds.y>1,"visible two-line dice blocks maintain real glyph separation");
  Check(rangeRows[2].transform.localPosition==hiddenOriginal,"inactive prefab row is excluded from reflow");
  var spacedPosition=rangeRows[1].transform.localPosition;
  for(int iteration=0;iteration<3;iteration++){FlowCardPreview.Apply(spacingUI);Canvas.ForceUpdateCanvases();}
  Check(Vector3.Distance(spacedPosition,rangeRows[1].transform.localPosition)<.01f,"repeated binding and render updates do not accumulate vertical drift");
  spacingUI.transform.localScale=Vector3.one*.5f;Canvas.ForceUpdateCanvases();
  firstBounds=RangeBounds(rangeRows[0].txt_range,spacingUI.transform);secondBounds=RangeBounds(rangeRows[1].txt_range,spacingUI.transform);
  Check(firstBounds.x-secondBounds.y>1&&rangeRows[0].txt_range.fontSize==24,"scaled card keeps separated blocks at unchanged font size");
  spacingUI.transform.localScale=Vector3.one;Save(camera,rt,"06-spacing-regression.png");
  FlowCardPreview.Restore(spacingUI);
  Check(rangeRows[1].transform.localPosition==secondOriginal&&rangeRows[0].txt_range.alignment==originalAlignment&&rangeRows[0].txt_range.rectTransform.anchoredPosition==originalTextPosition,"reuse restores native row positions alignment and text anchors");
  File.WriteAllLines(Path.Combine(Out,"ui-checks.txt"),Checks);
  harmony.UnpatchSelf();
 }
 static Vector2 RangeBounds(TextMeshProUGUI text,Transform root){text.ForceMeshUpdate(true);var bounds=text.textBounds;float a=root.InverseTransformPoint(text.transform.TransformPoint(bounds.min)).y;float b=root.InverseTransformPoint(text.transform.TransformPoint(bounds.max)).y;return new Vector2(Mathf.Min(a,b),Mathf.Max(a,b));}
 static void Save(Camera camera,RenderTexture rt,string file){foreach(var text in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>())text.font=font;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGBA32,false);tex.ReadPixels(new UnityEngine.Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Out,file),tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=null;}
}

