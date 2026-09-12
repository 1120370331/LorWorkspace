# 克丽丝塔夏原生粒子重制：接入与发布记录

## 当前状态

**已完成定向部署（2026-09-13 UTC）**。用户禁止桌面控制、游戏/Steam启动；实际证据使用隐藏后台 Unity 2019.3.15f1 / D3D11 离屏渲染、原生 API 编译、源码链接的宿主契约检查。不得称为整场游戏实战通过。

**用户已接受 Slash11**：“这个斩击我很满意”。固定当前白金配色、纹理破口、动线、错时回迹及0.35角色尺度，后续只保护这一接受面，不继续主动润色。其余动作和部署仍需完成。

**用户已否决 Hit01–04 的动作语义**：“打击……应该是……一招比斩击横向更有力的刀剑纵砸”。拳击式爆点、压力扇和Glove星核主导的方案全部作废；重新以强力自上而下刀剑主势为核心，落点压波/碎光为辅助。重新设计由particle_design负责，native_visual等待批准后实现，不能继续给旧拳击方案换色并称为修复。

| 表面 | 责任方 | 主线程结论与证据 |
| --- | --- | --- |
| 原版研究及设计 | particle_design | 已接受，具体原生 Mesh/Shader/Animator 数据重放；见 native-redesign 设计稿 |
| Slash11 白金光 | native_visual | ACCEPT：用户认可原质感但要求去黑光；独立复审 PASS，主线程亲看暗/中底早峰散及双向人物合成。保留锋口、破口、动线和错时金迹，黑体改为半透明白金余光 |
| Slash 人物定位 | 主线程实验、particle_review 复审 | ACCEPT：实际512 PNG、PPU50、legacy pivot、mapS3、AtkEffectRoot(-1.92,2.52)，artScale=.35，无额外偏移或转向；双向合成。synthetic engagement±10、ortho17，不涵盖全部游戏距离 |
| 普通攻击 loader / 宿主 | particle_integration | ACCEPT（限定接入）：改用 Resources 原生库、线性可读原创 brush、special-motion pivot、宿主scale1、SetScale no-op；保留caller寿命与清理。93近战宿主断言、真实游戏API编译 PASS；主线程读diff/检查及候选hash |
| Slash及其它四张去烘焙光效素材 | particle_visual | ACCEPT：五张512RGBA候选均经独立视觉审和主线程检查，已备份原图并写入workspace及本次部署 payload；ModInfo/pivot未改 |
| Hit 纵砸 | native_visual | ACCEPT：Greatsword 双主拖迹与 Swing 形成上方→左下剑尖落势，约0.15s触地后才有压波/碎光；独立视觉评审 PASS；角色上下文选 artScale=.35 |
| Pierce / Projectile | pierce_visual | ACCEPT：白金核心、淡金侧束、透明断口和0.26s前远刺主表现通过独立视觉及近刺双向角色校准，artScale=.35 |
| Cast / Area / Point | mass_visual | ACCEPT：Cast/Area/Point及组合时序通过独立视觉；Area使用+X前进和DesignWidth32，经适配器朝向/范围校准并完成Default角色上下文 |
| 其它动作去烘焙特效 | particle_visual | 正在准备Hit/Penetrate/S1/Slash2候选；尚未替换生产图 |
| 制作指南 | particle_design | 主线程已读改动并核对全部本地链接；按原始重放、分工调色、逐类评审、实际上下文及scoped发布更新 |

## 2026-09-13 的实施分工和当前返修

- `native_visual`仍独占主库；Hit纵砸 `hit-slam-candidate-03` 已通过独立视觉审查。源码快照 `4c74974a6473b7fd76cd888f69d6b864debbfbb6e709135846826602aed5139b`，视频 `4bc7173f10c4a7ac471aeadbcdf0602fe346954876ac5a97efe6d7803df3eb`；用户要求的上方→剑尖纵落、次级触地反馈及Slash11三帧零像素回归均有证据。角色上下文 `hit-context-small/0.35-left` 与四档诊断确认 adapter HitArtScale=.35；完整游戏移动/镜头仍未验证。
- `mass_visual`拥有 `ChristashaNativeMassRecipes.cs` 和 `VFXSource/ChristashaNative/Mass/`。`candidate-04`技术已完成，源码 `0c242d246cafe1fdf00b1c6ebcff0f9f876e857fcc94fe4eb8919fb1a6bee3e0`，配套driver `aede90df179f89ce3ce1b788d282cbc22f9b30d216a6ab07e7d522df122559ec`。原Xiao Shader仅读取 `_Main.g` 作为明暗，不应把max(RGB)灌入G；实际色彩来自 `_Color/_DissolveColor`。已修这个通道错误，但本轮尚无独立美术PASS或角色场景验收。该owner曾遇到429失败，保留当前文件/快照，待其余活跃实现收口后恢复，不重复已做研究。
- `pierce_visual`拥有 `ChristashaNativePierceRecipes.cs` 和 `VFXSource/ChristashaNative/Pierce/`。首轮偏暗偏细，已修复完整MPB保留/原生颜色采样并做白金核心修正；最新源码 `8DAF44AF350BED76C01279E9DECA894B98D0C860718BD67EE1EDBE7792689B04`，preview03技术检查及独立美术审均通过；近刺角色上下文以0.35校准。
- 三种独立recipe共用 `IChristashaNativePlaybackHooks`，原始单位Duration和BeforeAnimator/AfterAnimator/AfterParticles/StopAndClear；主driver唯一120Hz模拟，不允许hooks重复Simulate或Animator.Update、Play时加children。各recipe用独有marker，避免原生同名Main_Trail1串入Slash映色。
- 角色五张清理均已独立PASS：Slash r3、Hit/Penetrate/Slash2 r1、S1 r2。主线程已看原/新并确认安装原文件与工作区原文件一致；计划 `output/christasha-native-redesign/sprite-cleanup/accepted-skin-plan.json`，尚未应用到生产或安装。
- 9009006当前实际姿态解析见 `xiyin-motion-resolution.md`：Motion S是Special，当前skin缺该节点，预期回Default；不能把S1图当作其当前实际姿态。未修改动作字段。

## 已接受候选指纹

- Slash11完整库快照：`SteriaBuild/VFXSource/ChristashaNative/evidence/slash-candidate-11/ChristashaNativeParticleLibrary.cs.txt`，SHA256 `24b1d0a29605c90a94c80bcaf0a6e864ad2609a2fca1c94ef4190fc451b10f5d`。后续共享库增加其它动作不代表旧Slash证据自动失效，须核对被接受方法/素材未改。
- Slash11正常速度媒体：同目录 `comparison-normal-speed.mp4`，SHA256 `a0c1f988b1c6780a63b954ca3d8bedf55dcf38a86c22389c13999314c2753164`。实际评审使用带时标多帧，不能声称凭编码信息观看了视频。
- 已接受宿主adapter：`SteriaBuild/DiceAttackEffect_Steria_ChristashaParticleV2.cs`，SHA256 `0d497213281ccba288d26e0e07ac14633d9a27a20b39a6b340517ce98b275f6e`。证据 `output/christasha-native-redesign/integration/`，宿主fixture使用视觉替身，不代表GPU。
- 去旧光效角色候选：Slash `f15f4de24ebc621eeafaf055224208835e3b620ed37042898182b6ffefff2`、Hit `96b7d02c141feef0812f2d2ce7e5b83a5d9b0d02cb721dde936a29f7efe6bed1`、Penetrate `a675e8d23edc75a9d50681477939ba97607f4387c9672f817b5b340a93bb550b`、Slash2 `a6ab76f107fd9ca107a7769630a4658f9a5b351ade808ff4495a70a29dcb3550`、S1 `4bd92cde6425804748f405591a88db0e56b4a36ab75f4cf9494054396eeb5139`。五张均为512RGBA，ImageGen负责绘画，Sharp只做品红背景alpha和整画布512包装；无bbox缩放、平移或局部拼原。来源、坐标比较、独立review、workspace备份与应用记录均在 `output/christasha-native-redesign/sprite-cleanup/`。

## 当前安装基线与发布边界

安装路径：`D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`。同期音效发布已更新安装，不能用f76433e0旧DLL覆盖它。

新的精确安装基线为 `312fcc18d83f7346239a71d6ed46ac4dcc875fbd` 加三份与安装DLL Portable PDB校验和相符的已安装源码快照（HarmonyPatches、AnhierTextureCombat、MemoryConcerto），及部署XML中9001004的三个已部署ActionScript。其它30项工作区XML变化不进入本次发布。

- 部署前安装DLL：`4ba01608ce09fc598e553f2b41a8d0d919b2c64b497620c4ec19706fc1ae7762`；部署后为下方 receipt 记录的 `7208d473…`。
- 部署前后安装XML均为 `ab091603d7aa8739e456dd731d01fcf3efa1b1af1c65f30231825b171fcfaeb5`。
- 源码/安装证据和冻结副本在 `output/christasha-native-redesign/scoped-release-tools/`。
- 主线程已读并运行 `scoped_release.py plan`：基线匹配，未冻结动作/输入导致预期的BLOCKED状态；未执行prepare/build/deploy。

本次已用该scoped工具完成精确输入冻结、prepare、Release build、备份部署、逐文件原子替换和复核。部署 receipt 为 `output/christasha-native-redesign/scoped-release-candidate/deployments/20260912T180100-24e8f2ec/receipt.json`，安装 DLL SHA256 `7208d473ce2ccca39e607fb0fee360da4b16d82a371cd9857e4f0725cda4d5f0`，安装 XML 保持 `ab091603d7aa8739e456dd731d01fcf3efa1b1af1c65f30231825b171fcfaeb5`。原 `steria_christasha_particle_v2` AB 已按存在路径退役；原版提取参考包未发布。保留23页46处EffectRes与9009006的现有ActionScript合同，没有顺带修改玩法数据。当前未启动游戏验证，安装后须在用户方便时自行确认实战移动/镜头/HUD表现；回滚使用receipt中的scoped rollback命令。
