# 汐音：光耀万海 V3

书页 `9009006` 的 `FarAreaEach` 继续使用 `Steria_ChristashaXiyinV2`。出手首拍建立共享潮场，原版群攻管理器在 750 ms 取得结算权，再按成功名单为每个目标生成独立的腰侧浪击。退场沿同一个旋眼收流。

两道主浪、旋眼、五线谱及正反受击层来自定稿的有限厚度水体模拟。发布纹理由恒定色板求出水体颜色和透射率，背景、角色和字幕在游戏中独立绘制。入流是动态网格，按战场跨度、受击者当前轮廓与来流方向布置。运行时用透明层近似折射和相互遮挡。全程使用游戏原有音效、骰子效果资源和结算流程。

本机定稿源为 `preview_exports/xiyin_golden_tide/v3/xiyin-golden-tide-v3.mp4`，SHA-256 为 `d022ff6ffd305b5775db3fbacf0ceec5177a503ba5926b1d953aebf2806b7521`。同目录以及相邻 `v2` 和 `art` 保存烘焙所用的原始模拟与材质。日常运行使用 `Resource/Effects/ChristashaXiyinV3` 中已烘焙的图集。

## 复核

本次从已安装的星芒近刺版本 `output/christasha-starlance/release-02` 重建，使用该 DLL 对应的 PDB 确认源码。`bake_layers.py` 从定稿的本机源生成图集，`package.py` 复用前一次发布工具隔离构建、确认 PDB 和部署，`native_replay.py` 用隔离的 Unity 2019.3.15f1 与游戏程序集复跑。重建需要先前的本机发布快照和定稿模拟源。聚合 DLL 和 PDB 已生成到本机发布包，Git 提交聚焦本次源码和资源。

`output/christasha-xiyin/native-01` 保存 60 fps 原速回放、逐帧画面和 21 项原版管理器检查。回放使用真实动作图、原版 `EffectPhase` 和 `ActionPhase`，拦截并计数管理器的伤害调用，跳过战斗 UI 和结束回调的其他逻辑。完整接待实战未覆盖。

`output/christasha-xiyin/deployments/20261118T085548Z/receipt.json` 记录游戏安装、仓库副本、C 盘 Steam 别名校验和旧 DLL 对应备份。候选 `release-02` 的 DLL SHA-256 为 `982472f4c41497f7e6bb3c3740e9e33274071fb1b4c188f2271be1abe75b1593`，PDB SHA-256 为 `a34418c46862e7b3ab68ff4cfc1367232852dc709e3b1869ea3d22af29f2df30`。
