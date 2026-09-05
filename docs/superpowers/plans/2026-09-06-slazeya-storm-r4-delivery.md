# 斯拉泽雅「倾覆万千之流」R4交付

2026-09-06，R4.1完成独立视觉复审、主线程原生运行验收及游戏目录同步。

## 用户结果

初始水泡共同包裹敌方全体；0.82–1.28秒，原水面、底盖和168个持久泡沫身份在三维空间收向阵型中心。1.32秒后只留一个中心亮核，实际群攻回调才使水流由核向外传播、展开成四股强弱不同的环向瀑布碎流。中心为初始化时的EnvelopeCenter，水核半径0.06H，所有可视支持范围限制为0.12H。角色原位不动，收缩阶段不再沿用R3的全体包含下限。

材质按用户参考图调整为钢蓝灰厚水、深涡沟、中间亮水和局部冷白翻沫；减弱了散落C形粒子的视觉权重。保留细锐双闪、暗间隔、低雾及两段技能音效，没有台词。G1.35、Tail1.20和默认伤害／防御、空名单、重复回调、取消合同不变。

## 精确候选

| 文件 | SHA-256 |
| --- | --- |
| Bundle及同名.ab | `1BAF40BAA81BE2A1B4E1A73E572138961EBB641BAEB56D7A1279B534F8A33AE7` |
| Controller及Unity镜像 | `00A5A0D29E48BB794E913C831BE2B54F47C5A55B51DBE5FE1F1AA98C5AB3A37D` |
| Steria.dll | `E51D4726EB6338A8A2611E68702623EB8FD07D41F2A925E5B194194651EA975B` |

Schrodinger负责本轮设计；Carson实现与生成原生预览；Zeno独立视觉评审；主线程检查代码、原尺寸代表帧、运行与部署。设计见`../specs/2026-09-06-slazeya-storm-center-collapse-design.md`。

## 验收与边界

- 独立视觉PASS：`preview_exports/slazeya_storm_mass/round4/review-1baf40ba/independent-review.md`。核心、B0、材质、下泻与清空的相应采样通过；初版R4的低对比灰膜问题已关闭。
- 主线程真实Unity PlayMode **46项PASS**，目录`output/slazeya_storm_main_acceptance/r4-run-20260906-010157/`。原样复制生产源码与实际Bundle／WAV，使用最小游戏接口替身；覆盖初始异高身体包含、同一泡沫身份三维收核、完整粒子卡片范围、等待无外围残留、B0核心出生、环向外喷、默认回调、音效及清理。运行前后源码、Bundle和DLL哈希相同。
- Builder原生回读检查了闭合初泡、有限顶底与接缝、168个seed三维距离、全部启用渲染层的核心支持范围及连续波前。参考H=6，水核半径0.36，支持上限0.72，实际各层最大不超过0.65；核心等候检查到G=2.0。证据为round4下`native-foam-transport.csv`、`native-core-support.csv`、`native-wavefront.csv`及相邻帧。
- Release构建0错误、7个既有警告；源合同、Unity API／原生编译、10项托管音频契约检查通过。六PNG和两WAV保持原样。
- 音画MP4为91帧，完整解码通过；合入技能音效未重编码画面，视频流前后SHA均为`9156603B4E5994A5F791F26EF555CC4149830B085649DB092527F158A626E560`。

没有启动《废墟图书馆》完成实战。真实角色透明排序与主观听感仍待游戏内观察；无音频感知工具，未声称实际听过。预览使用代理人物，不能冒充游戏实战截图。部分白沫图案边缘可继续细化，独立评审无必须修复项。

## 同步和复现

仓库`SteriaBuild/SteriaModFolder`、D盘实际Steam游戏mod目录、C盘现有Steam副本各同步5项：Steria.dll、Bundle及.ab、gather_loop.wav、burst_tail.wav，15项哈希一致。具体目标路径与哈希见`output/slazeya_storm_main_acceptance/deployment-receipt.json`；旧文件备份为同目录`deployment-backup-20260906-010810/`。未推送远端或上传创意工坊。

音画预览：`preview_exports/slazeya_storm_mass/round4/audio/battlefield_with_audio.mp4`；纯视觉冻结：`round4/review-1baf40ba/`。Unity2019.3.15f1、Built-in、Gamma；从仓库根运行：

```powershell
./SteriaBuild/VFXSource/SlazeyaStormMass/build_slazeya_storm_bundle.ps1
dotnet build SteriaBuild/Steria.csproj -c Release
python SteriaBuild/VFXSource/SlazeyaStormMass/mux_slazeya_storm_audio.py --candidate-dir preview_exports/slazeya_storm_mass/round4 --expected-bundle <新构建并验收的Bundle哈希> --expected-driver <对应Controller哈希>
```

新构建必须使用自己的manifest／哈希及受影响验收，不套用旧版本PASS。
