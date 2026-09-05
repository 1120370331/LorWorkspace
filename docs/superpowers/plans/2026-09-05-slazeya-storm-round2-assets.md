# Round 2 实现审定与资产接口

最终状态：Round2.6 视觉独立 PASS，音效另行接入，主线程最终32项运行验证和15文件同步已完成；见 [最终交付](2026-09-05-slazeya-storm-delivery.md)。

主线程批准 `../specs/2026-09-05-slazeya-storm-mass-round2-design.md`。GatherDuration=1.35，TailDuration=1.20；Bundle、prefab、canonical controller 公共接口不改。输出到 `preview_exports/slazeya_storm_mass/round2`，首轮参考只读。

用户随后明确授权增加「闪电闪烁、浪沫造成的雾气效果增强打击感」。正在形成有边界的补充规格；与水幕平滑法线、材质峰值、喷沫重复图案的返修一同交付，不取消最终游戏目录同步。

补充规格 `../specs/2026-09-05-slazeya-storm-round2-lightning-addendum.md` 已由主线程批准：B0–.05 主闪、.05–.10 暗间隔、.10–.15 次闪；新增 CrestLiftMist、调整 ReturnMist 两簇落点，雾覆盖以 A 为准去掉重复 R 衰减，复用冻结体积 atlas。所有新增层在 B+1.20 内结束，三项水体返修仍必须完成。Chandrasekhar 统一实施、构建并交付明确稳定候选，随后才做最终评审冻结。

## 并行所有权

- Chandrasekhar：canonical controller、Unity Editor builder／shaders／import、source verifier、构建／媒体导出和专属 Unity 原生预览。保留已有 FarAreaEffect 游戏桥接。不得编辑纹理制作源与输出。
- 纹理实施 worker：仅 `SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_textures.py`、`SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/**`。不得编辑 Unity 工程、controller、构建／部署或媒体脚本。

## 资产接口（固定）

PNG 全部使用线性数据导入（sRGB=false），UV U 沿主浪流向、V 从浪脚指向浪唇。atlas 为 PNG 左上角开始的行优先顺序；第一行帧 0–3。每格至少 12px 透明 gutter，不允许可见边缘采入相邻格。调用方须原生确认播放方向。

| 文件（位于 source_assets/round2） | 尺寸 | RGBA 语义 |
| --- | --- | --- |
| water_body_rgba.png | 1024×512 | R 宽面水体密度，G 不均匀泡沫团，B 独立的方向破裂阈值，A 覆盖／厚薄窗口。U 平铺连续，V clamp。不得有贯穿面等距细线。 |
| water_normal_roughness.png | 1024×512 | RGB 标准切线空间法线编码到 0–1，A 粗糙度。法线来自同一水体中尺度高度场。U repeat，V clamp。 |
| water_flow_rg.png | 512×512 | RG 编码到 0–1 的缓变流向扰动，B 中尺度高度，A=1。U repeat，V clamp；shader 基础流向仍沿 U。 |
| foam_spray_4x4.png | 1024×1024 | 16 帧连续薄膜→水指→分离滴落。R 水膜密度，G 泡沫／水滴，B 局部亮面，A 覆盖。三通道是数据而非成品颜色。 |
| mist_density_lighting_4x4.png | 1024×1024 | 16 帧相同种子的团块旋卷／扩散／消散。R 密度，G 上／前受光，B 底部／内侧遮蔽，A 柔软覆盖。 |
| droplet_spindrift_4x2.png | 1024×512 | 8 个头重尾细水滴、弯曲水丝、泡沫碎片变体；RGB 灰度受光，A 形状覆盖。 |

纹理 worker 输出原图、可视 channel/atlas 板、生成种子与语义 manifest；亲自目视确认，不能把任意噪声叫作水纹。不得用无关外部资源。builder 从这些源 PNG 复制／导入 Unity Assets/Textures/Round2，设置 wrap/filter/atlas 采样和完整引用。

## 交付要求

美术造型、法线与动态遵守已批准规格；不能仅加层数／亮度／噪点宣称完成。主／次浪的卷脚、浪腹、肩和卷唇须连续形变；体积来自真正曲面和相符法线。各 worker 使用同一工作树，不覆盖其他人改动；自检限于本域，最终原生预览／独立评审和游戏同步由主线程统筹。

## 资产门禁

Dalton 已交付并冻结六 PNG，source pack 标识 `3b7ad7a97ebe4222d28f8d7066db5714a4290e465bdd3c7f51c1c998d5db6e8e`。主线程亲自检查水体 channel 板、喷沫／雾 atlas 和生成源码，并在最终 manifest 同步后亲自运行 `--verify` PASS；worker 的 `--verify-repro` 证明六 PNG 可逐字节重现。素材接口 ACCEPT，组合视觉待评。Dalton 已释放，可按所属纹理面恢复。

## 初次组合预览返工

主线程与 Zeno 均判组合视觉 REVISE：硬折面／卷唇、暗蓝水幕主次、重复小王冠喷沫。`round2/attempt1` 为只读问题参考；归档时根目录正生成新候选，报告依据的 manifest 为 `86AF8591…`，不得把该报告或图像作为最初 `B930E646…` 的精确正向验收证据。两者都未获 PASS。后续在 worker 明确交付稳定候选后再冻结输出和 hash，评审期间暂停自动再构建。
