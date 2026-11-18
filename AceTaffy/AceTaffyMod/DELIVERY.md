# AceTaffy 0.2.4 交付记录

## 2026-11-10 机制修订

- 每幕两张0费「临场发明」固定配备到速度最低的两颗速度骰子，其余书页仍按可用光芒随机安排。
- 每个机关的词条增益改为塔菲身上的独立实例：登场时与每幕开始时各+1层；机关被击败时移除该机关提供的全部层数，其他机关的层数保留。

验证候选：`Verification/Reception/output/20261110-161058`。真实Unity夹具 `RECEPTION_PASS invention-unit checks=465`，覆盖同词条机关独立增益、击败后整份移除、最低两颗速度骰配牌及光芒预算；静态包检查通过。DLL SHA256 为 `f6e27bd832c27efe9574ef3a373fa1045ba252681db0127fd2799fe12841f901`。实机接待仍需用户验收。

4费「1883式蒸汽爆轰」首骰新增专属压锋与破裂压弧。主体采用透明通道，压弧短促分离；喷口围绕实际窄尾缩放，左右及低、中、高倍率均保持挂接。其它骰子、机制、人物和原版声音沿用当前版本。

## 验证

候选为 `Verification/Vfx/ultimate-v1/20260928-153713-189-candidate/`，真实 Unity 工厂隔离检查通过，独立视觉复审 PASS。新源码 SHA256 为 `b32dbe9c8f786c746396a27902fca88b64314578ab79ab610b0d884d6183a00a`。本次复用匹配候选的 Release 编译与预览证据；不重复运行无关机制测试。

模拟读取原版挂点与当前人物参数，未重放完整游戏 MoveRoutine，也未启动用户游戏。实际接待由用户验收。

## 材料与复现

- 动态预览：上述候选目录的 `ultimate-normal-60fps.mp4`。
- 审查记录：`ArtSource/Effects/ultimate-v1/preview-review.md`。
- 内置 image_gen 素材与完整提示词：`ArtSource/Effects/ultimate-v1/`。
- 复现预览：`Verification/Vfx/run_ultimate_preview.ps1`。
- 发行包：`dist/AceTaffy-0.2.4.zip`。
- 发布清单：`Verification/release-manifest.json`。
- 旧记录：`DeliveryHistory/v0.2.3.md`。

重启游戏后加载新效果。
