# 风暴与晨光：施法—命中音效交付

2026-09-07。已加强「倾覆万千之流」聚集与爆发音效，给「潮雾晨光」补上整卡施法声和逐骰命中声，13:40同步游戏目录。仅技能SFX，无配音、人声或音乐。

## 声音与触发

风暴增加早期风水起音、中频涡流与水拍击主体。运行时聚集包络改为`.32*S(t/.03)+.23*S(t/1.35)`，30ms内建立起音，最大gain .55；命中gain维持.78，源全长RMS由−24.72提高到−16dBFS（+8.72dB）。聚集源低/中/上频带功率约41.84/48.74/9.34%，有别于旧版偏重低频。1秒循环和1.2秒爆发尾长保持，次电裂层低于主裂响9.40dB。

晨光采用短空气拂开、低Q不规则水光共振与暖水拍击。cast .32秒/gain .55，hit .72秒/gain .75；整卡只播放一次cast，每个真实有效骰回调一次hit，空成功列表保留全局命中声，多目标和重复回调不叠加。早期hit对cast做40ms退出，原视觉、结算和等待时间不变。

声音按现有GetVolumeEffect（master×effects）乘一次，二维实例源，不改变全局音量或BGM。暂停、.005慢速、静音/恢复按当前样本位置继续；取消和正常结束释放本实例。缺单个WAV不阻断另一个cue、画面或游戏。共享PCM读取器保留严格格式/块/长度/512KiB边界，两技能各自包装器仍限定本cue的精确帧数。

## 冻结资产

均为44100Hz、双声道PCM16 RIFF/WAVE。下表为量化PCM测量，dBFS而非LUFS。

| 源文件 | 时长 | sample peak / 8×true peak | 全长RMS |
| --- | --- | --- | --- |
| SlazeyaStormMass/source_audio/gather_loop.wav | 1.00s | −8.184 / −8.149 | −16.000 |
| SlazeyaStormMass/source_audio/burst_tail.wav | 1.20s | −5.205 / −5.189 | −16.000 |
| VeliaTideMist/source_audio/cast.wav | .32s | −7.314 / −7.295 | −17.000 |
| VeliaTideMist/source_audio/hit.wav | .72s | −5.801 / −5.794 | −17.000 |

四文件SHA分别为`F85082469BA9FF6971BEA4D00B4979D5C6F301943BD97F168D16C41B80D61996`、`D8673169FDADB38EB46419FC42214E5227919EC80723D5DEADB47A80059BB6BE`、`F007F39C87AA27FAAD7D6D4C76EFD2E58450AC2DB62792126C93AF1BC8CDB964`、`7A096D713548C52F3D3FAB50578F3A1C3638F3188B2F7C547C94D59B82480675`。两生成器、参数/种子/依赖及manifest保留，可确定性重生；无外部录音素材。

## 验收与边界

Popper设计，主线程冻结四cue合同；Maxwell制作资产，Lorentz负责运行时；Darwin独立技术复核对两个技能分别PASS。主线程检查生成器与源码、事件/资源生命周期、最终原生输出及部署。

- 独立复核重算全部PCM格式、峰值/RMS、DC、相位与循环接缝，并与FFmpeg独立完整解码一致；四源字节级重生一致。clipped=0，最大绝对DC约2.31e-7，mono fold损失约.046–.108dB。三次循环接缝值为0、边界能量无凹陷。实际同相叠加上界风暴−3.816、晨光−4.119dBFS；这不代表全游戏/BGM混音已测试。
- 14份固定gain混音均被独立重建，PCM完全一致；A/B无归一化。8份带声媒体的视频NAL流、各packet payload/PTS/DTS/duration均保留；PCM MOV音轨逐样本一致，AAC MP4为试听用有损副本。
- 触发使用实际接受视频时序：风暴round5 B≈1.55秒、68355样本；晨光cast=0、hits=.60/1.95秒、第二Begin1.65秒，生产事件不硬编码这些预览时点。
- 新鲜进程托管检查：风暴11组、晨光10组全部PASS；实际Unity/game API编译通过，包含四cue精确长度、错误RIFF/缺cue、去重、静音、慢速、listener暂停与清理。
- 主线程79项Unity原生检查全部PASS，保留前47项镜像/光束/生命周期，并验证真实工厂一cast两hit、输出DSP样本、提前/延迟命中、音量乘一次、.005pitch、暂停/静音、缺cast仍播放hit、取消与退出。最终证据`output/velia-tide-mist/acceptance/run-20260907-133404`及`main-acceptance.txt`。
- 第一轮新AudioSource首次GetOutputData读到空分析缓冲，复用source则有值；为分析通道预热后再测全部DSP通过。仅改夹具采样时点，生产输入未变。原始记录`run-20260907-132944`保留；原生设备配置48000Hz、buffer1024×4、隔离listenerVolume1。
- 最终隔离DLL构建0错误、7个既有警告；代码及四WAV在原生验收/构建/部署期间哈希稳定。视觉controller、shader、两Bundle、镜像方向、游戏XML与其他数值改动保持原样，无需重建已冻结视觉Bundle。

`subjective_listening: UNVERIFIED (audio input unsupported)`。当前模型无法接收音频，技术PASS不代表已听过或认证音色、刺耳程度、实际战斗掩蔽。原生夹具为生产组件与最小游戏替身，并非实际战斗录音。

## 同步与试听

DLL SHA：`70ABA496155E2D034A0CE4006A7641333808108020DE753F7FF11C1A15503EA7`。

已同步仓库Mod与C/D游戏逻辑路径的DLL和4个WAV，15个逻辑文件哈希核对通过；C是D安装的Junction。所有目标的CardInfo.xml、两视觉Bundle及.ab同步前后哈希保持。回执`output/storm-dawn-audio/deployment-receipt.json`，旧文件备份`output/storm-dawn-audio/deployment-backup-20260907-134029`，隔离构建`output/velia-tide-mist/delivery-20260907-133819`。

源试听：两个模块各自`source_audio/preview_mix.wav`。带声视频：`output/storm-dawn-audio/asset-preview/videos/slazeya_round5_battlefield_motion.mp4`、`velia_reviewed_r9_player.mp4`及enemy/black对应版本。完整媒体hash、14混音、A/B、时点与复现命令见该目录`artifact-manifest.json`、`handoff.md`。只把4个cue WAV部署到Resource/CustomAudio，预览及诊断不进入游戏。
