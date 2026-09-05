# Round 2 喷沫轮廓原子返修交付

2026-09-05。**新喷沫 PNG、生成器、manifest 已冻结；供主线程通知消费者接入构建。** 本报告替代旧 `asset-report.md` 的当前候选标识与喷沫说明。其余五张 PNG 字节不变。

## 冻结文件

- 新喷沫完整路径：`C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/foam_spray_4x4.png`
- 新喷沫 SHA-256：`49497b48f47d786502d53dbc6c0c0cb34c61e2ddaece767aa8bdfac9358a9385`
- 制作源完整路径：`C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_textures.py`
- 制作源 SHA-256：`d8dc142d13075372c9b66d98aeffb1a8620869b850568987cbbed3974e646f39`
- Manifest SHA-256：`39dc3f2a3a7a3107ee127b26255f7312de8a2810e2712cd9f1db655986d9425e`
- 当前六 PNG 包标识：`d846eaa6870123531c2e7cce01534cc57928222860ae56f9804f71a559a6b7d0`。算法仍是按文件名排序六行 `SHA256  filename\n` 后求 SHA-256。

单文件 hash 汇总已更新到同目录 `SHA256SUMS.txt`，机器可读语义和校验结果在 `manifest.json`。

## 修正的可见问题

先亲自查看 `preview_exports/slazeya_storm_mass/round2/attempt1/battlefield_05_maximum_crown.png` 及 `independent-review.md` 第 3 项。该目录 manifest 实际对应 Bundle `86AF8591A85C4891B70543B22AC27C6D3B60AC53DFE0BFEB1166626393B4331E`，使用的旧喷沫 hash 为 `4127f26fe4a718845bbbb386806dd3263426a11787a6f52b7988dfb4d8708b17`。

旧轮廓的横向盆底与多个直立水指，在原生峰值中反复读作小王冠／小喷泉。新轮廓改为从左下向右上沿弧线甩出的单片水舌：一个较长的受力前沿、两处很短且不等权的分叉、薄而破碎的尾缘，不再使用横向盆底或围成冠状的尖齿。孔洞在同一膜内坐标上扩大，随后形成三个空间、质量与分离时间不同的带尾重滴。

主种子仍为 `20260905`；喷沫使用固定 `+300/+301/+302` 场偏移，FBM 子层继续使用 `+ octave*101`。破孔、伸展与分離按连续年龄函数计算，分离继承对应水片尖端的位置和速度。

RGBA 语义未变：R 水膜密度、G 泡沫／水滴、B 局部亮面、A 覆盖。1024²、16 帧、PNG 左上起行优先、每格 256²、12px 透明 gutter、线性数据、U/V Clamp、末帧 A=0 均保持原接口。

已通过 `view_image` 亲自检查新 `inspection/foam_spray_4x4_atlas.png` 与 `inspection/foam_spray_4x4_channels.png`，并与上述原生失败图比较。新前中帧可辨单向斜弧水片，未再看到水平盆底与成排尖齿。这是源轮廓检查结果，**未声称新资源已经通过 Unity 组合视觉评审**。

## 自检结果

```powershell
py -3.12 SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_textures.py --verify
py -3.12 SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_textures.py --verify-repro
```

- `--verify`：**PASS，exit 0**。最终输出在 `verification.json`；manifest 的生成器 hash 与物理文件一致。
- 稳定版本的 `--verify-repro` 仅运行一次：**PASS，exit 0**，输出 `all six regenerated PNGs are byte-identical`；六图只在内存重生成，未重写其余五张 PNG。
- 喷沫 RGBA 范围分别为 `[0,203] / [0,255] / [0,181] / [0,235]`，尺寸及数据语义检查 PASS。
- 每格 12px gutter 的 RGBA 最大值 **0**；额外第 16px 内边界 A 最大值也为 **0**，没有支撑轮廓被边界切断。
- 相邻帧 A 全格平均绝对差最大 **0.0112**。非空帧 A 余弦相似度前段约 **0.8688–0.98**，末段下降水滴最低 **0.227**；最后一帧全透明。该指标用于发现跳变，不替代真实尺寸、寿命下的插值观感检查。
- AST 比较确认仅改动 `foam_frame`、`boards`、`manifest` 三个函数；`boards` 增加只更新传入图集检查板的能力，避免写其他素材画板。其余生成函数与校验、主命令逻辑未变。

## 其余五图字节不变证据

返修前记录在 `foam-rework-baseline.json`，返修后实际读取文件重新计算 SHA-256，逐项与基线相同；证据保存于 `foam-rework-integrity.json`。本轮只写了喷沫源 PNG，另外五图未重写。

| 文件（同一 source_assets/round2 目录） | 返修前后相同的 SHA-256 |
| --- | --- |
| water_body_rgba.png | `384c69a438ff279cb70ac40951fb2375a96a6e8ab9b988037173ef9a6d12930e` |
| water_normal_roughness.png | `1a395a68babe027e0892fda1d6133a44e94eaf0e418570bd60cb0795e844ddf8` |
| water_flow_rg.png | `8706ad8c97d1f2dadf09c0cee7d83f1b26993e2b648b8567237629d8d121e56d` |
| mist_density_lighting_4x4.png | `48f9edadd9ac23c63b813b53576207227740ed528f94694f8ff256ca3399e029` |
| droplet_spindrift_4x2.png | `e12d1835f191bb04f62c6ef4f6ce75485339fbfc44a8d578972a42373b7abc6c` |

## 原生组合待验证项与交接

单片水舌的最大 alpha 面积约 2266 像素当量，旧盆状喷沫约 7723；面积降低是去除完整喷泉图案的直接结果。实际颗粒密度、亮度分量、发射朝向及新加浪沫雾能否充分接续主浪唇，交给主线程和消费者在同一冻结候选的原生预览中检查。后段孤立重滴仍需检查实际粒子寿命下的双帧插值。

本 worker 没有改 Unity、shader、controller、build 或媒体逻辑，没有启动 Unity、部署、提交或派代理。资源已就绪；由主线程转告消费者使用上述新喷沫 hash，并负责通知构建与安排组合视觉评审。
