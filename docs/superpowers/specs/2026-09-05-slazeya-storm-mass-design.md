# 斯拉泽雅「倾覆万千之流」：敌方场地水风暴设计

日期：2026-09-05。阶段：研究／设计交付，尚未实现、运行 Unity 或部署。

本规格服从主线程的 [交付契约](../plans/2026-09-05-slazeya-storm-mass.md)。目标是一轮可实施、可用原生 Unity 代表帧评审的方案：**对方阵地的数股水流旋转收紧，形成低矮风暴；群攻结算时整体向外掀开，再断成水丝、泡沫和薄雾消散。**

## 1. 已读依据与现状根因

已读 `ModGuideDocs/README.md`、10 章特效入口／锚点／程序特效部分、11 章资源构建／运行时定位／预览部署／踩坑；`docs/superpowers/specs/2026-07-10-christasha-dawn-brushwork-design.md` 与 Round 8 规格相关约束；`SteriaBuild/设定/斯拉泽雅.md`；下列源码、邻近构建器与验证器。

| 源码证据 | 当前问题及本次处理 |
| --- | --- |
| `CardAbilities.cs:260–305` 的 `DiceCardSelfAbility_SlazeyaMassAttackTeamLightGain` | `OnUseCard` 额外创建施法者脚下 `OceanWaveEffectComponent`。删除此调用及专用辅助方法，保留流消耗记录、每 8 层威力加成和 `PrimalTidePowerScope`。 |
| `OceanWaveEffectComponent.cs:12–21,30–40,68–149` | 0.8 秒向外扩散，三组无纹理 Quad，固定半径 35，不能形成聚集—爆发层次。它不是新效果的视觉底座；入口移除后不必扩大范围删除旧类。 |
| `FarAreaEffect_Steria_OceanWave.cs:32–63,69–120,254–348` | 根节点在 `self.view.WorldPosition`；主要是相机朝向 Slash 粒子、莲花和大法阵。粒子旋转不等于绕中心公转，旋流剪影没有确定的几何约束。0.5 秒视觉爆发而 3.5 秒才释放 manager 门，伤害迟于爆点。 |
| `BehaviourAction_Steria_OceanWave.cs`；`SteriaModFolder/Data/CardInfo.xml:305–318` | 已有 `9002009`／`ActionScript="Steria_OceanWave"` 入口，保留注册和 XML。 |
| `SlazeyaAbilities.cs:20–49,344–363`；近期提交 `9757a8b` | 流强化已调整；9002009 属于斯拉泽雅第四种出牌模式。视觉改动不得改被动、流机制或技能伤害。 |
| `Assembly-CSharp/BattleFarAreaPlayManager.cs:476–625`；`FarAreaEffect.cs:20–38` | 默认 manager 已完整处理防御、伤害、死亡对话、UI、`OnEndFarAreaBehaviourAtk` 和空列表回调，无需另写群攻结算。 |
| `VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/Editor/{OceanBladeSlashUsableBundleBuilder,PlasmaLightningSlashBundleBuilder}.cs`；`Assets/Scripts/PlasmaLightningSlashDriver.cs`；`Assets/Shaders/PlasmaLightningSlashFlow.shader`；`VFXSource/PlasmaLightningSlash/verify_plasma_lightning_slash_source.ps1` | 可沿用原创 mesh／贴图／材质生成、独立 Bundle 构建回读、显式 shader phase、MPB、固定随机种子、原生 `ParticleSystem.Simulate` 与相机渲染的流程。不能把 Unity 工程专属 MonoBehaviour 序列化到 Bundle 后假定游戏能够运行。 |
| `7f2baee`、`43a39d0`、`d8c564d` 及既有晨光设计 | 近期效果修订反复强调连续轮廓、时间层次、贴体辉光和避免透明塑料壳。本次采用这些质量原则，不复制月牙造型，也不扩成同规模多轮重设计。 |

以上为源码诊断，未把类名里的“原神水系”或旧日志当成已验证的画面品质。本次研究没有生成或观看该技能的游戏实战帧。

## 2. 实际外部搜索与采用方式

2026-09-05 实际调用 Real Time VFX 公开搜索接口，查询 `tornado unity`、`anticipation dissipation`，再读取命中的主题正文。Google 返回跳转占位页，Bing 返回不相关结果，DuckDuckGo 要求验证码；这些结果不作为依据。已成功读取的原始来源如下，无需继续穷举。

| 原始来源 | 实际读到的依据 | 本次采用／边界 |
| --- | --- | --- |
| [Unity 2019.3：Velocity over Lifetime](https://docs.unity3d.com/2019.3/Documentation/Manual/PartSysVelOverLifeModule.html) | Orbital 控制绕轴公转，Radial 控制向心／离心运动，Offset 控制旋转中心；文档注明这些属性从 2018.1 加入。 | 微水滴以绕 Y 公转＋负径向速度聚集；爆发切换到正径向运动。纠正旧源码“旧版不能用 orbital”的注释；不需要 GPU VFX Graph。 |
| [Unity 2019.3：Renderer module](https://docs.unity3d.com/2019.3/Documentation/Manual/PartSysRendererModule.html) | Billboard 总朝相机；Horizontal Billboard 平行 XZ 地板；Stretch 按速度拉伸；Mesh 使用三维网格。 | 地面水纹用 XZ mesh，主旋流用确定的空间 ribbon，水丝用 Stretch、雾用 Billboard。仅旋转发射器并不能把 Billboard 地面纹理可靠铺平。 |
| [Gabriel Aguiar：Creating an awesome Tornado VFX with Unity Shader Graph](https://realtimevfx.com/t/creating-an-awesome-tornado-vfx-with-unity-shader-graph/9612)；[作者视频](https://youtu.be/Qyh9RPxeKcA) | 作者说明以 shader 制作龙卷风，另外使用脚本移动和吸引对象。 | 采用“形体／材质运动”和“场景行为”分责；只实现视觉聚集，不增加真实拉怪或单位位移。已读主题正文，未声称观看视频或核实全部节点；Shader Graph 工程不是 Built-in 可直接导入资产。 |
| [yosdevvfx：Unity Tornado VFX Tutorial (Vertex)](https://realtimevfx.com/t/unity-tornado-vfx-tutorial-vertex/27206)；[作者节点图](https://www.artstation.com/artwork/Dvn0P0) | 作者明确以 vertex offset 制作风暴，并提供节点图和教程。 | 支持用少量 mesh 及受控顶点运动建立风暴主体；本轮选更短的固定 ribbon mesh＋变换／UV 流动路径，不引入新的节点系统。 |
| [Catlike Coding：Texture Distortion](https://catlikecoding.com/unity/tutorials/flow/texture-distortion/) | 原文基于 Unity 2017.4.4f1：静止水材质会像玻璃／凝固物；UV 运动制造液体感；需要周期重置的 flow distortion 可用相差半周期的两次采样消除黑色脉冲。 | 必須有沿水带方向的 UV 滚动；首轮只做可平铺细流纹理＋小扰动。若采用会重置的 flow map，必须实现连续混合，不能整片周期熄灭。仅借数学方法，不照搬 Surface Shader 或贴图。 |
| [Wind Tornado VFX](https://realtimevfx.com/t/wind-tornado-vfx/15368) | 作者和回复特别讨论尾声旋转碎片的三维感及消散，并说明叶片使用背面剔除减少绘制负担。 | 水风暴主体消失后短暂保留同向旋转的少量水丝，建立体积退场；不直接复制其叶片或 GIF。已读讨论，未逐帧评审远程 GIF。 |
| [Need help with a tornado mesh](https://realtimevfx.com/t/need-help-with-a-tornado-mesh/14129) | 提问者的透明风暴 mesh 随视角出现接缝；回复指出过长窄三角形，并建议更均匀拓扑；作者确认修复。 | ribbon 沿弧足够细分，横向 3–4 排，避免极细长三角形和接缝补壳。 |
| [UE4 – Ice Projectile Magic](https://realtimevfx.com/t/ue4-ice-projectile-magic/18884) | 明确讨论 anticipation／climax／dissipation；反馈通过同一基色改变 HSV 的饱和度／亮度建立层次，指出过饱和问题。 | 仅借时序与水色层次原则，不能把 UE4 实现认作 Unity 2019 可用组件。 |
| [Unity 2019.3：ShaderLab Blending](https://docs.unity3d.com/2019.3/Documentation/Manual/SL-Blend.html) | Alpha blend 与 additive 的区别；凹形／互相覆盖透明面容易有排序问题；透明 shader 通常关闭深度写入。 | 水体 Alpha Blend，短时高光 Additive；减少重叠壳，保持 `ZWrite Off`，不采用多个透明圆锥叠色。 |
| [Unity 2019.3：AssetBundles](https://docs.unity3d.com/2019.3/Documentation/Manual/AssetBundlesIntro.html)；[ParticleSystem.Simulate](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/ParticleSystem.Simulate.html) | Bundle 是平台相关的 non-code 资源；`Simulate` 支持是否重启／递归／固定步长，模拟后暂停。 | 无自定义脚本 prefab＋Mod DLL 内共享驱动；预览复用驱动并真实模拟粒子，避免预览代理冒充运行时效果。 |

可复现搜索：[风暴](https://realtimevfx.com/search.json?q=tornado%20unity)、[聚集与消散](https://realtimevfx.com/search.json?q=anticipation%20dissipation)。所有参考只借制作方法；本次不复制、下载或部署外部美术资产。

## 3. 视觉冻结规格

### 大形、地面与尺度

主形是覆盖受方阵地的**宽底、低重心、开放螺旋水风暴**。侧上方观看时能看到倾斜水带绕场地旋转、上升、收紧；内部留孔和间隙，角色仍可辨认。禁止只用一张旋转法阵、满屏蓝雾、闭合玻璃圆锥或巨型莲花表示风暴。

`WorldPosition` 为世界坐标；XZ 是地面，Y 是高度。不要将 `UIPosition=(x,z/2)` 当世界坐标，也不要跟随施法者 `atkEffectRoot` 或角色翻转节点。

定位采用主线程冻结的受方集合：优先当前 `BattleFarAreaPlayManager.victims` 中有效且属于施法者对阵阵营的单位；不足以确定场地时用实际对阵阵营有效单位，回退 `self.currentDiceAction.target`。不写死 `Faction.Enemy` 或正／负 X。无任何有效受方时跳过可见资源，生命周期仍正常完成，不能回退到施法者脚下冒充敌方场地。

取得受方脚底 XZ 包围盒中心作为根位置，Y 取有效脚底高度中值并增加极小离地间隙。聚集开始后固定根位置和范围，避免目标受伤动作、死亡移除使风暴突然跳位或缩小。

用 `H` 表示代表性角色的可见身体世界高度，用 `Rx/Rz` 表示地面椭圆半轴。初始半轴取受方脚底包围盒半跨度加约 `0.6H` 留量；椭圆必须检查每个受方脚点的归一化半径，必要时统一扩大半轴以覆盖角落，不能只包住矩形的横纵中点。单目标回退约 `Rx=1.2H、Rz=0.8H`，多目标由实际站位决定。H 应取角色渲染尺寸／已验证的地图尺度，不能把 `ChangeScale` 的 S/M/L 值 `3/2.25/1.5` 直接当身体高度。

聚集主体高度约 `0.8–1.1H`，爆发最高水冠约 `1.4–1.7H`；尾部薄雾低于 `0.6H`。避免拉伸到画面顶部。地面纹理可以铺满受方范围，密实／高亮主体尽量围绕受方，不能侵入施法者一侧成为全屏覆盖。

### 最小层次：一主形、两种附属运动

| 层／固定节点 | 造型、材质与职责 |
| --- | --- |
| `AB_GroundRoot` | 1 个 XZ 软边环形面，深海蓝半透明，2–3 条不闭合水纹向内旋转。无符文。环内保留透明区，持续说明敌方场地位置。 |
| `AB_StormRoot` | 3 条空间上错开的开放弧带，错开角位、高度与半径，读作同一个上升旋涡。它们不是同一 mesh 等比复制的颜色壳。弧头稍宽，弧尾收细；纹理顺弧流动，前后段同向。开放端羽化。 |
| `AB_BurstRoot` | 聚集时完全隐藏。回调到达当帧出现阵地范围内的短时青白峰值；现有水带迅速掀开，配 1 个低矮扩张冲击环。爆发峰值不是第二场持续发光。 |
| `AB_ParticleRoot` | 最多 3 个系统：吸入的微水丝、向外上方喷溅的短水滴／水丝、低亮薄雾。喷溅可合并泡沫职责。粒子支撑主形，不能取代它。 |

工程起点约 5–6 个 MeshRenderer、3 个 ParticleSystem，总活粒子预算约 120–160；这是上限参考，不是必须凑够的数量。Mesh 每条约 48–64 个弧向分段、横向 3–4 排即可；不需要高密度体积模拟、碰撞或流体求解。

### 水色、材质、光照

水体基色深蓝 `#125579` → 海蓝 `#238CB0` → 浅青 `#76D9E5`，爆发及少量泡沫使用近白 `#E5FCFF`。这些是调色起点；实际按 Unity 颜色空间检查。维持蓝／青同一色族，低处较深、运动前沿较亮；不用紫黑核心或电弧来抢水风身份。

主体采用 Built-in Unlit ShaderLab，`Blend SrcAlpha OneMinusSrcAlpha`、`ZWrite Off`、正常深度测试；细高光可单独使用同 shader 的材质混合参数设置 `SrcAlpha/One`。主要 alpha 约 0.35–0.60，低亮外辉约 0.10–0.20。只在爆发 60–100ms 内显著提亮；白色不能覆盖整个主体。没有 Bloom 或场景灯光时，也必须读出蓝体与浅青前沿。光照层次来自色带与稀疏高光，不加实时灯、GrabPass、全屏折射或新的后处理依赖。

单条水带内部用 RGBA mask 分工：主体密度、顺流细纹、泡沫稀疏区、外轮廓羽化。细纹及小噪声只扰动内部，不把大形切成碎沙；不要用大块圆斑噪声制造塑料树脂感。流纹 UV 沿弧向移动，地面水纹采用连续角度周期，闭合处采样一致。使用显式 `_Phase` 和 dissolve／alpha 参数，由共享驱动写 MPB，不依赖材质 `_Time` 隐含推进。

排序遵循实际战斗角色层：地面纹在脚下；后半水带允许被角色遮住；前方水丝仅少量覆盖下身。不得给全部 renderer 强制同一高 sortingOrder，也不得靠 `ZTest Always` 掩盖地面定位错误。双面 ribbon 如需 `Cull Off` 只用单层面，不再添加前后两套透明壳。

## 4. 冻结时序：默认 manager 的聚集门＋爆发回调

**保留 `HasIndependentAction=false` 默认行为，不实现 `ActionPhase`，不自行调用 `GiveDamage`、`TakeDamage` 或重新筛选伤害对象。** 前一版独立 ActionPhase 建议已由主线程否决，不属于本规格。

| 相位 | 时间与视觉 | 游戏契约 |
| --- | --- | --- |
| 聚集前段 | Init 后 0–0.30s：低亮地面旋纹出现，外围水丝朝中心流入。 | `isRunning=true`，`_isDoneEffect=false`。 |
| 聚集成形 | 0.30–0.95s：三个弧带从后低到前高逐渐建立；旋转加快，半径约从 0.95R 收到 0.65R，保持开放中心。 | 累计视觉时长，不提前造成伤害或创建爆点。 |
| 压缩蓄势 | 0.95–1.20s：略收紧、前沿变亮；末段约 80ms 留出张力。 | 1.20s 后只置 `isRunning=false`，不结束、不销毁。manager 还有自身默认 1s pre-delay。 |
| 等待回调 | 门打开至实际 `GiveDamageFromManager`。维持压缩水带，phase 可继续缓慢流动，停止继续增强。 | 不按绝对秒数猜测伤害时间。Update 不能因 `!isRunning` 返回。 |
| 爆发 | 令实际回调为 `B=0`。B=0 当帧青白峰值和水冠启动；B=0.06–0.24s 径向迅速掀开，冲击环抵达约 1.05R，水丝向外、略向上喷出。 | 回调只 `TriggerBurst`；空受伤列表仍播放，重复回调由状态锁忽略。伤害仍由 manager 完整结算。 |
| 消散 | B=0.24–0.65s：主体依顺流方向破开、变窄，停止发射；B=0.65–1.05s：剩余水丝漂移、下降，地面残纹和薄雾最后消失。 | 尾声完成才 `_isDoneEffect=true` 并释放资源。预计总时长约 2.25s＋manager等待。 |

爆发全阵地同时成立；扩张环是命中后的运动延伸，不是重新按波前逐个结算伤害。若后续添加逐人小泡沫，只能按回调传入的受伤名单表现，不把所有覆盖者都标成受伤。本轮不必添加逐人系统。

消散必须包含形体变化：水带先顺流方向开缺、变细，剩余水滴继续移动，最后才透明归零。禁止静止整块一起淡出、突然缩成点、等到 3.5 秒硬销毁。异常取消可直接清理自己创建的 renderer／实例；不能改变战斗数值，不能留下相机滤镜或循环发射器。

## 5. 文件与资源契约：一轮实施路径

运行时修改严格限于 `CardAbilities.cs` 的上述视觉入口、`FarAreaEffect_Steria_OceanWave.cs` 的表现／生命周期；新增一个只依赖 `UnityEngine`、`System` 的共享视觉控制类，例如 `SteriaBuild/SlazeyaStormVisualController.cs`。`BehaviourAction`、卡牌 XML、`SlazeyaAbilities`、伤害／流逻辑原则上不动。

推荐继续使用现有 Unity 2019.3.15f1 工程，新增专属文件，不改其他 builder：

```text
SteriaBuild/VFXSource/SlazeyaStormMass/
  build_slazeya_storm_bundle.ps1
  verify_slazeya_storm_source.ps1
SteriaBuild/VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/
  Editor/SlazeyaStormMassBundleBuilder.cs
  Scripts/SlazeyaStormVisualController.cs  # 由 canonical runtime 源复制，构建校验 SHA-256
  Shaders/SlazeyaStormFlow.shader
  Textures/Generated/SlazeyaStormMass/...
  Materials/SlazeyaStormMass/...
  Meshes/SlazeyaStormMass/...
  Prefabs/SlazeyaStormMassPrefab.prefab
```

新增 bundle 名固定 `steria_slazeya_storm_mass`，prefab 名固定 `SlazeyaStormMassPrefab`，shader 名固定 `Steria/SlazeyaStormFlow`。只打包这个 prefab 及其资源依赖，不重建其他角色 Bundle。

Prefab 内仅 Transform、MeshFilter／MeshRenderer、ParticleSystem／Renderer 等原生组件，**不挂项目专用 MonoBehaviour**。主线程运行时加载并实例化后创建共享控制器；Builder 回读 Bundle 后用同一控制器播放。`Steria.csproj` 已排除 `VFXSource/**`，将 canonical 控制器放 `SteriaBuild/` 可随 Mod DLL 编译，Unity 的源镜像不会重复进入 DLL。

共享控制器的最小职责是绑定上述节点、接收显式 delta、推进聚集、响应一次 `TriggerBurst()`、推进爆发／消散、输出完成状态与释放自己持有的资源。建议普通 C# 类，由 FarAreaEffect 调用，不另设 MonoBehaviour 自走时钟。预览按固定步长调用同一接口，不能重新手写一套近似曲线。

粒子 `playOnAwake=false`，固定 seed；由共享驱动在正确相位启动。为了得到最直接的一致性证据，可让共享驱动显式 `Simulate(dt, withChildren:false, restart:false, fixedTimeStep:false)` 推進有限粒子系统；实际与预览都采用此路径，禁止同时自动 Play 和手工模拟形成双倍时间。单个粒子系统只被模拟一次，不递归重复模拟子系统。若实现选择正常运行时 Play，预览也必须按同一相位和参数模拟，并报告两路径差异。

共享工程有 `Assets/Editor/ChristashaDawnCoreOnlyAutoEnsure.cs` 的 `[InitializeOnLoad]` 自动重建钩子。这不是本任务修改范围：先记录启动前后相关文件哈希。如其会污染无关工作，使用独立临时 Unity 工程，仅复制所需 ProjectSettings、此次源文件和资源；不要修改／禁用其他 builder，更不要运行后回滚用户原有改动。此隔离是具体副作用出现时的备选，不要求先重建整个工具链。

构建顺序：同步共享源并验哈希 → Unity 构建专属 prefab → Bundle Windows64／LZ4 → 从 Bundle 回读并验证 → 对回读实例做原生预览 → 独立视觉评审 → 有关源验证、net472 Release 编译与主线程部署检查。含预览的 Unity 命令不带 `-nographics`；使用主线程已确认的 `C:/Program Files/Unity/Editor/Unity.exe`，后台启动，日志记录版本和输出路径。

构建验证关注：所有 mesh／材质／纹理引用有效、shader supported、无 Missing Script、相位节点齐全、回读实例与源 prefab 相符、粒子无无意循环。不要用“至少有几十层系统”作为品质门槛。

运行时加载遵循现有 Mod 下 `Resource/AssetBundle`／`Assemblies/AB`、无后缀／`.ab` 兼容路径，记录实际命中路径与版本。缺资源时记录失败并正常结束生命周期，不恢复旧的施法者脚下特效；缺 Bundle 的表现不算视觉通过。

## 6. 代表帧与拒收标准

预览采用侧上方正交相机：看得见 XZ 地面和 Y 向高度，俯角初值约 20–30°，投影后地面深度有明显压缩。放置标注高度 H 的简洁角色代理，显示 3–5 名受方与一名远离风暴的施法者；代理只解释比例与覆盖，不能替代粒子。相机／H／各脚底坐标写入预览 manifest。禁止只有俯视法阵或只有黑底孤立效果。

输出黑底和类战场暖灰地面两套 1280×720 帧，建议目录 `preview_exports/slazeya_storm_mass/`。每张都来自 Bundle 回读实例与共享控制器，使用真实 shader 和原生粒子渲染。

| 代表帧 | 必须看到 |
| --- | --- |
| `01_gather_early`：0.25s | 敌方脚下旋纹和向内水丝已经可辨，水冠未爆发，施法者脚下无副本。 |
| `02_gather_ready`：1.15s | 环绕场地的低矮立体风暴、收紧的方向和开放中心明确；至少一个角色可透过水带间隙读出。 |
| `03_burst_peak`：B+0.08s | 同一风暴向外掀开，青白前沿为瞬间峰值；群体覆盖明确，不是单个目标头顶小爆点。 |
| `04_dissipate`：B+0.65s | 主体已破开且明显变细，残余水丝继续漂移；不能与聚集帧只是亮度不同。 |
| `05_clear`：B+1.10s | 正常结束，地面没有持续环、发射器、白点或残留透明壳。 |

独立评审同时查看顺序帧，必要时看一段相同原生渲染的时间序列；对比原始意图和本规格返回 PASS／REVISE／BLOCK。明确拒收：来源在施法者；误用 XY 地面；一张盘旋法阵；封闭塑料锥／重复透明壳；UV 接缝或细长三角形闪烁；全白／全蓝遮满角色；聚集与爆发只改 alpha；噪声碎沙；爆发未等回调；尾声硬切；预览有自定义脚本而游戏无驱动。

功能验收由主线程按交付契约执行：身份互换仍在受方；单／多目标尺度合理；被防御的目标仍由默认 manager 判定；空受伤名单能爆发结束；重复回调不重复爆；不改流或威力；缺资源／取消能退出。Unity 代表帧证明视觉和资源路径，不等于实战验证。

## 7. 交付边界

本研究仅新增此报告，未改源码、Shader、素材、其他设计文件，未启动 Unity、未构建、未部署、未派生代理。C# LSP 因缺 .NET 10 无法初始化，已按任务授权使用 rg／直接源码读取，不修改开发环境。

实施与评审通过后，由主线程按已确认部署契约同步仓库 Mod、D 盘实际 Steam 安装及 C 盘现有 Mod 副本中的 `Steria.dll`、`steria_slazeya_storm_mass` 与 `.ab`，核对 SHA-256。报告只规定目标与检查，不声称同步已完成。
