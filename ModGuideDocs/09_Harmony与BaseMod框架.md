# Harmony与BaseMod框架操作手册

## 一、框架概述

### Harmony

**Harmony** 是一个运行时代码修改库，允许在不修改原始DLL的情况下：
- 在方法执行前/后注入代码（Prefix/Postfix）
- 完全替换方法实现（Transpiler）
- 修改私有字段和方法

### BaseMod

**BaseMod** 是废墟图书馆的官方模组框架，提供：
- 模组加载和初始化
- XML数据自动加载
- 资源管理
- 本地化支持

---

## 二、Harmony基础用法

### 初始化Harmony

```csharp
using HarmonyLib;

public class ModInitializer
{
    private static Harmony _harmony;

    public static void Init()
    {
        // 创建Harmony实例，ID需唯一
        _harmony = new Harmony("com.mymod.libraryofruina");

        // 自动应用所有带[HarmonyPatch]特性的补丁
        _harmony.PatchAll();
    }

    public static void Cleanup()
    {
        // 卸载所有补丁
        _harmony?.UnpatchSelf();
    }
}
```

---

## 三、Harmony Patch类型

### Prefix（前置补丁）

在原方法执行**之前**运行，可以：
- 修改参数
- 阻止原方法执行
- 提前返回结果

```csharp
[HarmonyPatch(typeof(BattleUnitModel))]
[HarmonyPatch("TakeDamage")]
public class Patch_TakeDamage
{
    // 返回false会阻止原方法执行
    [HarmonyPrefix]
    public static bool Prefix(BattleUnitModel __instance, ref int dmg)
    {
        // __instance 是被调用的对象实例
        // ref参数可以修改原参数值

        // 示例：所有伤害减半
        dmg = dmg / 2;

        // 返回true继续执行原方法，false跳过原方法
        return true;
    }
}
```

### Postfix（后置补丁）

在原方法执行**之后**运行，可以：
- 读取/修改返回值
- 执行额外逻辑

```csharp
[HarmonyPatch(typeof(BattleUnitModel))]
[HarmonyPatch("RecoverHP")]
public class Patch_RecoverHP
{
    [HarmonyPostfix]
    public static void Postfix(BattleUnitModel __instance, int amount, ref int __result)
    {
        // __result 是原方法的返回值（如果有）
        // 可以修改返回值

        Debug.Log($"{__instance.UnitData.unitData.name} 恢复了 {amount} HP");
    }
}
```

### 参数命名规则

| 参数名 | 含义 |
|--------|------|
| `__instance` | 被调用对象的实例（非静态方法） |
| `__result` | 原方法的返回值 |
| `__state` | Prefix和Postfix之间传递数据 |
| `___fieldName` | 访问私有字段（三个下划线+字段名） |
| 原参数名 | 直接使用原方法的参数名 |

---

## 四、Harmony实战示例

### 示例1：修改骰子威力计算

```csharp
[HarmonyPatch(typeof(BattleDiceBehavior))]
[HarmonyPatch("GetDiceResultValue")]
public class Patch_DiceResult
{
    [HarmonyPostfix]
    public static void Postfix(BattleDiceBehavior __instance, ref int __result)
    {
        // 检查是否有自定义Buff
        var owner = __instance.owner;
        if (owner == null) return;

        var myBuff = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_MyCustomBuff);

        if (myBuff != null)
        {
            // 骰子结果+Buff层数
            __result += myBuff.stack;
        }
    }
}
```

### 示例2：拦截伤害并触发效果

```csharp
[HarmonyPatch(typeof(BattleUnitModel))]
[HarmonyPatch("TakeDamage")]
public class Patch_TakeDamage_TriggerEffect
{
    [HarmonyPrefix]
    public static bool Prefix(BattleUnitModel __instance, int dmg,
        DamageType type, BattleUnitModel attacker)
    {
        // 检查是否有护盾Buff
        var shieldBuff = __instance.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_MyShield) as BattleUnitBuf_MyShield;

        if (shieldBuff != null && shieldBuff.stack > 0)
        {
            // 护盾吸收伤害
            int absorbed = Math.Min(shieldBuff.stack, dmg);
            shieldBuff.stack -= absorbed;

            if (absorbed >= dmg)
            {
                // 完全吸收，跳过原方法
                return false;
            }
        }

        return true; // 继续执行原方法
    }
}
```

### 示例3：修改卡牌费用

```csharp
[HarmonyPatch(typeof(BattleDiceCardModel))]
[HarmonyPatch("GetCost")]
public class Patch_CardCost
{
    [HarmonyPostfix]
    public static void Postfix(BattleDiceCardModel __instance, ref int __result)
    {
        // 检查卡牌是否有特定关键字
        if (__instance.GetID().id == 9000101) // 特定卡牌ID
        {
            // 费用-1，最低为0
            __result = Math.Max(0, __result - 1);
        }
    }
}
```

### 示例4：访问私有字段

```csharp
[HarmonyPatch(typeof(BattleUnitModel))]
[HarmonyPatch("OnRoundStart")]
public class Patch_RoundStart
{
    [HarmonyPostfix]
    public static void Postfix(BattleUnitModel __instance,
        int ___hp,           // 访问私有字段hp
        int ___breakDetail)  // 访问私有字段
    {
        Debug.Log($"当前HP: {___hp}");
    }
}
```

---

## 五、BaseMod框架

### ModInitializer基类

```csharp
using BaseMod;

public class MyModInitializer : ModInitializer
{
    // 模组加载时调用
    public override void OnInitializeMod()
    {
        // 初始化Harmony
        Harmony harmony = new Harmony("com.mymod.lor");
        harmony.PatchAll();

        // 注册自定义资源
        // ...

        Debug.Log("[MyMod] 模组已加载");
    }
}
```

### BaseMod自动加载

BaseMod会自动加载以下内容：

| 目录/文件 | 自动加载内容 |
|-----------|--------------|
| `Data/*.xml` | 游戏数据定义 |
| `Localize/*/` | 本地化文本 |
| `Assemblies/*.dll` | C#程序集 |
| `Resource/CombatPageArtwork/` | 卡牌图片 |

### 手动注册资源

```csharp
public override void OnInitializeMod()
{
    // 获取模组路径
    string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    // 加载自定义AssetBundle
    string abPath = Path.Combine(modPath, "AB", "myeffects.ab");
    AssetBundle bundle = AssetBundle.LoadFromFile(abPath);

    // 注册到游戏
    // ...
}
```

---

## 六、常用Harmony补丁场景

### 场景1：添加新的触发时机

```csharp
// 在角色使用书页时触发自定义逻辑
[HarmonyPatch(typeof(BattleUnitModel))]
[HarmonyPatch("OnUseCard")]
public class Patch_OnUseCard
{
    [HarmonyPostfix]
    public static void Postfix(BattleUnitModel __instance,
        BattlePlayingCardDataInUnitModel curCard)
    {
        // 触发所有自定义被动的OnUseCard
        foreach (var passive in __instance.passiveDetail.PassiveList)
        {
            if (passive is IMyCustomPassive customPassive)
            {
                customPassive.OnCustomUseCard(curCard);
            }
        }
    }
}

// 自定义接口
public interface IMyCustomPassive
{
    void OnCustomUseCard(BattlePlayingCardDataInUnitModel card);
}
```

### 场景2：修改游戏常量

```csharp
[HarmonyPatch(typeof(StageController))]
[HarmonyPatch("get_MaxRound")]
public class Patch_MaxRound
{
    [HarmonyPostfix]
    public static void Postfix(ref int __result)
    {
        // 将最大回合数改为50
        __result = 50;
    }
}
```

### 场景3：拦截UI显示

```csharp
[HarmonyPatch(typeof(BattleUnitInfoManagerUI))]
[HarmonyPatch("UpdateStatUI")]
public class Patch_UpdateUI
{
    [HarmonyPostfix]
    public static void Postfix(BattleUnitInfoManagerUI __instance,
        BattleUnitModel unit)
    {
        // 在UI更新后添加自定义显示
        // ...
    }
}
```

---

## 七、Harmony高级用法

### Transpiler（IL修改）

直接修改方法的IL代码，最强大但也最复杂：

```csharp
[HarmonyPatch(typeof(SomeClass))]
[HarmonyPatch("SomeMethod")]
public class Patch_Transpiler
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        for (int i = 0; i < codes.Count; i++)
        {
            // 查找并修改特定IL指令
            if (codes[i].opcode == OpCodes.Ldc_I4_S &&
                (sbyte)codes[i].operand == 10)
            {
                // 将常量10改为20
                codes[i].operand = (sbyte)20;
            }
        }

        return codes;
    }
}
```

### 手动Patch

```csharp
public static void ManualPatch()
{
    var harmony = new Harmony("com.mymod.lor");

    // 获取原方法
    var original = typeof(BattleUnitModel).GetMethod("TakeDamage",
        BindingFlags.Public | BindingFlags.Instance);

    // 获取补丁方法
    var prefix = typeof(MyPatches).GetMethod("TakeDamage_Prefix",
        BindingFlags.Static | BindingFlags.Public);

    // 应用补丁
    harmony.Patch(original, prefix: new HarmonyMethod(prefix));
}
```

### 条件Patch

```csharp
[HarmonyPatch]
public class ConditionalPatch
{
    // 动态决定要patch的方法
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        // 根据条件返回不同的方法
        return typeof(BattleUnitModel).GetMethod("TakeDamage");
    }

    [HarmonyPrefix]
    public static bool Prefix() => true;
}
```

---

## 八、调试Harmony补丁

### 启用Harmony日志

```csharp
public override void OnInitializeMod()
{
    // 启用详细日志
    Harmony.DEBUG = true;

    var harmony = new Harmony("com.mymod.lor");
    harmony.PatchAll();
}
```

### 检查补丁是否生效

```csharp
public static void CheckPatches()
{
    var harmony = new Harmony("com.mymod.lor");

    // 获取所有已应用的补丁
    var patches = Harmony.GetAllPatchedMethods();

    foreach (var method in patches)
    {
        var info = Harmony.GetPatchInfo(method);
        Debug.Log($"方法: {method.Name}");
        Debug.Log($"  Prefixes: {info.Prefixes.Count}");
        Debug.Log($"  Postfixes: {info.Postfixes.Count}");
    }
}
```

---

## 九、常见问题

### Q1: Patch不生效

**检查项：**
1. 方法名和参数类型是否完全匹配
2. 是否调用了`harmony.PatchAll()`
3. 类是否标记了`[HarmonyPatch]`特性

### Q2: 游戏崩溃

**可能原因：**
1. Prefix返回false但没有设置__result
2. 访问了null对象
3. 参数类型不匹配

**解决：** 添加try-catch和日志

```csharp
[HarmonyPrefix]
public static bool Prefix(BattleUnitModel __instance)
{
    try
    {
        // 补丁逻辑
        return true;
    }
    catch (Exception e)
    {
        Debug.LogError($"[MyMod] Patch错误: {e}");
        return true; // 出错时继续执行原方法
    }
}
```

### Q3: 与其他模组冲突

**解决：**
1. 使用唯一的Harmony ID
2. 设置补丁优先级

```csharp
[HarmonyPatch(typeof(BattleUnitModel))]
[HarmonyPatch("TakeDamage")]
[HarmonyPriority(Priority.High)] // 优先级：First > VeryHigh > High > Normal > Low > VeryLow > Last
public class Patch_HighPriority
{
    [HarmonyPrefix]
    public static bool Prefix() => true;
}
```

---

## 十、完整示例：自定义流系统

```csharp
using HarmonyLib;
using BaseMod;
using System.Collections.Generic;
using UnityEngine;

// === ModInitializer ===
public class FlowModInitializer : ModInitializer
{
    public static Harmony harmony;

    public override void OnInitializeMod()
    {
        harmony = new Harmony("com.flowmod.lor");
        harmony.PatchAll();
        Debug.Log("[FlowMod] 已加载");
    }
}

// === 流Buff ===
public class BattleUnitBuf_Flow : BattleUnitBuf
{
    protected override string keywordId => "FlowMod_Flow";
    protected override string keywordIconId => "FlowMod_Flow";
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    public int ConsumeFlow(int amount)
    {
        int consumed = Mathf.Min(stack, amount);
        stack -= consumed;
        if (stack <= 0) Destroy();
        return consumed;
    }
}

// === Harmony补丁：骰子威力加成 ===
[HarmonyPatch(typeof(BattleDiceBehavior))]
[HarmonyPatch("ApplyDiceStatBonus")]
public class Patch_FlowDiceBonus
{
    [HarmonyPrefix]
    public static void Prefix(BattleDiceBehavior __instance, ref DiceStatBonus bonus)
    {
        var owner = __instance.owner;
        if (owner == null) return;

        var flowBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;

        if (flowBuf != null && flowBuf.stack > 0)
        {
            // 每层流+1威力
            int flowBonus = flowBuf.ConsumeFlow(flowBuf.stack);
            bonus.power += flowBonus;
        }
    }
}

// === 被动：获得流 ===
public class PassiveAbility_FlowMod_GainFlow : PassiveAbilityBase
{
    public override void OnWinParrying(BattleDiceBehavior behavior)
    {
        var flowBuf = owner.bufListDetail.GetActivatedBufList()
            .FirstOrDefault(b => b is BattleUnitBuf_Flow) as BattleUnitBuf_Flow;

        if (flowBuf != null)
        {
            flowBuf.stack += 2;
        }
        else
        {
            owner.bufListDetail.AddBuf(new BattleUnitBuf_Flow { stack = 2 });
        }
    }
}
```
