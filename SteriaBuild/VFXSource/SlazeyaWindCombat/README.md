# 斯拉泽雅风系粒子特效

> **2026-11-16：旧 Slash / Hit / Pierce 已由水雷效果接替并部署。** 新工程为 `../SlazeyaWaterThunderSlash/UnityProject`；三个近战工厂键统一水雷表现，远程与守备仍使用本工程的旧风系资源。详见 [废弃标记](SLASH_DEPRECATED.md) 与 [水雷部署记录](../SlazeyaWaterThunderSlash/deployment/DELIVERY.md)。下文为历史交付记录。

当前交付候选：`candidate-15-pierce`。五类为斩击、打击、突刺、防御、远程；配色为青玉与青白。主刀光采用连续的 SweptWedge 主刃与错时回风，其它动作采用 FormedArc。近突刺改为0.12秒刺到目标、短时贯穿、快速收回；确认命中后在胸前扩张青白光圈并渐隐。其它四类的592帧与已部署candidate-12-final逐字节相同。前九版被用户否决，第十版防御缺失，候选14光圈过软被独立复核退回，均不能当成当前视觉接受证据。

- [新版突刺](evidence/candidate-15-pierce/Pierce-dark-left-60fps.mp4)
- [五类连续预览](evidence/candidate-15-pierce/SlazeyaWind-showcase-60fps.mp4)
- [突刺分阶段](evidence/candidate-15-pierce/Pierce-dark-left-stages.jpg)
- [当前 AssetBundle](../../SteriaModFolder/Assemblies/AB/steria_slazeya_wind_combat)
- [Unity 制作场景](UnityProject/Assets/WindAuthoring.unity)

主线程亲自制作。本轮按仓库要求增加独立视觉复核，候选15获得PASS；部署沿用用户指定的Terra代理。内部复核不代表用户已经认可最终风格。

## 技术边界

Unity **2019.3.15f1 / Built-in / Gamma / D3D11**。预制体 `SlazeyaWindSlash/Hit/Pierce/Guard/Projectile` 仅包含 Transform、ParticleSystem、ParticleSystemRenderer；驱动源为 `SteriaBuild/SlazeyaWindVisual.cs`，使用唯一 120Hz 时钟，手动 Emit/Simulate/Pause。新生粒子先 Simulate(0)；Mesh 顶点色使用 Color32，以避开该目标版本的 Mesh 粒子颜色布局问题。

游戏适配器为 `DiceAttackEffect_Steria_SlazeyaWind.cs`，资源缓存为 `SlazeyaWindParticleAssets.cs`。近战按原生挂点、2倍基础尺度及一次骰子倍率处理；Guard 基础尺度为1。远程根在中立空间移动，0.26秒抵达跟踪目标后停止飞行，并播放中性风压与散光；它不自行结算伤害。所有关闭、销毁和取消路径清理粒子。inactive 子树显式设置 Effect 层，Renderer 使用 Particle 排序层。

近突刺的 `SlazeyaWindImpact.cs` 只配对同一帧、同一目标的原生表现与Penetrate受击事件；确认后替代本次通用火花，等风刺实际到达才播放光圈。光圈冻结世界位置，超过120fps的无模拟tick帧也恢复锚点；空刺、错误目标、跨帧回调、取消不出圈。伤害计算不变。

只迁移 9002001/2/3/4/6/7/8 的15个 EffectRes，以及动态追加 Guard 的一个 EffectRes，并注册5个工厂键。9002009群攻、天气、战斗数值、音效和其它角色效果不在本次变更范围。

## 验证证据

- `evidence/candidate-15-pierce/acceptance.log`：AB 构建后重新加载，五类×双朝向×深/中灰背景，完成、暂停、同 seed 重播、取消清理及真实 Update 循环通过；另有miss和Pierce扩张/锚定检查。
- `readability.json`：20组实际 GPU 帧均有可见像素；它只证明基础可见性，不代替美术评价。
- `output/slazeya-wind-combat/factory-pierce-15/acceptance.log`：97项真实Unity/game API、生产adapter/driver/命中路由、AB、原生工厂与player loop检查通过；Harmony实际安装到原生方法。场景使用合成Bada变换，未启动完整接待或游戏战斗。
- `output/slazeya-wind-combat/scoped-pierce-release/build.log`：隔离Release构建成功，0错误、8项既有警告；PDB核对101份源码文档与测试工作区一致，13份资源及程序集引用保持已部署基线。旧managed spy不作为本轮验收依据，见Verification目录README。
- `Verification/SlazeyaWindCombat/verify_routes.py`：相对任务开始快照精确验证15+1+5变更。

预览角色采用实际装备的原版 Bada 图像、PPU、pivot和地图S=3比例；为隔离新光效，省略原皮肤Effect9层。Bada没有Fire姿势，远程使用Default近似。预览没有完整战斗镜头、HUD、所有体型或游戏后处理。最终实战与用户美术评价仍需外部验收。

## 在 Unity 中继续制作

打开 `UnityProject`，使用菜单 **Steria → Wind → Open authoring stage**。选中 `Wind Slash - Live Authoring`：关闭 Auto Play 可拖动 Sample Time，Playback Speed 控制预览速度。双击 Blade Material 可直接调整固定 Shader 的参数；构建器保留已有材质的 Inspector 修改。场景保存的角色 Sprite 依赖位于忽略的本地 `Assets/Reference`，不进入发布AB。

```powershell
# 编辑器关闭时，使用新版本号构建和渲染，不覆盖旧证据。
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/SlazeyaWindCombat/build_preview.ps1 -Revision candidate-next
python -X utf8 SteriaBuild/VFXSource/SlazeyaWindCombat/make_showcase.py SteriaBuild/VFXSource/SlazeyaWindCombat/evidence/candidate-next

# 编辑器保持打开时，可对已保存的工程快照离屏渲染。
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/SlazeyaWindCombat/capture_snapshot.ps1 -Revision candidate-snapshot-next

dotnet build SteriaBuild/Steria.csproj -c Release --no-restore
```

脚本使用隐藏后台窗口、D3D11、4个job worker，并仅对启动器子树临时设置该机器需要的CPU affinity和OpenSSL兼容变量。不要并发打开同一个Unity工程。真实工厂模拟的准备脚本会复制本机游戏依赖到忽略的Plugins中；这些参考DLL不是发布内容。

## 来源与交付

两种主Shader来自 `unity-sword-slash-vfx` 固定基础设施，保持字节不变。楔形蒙版由内置ImageGen生成，提示词与原件路径见 `source_assets/wind-wedge-v3.json`。流光为Screaming Brain Studios CC0 Perlin，光尘为Kenney CC0 light_01。光圈选自skill索引的VFXTextureMaker `circle_01.png`，haw2fregel/MIT，原字节不变，仅使用PNG，未导入其Unity2022工具。许可和哈希见 `source_assets/`、`provenance.json`。素材仍为candidate，没有提升skill库状态。游戏角色仅供本地预览，不进入发布AB。

已于2026-11-14由Terra发布新版突刺到D盘实际游戏目录：DLL、AB、许可说明共3文件，仓库包和C盘别名均核验SHA256；XML保持不写。主线程再次核对了游戏和仓库的3文件。隔离发布只在已部署五动作基线上修改driver、adapter并新增命中路由。

本轮部署记录与备份位于 `output/slazeya-wind-combat/deployments/20261114T080500Z-a5fea909/`，精确发布信息见 `delivery-candidate.json`。本次未启动或重启游戏，实战验收仍待进行。共享文件中的无关改动保持原样。
