# 安希尔贴图攻击特效独立视觉验收

## 1. 结论：PASS

当前冻结候选 `6527f84a4170ba224e54b54aa475714c3dc7c2046d1a9474269f29ada82ebf31` 通过独立视觉验收。六个 profile 的主题、轮廓、接触时机、方向感、目标/防御锚点和退场方式均贯彻用户原始意图，没有发现必须返修的视觉缺陷。

本次验收只覆盖 Unity 2019.3.15f1 隔离预览中的生产渲染核心与最终 PNG/JSON 资源，不等同于完整游戏战斗 UI、BattleController、相机、音效和原版胜负反馈验收。

## 2. 对原始用户意图的贯彻程度

- 已摆脱旧版“一张 AI 图整体缩放”的问题：六类效果在 `00.png..30.png` 与 `phase-contact.png` 中都有起始、接触、完整、退场和结束碎片/消散阶段；不是整张慢淡入淡出。
- 书页主题清晰：海潮使用蓝色水体、泡沫和水压边缘；回忆使用黑紫实体笔触加局部橙红燃烧。寒地后勤归入回忆的视觉语汇没有被海潮化。
- 攻击类型可区分：SeaPierce 是细长水锋，SeaFarHit 是远程压缩水束加目标水花，MemorySlash 是低矮横斩，MemoryPierce 是窄楔形刺入，MemoryHit 是局部爆点，MemoryGuard 是防御者身前的竖弧护势。
- 用户最新拒绝点“太圆滑、没力气”已被 SeaFarHit v2 回应：最终帧里锋头、撕裂水带和目标端水花都明显尖锐有冲量；观感更接近高压水击，不再是圆滑水卷或泪滴飞溅。个别水束尖角有轻微冰裂联想，但连续水纹、泡沫碎片和目标水花仍把它读成水压，而不是冰锥。

## 3. 用户体验问题

未发现必须返修的体验问题。

- 默认首帧可见：`video-tiles/000.png` 已显示所有六类效果的初始方向，符合 1/60 秒首帧保留要求。
- 接触反馈快：`video-tiles/001.png` 和 `002.png` 在 0.017s/0.033s 已形成接触亮峰，正常速度下不会显得拖沓。
- 目标锚点合理：攻击类效果都在目标相对位置形成接触或爆点；MemoryGuard 停在防御者身前，没有滑向攻击目标。
- 退场可读：`video-tiles/011.png`、`015.png`、`025.png` 显示主体先收束、尾迹/碎片后散去；没有大块透明气泡、绿底边、矩形边、整片复制壳或明显裁切直缝。

## 4. 竞品参考差距

与原版参考 `theme_ink_purple_001.png`、`theme_fire_001.png`、`theme_blue_001.png` 对照后，当前候选达到同类成熟效果的合理预期：

- 回忆系继承了原版紫黑墨迹的实体笔触密度，并用橙红局部火舌补足燃烧感，没有退化成普通规则火焰链。
- 海潮系比原版蓝色回旋参考更贴合本次书页：SeaPierce 保留水体流动，SeaFarHit 则更像冲击束，避免了原版蓝色环状素材在远程打击上可能带来的“绕圈装饰感”。
- 当前缺口主要是完整游戏内的音效、受击反馈和相机语境还未验证；这属于最终游戏路径验收，不是本轮视觉预览的失败。

## 5. 技术债和解耦问题

本轮只做视觉验收，未审查生产源码差异。基于可见资源与预览证据，没有发现由视觉实现暴露出的明显技术债：

- 预览显示六类 profile 各自有独立主体与 accent/水花职责，没有把同一张大图当作所有攻击的全局替代。
- `facing-and-body-contact.png` 的浅底、反向和 body-only 视图中，黑紫主体保留可读性；说明不再依赖会吞掉暗部的亮度抠图观感。
- `checks.txt` 报告每类不超过 3 个 renderer、共享解码缓存、并发 phase/color 隔离、结束禁用 renderer；这些是实现者证据，我未把它当作视觉 PASS 的唯一依据，但它与视觉观察一致。

## 6. 回归风险

- 中等风险：完整游戏战斗路径未演练，真实 BattleController、成功防御分支、音效、原版 hit effect 叠加和镜头语境仍可能暴露锚点或遮挡差异。
- 低风险：SeaFarHit 的锐化修订接近“冰裂”边界；如果实机背景更冷、更亮，可能需要微调泡沫/水滴比例来保持水感。
- 低风险：SeaPierce、MemorySlash、MemoryPierce 在 0.067s 到约 0.100s 有短暂停留，当前不影响正常速度读感，但实机若叠加角色位移，可能显得略静。

## 7. 证据：命令、截图、日志、路径、复现步骤

候选身份：

- `SteriaBuild/VFXSource/AnhierTextureCombat/previews/candidate-hashes.json`
- candidateId: `6527f84a4170ba224e54b54aa475714c3dc7c2046d1a9474269f29ada82ebf31`
- manifest SHA: `eb6def742be2097a2856306def9296f069b6176c7b1409139a1ce8f8f000db46`
- MP4: `all-six-60fps.mp4`, SHA `f49769829ef6428f64b064887bb0fce2cf962ba4bede8a2e5442b30f0655db5e`
- GIF: `all-six-30fps.gif`, SHA `a5531c87efa1628d687c97b2d14e465c3670d1d77630af8c0b85aa99deacbe88`

实际查看的关键输入：

- `phase-contact.png`, SHA `594bbe74623d0ecc31893391bed197cbd831ebbc1e5da9cd74e1693aec745c98`
- `facing-and-body-contact.png`, SHA `3e047b5a460d953b7c6346a8ef7fded0aa54155f05d2308926c40e46ea4dcb16`
- `output/anhier-texture-vfx/card-art-contact.png`, SHA `63e586a2697d4d1b9b17b1613303c6a42509f16f601790a310569eddc139ac45`
- `output/anhier-texture-vfx/imagegen-sea/qa/SeaFarHit-v1-v2-small-scale.png`, SHA `76bf477f6f5a00e666272bd6d1ea009d92cef85e6bcfe87ab93c995bfa0826c4`
- native refs:
  - `theme_ink_purple_001.png`, SHA `18530ed5cc5cfdfdec66d4cf421076cedabefe0b1fc0d7ea93a3815c5e6c728c`
  - `theme_fire_001.png`, SHA `540e9bdd492954e65db5f5b588031cb8698b72c03477a5722c94bdea9e76aaa4`
  - `theme_blue_001.png`, SHA `116d3757dcba15b61047c353162858723c9fdd3916fe1c62c2e0f7def40870e8`

抽样帧查看：

- `video-tiles/000.png` 初始帧，SHA `aa2b2de1951573b4d0503ce9ea5dabfc37ead5cd000205d6ee711f85b7185438`
- `video-tiles/001.png` 0.017s，SHA `88fc3547bd9eb410dfe51e3461c1b8a7c8858ea87ac1fe5001e69cc92df715d9`
- `video-tiles/002.png` 0.033s，SHA `785112275224fb775b5b1320ec8eb9ba9c54533412bbc30a12cb9565a05d6257`
- `video-tiles/004.png` 0.067s，SHA `8fcb5d45f2019fd92c2410e67282eac08321cc2cb2da2ec424197af2bca32b82`
- `video-tiles/011.png` 0.183s，SHA `ee5d6675e5d377030fdedb5488a9b6e01a00f84192ce08faaeee50f47894a8b3`
- `video-tiles/015.png` 0.250s，SHA `cd36ac745e537baa9e67db8d1f80562e9751f739d9d40af7e4d5e4d4ad2d3efa`
- `video-tiles/025.png` 0.417s，SHA `dcb8c2adb1483fa748983363b0c6ee018cd0ccb30f46279d9adc39f251104cde`
- `video-tiles/030.png` 0.500s / loop clear，SHA `7761ee0b990fd3cabf0373600d686f148285e8782034cc2da2fb96f51845157a`

命令证据：

```powershell
Get-Content -Raw AGENTS.md
Get-Content -Raw docs/superpowers/plans/2026-09-09-anhier-texture-attack-delivery.md
ffprobe -v error -show_entries format=filename,duration,size:stream=codec_name,width,height,r_frame_rate,avg_frame_rate,nb_frames -of json SteriaBuild/VFXSource/AnhierTextureCombat/previews/all-six-60fps.mp4
ffprobe -v error -show_entries format=filename,duration,size:stream=codec_name,width,height,r_frame_rate,avg_frame_rate,nb_frames -of json SteriaBuild/VFXSource/AnhierTextureCombat/previews/all-six-30fps.gif
Get-FileHash -Algorithm SHA256 <reviewed files>
Get-Content -Raw SteriaBuild/VFXSource/AnhierTextureCombat/previews/checks.txt
```

媒体边界：

- `ffprobe` 证实 MP4 为 H.264、1280x1128、60fps、240 帧、4.0s；GIF 为 1280x1128、16 帧、0.8s。
- 我尝试用 browser/computer-use 直接打开本地 GIF，但被浏览器安全策略拦截；chrome-devtools 页枚举也因共享 profile 正在运行而不可用。因此动态判断基于最终 MP4/GIF 元数据、已解码 `video-tiles` 连续帧、各 profile `00.png..30.png`、以及 `phase-contact.png`/`facing-and-body-contact.png` 的实际视觉查看。

## 8. 必须返修项

无。

## 9. 可延期项

- 在真实游戏战斗中跑一次包含攻击、受击、成功防御、左右朝向的实机 smoke，确认音效、原版 hit effect 叠加、BattleController 锚点和相机语境。
- 若实机冷色背景让 SeaFarHit 过度像冰，可把尾部少量碎尖改成更湿润的泡沫/水滴边缘；当前预览不需要因此返修。

## 10. 建议下一轮任务

进入主线程最终集成验收：核对 18 个 EffectRes 绑定、部署哈希、资源清单和真实游戏代表路径。视觉预览层面可以作为 PASS 候选继续推进。
