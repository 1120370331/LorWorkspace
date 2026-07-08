# Unity AssetBundle 特效制作实战：安希尔蓝白刀光

本文记录一次从“代码生成粒子”升级到“Unity AssetBundle 特效资源”的完整流程。案例是安希尔的蓝白透明刀光斩击，目标是做出接近商业 Mod 的夸张渐入渐出、发光、光影、粒子、命中十字星效果，同时能在《Library Of Ruina》内稳定播放。

## 适用场景

- 需要制作自定义 `DiceAttackEffect`，但运行时代码拼出来的 Sprite/Particle 效果太单薄。
- 需要使用 Unity prefab、Mesh Particle、Additive 材质、透明贴图等方式制作更复杂的视觉层次。
- 需要把特效打成 AssetBundle，随 Mod 部署到 `Assemblies/AB` 或 `Resource/AssetBundle`。
- 需要参考其他 Mod 的特效编排，但不能直接复制对方资源。

## 最终文件结构

本次案例涉及的主要文件：

```text
SteriaBuild/
  DiceAttackEffect_Steria_AnhierBlueWhiteSlash.cs
  Steria.csproj
  VFXSource/
    AnhierBlueWhiteSlash/
      README.md
      build_and_deploy_bundle.ps1
      hanzhou_vfx_reference_dump.txt
      UnityProject/
        Assets/
          Editor/
            AnhierBlueWhiteSlashBundleBuilder.cs
        AssetBundles/
          steria_anhier_bluewhite_slash
```

游戏部署目标：

```text
D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\
  Assemblies\
    Steria.dll
    AB\
      steria_anhier_bluewhite_slash
      steria_anhier_bluewhite_slash.ab
```

## 推荐工具链

### 必备工具

- Unity 2019：尽量使用与《Library Of Ruina》兼容的 2019 系列。案例使用本地 `C:\Program Files\Unity\Editor\Unity.exe` 批处理构建。
- .NET SDK：用于 `dotnet build SteriaBuild\Steria.csproj -c Release` 编译 Mod DLL。
- PowerShell：用于自动构建、复制 AssetBundle、复制 DLL、检查日志。
- ripgrep：用于快速定位特效类、构建器、日志关键字。
- Unity Editor Script：用 C# 在 `Assets/Editor` 下自动生成贴图、材质、Mesh、ParticleSystem、Prefab，并打包 AssetBundle。

### 辅助工具

- AssetStudio / UABEA / UnityPy：用于查看 AssetBundle 或 prefab 结构。只建议用于学习编排、层级、材质和粒子参数，不要直接搬资源。
- dnSpy / ILSpy：用于查看其他 Mod 或游戏侧 C# 接入方式，理解 `DiceAttackEffect` 生命周期。
- Unity 自写 Reference Dumper：在 `Assets/Editor` 下写一个只读导出脚本，把参考 prefab 的层级、ParticleSystem 参数、Renderer 模式、材质名、mesh 绑定、startDelay、startLifetime、startSize、burst 数量、sortingOrder 导出成文本，方便对照。
- 游戏 Mod 日志：例如 `Steria.log`，用于确认 AssetBundle 是否加载、prefab 是否使用、运行时对齐参数是否生效。
- 截图或录屏工具：特效必须靠实际画面评估，日志只能证明加载和参数。

## Unity License / 激活问题

Unity batchmode 构建 AssetBundle 前，必须先解决 Unity License。这个问题很容易被误判成脚本错误。

### 常见表现

- batchmode 日志停在 Licensing。
- 命令行返回非 0 exit code。
- `unity-build.log` 里没有进入 `AnhierBlueWhiteSlashBundleBuilder.BuildBundle`。
- Unity Hub 里能看到许可证页面，但 batchmode 仍然不能构建。
- 日志里出现 activation、license、serial、ulf、alf 相关信息。

### 本次踩坑

本机 Unity Hub 的许可证界面显示：

```text
Unity Personal
Activation date: Fri, Aug 17, 2018
Expiration date: Thu, Jul 16, 2026
```

这里要注意两点：

- 看到 Unity Personal 不代表 batchmode 一定能用，仍然要用实际构建命令验证。
- 如果许可证快过期或 Hub 状态异常，Unity Editor 能打开和 batchmode 能构建是两回事。

### 处理步骤

优先走 Unity Hub：

1. 打开 Unity Hub。
2. 进入 Licenses。
3. 如果没有可用许可证，点 Add license。
4. 选择 Unity Personal。
5. 确认列表里出现 Personal license。
6. 关闭所有 Unity 进程。
7. 重新跑 batchmode 构建脚本。

如果 Hub 不生效，再考虑手动激活：

1. 用 Unity 生成 `.alf` 激活请求文件。
2. 到 Unity 官方 License 页面上传 `.alf`。
3. 下载 `.ulf`。
4. 用 Unity 命令行导入 `.ulf`。

不要在 License 未解决时继续调 prefab 或 C#。这时构建器根本没跑起来，改代码不会改变结果。

### 如何确认 License 已经没问题

看 `unity-build.log`，至少要进入项目加载和构建器执行阶段，并看到类似：

```text
AnhierBlueWhiteSlashBundleBuilder:BuildBundle()
Verify passed: loaded particle prefab ...
Built AssetBundle: ...
Batchmode quit successfully
```

如果日志只停在 Licensing，说明还没到特效构建阶段。

## 正确的参考方式

参考其他 Mod 时，重点看“结构”和“方法”，不要复制资产本身。

可以参考：

- prefab 层级如何组织，例如主刀光、拖尾、烟雾、命中闪光、碎片分层。
- 每层粒子的大致职责，例如 Mesh Particle 做主刀光，Billboard 做烟雾，Stretch Particle 做飞散光针。
- 时间编排，例如主刀光先出现，命中光在 0.10 秒后爆发，碎片再晚一点扩散。
- 粒子数量级，例如一个高质量斩击通常是十几到几十个 ParticleSystem，而不是一个 Sprite。
- 渲染模式，例如 `Mesh`、`Billboard`、`Stretch` 各自承担不同视觉任务。
- 材质和混合方式，例如 Additive 做发光，Alpha Blended 做雾。

不要参考成这样：

- 不要直接复制对方贴图、prefab、材质、AssetBundle。
- 不要把解包出来的参数一比一照抄，尤其是资源路径、排序层、shader 名称、mesh 尺寸。
- 不要直接复用对方命名、特效标识或商业素材。

本次安希尔刀光的做法是：参考“寒昼式”多层粒子编排，但贴图、mesh、材质、prefab 全部由本项目的 Unity Editor Script 原创生成。

## 做到“寒昼级别”的参考方法

“参考寒昼级别”不是照抄参数，而是拆解它为什么好看，然后用自己的资源复现同样的视觉逻辑。实际做的时候按下面流程走。

### 1. 先拆结构，不看数值

先把参考特效拆成层级表：

```text
Root
  主刀光层
  白色核心层
  外圈蓝色发光层
  第二道残影/回声层
  速度线/拉伸光针层
  命中爆光层
  十字星层
  碎片层
  烟雾/雪尘层
```

先回答每一层“负责什么视觉作用”，不要急着抄 `startSize=多少`。寒昼级别的观感通常来自层次，而不是某一个神奇参数。

### 2. 再导出参数，只看范围

用 AssetStudio / UABEA / UnityPy 或自写 Unity Dumper 查看参考 prefab。记录这些字段：

- GameObject 名称和父子层级。
- ParticleSystem 数量。
- 每个系统的 `startDelay`。
- 每个系统的 `startLifetime`。
- 每个系统的 `startSize`。
- 每个系统的 burst 数量。
- Renderer 类型：`Mesh` / `Billboard` / `Stretch`。
- sortingOrder。
- 材质 shader：Additive 还是 Alpha Blended。
- shape 类型和发射范围。
- 是否启用 colorOverLifetime / sizeOverLifetime / velocityOverLifetime / noise。

但记录时只记“范围”和“关系”，例如：

```text
主刀光：0.00s 出现，寿命约 0.3-0.5s，sorting 较高
命中闪：比主刀光晚约 0.08-0.14s，尺寸大但粒子少
碎片：命中闪之后爆发，Stretch 渲染，速度高，寿命短
烟雾：透明 Alpha Blend，寿命更长，sorting 较低
```

不要做：

```text
把对方 prefab、贴图、mat、mesh 复制到自己 AB
```

### 3. 建自己的“等价层”

参考每一层的职责，用自己的资源建等价层：

| 参考层 | 自己的实现 |
|--------|------------|
| 主刀光 Mesh | 自己生成 crescent mesh + 原创刀光贴图 |
| 白色核心 | 更细的 thin mesh + 白蓝 core 贴图 |
| 外发光 | 更宽的 glow 贴图 + Additive 材质 |
| 残影 | 低透明 echo 材质 + 更晚或更偏的位置 |
| 光针 | Stretch Particle + spark 贴图 |
| 命中十字星 | 原创 impact glow texture |
| 碎片 | Stretch / Billboard 小粒子 |
| 烟雾 | Alpha Blended mist 贴图 |

这样做出来的东西“结构上像高级特效”，但资产是自己的。

### 4. 调整顺序：先大形，再层次，再细节

不要同时改十几个粒子系统。按这个顺序调：

1. **大形**：主刀光弧度、长度、方向、命中点。
2. **出现方式**：是否从挥刀方向逐段出现，而不是整片弹出。
3. **亮度层次**：白芯最亮，蓝色外光其次，残影更淡。
4. **命中爆点**：十字星和爆光是否贴目标。
5. **速度感**：Stretch 光针是否沿攻击方向飞散。
6. **空气感**：烟雾、雪尘、冷气是否托住主体，但不遮挡主体。
7. **性能**：粒子数量、mesh 尺寸、maxParticles、贴图尺寸是否安全。

每轮只改一个问题。例如“刀光突然出现”就只处理分段 delay，不要同时改贴图和命中点。

### 5. 对比寒昼时看这几个指标

画面对比时不要只说“好不好看”，按指标看：

- **层数**：是否至少有主刀光、核心、外光、残影、命中、碎片、烟雾。
- **时间差**：是否有先后节奏，而不是所有层同一帧爆出来。
- **亮度中心**：眼睛是否先看到白芯和命中点。
- **运动方向**：刀光、光针、碎片方向是否一致。
- **渐入渐出**：是否有 fade in / peak / fade out，而不是硬切。
- **命中可信度**：十字星是否贴目标，不在身后。
- **战斗可读性**：华丽但不能挡住骰子、角色和伤害反馈。

### 6. 本案例的寒昼级别调整路线

本次安希尔刀光最后采用的路线：

1. 放弃单线条 Sprite。
2. 改成 AssetBundle prefab。
3. 用原创程序贴图生成蓝白刀光、十字星、碎片、雾。
4. 用 Mesh Particle 承担主刀光。
5. 用分段 mesh + delay 做挥刀式出现。
6. 用 Stretch Particle 做速度线和碎片。
7. 用 Alpha Blended 烟雾托底。
8. 用运行时代码把 `AB_ImpactRoot` 对齐到目标。
9. 用版本日志确认游戏实际加载的是新 DLL。
10. 用构建日志确认 AB 内有足够粒子层级。

这套路线比“网上找一张刀光 PNG 贴上去”稳定得多，也不会侵犯别人资源。

## 制作流程

### 1. 先定义游戏内目标

不要一开始就写粒子参数。先定义游戏内应该看到什么：

- 主体：蓝白透明月牙刀光，边缘有深蓝外光，中心有白色高亮。
- 动作：不是整片突然出现，而是像挥刀一样从左到右逐段出现。
- 命中：目标处出现十字星、白蓝爆光、碎片和雪尘。
- 方向：角色在左或右攻击时，刀光方向必须跟随攻击者与目标相对位置。
- 位置：命中特效必须贴近目标命中点，不能偏到目标身后。
- 性能：不能因为 mesh 或粒子尺寸过大导致游戏一帧后未响应。

### 2. 运行时代码负责接入和对齐

`DiceAttackEffect_Steria_AnhierBlueWhiteSlash.cs` 负责：

- 在 `Initialize` 中记录攻击者和目标：
  - `_selfRoot`
  - `_targetRoot`
  - `_atkDir`
- 在 `Start` 中优先加载 AssetBundle prefab。
- 找不到 AssetBundle 时回退到运行时生成的 fallback 效果。
- 读取 AssetBundle 路径：
  - `Resource/AssetBundle/<bundle>`
  - `Resource/AssetBundle/<bundle>.ab`
  - `Assemblies/AB/<bundle>`
  - `Assemblies/AB/<bundle>.ab`
- 实例化 prefab 后根据方向旋转根节点。
- 调用对齐逻辑，把 AB 内部的 `AB_BladeRoot` 和 `AB_ImpactRoot` 移到正确位置。

核心思路：

```csharp
Vector3 targetLocal = root.transform.InverseTransformPoint(_targetRoot.position);
```

用目标世界坐标转成 prefab 本地坐标，再移动内部根节点。这比在 prefab 里写死 `x=8.75` 稳定，因为不同站位、不同方向、不同目标位置都会变化。

### 3. Unity 构建器负责生成资源

`AnhierBlueWhiteSlashBundleBuilder.cs` 负责自动生成：

- 原创透明 PNG 贴图：
  - 主刀光 core
  - 外发光 glow
  - 回声 echo
  - 命中十字星 glow
  - 碎片 spark
  - 雾 mist
- 材质：
  - `Particles/Additive`
  - `Particles/Alpha Blended`
- Mesh：
  - 整体刀光 mesh
  - 分段刀光 mesh
  - 细核心刀光 mesh
- prefab：
  - `AB_BladeRoot`
  - `AB_ImpactRoot`
  - 多个 ParticleSystem 子节点
- AssetBundle：
  - `steria_anhier_bluewhite_slash`

这种做法的优点是：贴图和 prefab 都可以从代码重建，换机器或清 Library 后仍然能复现。

### 4. 刀光“挥出过程”的实现

最初的问题是刀光整片突然出现，看起来不像挥刀。

最终采用分段 mesh + 延迟播放：

```text
AB_Blade_OuterBlue_A       delay 0.000
AB_Blade_OuterBlue_B       delay 0.030
AB_Blade_OuterBlue_C       delay 0.062

AB_Blade_WhiteCore_A       delay 0.018
AB_Blade_WhiteCore_B       delay 0.052
AB_Blade_WhiteCore_C       delay 0.086

AB_Blade_SecondCrescent_B  delay 0.070
AB_Blade_SecondCrescent_C  delay 0.105
```

也就是把一整道刀光拆成 A/B/C 三段。每段使用同一张刀光贴图的不同 UV 范围，按时间错开出现。这样不需要写自定义 Shader，也能做出从左到右逐渐挥出的效果。

分段 mesh 的关键不是把贴图裁成三张，而是 mesh 顶点只覆盖某段 U 范围：

```csharp
float t = Mathf.Lerp(uStart, uEnd, localT);
uvs.Add(new Vector2(t, 0f));
uvs.Add(new Vector2(t, 1f));
```

这样能保留完整贴图的渐变、边缘噪声和透明衰减。

### 5. 命中十字星对齐

最初的问题是十字星明显偏到角色身后。根因是 impact 位置写死在 prefab 内，例如 `new Vector3(8.75f, -0.05f, 0f)`，但实际战斗中目标位置会随站位、方向、动作根节点变化。

解决方式：

- prefab 里保留 `AB_ImpactRoot`，只作为可移动分组。
- 运行时拿 `_targetRoot.position`。
- 转换为 AB root 本地坐标。
- 把 `AB_ImpactRoot.localPosition` 设置到 targetLocal 附近。
- 加一点前后偏移，避免完全压在角色中心。

示例策略：

```csharp
float side = targetLocal.x >= 0f ? 1f : -1f;
impactRoot.localPosition = new Vector3(
    targetLocal.x - side * 0.22f,
    targetLocal.y + 0.12f,
    -0.02f);
```

调试时必须在日志里打印：

```text
AnhierBlueWhiteSlash AB aligned: targetLocal=(...), scale=..., dir=...
```

如果游戏内还偏，就用这条日志继续微调偏移量，不要盲调。

### 6. 方向和距离处理

方向来自攻击者和目标的世界坐标：

```csharp
_atkDir = (Direction)((target.WorldPosition - self.WorldPosition).x > 0f ? 1 : 0);
```

AssetBundle prefab 实例化后：

```csharp
main.transform.localRotation =
    _atkDir == Direction.LEFT
        ? Quaternion.Euler(0f, 180f, 0f)
        : Quaternion.identity;
```

刀光长度按目标距离缩放：

```csharp
float forwardDistance = Mathf.Max(1.2f, Mathf.Abs(targetLocal.x));
float distanceScale = Mathf.Clamp(forwardDistance / 8.75f, 0.70f, 1.35f);
bladeRoot.localScale = new Vector3(distanceScale, 1f, 1f);
```

这样近距离不会过长，远距离不会太短。

### 7. 远程弹道：AB 负责素材，运行时负责路径

远程弹道和近战刀光不是同一种问题。近战刀光可以在 AB 中预排多个 mesh 粒子，用 `startDelay` 做“挥出过程”；远程弹道如果目标距离、Z 深度、双方站位会变，就不能只靠 prefab 中固定长度的粒子轨迹。

错误做法：

- 在 AB 里预先做一条固定 8 到 10 单位的轨迹。
- 用 `AB_TravelRoot.localScale` 按距离拉伸。
- 用多段 mesh 粒子错开 `startDelay` 模拟飞行。
- 命中点只按 `targetLocal.x` 和 `targetLocal.y` 放置，忽略或弱化 `targetLocal.z`。

这种做法在短距离测试时看起来还行，但日志里一旦出现：

```text
AnhierBlueWhiteFarHit AB aligned: targetLocal=(41.02,1.23,-17.01), dir=LEFT
```

就会暴露问题：AB 中的固定轨迹不可能覆盖 40 单位以上的真实路径，缩放上限还会让轨迹停在中段；命中爆点如果没有使用目标世界坐标，也会看起来偏到目标身后或偏离目标身体。

正确做法是把远程特效拆成两层：

- AB 层：负责好看的素材和局部演出，例如施术者面前的法阵、命中十字星、爆点、碎片、雾气。
- 运行时层：负责从施术者到目标的真实路径，例如弹头、拖尾、路径长度、命中点世界坐标。

本案例最后采用：

- FarHit 加 `UseRuntimeProjectile => true`。
- AB 中的 `AB_TravelRoot` 在 FarHit 下隐藏，避免固定轨迹干扰。
- C# 运行时创建 `TrailRenderer`：
  - 白蓝核心轨迹。
  - 蓝色余辉轨迹。
  - 细火花轨迹。
- 在 `Update()` 中用 `Vector3.Lerp(_runtimeStartWorld, _runtimeImpactWorld, eased)` 推进弹头。
- 每帧用 `RefreshRuntimeImpactWorld()` 从目标 pivot 刷新命中世界坐标。
- 弹头接近目标后再激活 AB 中的 `AB_ImpactRoot` 粒子。

关键代码结构：

```csharp
protected override bool UseRuntimeProjectile => true;

private void UpdateRuntimeProjectile()
{
    RefreshRuntimeImpactWorld();
    float raw = Mathf.Clamp01(_elapsed / RuntimeProjectileTravelTime);
    float eased = 1f - Mathf.Pow(1f - raw, 3f);
    _runtimeProjectileRoot.transform.position =
        Vector3.Lerp(_runtimeStartWorld, _runtimeImpactWorld, eased);

    _impactRoot.position = _runtimeImpactWorld;
}
```

`TrailRenderer` 的价值是“先出现的先消失，后出现的后消失”，这比 AB 中一次性 burst 出一排粒子更适合真正的子弹/飞行轨迹。AB 的分段粒子可以作为近战刀光、冲击波、短距离突刺的表现方式，但远程飞行路径最好交给运行时代码。

## AssetBundle 构建与部署

推荐用统一脚本：

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\VFXSource\AnhierBlueWhiteSlash\build_and_deploy_bundle.ps1"
```

脚本做的事情：

- 调用 Unity batchmode。
- 执行 `AnhierBlueWhiteSlashBundleBuilder.BuildBundle`。
- 输出到 `UnityProject/AssetBundles`。
- 复制到项目 Mod 文件夹。
- 复制到 C 盘 Steam 目录。
- 复制到 D 盘实际游戏目录。
- 同时复制无后缀和 `.ab` 两份，兼容不同加载路径。

手动 Unity 命令结构：

```powershell
& "C:\Program Files\Unity\Editor\Unity.exe" `
  -batchmode -quit -nographics `
  -projectPath "...\VFXSource\AnhierBlueWhiteSlash\UnityProject" `
  -executeMethod AnhierBlueWhiteSlashBundleBuilder.BuildBundle `
  -logFile "...\VFXSource\AnhierBlueWhiteSlash\unity-build.log"
```

## DLL 编译与部署

只重建 AB 不够。只要改了 `DiceAttackEffect_*.cs`，就必须重新编译并部署 DLL。

编译：

```powershell
dotnet build "C:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\Steria.csproj" -c Release
```

部署：

```powershell
Copy-Item `
  "C:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\bin\Release\net472\Steria.dll" `
  "D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\Steria.dll" `
  -Force
```

如果有 C 盘和 D 盘两个游戏目录，两个都要复制，实际游玩的目录尤其不能漏。

## 验证清单

### 构建验证

检查 Unity 构建日志：

```powershell
Select-String -LiteralPath "...\unity-build.log" `
  -Pattern "Verify passed","Built AssetBundle","Copied bundle","error CS","Exception" `
  -CaseSensitive:$false
```

期望看到：

```text
Verify passed: loaded particle prefab AnhierBlueWhiteSlashPrefab
Built AssetBundle: ...
Copied bundle to game mod folder: ...
```

不应出现：

```text
error CS
NullReferenceException
Bundle output not found
Verify failed
```

### 文件验证

检查 D 盘实际游戏目录：

```powershell
Get-Item `
  "D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\Steria.dll", `
  "D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB\steria_anhier_bluewhite_slash", `
  "D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB\steria_anhier_bluewhite_slash.ab" |
  Select-Object FullName,Length,LastWriteTime
```

### 运行时验证

启动游戏并触发一次攻击后，检查 Mod 日志：

```powershell
Select-String `
  -LiteralPath "D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Steria.log" `
  -Pattern "AnhierBlueWhiteSlash","AssetBundle","aligned","fallback","Exception","ERROR" `
  -CaseSensitive:$false |
  Select-Object -Last 120
```

期望看到：

```text
AnhierBlueWhiteSlash: loaded AssetBundle ...
AnhierBlueWhiteSlash: using AssetBundle prefab (...)
AnhierBlueWhiteSlash AB aligned: targetLocal=(...), scale=..., dir=...
```

如果没有 `AB aligned`，说明游戏跑到的 DLL 不是新版本，优先查 DLL 是否复制到了实际游戏目录。

## 关键踩坑

### 1. 只部署 AB，不部署 DLL

表现：

- 游戏日志能看到 `using AssetBundle prefab`。
- 但看不到新加的版本标记或 `AB aligned` 日志。
- 视觉效果仍然是旧位置、旧方向逻辑。

原因：

- AssetBundle 更新了，但运行时代码还是旧 DLL。

解决：

- `dotnet build` 后复制 `bin/Release/net472/Steria.dll` 到实际游戏目录的 `Assemblies/Steria.dll`。
- 给关键版本加日志标记，例如：

```csharp
private const string EffectVersion = "AB-align-swing-reveal-20260706";
```

### 2. Unity batchmode 超时不等于构建失败

表现：

- PowerShell 命令超时。
- 但 Unity 日志尾部其实有：

```text
Verify passed
Built AssetBundle
Batchmode quit successfully
```

处理：

- 不要直接重做或乱改。
- 先查 `unity-build.log`。
- 再查 AssetBundle 输出文件时间戳和大小。

### 3. Mesh 粒子尺寸过大会卡死游戏

表现：

- 特效播放一帧后游戏未响应。

常见原因：

- mesh 宽高过大。
- 粒子 startSize 过大。
- burst 数量过多。
- 没有限制 `main.maxParticles`。
- Stretch 粒子长度和速度叠加后过于夸张。

处理：

- mesh 本体保持小尺寸，例如宽 1 左右，高 0.1 到 0.3。
- 用 ParticleSystem 的 `startSize` 放大，而不是把 mesh 顶点做成几十上百单位。
- 每个系统设置合理 `maxParticles`。
- 先做安全版 AB，再逐层加回烟雾、碎片和拖尾。

### 4. 命中特效写死位置

表现：

- 十字星偏到目标身后或目标中心外。

原因：

- prefab 里写死 impact 坐标，只适合一种站位。

解决：

- prefab 只保留可移动分组根节点。
- 游戏运行时根据 `_targetRoot.position` 重定位。

### 5. 刀光整片突然出现

表现：

- 光效很亮，但像贴图弹出来，不像挥刀。

解决：

- 拆分 mesh 为多段。
- 每段错开 `startDelay`。
- 主体、白芯、回声层使用不同延迟和排序。

### 6. 找错游戏目录

表现：

- 文件看起来复制了，但游戏没变化。

原因：

- 同时存在 C 盘 Steam 和 D 盘 Steam 游戏目录。

解决：

- 日志显示实际路径时，以日志里的 Mod 根目录为准。
- 本案例实际使用：

```text
D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder
```

### 7. README 或旧文档过期

表现：

- 文档写着 Unity 授权阻塞，但实际已经能构建。

解决：

- 每次跑通后更新教程文档。
- 记录真实命令、真实路径、真实日志关键字。

### 8. Unity License 没激活却继续调参数

表现：

- 怎么改 builder 都没有新 AB。
- `unity-build.log` 没有 `BuildBundle()`。
- AssetBundle 文件时间戳没变。

原因：

- Unity batchmode 卡在 License 阶段，构建器根本没有执行。

解决：

- 先到 Unity Hub 的 Licenses 页面确认 Personal license。
- 必要时生成 `.alf` 并导入 `.ulf`。
- 直到日志出现 `Verify passed` 再继续调视觉参数。

### 9. 参考资料只看结果，不拆方法

表现：

- 觉得参考特效“很帅”，但自己做出来只有一条线。
- 反复加亮度，仍然没有高级感。

原因：

- 没拆层级、时间、渲染模式，只是在模仿最终画面。

解决：

- 先做参考层级表。
- 再做自己的等价层。
- 最后按“大形 -> 时间 -> 亮度 -> 命中 -> 粒子细节 -> 性能”顺序调。

### 10. 远程弹道只用 AB 预排

表现：

- 屏幕上只有几个点或短线。
- 拖尾集中在路径中间，看不到完整飞行路径。
- 先出现的拖尾没有先消失，整体像一帧闪现。

原因：

- prefab 中的粒子轨迹是固定长度。
- `startDelay` 只能模拟预排时间，不能感知游戏内真实距离。
- `AB_TravelRoot.localScale` 有缩放上限，远距离时会明显不够长。
- ParticleSystem burst 是一次性发射，不等于“弹头正在移动”。

解决：

- AB 只做法阵、爆点、贴图、mesh 和局部粒子层。
- 运行时用 `TrailRenderer` 或逐帧移动的粒子发射器做弹道。
- 用 `Vector3.Lerp(startWorld, targetWorld, t)` 推进弹头。
- 用 `TrailRenderer.time` 控制先出现先消失。
- 远程命中类单独加开关，例如 `UseRuntimeProjectile => true`，不要影响近战斩击、打击、突刺。

### 11. 命中点没有使用目标世界坐标

表现：

- 爆点或十字星看起来在目标身后。
- 不同方向、不同站位时偏移不同。
- 短距离还可以，远程或特殊位移时明显错位。

原因：

- 只用 `atkEffectRoot` 或 prefab 本地坐标。
- 只对齐 `targetLocal.x/y`，忽略目标的 `z` 深度。
- 在 `Start()` 里对齐一次，但目标动作和 pivot 在播放过程中可能变化。

解决：

- 目标根节点优先取 `target.charAppearance.GetAtkEffectPivot(ActionDetail.Hit)`，没有再回退到 `target.atkEffectRoot`。
- 运行时每帧刷新 `targetRoot.position`。
- 命中爆点用世界坐标放置：`impactRoot.position = runtimeImpactWorld`。
- 远程弹头接近目标时再激活爆点粒子，避免爆点比弹道提前出现。
- 日志打印 `targetLocal` 和 `impactWorld`，看到 `targetLocal.z` 很大时优先查 Z 轴对齐。

## 调试策略

遇到效果不对时，按这个顺序查：

1. 日志是否创建了自定义 effect。
2. 是否加载到了 AssetBundle。
3. 是否使用了 AssetBundle prefab。
4. 是否出现版本标记。
5. 是否出现对齐日志。
6. `targetLocal` 数值是否合理。
7. `targetLocal.z` 是否很大；如果很大，不能只调 X/Y。
8. 远程弹道是否出现 runtime 日志，例如 `runtime projectile created` 或 `AB runtime aligned`。
9. 远程弹道是否用了 `TrailRenderer`，而不是只用 AB 里的固定粒子段。
10. AB 文件时间戳是否新。
11. DLL 文件时间戳是否新。
12. 游戏目录是否是实际运行目录。
13. 确认不是旧 DLL / 旧 AB 后，再回到 Unity prefab 参数调整。

不要跳过日志直接调视觉参数，否则很容易在旧 DLL 或旧 AB 上浪费时间。

## 一套可复用的制作模板

新做一个类似特效时，可以按这个模板复制思路：

1. 新建 `VFXSource/<EffectName>/UnityProject`。
2. 写 `Assets/Editor/<EffectName>BundleBuilder.cs`。
3. 在 builder 中生成贴图、材质、mesh、prefab、AssetBundle。
4. prefab 根节点下至少保留：
   - `AB_BladeRoot`
   - `AB_ImpactRoot`
   - 其他可选分组，例如 `AB_TrailRoot`、`AB_GroundRoot`
   - 远程特效可额外保留 `AB_CasterRoot`、`AB_TravelRoot`
5. 运行时代码加载 AB prefab。
6. 运行时代码按攻击者/目标位置旋转和重定位分组。
7. 如果是近战刀光、打击、突刺：可以在 AB 中用分段 mesh 和 `startDelay` 预排挥出过程。
8. 如果是远程弹道：AB 只做法阵和命中素材，弹头和拖尾用运行时代码驱动。
9. 远程弹道运行时至少记录：
   - `runtime projectile created`
   - 起点世界坐标。
   - 命中世界坐标。
   - `targetLocal` 和方向。
10. 写构建部署脚本。
11. 加版本标记日志。
12. 构建 AB。
13. 编译 DLL。
14. 复制 AB 和 DLL 到实际游戏目录。
15. 游戏内触发一次，查日志和画面。

## 本次经验结论

- 高质量斩击特效不能只靠单条 Sprite，至少需要主刀光、白芯、外发光、回声、拖尾、命中闪、碎片、烟雾多层叠加。
- Unity AssetBundle 是比纯运行时代码更适合复杂特效的方案。
- AssetBundle 适合承载美术素材、局部粒子编排和短时间爆点，但不适合单独承担会随真实战斗距离变化的远程弹道。
- 远程子弹、射线、拖尾这类效果，最好用 AB 做素材，用运行时代码控制弹头位置、TrailRenderer 和命中点。
- 参考别人的特效时，真正有价值的是层级、时间、渲染模式和粒子数量级，不是贴图文件本身。
- 方向、命中点、距离这些和战斗场景有关的东西，必须留给运行时代码处理，不能完全写死在 prefab。
- 命中点要优先使用目标攻击 pivot 的世界坐标；看到 `targetLocal.z` 很大时，说明只调 X/Y 一定不够。
- 每次视觉修正都要同时考虑 AB 和 DLL 两条部署链。
- 日志版本标记非常重要，它能立刻判断游戏跑的是不是你刚编译的代码。
