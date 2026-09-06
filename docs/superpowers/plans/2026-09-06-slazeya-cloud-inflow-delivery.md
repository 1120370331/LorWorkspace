# 斯拉泽雅云吸入修正交付

2026-09-06，版本`2026-09-06.cloud-inflow.1`。修复用户指出的“整圈缩小”观感：
云团沿XZ方向向内移动，外围保留高度与云瓣体积，靠近中心后才压实成圆亮核。
原云形、噪声资产、配色、闪电、瀑布和技能音效保留，无角色配音。

## 实现及验收

使用同一连续体积场的解析正向输运/逆向查询。XZ行程P/I使内侧先进入核心，
r>=.35H保留源高度，r<.35H才压缩高度。局部Jacobian补偿及逆梯度采样替代
全局缩放、全局消光倍数。源半径分位初始化缓存，G.65–1.28使用单调多锚曲线。
整体proxy只是当前可见支持的包围盒，不驱动材质缩放。

研究设计Russell、实施Sagan、独立视觉评审Ramanujan；主线程审定迭代、检查源码/原图、
运行最终验证并交付。首个球面输运候选被拒绝：仅约3帧补入，并显薄盘/梳齿纹。
按分层证据改为圆柱输运、局部采样和质量分布时间锚后，吸入专项及完整衔接均PASS。

- 独立专项：20张主侧图及49帧，1.033–1.200s约11帧/.167s持续可见核连接和外层补入。
- 完整终审：91帧及18张关键原图；持核、B0到B14外喷及退场通过，未见外围残云/方盒。
- 原生构建：`unity-build-20260906-115903-768-8d58821e.log`，Unity2019.3.15f1/D3D11，Bundle读取及完整预览PASS。
- CPU/GPU输运：2376条有限记录；核心外1390对有效密度样本，最大逆映射误差约1.69e-6H，密度查询绝对差约0.000326。已积核样本不冒充可逆输运证明。
- 最终PlayMode：`output/slazeya_storm_main_acceptance/r5-run-20260906-204756`，54 PASS、0失败。
  同168个ID平均内移.917H、最大尺寸中位比1.341，排除了整体等比缩小。
- 完整可见核心支持最大.10773H/.12H；cloud box角点.05543H/.06H；cloud parcel .01279H/.06H。
- 源码验证、Unity API编译、隔离Release DLL编译、音效10项受控检查、原生声音状态及AV数值验证通过。
  DLL 0错误、7个现有非本次特效警告；音效时序和素材未改。

## 最终候选与交付

| 产物 | SHA256 |
| --- | --- |
| Bundle | `918B3213A65BA542FAEC7F83CE6926A57CE464A81A8990DD65272B6E3AACB8C5` |
| 交付DLL | `055FE0C94962DA60A476B91B9F1D15EC92AAD502AC0AE0CD4CED969145F61A2F` |
| Controller及镜像 | `3DA8605425D84085F1F62704A5BC69BC01629941C188813E17D4C0961EF025C9` |

出图Controller为`E6347866D58A5DF939246F787C4833B15B47F0048FA90B82BB991ED9EE5D3E05`。
之后原生门发现无目标default Footprint初始化曲线异常，已仅对null root+零几何初始化零曲线；
正常布局（含有效布局但缺prefab）仍严格构造原曲线。此修复不改变任何预览像素/Bundle，
最终54项检查使用修复源码。绑定证据：`output/slazeya-cloud-inflow/preview-binding.json`。

DLL由当前完整工作区源独立编译至`output/slazeya-cloud-inflow/delivery-20260906-204749`，
编译输入前后及交付前哈希一致。并行数值、XML、设定及指南修改保留，不纳入本次特效源码提交。

2026-09-06 20:53，仓库Mod目录、C盘游戏Mod、D盘游戏Mod共18文件复制并逐项哈希复核通过。
回执：`output/slazeya-cloud-inflow/deployment-receipt.json`；备份：
`output/slazeya_storm_main_acceptance/deployment-backup-20260906-205348`。
冻结预览：`preview_exports/slazeya_storm_mass/round5/review-918b3213`，带音效视频位于其`audio/battlefield_with_audio.mp4`。

这些证据来自Unity原生代理场景和最小游戏接口替身，未进行实际游戏战斗/主观试听。
近核长褶的少量束带感经独立评审列为非阻断观察；原生渲染读回墙钟不作为游戏FPS。
