# Buff系统与特效操作手册

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

    // 添加层数时
    public override void OnAddBuf(int addedStack)
    {
        this.stack += addedStack;
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

// 下一幕生效
owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 2, owner);

// 对目标添加
target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Binding, 3, owner);
```

---

## 四、Buff本地化 (Localize/cn/EffectTexts/)

```xml
<?xml version="1.0" encoding="utf-8"?>
<BattleEffectTextRoot>
  <BattleEffectText ID="MyMod_CustomBuff">
    <Name>自定义Buff</Name>
    <Desc>每层使攻击骰子威力+1</Desc>
  </BattleEffectText>

  <BattleEffectText ID="MyMod_Stun">
    <Name>眩晕</Name>
    <Desc>无法行动</Desc>
  </BattleEffectText>
</BattleEffectTextRoot>
```

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

## 八、下一幕生效的Buff

```csharp
public class BattleUnitBuf_StrengthNextTurn : BattleUnitBuf
{
    protected override string keywordId => "MyMod_StrengthNext";

    public override void OnRoundStart()
    {
        // 下一幕开始时转换为实际效果
        _owner.bufListDetail.AddKeywordBufThisRoundByEtc(
            KeywordBuf.Strength, this.stack, _owner);
        this.Destroy();
    }
}

// 使用方式
owner.bufListDetail.AddBuf(new BattleUnitBuf_StrengthNextTurn() { stack = 3 });
```

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
