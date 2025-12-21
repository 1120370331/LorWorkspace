# Steria Mod - Library of Ruina

## 项目概述
这是一个 Library of Ruina 的 mod 项目，使用 BaseMod 框架开发。

## 重要参考资源

### Mod 开发教程
```
c:\Users\rog\WorkSpace\projects\games\lor\ModGuideDocs
```
遇到不清楚的开发问题时，先查阅此目录下的教程文档。

### 游戏源码（反编译）
```
c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\Assembly-CSharp
```
**重要**: 遇到不确定的 API 用法、类结构、方法签名时，一定要查看游戏源码！
- 查找类的继承关系
- 确认方法参数和返回值
- 了解游戏内部逻辑流程
- 查看原版被动/卡牌的实现方式

## 构建与部署

### 编译 DLL
```bash
cd c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild
dotnet build Steria.csproj -c Release
```

### 部署到 Mod 文件夹
编译完成后，将 DLL 复制到 mod 文件夹：
```bash
cp "c:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/bin/Release/net472/Steria.dll" "c:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/SteriaModFolder/Assemblies/Steria.dll"
```

### 一键构建并部署
```bash
cd c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild && dotnet build Steria.csproj -c Release && cp "bin/Release/net472/Steria.dll" "SteriaModFolder/Assemblies/Steria.dll"
```

## 项目结构

### 核心文件
- `Steria.csproj` - 项目文件
- `ModInitializer.cs` - Mod 初始化入口
- `HarmonyPatches.cs` - Harmony 补丁（流消耗、弃牌检测等）
- `AnhierAbilities.cs` - 被动能力实现 (PassiveAbility_9000001-9000005)
- `AnhierCards.cs` - 卡牌能力实现 (DiceCardAbility_*, DiceCardSelfAbility_*)
- `BattleUnitBuf_Flow.cs` - 流 Buff 实现
- `BattleUnitBuf_PreciousMemoryStrength.cs` - 珍贵的回忆弃置效果 Buff
- `SteriaLogger.cs` - 日志工具类
- `CardAbilityHelper.cs` - 卡牌能力辅助方法

### Mod 资源文件夹
`SteriaModFolder/` - 最终部署的 mod 文件夹
- `Assemblies/Steria.dll` - 编译后的 DLL
- `Localize/` - 本地化文件
- `Resource/` - 资源文件（图标等）
- `StaticInfo/` - XML 配置文件（卡牌、被动等定义）

## 命名空间规则

**重要**: 游戏需要在全局命名空间中查找以下类型的类：
- `PassiveAbility_XXXXXXX` - 被动能力类
- `DiceCardAbility_*` - 骰子能力类
- `DiceCardSelfAbility_*` - 卡牌使用时能力类
- `BattleUnitBuf_*` - Buff 类（部分需要在全局命名空间）

工具类和辅助类可以放在 `Steria` 命名空间中：
- `SteriaLogger`
- `CardAbilityHelper`
- `HarmonyHelpers`

## 流机制说明

### 流消耗逻辑
- 使用书页时，消耗所有流层数
- 流层数循环分配给骰子，每1层流给1颗骰子+1威力
- 特殊卡牌（如清司风流 ID:9001008）只获得加成而不消耗流

### 添加新的"只获得流加成而不消耗流"的卡牌
在 `HarmonyPatches.cs` 的 `_flowBonusOnlyCardIds` 集合中添加卡牌ID：
```csharp
private static readonly HashSet<int> _flowBonusOnlyCardIds = new HashSet<int>
{
    9001008,  // 清司风流
    // 在此添加更多卡牌ID...
};
```

## 日志文件
日志输出到：`c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\SteriaModFolder\steria_log.txt`
上一次游戏运行的日志在本电脑的位置：
C:\Program Files (x86)\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\steria_log.txt