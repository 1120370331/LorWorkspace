# 乐章骰子 UI：原版图标与颜色语义修复

日期：2026-09-07。阶段：只读研究设计，提交主线程审阅后实施。

最新修订：用户先纠正‘乐章骰子应只有一种统一的中心图标才对’，随后明确‘自己画一个，不用复用’。最终中心必须原创，废止下文历史研究中复用任何原版音乐 glyph 的方案。上一候选已经通过的原版骰框、布局、蓝灰色、卡色及状态修复继续保留；本轮只制作并统一原创中心图形。

## 用户意图与边界

用户：‘修一下乐章骰子用这个贴图，他太突兀了，还是用废墟图书馆自带的icon来改一个出来，以及相关卡颜色的显示也是逻辑很乱的，不美观。’

成功标准：以原版骰框为视觉基础，所有乐章骰使用同一个中心图标，乐章识别清楚但视觉权重接近普通骰子；整卡稀有度、EGO、高亮和禁用状态保持已经修复的原版一致逻辑；战斗/卡组/详情/动作骰/伤害图标的乐章中心一致。保留游戏原有行距和骰子顺序。

本任务只修 UI，不改伤害倍率、威力、拼点、反击触发、XML 骰种、卡牌稀有度或卡面立绘。尤其不要为了 UI 判定去修改共享战斗判定 `IsMusicDiceBehaviour`。反击攻击骰的当前战斗判定与注释冲突记录在风险里，机制修正另行授权。

## 已读依据

- `ModGuideDocs/README.md`；`02_战斗书页完整制作.md` 的稀有度、Type、Detail、专属页；`09_Harmony与BaseMod框架.md` 的 Postfix/UI；`10_角色外观与特效系统.md` 的 Sprite/透明边距/尺寸规则。
- `SteriaBuild/SteriaModFolder/README.md`；`docs/superpowers/specs`、`plans` 现有视觉设计/交付组织（没有乐章 UI 设计文档）。
- `MusicDiceSystem.cs` 全部 UI 工厂及样式；`HarmonyPatches.cs` 乐章 UI/伤害图标补丁；`PhantomDreamCardVisuals.cs` 独立卡页装饰入口。
- 原版反编译 `BattleDiceCardUI.cs`、`BattleDiceCard_BehaviourDescUI.cs`、`BattleSimpleActionUI_Dice.cs`、`BattleDiceBehaviourUI.cs`、`DamageTextEffect.cs`、`UI/UIOriginCardSlot.cs`、`UI/UIDetailCardDescSlot.cs`、`UI/UISpriteDataManager.cs`、`UI/UIColorManager.cs`。
- 近期提交总体为风暴/潮雾特效；相关历史为 `c24fbd0`（完善乐章骰子显示与流倍率）、`9757a8b`、`f878d33`。读过现有 `工具/generate_music_dice_template.py`，它只生成旧圆角面板/软光，不是原版 UI 验证器。仓库已有 VFX 预览不覆盖本 UI。
- 实际查看 `Resource/ArtWork/music_dice_die_icon.png`：透明底，青蓝立体六边宝石、四分旋涡、粗白发光轮廓。
- 最初查看的 `output/music-dice-ui/native-sprites/BehaviourDetail_{Slash,Hit,Guard}.png` 属原版另一处旧资源，不能作为当前卡页的精确素材依据。主线程随后沿真实 `level2:77176` 的 `UISpriteDataManager` 序列化字段解析 `_cardBehaviourDetailIcons` 与 `CardStandbyBehaviourDetailIcons`，导出 `CardAttack_{index}_{name}.png` 和 `CardStandby_{index}_{name}.png`，回执为 `output/music-dice-ui/card-icon-provenance.json`。已实际查看当前普通 Slash `CardAttack_0_AfterIcon_0_8.png`（fileID 2 / pathID 20203）与反击 Slash `CardStandby_0_AfterIcon_3_10.png`（fileID 2 / pathID 24361）：两者均为六边框＋白动作线稿，带原生轻笔刷外缘、内侧暗部及光泽；普通为橙红，反击为金黄。应精确保留这套原生结构，仅转有色部分，不能误把原生光泽当作应删除的自定义装饰。

原版是用户指定权威，现有原版源码已能解释关键问题，不需要泛搜外部同类设计。

## 根因诊断

1. **图标语言改变。** 旧 `MusicDiceSpriteFactory` 优先从运行 DLL 上级的 `Resource/ArtWork`（回退 DLL 同级）读取 `music_dice_die_icon.png`；旧 `EnsureBookDiceBackdrop` 把它添加为 sibling Image，隐藏原版斩/突/打图标。额外宝石的旋涡雕饰、厚重体积和大面积饱和渐变与当前原版六边框白线稿不协调。原版自身的细框、轻光泽、笔刷边应保留。所有乐章骰合并为一个中心符号本身符合用户意图，之前将三种中心保留是设计错误，已由本轮修订纠正。
2. **整卡边框被当成每颗骰子的高光。** 原版 `UIOriginCardSlot.img_linearDodge` 是 `SetLinearDodgeColor` 遍历的整卡稀有度/高亮框数组（源码约 145-166 行），不是骰子图标数组。当前 `ApplyOnOriginSlot` 用骰子下标对齐它，禁用若干框段。原版 `BattleDiceCardUI.img_linearDodges` 同样由整卡 `SetLinearDodgeColor` 管理（约 751-763 行），当前 `ApplyBattleDiceCardFaceStrip` 将部分条段设白。卡牌含几颗乐章骰便可能有几段框被破坏，是‘逻辑很乱’的直接根因。
3. **布局补偿造成混合骰错位。** 自定义图标用 `BookDieIconScale`、0.75 卡面缩放、0.62 向乐章骰中心收拢；普通 Guard 仍在原位。混合骰的间距/队列关系因此失真。
4. **颜色语义被重置。** 战斗描述行把 `txt_ability` 和 `txt_range` 全部设白，覆盖原版 `Rwbp` 文本语义和骰种点数色；卡组详情却未统一点数色。清理仅强制白色/HSV 1，不保存原先状态，可能覆盖刚由原版更新的稀有度、目标抗性、交互状态。不能把‘白色’等同于‘恢复原版’。
5. **动作 UI 以整卡判定。** 两个 `PrepareDice` Postfix 只检查 cardOfBehaviour，`汐音：海之还愿`（9008006）末尾两颗 Guard 也会被染蓝。`PrepareDice(BattleDiceBehavior)` 原版存在但未覆盖。应以该次参数的真实骰子 Type/Detail 判定，不能靠卡页整体或图标名。
6. **静态 XML 对齐运行时列表。** 当前 BattleDiceCardModel 的 `TryGetDiceBehaviourAt` 取 XmlData.DiceBehaviourList；原版 UI 使用 `GetBehaviourList()`。`汐音：光耀万海`（9009006）动态复制骰后，新增行可能丢样式。显式 SetBehaviourInfo 参数优先，其次与原版相同的运行时列表。
7. **伤害图标另有入口。** `ApplyOnDamageText` 也使用宝石；只修卡片会留下战斗伤害时的突兀图形。`DamageTextEffect.Update` 会把 bg 颜色复制给主图标并更新 alpha，因此只改主 Image.color 会在淡出时被覆盖。

## 冻结设计合同

### 原版图标

- 骰框来源：`UISpriteDataManager.instance._cardBehaviourDetailIcons` 的当前原版 `AfterIcon_*` 资源。所有乐章攻击骰固定一个共同框基底与一个共同中心，不再按 Slash/Penetrate/Hit 选择不同中心。防御和反击继续保留原版资源，包括 `CardStandbyBehaviourDetailIcons`。
- 改造方式：保留已经接受的蓝灰原生六边框、轻光泽、笔刷边、alpha、暗部明度、尺寸、pivot、裁切和纹理；清除旧攻击中心，仅放一个共同的原创白色音乐中心。中心不能复用原版音乐 Buf、字体、Emoji 或现成图标，具体笔形见下方‘统一中心补充合同’。不用另一处旧 `BehaviourDetail_*` 素材替代当前骰框，不添加圆角底板、额外立体宝石、环形光圈或第二套 UI 边框。
- 已确认直接乘 Image.color 不可用。优先缓存由原版 sprite 像素派生的色相变体：对有饱和度的像素转色，纯白/近白线稿保留原色，源 alpha 与抗锯齿边界保持；以原版 sprite 为缓存键。不需要外部 PNG，不修改共享 sprite/material。若实现者选择原版 `RefineHsv` / `_2dxFX_HSV`，须以实例化材质隔离并证实白线和原有状态不受影响。不可把已处理图作为下一轮输入累积调色。
- 动作骰继续使用 `BattleSimpleActionUI_Dice.GetFaceSprite` 已配置的原版多面骰及边缘材质；不替换骰面几何，`imgIcon` 与 `imgDetailIcon_Center` 的乐章中心均用同一音乐符号。
- 伤害入口的乐章中心也复用同一个音乐符号，不再使用动作类别中心或旧宝石 PNG；保持数字、HP/混乱伤害颜色、现有原版外围状态及淡出节奏。
- 旧 `music_dice_die_icon/face/glow` 文件即使仍在部署目录，也不能再影响该路径；避免仅靠删文件让旧程序回退生成圆角面板。

### 色彩分工

| 信息 | 控制范围 | 规则 |
| --- | --- | --- |
| 书页稀有度、EGO | 原版 frame、linearDodge、cost、range 系统 | 完全由 UIColorManager 和原版状态方法负责，绝不索引/染白/隐藏整卡框数组 |
| Hover/selected/disabled | 原版状态和原版 graphic 集合 | 保留优先级，乐章 refresh 不重新亮起被禁用/置灰的控件 |
| 乐章类型 | 本颗普通攻击骰的图标和点数范围；动作骰自身 | 默认蓝灰 `#8FAFC4` (143,175,196)，亮边 `#C9D8E1` (201,216,225)，单一色系；不要叠蓝渐变面板 |
| 防御、闪避、反击 | 原版该骰图标和点数范围 | 完全原版，含乐章卡上的 Standby 攻击骰 |
| 能力正文和关键词 | txt_ability | 保留原版 Rwbp 和富文本局部颜色，不统一染白或染蓝 |
| 点数变化/胜败/破碎 | 原版动作状态处理 | 保留 Normal/Increase/Decrease、动画和 alpha；仅乐章默认基色参与，不在 refresh 强制回 Normal |

颜色为设计目标，不硬编码其他原版稀有度色值（原版值由序列化配置决定）。若实际原版 shader/色样需要微调，可报告技术差异并在预览复核后微调此两色；不扩大为重新设计卡牌配色。

### 判定与状态恢复

- 新建或局部限定 **UI 专用**判定：卡有 SteriaMusicDice 且 `Type == Atk` 且 Detail 属 Slash/Penetrate/Hit。`IsMusicDiceBehaviour` 战斗逻辑保持原状。不得静默修改反击的战斗规则。
- 每次数据绑定前恢复上一次由本模块修改的字段，再执行原版绑定，最后按新数据施加当前样式；清理作用域仅为本模块拥有的 icon/文本字段。不要在原版 Postfix 后用旧快照覆盖新原版状态。
- 对刷新/复用要幂等；不添加与删除装饰层，不重复添加 HSV、不改变共享材质。非乐章回收、隐藏描述及空列表都可恢复；null 或失败 PrepareDice 不拿旧 cardOfBehaviour 误上色。
- 不遍历无关子树后统一白色；不要猜 img_* 字段名字并做数组配对。已知原版字段就明确取已知字段。
- `SetPreviewResist` 本次保持显示合同：目标抗性状态不得被简单样式刷新清零；若乐章固定抗性需要更正文字，是独立的信息准确性工作，不能顺手改战斗规则。本次预览要检查选目标前后没有残色/明度突变。

## 实施顺序与责任界面

1. 按现有 verifiers 风格补最小静态保护，约束不再从 card frame 数组推导骰高光、不再读取宝石路径、使用运行时行/显式行为参数。具体运行 UI fixture 由主线程准备并验收。
2. 实现者仅修改 `MusicDiceSystem.cs` 的 UI 工厂/样式与必要 UI 专用 helper，以及 `HarmonyPatches.cs` 的本组 UI 补丁（包括伤害图标现有入口所需最小参数传递）。排除所有拼点/伤害/威力规则、独立专属书页补丁、Slazeya/Velia VFX、卡牌 XML。
3. 替换旧 backdrop/suppression/compactness 流程；删去失去调用的旧 UI 程序化贴图/反射猜字段方法，保留无关代码。
4. 修动作骰三个重载的实际参数判定、战斗描述动态行、卡组/详情点数一致和精确状态恢复。
5. 构建并交给主线程运行代表 UI 路径，独立预览 reviewer 对比原版与候选。不得用只绘图标的手工示意声称真实卡牌交互已通过。

## 预览和验收矩阵

| 场景 | 数据/操作 | 必须看到 |
| --- | --- | --- |
| 稀有度对照 | Rare `9008005 海愿斩`、Unique `9007006 汐音·夜痕`，与同稀有度普通页并排 | 框、费用、框亮边同原版，无缺段/白段；仅骰图标/范围体现乐章 |
| 混合防御 | `9008006 汐音：海之还愿` 卡面/详情/逐颗动作 | 前三颗蓝灰原版攻击轮廓，末两颗 Guard 完全原版；等间距，顺序不变 |
| 混合反击 | `9009007 汐火联星` EGO | 前两颗 Atk 为乐章，最后 Standby/Slash 保留原版反击图案与范围色；EGO 框仍原版 |
| 动态骰 | `9009006 汐音：光耀万海` 复制后显示 | 新增真实攻击骰也有样式，行数/图标数匹配运行时列表 |
| 乐章唯一中心 | `9007006` 的突刺/斩击及 `9009007` 的打击 | 三者中心完全相同，均为自己绘制的白色连音双音符；保留已验收的原版骰框和透明边界 |
| 状态 | 卡组 hover/退出 hover、手牌费用不足/恢复、选目标/退出、连续刷新 | 高亮/禁用清楚；不会由刷新恢复亮白框或变亮 icon |
| 复用 | 同一 UI 乐章→普通→乐章、同帧重复刷新；隐藏行 | sprite/color/enabled/HSV 正确，普通卡没有残色/隐藏；无额外装饰对象累积 |
| 动作与伤害 | 一个攻击结果从 prepare→roll→impact→fade；随后普通骰 | 使用原版骰面/图标，无宝石；范围与默认值色一致，增强/削弱/淡出保留 |

预览材料：以真实原版资源构建的 Unity 2019 fixture 或实际游戏截图。至少一张 9008006 的卡面＋详情混合对照、一张 Rare/Unique/EGO 的普通/高亮/禁用对照，以及动作/伤害 early/mid/fade。fixture 要调用候选真实代码/原版绑定方法或等价的数据合同；无法覆盖的真实游戏交互明确标记，不以拼图代替。

拒绝条件：中心复用原版音乐图标、字体、Emoji 或现成图标；乐章仍按攻击类型显示多个中心；不同 UI 表面的乐章中心不同；新中心与旧 Slash/Hit/Penetrate 叠在一起；24..32 px 下音杆/连梁断裂或两音头不可辨；出现额外自定义立体宝石或圆角背板；删除当前原版骰框自身的六边框、光泽或笔刷边；使用未由当前 UI 管理器引用的旧资源替代骰框；整卡框被乐章样式修改；混合骰错位；Guard/Standby 被染乐章色；EGO/稀有度语义丢失；原版正文富文本颜色被统一重写；动态骰漏样式；复用后普通卡残色；无实际候选代码或原版骰框资源的预览。

## 统一中心补充合同（本轮唯一实施范围）

- **唯一中心锁定为自己绘制的白色连音双音符。** `Orchestra_Enthusiastic`、`SingingMachine_Rhythm` 等原版候选只保留为已放弃的研究历史，本轮禁止读取、描摹或复用这些中心；不再搜索资源。不能调用字体、Emoji、图标库或现成音乐 glyph。使用仓库自有 SVG 路径，或代码定义多边形/Bezier 路径，超采样生成透明 sprite。
- **可执行笔形规格：** 以 100×100 的中心-only 设计坐标绘制，内容边界约 x=13..87、y=10..91。两枚白色实心斜椭圆音头，左中心约 (29,78)，大小约 29×19，向右上倾斜 20°；右中心约 (73,67)，大小约 26×18，向右上倾斜 15°。两个音头不能机械复制同一形状。左音杆由约 (39,77) 向 (40,27) 上伸，右音杆由约 (82,65) 向 (83,15) 上伸；主体宽约 7..9，轻微弧度，顶端略收尖。用一条从 (40,27) 向 (83,15) 右上扬的短连梁连接，厚约 8..10，接点重叠避免细缝。整体仅两音头、两音杆、一连梁，轻微不规则轮廓控制在设计单位 1..2 内，不能加噪点、飞白碎屑或细小装饰。
- **栅格与小尺寸规则：** 建议 512×512 或更高超采样后下采样为 128×128 的 RGBA 中心-only 源；白色主体 RGB 固定白，仅 alpha 表示抗锯齿，不加彩色阴影、光圈、渐变、徽章或底板。在实际 24、28、32 px 显示时，两枚音头与连梁必须可辨，主杆有效宽不少于约 2 px；若缩小后断裂，局部加粗笔形，不能靠 glow 补可读性。上述坐标是原创构图指导，不引用任何现成 glyph 路径。
- **卡页必须合成一次后缓存，不能直接在现有完整攻击图标上覆盖音符。** 固定当前原版 `CardAttack_0_AfterIcon_0_8` 为已接受骰框基底；其 Slash 与框已烘焙为一张图，先在内框限定区域提取白色 Slash 主连通形体和抗锯齿边界，用该区域周围的有色底层重建被覆盖部分，再按现有蓝灰变换处理并合成统一音符。框外 alpha、笔刷外缘、框线和原生光泽不得参与中心清除。合成输出不得保留旧 Slash 白边、尖端或阴影残迹，完整卡页图标的结果对所有乐章攻击骰相同。
- 已查看另一原版无攻击中心资源 `Dice_6.png`：白色六边骰框与内部结构线；可作为明确的空骰框参考，但本轮默认不替换已接受的当前卡页框，避免重做其光泽与色彩。如果实现者发现当前框中心无法干净分离，应先报告具体预览缺陷，主线程可批准使用精确原版空骰框；不自行换成程序绘制的新框。`Dice_LinearDodge_6.png` 是配套的高光面，不是中心符号，不单独采用作背板。
- 中心为原创白色实心笔形；使用原版攻击中心可用区域，视觉宽高不超过内框的约 60%，不得碰框。保留现有已通过的图标整体位置、尺寸、帧光泽和蓝灰色，不重做布局/配色。
- 卡组卡面、战斗卡面、两种详情、动态新增乐章骰、动作侧图标、动作中心、伤害图标的音乐中心形状必须一致；动作的两个中心都使用同一原创透明 glyph-only sprite，卡面用同一符号与固定原版骰框清除旧 Slash 后合成。不能把整张带框卡面图标再塞进动作骰造成套框。
- **伤害三层明确规则：** 主线程批准 `img_resistIcon`、`img_resistIconBg`、`img_resistIconFg` 三层均换为同一个原创透明 glyph-only sprite；仅替换 sprite，保留每层本来的 enabled、color、material 与 alpha 和原有淡出更新。禁止为了表现音符擅自开启隐藏层、重置颜色或加第四层。
- 验证只补唯一中心合同：Slash/Hit/Penetrate 的乐章中心一致；所有入口同一符号；Guard/Standby 保持原状；乐章→普通复用恢复原始中心。复用已经通过且不受影响的卡色/布局检查证据，重跑受中心替换影响的 fixture 与代表预览即可。

## 明确风险

- 原版图标已导出并检查，序列化整卡色值仍由原版 UIColorManager 负责；不能从纯图标像素推断稀有度配置。
- 当前 `IsMusicDiceBehaviour` 及 `IsMusicAttackDiceBehaviour` 只检查 Detail，反击 Slash 也返回 true；注释却说反击不属于乐章。UI 按上述明确 Type 规则修正，战斗规则保持不动并在交付注明差异，不能借视觉修复改机制。
- 原版 UI 库有 HSV 状态机；直接乘色可能受旧颜色影响。必须用原版源 sprite/实例材质与精确恢复，预览真实结果后确认。
- 当前 worktree 有独立书页修复和 VFX 并发改动；实现者不得覆盖或回滚它们。这个文档是本研究阶段唯一写入文件。
