# 废墟图书馆模组制作操作手册

## 文档索引

| 文档 | 内容 |
|------|------|
| [01_模组基础结构](01_模组基础结构.md) | 文件夹结构、命名规范、必要引用 |
| [02_战斗书页完整制作](02_战斗书页完整制作.md) | CardInfo.xml、骰子属性、群攻、特效、C#效果实现 |
| [03_核心书页与被动系统](03_核心书页与被动系统.md) | EquipPage、被动类型、随幕变化被动 |
| [04_Buff系统与特效](04_Buff系统与特效.md) | 自定义Buff、内置Buff、特效资源 |
| [05_异想体书页系统](05_异想体书页系统.md) | 异想体能力、E.G.O书页 |
| [06_敌人与关卡系统](06_敌人与关卡系统.md) | 敌人定义、关卡配置、Boss战管理 |
| [07_完整示例与常见问题](07_完整示例与常见问题.md) | 完整角色制作流程、调试技巧 |
| [08_编译与部署](08_编译与部署.md) | 项目配置、编译方法、部署到游戏、创意工坊发布 |
| [09_Harmony与BaseMod框架](09_Harmony与BaseMod框架.md) | Harmony补丁、BaseMod初始化、运行时代码修改 |

---

## 快速参考

### XML文件对应关系

| 功能 | 定义文件 | 本地化文件 |
|------|----------|------------|
| 战斗书页 | `Data/CardInfo.xml` | `Localize/cn/BattleCards/` |
| 骰子效果 | CardInfo.xml中的Script | `Localize/cn/BattleCardAbilities/` |
| 被动能力 | `Data/PassiveList.xml` | `Localize/cn/PassiveDesc/` |
| 核心书页 | `Data/EquipPage_*.xml` | `Localize/cn/Books/` |
| Buff效果 | C#代码 | `Localize/cn/EffectTexts/` |
| 敌人单位 | `Data/EnemyUnitInfo.xml` | `Localize/cn/CharacterName/` |
| 关卡 | `Data/StageInfo.xml` | `Localize/cn/StageName/` |

### C#基类速查

| 功能 | 基类 | 命名格式 |
|------|------|----------|
| 被动能力 | `PassiveAbilityBase` | `PassiveAbility_{名称}` |
| 书页效果 | `DiceCardSelfAbilityBase` | `DiceCardSelfAbility_{名称}` |
| 骰子效果 | `DiceCardAbilityBase` | `DiceCardAbility_{名称}` |
| Buff | `BattleUnitBuf` | `BattleUnitBuf_{名称}` |
| 异想体 | `EmotionCardAbilityBase` | `EmotionCardAbility_{名称}` |
| 关卡管理 | `EnemyTeamStageManager` | `EnemyTeamStageManager_{名称}` |

### 常用触发时机

| 时机 | PassiveAbilityBase | DiceCardSelfAbilityBase | DiceCardAbilityBase |
|------|-------------------|------------------------|---------------------|
| 幕开始 | `OnRoundStart()` | - | - |
| 幕结束 | `OnRoundEnd()` | - | - |
| 使用书页 | `OnUseCard()` | `OnUseCard()` | - |
| 投骰前 | `BeforeRollDice()` | `BeforeRollDice()` | `BeforeRollDice()` |
| 攻击成功 | `OnSucceedAttack()` | `OnSucceedAttack()` | `OnSucceedAttack()` |
| 拼点胜利 | `OnWinParrying()` | `OnWinParryingAtk()` | `OnWinParrying()` |
| 拼点失败 | `OnLoseParrying()` | `OnLoseParrying()` | `OnLoseParrying()` |
| 受伤后 | `AfterTakeDamage()` | - | - |
| 击杀 | `OnKill()` | - | - |

---

## 参考模组

- **寒昼事务所V4.0** - 位于 `../寒昼事务所V4.0/`
- **官方代码反编译** - 位于 `../SteriaBuild/Assembly-CSharp/`

---

## 版本信息

- 基于《废墟图书馆》游戏版本
- 参考寒昼事务所V4.0模组结构
- 使用BaseMod框架
