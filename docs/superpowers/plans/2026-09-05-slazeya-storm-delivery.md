# 斯拉泽雅「倾覆万千之流」最终交付

此为R2历史交付。当前游戏同步版本与复现步骤见[2026-09-06 R4交付](2026-09-06-slazeya-storm-r4-delivery.md)。

2026-09-05，已构建、验收并同步游戏目录。用户原始目标及后续增加的质量要求、闪电／浪沫雾、仅技能音效均包含在交付中。

## 最终表现与规则

- 敌方实际阵型 XZ 场地上的主／次卷浪：1.35s 聚集，默认群攻管理器回调时爆发，1.20s 倾覆、撕裂和消散。
- 连续卷唇曲面、平滑变形法线、深蓝浪腹／亮浪唇／不均匀泡沫，采用原创数据纹理和图集。
- 命中时局部一强一弱两次青白短闪，中间有暗间隔；喷沫带出抬升水雾，继而退成落点低雾。
- 原创风压低鸣循环与雷击／水浪／消散技能音效；遵守总音量×音效音量，静音、暂停、慢速、重复回调与取消均受控。没有角色台词或音乐。
- 保留卡牌9002009、ActionScript入口、流和威力规则；删除旧施法者脚下重复视觉入口，伤害／防御仍由原游戏管理器处理。

## 候选标识

| 文件 | SHA-256 |
| --- | --- |
| 视觉 Bundle `steria_slazeya_storm_mass`（及 `.ab` 相同副本） | `03E113FCBD45DE93E79620824B59C8E7D9E8D2EDBD3B92FC1FC43415DDAC4E3A` |
| 视觉 driver | `5D6532FAF0C86382E0BE86CA0662A765A797151924F3EFEFE95ED57EDD515E8A` |
| AudioController | `5CB0DFA3B682A09DB99FBDABD4A135318F128DF6A973A01CC3A893E0CD9B8566` |
| 最终 FarAreaEffect 桥接 | `5EB50E0A181AC5B39C379016AA1467ECC5B0988236DCF9D978F8A980E0A22728` |
| `Steria.dll` | `1782F1F6105EEFC68DC59C810E43EC48B34475CF19EE3F04994F004218799D24` |
| `gather_loop.wav` | `FA4907228FE9408C9634227FB1CAF3D944F6C4D083A562294B46806AF5427C31` |
| `burst_tail.wav` | `D344F12283073F67918A7F97CC2B062275146F6F275A5906FCEAC2B480AE202C` |

## 验收证据与边界

- Epicurus 负责研究／设计，Chandrasekhar 负责实现与预览，Dalton 负责原创水纹／图集及喷沫轮廓返修，Hegel 负责原创技能音效，Zeno 负责独立视觉评审。主线程亲自检查改动、素材、代表帧及最终运行／部署。
- Zeno 对冻结视觉候选检查33张代表及相邻原生帧，结论 **PASS，无必须修复项**。报告：`preview_exports/slazeya_storm_mass/round2/review-03e113fc/independent-review.md`。
- 主线程最终真实 Unity PlayMode **32项 PASS**，执行原样复制的生产源码与实际 Bundle／WAV，游戏对象使用最小契约替身。覆盖阵营互换、范围、一次结算回调、空名单、取消、两次闪电与暗间隔、音源加载／静音／master×FX一次／暂停恢复／0.005慢速／不重播／清理。最终日志：`output/slazeya_storm_main_acceptance/unity-main-acceptance-final.log`；结果：同目录 `main-acceptance.txt`。音效 pitch 下限最后一次修正后的候选已重验，未复用旧31项结果作为新代码证明。
- Release 编译0错误、7个既有警告；视觉／音效源验证、Unity API检查、Bundle回读、shader支持、原生粒子年龄、两闪时序、尾声清空与源资源哈希均通过。
- 两份WAV为44100Hz PCM16 stereo，峰值约−3.50dBFS；完整PCM解码、无削波、循环接缝和时点检查通过。没有可用音频感知工具，未声称实际听过或完成主观听感验收。
- 音画 MP4 采用 `-c:v copy`，H.264流前后 SHA-256 均为 `45BAB183667000C0E98A7DE4708F3B1C64A635640A7B33722CA287D2DBACE220`；91帧完整解码。声音为AAC192kbps，原PCM试听保留。未改已通过的画面帧或视觉 Bundle。
- **没有启动《废墟图书馆》完成实战测试。** 实际战场相机／真实角色透明排序和主观听感仍需实战观察；原生预览及契约烟测不冒充实战证明。

## 同步与恢复

已在每个目标同步5个文件：`Assemblies/Steria.dll`、`Assemblies/AB/steria_slazeya_storm_mass`、同名`.ab`、`Resource/CustomAudio/SlazeyaStorm/gather_loop.wav`、`burst_tail.wav`。

1. 仓库 `SteriaBuild/SteriaModFolder`。
2. `D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`（实际 Steam 安装）。
3. `C:/Program Files (x86)/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`（现有副本）。

15项 SHA-256 全部与源文件一致。部署凭据：`output/slazeya_storm_main_acceptance/deployment-receipt.json`。原文件备份：同目录 `deployment-backup-20260905-134806`。本次未上传创意工坊或推送远端。

## 预览与再生成

音画预览：`preview_exports/slazeya_storm_mass/round2/audio/battlefield_with_audio.mp4`。只听声音：同目录 `preview_mix.wav`。纯视觉冻结目录：`round2/review-03e113fc`，第一轮和失败尝试仅作历史参考。

从仓库根运行：

```powershell
python SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_textures.py
python SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_audio.py
./SteriaBuild/VFXSource/SlazeyaStormMass/build_slazeya_storm_bundle.ps1
dotnet build SteriaBuild/Steria.csproj -c Release
python SteriaBuild/VFXSource/SlazeyaStormMass/mux_slazeya_storm_audio.py
```

Unity版本2019.3.15f1，默认编辑器路径 `C:/Program Files/Unity/Editor/Unity.exe`。重建会产生新Bundle hash，须重新验证相应候选；AV mux 的冻结hash守卫有意防止把未评审画面混成当前交付。
