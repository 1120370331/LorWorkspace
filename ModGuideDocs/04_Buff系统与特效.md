# Buff系统与特效操作手册

**合计规则与状态栏是两份合同：** 资源判断可以按技能设计合计普通状态、强化状态或其它来源；UI 图标仍必须分别显示每个独立状态的真实层数。不要为方便实现合计而让两个图标都读取同一个总数，也不要用机制模拟的“总量正确”推断状态栏正确。验收时同时记录底层各 Buff 层数、规则实际读到的合计、每个 UI 图标对应的实例与可见数字；计数路径可用模拟工程核验，显示结论需要实际 UI 数据绑定和渲染证据。案例：AceTaffy 的烟气和蒸汽压力分别显示，烟气计数统一包含压力；蒸汽回收的支付明确例外，只扣普通烟气。为例外提供独立消耗入口，不能改坏其它书页的通用计数与消耗。玩家文案直接使用烟气名称，保留原版烟气说明，不用兼容实现说明覆盖原版文本。

**先区分“获得”与“生效”：** 状态可以现在进入待生效列表、到下一幕才参与计算。原版半透明状态图标表达的正是这类待生效状态；完整流程、API 选择和月之引力案例见第八节。

## 一、自定义Buff实现

### 基础Buff结构

```csharp
public class BattleUnitBuf_MyCustomBuff : BattleUnitBuf
{
    // Buff关键字ID（用于本地化）
    protected override string keywordId => "MyMod_CustomBuff";

    // Buff图标ID
    protected override string keywordIconId => "MyMod_CustomBuff";

    // Buff类型：正面/负面/无
    public override BufPositiveType positiveType => BufPositiveType.Positive;
    // BufPositiveType.Positive  - 正面Buff（绿色）
    // BufPositiveType.Negative  - 负面Buff（红色）
    // BufPositiveType.None      - 中性

    // 初始化
    public override void Init(BattleUnitModel owner)
    {
        base.Init(owner);
        // 初始化逻辑
    }

    // 层数已经由添加入口修改；此回调只处理通知、上限或表现
    public override void OnAddBuf(int addedStack)
    {
        // 不在这里再次 stack += addedStack，否则原生添加/晋升会重复加层
    }

    // 每幕开始
    public override void OnRoundStart()
    {
        // 幕开始效果
    }

    // 每幕结束
    public override void OnRoundEnd()
    {
        // 层数递减
        this.stack--;
        if (this.stack <= 0)
        {
            this.Destroy();
        }
    }

    // 销毁时
    public override void OnDestroy()
    {
        // 清理逻辑
    }
}
```

---

## 二、Buff常用方法

### 伤害修改

```csharp
public class BattleUnitBuf_DamageBoost : BattleUnitBuf
{
    // 造成伤害时的加成
    public override int GetDamageIncreaseRate()
    {
        return this.stack * 10; // 每层+10%伤害
    }

    // 受到伤害时的减免
    public override int GetDamageReductionRate()
    {
        return this.stack * 5; // 每层-5%伤害
    }

    // 固定伤害减免
    public override int GetDamageReduction(BattleDiceBehavior behavior)
    {
        return this.stack; // 每层-1伤害
    }
}
```

### 骰子修改

```csharp
public class BattleUnitBuf_DiceBoost : BattleUnitBuf
{
    // 投骰前修改
    public override void BeforeRollDice(BattleDiceBehavior behavior)
    {
        // 攻击骰子威力+1
        if (behavior.Type == BehaviourType.Atk)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = this.stack });
        }
    }

    // 修改骰子最小值
    public override int GetDiceMin(int diceMin)
    {
        return diceMin + this.stack;
    }

    // 修改骰子最大值
    public override int GetDiceMax(int diceMax)
    {
        return diceMax + this.stack;
    }
}
```

### 状态效果

```csharp
public class BattleUnitBuf_Stun : BattleUnitBuf
{
    protected override string keywordId => "MyMod_Stun";
    public override BufPositiveType positiveType => BufPositiveType.Negative;

    // 是否可以行动
    public override bool IsActionable()
    {
        return false; // 无法行动
    }

    // 是否可以被选为目标
    public override bool IsTargetable()
    {
        return true;
    }

    public override void OnRoundEnd()
    {
        this.Destroy(); // 一幕后消失
    }
}
```

---

## 三、内置KeywordBuf枚举

```csharp
// 常用内置Buff类型
KeywordBuf.Strength      // 强壮（攻击骰子威力+1）
KeywordBuf.Endurance     // 忍耐（防御骰子威力+1）
KeywordBuf.Quickness     // 迅捷（速度+1）
KeywordBuf.Protection    // 守护（受到伤害-1）
KeywordBuf.Fairy         // 振奋（恢复HP+1）

KeywordBuf.Weak          // 虚弱（攻击骰子威力-1）
KeywordBuf.Disarm        // 武装解除（防御骰子威力-1）
KeywordBuf.Binding       // 束缚（速度-1）
KeywordBuf.Vulnerable    // 易伤（受到伤害+1）
KeywordBuf.Burn          // 烧伤（幕结束受伤）
KeywordBuf.Bleeding      // 流血（幕结束受伤）
KeywordBuf.Paralysis     // 麻痹（骰子最大值-1）
KeywordBuf.Erosion       // 腐蚀（额外伤害）

KeywordBuf.Stagger       // 晕眩
KeywordBuf.Seal          // 封印
KeywordBuf.Smoke         // 烟雾
KeywordBuf.Charge        // 充能
```

### 添加内置Buff

```csharp
// 本幕生效
owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 2, owner);

// 常见状态进入待生效列表，在下一次该单位的状态晋升时生效
// 注意 Burn 等特殊分支，以及幕开始回调相对晋升的先后，详见第八节
owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 2, owner);

// 对目标添加
target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Binding, 3, owner);
```

---

## 四、Buff本地化 (Localize/cn/EffectTexts/)

```xml
<?xml version="1.0" encoding="utf-8"?>
<BattleEffectTextRoot>
  <effectTextList>
  <BattleEffectText ID="MyMod_CustomBuff">
    <Name>自定义Buff</Name>
    <Desc>每层使攻击骰子威力+1</Desc>
  </BattleEffectText>

  <BattleEffectText ID="MyMod_Stun">
    <Name>眩晕</Name>
    <Desc>无法行动</Desc>
  </BattleEffectText>
  </effectTextList>
</BattleEffectTextRoot>
```

`keywordId` 对应上表的 BattleEffectText ID，`keywordIconId` 则是图标资源键。需要在书页说明栏展示此状态时，还应通过 CardInfo 的 Keyword 或能力 Keywords 登记该 ID；正文的 `[名称]` 只负责高亮。完整示例见[第 02 章第八节](02_战斗书页完整制作.md)。玩家可见的中文正文按项目规范使用全角中文标点；`[名称]`、XML 与 ID 等机器语法仍保留半角解析格式。

---

## 五、Buff图标

| 属性 | 规格 |
|------|------|
| 格式 | PNG |
| 尺寸 | 64x64 像素 |
| 位置 | `Assemblies/ArtWork/buff/` |
| 命名 | 与`keywordIconId`一致 |

---

## 六、特效系统

关于战斗特效的详细制作方法，请参阅 **[10_角色外观与特效系统.md](10_角色外观与特效系统.md)**。

### 快速参考

在骰子行为中指定特效：

```xml
<Behaviour Min="4" Dice="8" Type="Atk" Detail="Slash" Motion="J"
           EffectRes="Zwei_J" Script="" Desc="" />
```

自定义特效类命名规则：`DiceAttackEffect_[名称]_[锚点后缀]`

---

## 七、护盾系统

### 添加护盾

```csharp
// 添加护盾Buff
owner.bufListDetail.AddBuf(new BattleUnitBuf_Shield() { stack = 10 });
```

### 护盾Buff实现

```csharp
public class BattleUnitBuf_Shield : BattleUnitBuf
{
    protected override string keywordId => "MyMod_Shield";
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    // 受到伤害前
    public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
    {
        if (this.stack > 0)
        {
            int absorbed = Math.Min(this.stack, dmg);
            this.stack -= absorbed;

            if (this.stack <= 0)
            {
                this.Destroy();
            }

            // 返回true表示完全吸收，false表示部分吸收
            return absorbed >= dmg;
        }
        return false;
    }
}
```

---

## 八、理清“获得”：即时状态、半透明待生效状态与跨幕结算

游戏中的“获得”至少要拆成**触发、入队、加层、生效、消耗/清除**五件事。比如“命中时使目标下幕获得 2 层强壮”，命中当下就可以登记并显示待生效的 2 层，但本幕的骰子尚不享受它。原版常用半透明图标表示这份已登记、尚未生效的状态。

不能仅凭描述里有没有“下回合”选择实现：一些原版状态的默认获得方式就会延后，烧伤又有即时分支。制作新状态时，先把生效时间写清，再选择列表和入口。

### 第 1 步：确定状态属于哪一幕

**做什么：** 为每条状态规则记录：什么时候触发、谁施加给谁、加多少层、本幕还是下幕生效、持续多久，以及清除/转化是否包含待生效层。

| 规则想表达什么 | 入哪个列表 | 何时参与普通 Buff 效果计算 |
|---|---|---|
| 本幕立即获得并生效 | Activated：`GetActivatedBufList()` | 入列表后参与相应的后续计算；已经结算的事件不会倒放 |
| 本幕登记，下幕生效 | Ready：`GetReadyBufList()` | 下一次该单位的状态晋升时进入 Activated |
| 登记到再下一幕 | ReadyReady：`GetReadyReadyBufList()` | 先转入 Ready，再等一次晋升 |
| 下一幕执行一次特殊事务 | 依事务选择回调/调度器 | 例如发牌、回血；不应无条件把它当成需要原生状态图标的 Buff |

同种状态的“本幕已生效 3 层”和“下幕待生效 2 层”可以并存，必须分开计数。本幕伤害与威力不能把下幕层数也算进去；“转化所有层数”则需要明确是否读取这两份。

**为什么：** 把所有层数放进一个变量，无法同时表达当下的战斗效果和下一幕的预告。

**带来什么效果：** 玩家看到的状态与实际结算时间一致，后续读取和消耗也有明确边界。

### 第 2 步：选择时间入口和来源入口

**做什么：** 时间和来源是两个独立维度，按下表选择。

| 入口 | 常见去向 | 来源语义 / 注意事项 |
|---|---|---|
| `AddKeywordBufThisRoundByCard(type, n, actor)` | Activated | 书页来源，走原生施加者/接收者的书页状态修正与相关事件 |
| `AddKeywordBufByCard(type, n, actor)` | Ready | 书页来源；本版本对 `KeywordBuf.Burn` 转入 ThisRound 分支 |
| `AddKeywordBufNextNextByCard(type, n, actor)` | ReadyReady | 本版本对 Burn 同样转为立即添加，不能用它强行延后烧伤 |
| `AddKeywordBufThisRoundByEtc(type, n, actor)` | Activated | 其他来源，不走同一套 ByCard 施加/接收修正链 |
| `AddKeywordBufByEtc(type, n, actor)` | Ready | 其他来源；Burn 例外为 Activated |
| `AddBuf(customBuf)` | Activated | 添加自定义实例，调用 Init；不会自动执行完整 KeywordBuf 加层修正链 |
| `AddReadyBuf(customBuf)` | Ready | 添加同一个正式状态类型的待生效实例，调用 Init；不是创建隐藏计时器 |
| `AddReadyReadyBuf(customBuf)` | ReadyReady | 再延后一次晋升；图标展示还需核对对应 UI 是否支持此列表 |

例如在战斗行动中命中时，通过书页获得下幕束缚：

```csharp
target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Binding, 2, owner);
```

明确本幕立即获得强壮时：

```csharp
owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 2, owner);
```

原生 `ByCard` 会经过 `OnGiveKeywordBufByCard`、`OnAddKeywordBufByCard`、施加倍率和 `OnAddKeywordBufByCardForEvent` 等流程；`ByEtc` 不等价于它。`actor` 是施加者，target/owner 所属的 bufListDetail 是接收者，二者不要混用。本模组还对这些入口加了潮/黄金之潮补丁，改变入口会改变联动；不能只为“显示出来”而随意换方法。

**烧伤例外：** 本仓库游戏源码里的默认 ByCard、ByEtc，甚至 NextNextByCard，在遇到 Burn 时都转调 ThisRound。若设计确实要求未来某幕才施加烧伤，应另行安排正确时间的施加事件，并验证叠加、伤害和清理，不能只从方法名推断时间。

**为什么：** “立即/延后”决定何时生效，“ByCard/ByEtc”决定按哪条来源与修正链计算，改动任何一项都会改变机制。

**带来什么效果：** 状态不仅在正确时间出现，层数加成、免疫与相关被动也按设计参与。

### 第 3 步：让原版列表驱动半透明显示与晋升

**做什么：** 在第 N 幕行动中登记 Ready，交给原版跨幕流程处理：

```text
第 N 幕行动：AddReadyBuf / 默认 Keyword 入口 → Ready
  本幕：可显示待生效图标，不参与 Activated 的伤害/威力计算
  本幕结束：普通 OnRoundEnd 遍历 Activated，不结算 Ready 的幕末效果
第 N+1 幕的状态处理：
  先把 Ready 合入/加入 Activated，再清空 Ready
  再把 ReadyReady 移到 Ready，并清空 ReadyReady
  最后对 Activated 快照调用各 Buff 的 OnRoundStart
第 N+1 幕结束：
  按该状态自己的 OnRoundEnd 递减、清空或保留
```

`GetBufUIDataList()` 把三种列表分别导出。在已核对的 `BattleUnitBottomStatUI` 中，Activated 图标与层数 alpha 为 **1.0**，Ready 为 **0.3**。因此状态应放进正确列表，让原版负责样式；直接把已生效图标改成 0.3 并不会延后伤害计算。图标还需要正确的 keywordIconId、资源注册和非隐藏状态；缺图或 Hide 不能靠换列表解决。

这个底部 UI 没有同样遍历 ReadyReady，不能因为数据层支持再下一幕队列，就承诺它会显示第二组半透明图标。其他状态面板或模组扩展应分别核对。战斗播放也可能消费事件快照，不能要求修改列表后所有界面在同一帧立即刷新。

**为什么：** 半透明是待生效数据的呈现结果，激活和持续时间由列表推进及状态回调决定。

**带来什么效果：** 玩家先看到下幕将获得的状态，下幕再看到正式生效，不需要维护两套伪装成同一效果的计时图标。

### 第 4 步：处理跨幕时序，避免多等一幕或重复加层

**做什么：** 核对施加行为发生在接收者的哪一步。本仓库 `BattleUnitModel.OnRoundStart_before()` 的关键顺序是：

```text
被动 OnRoundStart
→ 情感能力 OnRoundStart
→ bufListDetail.OnRoundStart：Ready 晋升，然后 Activated.OnRoundStart
→ 手牌 OnRoundStart
→ 被动 / Buff 的 OnRoundStartAfter
```

所以“进入 Ready”准确地说是等待**该接收者下一次晋升**。在被动 OnRoundStart 中向自身添加 Ready，可能紧接着就在当前这次幕开始晋升；在 Activated Buff 的 OnRoundStart 或更晚回调才添加 Ready，则本次晋升已结束，会再等一幕。向其他单位施加还要看接收者是否已处理过幕开始，不能只看施加者的回调名。

若规则已安排在“下一幕开始时执行”，到时应选择相应的即时入口，避免又排一次 Ready。优先在原触发事件中一次性登记目标幕，减少隐藏辅助 Buff 的二次排队。

另外，原生 Keyword 添加方法先执行 `buf.stack += amount` 再调用 `OnAddBuf(amount)`；Ready 晋升时也会加层或加入实例后调用该通知。**不要在 OnAddBuf 中再次 `stack += addedStack`**，也不要把它当成“玩家刚刚又获得一次”的唯一证据。上限裁剪可以在此进行，获得奖励或触发被动则应放在有明确来源和事件次数的入口。

`AddReadyBuf` 会调用 Init，晋升成 Activated 时的 AddBuf 还可能再次调用 Init。Init 应负责挂接 owner 等可重复初始化，不应借此再发奖励或重复计时。

**为什么：** 名字都叫 OnRoundStart 的回调并不同时发生。额外的计时器、再次排队和重复加层会分别造成延后、提前或层数翻倍。

**带来什么效果：** 一次获得只计算一次层数和来源修正，在目标幕准确生效。

### 第 5 步：自定义状态用同一正式类型进入 Ready

**做什么：** 对“下幕生效，在生效幕末清空”这类自定义状态，正式 Buff 类型本身描述效果与幕末清理；添加助手负责进入 Ready，并在同一目标幕内合并重复获得。

有两个实现选择：

- 用已接入的关键词扩展注册唯一 `bufType`，明确同 KeywordBuf 状态的加层与晋升规则，再走对应 Keyword 添加方法。只定义 `keywordId` 或把名字写成枚举字符串，不会自动完成注册。
- 保留自定义类的 `KeywordBuf.None`，自行按具体类型合并 Ready 和晋升后的 Activated。原生跨幕合并要求有效的非 None KeywordBuf，不能假设同一个 C# 类型会自动合并，也不能用 `RemoveBufAll(KeywordBuf.None)` 清理某一种自定义状态。

下面是第二种方式的独立教学示例；它不替换当前月之引力，也未接入潮加成等自定义来源规则。使用时需另配 keywordId 对应文本与图标：

```csharp
using System;
using System.Linq;

public class BattleUnitBuf_ExampleDeferredBreak : BattleUnitBuf
{
    protected override string keywordId { get { return "MyMod_DeferredBreak"; } }
    protected override string keywordIconId { get { return "MyMod_DeferredBreak"; } }
    public override BufPositiveType positiveType { get { return BufPositiveType.Negative; } }

    public static void AddNextRound(BattleUnitModel target, int amount)
    {
        if (target == null || amount <= 0) return;
        var detail = target.bufListDetail;
        var pending = detail.GetReadyBufList()
            .OfType<BattleUnitBuf_ExampleDeferredBreak>()
            .FirstOrDefault(b => !b.IsDestroyed());
        if (pending == null)
            detail.AddReadyBuf(new BattleUnitBuf_ExampleDeferredBreak { stack = Math.Min(10, amount) });
        else if (detail.CanAddBuf(pending))
            pending.stack = (int)Math.Min(10L, (long)pending.stack + amount);
    }

    public override void OnAddBuf(int addedStack)
    {
        stack = Math.Min(10, Math.Max(0, stack)); // 通知时层数已经添加
    }

    public override void OnRoundStart()
    {
        // None 类型不会由原生按关键词合并，晋升结束后归并到首个有效实例。
        var first = _owner.bufListDetail.GetActivatedBufList()
            .OfType<BattleUnitBuf_ExampleDeferredBreak>()
            .FirstOrDefault(b => !b.IsDestroyed());
        if (first != null && !ReferenceEquals(first, this))
        {
            first.stack = (int)Math.Min(10L, (long)first.stack + stack);
            Destroy();
        }
    }

    public override float BreakDmgFactor(int dmg,
        DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
    {
        return 1f + Math.Min(10, Math.Max(0, stack)) * 0.1f;
    }

    public override void OnRoundEnd() { Destroy(); }
}
```

在第 N 幕命中回调中调用 `BattleUnitBuf_ExampleDeferredBreak.AddNextRound(target, 2)`，会登记下次晋升的 2 层；第 N 幕不提供该伤害倍率，第 N+1 幕生效，N+1 幕末清除。这个简化示例适合所述一幕状态；若状态永久存在、需要在晋升过程中就查询聚合层数，或具有不同来源/持续时间，需设计更完整的合并模型。

`AddReadyBuf` 会检查 `CanAddBuf`，但不会替你执行 ByCard 的整套来源修正和获得事件。决定了低层自定义入口后，应由一个清晰的助手处理所需来源、免疫、上限和通知，避免绕过联动或在晋升时再次消费来源资源。

**为什么：** 同类多个 Activated 实例可能各自计算倍率；例如两个 1 层实例可能变成 `1.1 × 1.1`，而设计想要的是一个 2 层实例的 `1.2`。只改成 AddReadyBuf，而不检查合并规则，仍可能留下错误。

**带来什么效果：** 同一状态有稳定的层数、上限和图标，待生效与正式生效由原生列表区分。

### 第 6 步：以“月之引力”为例定位当前实现偏差

当前 [ChristashaAbilities.cs](../SteriaBuild/ChristashaAbilities.cs) 的 `AddMoonGravity` 创建 `BattleUnitBuf_MoonGravityNextRound`，并用 **AddBuf 放入 Activated**；辅助类再在自己的 OnRoundStart 中创建正式 `BattleUnitBuf_MoonGravity` 并销毁自身。

这个实现可以实现某些时点下的延迟效果，但没有复刻原生 Ready 状态：

- UI 数据把它归为 Activated，会按正式状态列表显示。注释写“本回合隐藏”并不生效，当前类没有设置 Hide 来完成该注释，隐藏本身也不等于原生半透明预告。
- 本幕的负面状态统计或自定义遍历可能把辅助状态视为已生效状态；额外的 `SteriaMoonGravityNext` 名称又把同一机制拆成两种状态身份。
- 转正时间取决于辅助实例加入 Activated 的时点，不能无条件断言“任何时候调用都正好下一幕”。

复刻原生语义时，应让**正式月之引力实例进入 Ready**，同一目标幕的待生效层合并并保持最多 10 层，晋升后按正式类型提供混乱伤害倍率，在生效幕结束时清除。迁移时必须同步检查：

1. `ChristashaAbilityHelper.AddMoonGravity` 的所有施加者和调用时机，以及来源加成是否应参与。
2. 当前、待生效和必要的再下一幕层数分别怎样读取、叠加和清除，不能全都改成只读 Activated。
3. [ChristashaCards.cs](../SteriaBuild/ChristashaCards.cs) 的 `DiceCardAbility_ChristashaTwinStarBurn` 目前把 Activated 中正式与辅助两类月之引力都转成烧伤。迁移后若保留“包含待生效层”的现有范围，应改为消费正式类型的 Activated + Ready，再明确是否包含 ReadyReady。
4. 从 Ready 消耗或移除时，显式维护列表；本版本底部 UI 的 Ready 分支不按 destroyed 过滤，`Destroy()` 单独清零可能留下待生效图标，使用 `RemoveReadyBuf` 等正确移除方式，且不要在原列表枚举中直接删除。
5. 转成烧伤后仍走烧伤自身的即时规则；“月之引力是下幕”不能推导“烧伤也下幕”。同步调整本地化，避免独立“下回合”辅助名与原生待生效展示重复。

这里记录的是代码诊断与迁移规范，**当前月之引力运行时代码未在本次教程修改中替换**。

### 第 7 步：按状态列表与效果验证“获得”

| 场景 | 应看到的列表 / UI | 应得到的实际效果 |
|---|---|---|
| N 幕行动中施加 2 层 | Ready=2；有图标且非隐藏时显示半透明预告 | N 幕不增加混乱伤害倍率 |
| 同幕再施加 3 层 | 同一待生效项合并为 5 | 不提前生效，不重复出现多个倍率实例 |
| N 幕结束 | Ready 仍为 5 | 不被该状态的普通 OnRoundEnd 提前清掉 |
| N+1 幕开始 | Ready 清空，Activated=5，图标转正式显示 | 仅该效果时倍率为 1.5，未翻倍加层 |
| N+1 幕又施加 2 层 | Activated=5 与 Ready=2 分开 | 本幕仍按 5 层，新增 2 层等下一次晋升 |
| N+1 幕结束与下一幕 | 当前 5 层清除，待生效 2 层按时晋升 | 无多等一幕，也不带回旧层数 |
| 超过上限、免疫、转化/清除 | 拒绝或合并符合规则；待生效图标不残留 | 最多 10 层，消耗范围明确，来源加成只算一次 |
| 幕开始不同回调中获得 | 按接收者晋升先后检查 | 不凭回调名字猜测时间 |

本节的完整教学类已编译，并用实际游戏 DLL 的 Ready 晋升、幕末清理与混乱伤害倍率接口完成 9 项隔离检查；探针跳过与本题无关的平台成就检查。半透明样式依据 UI 源码核对，未将这些检查表述为游戏画面实测。

以上倍率只指教学状态自身，其他混乱伤害修正仍按游戏结算。分别验证列表、效果数值与实际 UI；只看到半透明图标或只有类名带 NextRound，都不能证明逻辑正确。

**源码索引（以 `SteriaBuild/Assembly-CSharp/` 为根）：** `BattleUnitBufListDetail.cs` 的 AddKeywordBuf 系列、AddBuf/AddReadyBuf/AddReadyReadyBuf、OnRoundStart/OnRoundEnd、GetBufUIDataList、BreakDmgFactor；`BattleUnitModel.cs` 的 OnRoundStart_before；`BattleUnitBottomStatUI.cs` 的 Activated/Ready 绘制；`BattleUnitBuf.cs` 的 Init/OnAddBuf/Destroy。项目来源加成另见 `SteriaBuild/HarmonyPatches.cs` 的 TidePatch。

---

## 九、条件触发Buff

```csharp
public class BattleUnitBuf_CounterAttack : BattleUnitBuf
{
    protected override string keywordId => "MyMod_Counter";
    public override BufPositiveType positiveType => BufPositiveType.Positive;

    // 被攻击命中时触发
    public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
    {
        if (atkDice?.owner != null && this.stack > 0)
        {
            // 反击：对攻击者造成伤害
            atkDice.owner.TakeDamage(this.stack * 2, DamageType.Buf, _owner);
            this.stack--;

            if (this.stack <= 0)
            {
                this.Destroy();
            }
        }
    }
}
```
