# 汐音原生视觉配方（制作中的候选）

源码：`SteriaBuild/ChristashaNativeMassRecipes.cs`。它只组合视觉，不管理伤害、角色、音频或镜头。旧五系统配方不再作为此类效果的美术基准。

`CreatePrototype("Cast"/"Area"/"Point", parent)` 返回 inactive 实例；调用者拥有实例和传入的原创可读 brush。库拥有 inactive 模板、材质、派生局部纹理与局部 mesh。`Dispose` 前须清理所有实例。模板缓存只在 `Application.isPlaying` 时 `DontDestroyOnLoad`。

实例以 `__ChristashaNativeMass_*` 子节点识别配方，根名可由适配器修改。主库唯一时钟调用 `BeforeAnimator → Animator.Update → AfterAnimator → PS.Simulate → AfterParticles → 零时长同步`；本文件没有另开计时器、MonoBehaviour 或再次推进原动画。

## 本轮来源与分工

| 配方 | 原生路径 | 选择与变化 |
| --- | --- | --- |
| Area | `prefabs/battle/diceattackeffects/new/fx/mon/xiao/fx_mon_xiao_areatrail1` | 保留 `Trail` 主torus、`Trail2` 与 `Trail_Wave` 异速副波、`Spark1/2`；去掉火舌、Burn、Distortion、额外同重环。主波有纵深倾转和原mask侵蚀，施放后0.2秒展开，末段消散为泡丝与开放弦线。 |
| Cast | `prefabs/battle/creatureeffect/new_illusioncardfx/4_n/fx_illusioncard_4_n_orchestra_start` | 取 `Line1/2` 原size/rotation/色动画，运行时开掉原纹理一大段并重映白金，尺寸收至施法者附近；两根开放细弦补足向前的起势。去掉其它完整谱环、大圆点和场景层。 |
| Point | `prefabs/battle/diceattackeffects/new/fx/pc/rolrang/fx_pc_rolrang_glovetrail_cross` | 只取 `Trail1_cross` 短核心，0.065秒寿命，新增本项目局部brush的短泡丝；不引入原空间拖迹、大烟或拳套动作语义。 |

本项目不重新发布上述原始游戏纹理、Shader、Mesh或导出研究bundle。`source-inspection/` 是本地忽略的只读诊断输出。唯一传入的原创位图沿用已生成的 `../Assets/tide-brush-imagegen-v1.png`，被当作局部碎浪笔触，不是整张特效。

## Xiao通道核实，不能按纹理名猜配色

候选01将原颜色的最大通道当作每个白金通道的能量，主波过曝为白块。02降低颜色后仍像金箔。使用 `inspect-shader.py FX_APF_Mon_Xiao_AreaTrail` 对实际D3D11 pixel-0-2反汇编，核实：

- `t2/s0 = _Main`，片元最终只读取 `_Main.g`，与 `_Color.rgb` 相乘。将原色纹的 `max(R,G,B)` 写入G会抹掉原版绿色通道内的亮度变化；同一张纹理有红火像素，并不意味着此Shader直接输出原RGB。
- `t1/s1 = _Mask`，只取R；`t0/s2 = _Noise`，只取R；粒子 `COLOR.r/g` 控制覆盖/侵蚀，不是可随意染金的RGBA颜色。
- constant buffer offset64为 `_Color`，offset96为 `_DissolveColor`。原动画实际主色 `(2.5,.9,.3)`、溶解色 `(1,0,0)`。黑底/主体/切锋不能靠统一三通道max强度解释。
- 03保留主纹理的原G，不派生或覆盖它，改造动画输出色阶；其它实际RGB纹理按亮度转白金。派生只在缓存初始化发生，非逐帧读取像素。每帧先恢复原MPB后采动画再映色，避免常量通道反馈。

## 事件与空间合同

这只是当前候选的艺术数值，尚待主线程上下文验收后固化：

- 根坐标的攻击传播方向是 `+X`；Area原点是敌阵艺术中心附近，正向展宽。`AreaDesignWidth=32` 为给适配器的目标宽度基准，最终视觉像素边界仍需本轮量测确认。不得读取 `BattleUnitManager` 偷窥阵形。
- 原生Area内部艺术变换 `(6.4,7.6,6.4)`，内部平移 `(9.5,2.4,0)`。适配器应只对最外实例做宽度/朝向换算，不再附加原 `SetScale(2*roll)`。
- 总战斗时间0秒Cast；0.3秒Area开始；Area自身age0.2秒达到展开峰值，对应总0.5秒的既定释放门。Point独立世界点，只由适配器从成功目标列表生成。
- Duration原始秒：Cast0.84、Area1.40、Point0.58；尾段可见弦线/PS分别延至约0.78/1.32/0.48秒（相当于旧时长下 Cast速率约0.76、Area约0.82、Point约0.72），主driver负责按播放速度换算并清理。Area的0.3开始、0.5释放门不变；Point不会主动制造命中或震屏。

## 重放

仅后台隐藏Unity **2019.3.15f1**，D3D11/Built-in/Gamma，120Hz手动时钟、60fps输出，无Bloom。不得启动游戏或Steam、不得碰用户桌面。warm项目需主线程/当前owner交接租约。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/ChristashaNative/Mass/render-mass.ps1 -Output C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/ChristashaNative/Mass/evidence/candidate-03
python SteriaBuild/VFXSource/ChristashaNative/Mass/package-mass.py SteriaBuild/VFXSource/ChristashaNative/Mass/evidence/candidate-03
```

runner默认使用共享主库已冻结的 `shared-mass-hook-base/ChristashaNativeParticleLibrary.cs.txt`，避免并行Hit开发修改预览输入。每轮保留确切源码快照/SHA；取消与replay检查包含粒子、MPB和Transform，此外核对原始源Transform/PS/Material未变与Play不增殖层级。编译/重放PASS不代表美术接受，更不等同于实际战斗录像。
