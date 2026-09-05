# 斯拉泽雅「倾覆万千之流」R3交付

2026-09-06，R3.8完成原生视觉评审、运行检查与游戏目录同步。

## 最终表现

一个共同水泡覆盖敌方全体。已有泡沫块在0.82–1.12秒向爆裂带汇流，泡体在1.02–1.23秒压紧，1.20–1.32秒形成局部白青蓄光峰；压缩下限仍包含全体身体。1.35秒开放默认群攻管理器的结算门，等待实际伤害回调后才破壳。四股强弱不同的水舌向外喷出、下泻、拉丝碎落，接入低雾并在回调后1.20秒清理。

保留双次局部闪电及暗间隔、浪沫雾、两份技能音效；没有台词。卡牌9002009、流与威力规则、默认伤害及防御处理保持。视觉不追加伤害、不移动敌人。

## 候选与验收

| 项目 | SHA-256 |
| --- | --- |
| Bundle及同名.ab | `3B14E925E820BBD37198D92568F0F239B308D5AD61B631011FD0D99C2486B2A1` |
| Controller及Unity镜像 | `D4D887F76D8C0448A4DE93FDB9D33E80AE2243EEFEAA748915CB212B15A2E617` |
| Steria.dll | `C9AB0D44C17733D0CE13B6DABE23B89CB75C6A4145D739A73B2525C08193D3CC` |

研究设计由Epicurus完成；Chandrasekhar实现R3基础，Carson完成最终内聚、蓄光和瀑布喷射；Zeno独立视觉复审；主线程检查代码及代表帧、运行验收并部署。

- 独立视觉PASS：`preview_exports/slazeya_storm_mass/round3/review-3b14e925/independent-review.md`。同镜头检查24张原生图，包括相邻帧，确认内聚、压缩、爆前蓄光与水幕下泻；旧版本的硬环墙已移除。
- 主线程Unity PlayMode **42项PASS**：`output/slazeya_storm_main_acceptance/r3-run-20260905-235650/`。原样复制生产源码、实际Bundle和WAV，使用最小游戏接口替身。覆盖异高身体与脚底包含、效果挂件排除、168个泡沫身份连续与真实内聚、环向外喷、敌我互换、回调等待与空名单、重复回调、取消、音量、暂停慢速、双闪和清理；运行前后候选哈希一致。
- Release构建0错误、7个既有警告；源合同、Unity2019 API编译、原生Bundle回读和几何诊断通过。
- 音效源与10项托管音频契约检查通过。MP4共91帧完整解码，合入技能音效时原视频编码流保持不变；视频流SHA为`31C1F0C23B456D823684D4A3922643AE3CDC8BBC5CF1102B8B335643017F1022`。

验收边界：没有启动《废墟图书馆》完成实战。真实角色、战场透明排序及主观听感仍留待实战观察；无音频感知工具，未声称实际听过音效。原生预览使用代理人物，不能冒充游戏截图。泡沫形状的相似性和个别细水片锐边为非阻塞的后续美术细节。

## 同步与预览

以下三个位置各同步5项并验证哈希：Steria.dll、Bundle、同名.ab及gather_loop.wav／burst_tail.wav。

1. `SteriaBuild/SteriaModFolder`
2. `D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`
3. `C:/Program Files (x86)/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`

15项全部一致。凭据：`output/slazeya_storm_main_acceptance/deployment-receipt.json`；原文件备份：`output/slazeya_storm_main_acceptance/deployment-backup-20260906-000250/`。未推送远端或上传创意工坊。

音画预览：`preview_exports/slazeya_storm_mass/round3/audio/battlefield_with_audio.mp4`。冻结原生图及连续帧：`round3/review-3b14e925/`。项目Unity2019.3.15f1、Built-in、Gamma。

从仓库根再生成：

```powershell
./SteriaBuild/VFXSource/SlazeyaStormMass/build_slazeya_storm_bundle.ps1
dotnet build SteriaBuild/Steria.csproj -c Release
python SteriaBuild/VFXSource/SlazeyaStormMass/mux_slazeya_storm_audio.py --candidate-dir preview_exports/slazeya_storm_mass/round3 --expected-bundle <新构建并验收的Bundle哈希> --expected-driver <对应Controller哈希>
```

重建后的候选须核对新manifest及相关验收；不将旧哈希的PASS套用到新源或新画面。
