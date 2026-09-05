# Slazeya Storm Mass Round 2：纹理资产交付

> 历史首轮冻结记录。当前喷沫已完成“小王冠”轮廓返修；新候选、完整 hash、验证及五图不变证据请读 [foam-rework-report.md](foam-rework-report.md)。下文旧喷沫与旧包标识不再代表当前候选。

2026-09-05。六张源 PNG 与生成器均已冻结。`--verify` 和独立重生成 `--verify-repro` 均 exit 0；后者确认六张 PNG 逐字节一致。此前主线程遇到的 `generator changed since generation` 是末次修订期间旧 manifest 未更新所致，最终 manifest 已同步，并再次通过 `--verify`。此后没有修改生成器或六张 PNG。

六 PNG 候选标识：`3b7ad7a97ebe4222d28f8d7066db5714a4290e465bdd3c7f51c1c998d5db6e8e`。计算方式是对按文件名排序的六行 `SHA256  filename\n` 再求 SHA-256；单文件完整 hash 见下表及 [SHA256SUMS.txt](SHA256SUMS.txt)。该文本也记录生成器和 manifest 的 hash。

## 完整路径与固定 SHA-256

| 原图完整路径 | 尺寸 | SHA-256 |
| --- | --- | --- |
| `C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/water_body_rgba.png` | 1024×512 | `384c69a438ff279cb70ac40951fb2375a96a6e8ab9b988037173ef9a6d12930e` |
| `C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/water_normal_roughness.png` | 1024×512 | `1a395a68babe027e0892fda1d6133a44e94eaf0e418570bd60cb0795e844ddf8` |
| `C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/water_flow_rg.png` | 512×512 | `8706ad8c97d1f2dadf09c0cee7d83f1b26993e2b648b8567237629d8d121e56d` |
| `C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/foam_spray_4x4.png` | 1024×1024 | `4127f26fe4a718845bbbb386806dd3263426a11787a6f52b7988dfb4d8708b17` |
| `C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/mist_density_lighting_4x4.png` | 1024×1024 | `48f9edadd9ac23c63b813b53576207227740ed528f94694f8ff256ca3399e029` |
| `C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/droplet_spindrift_4x2.png` | 1024×512 | `e12d1835f191bb04f62c6ef4f6ce75485339fbfc44a8d578972a42373b7abc6c` |

制作源完整路径：`C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_textures.py`。

制作源 SHA-256：`1ae13de2101f345e4c2ba46ce786b293fdcd7ce31c494f3b829cc51872eff211`。

机器可读接口与种子：`C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/manifest.json`。

## 制作及通道

依赖：Python 3.12、NumPy 2.2.6、Pillow 11.3.0；主种子 `20260905`，各领域使用固定偏移，FBM 子层使用 `+ octave * 101`。全部为自有数学场，不含外部下载或复制素材；没有调用图像生成服务。

| 资产 | RGBA 数据语义与制作方法 |
| --- | --- |
| Water body | R 宽面密度、G 泡沫团、B 独立方向破裂阈值、A 覆盖与厚薄窗。多尺度 domain warp、五组宽窄不同且局部断续的折流与三处分叉汇合；泡沫集中在肩／唇且带孔隙。 |
| Normal / roughness | RGB 标准切线空间法线；由上述同一中尺度高度的 UV 梯度派生，PNG Y 梯度反号对应 +V 向浪唇；泡沫区减弱微法线。A 粗糙度。 |
| Flow | RG 为缓变扰动，解码 `RG*2-1`；不是完整流速，消费者基础流向仍沿 +U。B 同源中尺度高度，A 恒 255。 |
| Foam spray | R 水膜密度、G 泡沫／重滴、B 局部亮面、A 覆盖。固定膜内坐标持续拉伸、侵蚀开洞；六组不等宽水指的尖端在分离时继承位置与速度，再沿重力轨迹下降。 |
| Mist | R 积分密度、G 上／前受光、B 下部／内部遮蔽、A 柔覆盖。对同一个三维团块场做逆旋卷／膨胀取样与 28 层射线积分；定向梯度和上游密度提供受光／遮蔽。 |
| Droplet / spindrift | RGB 相同的灰度受光，A 轮廓；5 个头重尾细的水滴／曲丝及 3 个破碎泡沫变体。它们是变体选择图集，不是连续动画。 |

固定导入约定保持不变：全部 `sRGB=false`，RGBA 为 straight data，源图不预乘；水体三图 `U Repeat / V Clamp`，PNG 顶边对应浪唇 +V。图集 `U/V Clamp`，4 列、每格 256²，PNG 左上起行优先，首行 0–3，每格 12px 全透明 gutter。推荐 Bilinear、禁 mip、无压缩；实际导入仍由消费方负责。16 帧图集 `t=frame/15`，非循环，最后一帧 A=0。

## 已完成自检

所有命令从仓库根目录执行，未启动 Unity、构建、部署、提交或派代理。

```powershell
py -3.12 SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_textures.py
py -3.12 SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_textures.py --verify
py -3.12 SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_textures.py --verify-repro
```

- 生成与数据检查：PASS，6 张 PNG、8 张 inspection 板、manifest。
- `--verify`：PASS，检查尺寸、数据范围、U 接缝、V alpha 边界、gutter、法线长度、流图 A、滴沫灰度及 manifest／文件 hash；最终输出保存于 `verification.json`。
- `--verify-repro`：PASS，`all six regenerated PNGs are byte-identical`；只在内存重生成，不重写冻结图。
- Python AST 语法检查：PASS。
- 三张水体图的两侧 U 边 RGBA 最大差值：**0 LSB**；水体图顶／底 A：**0**。双拼通道板也已目视检查，没有看到 U 断口。
- 法线解码长度最大误差：**0.005042**；密度 R 与独立破裂 B 相关系数 **-0.0455**，没有复用成同一噪声。
- 三个图集每格 12px gutter 的 RGBA 最大值：**0**。额外查看第 16px 内侧支撑边界：喷沫／滴沫 A 最大 0，雾最大 **20/255**，雾边缘已有柔化，仍需原生过滤确认。
- 喷沫相邻帧 A 的全格平均绝对差最大 **0.05004**；雾 **0.03202**。雾非空相邻帧 A 余弦相似度 **0.9961–0.9982**。喷沫后期分离重滴在下降，相邻轮廓最低重合相似度 **0.2567**；这不是逐帧换随机种子，仍须以实际粒子尺寸／寿命确认帧间插值是否充足。第 15 帧全透明不参与余弦计算。

已亲自通过 `view_image` 检查 `inspection/` 中的水体通道板、U 双拼板、喷沫／雾／滴沫的 atlas 板与通道板。检查包含 256px 原生格以及 112px 通道缩图、水体 128×64 缩图。修正了首轮细杆式水指和水滴硬黑白受光；最后一轮未继续扩展美化范围。画板是离线数据诊断和着色示意，**不是 Unity 原生效果或最终 AAA 验收**。

## 留给组合原生预览的具体事项

1. 确认导入后的图集首帧位置、播放方向、贴图过滤及实际内存／压缩设置。雾靠近内侧 gutter 的低 alpha 支撑需要在双线性采样时查看是否出现矩形截边。
2. 喷沫后段孤立水滴帧间位移较明显，消费者应按真实粒子年龄采样，并检查双帧插值在目标寿命与尺寸下是否产生重影或顿挫。
3. 验证变形 mesh 的 tangent handedness、+V 方向、实际粗糙度／法线强度。源数据标准编码正确不等于消费 shader 的 TBN 已经正确。
4. 雾末帧保留 RGB 密度／光照数据但 A=0；最终 shader 的输出 RGB 必须受覆盖／生命周期约束。实际材质色调、预乘次数、透明排序、overdraw、峰值白化与角色可读性由组合预览验收。

最终 Unity 材质／粒子、Bundle 回读、玩法和部署均未在本纹理任务中验证。写入仅限约定的生成器和 `source_assets/round2/**`；未改消费方、回滚或覆盖并行工作。
