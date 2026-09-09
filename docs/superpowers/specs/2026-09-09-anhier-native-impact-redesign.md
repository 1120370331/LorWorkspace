# 安希尔攻击特效：按游戏本体重做打击存在感与动作空间

状态：主线程批准实施；以下已合入远程组件后续精确量测。测量基线为 `6557e8a` 中的六种 `AnhierTextureCombat`。本轮依据用户“对照废墟图书馆怎么设计特效的来做，参考游戏本体”“现在的特效跟没有一样”，不以旧预览 PASS 为接受证据。用户随后明确要求直接模拟，无须打开游戏；验收采用同条件原版/候选模拟。两套已安装 Unity 2019 编辑器均在导入前因现有许可证校验失败，批准用实际生产源码与反编译原版基类在受控托管变换夹具中生成对照，并明确标注非 Unity 引擎运行；实际游戏 DLL 的 API 编译另行验证。

最新用户约束：“你不应该直接抄巴达的皮肤，而是做独立特效，合适就行。”成品必须是原创贴图和通用效果组件，不修改、复制或绑定巴达皮肤。原版数据只作为动作尺度、挂点和时间校准依据，本章数值作为合理范围参考，不要求照搬巴达的造型或逐像素尺寸。主展示使用独立绘制的中性人形与两种体型；隐藏角色原有 Effect(9) 层，避免将巴达金色招式误认成原创成品。原版动作对照只保留为清楚标注的参考，不再是唯一的验收画面。

## 1. 已确定的根因

问题不是只差一个 XY 偏移。现在的系统把原版的**动作笔触**做成了位于手边的一小截装饰，并同时削弱了原版的尺度、完整形状保持时间和力量反馈。

1. **尺度链被改变，且旧预览没有测到。** `DiceAttackEffect.Initialize()` 把攻击挂到动作枢轴，设置 `localScale=2*(1+additionalScale)`；`DiceEffectManager.CreateBehaviourEffect()` 随后调用 `SetScale(scaleFactor)`，再乘 `1+scaleFactor`。`RencounterManager.PrintEffect()` 中这个 factor 是 `resultDiceValue/vanillaDiceFace`，上限 2，并传入 `time=1f`。常规顶点掷骰 factor=1 时，原版是 4 倍素材单位。当前代码只有 `1+.12*factor`，即 1.12 倍，并将父系世界尺度夹在 `.55..1.75`。`BattleUnitView.ChangeScale()` 又明确使用 S/M/其他地图 3/2.25/1.5 倍；夹取会进一步压缩常见场景中的效果。旧夹具只输入 actorScale=1、factor=0，因此没有覆盖这些实际参数。
2. **斩击形状不跟动作。** 原版 Bada_J 是角色身前的高纵向弧，PhilipEGO_J 是纵向落斩火弧。当前 MemorySlash 的高宽比为 1.727/6.5=0.266，是水平狭弧；挂到同一个真实 Slash 枢轴后，它仍是手边细线，无法表现原版动作的整段挥击路径。把旧母图转 90 度只能换方向，不能得到上轻下重的落斩锋面与尾迹。
3. **接触事件之后仍在逐步揭示。** 原版常规攻击全笔触在事件首帧即出现，保持 250ms，随后到 666.7ms 平滑退掉。当前多数轮廓首帧只剩峰值 alpha 质量的 23–44%，33ms 才完整；到 183ms Slash 只剩 38%，283ms 只剩 9%。这与已发生的出招/打击动作脱节。精确整张母图只停留 33ms，但不能误称整个高存在感峰值只有 33ms：67–116ms 阶段仍保留约 99% alpha 质量。正确结论是**前段缺失、后段过早解体、可读时间总量不足**。
4. **世界坐标重定向绕过了动作姿态。** 当前取动作 pivot 的位置后脱离父级，每帧按目标方位重算 Z 角度与水平翻转；原版近战跟随完整 pivot 的旋转、缩放和镜像。目标高低位置不应让落斩变成一段朝向目标根的射线。当前 MemoryHit 更被整体移至 `target.atkEffectRoot`，失去原版 H 从攻击者动作出发的主笔触；原版受击效果才属于目标根。
5. **排序策略与较强原版参考不同。** Bada H/J/Z/G 使用 sortingLayer `Particle`，ID `2898253913`（signed `-1396713383`），order 0；当前子渲染器只设置 order 110，留在 Default。GameObject 的 `Effect` layer 和 SpriteRenderer 的 sortingLayer 是两回事。原版 TagManager 中 Particle 排在 Character/CharacterUI 之后。但 Bada 角色原素材、Basic/Philip/Kurokumo 较早攻击也使用 Default，所以这只是已确认的渲染契约差异，**不能在没有游戏摄像机证据时断言它必然被全部角色遮挡**。本轮应采用 Bada 的 Particle 排序，并实机确认。

## 2. 游戏本体量测与可见证据

研究脚本与 JSON 均位于 `output/anhier-native-impact/research/`。原图仅本地参考，不进入发布目录。

- `native-pose-comparison.png`：实际 Bada Slash/Penetrate/Guard 分层姿势，姿势单独 / 原版攻击 / 当前攻击三列，同一单位尺度、factor=1、t=34ms。已人工检查。它不是游戏截图，不能充当最终验收；它隔离并直接展示形状、根位置、尺寸差异。
- `native-shape-atlas.png`：Philip J/H/Z、Index Z、Bada J/H/Z/G 的真实 alpha 形状及同单位尺寸。
- `native-prefab-measurements.json`：prefab、Sprite、PPU、pivot、原矩形、裁切偏移、alpha bbox、各 renderer 排序、脚本字段、Transform。
- `bada-character-measurements.json`：原版 `char_418.pres` 的全部 Transform 和实际动作的 SpriteRenderer；`bada-body-bounds.json` 排除动作自带 `effect_atk` 后的人物边界。
- `current-profile-measurements.json`：六种现有贴图在每个阶段的 alpha 质量、边界和累计存在感；`native-tag-manager.json` 是游戏原始 layer 表。
- 重现：`python -X utf8 output/anhier-native-impact/research/measure_native.py`，再运行同目录 `compose_reference.py`。脚本只读游戏和当前源码，仅写研究目录。

以下边界以 alpha>10/255 量测，单位为 **actor hierarchy scale=1、roll factor=1 的实际效果局部世界单位**。并非 PNG 全画布尺寸。Bada_G 已包含 `additionalScale=(-.5,-.5,-.5)`，因此是 2 倍而非普通攻击的 4 倍。

| 原版资源 | Sprite / PPU | 实际宽×高 | 形状与用途 |
|---|---|---:|---|
| Bada_J | resources.assets:23059 / 100 | 7.52×18.00 | 竖向前弧，整段挥击；不是水平刀条 |
| PhilipEGO_J | resources.assets:23358 / 100 | 10.24×14.40 | 上部轻细，落点白热加厚，纵向落斩 |
| Bada_H | resources.assets:21398 / 100 | 17.28×4.80 | 横向冲击带，前重后轻 |
| PhilipEGO_H | resources.assets:21514 / 100 | 17.68×9.00 | 厚重横扫火弧，前端白热且轮廓完整 |
| Bada_Z | resources.assets:23027 / 100 | 14.32×8.96 | 压力锥与后方爆散，不是只有一根尖线 |
| PhilipEGO_Z | resources.assets:22159 / 100 | 20.36×3.92 | 前端锐锋，完整长尾 |
| IndexUnit_Z | resources.assets:22894 / 50 | 17.76×2.56 | 细而长的穿刺轨迹，PPU 不是 100 |
| Bada_G | resources.assets:21209 / 100 | 6.16×8.62 | 前方盾形接触环，单独采用半尺寸系数 |

这些常规 prefab 除 Bada_G 外，已解码 `offset=0, additionalScale=0, addedDestroyTime=0`，根 Transform 位置 0、旋转 identity、scale 1，材质为 Sprites-Default。它们的绝对存在感主要来自原生尺度链和动作空间，而非额外 Bloom。

| 当前 Profile | 当前峰值宽×高（factor=1） | 250ms 前后存在感问题 |
|---|---:|---|
| SeaPierce | 7.28×1.71 | 183ms 后 alpha 质量只剩 46% |
| SeaFarHit 主体 | 5.04×1.69 | 216ms 后 33%，且起点和命中前段错位 |
| MemorySlash | 7.28×1.93 | 183ms 后 38%，283ms 后 9% |
| MemoryPierce | 7.28×1.96 | 183ms 后 40% |
| MemoryHit | 4.26×3.55 | 小型目标爆点取代整个 H 动作 |
| MemoryGuard | 3.44×5.15 | 183ms 后 48%，尚需地图尺度再压缩 |

当前 six-profile 峰值归一化 alpha 积分为 .224/.211/.200/.209/.277/.230 秒；原版 `Anim_Default` 为 .45833 秒。这个量测仅比较完整形状存留，不是亮度/打击感的完整模型。

原版 `resources.assets:7048`、`Anim_Default` 的 alpha 三次系数已在 manifest 对象 timing 内解码：t=0 与 .25 的值均为 1；.25 后 `27.647995*d^3 - 17.279999*d^2 + 1`，d=t-.25；t=.666667 为 0。`PrintEffect(...,time=1)` 因此实际保持 250ms、667ms 视觉结束，GameObject 在 1s 销毁。补充：Initialize 确定 Animator.speed；后续 SetDestroyTime 只改变对象销毁时间，不重新缩放既定视觉时钟。模拟和新适配器需要分开处理这两个时钟，不能把该 setter 错误解释为动画变速。

### Bada 真实动作和枢轴

`char_418.pres` 的 `CharacterAppearance` 根矩阵是单位矩阵；`AtkEffectRoot=(-2.2,2.34,0)`，scale 1、identity 旋转。H/J/Z/G/E/S/F 显式 pivot 全部为空，`GetAtkEffectPivot` 因而都回落同一个 root。不是每种 Bada 动作另有未测出的挂点。

人物本体高度 Default=4.90，Slash=4.03，Penetrate=3.97，Hit=4.04，Guard=4.01 单位；因此 Bada_J 原版纵弧约 4.47 个 Slash 姿势高度。当前 Slash 仅 .48 个高度，并且方向完全不同。将老 fixture 的 Default 当成攻击姿势会掩盖此问题。攻击动作还各有 `CharacterAppearanceType.Effect(9)` 的内建金色 `effect_atk`，它是动作 prefab 内独立 SpriteRenderer，**不是烘焙在人物 body PNG 上，也不是由 CreateBehaviourEffect 创建**。对比图所有列保持这一层相同；它不能被误认为新特效自身的存在感。

`char_418` 没有 Fire 动作；`char_187.pres` 更只有 Default。源码 `CharacterAppearance.ChangeMotion()` 对不存在的 Fire 回退 Default。因此远程验收必须使用确有 Fire 的实际装备皮肤；Bada 只能检验无 Fire 时的引擎回退，不能假装有开火姿势。

### 原版远程构成

原版 `RedHoodMercenary_Gun`（root resources.assets:39348）明确有根部 Fire、子 Bullet、Trail、Hit。Fire sprite 350×272/PPU100、pivot(.9032,.49)；Hit 342×364/PPU100、pivot(.1983,.475)；Bullet 80×46/PPU100、pivot(1,.6)，Trail 432×27/PPU100、pivot(0,.6)。这是不同角色的素材和不同锚点，不是一张水弹到目标后继续当全部反馈。

`DiceAttackEffect_RedHoodMercenary_Gun.Initialize` 把枪口放 self Penetrate pivot，把独立 Hit 放 target.atkEffectRoot，距离<6 时关 Bullet；寿命 .5s，Animator.speed=2。`Anim_GunFarAtk`（7095）四个 renderer 的 alpha 都从首帧 1，原 clip .6667 前保持 1、1.0 退到 0，对应实际保持333ms、500ms视觉结束。**发射和命中可在同一战斗事件被完整表达，存在感不依赖追加长飞行。** 本设计学它的事件分工；不照搬枪口火焰像素，不把水锋横向拉成长激光。

## 3. 批准后实现的运行时契约

维持现有 18 颗骰子、6 Profile 的 EffectRes 绑定和海潮/回忆主题。海潮 9001001/7/8，回忆 9001002/3/4/5/6/9/10，4 颗 MemoryGuard。角色身份不能成为全局替换条件。音效、震屏、时间与胜负事件继续由游戏负责；不得为追求“打击感”无条件叠加两次 shake/sound。

**近战与守备重新采用 native attachment。** 主 `DiceAttackEffect` 保留在 self 对应 pivot 下，localPosition 使用明确的 profile offset，localRotation identity，继承整个父链一次。H/J/Z/G 使用明确动作路由；可用确定的 `_H/_J/_Z/_G` 对象名调用原基类 `Initialize`，而不是让英文 `MemoryHit` 等字符串碰运气命中名字启发式。保留 `base.SetScale` 的 `1+factor`；禁止世界尺度 clamp、目标斜率代替动作角度、二次水平翻转。源美术统一向原版的 LEFT 方向，运行时镜像交给 `characterRotationCenter`。

**素材在 native 单位中归一化，不能把原6.5宽PNG再盲乘4。** 下表给出进入 native 2x 与 roll 放大之前的局部素材目标。JSON 应表达素材 PPU/alpha-content span；发布后通过实际 runtime bounds 检验。

**复用原生动画/生命周期，避免另造不一致时间。** 优先让主 SpriteRenderer 使用现有 native `Anim_Default` controller（可以在运行时引用游戏自带 controller，不发布原版图片），原创 frame 子层同步同一 alpha 与规范化时间；或由共享 timeline 精确表达同一原生曲线。调用方的 destroyTime 必须生效，t=elapsed/destroyTime，视觉曲线 .6667 结束，销毁服从原生有效寿命。若原生动画驱动主 renderer，其 alpha 必须同步给全部原创子层，不能留下一层常亮。不得保留 no-op `SetDestroyTime` 或固定 .4/.5 截断通用近战。

**渲染采用已测 native 层。** 近战/guard renderer sortingLayerName=`Particle`、逐层 order 0/1；GameObject layer 由 `SetLayer("Effect")` 统一设置。独立目标 hit 子节点也遵循此规则；夹具设置原版 TagManager 排序，不能只有 Default 一层。维持共享 Sprites-Default RGBA、黑紫不按亮度抠 alpha、逐实例 sprite/color、共享缓存。

**远程单独沿用枪口/目标命中契约。** 在 native PrintEffect 事件即给出完整枪口和目标接触水花，不模拟33ms飞行。枪口在 self Fire pivot 的世界位置，Hit 在 target.atkEffectRoot 的世界位置。后续对 Gun 组件的 104 字节原生布局进行了指针交叉验证：additionalScale=(1,1,1)，因此 root 初始2倍，再乘1+roll，但 root 属于 BattleSceneRoot，不继承角色地图缩放。factor1、单位 SceneRoot 时原生 Fire 的 alpha 边界13.56×10.24，Hit 为11.24×11.68。新枪口局部宽约3.4，目标水花局部面积约8.2，保持素材比例后 factor1 约14.1×9.3，与原生 Hit 面积一致；禁止额外乘 mapScale3。

远程本地素材存在方向例外：Fire 向左，Hit 从接触点向右展开；整体方向由发射者明确镜像一次。目标水花保留在自有中立根下，只追踪目标位置，不继承目标的反向镜像、旋转、尺度。缩放之后执行 Update 再量测命中点，避免把原版 Initialize/SetScale 间的临时偏移当成稳定结果。目标水花是水体接触，胜利专属伤害火花仍交给引擎，不添加必定受伤的星爆。采用 .5s 窗口，强态保持 .333s、随后淡至 .5s。量测证据为研究目录的 redhood-gun-exact-base-fields.json 与 redhood-gun-dimensions.json。

## 4. 六种轮廓和色彩的具体重做

表中最终尺寸同上，factor=1、actor hierarchy scale=1；`素材局部`尚未乘普通 melee 的 4 倍。以 native 大形为基线，允许 ±15% 校准，不接受再缩回旧版占比。偏移要按 pivot 到手部/挥击面的关系校准，不能仅以 sprite bbox 中心等于目标中心作为正确。

| Profile | 重做形状与素材处理 | 素材局部 / 最终 alpha footprint | 根与高光 |
|---|---|---|---|
| MemorySlash | 必须换独立纵向落斩母图。参考 PhilipEGO_J 的上细下厚与 Bada_J 的身前纵弧。完整弧由头顶前方贯至膝下，黑紫内侧宽笔触、外缘局部橙白热刃；上段为断裂轻尾，主冲击集中下前段。旧水平母图留作输入参考，不直接旋转当成品。 | 局部约 2.5×3.5；最终约10×14，高/宽1.3–1.6 | self Slash pivot；局部原版中心/挥击面，不朝 target 根追踪。落点锋面明显比上尾重。 |
| MemoryPierce | 可复用当前母图。增加真实 native 尺度并把完整尖锋与长暗尾保持；不要过早把整条尾部打洞消失。参考 PhilipZ / IndexZ 的尖锐主轴。 | 局部宽4.2，保原比例；最终约16.8×4.53 | self Penetrate pivot；以居中笔触覆盖动作前后，前锋朝左。当前偏尾的 .15 pivot 不可原样搬进 native 父系。 |
| MemoryHit | **把原 MemorySlash 水平弧母图转用为 H 主体**，其高宽比 .266 与 BadaH .278 非常接近，这是有原版形状依据的素材复用。保留前重后轻的黑紫/橙红笔触，按新 H 尺度保持完整；旧 MemoryHit 目标星爆退出运行，不能在攻击对守备失败仍打印效果时伪造成功受击。 | 主体局部宽4.4，保原比例；最终约17.6×4.68，接近 BadaH17.28×4.8 | self Hit pivot；不生成自有 target damage burst。HasDamagedEffect=false，原版管理器仅在 Win 分支添加受击火花。 |
| MemoryGuard | 可复用当前竖弧，但开口朝防御者身侧、受力外缘面向对手；补足前接触段的橙白火星/厚度，不让整圈都是暗紫。参考 BadaG 的可读盾面与力量收束。 | 素材局部高度4.4、比例保留；使用 native G additionalScale=-.5，factor1最终约5.87×8.8 | self Guard pivot；成功防御者身前，绝不跟 target；稳定中心，受击侧一次压紧后释放。 |
| SeaPierce | 可复用当前水体母图，恢复长刺和足够厚的压力主体。主水体深蓝至青蓝，前端完整白蓝切水刃，尾部小飞沫；首帧就读到接触锋。 | 局部宽4.2；最终约16.8×3.94，水体可在宽高±15%范围内补厚 | self Penetrate pivot；居中 native 攻击域，不再把尾端钉在手边向远处无限伸。 |
| SeaFarHit | 复用已有水锋/独立水花，按原版 Fire+Hit 分工重排；目标水花为主视觉 | factor1、单位 SceneRoot：枪口宽约13.6，目标水花约14.1×9.3，与原版 Hit 的可见面积相当；保持比例 | neutral SceneRoot，枪口定位 self Fire、命中仅追踪 target世界位置；不继承 actor/target尺度，不模拟33ms飞行。 |

不能机械地用一个素材 pivot 数值适配所有造型。新的 centered native 主体建议先取(.5,.5)对应原版画布中心，再根据**原创 alpha 内容对挥击面的位置**设置小的固定局部 offset；使用左右镜像与两种实际皮肤检查。原版本身的图集裁切偏移已包含于其 Sprite，不要对导出 trimmed PNG 再重复加一次 offset。

### 时间和笔触层级

常规近战：事件首帧至少展现完整主形的 75–85%，1帧内完整；0.017–.25s 保持完整大轮廓（至少85%峰值主体 alpha 质量），可做局部锋缘/火舌运动；.25–.5s 才收热、侵蚀后尾；.5–.667s 收余迹至透明。不要在.183s就只剩三成细碎笔触。颜色峰值不等于整图白化：黑紫体积必须始终可见，只有冲击前缘/折角白热。海潮用水体深浅反差替代全图发光。Guard 参考 native G 保持明确盾面至少.20–.25s；远程参考 native Gun 强态至少.25s、可至.333s，.5s结束。

这明确替代 `ModGuideDocs/13` 上一轮示例中的“斩是低宽横扫”、固定0.4–0.5s通用近战和绝对世界坐标镜像规则。该示例描述旧实现，不是反驳新用户意图的冻结规范。实现阶段应同步修正对应说明，避免后续生成器恢复被否定的行为。

## 5. 验收与拒收

**先做原版/候选同路径 A/B。** 同一实际角色皮肤、同一动作、同一朝向、同一 actor scale、同一 roll factor、同一相机与时钟，对应原版 BadaJ/H/Z/G 或 PhilipJ/H/Z，以及 RedHood 远程。使用真实 `Initialize -> SetScale -> SetLayer -> Update` 链；至少记录当前动作名、self/target/pivot localToWorldMatrix、效果 bounds、sortingLayer、incoming lifetime/factor。不要只直接调用 `Timeline.Sample`，不要换一个更近的相机让效果显得更大。

按用户追加要求采用模拟，不以打开游戏或实战为完成前提。代表近战使用真实动作层级与当前 EffectRes 映射数据，另外取远程命中和 Guard；原版与候选在同条件下包含0/17/50/150/250/400/667ms代表帧与正常速度播放。最少一次反向角色、非单位 pivot旋转、3/2.25/1.5地图尺度与factor1，验证完整父系姿态。托管模拟必须执行未替写的生产适配器方法及原版基类逻辑，用其输出矩阵驱动图像合成；不能独立写一组看起来合理的图像位置冒充执行结果。它不声称验证实际 Unity GPU、镜头或音效混音。

量化尺寸须落于上表目标 ±15%，并在有不同 map/height scale 时保持相对人物比例，不可出现 1.75 截断。作为 sanity check：新 MemorySlash 在 Bada Slash 下约3–3.6个姿势高度，旧版小于半个；新 Pierce 应有约4个 Bada 身高的长轴，不能退成旧版针线。还必须视觉确认锋面在动作前方，而不是为了达到宽高指标跨过对手头顶或把防御罩放背后。

直接 REVISE 的条件：

- Slash 仍为手边水平细条，或机械转旧横弧导致落点/尾迹同重；Hit 仍只有目标小星爆。
- 首帧几乎不可见、事件150ms已进入稀疏余迹、250ms完整攻击域丢失；用慢镜头才看得见。
- 只放大 PNG 或修改 fixed offset，却保留 clamp/power弱化/目标斜率/固定寿命；素材归一化与 native 倍率同时叠乘失控。
- 效果绕错误根镜像两次、Guard跟攻击者走、主近战飘到目标根、远程命中延后明显。
- 用不同相机、仅Default站姿或只有Timeline.Sample的新夹具证明 PASS；原版内建 effect_atk 被误算作新效果。
- 黑紫主体被抠空、整图透明薄壳、重复轮廓层、火焰白化整段、水锋拉成长光柱。
- 成功构建即结束，没有独立动态预览审查；将托管模拟声称为真实 Unity 引擎或游戏画面。用户已明确模拟足够，不再设置实战门槛。

## 6. 实现边界与证据限制

生产所有权建议由一个 runtime/preview 实现者统管 `AnhierTextureTimeline.cs`、`DiceAttackEffect_Steria_AnhierTextureCombat.cs`、`import_masters.py`、profile JSON 生成与 native-path preview；**仅新纵向 Slash 母图**可以由单独美术实现者在 `imagegen_masters` 内明确独占新子目录。原 MemorySlash 母图不覆盖，作为 MemoryHit 的输入保留；其余母图已有可复用形状。待主线程批准设计后交接，不能各自改契约。最终预览由第三位独立审查者与用户意图对照。研究者此次没有重新生成整个原版库，也没有运行游戏/Unity或更改安装目录。

静态研究证明这些尺度/时序/形状差异足以解释“跟没有一样”；摄像机合成、实际使用的用户皮肤、时停与 native action start 时刻仍需主线程实机证据完成。此研究不承诺单靠排序修改即可解决问题，也不把假定 actorScale=1 的参考合成当成实际地图。

权威源定位：`Assembly-CSharp/Battle/DiceAttackEffect/DiceAttackEffect.cs:25/70`；`DiceEffectManager.cs:72`；`RencounterManager.cs:1413/1467/1482/2121/2249/2494/2835/2921`；`CharacterAppearance.cs:81/426/465`；`CharacterMotion.cs:9`；`BattleUnitView.cs:116/273`；`DiceAttackEffect_RedHoodMercenary_Gun.cs:9`。native 数据由现有 `NativeAttackReference/references/all/manifest.json` 引用，每个 Sprite/组件路径 ID 在研究 JSON 中保留。
