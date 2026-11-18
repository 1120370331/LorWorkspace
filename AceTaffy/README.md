# 永雏塔菲：蒸汽来客

独立《废墟图书馆》BaseMod 接待模组，包 ID 为 `AceTaffy`。当前开发版包含自律发明接待。

## 内容
- 一场都市之星章节的单波接待：永雏塔菲，最多出战五名司书、一个楼层。来宾体力600、混乱抗性350，初始六颗速度骰。
- 来宾与司书使用独立被动配置。九张可获取战斗书页和来宾专用的“临场发明”；司书版仍保留原有属性与三个自定义被动、原版速战速决Ⅲ。
- 开局召唤500体力的蒸汽工坊：全抗性抵抗，不会混乱，不主动行动，可被摧毁。每次命中使塔菲恢复10点混乱抗性并增加1点发明进度，工坊存活时每幕另加1点；达到5点后准备1个随机机关，于下一幕开始时登场并参与该幕配牌。
- 每幕将两张0费临场发明安排到速度最低的两颗速度骰子，其余按可用光芒随机安排。临场发明默认攻击工坊，可被拦截，幸存骰子随后继续攻击工坊一次。工坊先于塔菲被击败时，立即取消未使用的发明书页、永久摧毁两颗速度骰子，取消待登场机关，并停止发明配牌与进度。
- 五种限定词与五类装置组成25种实际发明单位，体力与混乱抗性按上一版各组合最终数值降低20%（四舍五入）。各有独立外观、独特六项抗性，固定两颗速度骰；每种类型每幕使用两张专属书页。敌方共六个位置：左侧工坊、中间塔菲、右侧四个机关呈2×2四边形排列；在场与待登场机关合计最多四个。满场再完成发明时，所有友方下一幕各随机获得3层强壮、迅捷或守护，不再排队。
- 机关登场时及之后每幕开始时，为塔菲增加1层对应词条增益，每个机关的增益独立累积；机关死亡时移除该机关提供的全部增益层数。塔菲开局8层持续守护，每消灭一个己方发明减少1层；第8个后归零，第9个起每个增加1层持续易损。详细规则与复现见 `Verification/Reception/README.md`。
- 机关之间提供实时支援：机关装置使其他机关伤害+15%，连锁装置使混乱伤害+15%，毁灭装置使进攻型骰子最大值+1，能源装置使所有骰子威力+1；防御装置向其他所有友方提供2层持续守护。超级装置的支援翻倍，同类可叠加。连锁电弧、破甲爆裂、增压喷流改为远程书页。
- 战斗中文以游戏 resources.assets 的原版简体中文为参考，统一术语、触发时机及中文标点。
- 蒸汽压力、原版烟气兼容、自伤防护、第三颗攻击骰的每幕一次追加投掷。
- 九张玩家书页卡图、五张发明类型书页图、25套发明单位贴图、九个塔菲动作、可保留司书头部的衣装投影，以及蒸汽压力、结晶防护和31个发明／光环图标。塔菲战斗体型依据原版实测约三头身和偏窄躯干，头部直接衔接衣领。
- 战斗立绘包含常态、攻击闭嘴笑、受击三种表情，按动作自动切换。
- 4费爆轰首骰采用独立压锋、透明通道与短促分离压弧，喷口在不同掷骰倍率下保持稳定。
- 四种蒸汽攻击／防御表现；动作声音直接使用原版角色预制体的打击、突刺、防御等音效，由原版流程控制播放。
- 战前二十八句、战后十七句及战斗台词、书页故事。图书馆使用原版背景、原版安吉拉和罗兰；塔菲有独立正式立绘。独特直播间场景使用自绘图。塔菲的轻松台词使用“喵～”。

## 安装与游玩
1. 安装并启用 BaseMod（本机已安装创意工坊 `2603522001`）。
2. 将完整 `AceTaffyMod` 文件夹复制到实际游戏的 `LibraryOfRuina_Data/Mods/`。
3. 以带模组模式启动游戏，启用“永雏塔菲：蒸汽来客”。
4. 邀请条件为一本邀请价值6的书。原版无额外 BookValue 的都市之星书籍按章节值6参与匹配；接待列表的实际显示受已启用模组的邀请条件共同影响。
5. 击败来宾获得“永雏塔菲之书”。烧书奖池包含核心书页和全部九张战斗书页；沿用原版抽取规则，可能需要重复烧书，Unique核心书页最多一张。

本机部署位置：`D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\AceTaffyMod`。

## 规则约定
压力上限１０、幕末减１。状态栏分别显示烟气与压力层数。烟气上限１５只对拥有蒸汽心脏被动的单位生效。蒸汽回收只消耗３层普通烟气；不足时不消耗，也不获得使用时的抽牌与压力奖励。

第三颗攻击骰按普通攻击骰顺序计算，不把防御与反击算入序号。该骰子拼点胜利或单方面攻击命中时，若压力不少于六层，复式汽缸消耗三层压力，使用原生同骰追加攻击流程；两个触发入口共用每幕一次额度。过压的攻击威力加成覆盖攻击反击骰。结晶防护每幕首次自身书页扣血时生成一颗４～８反击格挡骰；原版自伤书页也可触发。爆轰为三颗骰子，使用时记录烟气的威力奖励。

初稿中的旧被动别名、章节冲突和骰子编号笔误的处理见 `IMPLEMENTATION_CONTRACT.md`。

## 开发与检查
需要 .NET Framework 4.7.2 开发工具、dotnet、Python/Pillow、游戏 Managed 程序集，以及仓库 `SteriaBuild/Libs` 中的 BaseMod/Harmony/游戏引用。

```powershell
python AceTaffy/normalize_ui_art.py
python AceTaffy/import_character_sheets.py --variant layered-native
python AceTaffy/Verification/Story/import_portraits.py
python AceTaffy/generate_invention_units.py
python AceTaffy/import_invention_units.py
./AceTaffy/build.ps1
dotnet build AceTaffy/Verification/MechanicsProbe.csproj -c Release '/p:AceTaffyManagedDir=D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Managed'
./AceTaffy/Verification/bin/Release/net472/MechanicsProbe.exe
```

`build.ps1` 暂存角色到 BaseMod 实际识别的 `Char/`，图标到根 `ArtWork/`，校验文件后才允许部署。游戏/BaseMod依赖 DLL 不打进发布包。

## 美术与验证材料
- `ArtSource/Cards/`：当前高分辨率卡图；`CardIterations/` 保留旧版与比例被否定的版本。
- `ArtSource/card-direction-v2.md`、`card-prompts-v2.md`：动作、镜头、背景、动线和表情设计及 image_gen 提示词。
- `ArtSource/Character/slender-v2/`：最终分层身体、三种头部表情与装配清单。`native-reference/` 保存原版模板实测；`import_character_sheets.py --variant layered-native` 可复现。
- `ArtSource/Story/`：剧情台本、独特场景源图及三套正式立绘。生成过但不采用的图书馆背景仅作为草稿留档，不装入当前包。
- `ArtSource/Audio/native-main.log`：当前原版动作音效的预制体、资源与构建核验记录。`ArtSource/Audio/v2/` 仅保留已停用的历史候选，运行时不再依赖它们。
- `Verification/card-contact-sheet.png`：当前九张卡图总览。
- `Verification/Vfx/unity-preview/`：真实 Unity 隔离场景中经原生特效工厂生成的事件／退场图与单一日志，使用模拟手部挂点，不是用户游戏录像。
- `Verification/face-states.png`、`neck-closeups.png`：表情和头领衔接检查。
- `Verification/Story/story-preview.png`：使用真实原版背景与人物资源的剧情组合预览。

原版测量、生成参考分工、分层装配、无裸颈拼接、表情绑定与复现步骤已详细写入 `../ModGuideDocs/12_战斗单位分层建模与核心书页投影.md`，其余相关经验也补入对应教程。

图像由内置 image_gen 按用户人物参考生成，源图已保存在工程内。最终验证范围与待验项目见 `DELIVERY.md`。
