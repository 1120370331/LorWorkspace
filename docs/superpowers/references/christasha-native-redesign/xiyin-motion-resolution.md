# 汐音 9009006 的实际动作图解析（只读）

2026-09-13，供离屏人物上下文选择，未启动游戏或修改书页。

当前 XML `Motion="S"` 经 `MotionConverter.MotionToAction` 映到 `ActionDetail.Special`（枚举12），不是 `S1`（枚举13）。`BattleFarAreaPlayManager` 在调用 `SetFarAreaAtkEffect` 前执行这个 `ChangeMotion`；现有汐音 Action/FarArea 类没有再次切换人物动作。

Christasha 的 `ModInfo.Xml`/`ClothCustom` 没有 `Special` 节点/图片，只有 `S1`。在当前原版与已安装 ExtendedLoader 读取规则下，这颗骰子的预期图为 **Default**：

1. 原版 `WorkshopAppearanceItemLoader.LoadCustomAppearanceInfo` 只遍历枚举0到11，不会替不存在的12 Special生成Penetrate回退。
2. 已安装 `1ExtendedLoader.dll` 的 `EarlyPatches.WorkshopAppearanceItemLoader_LoadCustomAppearanceInfo_Postfix` 逐动作读取XML节点；IL `0313..0322` 中节点不存在即跳到下一动作，因此不会把S1隐式填给Special。
3. `WorkshopSetterPatch.WorkshopSkinDataSetter_SetData_Prefix` 把未出现在skin数据中的动作从 `_motionList` / `_characterMotionDic` 移除（IL `00cf..00f0` 检查、`018f..01af` 移除）。
4. 原版 `CharacterAppearance.ChangeMotion` 对缺失的Special按最终回退设为Default；只有Slash2/Penetrate2/Hit2另有对应普通动作回退。

只读IL证据：`output/christasha-native-redesign/skin-special-loader-il.txt`。解析使用仓库 Mono.Cecil 读取以下文件的元数据，未执行目标程序：

`D:/Game Center/Steam/steamapps/workshop/content/1256670/2603522001/Assemblies/1FrameworkAssemblies/1ExtendedLoader.dll`

本次Mass人物合成应使用实际Default图及其pivot，不能因为存在S1.png就把它标作9009006的当前游戏动作。去除S1烘焙光效仍对其它现有S1书页有用。若后续明确调整汐音施法姿态，需要作为单独的视觉编排变更记录和检查；此解析不授予现成动作运行时实战PASS，也不覆盖其它能力主动换肤/altMotion的特殊状态。
