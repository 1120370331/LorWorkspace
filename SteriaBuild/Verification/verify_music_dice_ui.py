from pathlib import Path
import re
root=Path(__file__).resolve().parents[2]
s=(root/'SteriaBuild/MusicDiceSystem.cs').read_text(encoding='utf-8-sig')
h=(root/'SteriaBuild/HarmonyPatches.cs').read_text(encoding='utf-8-sig')
visual=s[s.index('internal static class MusicDiceSpriteFactory'):]
for forbidden in ['music_dice_die_icon','music_dice_face','music_dice_glow','img_linearDodge','img_linearDodges','txt_ability','SetValueColor(','.enabled =','localScale =','anchoredPosition =','RefineHsv']:
 assert forbidden not in visual,forbidden
assert s.count('IList<DiceBehaviour> list = cardModel?.GetBehaviourList();')==2
assert 'behaviour.Type == BehaviourType.Atk' in visual
for arg in ['typeof(List<BattleCardBehaviourResult>)','typeof(BattleDiceBehaviourUI)','typeof(BattleDiceBehavior)']:
 assert '"PrepareDice", new Type[] { '+arg+' }' in h,arg
for cls in ['BattleDiceCardUI_SetCard_MusicStyle_Patch','BattleDiceCard_BehaviourDescUI_SetBehaviourInfo_MusicStyle_Patch','UIOriginCardSlot_SetData_MusicStyle_Patch','UIDetailCardDescSlot_SetBehaviourInfo_MusicStyle_Patch']:
 body=h[h.index('class '+cls):];body=body[:body.index('[HarmonyPostfix]')]
 assert '[HarmonyPrefix]' in body and 'Restore' in body,cls
print('PASS: original music icons; per-die UI predicate; runtime alignment; bind prefixes; no frame/text/state resets')

assert 'Orchestra_' not in visual and 'Resources.Load' not in visual and 'Font.' not in visual
assert 'GetGlyphSprite' in visual and 'GetBlankFrameSprite' in visual
factory=visual[:visual.index('public static class MusicDiceVisuals')]
for old_source in ['UISpriteDataManager', 'Graphics.Blit', 'ReadPixels', 'Resources.Load', 'ClearBakedAttackCenter', 'CompositeGlyph', 'GetPixels', 'source.texture']:
 assert old_source not in factory,old_source
for target in ['diceUi.imgIcon','diceUi.imgDetailIcon_Center','effect.img_resistIcon','effect.img_resistIconBg','effect.img_resistIconFg']:
 assert target+', true)' in visual,target
print('PASS: original code-drawn glyph; independent clean vector frame; all action/damage centers share glyph; no font/resource center')

assert 'GetManifestResourceStream' in factory and 'ImageConversion.LoadImage' in factory
for costly in ['Rasterize(', 'GlyphDistance(', 'CurveDistance(', 'SmoothUnion(']:
 assert costly not in factory,costly
project=(root/'SteriaBuild/Steria.csproj').read_text(encoding='utf-8-sig')
for name in ['Card','Glyph','BlankFrame']:
 assert 'LogicalName="Steria.VisualAssets.MusicDice.'+name+'.png"' in project
 assert (root/'SteriaBuild/VisualAssets/MusicDice'/ (name+'.png')).is_file()
print('PASS: baked owned artwork embedded explicitly; actual manifest loader; no runtime vector rasterization')
