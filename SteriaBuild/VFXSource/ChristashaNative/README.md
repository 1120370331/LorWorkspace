# 克丽丝塔夏原生粒子重制：Slash 候选

本目录是 2026-09-12 重新开启的原生粒子制作源。当前最新斩击为 `evidence/slash-candidate-11`：用户认可现有质感后明确要求将违和的黑光改为白金光。10 的动作和破口仍是基线，“必须黑体”的旧美术要求已被用户新要求覆盖。当前目录不部署、不提交；Hit03 保存为暂停中的候选。

## 可重建入口

Windows / Unity **2019.3.15f1** / Built-in / Gamma / D3D11。所有预览使用后台 `-batchmode`、隐藏窗口和 RenderTexture，不启动游戏、Steam，不读取或操作用户桌面。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/ChristashaNative/render-slash.ps1 -Output C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/ChristashaNative/evidence/slash-candidate-10
python SteriaBuild/VFXSource/ChristashaNative/package-slash-evidence.py SteriaBuild/VFXSource/ChristashaNative/evidence/slash-candidate-10 --startup
```

预览项目运行权由主线程交接，不能与别的 Unity 进程同时打开。`render-slash.ps1` 仅复制本目录 Editor 脚本、局部 brush 和 `SteriaBuild/ChristashaNativeParticleLibrary.cs` 到临时项目。它不会修改主线程的 `NativeReferenceRenderer.cs`。输入参考包默认为 `output/christasha-native-redesign/native-import/native_reference_christasha_sources`；原始游戏资源只能放在本地研究缓存中，不进入模组发布目录。可重建提取入口由 `tools/` 独立 owner 提供。

## 原版继承与主题变化（07–10 基线，11 颜色修订见后文）

使用原版 `FX_PC_RolRang_Slash_Down`，完整资源路径见纯库 `SlashResource`。保留原 torus 载体、原编译 Shader、MainTex/Noise/ColorNoise 通道机制、原 Animator、0.3 父尺度和 X180 变换、MainTrail1 与 MainTrail2 的不同角速/衰减。没有把原版粒子重建成静态平片。

主题改造：

- 新增 `(1.22,0.76,1)` / Z=-21° 的艺术空间，形成偏长的斜向主刀势；不归一化原层级。
- 主迹换用 ImageGen 原创局部破锋 mask，宽部有贯穿负形，尾部碎成细丝。它只有竖向笔触，不包含完整刀光轮廓，不是整张概念图贴片。原图保留，运行时按 1.6 倍标量覆盖率做原 Shader 白平台校准，Clamp / linear / no mipmap，黑孔位置不改。
- MainTrail1 保留原动画的暗主体与 HDR 奶白蚀刻锋；MainTrail2 按其实际动画值保留 HDR 奶白切锋。第二条原生 Trail 在 10 恢复原生零延迟、起始 X 尺度 0.91、局部相位 -11°，仍沿原生独立角速运行。这里不能将 MainTrail2 的材质黑色默认值当作其运行时职责。
- 新增 `GoldReturn`：借用同一原生 torus/Shader，但位置偏移、斜角 +38°，尺寸 18×7.4×18、寿命 0.33s、延迟 0.04s、独立角速倍率 0.72。它是较细较短的蜜金返回刀迹，不能作为同步外扩透明壳理解。09 将其覆盖收成两条不等长、两端渐尖的窄丝，剔除 07 的宽圆钝头，后生时刻与角速未动。
- `TideSplinters/TideFilaments/TideDust` 沿主粒子的实际旋转位置分时发射，分别是原创短碎箔、弯曲细丝、极细粒尘。三尺度各有长度/速度/寿命；原 Particle1 尾散也保留并单独改为暖色。

原始参考和主题候选由同一渲染器、同一原编译 Shader、同一资源转换、同一固定相机（ortho=11.5）、同一手动 120Hz 时钟、同一 60fps 输出呈现。主题变形/新增副迹属于明确改造。所有媒体标作 **original data Editor replay**，不是原版战斗录像或实机挂点验收。没有使用 Bloom 或后期掩盖形体。

## 原版 Shader 的关键实际发现

不能只看 Material 序列化默认值。原材质 `_MainColor=(2,2,2,0)`、`_OutColor=(1,1,1,0)`，但真正 `Animator.Update` 后的 MPB 是 `_MainColor=(0,0,0,0)`、`_OutColor=(1.5,1.5,1.5,0)`。单改克隆材质会被 MPB 覆盖。

**必须逐条 Trail 采样。** 07 只采了 MainTrail1，又把 MainTrail2 按默认黑色强制压暗，删掉了早段真正的亮锋，之后用高 HDR 宽主体补偿是错误实现。`evidence/slash-role-probe/original/metadata.txt` 的 ACTUAL f=1/3/6/10 已证明 MainTrail2 的原 MPB `_MainColor=(3,3,3,0)`。09 恢复两条原生 Trail 各自实际职责，逐帧还原原采样，再应用小幅暖色映射。

原 MainTrail DXBC 只读反汇编和纹理/寄存器绑定确认：`MainTex.r` 控制覆盖；`OutColor` 乘由 `Noise.r` 与粒子 `COLOR.r` 产生的窄阈值锋；`MainColor` 乘 `ColorNoise` 的宽体积项。MainTex 不直接提供亮锋，MaskPower 也不是纹理幂。数学说明见 `docs/superpowers/references/christasha-native-redesign/native-maintrail-shader-semantics.md`。该公式不能套到 MainTrail2 的另一个 Outline Shader。

常量动画通道不保证每一帧重新写入。主题映射前先恢复独立缓存的原始颜色通道，更新 Animator，保存原始采样值，然后提交映射结果；不能把上一帧的主题色再次当作原色相乘。颜色 alpha=0 是此 Shader 的原职责值，不能统一改成 1。

原 renderer 的 sharedMaterials 包含未使用的 null 尾槽。检查时忽略空槽，对实际使用的每个 Shader 做 `isSupported` 和真实渲染验证，不能把空槽误报成 Shader 平台不支持。

## 公开 API 与边界

```csharp
// gameResolver: Resources.Load<GameObject>(path)
// localBrush: 此目录原创 PNG，以 linear Texture2D.LoadImage 载入且保持可读。
var library = new Steria.Vfx.ChristashaNativeParticleLibrary(gameResolver, localBrush);
GameObject instance = library.CreateSlashPrototype(parent); // 返回 inactive
var visual = Steria.Vfx.ChristashaNativeParticlePlayback.Play(instance, 19037u, 1f);
visual.Advance(deltaTime); // 唯一时钟；调用方每帧一次
float wallDuration = visual.Duration;
visual.StopAndClear();
```

`originalBrush` 可选仅用于来源对照；正式主题候选必须传入新 brush。调用方拥有传入 brush 和实例；library 仅拥有缓存模板、克隆材质、派生覆盖纹理及原创局部 Mesh，销毁全部实例后才 Dispose。库不修改原 Resources。

克隆必须在 inactive staging parent 下完成，激活前移除全部 MonoBehaviour、音频、碰撞、刚体、相机和灯光，并关闭 Animator 事件/root motion。模板不包含本库脚本组件。手动时钟只推进非 sub-emitter PS，防止同一系统重复推进；Animator 与粒子同一增量；所有 PS 始终 Pause；新生粒子后做零时长 Simulate 同步。Play 不添加层级，StopAndClear 清空所有系统。

尚未接入游戏适配器，也没有证明最终角色尺度、动作/命中时刻、全战斗取消与后处理表现。所有这些仍由主线程后续接入与验收。

## 候选与不足

01–05 是被实现者主动排除的诊断输出：包括只改材质被 Animator 覆盖、固定金色破坏黑白职责、常量 MPB 反馈变暗，以及新 mask 的白平台/色职责校准。它们不是美术已接受版本。

07 已被独立评审 REVISE：高 HDR 填宽主体导致熔金箔/油蜡感；GoldReturn 末端过圆钝。09 针对这两个表面修复，未增加系统。09 仍需独立判断：切锋与暗部的关系是否成立、峰后能否退成余势、副迹是否解释同一次挥刀；GoldReturn 窄带局部仍可能看见原低面数载体的折角，实际角色受力与挂点尚未验证。粗碎箔比概念图克制，不以增加数量作为默认修复。

09 独立复审已解除油箔宽面、钝圆金滴和峰后落差问题，唯一剩余项是 17ms 第一帧全黑：保留自旧暗层假设的 MainTrail2 0.018s 延迟让实际 HDR 切锋晚了一帧。10 仅将此延迟恢复 0，GoldReturn 0.04s 后生与全部已接受材质、mask、旋速均不变。起刀复验板使用 `--startup`，首三行固定 0/17/33ms。

## 用户指定的白金光修订（Slash11）

用户原话：“这个斩击目前的质感还不错。就是这个黑光呢，看起来太违和，不如改成白金光。”因此不再以黑色主体为接受条件，也不改变用户已经认可的纹理、透明破口与动作。

11 只调整 MainTrail1 的颜色/覆盖职责：给它单独的同形 mask，将 R 覆盖乘 0.22，使宽部成为半透明余光；由每帧原 `_OutColor` 包络映射到约 `(1.02,.975,.795)` 的近单位白金填色，避免 07 的高 HDR 宽面。窄锋的 Out 项补偿同一 0.22 覆盖衰减，保留原本的亮度等级。MainTrail2 HDR 切锋、GoldReturn、所有 mesh、旋速、后生时刻和根尺度均不改。brush 原图、裂口位置与完全透明区域仍保持。

`slash-candidate-11/context/` 使用主线程的真实角色实验方法：实际 PNG / PPU50 / XML pivot，地图 S=3，AtkEffectRoot=(-1.92,2.52)，artScale=.35，双朝向，固定 ortho17。它仍是后台合成角色上下文，不是游戏录屏；输入 PNG/XML 的哈希在 `inputs.json`，原实验已标明皮肤 PNG 内自带的历史视觉内容。

`context-clean/` 仅额外用主线程的 `sprite-cleanup/Slash/candidate-r3-512.png` 替换实验演员的 Slash 图，其他姿态/尺寸/相机不变，用于排除旧图内腰环对新光效的干扰；没有替换生产皮肤。可用 `render-platinum-context.ps1` 重放两组。后台运行已统一加 `-job-worker-count 4`，只把自己启动的 Unity PID 设置为 BelowNormal，仍隐藏窗口。

集成所用静态 library 的缓存根在实际 Play 模式执行 DontDestroyOnLoad，以免跨场景时模板被销毁而其缓存材质残留；Editor 不进入此分支。此修复不修改画面，游戏内真实跨场景路径仍需集成阶段验证。

## Runtime MPB 兼容修订

部署环境出现过 Animator.Update 后 Renderer 的 MPB 为空，若直接读取 `_OutColor=0` 会把整条效果错误映成黑色。Playback 现在在首次 Play 前从克隆主题材质保存独立安全通道：MainTrail1 用低强度原始 OutColor safety sample，MainTrail2 用其主题材质 HDR 值；每帧先恢复上次原始 block，再更新 Animator，只有关键采样为空或近零时才使用对应安全值，正常非零 MPB 仍逐帧映射。不会把真实的暗体采样替换掉，也不改变 mask、alpha、mesh 或时间曲线。

`evidence/slash-mpb-fallback-diagnostic-v2/` 同时保存正常 MPB 与在每次 Animator.Update 后主动清空 Renderer block 的重放。两组使用相同原生 Shader、固定相机、seed 与时钟；`mpb-diagnostic.txt` 和 `mpb-fallback-receipt.json` 记录二分条件、源码及代表帧 SHA。两组 0.100s 代表帧不同但都保留白金光，说明 fallback 分支真正被执行；`focused-checks.txt` 仍通过清理、同 seed 重播与层级稳定检查。

## 时序候选（timing candidate-15）

`evidence/candidate-15/` 是在上述 fallback 之后的当前时序候选。默认 `Play(instance, seed, 1f)` 对 Slash 使用目标 Duration 1.30s、峰前原速、峰后 0.92 视觉时钟；Hit 使用目标 1.08s、峰后 0.93。主锋和 Hit 的 0.15s 落点保持原时刻，只有后段退场延长。候选板按合同抽取 Slash 的 17/50/100/167/283/500/750/1300ms，正常速度视频为 60fps。

`candidate-16` 的 `candidate/`、`no-mpb-diagnostic/` 和 `low-mpb-diagnostic/` 分别覆盖正常、空 block、低值 block；低值只在主段阈值内回退，进入退场后保留合法低亮采样，避免把尾迹永久托亮。Hit 的对应正常/空 block 诊断位于 `evidence/timing-hit-03/`，其时序源快照与当前共享库一致。当前源码最终 SHA256 由候选 manifest 记录。

11 的源码完整快照为 `evidence/slash-candidate-11/ChristashaNativeParticleLibrary.cs.txt`。暂停时的 Hit03 则保留自己的源快照，避免后续白金光修订混淆其候选身份。

ImageGen 使用内置工具生成，原提示词与 PNG 在 `Assets/`。源图未做外部软件绘制或抠图。生成图是原创局部纹理；本目录不发布游戏原像素、Mesh 或 Shader。依赖与候选 SHA256 由 `candidate-manifest.json` 记录。
