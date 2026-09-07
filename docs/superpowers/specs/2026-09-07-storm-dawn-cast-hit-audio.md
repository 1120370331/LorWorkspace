# 风暴／晨光：施法与命中技能音效

2026-09-07；**主线程已批准并冻结本设计合同，参数以本文为准**。基线 `69400f0a52329ff6c3a37fd186b968f4d5f7bd0e`。本次仅更新状态；不修改参数、不继续研究。主线程分别派发资产与运行时实施；本文批准不代表已实现或已完成主观听感验收。

用户：“目前这两个技能的音效都不明显，增强并制作音效，施法-命中。”只做技能 SFX，无台词、配音、呼吸人声、合唱或音乐。风暴体现聚云、压缩、爆发；晨光体现暖光穿云与柔和但明确的照射冲击。保留已批准视觉、Velia r9 镜像／光束运动、结算与原有等待时间。

## 依据与诊断

已读根 `AGENTS.md`（根目录无 README）、`ModGuideDocs/README.md`、模组 README、教程10“动作音效绑定”、[旧声音合同](../plans/2026-09-05-slazeya-storm-sfx.md)、[旧交付](../plans/2026-09-05-slazeya-storm-delivery.md)、R4交付、Velia 模块 README／r9设计与交付；检查两技能 controller／host、风暴生成器／manifest／音效 verifier／mux入口及相关提交 `9614abf`、`9a326ff`、`c9c5dd6`、`69400f0`。采用已有可复现分层合成方法，当前不需要外部音频素材或新研究依赖。

本次对现存两个源 WAV 只做只读 PCM 解码、哈希、peak/RMS核对；均匹配原 manifest。主线程已确认有效MP3经音频输入工具返回不支持音频输入；本阶段及后续代理不得声称试听。下面的掩蔽判断是声学设计推断，不是已听见的缺陷结论。此限制不阻止获授权的制作、客观检查与可播放预览交付。

| 旧素材 | sample peak | 全长 RMS | 前120ms RMS | 原 gain 后全长 RMS（option=1，聚集取最大包络） |
| --- | --- | --- | --- | --- |
| `gather_loop.wav` | −3.50 dBFS | −15.70 dBFS | −16.03 dBFS | −26.16 dBFS（×.30） |
| `burst_tail.wav` | −3.50 dBFS | −24.72 dBFS | −15.59 dBFS | −26.88 dBFS（×.78） |

- 风暴聚集旧包络为 `.30*smoothstep(t/1.35)`，t=.10s时gain仅.00469（−46.57dB），t=.32s时也仅.04258。旧manifest频带功率中84.4%在20–250Hz，250–2000Hz仅14.3%；缺少早期起音且偏低频，在小音箱或战斗混音中易不明显。
- 风暴爆发峰值已有余量，但峰值与全长RMS相差21.22dB；短裂响后主体快速衰减。优先增强水压主体、可听中频及有层次的尾声，不能只把峰值推近0dBFS。旧交付的数值PASS未认证主观听感。
- Velia当前host没有自定义音频伴随器，视觉入场.32s、每骰回调脉冲.72/.77s均缺专属声音标记。不能把“无自定义接入”误报为整个游戏完全无声。教程MotionSound按动作绑定；本任务仍用实例伴随器接真实回调，避免泛化到角色所有动作。

## 四个运行时文件合同

统一 RIFF/WAVE format_tag=1、44100Hz、PCM16 little-endian、双声道L/R、blockAlign=4、byteRate=176400；无外部采样。以下RMS为量化后全文件、双声道样本合并的 `20log10(sqrt(mean(x²)))`，含尾部静音；不是LUFS。sample peak取任意声道绝对最大，true peak以与旧生成器一致的8倍过采样估计并记录。

| 技能／文件 | 长度／每声道frames | sample peak上限 / 8×true peak上限 | 源全长RMS目标 | runtime gain |
| --- | --- | --- | --- | --- |
| Slazeya `gather_loop.wav` | 1.00s / 44100，循环 | −8.0 / −7.5 dBFS | −16±1 dBFS | 新包络，最大.55 |
| Slazeya `burst_tail.wav` | 1.20s / 52920，一次 | −5.0 / −4.5 dBFS | −16±1 dBFS | .78，保持原值 |
| Velia `cast.wav` | .32s / 14112，一次 | −7.0 / −6.5 dBFS | −17±1 dBFS | .55 |
| Velia `hit.wav` | .72s / 31752，每个有效骰回调一次 | −5.5 / −5.0 dBFS | −17±1 dBFS | .75，两骰相同 |

Slazeya authoring为既有 `SteriaBuild/VFXSource/SlazeyaStormMass/source_audio/`，运行时保持 `Resource/CustomAudio/SlazeyaStorm/`；Velia新增 `SteriaBuild/VFXSource/VeliaTideMist/source_audio/`，运行时 `Resource/CustomAudio/VeliaTideMist/`。路径均相对各自模组根。仅这四个WAV部署；试听混音、manifest和诊断属于制作证据。

这些较低的源峰值用于保证叠加余量；通过减小无效低频、分层动态与更早起音提高可听主体。目标中心值下，风暴聚集最大RMS约−21.19（比旧版高4.97dB），爆发约−18.16（高8.72dB）；晨光施法／命中约−22.19／−19.50dBFS。都是设计计算，必须以新WAV复测。

## 音色与相位设计

**Slazeya施法。** 厚风压包裹密集水流摩擦，中心低频稳固，中频有向内拉紧的粗糙纹理。沿用periodic FFT噪声、独立随机种子与循环粒子；不把一次性上升／爆点烘进1秒循环，避免等待时每秒重新施法。降低38–190Hz层权重，增强250–1800Hz水摩擦及700–2400Hz带通涡流；保留少量2–6kHz空气细节，抑制持续尖嘶。制作参考功率分配：20–250Hz约35–55%，250–2000Hz约35–50%，2–12kHz约5–15%；用于定向调音，不代替试听。

起音用运行时包络：`g(t)=.32*S(t/.03)+.23*S(t/1.35)`，其中 `S(u)=smoothstep(clamp01(u))`。30ms内建立可识别风水声，再持续增强至G=1.35s；实际回调前维持.55。无额外音源、无每循环新起音。B发生时记录当下g，按既有40ms线性淡出，不要求先到G，绝不为声音推迟B。

**Slazeya命中。** B+0开始，0–12ms是短而硬的水面破裂／电裂，20–100ms由120–900Hz厚水拍击接住；前120ms源RMS目标−12±1dBFS（旧值−15.59），最大主体必须在前80ms出现。避免只有超低轰鸣和针状高频峰。B+.10保留更轻电裂，对应已有次闪，独立裂响层峰值比主裂响低至少8dB，不再叠第二个低频撞击。+.12–.45保持有重量的卷水，+.45–.90退为喷沫／风水摩擦，+.90–1.20稀疏水滴消散；最后30ms平滑至零。整体仍是一击及余波，无枪声式连射、长雷鸣或旋律。

**Velia施法。** .32s内完成暖光开隙：前8–15ms柔软但清楚的“拂开”触感，.03–.18s中频空气向外舒展，.18–.29s留下很短的暖玻璃／水光共振，末30ms归零。以350–1500Hz低Q非谐和阻尼共振、600–2400Hz软摩擦为主体，2–5kHz少量光尘；低于100Hz弱化。共振只作质感，无固定和弦／音阶、持续铃声、人声共振峰或合唱垫底。只在整卡首次入场播放一次，第二骰复用云幕时不重放施法音。

**Velia命中。** 每个真实回调B+0起柔和的水光拍击，5–12ms攻击时间，20–60ms形成清楚的中心音头，贴合原+.04–.07s视觉峰值；前120ms源RMS目标−14±1dBFS。主体为250–1100Hz暖水压与700–2200Hz低Q亮共振，避免照搬风暴电裂或纯白噪声。+.12–.48s保留低于音头的轻柔光尘／气流，对应持续照明；+.48–.69退场，最后30ms归零。两骰同素材、同gain，不叠层、不因受击人数增响；第二骰+.72至+.77自然静音，不拉伸音频或改视觉尾长。

四文件都采用中心占主导的M/S立体声：低于200Hz近单声道，只给中高频有限宽度；无反相拓宽、硬左右扫动或朝向相关响度。镜像无需换声道，暖光／风暴均为全局二维技能事件。优先调层权重／滤波／包络，必要时使用可复现的离线温和动态整形并记录参数；禁止硬削波、靠最终混音归一化掩盖超标。

## 局部混音与触发合同

- 每技能最多一个cast/gather源和一个hit源；音量为上述gain×局部包络×现有 `GetVolumeEffect()` **一次**。主线程已确认 `FarAreaEffect_Steria_OceanWave.ReadEffectVolume` 调用一次，`OptionDataModel.GetVolumeEffect` 返回master×effects；不再乘master。不新建全局volume／mixer／listener修改，不压低BGM或其他技能，不忽略listener暂停。AudioSource保持2D、Doppler=0、playOnAwake=false；原风暴无空间衰减，不从距离补偿找解法。
- 最坏同相重叠幅度上界：风暴 `.55*10^(-7.5/20)+.78*10^(-4.5/20)=.6965`，约−3.14dB；晨光 `.55*10^(-6.5/20)+.75*10^(-5/20)=.6823`，约−3.32dB。因此在option≤1且无额外源重复时，单技能保留至少3dB true-peak估计余量，提前回调也成立。仍须测实际混音；此界不保证含BGM／其他技能的总游戏输出不削波。
- Slazeya仍由 `FarAreaEffect_Steria_OceanWave.GiveDamageFromManager`触发一次burst，含空成功列表；无受方不改变既有不创建声源规则。维持Init／Advance／TriggerBurst／Dispose桥接，不按计时器或人数补造命中。
- Velia增加独立伴随器，由 `FarAreaEffect_Steria_VeliaTideMist`首次Init创建并起cast；通过现有成功的BeginDice分支设置ordinal，实际 `GiveDamageFromManager`通过旧guard后触发该ordinal的一次hit，空成功列表也播放全局脉冲声。重复Init复用／BeginDice／回调不重复声音，过期card／owner不发声。若B早于cast结束，cast从当前音量40ms内淡出（自然终点更早则先结束）；不延迟hit。下一骰仅重新arm一次hit；不写伤害、成功名单、队列或manager状态。
- 同现有风暴规则，音效包络／样本位置跟随scaled time；正常pitch=1，慢速按Time.timeScale（含.005），pause=0暂停并保持位置，恢复同步当前位置，不重放音头。保留既有pitch上限与漂移校正策略，不单独创造音频时钟。AudioListener暂停期间不绕过静音，恢复按当前技能年龄同步。
- 音量滑块／静音在Advance(0)也刷新；mute不重置触发或年龄，解除静音只听未结束部分，不补播已消耗事件。取消、替换owner/card、disable、destroy、既有watchdog立即停本实例声音／释放源；不等尾声完成，不遗留循环。正常结束不得增加manager等待。
- 音频加载失败只跳过失败cue，不阻断另一cue、视觉或结算；缺camera／material也不应阻断有效会话的声音。clip缓存沿用既有策略，验收替换WAV后须用新进程以排除路径缓存旧clip。

## 实施交接与验收

制作owner复用 `generate_slazeya_storm_audio.py` 的分层合成、循环接缝和确定性校验；新增独立 `VeliaTideMist/generate_velia_tide_mist_audio.py`。固定seed、依赖版本、层参数、源hash、格式、全长／前120ms RMS、peak／true peak、DC、mono fold和试听时点写入各自manifest。只调整本音效源验证的旧gain假设；不覆盖历史证据。

运行时owner修改 `SlazeyaStormAudioController.cs` 的gain／包络，新增 `VeliaTideMistAudioController.cs` 及Velia host最小音频桥接。注意旧 `ReadPcm16Wave` 只允许44100／52920 frames，不能直接读取两个新长度；推荐抽出有界纯PCM helper，由旧Slazeya公开wrapper继续保留这两个frames的严格guard，Velia wrapper仅接受14112／31752。保持严格RIFF／数据长度／512KiB上限和按cue精确frames验证，禁止宽泛放开；主线程可按最小改动原则选择独立小loader。视觉controller、shader、贴图、Bundle、镜像和gameplay文件无本任务改动。所有owner须保留同worktree并发修改，不回退他人变更。

| 验收面 | 最小证据与拒收条件 |
| --- | --- |
| 四源与混音 | 精确格式／frames／hash、独立完整解码、确定性重现；满足上表峰值和RMS，clipped=0、各声道abs(DC)<1e-4，mono fold损失≤1.5dB、低频相关≥.85。循环至少连播三次，检查接缝值／斜率及20ms能量无明显凹陷；不以单个零端点证明无爆音。 |
| 施法／命中辨识 | 当前独立检查decoded PCM、前120ms／全长动态、频谱中频存在感、事件混音与native DSP输出，交付可播放原始PCM和同gain链A/B，不做响度归一化。主观目标：两技能均有清楚施法音头与独立命中，风暴厚重强烈，晨光温暖柔和但不被吞没；无刺耳嘶声、机械振铃、循环“喘息”、假人声、音乐或尾声突断。现有工具不支持听觉，主观目标留给外部试听并明确未验证，不据此阻塞制作。 |
| 事件与清理 | 主线程用既有真实路径检查提前／延后／空列表／重复回调、Velia两骰、mute→unmute、pause→resume、.005慢速、取消及缺单个WAV；输出事件时间／次数、sample position、局部gain和清理状态。次数应为Slazeya一聚集一爆发；Velia一施法、每有效骰一命中。 |
| 音画与保留项 | 用已批准当前视觉视频及其timing生成音轨，`-c:v copy`合流，视频流hash不变。Velia r9的205帧/60fps：cast=0、B1=.60、第二Begin=1.65、B2=1.95s；这些仅为预览时点，生产不能硬编码。Slazeya B从当前接受manifest读取，勿套用旧B=1.55。核对音头、主体、等待和归零；不改视觉配合声音。 |

独立reviewer分别给两技能技术范围的 `PASS / REVISE / BLOCK`，写明被解码文件、动态／频谱／事件／DSP证据，固定注明 `subjective_listening: UNVERIFIED (audio input unsupported)`。技术PASS可交付候选，不把波形PASS冒充主观声音PASS，也不因工具限制新增权限阻塞。主线程另核真实game选项／实战混音；若后续有可观测掩蔽或总输出削波证据，回到本地层权重／gain调整并重新冻结相应合同，不改全局混音。最终部署／构建属于后续阶段，主观听感明确留给外部试听，本文不宣称完成。
