# 斯拉泽雅风暴：同步技能音效交付

最终状态：音效与已接受画面合成，最终代码通过主线程32项运行验证，并已随DLL及视觉Bundle同步到三处模组目录；见 [最终交付](2026-09-05-slazeya-storm-delivery.md)。

用户明确追加合适音效，并确认「只要技能音效」，不制作角色台词、配音或音乐。

画面 `round2.6` 已独立 PASS，Bundle=`03E113FCBD45DE93E79620824B59C8E7D9E8D2EDBD3B92FC1FC43415DDAC4E3A`，视觉驱动=`5D6532FAF0C86382E0BE86CA0662A765A797151924F3EFEFE95ED57EDD515E8A`。本次保留所有视觉源、Bundle 和已通过的帧，音效采用独立 PCM WAV 文件与 C# companion，避免重建／改变已接受画面。

## 固定声音接口

- authoring 源：`SteriaBuild/VFXSource/SlazeyaStormMass/source_audio/`。
- 运行时路径：Mod 根下 `Resource/CustomAudio/SlazeyaStorm/`。
- `gather_loop.wav`：1.00s，44100Hz、16-bit PCM、stereo。可无缝循环的低鸣、风压／水流摩擦声，不包含爆点、音乐、语音。声源 gain 从 0 在 GatherDuration=1.35s 内平滑升至 0.30；等待实际回调期间保持。
- `burst_tail.wav`：1.20s，同格式。t=0 雷击／低频冲击与水浪拍击，t=.10 更轻的短电裂响，随后喷沫、细滴和风水噪声退去；最后约30ms平滑归零。声源 gain=0.78。
- 每个源 WAV sample peak ≤ -3dBFS、无削波、低 DC、首尾无爆音；预览混合不削波。上述 gain 是消费者接口，可由主线程根据实际素材证据调整，但必须两端同步。
- 仅原创生成或明确可用的合法现有素材；本轮选择确定性离线多层合成，不复制未经核实的外部音频。不要把同一段白噪声直接当所有相位。

## 接入与生命周期

- 新 `SteriaBuild/SlazeyaStormAudioController.cs`，plain C#，本实例持有两个 AudioSource，clip 静态缓存。可直接解析本任务固定 PCM16 WAV 到 AudioClip，验证 RIFF/chunk/声道/采样率/长度；避免新增 UnityWebRequest 依赖。
- `FarAreaEffect_Steria_OceanWave` 仅增音效伴随实例的 Init／Advance／TriggerBurst／Dispose。画面时序、定位、默认 manager、伤害和流逻辑不变。
- Burst 只来自 manager 回调，含空受伤名单，重复回调不能再次播放。聚集声在 B 后约40ms淡退，burst 同帧启动；尾声≤1.20s，取消／禁用／销毁立即停声并释放实例源，无遗留循环。
- AudioSource playOnAwake=false、spatialBlend=0、无 Doppler；保持默认不忽略 AudioListener 暂停。尊重 Time.timeScale 的暂停／恢复和速度；不改全局音频状态。
- 音量只乘一次 `GlobalGameManager.Instance.CurrentOption.GetVolumeEffect()`，该函数已由主线程读源码确认是 master×effects。必须尊重静音和运行中音量变化；不要再次乘 master。没有游戏 option 的 Unity fixture 可回退1。
- 源/音频文件缺失不阻塞视觉，不让加载异常中断攻击；有效受方但无视觉 Bundle 时音效可独立工作，无受方则不在施法者处制造声源。

## 所有权

- 音效资产 worker：仅 `generate_slazeya_storm_audio.py`、`source_audio/**`，提供两个 WAV、种子/层次/数值检查 manifest、按原生 B=1.55s 配好的预览 WAV。不得改视觉／运行时／游戏目录。
- Chandrasekhar：`SlazeyaStormAudioController.cs`、FarAreaEffect 的最小音效桥接、必要的本音效验证脚本与把现有已接受 MP4 无损视频流复用后混入声音的脚本。不得改视觉 controller／shader／mesh／贴图／Bundle。
- 主线程：原生 PlayMode 音效触发、静音、清理与既有21项验证；校验视频流未改、音轨时点；扩展部署清单，复制新 DLL、现有视觉 Bundle 与两 WAV 到仓库 Mod/C/D 现有游戏 Mod，逐文件 SHA256；选择性提交。

## 音画预览

按已接受 manifest 的 B=1.55s、G=1.35s 和3.0333s视频长度，使用同一源 WAV／gain／循环规则离线混合一条试听音轨，与现有 `battlefield_motion.mp4` 采用 `-c:v copy` mux。预览不得靠修改视觉帧适配声音；记录 source WAV hash 和音轨时点。明确区分声音文件／解码／包络检查与实际听感，未听过不得声称听过。
