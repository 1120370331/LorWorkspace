# 克丽丝塔夏粒子 V2 交付

历史状态：内部视觉与接入检查通过、已部署；**之后被用户否决基本美感，当前正在重做。** 原版的设计、动线和基本功没有充分落实。本文保留上轮技术交付事实，不能再视为用户美术接受证明。

## 实际结果

- 原创金白海潮主题：Slash（H/V共姿态）、Hit、Pierce、Projectile、Cast、Area、Point七个prefab；另外提供不绑定书页的Guard编辑器示例。
- 23张书页的46处旧EffectRes迁移完成，`9009006 汐音：光耀万海`增加独立群攻ActionScript。原有能力、点数、类型、复制骰子和音乐机制保持；其余五张既有Xiao特殊动作不在本次重制范围。
- 汐音0.3s展开范围，0.5s允许原生manager结算，成功目标才产生一次Point；空命中、重复回调、连续骰、取消和缺资源均能收尾。普通远程不伪造成功命中反馈。
- 删除旧Dawn、GoldenMass三份运行代码及旧注册；仓库模组和实际游戏目录的两份旧Dawn包均已备份移除。旧源目录保留为已废弃历史。
- [新版指南](../../../ModGuideDocs/14_粒子特效制作指南.md)与[V2源目录](../../../SteriaBuild/VFXSource/ChristashaParticleV2/README.md)包含七类制作配方、实际参数、构建预览、接入与部署流程。

研究/指南由particle_design负责，纯视觉与引擎预览由particle_visual负责，书页与manager接入由particle_integration负责，particle_review独立验收。主线程亲自读差异和关键帧、运行接入检查、冻结发布基线并部署。

## 最终候选

| 项目 | SHA256 |
| --- | --- |
| 纯视觉driver | `2a0f1da75754a97b2f2ebc08a53f2e6c53ad6efaa37bb8de3003f74a4a9e4805` |
| 最终近战/远程adapter | `0457f38642ba86fe8f61a7fcebdd2411199e9dae0666fe76ff2eadad68eb6d2f` |
| 七根AssetBundle | `a0948f97682402b5e50ff3ca38d1f67e0d86a61c523c59b717830ffcf231f690` |
| 已部署Steria.dll | `37e271da0fb50cb96bffa34666c187059e457b2d2093d7affbf0f157239d30bd` |
| 已部署CardInfo.xml | `4f4575bbe8f26072968414185dc5aa49f55d6f69b0021e87822cbcdda02c59dd` |

视觉候选为 `SteriaBuild/VFXSource/ChristashaParticleV2/evidence/candidate-r4`。r4沿用未变的r3美术包，用修复后的纯C#driver重新导出真实引擎画面。driver没有被序列化进AB，复用该包有效。最终展示 `showcase-60fps.mp4` 为584帧/60fps/9.733秒，已剔除被拒70°旋转的V方案。该旧方案仍留在历史单项媒体中，不能作为最终V展示。

工作区初始已有其他角色改动，期间又出现另一任务的9001004动作修改。发布前核实游戏DLL与XML均匹配提交 `440d8ba3d24c005ced2de81bf9c4bdd5a3de6652`，因此以此提交构建独立发布目录，只叠加本次源代码和47个视觉配置属性（46个EffectRes + 1个ActionScript）。**未顺带部署工作区其他33处配置修改。** 工作区CardInfo继续保留它们，所以其文件哈希有意不同于本次部署XML。

精确可重建发布源、所需引用与输入哈希保存在 `output/christasha-particle-v2/scoped-release/`：

```powershell
dotnet build output/christasha-particle-v2/scoped-release/SteriaBuild/Steria.csproj -c Release
```

本次工作区全量构建曾通过，但该DLL包含其他未发布工作，未用于最终发布。最终采用上述独立基线的Release：0错误、7处其他代码的编译警告。

## 验收证据

- Unity 2019.3.15f1、Built-in Gamma、D3D11、RTX4090 Laptop：AB构建与七根加载、无自定义/丢失脚本、Shader、排序、重播、清空、倍速、Tint隔离通过。
- 主线程实际检查早期/峰值/回收/结束图、左右面向和两体型、汐音完整分阶段、原故障帧及最终caller。媒体检查采用时标帧和60fps编码参数，不声称工具具备连续视频感知。
- 原生方法接入fixture直接执行生产adapter、原生基类及BattleFarAreaPlayManager.ActionPhase，主线程130项检查通过；最后去除V附加角后，仅重跑受影响近战83项，通过左右H/V姿态一致检查。
- 默认与任务开始基线的严格书页检查通过；另一任务3处动作修改有独立receipt，不归入本任务。已部署XML则在游戏原配置上只改本次47个属性。
- [独立评审](2026-09-12-christasha-particle-v2-review.md)最终PASS，无剩余必须修改的视觉项。

首轮密集周期纹理、Hit长锥、Point小月牙已修。之后复现了新生辅助粒子的异色长针/巨雾：CPU数据正常，而渲染首读偶发异常。发射批次后零时长Simulate消除问题；8组顺序/采样对照GPU逐像素一致，CPU时间/轨迹与原时钟逐字节相同。修复和对照在 `evidence/gpu-sequence-sync/`，没有通过重截单帧或遮罩隐藏问题。

旧Unity后来出现启动级0xc0000005。本机通过让新构建子进程继承高16逻辑核mask `4294901760` 恢复；启动器随后恢复原mask。此为本轮已验证的进程级工作绕行，没有改系统/BIOS/电源/许可证，也未将其当成已诊断硬件根因。入口校验新revision、退出码、新日志时间和PASS标记。

## 部署和回滚

实际目录：`D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`。C盘Steam入口解析到同一路径，已去重写入并从两个入口复核。

部署时间：2026-09-12 18:43:54（Asia/Shanghai）。备份和每个文件的前/后哈希：`output/christasha-particle-v2/deployment-20260912T104354Z/receipt.json`，同一receipt也保存在 `scoped-release/deployment.json`。

安装项为 `Assemblies/Steria.dll`、`Assemblies/AB/steria_christasha_particle_v2` 和定向合入的 `Data/CardInfo.xml`。仓库模组同步DLL与AB，工作区CardInfo保持其他任务修改。要回滚，从上述备份的game/package相对路径恢复旧文件，并移除本次新包；只恢复有明确备份的文件，不覆盖后续其他任务成果。

接受边界为真实Unity GPU代理预览、原生方法契约、Release和部署哈希。未进行完整游戏实战；真实角色皮肤、HUD与摄像机遮挡仍以游戏体验为最终依据。Guard只是可运行制作示例，未新绑防御书页。
