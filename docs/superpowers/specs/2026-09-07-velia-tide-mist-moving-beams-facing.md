# 潮雾晨光：移动光束与施放朝向

2026-09-07，**主线程已批准并冻结本提案，参数以本文及本次批准为准**。本次仅更新此状态行；不修改参数、不继续研究。实施由主线程派发。

## 意图与保留项

用户：“我觉得需要光束移动效果；并且，作为友方单位释放时，应该反转释放到敌人区域。”本次明确新增光束几何运动，覆盖历史“只减慢遮光、不新增扫动”的限制。

基线 `c9c5dd6` / first-r8。保留原0–.18s亮度曲线、首骰.72s/次骰.77s、.18–.48s保持、.14s局部命中、.40s最终退出、.32s入场、云图与材质色彩/亮度层次。既有云体与阴影的入场后.65倍漂移保持；不增加贴图或散射层，不延长manager等待。

已复用并检查当前模块README、controller/host、提交中的shader、r8交付和205帧预览协议。沿用前方案NVIDIA云遮蔽散射依据，无需新外部搜索。当前beam的三组斜率是常量，云影微动不能满足新需求；本次必须直接改变光束中心线角度。

## 光束运动提案

太阳仍固定于特效规范坐标(.515,.70)，三束从同一光隙发出；不转动云层或整体画面。保持原beam宽度/spread/权重(.95/1/.72)。新增有界正弦角度漂移，整张卡连续，不在BeginDice或回调时重置。

```text
t = _State.z                    // 已有单次推进的整卡Elapsed，不用_PulseAge
theta0 = atan(baseSlope * _Aspect)
theta = theta0 + radians(amplitudeDegrees) * sin(2*pi*t/3.6 + phase)
slope = tan(theta) / _Aspect
beam(fxUV, sun, slope, 原width, 原spread)
```

角度用实际屏幕比例计算；不要把UV斜率直接当屏幕角度。无sawtooth、frac时间跳变或逐骰随机重抽。三束同周期、小相位差、不同幅度，缓慢往返且互相保持顺序；运动始终存在，亮度仍由原r8曲线控制。无需修改controller时钟或新增动画状态。

| 束 | 原baseSlope | 摆幅 | 相位（弧度） | 16:9下斜率范围 |
| --- | --- | --- | --- | --- |
| 内束 | .11 | ±10° | -1.68 | .0105–.2167 |
| 中束 | .41 | ±7.5° | -1.62 | .3065–.5354 |
| 外束 | .73 | ±5.3° | -1.55 | .6050–.8892 |

三束都保持朝向目标半场；参数避免穿越和交换顺序。云隙y=.54处三束中心均仍落在已知规范开口附近，必须原生确认遮挡后至少两束持续可读、有暗隙，而非亮成一张扇形白片。运动过程中遮光按当前位置重新采样，可自然遮断；不能为保证可见而绕开cloudShadow。

解析候选在1280×720、viewport y=.25处：第一保持区间elapsed .78→1.08s，三束中心分别移动约30/34/42px；第二保持区间2.13→2.43s，分别约22/28/37px，方向反向。这是公式计算值，**不是已通过的原生视觉测量**。目标为每个.30s保持段能观察到约20–50px的真实光束位移，非亮度变化或云纹理错觉。遮挡/保护使可见峰偏移时，优先检查同一束完整连续帧，不能只用整体发光重心证明移动。

## 主线程确认的朝向合同

游戏依据（主线程检索，不重复调查）：`BattleUnitModel.formationCellPos`166–176同时使用Enemy负号和 `AllyFormationDirection==LEFT`负号；`StageController`395–413公开方向，常规Invitation为RIGHT，其他场景可能LEFT，默认RIGHT见4373。因此不能写死Player永远镜像。

```text
mirrored = (self.faction == Faction.Player)
           XOR (stage != null && stage.AllyFormationDirection == Direction.LEFT)
```

stage不可用时使用正常RIGHT约定。在host创建visual后、首次渲染前设置，**每张卡/会话缓存一次**，骰间复用不改方向；不能随受击列表、人物动作或临时站位来回翻转。

| 施放阵营 | AllyFormationDirection | IsMirrored | 照射方向 |
| --- | --- | --- | --- |
| Enemy | RIGHT或stage空 | false | 朝右侧Player区域 |
| Player | RIGHT或stage空 | true | 朝左侧Enemy区域 |
| Enemy | LEFT | true | 朝左侧Player区域 |
| Player | LEFT | false | 朝右侧Enemy区域 |

纯Unity controller最小新增：`SetMirrored(bool)`、只读`IsMirrored`，默认false；`Apply`传 `_MirrorX`为0/1。shader属性默认0。Faction/StageController/Direction引用只在 `FarAreaEffect_Steria_VeliaTideMist` host，controller/filter不得依赖游戏类型。现有会话复用、取消、缺资源、相机独占资源规则不变。

## 坐标隔离与采样一致性

shader明确区分三种坐标，不在顶点阶段镜像全屏UV：

| 坐标 | 用途 | 是否镜像 |
| --- | --- | --- |
| `sourceUV` | `_MainTex`原始相机RGB/alpha及现有上下方向修正 | 从不 |
| `screenUV=viewportUV` | 人物保护rect、局部成功hit、HUD保护、朝向探针 | 从不 |
| `fxUV=(lerp(screenUV.x,1-screenUV.x,_MirrorX),screenUV.y)` | 云图/薄雾、太阳/光晕、动态beam、cloudShadow路径、云面光场 | 仅此处一次 |

原图采样及局部hit loop必须继续使用原坐标，不能因复用变量`uv`而误反转。投影生成的 `_HitPoints`/`_ProtectionRects`保持原屏幕值，不反转、不重排。效果的云alpha/光强在fxUV求值后，再乘screenUV上的保护权重。

太阳在规范fx坐标固定；镜像时其屏幕位置自然变为(.485,.70)。所有cloudBank、cloudShadow采样点都留在同一fx域、读取同一.65云时钟；不能在内部再次镜像。beam角度运动仅改变光束mask，透射继续沿该fx像素到太阳的路径采样，因此云体/光源/阴影一致。不要镜像后再手动取负beam slope造成双翻转。

## 文件与实施边界

- `SteriaBuild/VeliaTideMistVisualController.cs`：仅镜像属性/方法与uniform写入；r8所有时间与事件曲线不变。Unity镜像沿既有构建流程复制。
- `SteriaBuild/FarAreaEffect_Steria_VeliaTideMist.cs`：只增加一次朝向解析/种子设置，按上表，game引用止于host；不改结算与清理协议。
- `SteriaBuild/VFXSource/VeliaTideMist/UnityProject/Assets/Shaders/VeliaTideMist.shader`：动态角度函数、_MirrorX与坐标拆分；原纹理、强度、光影层次保持。
- 同项目builder与模块预览/验证：分别导出有明确标签的Enemy正常方向和Player镜像方向；保持205帧/frame36与117回调/Begin99/frame200完成。构造角色世界位置和投影输入，不能水平翻转已渲染源画面来伪造测试。
- 常规预览Enemy施放者在左、Player目标在右；Player施放者在右、Enemy目标在左。两方向均保留非对称舞台、可读数字及原始TOP LEFT/BOTTOM RIGHT探针，导出两个独立视频/时序证据，保留旧r8输出。
- 主线程拥有原生验收fixture的Direction/StageController最小替身及四组合检查、游戏实际方向验证；实施owner不改主线程验收栈。新DLL和Bundle的配套最终构建/部署由主线程负责。

## 预览与接口PASS条件

1. **真实运动**：两方向各观看连续60fps保持段，至少两束有可追踪移动及暗隙；首/次保持段20–50px目标如上。无每骰角度重置、相位突跳、闪烁、互相穿越或探照灯式急甩。回头点速度平滑；不通过增强亮度代替运动。
2. **正确朝向**：四个阵营/队形组合的host初始化值满足表格。两组主要原生视频明确施放者、目标及方向，照射主体朝对手区域。镜像时云开口、太阳、beam与遮光整体对齐。
3. **原画面不翻转**：HUD/数字/人物方向和非对称舞台探针仍正常，局部成功光痕落在原投影目标，空成功列表不造假hit；人物保护仍贴实际人物，无镜像错位或挖洞。源画面alpha和UV上下修正保持。
4. **时间不退化**：精确检查r8起亮、+.18–.48保持值、.72/.77归零、hit .14、Fade .40；连续BeginDice不重置Elapsed/镜像/beam相位。暂停与重复render不动，恢复后连续；正常与取消后恢复原画面/保留其他滤镜，缺资源不阻塞。
5. **外观保持**：新旧云图/atlas SHA和导入设置一致，冷云腹/暖白日心保持；光束移动经过人物时仍可读，新增近白<=15%，没有新增噪声、贴图接缝、颜色涂抹或过亮合束。

独立reviewer分别对Enemy与Player原生视频/代表帧给PASS/REVISE/BLOCK。主线程批准前本文运动参数仍为提案；不把解析位移或原生代理证据写成真实游戏验证。
