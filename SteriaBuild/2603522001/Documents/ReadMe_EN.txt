If there is a png file which name is equal to the keywordIconId of BattleUnitBuf or BattleDiceCardBuf in the ArtWork folder, the icon of the Buff will be automatically generated. For BattleDiceCardBuf, the file name is "CardBuf_"+keywordIconId.png. For BattleUnitBuf, the file name is keywordIconId.png.
The BaseMod tool can help authors transfer old mods to the workshop with minimal effort, and provide common functions for new authors.Such as original Xml unpacking, custom battle backgrounds and MapManager, mod custom savedata storage and load, all special actions of character, custom gift, custom emotionalcards,custom bookicon and convenient LorId generation, etc. We decide to write a document later.
Notes for old mod converts:
The author needs to copy all the files in the original mod (except for Story, use PM's editior is more convenient) into the root directory of the new mod, and modify the Xml (for example, where use the original passiveability) and the part about the id in the Dll (which converts the original digital id to LorId). Finally, use the upload tool in the BaseMod directory upload your mod.
If you need to add multi-language story texts, please create a new language folder in the \NormalInvitation\Data\StoryText directory and store them, such as \NormalInvitation\Data\StoryText\en\Story1, When writing the story file, the XmlNode <Story Condition="Start">Story1</Story> or <Story Condition="Start">Story1.xml</Story>is all ok.
All items in Xml are recognized as the content of the current Mod by default, and the unmarked sub-items will be uniformly recognized as the content of the current Mod. If you need to add the original content, please refer to the following format: <Book Pid="@origin">31< /Book>,you can also use this method to make your mod drop books from other mods or use books from other mods to invite .
Which need to add Pid is:
CardDropTable: Card
Deck: Card
DropBook: DropItem
EquipPage: Episode, EquipEffect_Passive, EquipEffect_OnlyCard
Stage: Condition_Stage, Wave_Unit, Invitation_Book
Xml unpacking: located in \Library Of Ruina\LibraryOfRuina_Data\Managed\BaseMod directory.
Now if the mod's id ends with "@origin", then the content in this mod (such as battle pages, core pages, etc.) will be recognized as the original content, and if the ids is same as some item in origginal content, the original content will be overwritten. This can also be used for quick migration of old mods (so that the id-related parts in the xml and dll don't need to be modified, but obsolete method references still need to be modified)
Also fixed a little bug about ReadyBuf, and opened the unit capacity limit in most cases (up to 99 per side, 8 each in the sidebar UI).
If you need to upload and update Mod, please use the Mod upload tool, which is in the root directory of BaseMod.