# 废墟图书馆模组制作操作手册

**从这里开始：** [00_一个优秀的废墟图书馆模组包含什么](00_一个优秀的废墟图书馆模组包含什么.md)。先了解基础层、优化层与卓越层的组成和完成标准，再按需要进入具体制作章节。

**默认验收方式：** 尽量使用模拟工程，给每次运行指定一个主日志文件并读取输出分析；仅为具体覆盖缺口补游戏内验证。详见[08_编译与部署](08_编译与部署.md)。

固定造型的战斗立绘逐动作独立绘制完整人物。第 12 章记录了常态克丽丝塔夏拼头造成的头颈脱节，并列出完整动作的绘制方法、原版实测比例和可换头衣装的导出方法。[战斗单位制作与投影](12_战斗单位分层建模与核心书页投影.md)

## 文档索引

| 文档 | 内容 |
|------|------|
| [00_一个优秀的废墟图书馆模组包含什么](00_一个优秀的废墟图书馆模组包含什么.md) | 基础规则与奖励、视听与故事、原版机制与异想体改造、粒子/镜头/高光/配乐，以及制作顺序和验收 |
| [01_模组基础结构](01_模组基础结构.md) | 文件夹结构、命名、中文全角标点与机器标记边界、必要引用 |
| [02_战斗书页完整制作](02_战斗书页完整制作.md) | CardInfo.xml、骰子、关键词高亮与说明、4:3 卡图/参考图/imagegen、群攻与效果实现 |
| [03_核心书页与被动系统](03_核心书页与被动系统.md) | EquipPage、被动类型、变幻莫测逐幕配牌与节奏安排 |
| [04_Buff系统与特效](04_Buff系统与特效.md) | 自定义/内置 Buff、获得与跨幕生效、半透明待生效状态、特效资源 |
| [05_异想体书页系统](05_异想体书页系统.md) | 异想体能力、E.G.O书页 |
| [06_敌人与关卡系统](06_敌人与关卡系统.md) | 敌人定义、关卡配置、接待与书籍图标、战前/战后/战中剧情、Boss战管理 |
| [07_完整示例与常见问题](07_完整示例与常见问题.md) | 完整角色制作流程、调试技巧 |
| [08_编译与部署](08_编译与部署.md) | 项目配置、编译、模拟工程优先验收与单次主日志、部署和发布 |
| [09_Harmony与BaseMod框架](09_Harmony与BaseMod框架.md) | Harmony补丁、BaseMod初始化、运行时代码修改 |
| [10_角色外观与特效系统](10_角色外观与特效系统.md) | 角色皮肤、动作素材、基础特效系统 |
| [11_UnityAssetBundle特效制作实战](11_UnityAssetBundle特效制作实战.md) | Unity AB 特效制作、参考方法、工具链、构建部署与踩坑 |
| [12_战斗单位分层建模与核心书页投影](12_战斗单位分层建模与核心书页投影.md) | 固定造型的逐动作绘制完整人物、拼头错误案例、原版比例与接地、司书换头和投影部署 |
| [13_贴图攻击特效制作流程](13_贴图攻击特效制作流程.md) | 多图阶段绘制、参考生图与透明清理、书页绑定、RGBA 时间线和可复现预览 |
| [14_粒子特效制作指南](14_粒子特效制作指南.md) | 原版粒子调用链、七类效果配方、克丽丝塔夏 V2、统一时钟、群攻接入与构建部署 |
| [15_书页动作与出刀节奏制作](15_书页动作与出刀节奏制作.md) | ActionScript、调整/出手/收势、双侧队列、PRE 时点、长—中—更长与回退验证 |
| [16_音效选材合成与书页应用](16_音效选材合成与书页应用.md) | 开放音效许可、试听选材、燃烧合成、书页音效时点与部署 |
| [17_程序集反编译与源码追踪](17_程序集反编译与源码追踪.md) | dnSpy、ILSpy、游戏程序集定位、方法调用链与 Harmony 签名核对 |
| [18_剧情对白与事件语音制作](18_剧情对白与事件语音制作.md) | 战前/战中/战后剧情、从单句录音到配音接入、开幕/击杀语音、同步与验收 |

---

## 快速参考

### XML文件对应关系

| 功能 | 定义文件 | 本地化文件 |
|------|----------|------------|
| 战斗书页 | `Data/CardInfo.xml` | `Localize/cn/BattlesCards/` |
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
| 骰子动作节奏 | `BehaviourActionBase` | `BehaviourAction_{ActionScript}`，XML 省略类名前缀 |
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

## 本仓库示例与源码

- **Steria 模组数据与资源** - `../SteriaBuild/SteriaModFolder/`
- **Steria 运行时代码与验证** - `../SteriaBuild/` 下的 C# 源码、`Verification/` 和 `VFXSource/`
- **游戏程序集反编译** - `../SteriaBuild/Assembly-CSharp/`
- **BaseMod 实现** - `../SteriaBuild/BaseMod/`

---

## 版本信息

- 基于《废墟图书馆》游戏版本
- 以 Steria 当前模组结构与已实现功能作为仓库示例
- 使用BaseMod框架
