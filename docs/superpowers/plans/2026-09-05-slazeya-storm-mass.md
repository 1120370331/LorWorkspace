# 斯拉泽雅「倾覆万千之流」风暴特效交付契约

最终状态：已完成升级、技能音效、原生验收及 C/D 游戏目录同步。最新候选和32项主线程验证见 [最终交付](2026-09-05-slazeya-storm-delivery.md)。下方各阶段的阻塞／未部署记录为历史过程，不代表最终状态。

## 目标

技能 9002009 在对方场地播放一次水色风暴：聚集、群攻结算同步爆发、消散。完成 Unity 2019.3.15f1 预览评审、Release 编译和游戏目录同步。

2026-09-05 用户看完首轮原生预览后明确修订质量目标：**“这个特效是不是太素了，我希望是3A级别的”**。首轮候选视觉 REVISE；原先为精简实现设定的 5 网格／3 粒子系统上限取消。继续原始任务和最终游戏目录同步，以重新批准的 Round 2 视觉规格为准。

## 冻结的接入约定

- 保留 `BehaviourAction_Steria_OceanWave` 和卡牌 XML 的技能入口。
- 删除 `DiceCardSelfAbility_SlazeyaMassAttackTeamLightGain` 中重复的施法者脚下视觉调用；保留所有流层数和威力逻辑。
- 使用默认 `BattleFarAreaPlayManager` 结算，不覆盖伤害、防御、卡牌销毁或结算通知。
- `isRunning=true` 为聚集门；聚集完成置为 false；`GiveDamageFromManager` 触发一次爆发；消散结束再设置 `_isDoneEffect` 并清理。
- 地面为 XZ，Y 为高度。位置取当前群攻受方的有效世界位置，不写死敌方阵营或屏幕左右。
- AssetBundle 不依赖 Unity 项目内未随 Mod DLL 部署的自定义 MonoBehaviour。运行时和预览须共享视觉驱动逻辑。
- 只更改本次特效涉及的文件；保留任务开始时已有的文档、工具和其他效果改动。

## 验收

| 范围 | 证据 |
| --- | --- |
| 制作参考 | 已读取的 Unity 官方或公开技术资料及具体采用方式 |
| 视觉 | Unity 实际渲染的早期聚集、聚集完成、爆发、消散帧；独立评审 PASS |
| 定位 | 双方身份互换时仍在受方；单目标及多目标范围合理 |
| 时序 | 默认管理器结算，空受伤列表仍爆发并结束，重复回调不重复爆发 |
| 资源 | Bundle 成功回读，shader 有效，无缺失脚本，驱动与预览一致 |
| 构建 | 有关源验证和 net472 Release 编译通过 |
| 同步 | 仓库 Mod 与 C/D 现有游戏 Mod 的 DLL、Bundle SHA-256 一致 |

实际 Steam 安装由 D 盘 appmanifest_1256670.acf 确认；C 盘同时保留现有模组副本，与仓库既有部署惯例一致同步。游戏内真实战斗未完成前不得把 Unity 预览称为实战验证。

## 设计审定

研究规格已 ACCEPT：开放低矮三股水风暴，1.2 秒聚集门，manager 回调爆发，1.05 秒消散。唯一工程修订：使用 `VFXSource/SlazeyaStormMass/UnityProject` 专属最小工程，避免共享工程的 `InitializeOnLoad` 钩子重建用户其他效果。

## 当前门禁与恢复点

以下许可证阻塞已解除：主线程于 11:22 亲自执行构建，Unity 日志 11:22:40 进入渲染并成功退出 0；资源包 hash `BBBD8BEF377267CBC9FB5939E91A99D07A9F84E0F6DA6F0920E977DAB01A6E95`，10 张代表帧及连续帧已生成。媒体编码兼容修复由原实现代理负责。主线程独立 Unity PlayMode 契约烟测 15 项 PASS（真实 Unity、简化游戏对象，不是实战）。

用户以质量不足打回首轮，具体为规整丝带绕圈、风暴体积弱、材质线纹均匀、爆发对比不足。Round 1 参考已留存 `preview_exports/slazeya_storm_mass/round1`。研究代理 Epicurus 重做视觉规格；实现代理 Chandrasekhar 保持原有源码所有权；独立评审代理 Zeno 收束首轮 REVISE。尚未部署和提交。

### 首轮历史恢复记录（许可证项已解决）

- 实现代理：Chandrasekhar；研究代理：Epicurus（研究交付已接受并释放）。主线程已逐项读取运行时、驱动、shader 和专属 builder 源码，检查卡牌差异只删除旧视觉入口。
- 有关源验证全部通过；Release 编译 0 错误、7 个既有警告；diff 空白检查通过。
- **BLOCKED：Unity 2019.3.15f1 本机许可证无效。** `preview_exports/slazeya_storm_mass/unity-build.log` 报出 `Unity has not been activated with a valid License`，在项目和 builder 执行前退出 1。
- 尚未生成 Bundle／原生预览，尚未进行独立视觉评审，尚未运行主线程 Unity PlayMode 验收，尚未部署或提交。
- Release 候选 DLL SHA-256：`84183B77F2DD1BD52974B8CDCBB103C604B600CFF08AC05CAED8329206BA80C3`。仓库 Mod 与 C/D 游戏目录仍保留原 DLL（SHA-256 `1B9F67A325DE26F7073777416F6AD61159B336B22AA9991BCE6FF1B402075AD1`）。
- 等用户通过 Unity Hub 恢复有效许可证后，续跑 `SteriaBuild/VFXSource/SlazeyaStormMass/build_slazeya_storm_bundle.ps1`；随后第三代理评审原生预览。主线程临时验收工程位于 `output/slazeya_storm_main_acceptance/UnityProject`，待原样复制最终运行时源码和 Bundle 后执行 `MainAcceptanceEntry.Run`。
- 视觉与功能门禁通过后才运行主线程准备的 `output/slazeya_storm_main_acceptance/deploy_candidate.ps1`，备份并同步 3 个目标的 DLL、Bundle 和 `.ab`，检查 9 项 SHA-256；随后选择性提交本任务文件。
