# 倾覆万千之流：外围云带、冷色天气与细雨

2026-09-08。研究设计交主线程冻结；本文不代表实现或视觉验收通过。用户：「可以给倾覆的特效外部增加一些副云带运动来丰满画面，以及增加颜色滤镜天气氛围感，雨滴效果」。

## 意图与边界

在现有成熟主云、收核及水流爆发周围增加有方向的稀疏天气运动，并让战场由暖灰进入冷灰蓝雨幕。主云仍是最大、最厚的质量主体；持核时外围保留轻运动，爆发时天气让位于水沫和闪电。不改伤害、Gather=1.35s、Tail=1.20s、真实回调、既有一次镜头抖动、两次闪电和暗间隔、声音或全局相机状态。

采用独立单 pass 天气图像效果：三个世界投影锚定的破碎副云弧、轻量冷色调、两层细雨在同一 shader 合成。新增零 ParticleSystem、零主云体积分、零光源、零渲染相机；现有八系统、一个主云 proxy、主云所有着色与水体波前保持。新增层是空气中的薄云丝，不是假装又一层实体主云。

## 已读依据和诊断

已读 ModGuideDocs/README、SteriaBuild/SteriaModFolder/README、教程08编译部署、教程10特效生命周期/AssetBundle、教程11参考分层/混合/构建验证；9月7日爆发设计及交付、风暴晨光声音设计及交付相关内容；canonical VisualController、OceanWave host、Builder、Cloud/Particles/Flow shader、source verifier、build脚本及最近提交。当前视觉版本 `2026-09-07.burst-abundance.2`，相关提交 `f1e7587`；工作树 HEAD 还有后续音乐骰子变更，不将其倒退。

视觉实看 `preview_exports/slazeya_storm_mass/round6/reviewed-20260907-1559/` 中 `battlefield_02_cloud_swirl.png`、`battlefield_03c_core_hold.png`、`battlefield_06_waterfall_B14.png`、`battlefield_08_low_mist.png`。原生代理场景，非实战录屏。该交付 Bundle SHA256 为 `524AECBB8C8DAD193857D88436BB0D2F0D1A0EC9119E05774AEE48ECA0CC5F48`。

- 聚集主云右缘已接近画幅，简单把外云做成更大闭合环会裁切画面；主云上、左侧还有空气可用。
- 收核后的画面只剩极小白核及角色，背景暖灰完全不变，施法从大云过渡到持核时天气重量断开。应由低密度外云/雨维持空间运动，不放大或重做核。
- 爆发已有四个强弱不等的水舌、薄沫和飞滴。新增雨应是统一受风向下的细线，与向外喷出的不规则短沫分开；增大水滴或用瀑布 atlas 延长会混淆两者。
- 旧底雾只在爆发尾部存在，不能充当天气前奏。副云需早起、缓动、在持核继续漂移，随后在原尾长内清空。

参考复用了仓库 Velia 的独立纯 C# driver + camera component + cloned material 模式。外部只核对 [Unity 2019.3 OnRenderImage 文档](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/MonoBehaviour.OnRenderImage.html)：多个 image filter 顺序接收前一个 destination，所有路径必须输出 Graphics.Blit。没有引入第三方云 shader、外部纹理或新的渲染管线。

## 冻结时钟与三权重

`G = visual.Elapsed`；`B = visual.BurstAge`（未回调为 -1）；`S(x)=smoothstep(0,1,clamp01(x))`。天气不拥有第二套累加时钟，不按 GatherDuration 自造爆发，不在 OnRenderImage 更新状态。

三个可诊断强度 `CloudWeight / GradeWeight / RainWeight`，默认各为 1，独立开关只供预览隔离和有界调试，运行时不随机改变。共同总包络 `E=S(G/.32)`；回调后乘 `1-S((B-.30)/.80)`；`visual.IsComplete`、Cancel、Dispose 或 B>=1.20 时严格为0。B=1.10已自然归零，1.20是硬清理边界。

| 分量 | 局部曲线及上限 |
| --- | --- |
| 云 | `E * (B<0 ? 1 : lerp(1,.55,S(B/.12)))`；三弧合成后的覆盖 alpha≤.30，采用 max/并集密度后一次合成，不让重叠叠白。 |
| 调色 | `E * (B<0 ? 1 : lerp(1,.72,S(B/.08)))`；有效 grade mix≤.28；不得把闪电做成第二次全屏曝光脉冲。 |
| 雨 | `E * S((G-.12)/.46) * (B<0 ? 1 : lerp(1,.60,S(B/.10)))`；远层每线峰值alpha≤.16，近层≤.24；仍被同一E清尾。 |

提前回调从当时E连续衰减；迟回调在原watchdog内维持缓动，不重复入场、不无限积累。暂停时所有雨滴/云相位冻结；Advance(0)只刷新投影/有效性，不推进运动。

## 外围副云的明确构图

从 footprint 的初始主云盒 `InitialMacroPose(f,0,0)` 投影出屏幕中心及屏幕椭圆尺度；使用实际 camera.WorldToViewportPoint，采用固定初始外形作空间锚，不跟随主云收缩至针点。投影数据由host/preview传入driver。宽高要有有限值/正值检查，相机后方或投影不可用则跳过云与依赖锚点的保护，不把NaN传入shader。正常镜头变化每帧重算投影，shader内不读自造相机。

以屏幕右为0、上为π/2，在该椭圆外侧做3段**不闭合**云弧。实际主云投影若不规则，只以此定大致位置，弧本身必须有破口及非同心偏移。

| 弧 | 角中心 / 全宽 | 相对初始主云椭圆半径 | 形状与运动 |
| --- | --- | --- | --- |
| 左上主副带 | 2.45rad / .95rad | 1.10 | 局部中心向屏左偏 `.055*椭圆宽`、向上偏`.02*椭圆高`；有效厚度`.065～.12*椭圆短轴`；角速度−.11rad/s；最明显的一带。 |
| 右后高云 | .95rad / .65rad | 1.14 | 局部中心向上偏`.10*椭圆高`；厚度`.055～.085*短轴`；角速度−.075rad/s；alpha为左上的.70；避免正右最外侧堆积。 |
| 左侧下方断丝 | 3.50rad / .42rad | 1.05 | 中心向左偏`.07*椭圆宽`；厚度`.025～.05*短轴`；角速度−.15rad/s；alpha为左上的.45。不得发展成前排围栏。 |

以上角度为首帧构图，局部运动角偏移有界于±.15rad（平滑缓动或对总相位做有界映射），长期持核不把弧转到镜头右缘形成新堵塞。细密度沿负切向持续平移，速度约`.12～.22*H/s`的投影量感；三层速度不同，径向扰动≤.035*椭圆短轴。不得整张正弦呼吸闪烁。主体骨架每弧2～3处强弱不等软团，切向拉长、边缘撕细；低频密度+独立细噪侵蚀，两级足够，不能用等间距圆珠/重复相同亮筋。

弧首尾用宽软锥形渐隐，各弧至少一处明显断口；三个弧总覆盖角不超过约2.0rad。距离主云轮廓有间隙，最大只在局部软边相接，不能成为第二闭合环、双壳、平面椭圆描边或大张半透明板。屏右最外8%、顶端8%做额外软退场，禁止硬裁切。云色范围钢蓝灰：暗部约(.13,.19,.23)，主体(.30,.39,.46)，极少亮丝不超过(.50,.59,.64)，不加发光、金边、彩虹、强白描边。可采用单层程序化密度场；无需生成新bitmap或调用imagegen。

## 冷色调与雨

调色从源图求亮度，轻降饱和（完整grade目标只降低约12%），乘冷色比率约(.78,.90,1.05)，再按最大mix=.28混回source。暗部可混入极轻钢蓝空气色(.12,.19,.24)，最高有效alpha .025；原黑背景不能抬成蓝色屏幕。中高亮度保留系数 `1-.8*S((luma-.50)/.40)`，避免主水沫/闪电被染成青块；不 blur、不 warp、不降全局曝光、不控制BGM或原后处理。整体source alpha原样输出。

雨在同pass内两个独立seed的稀疏解析线场，统一从屏上向左下落（与云负切向相符）。使用分格hash给每条起点/相位/长度/亮度，邻格独立；不能显示规则网格、每行同步更新或重复斜杠壁纸。按解析cell周期直接算当前帧，无累计粒子，无补发历史雨。

| 层 | 720p基准尺度 | 倾角/运动 | 密度 |
| --- | --- | --- | --- |
| 远雨 | 宽.65～1.0px、长8～16px，软尾 | 相对竖直左倾12°～17°；约300～430px/s | 可见区域同时约45～65条，低反差钢蓝灰 |
| 近雨 | 宽1.0～1.4px、长18～32px；极少35px | 同方向左倾17°～23°；约500～680px/s | 同时约12～18条，亮度稍高，避免正中 |

尺度按 `source.height/720`换算，至少有抗锯齿软边；不依赖相机world单位使不同分辨率雨突然变成宽水条。屏幕椭圆保护区以核心投影为中心，半径约(.15,.19)viewport；中心雨与副云削弱85%，外围柔和恢复。每个角色身体投影沿用Velia软椭圆保护思想，云/雨在头与上身至少削弱75%，grade可保留≤原强度的40%。不对身体用可见矩形挖洞，不掩掉水流自身。近雨的强条优先在两侧与上部空域，最多偶发1～2条划过中心外围，不能形成持续横越头部的高亮线。

只做空气落雨；不加镜头玻璃水滴、折射污迹、地面大溅点或新雨声音。B+.8雨应已非常稀薄，云片宽面不可残留，B1.10全零。

## 最小文件与接口合同

新增canonical：

- `SteriaBuild/SlazeyaStormWeatherController.cs`：plain C#、IDisposable，构造绑定 `SlazeyaStormVisualController visual`；`SetProjection(Vector4 cloudEllipse, Vector2 coreViewport, Rect[] bodyRects)`，`Apply(Material material,float aspect)`，`Cancel()`，`Dispose()`；只读 `Envelope/CloudWeight/GradeWeight/RainWeight/IsComplete` 诊断。诊断weight可有显式配置方法，默认1。不引用game类型、不自行Advance累计。
- `SteriaBuild/SlazeyaStormWeatherScreenFilter.cs`：MonoBehaviour，`Initialize(SlazeyaStormWeatherController,Material template)`克隆一次材质；`Release()`幂等，只释放自己material；OnRenderImage只Apply+Blit，所有跳过路径Blit原图。OnDisable先Cancel本weather再Release，不能Cancel原visual/伤害；OnDestroy幂等释放。
- 上述二文件镜像到 SlazeyaStormMass UnityProject/Assets/Scripts；新增同项目 `Assets/Shaders/SlazeyaStormWeather.shader`，shader名 `Steria/SlazeyaStormWeather`，单pass、target3.0，UI/source UV翻转沿用Velia已有实现要点。禁止GrabPass、_Time、Camera.main和全局Shader.SetGlobal调用。
- Builder新增一个单独打入原Bundle的 `SlazeyaStormWeatherMaterial`，不新增可见carrier到主prefab；使用已有Bundle加载途径取得模板。host `FarAreaEffect_Steria_OceanWave.cs`只新增可选weather伴随器、EffectCam组件及投影刷新/清理。缺材质、shader不支持、缺相机、无有效目标时跳过天气，不改变既有视觉/音效/伤害结算。

`SlazeyaStormVisualController`不需要生产行为修改；若仅暴露既有footprint供投影，应优先使用host已持有footprint，不扩公共接口。旧shader/纹理/粒子/版本值不为了天气顺便调整。主线程已核对当前Steria.csproj使用SDK默认编译glob，只有四组Compile Remove，没有禁用EnableDefaultCompileItems，也没有Compile Include；新增两个root .cs会自动包含，无需修改csproj。保留其原有InputLegacyModule引用工作树改动。

运行时只给 EffectCam.gameObject.AddComponent 自己的filter，不调用BattleCamManager.RemoveCameraFilterAll，不查找/销毁别人的同类实例，不修改其他filter启用状态、camera depth、cullingMask、targetTexture、Transform、FOV、RenderSettings或共享material。Dispose顺序：weather.Cancel → filter.Release/销毁该组件 → weather.Dispose → 原既有清理。游戏主动disable本filter只使本天气失效，不能使原burst消失。

## 最终预览调整

首候选右高云被投影盒外缘与屏边保护挤出，最终改为左上主带、左下次带和中间小断丝；目标位于另一半屏时镜像局部云域，仍由世界初始投影锚定。天气专用纵半径为投影盒的 .78，三弧角中心约 2.85/3.98/3.32，跨度 .85/.62/.30，主次宽度 .12/.10；低频沿角尺度 4.5、软肩底密度 .45。原 alpha 上限、雨、调色、保护和尾部时序保持。

局部断云最终使用噪声偏移的二维乘法软衰减，保留少量薄丝；取消贯穿径向的直切及减法截零，避免硬切线或暗色咬口。提前回调后入口包络使用 S((G-B)/.32) 冻结于回调时刻，再乘退场曲线，不会再次增强。最终视觉候选和正式复建 AB 均为 F9969F0DD3F3154945993D509E466E55E8FFDE619A644D9E50C14F5C5AD77462。

## 相机与UI证据边界

`BattleCamManager`的EffectCam getter、GetAllCamera、AddCameraFilter仅证明有独立`_uiCam/_effectCam`并在EffectCam附加image effect；源码没有给出这些camera的serialized depth/culling/targetTexture，不能据字段顺序断言UI后绘制。Velia现有接入同一EffectCam且保护身体，是可用生命周期参考，不是“UI必不受影响”的证明。

首候选按保守空间保护：上方12%、底部20%、左右4%的viewport范围对全部weather渐隐到0；邻接区域用至少3%软过渡，中心/身体保护如上。这降低战斗UI受染风险，但不能作为所有UI均未覆盖的证明。真实合成顺序或复杂UI仍需外部实战验收；若发现UI在EffectCam source内且落在保护外，补具体UI区域保护或修实际接入相机，不能直接宣称已经无影响。

## 预览与接受条件

仍走原build脚本FirstCandidate→独立review→Reviewed；所有新runtime/镜像/material/shader/Builder/build/verifier输入进入visual-input-sha256，不能绕过已审冻结和AB重建一致门。新目录round7，不覆盖任何round6。主线程负责真实acceptance环境，worker只使用独立构建/预览进程。

Native preview在现有 `CreateCamera`（H=6、1280×720、ortho13.8、原五目标布局）的camera上附加真实weather filter，driver绑定**同一个实际SlazeyaStormVisualController**，用同camera投影脚点/身体与主云初始盒。原Render(camera,RT,pixels)路径真实执行OnRenderImage，不另做PS叠图。现有主云/粒子/core像素/容量验证在未挂weather的原camera或weather关状态运行；不能删旧门或用全画幅天气去否定主云有限核合同。原core baseline应保持相同。

代表图采G=.20/.55/.94/1.28、hold G=2.0、B0、B+.05/.116667/.25/.45/.80/1.10/1.20；选择与旧manifest相同时点并记录实际时钟，避免.12冒充.116667。同候选同模拟帧同时输出weather off/on；G=.55、G1.28、B+.25各补cloud-only / grade-only / rain-only，作为局部诊断，不额外重跑整套场景。至少输出完整固定镜头视频/连续帧，附两段间隔约.10s的原图看云位移/雨运动；单帧好看不足以通过。

1. Early G=.20：已有浅冷空气与左上断云，雨刚开始，不能全屏蓝罩突然落下。
2. Mid G=.55/.94：主云厚度/形状与旧基线保持，外围可辨两段主副带和一小断丝；右缘不新造闭合环、双壳/等间距圆珠/重复贴片。副云明显弱于主云且确实位移。
3. Hold G1.28/2.0：小核仍最亮、角色头部清楚；外围小雨/云提供天气连续性，不能围成第二主核或停止为静态装饰。
4. Burst B0～+.25：真实主波前/闪电/水沫可读；雨与水沫方向、粗细明确不同，不能把暗间隔填白；外围强度在.12内连续回落，不新增爆发。
5. Fade +.45/.8：cloud/rain/grade都逐步弱化，+.8只极轻空气细节，+1.10输出与weather-off一致（允许RT读回1LSB），+1.20组件/材质清理、无残留循环；取消/缺资源同样保证pass-through。
6. Lifecycle：pause前后相位不变；重复callback不重置天气；延迟callback不积雨；两个独立weather实例释放一个不影响另一个/Velia/原相机filter；原native主云/爆发/source gates通过。

拒收优先次序：遮挡角色/爆发、云弧像同心描边或复制板、雨变成宽长水条/规则斜杠、grade把全景染青、B1.1后残留。先下调相应独立weight或局部宽度/密度，保持三层职责及主云冻结；不得靠重改主云或抖镜过审。主线程与独立reviewer以原始运动预览决定PASS/REVISE；本设计仅提供可实施首候选，不预告视觉PASS。
