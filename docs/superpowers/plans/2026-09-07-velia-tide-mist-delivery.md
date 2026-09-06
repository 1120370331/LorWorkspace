# 潮雾晨光全屏特效交付

2026-09-07。依照9004004卡图制作冷蓝潮雾、暖白晨光和柔和的分骰光峰。
一次铺场贯穿两颗骰，第一峰亮内沿，第二峰向外围/偏下云沿扩散，最后统一淡出。
成功目标有局部柔金光痕，多目标不重复叠加全屏峰，空成功列表不伪造局部命中。

## 接入与保持项

Card9004004仅修改两个ActionScript为`Steria_VeliaTideMist`。EffectRes、骰子数值、
FarAreaEach/Team及VeliaCards.cs全部强化、自眩、治疗逻辑保持。默认manager仍负责结算。
同owner/currentDiceAction复用会话，脉冲结束后再检查后续骰队列。
EffectCam附加独立自有OnRenderImage组件及克隆材质，不改全局曝光、其他滤镜或相机设置。
本轮没有新增音效或配音。

## 验收

研究设计Planck，实施Helmholtz，独立视觉复审Gibbs；主线程检查源码和原图，运行工厂
流程、隔离DLL构建并交付。矩形保护凹口、均匀日轮及云沿不足经原生迭代修复。
最终r5有冷云腹/暖沿及日轮下缘遮叠，人物、武器和代理数字清晰，视觉PASS。

- Unity2019.3.15f1 / Built-in / D3D11正式Bundle、材质/Shader/Atlas原生回读通过。
- 145帧60fps，另有+0.10/+0.18秒传播帧；新增近白像素8818/921600（约.96%）。
- 完成帧与初始帧像素差0。正式包与已通过首门仅页脚7像素最大1/255差异，效果区一致。
- 主线程31项原生流程检查PASS：两骰复用、同behavior再入队、多目标/重复回调、空命中、
  暂停、旧会话替换、取消、缺相机/材质、其他滤镜/相机保留及精确像素恢复。
- 源码/真实Unity与game API编译通过；隔离DLL编译0错误、7个现有非本特效警告。
- VeliaCards、卡图和原薄雾图集哈希保持；73个C#编译输入及项目文件在构建/交付期间一致。

运行证据：`output/velia-tide-mist/acceptance/run-20260907-012832`。
冻结预览：`preview_exports/velia_tide_mist/review-521db741`。

## 产物与同步

| 产物 | SHA256 |
| --- | --- |
| Bundle | `521DB741957FCF5DFC26AC630610BC1779B29A25B431731CE998FC5D8DB42F98` |
| DLL | `38DEB64DE1CA451397602E5B0745246C2972C0644BF74253BA35E1F011F9A1EC` |
| 视频 | `524D595394200585B297B62F513D728CCE7FB823F4029196F933D348B62065B2` |

材质入口`VeliaTideMistMaterial`；Bundle仅显式材质及Shader/薄雾依赖，无角色/UI/Mono脚本。
01:35同步DLL、Bundle、.ab和该卡两个XML属性，12个逻辑路径复核一致。
C盘Steam游戏路径是指向D盘安装的Junction，实际是仓库Mod与一个物理游戏安装。
首次同步因别名引起幂等检查误报，确认后修复并复核，没有覆盖其他卡牌配置。

回执：`output/velia-tide-mist/deployment-receipt.json`。游戏原XML备份保留于
`output/velia-tide-mist/deployment-backup-20260907-013229/game-C`，最终同步备份为同父目录
`deployment-backup-20260907-013559`。并行的其他卡牌、数值、设定及指南修改保持未暂存。

证据来自Unity原生代理场景、真实仓库sprite和最小游戏接口替身，不冒称实际战斗/HUD验证。
