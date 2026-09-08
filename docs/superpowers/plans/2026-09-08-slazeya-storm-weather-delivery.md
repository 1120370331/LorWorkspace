# 倾覆天气层交付

## 最终行为

新增三段低反差外围软云、冷钢蓝色天气调色和远近两层向左下落的细雨。副云在蓄力与持核期间持续漂移，目标位于另一侧时镜像到可见空域；爆发时降低天气权重，B1.10归零，B1.20随原流程清理。独立单 pass、每实例克隆材质，只读原 visual 时钟，没有新增粒子、光源或相机。

主云几何、旧八粒子系统、波前、爆发、声音和战斗结算保持；主云 canonical 与镜像 SHA256 都是 CC4A4CC1273CB4750618B8576D9E2CD55ACA98BA4E897DF99C7744170E66038F。缺资源时跳过天气，取消只清理自身组件和材质，不清空其他相机效果。

## 验收与产物

- storm_weather_design 设计，storm_weather_impl 实施，storm_weather_review 独立复核，主线程检查改动/原图并完成最终门禁。首候选弱小碎点经位置、宽肩和软断口修订；最终独立视觉 PASS，看到慢于雨的云运动，无同心壳、硬切线或暗色咬口。
- 已审候选：preview_exports/slazeya_storm_mass/round7/firstcandidate-20260908-072057-791-bad72383。
- 正式 FirstCandidate→Reviewed 输入冻结、全套原核心源/原生门和 AB 重建一致通过；正式目录 preview_exports/slazeya_storm_mass/round7/reviewed-weather-final。
- 主线程原生 PlayMode 78项 PASS（原69项及9项天气相关检查），证据 output/slazeya_storm_main_acceptance/r7-run-20260908-154050/main-acceptance.txt。覆盖实际生产 host 加载模板并接入 EffectCam、暂停/早回调/重复回调、尾部移除、死亡取消、保留其他 image effect；主云、声音和抖动原检查仍通过。
- B1.10 的 off/on PNG SHA256完全一致：E773A5E75F931240A943E8F84DD26AEE1288F6FE0ED0F6297D85BEC977C6D614。天气视频由同相机实际 OnRenderImage 连续帧生成，并完整解码验证。
- 项目 Release 构建0错误；实际Unity2019 API编译与shader门通过。验收夹具补齐了原版 GetAliveList 默认路径；两次无新日志的 Unity 原生启动失败与应用代码无关，成功运行使用新日志和独立结果文件。
- Bundle：F9969F0DD3F3154945993D509E466E55E8FFDE619A644D9E50C14F5C5AD77462；DLL：8877EA4BA8886F236E67AE7FF2E954AB88E67B1615E5CFCF065BEDD71F08BFC8。
- 已同步仓库模组包与C/D既有游戏路径的DLL、Bundle、.ab，9个逻辑目标哈希检查通过。备份/回执：output/slazeya-storm-weather/deployment-20260908-154424/。C与D安装存在junction，不将逻辑文件数当成独立安装数。
- 动态预览：preview_exports/slazeya_storm_mass/round7/reviewed-weather-final/weather/weather_motion.mp4。

## 复现与边界

使用build_slazeya_storm_bundle.ps1的新PreviewDirectory；Reviewed还需传ReviewedCandidateDirectory并保证源/材质/shader输入一致。新增天气Material是独立AB资产，必须保留其.mat/.meta。主线程运行入口为output/slazeya_storm_main_acceptance/run_r7_acceptance.ps1；旧Unity本机启动采用临时OPENSSL_ia32cap=:~0x20000000。

画面是原生代理场景，不是游戏实战录屏。EffectCam真实UI合成顺序和游戏帧率未完成实战验证；保守屏边/身体保护降低遮挡，但不声称所有UI均不受影响。DLL包含当时完整工作树，其余未提交改动保留且不加入本任务源提交。
