# 薇莉亚 · 分层模型 R2

本目录保留最初的路径名，当前已更新到 R2：九种实际绘制的衣装姿势、统一坐标的头部母版、缩短的可见颈段，以及直接画进衣服的十字挂坠。2026-09-09 已同步到工作区模组与实际 D 盘游戏目录，部署文件 SHA-256 核对通过。

## 查看

- [动作总览](build/poses.png)：待机、防御、闪避、受击、斩击、突刺、打击、移动与跪姿施法。
- [版本与换头对比](build/comparison.png)：现游戏版 / R2 / 同衣装配原版头 / 衣装。
- [锚点检查](build/anchors.png)：蓝色颈根、红色站位、灰色地面线与绿色肩肘腕参考线。
- [交互预览](build/preview.html)：动作、换头、前后层、朝向与锚点开关；无挂坠独立开关。
- [model.json](model.json)：共享头部画布、各动作的颈口、地面、肩肘腕坐标与表情。

## 修正要点

**饰品只有作为单独单位、有逻辑绑定才应该独立。** 两个条件缺一不可；纯视觉装饰直接绘入所属头部或衣装。绘制时的临时图层不代表独立单位。薇莉亚当前十字挂坠作为衣装细节处理，保持无独立饰品槽和开关。

头部先整体设计，再拆到同一 512×610 画布。前后发、脸型和五官共用尺寸、挂点与变换，避免各自紧裁拉伸导致脸歪。母版颈根由 `(239,452)` 调到 `(248,395)`，去除下方多余颈部皮肤；五官相对位置没有移动，可见颈段约缩短 12 个输出像素。

**底盘规则：无特殊设计时，鞋底/实际支撑底部与默认站姿基线齐平。** 已测量原版 Bada 的 UI 默认动作与战斗默认动作，鞋底相对角色原点均约 `+0.06` 单位。`model.json/grounding` 将此作为目标，按每动作鞋底有效像素求运行时 Pivot，同时换算投影头部位置；不改人物 PNG、比例或五官关系。Default / Slash 的 `pivot_y=-274`，其余当前动作 `-276`（原先均为 `-268`）。

测量依据见 [Bada 基线](sources/grounding/bada_reference.json)，计算结果见 [逐动作落地参数](build/Velia_Layered_v1_Projection/grounding.json)。当前无特殊高度例外；未来浮空等设计须写明偏移和原因。对照截图时应统一选中状态，原版选中槽位本身有 1.05 倍放大。

衣装按 Modicos 参考的重心、支撑脚和弯肘规律补绘。长袍不再整件旋转冒充动作。十字挂坠已经烘焙进衣装，`wearing.accessories` 为空；前侧手臂作为衣装 `_front` 层遮挡头发和挂坠。

九个动作的链子与十字均逐张绘制进衣装，分别设计领口连接、胸前长度和倾斜；已移除共用吊坠 PNG 的粘贴逻辑。笔画坐标见 [pendant_strokes.json](sources/r2/pendant_strokes.json)，胸前检查见 [九张局部预览](review/pendants_all/chest_review.png)。防御、闪避与祈祷遵从手臂遮挡，不强行把十字移到手外。

大部件先用 Lanczos 预滤波，再以 3 倍尺寸装配后统一缩小，修复五官与发丝的采样碎点。normal / attack / damaged 使用配准后的不同表情；cast 使用闭眼层搭配平静的正常眉嘴，Special/S1 均为闭眼祈祷。肤色随母版绘制，当前没有局部改肤色开关。

## 来源与重建

规范与经验见 [Guide 12](../../../ModGuideDocs/12_战斗单位分层建模与核心书页投影.md)。当前源图位于 `sources/r2/raw/`；色键、拆层、短颈处理和挂坠合成见 [prepare_revision2.py](prepare_revision2.py)，原始坐标见 [provenance.json](sources/r2/provenance.json)，提示词见 [generation.md](sources/r2/generation.md)。

```powershell
# 仓库根目录，Python + Pillow；无生图服务或游戏运行依赖
python SteriaBuild/CharacterModels/Velia_Layered_v1/build_model.py
python SteriaBuild/CharacterModels/Velia_Layered_v1/verify_model.py

# 仅重新处理已保存的纯品红源图时运行，不重新生图
python SteriaBuild/CharacterModels/Velia_Layered_v1/prepare_revision2.py
```

普通重建只读取已保存源图和动作配方。旧导入脚本与旧源图保留为 R1 来源记录，不参与当前人物装配。被否定的首版预览与配方保存在 `review/rejected_r1/`，用于前后对照。

## 导出与验收范围

`build/assembled/` 为完整人物参考图。投影目录 `build/Velia_Layered_v1_Projection/` 有 12 个动作条目：9 种姿势，另有 Fire=Penetrate、Aim=Guard、S1=Special；每动作有主图和 `_front` 图，开启独立头部。

候选哈希与结果分别见 `build/build_manifest.json`、`build/verification.json`。校验涵盖共享头部配准、落地位置、表情、挂坠合入衣装、前层导出、换头隔离与 XML 坐标往返；这些是技术检查，不能代替用户的美术评价。

## 游戏部署

同一分层源生成两种运行时输出：

- **来宾 `Velia`：** 完整外观烘焙到动作图，关闭原生头部。原有敌方书页 4、5、6 继续使用此名称，显示当前批准的薇莉亚头部与动作；原来的技能效果继续独立运行。
- **司书 `Velia_Layered_v1_Projection`：** 仅导出衣装主层与前层，开启司书原生头部。书页 99000004 的外观指向已更新，其他书页字段不变。

制作端仍保留 head / wearing 分层。来宾此次采用完整动作烘焙的现有加载方式，没有新建 C# 头部注册逻辑。DLL、敌方书页数据和战斗书页数据未改动。

[最新部署报告与备份索引](deployments/20260908T193206051511Z/deployment.json) 包含底盘校准及 SP 闭眼祈祷修正，记录源候选、53 个发布文件及逐文件前后哈希；[首次部署记录](deployments/20260908T164801021024Z/deployment.json) 保留原始皮肤备份。实际目标为 `D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder`；C 盘 Steam 入口解析到同一目录，按真实路径去重写入后分别核验。

```powershell
# 从已通过校验的 build 生成运行时包；不部署
python SteriaBuild/CharacterModels/Velia_Layered_v1/deploy_to_game.py

# 备份后同步工作区模组和游戏目录
python SteriaBuild/CharacterModels/Velia_Layered_v1/deploy_to_game.py --game-mod "D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder"
```

部署时游戏未运行，下一次启动加载新资源。文件与加载配置已核验，尚未进入游戏做战斗实测。侧视图、身体肤色联动及实际游戏中的投影头部缩放仍是后续验收范围。浏览器本地文件策略曾阻止交互自动化，不将 HTML 生成或语法检查声称为交互实测通过。

当前装配实现共用 `../shared/layered_model.py`；原构建命令保持可用。共用工具迁移已核对原图像，只有 Special/S1 因闭眼要求变化。
