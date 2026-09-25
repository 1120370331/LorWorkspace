# 克里丝塔夏普通攻击首次播放卡顿修复

2026-09-25。覆盖 SlashH/SlashV、Hit、PierceNear、PierceFar 及飞剑背后法阵 Portal。视觉配方、Shader、纹理、动作时序和命中门禁均未改。

## 原因与修复

原适配器在第一次攻击的 `Initialize` 中同步解码 1254×1254 笔触 PNG、加载 Shader AssetBundle，并惰性构建动作模板。Slash/Hit 会对整张笔触图反复逐像素处理，再上传多张派生纹理。近刺、飞剑、法阵也各有首次模板建立；仅缓存公共 library 不足以消除首播停顿。

`ModInitializer.OnInitializeMod` 注册自定义特效后，现在在主线程预备五种配方。每种配方使用固定种子创建一次纯视觉实例，推进早期和命中阶段，使用独立 64×64 RenderTexture/Camera 绘制，随后停止、隐藏并销毁临时对象。飞剑推进到 0.38 秒抵达点之后，覆盖确认命中闪光。预备不经过战斗适配器，不消费实际攻击种子，也不触发伤害、音频或震屏。失败配方可重试；公共资源加载失败不会永久关闭 library。预备异常不会阻断模组其余初始化。

## 性能与回归证据

本地 Unity 2019.3.15f1 / D3D11 代理，在同一进程、相同库中测得如下墙钟耗时（毫秒）：

| 配方 | 首次模板创建 | 预备后再次创建 | 首次 Play + 两次绘制 | 预备后 Play + 两次绘制 |
|---|---:|---:|---:|---:|
| Slash | 950.55 | 1.18 | 14.54 | 3.68 |
| Hit | 1049.10 | 1.23 | 9.18 | 4.96 |
| Pierce | 119.30 | 0.28 | 4.32 | 1.47 |
| Projectile | 2.28 | 0.28 | 1.78 | 1.33 |
| Portal | 0.54 | 0.08 | 1.90 | 0.78 |

公共笔触解码约 20.86 ms，Shader 包和资源加载另记在 `output/christasha-first-play/benchmark2/timing.txt`。这组数值证明主要首次同步成本存在，且缓存后的创建显著变短；代理 Editor 耗时不能当作真实游戏帧时。

`output/christasha-first-play/visual-frame-loop/complete.txt=PASS`，Unity 日志包含 `CHRISTASHA_FRAME_LOOP_PASS`。主线程查看早段、峰值、接触、退场及未命中帧；独立预览复核 PASS。Slash、Pierce、Projectile、Portal 及已比对的左右命中/未命中帧与上一交付逐字节一致。Hit 的 10 张中段图有局部粒子亮度差异，轮廓、落点、材质和时序无可见退化。MassRecipes 源快照的哈希差异仅为换行符。

托管接入契约 258 项 PASS，真实游戏 Managed API 编译 PASS；Release 构建 0 错误、7 项既有警告。新旧 Portable PDB 均为 94 份源文档，仅生产适配器、`ModInitializer` 和生成的 AssemblyInfo 文档校验和变化。

## 部署与剩余验证

DLL `BC5EF6FCDFC8F10F768E027FA6EA389CC687EB89EC593C84B1622A50186A447E` 已更新到仓库模组包及实际游戏目录，并核对 C 盘 Steam 链接入口哈希相同。旧版备份和收据在 `output/christasha-first-play/deployment-20260925T093611Z/receipt.json`；Shader AB 未改。

曾冷启动游戏以核对启动日志，但该次仅进入游戏资源加载，没有执行到 `ModInitializer.OnInitializeMod`，随即正常关闭。因此仍需在实际模组加载后的第一场战斗观察 `Steria.log` 的 `Christasha combat preparation: 5/5` 和首次斩、打、近刺、飞剑的游戏帧时；不把 Unity 代理的耗时写成实机结果。
