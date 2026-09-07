# 倾覆万千之流：爆发增量与镜头冲击交付

2026-09-07。已增加爆发向外喷散的水沫层次，并在实际命中回调加入一次游戏原生镜头抖动，16:18同步游戏目录。

## 最终效果

保留云团向内吸入、近核压实、B0有限核心及原1.35秒聚集/1.20秒尾长。主水舌略增厚，在原波前增加一批有限薄沫/飞滴；瀑布浪沫改为.06/.12/.18/.24四批，共112条，分为32条较长水流与80片短而宽的泡沫。长宽独立、出生点和方向变化，避免新增水量变成成排长条。一个RingJet、八个粒子系统、原贴图/颜色/云体计算保持。

理想完整发射总量：17张sheets、258颗drops、112条streaks，共387（原223，约1.74倍；不是像素面积或主观量感比例）。Drops/Streaks容量320/128，保留原随机seed2125/2017；新增批次均有限，错过窗口不补放旧喷发。原生60/240Hz峰值585/586，小于640。B+.8时sheets/streaks归零，只剩13颗原细滴与低雾；B1.20清空。

`FarAreaEffect_Steria_OceanWave`缓存有效目标，原burst guard内至多请求一次`BattleCamManager.ShakeCam(65f,.022f,.016f)`。全部防御的空成功列表仍有真实爆发和一次抖动；无目标、重复/失效owner/card/manager回调不新增请求，缺相机/API异常不阻断视觉和音效。沿用游戏已有EarthQuake服务，固定.25 scaled seconds；更强震动、暂停和恢复由游戏管理。不添加滤镜、不改Transform/FOV或清空其他相机效果。已请求的震动可由游戏自行完成其有界时间。

## 设计、实施与复审

Turing设计，主线程确认实际游戏相机API与边界；Bernoulli实现视觉与预览，Locke仅负责OceanWave镜头接入。Descartes对首版返回REVISE：前排浪沫有梳齿排列。第二版只修改WaterfallStreaks的独立长宽、出生位置与动量，保留数量和其他层；独立聚焦复审PASS。主线程阅读所有变更，检查原始峰值/下落图、固定镜头A/B和镜头示意。

真实Unity BakeMesh确认Stretch支持独立尺寸：X改变宽度，Y改变长度；保留原atlas、年龄流、生命周期与lengthScale3.35。十张Gather和真实B0与原round5逐字节一致，原baseline目录保持。第一版及第二版证据都保留，未覆盖历史预览。

已审第二版：`preview_exports/slazeya_storm_mass/round6/first-revision2-20260907-150405/full-native`。正式读回：`preview_exports/slazeya_storm_mass/round6/reviewed-20260907-1559`。两个候选AB逐字节一致。12张固定采样对比中10张完全相同，另两张仅3/25像素相差1/255；+.12明确取实际+.116667样本，+.8精确补齐。

## 最终门禁与证据范围

- 源验证46项、Unity2019.3.15f1/D3D11真实API编译、正式AB构建/读回通过。Reviewed阶段执行完整原有云/包络/atlas验证，CPU/GPU波前3456对样本最大误差约.000110；没有以新水量替代旧云体正确性。
- 主线程69项PlayMode检查全部PASS：原云吸入/核支持/相反阵营目标/音频状态，加真实粒子数量/容量/旧seed/.8退场/预算，以及相机API spy的一次性、参数、空成功列表、无目标、取消、替换card、缺服务和相机异常。证据`output/slazeya_storm_main_acceptance/r6-run-20260907-161052`与`main-acceptance.txt`。
- 原native夹具旧聚集音量断言已按此前383b9f5的.55增益更新，当前四个音效源/音频controller保持哈希；不重新制作声音。当前SFX混音重建误差0 PCM16 LSB，视频流复制哈希一致，无归一化或音画重计时。
- 隔离Release DLL构建0错误、7个既有警告，受测源码、编译输入和AB在交付前保持哈希。Velia模块/Bundle、四个WAV、卡牌XML和无关工作区修改保持。
- 项目编译输入仍为76项、无文件移除。除本次两个runtime文件，相较上次音效构建还包含工作区并行修改的HarmonyPatches.cs、MusicDiceSystem.cs；保留其当前内容和构建输入清单，不将这些源码加入本次提交。

画面来自原生代理场景/原始帧抽查，不是实际战斗录屏；未完整观看视频或主观试听。镜头示意使用独立预览相机、20/10px初始偏移的.25秒近似，固定镜头水量A/B不抖动。示意明确标注非实际游戏EarthQuake shader；game API的真实编译与spy调用不等于已观察游戏shader输出。预览时序仍B≈1.55秒、91帧/30fps。

## 产物与同步

- Bundle SHA256：`524AECBB8C8DAD193857D88436BB0D2F0D1A0EC9119E05774AEE48ECA0CC5F48`
- Controller/Unity镜像：`CC4A4CC1273CB4750618B8576D9E2CD55ACA98BA4E897DF99C7744170E66038F`
- OceanWave host：`418E2966BB017F24D0384DA8DCE3E5C63DFF4424467E025427584AE1DDA93456`
- DLL SHA256：`33D8751D1A704ECB2E8AC13C3EC6D2D37490583793DCAA37704EFD2CE8172037`

同步仓库Mod及C/D两个游戏逻辑路径的DLL、Storm Bundle和.ab，9个逻辑文件复核通过；C是D安装的Junction。原四WAV、Velia Bundle/.ab及XML前后哈希保持。回执`output/slazeya-burst-impact/deployment-receipt.json`，备份`deployment-backup-20260907-161848`，隔离构建`delivery-20260907-161052`均在同父目录。

正式固定镜头带声预览：`reviewed-20260907-1559/audio/battlefield_with_audio.mp4`；独立镜头示意：同目录`comparison-presentation/camera-approximation_with_current_SFX.mp4`。同目录有完整源码/Bundle/原生计数/相机偏移及音画证据。复现使用模块构建脚本的新PreviewDirectory，Reviewed须传已审候选并验证全部visual-input-sha256和重建AB一致。
