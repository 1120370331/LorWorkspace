# Hit：白金刀剑纵砸候选

当前 `evidence/hit-slam-candidate-03/` 待独立审查。用户明确指出 Hit 是“比斩击横向更有力的刀剑纵砸”，不是拳击一个点。旧 `hit-candidate-01` 至 `04` 已在 `rejected-fist-hit-direction.json` 作废，不能作为生产或验收基线。

## 源骨架与真正主动作

唯一原生来源为 `FX_PC_RolRang_Greatsword`。保留 `Main_Trail1`、`Main_Trail2`、`SwingTrail` 原三段主刀载体、父 Transform 和 Clip。Main1 原角速 43.633 rad/s、Main2 8.727 rad/s，寿命 0.7/0.5s；SwingTrail 的原负向角速和 0.15s 后生收势保持。没有使用 Slash11 整体转 90°，没有替换成拳击扇，也没有从 Glove 加主星核。

主拖迹 0–0.15s 从上方来到左下；0.15s 到达原生 `Ground_Light1` 接触组（原生 root 坐标约 -6.87,-2.5），随后小压波/碎光出现；0.25–0.6s 下段退为细丝。`Ground_Wave1`、`Upper_Particle`、`Upper_Particle_Start`、局部 Smoke 都是接触辅助。去掉石块、地裂、地面大片破坏。原 `Upper_Particle_Start` 的远端子偏移只在接触组内收回，没有把三个主刀系统拉到爆点。

## 材质与落点对齐

- Main1 使用与用户接受的 Slash11 相同的白金职责方法：原始包络采样、低透明白金余光、窄蚀刻锋。Hit 有自己的实例材质，未改变 Slash 的材质/brush/曲线。
- Greatsword Main2 原 Clip 初始 RGB 为黑，约 0.15s 才升亮；照抄它会产生黑大弧。Hit 专属映射让起刀即有白金窄锋，原后段升亮仍参与强度。
- 原 brush 的窄外缘覆盖只进入 Main2，并在 U 方向移动 0.24，让可见刀口沿保留的原重角速落到左下接触组。这个调整发生在局部覆盖纹理，不转动角色、不修改原 PS 旋速或 Clip。
- Outline Shader 的实际 alpha 含 `pow(1-v, _Mask)`。触地后将该指数从原值收至约 80，使宽度退为细白线；没有把不透明 RGB 降黑来假装淡出。Main1 的余迹仍沿原生侵蚀包络。
- SwingTrail 的原 Flash Shader 用 U 阈值收束（MaskPower 至少 7.2），避免原完整同心环占据画面；保留原 Mesh 和负向收势。
- 低位两侧碎箔/细丝在 0.15/0.185s 发射，使用原创局部 brush 和内置 Sprites/Default。它们不会在触地前替代主刀动作；没有伤害、镜头或音效调用。

## 原始数据对照与复现

原/主题两侧均是同一个 Greatsword subset；原侧保留原生放置/时序/材质，主题侧为上述改造。原侧不是此前被拒的拳爆子集，也不是完整游戏录像。

两侧均在 instance 外层加 `Euler=(-10,0,0)`，匹配 `FX_Script_AttackBasic.Initialize` 的 Greatsword 行为。工厂依旧返回 inactive、raw native units；没有把这层旋角藏入内部父系，最终适配器需要显式施加，不能重复。固定 camera ortho10、(-2,0,-100)、960×540、Gamma、60fps；这是孤立效果对照，不证明实际角色尺度已经校准。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/ChristashaNative/render-hit.ps1 -Output C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/ChristashaNative/evidence/hit-slam-candidate-03
python SteriaBuild/VFXSource/ChristashaNative/package-slash-evidence.py SteriaBuild/VFXSource/ChristashaNative/evidence/hit-slam-candidate-03 --recipe Hit
```

只用隐藏 Unity batchmode，4 个 job workers，自有 PID 设为 BelowNormal。没有桌面或游戏操作。

## 本轮观察与边界

已逐帧看 0.133/0.15/0.2/0.25s：刀口的下端在 0.15s 与接触组衔接，0.2/0.25s 的小波和低散跟随同一落点；解决了 slam01/02 的“头顶亮帽 + 远处爆点”。这不是美术 PASS，仍由主线程/独立 reviewer 判断力度、纵砸辨识度、落点与收尾；实际 Hit.png 剑尖/脚底的尺度与挂点由角色上下文验收。

Unity API 编译、实际 Shader 渲染、重播粒子/MPB 状态与清理通过。共享 hooks/dispatch 扩展后，`slash11-regression/comparison.csv` 的 17/100/283ms 三帧对用户接受的 Slash11 均为 changed_rgb_pixels=0、max_channel_delta=0。Slash11 未重新设计。

候选源码以目录内 `ChristashaNativeParticleLibrary.cs.txt` 为准；后续其他 recipe 扩展不能用活动工作树替代本候选身份。原始材料和 Mesh 均运行时借用玩家已安装的游戏；本目录不发布原始游戏资源包。
