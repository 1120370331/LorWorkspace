# 回忆系 v5 火焰音效：交付记录

用户试听 v5 后确认“差不多了，应用进去”。已应用斩击 0.46s、打击 0.55s、突刺 0.36s，未发布长坠落或持续燃烧样音。

## 应用结果

- `MemoryFlameAudio.cs` 在真实 `RencounterManager.PrintSound` 反馈点替换 6 张书页、11 颗近战攻击骰的成功动作声。
- 范围：9001002、9001003、9001004、9001005、9001009、9001010。严格检查 `SteriaBuilding` 包、卡牌、动作、回忆 EffectRes、结果和已知动作脚本。
- 使用原生 `CharacterSound` 声音 prefab、`BattleEffectSound` 与 `BattleSoundManager.VolumeFX`；保留原生生命周期，不添加伤害/时钟、循环或音高处理。
- 失败、平局、近对远弹反、防御/闪避、未批准书页/动作保留原声。新声音无法加载/初始化时回落原声。
- 正式音频与批准 v5 WAV 逐字节一致；制作源、生成器与批准记录位于 `SteriaBuild/VFXSource/MemoryFlameAudio/`。
- 新第 16 章覆盖开放库寻找、许可、试听、合成、三类 WAV、映射、反馈时机和部署。

## 验证

- 原合成与 v5 编辑确定性重现：3/3 源与发布文件哈希相同。
- Release 编译通过，0 错误、7 个原有警告。补齐当前其他粒子代码需要的 Physics/Physics2D 两项引用后完成构建；未改变相关粒子实现。
- 实际编译 Steria.dll / 游戏类型 / 生产 PCM 读取器：39 项策略检查通过。
- Unity 2019.3.15f1 隔离 Play Mode：53 项检查通过，包括真实私有 PrintSound Harmony 安装与派发、一次事件一个原生播放器、AudioClip 帧数、音效音量只生效一次、无循环/额外 pitch，以及失败回退。
- Unity 检查与部署使用同一 DLL，SHA256：`4ba01608ce09fc598e553f2b41a8d0d919b2c64b497620c4ec19706fc1ae7762`。
- 未将普通 Windows 控制台当作 Unity 环境；控制台的 ECall 限制通过隔离 Unity 原生检查解决，未放宽生产逻辑来适配测试。

## 部署

实际目录：`D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`。

C 盘 Steam 路径解析为同一 D 盘目录，按真实路径去重。仓库包和游戏目录共写 9 个文件：两处 DLL、两处三份 WAV，以及游戏 CardInfo 中原先缺少的 9001004 三个 ActionScript（该 XML 为单文件写入，但仅改这三项文本属性）。其他游戏 XML 数据原样保留。

| 文件 | SHA256 |
|---|---|
| slash.wav | `6b01e607af60885e016c4092894c120ec63b7c2c75df24f40bc24b670ee3017c` |
| hit.wav | `8fde15e0ce62aeecb1340ac415d43b78f604fc4046adaace18fe3a6f56509268` |
| pierce.wav | `7f685712b51032a63af17c96172dff7c11eca2bb75d140ad2753e1a581344fdc` |

备份：`output/memory-flame-delivery/backup-20260912-220328/`。详细前后哈希、路径和变更文件见 `output/memory-flame-delivery/deployment.json`，Unity 证据见同目录 `unity-result.txt` / `unity.log`。失败回退与 clip 缓存按进程工作，游戏重新加载模组后使用新声音。

## 边界

用户已确认 v5 样音，实际 Unity 接入已检查。本轮没有进入完整游戏战斗试听与 BGM/其他技能的综合混音，不据技术检查宣称最终实战听感已验收。其他并行工作树变更保留，不属于本次音效验收范围。
