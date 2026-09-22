# 克丽丝塔夏白金特效交付：through-r12

已于 2026-09-22 部署到实际游戏目录和仓库模组目录。全程单线程，没有启动智能体。此前被否决的三角片、宽剑/弓形护手候选没有部署。

## 交付行为

- 黑光根因：原 Animator 即使 speed=0，仍在脚本 Update 之后重写原生黑色 MPB。克隆 Animator 现在由手动时钟独占，每次求值后禁用并保留 controller state。真实 Play Mode 的同帧颜色覆盖从基线 1487 次降为 0；斩击、重砸、突刺、飞剑、施法、范围、点位均覆盖。
- 突刺按用户红框放大：宽体根部固定在出手点，穿过目标并延伸至敌人身后；单独保存命中点，光圈不会错误挪到穿出点。0.16s 接触，随后 0.075s 完成贯穿，0.28s 后侵蚀退出。角色上下文左右两侧均实测覆盖约 23.84 世界单位，载体峰值高 10.4896。
- 飞剑沿用用户选定的细长直刃、窄剑脊、小直护手，走上扬再收落的弧线。二次时间曲线令飞行由慢到快；剑尖沿切线转向，世界空间拖尾保持弧线。0.38s 准确落到实时目标，命中时强化爆闪、扩散冲击与 24 枚碎光。
- 真实成功命中后调用已有镜头反馈 helper，一次轻抖：X=.012、Y=.009、Speed=70、时长 .14s。未命中、提前取消、过期/错误目标和重复回调不能触发额外命中或重复抖动。不修改伤害结算。

## 候选和检查

证据目录：`output/christasha-platinum-fix/through-r12/`。视频 `strikes-60fps.mp4`、逐帧图、深/中底阶段板、`frame-loop.txt`、`build-source-checks.json` 和 `candidate-manifest.json` 均保留。

- Unity 2019.3.15f1 / Built-in / Gamma / D3D11：新 Shader AB 构建、加载和真实 Play Mode 通过。
- 七配方、同 seed 重播、取消、未命中、双向角色比例与贯穿端点通过；主线程查看起势、接触、峰值和退场帧。
- 222 项生产适配器契约断言通过，包括加速、动态目标、切线朝向、贯穿位置和轻抖时机。
- 实际游戏 Managed API 编译通过；Release 构建 0 错误、7 项既有警告。
- 91 份 PDB 源文档校验和匹配本次构建；其余已安装源码没有漂移，未混入新出现的其他功能。

证据边界：GPU 画面为真实 Unity Play Mode、实际角色贴图的代理场景，不是游戏战斗录像。远程借用 Penetrate 姿态，因为该皮肤没有 Fire 图。镜头抖动使用游戏现有 helper，其触发/一次性门禁和真实 API 已验证；离屏视频没有冒充实机相机滤镜录屏。

## 部署、备份与精确指纹

实际目录：`D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`。仓库包和 C 盘 Steam 链接入口也已复核。

- DLL：`5cddc0b97c81b058d84a569e2ae7950114aed0bc0e0bf75d289e1832fc74ed40`
- Shader AB：`b40c46753a718612fa1804665a6820999ac905e9ee6088b1b2a27b40d3c76b94`
- 原创笔触 PNG：`c805e4729653114577fa4edaf4d4e50688ff3b09ab0d771bf7d31f0b964f0255`

部署收据和逐文件备份：`output/christasha-platinum-fix/deployment-20260922T070056Z/receipt.json`。先安装资源，再原子替换 DLL，随后复核 SHA256。游戏已有笔触图未重写；发现仓库模组缺少该必需图，已补齐同一份原创素材。两份 CardInfo.xml 均确认没有变化。

原 DLL `47c561286f8597a30e34300300c67751bacfb7ad4470f422095df756a9d2fa4c` 的备份保存在收据的 backup 路径。回滚必须按收据核对当前文件仍是本次 after hash，再恢复对应备份；只移除收据中 before=null 的本次新增文件，不能覆盖后续其他任务的发布。

源码提交仅包含本任务文件与本任务新增发布资源；已有混合工作区构建 DLL 不纳入源码提交，以免将其他已安装但未提交功能混入该提交。实际部署 DLL 的内容由上述 PDB、候选 manifest 和收据约束。
