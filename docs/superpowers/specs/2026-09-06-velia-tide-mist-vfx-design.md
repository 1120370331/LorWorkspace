# 潮雾晨光：全屏视觉设计

## 主线程审定的接入合同

视觉设计通过。卡9004004的两个ActionScript设为`Steria_VeliaTideMist`；保留原EffectRes、
所有骰子数值和VeliaCards.cs全部增益、自眩、治疗逻辑。CardInfo.xml只修改这两个属性。

BattleFarAreaPlayManager每骰调用ActionScript的SetFarAreaAtkEffect；isRunning控制前奏，
默认manager伤害结算后GiveDamageFromManager每骰一次（含空list），之后等待IsDoneEffect。
禁止在逐目标OnSucceedAttack反复全屏闪光，禁止接管伤害或复制OnEndFarAreaBehaviourAtk。

同一owner+currentDiceAction复用一个持续FarAreaEffect会话，工厂为下一骰BeginDice；
每骰去重，首骰约.32s铺场后开门，后续不再铺场。脉冲结束的Update再读public
cardBehaviorQueue.Count（在OnEndFarAreaBehaviourAtk之后）：有下一骰则IsDoneEffect=true
允许manager继续，并保持低亮底态；末骰统一淡出后释放。骰间Update仍推进与清理。
正常、无回调超时、卡变更、owner死亡/消失或manager结束都有有界清理。

相机使用BattleCamManager.EffectCam的原生OnRenderImage链。自有独立组件/材质实例，
不改其他滤镜、相机设置、RenderSettings，不调用RemoveCameraFilterAll，不销毁相机GO。
无相机/材质正常通过生命周期；OnDisable/OnDestroy释放自有资源。新施放可结束同类旧
会话，旧回调不能释放新会话。渲染controller/host保持纯Unity依赖，OnRenderImage只渲染，
显式时钟只推进一次。资源用独立shader/material-only Bundle，复用只读薄雾图集。

成功目标位置和角色保护范围采用该相机投影；字形/数值及UV上下方向通过原生相机叠加
代理场景检查。仅成功列表产生局部光痕。原生预览不得称为真实战斗/真实HUD验证。
接口依据：BattleFarAreaPlayManager.cs430–626，BattlePlayingCardDataInUnitModel的public
cardBehaviorQueue/GetDiceBehaviorList，BattleCamManager.EffectCam及其既有image-effect链。

## 视觉方案

原生复审校准：r2的矩形保护凹口及均匀日轮被拒绝；r3已修复保护和暖白坡度；
r4b已通过第二峰外围/下侧受光传播。剩余云腹/暖沿不足。主线程批准无角色云bank区域
总alpha上限可提高到约.45–.50，以便软边云脊遮断日轮下缘；已注册人物范围继续轻雾且
保持数字/脸/武器可读。不能因全屏横带将无角色云脊一律压成薄灰带。此项覆盖下文.25
的起始假设；不修改日心亮度、脉冲时序或增加贴图/云生成器。

2026-09-06｜研究设计，已按主线程确认的逐骰及EffectCam协议对齐；本研究未实施、未作原生视觉验收。

**意图。** 将9004004卡图的“冷雾托住暖白晨光、祈祷者保持安静剪影”转译为柔和而有两次明亮冲击的全屏效果。不贴人物；不扩展UI、音效。保留FarAreaEach、Team、两颗Slash及全部强化、自眩、回血/回混乱行为。当前XML两骰均为Steria_WaterSlash；仅此配置不能表达持续晨雾与分骰光变，实际播放问题仍待游戏确认。

**构图与材质。** 从后景观感到前景依次为：上方晨光、两侧及底部云沿、清晰战斗主体、短暂近景光雾。晨光中心约屏幕(0.5,0.72，左下为原点)，可见轮廓宽约45%屏宽；下缘被雾切断，内部有柔和亮度坡度，不能成为均匀大白圆。两侧雾不对称，低速横移，底部更薄；大软形承担体积，纹理只作弱变化。仅2–3片宽而模糊的光束穿云，不做锐利放射条。

雾暗部#344B60、亮部#8FA6B4；光晕#E8C789、局部峰值#FFF5DC，色值为视觉起点。冷灰始终占较大面积，金色集中在日轮和朝光雾沿。屏幕中央角色带雾alpha≤0.08，边沿约0.12–0.25；这些是合成总量，不能逐层叠满。保留脸、衣褶、武器和骰子对比。屏幕后处理的“后景”只是构图关系，并非真实角色深度遮挡；角色区优先依据主线程提供的投影范围柔和减权。

**事件节奏。** 时长是事件后的视觉曲线，不是游戏结算定时器。

主线程已确认：EffectPhase逐骰读取ActionScript，经CreateInstance_BehaviourAction(...).SetFarAreaAtkEffect(attacker)接入；isRunning只阻塞intro，manager伤害后每骰调用一次GiveDamageFromManager(List)，再等IsDoneEffect进入NextDice。同卡持续的FarAreaEffect承载一次铺场，BeginDice重置本骰门；每骰回调只触发一次全屏脉冲，成功目标列表用于局部反馈。第一骰完成仅放行逐骰门、保留低亮雾场，末骰完成才收束释放。OnSucceedAttack不触发全屏闪，VeliaCards增益及恢复逻辑完全独立；具体接口合同由主线程维护。

- 一次铺场：0.28–0.35秒内先显冷雾，再亮云后日轮；等待下一事件时保持低亮度，不循环闪烁。
- 每骰一次脉冲：收到该骰GiveDamageFromManager后约0.04秒升亮、0.03秒短峰、0.20秒衰减。第一骰照亮内云沿；第二骰光域向两侧及下方扩约15%，尾光多留0.05秒，峰值仍封顶。骰间回到薄雾底态，不重新揭幕。
- 成功命中：在实际受击位置出现0.10–0.16秒的小片柔白金光和短斜向光痕，先亮后散；局部反馈跟随已确认成功结果。无成功目标则无局部亮斑，全屏脉冲仍表达该骰发动。不得把每个目标回调都当作新一轮全屏爆闪，也不重新抽选治疗对象。
- 结束回落：最后事件后0.35–0.45秒，近景亮雾先退、云沿随后松散、滤镜归零。中断亦归零。事件拥挤时限制叠加亮度，不推迟或制造伤害结算。

**滤镜。** 以空间遮罩控制亮度，不铺黄色透明矩形：日轮及朝光云沿短时提亮，角色区影响弱，冷雾暗腹不跟着漂白。先试全局增益1.00→1.06→1.00，主要冲击由局部光域完成；保持原图高频细节，局部发光用软饱和合成压住峰值。柔化仅作用于光层，不模糊整幅战斗画面；首版不依赖全局Bloom、自动曝光或HDR，避免阴沉背景一闪变成白板。

**资产与实现建议。** 已查看SlazeyaStormMass/source_assets/round2/mist_density_lighting_4x4.png的检查图：软边、非均匀云腹适合薄雾。参考现有SlazeyaStormParticles.shader：A已含密度及生命周期，R只参与明暗，不再次相乘压碎alpha；使用线性遮罩、帧间插值，长等待需保持中段帧或交叉淡化，不能硬循环16帧。该图集是候选复用资产，须经本效果原生帧确认。

最小新增：独立晨光合成shader/材质、事件驱动控制器及AB资源配置。软日轮、宽光束用平滑形状遮罩结合现有雾纹理即可，无需新画人物、贴图或云噪声算法。不改Slazeya原材质与体积云系统。沿用“AB承载资源、DLL接入、显式时钟、材质实例隔离”的现有分工。

Unity2019 Built-in接入已确定为BattleCamManager.EffectCam上的自有独立post-effect组件，以OnRenderImage→Graphics.Blit加入现有CameraFilterPack链。源图保持清晰，雾作预乘alpha混合，光作受控增亮；src/dst不得同一RT，按实际Gamma/Linear核对色彩。保留原滤镜链，仅销毁自己组件、材质并释放自有临时资源，绝不调用RemoveCameraFilterAll或修改全局RenderSettings。主线程确认链内顺序与UI实际合成效果；本方案无需另开CommandBuffer路线。不能直接移植OceanWave的单次爆发协议；旧WaterSlash是否重复，由主线程原生检查后调整纯视觉路由。

**原生帧验收（最多六项）。** 用实际阴沉战场、角色和UI作底，导出揭幕/等待/两骰峰值与峰后/退出；峰值附近60fps连续帧。

1. 一次揭幕、两次可辨光峰、一次回落；第一骰结束仍保留低亮雾场，多目标不额外产生全屏峰。
2. 同帧能读出暖白核心、暖云沿、冷暗云腹；日轮不均匀，画面不通体黄。
3. 两次峰值中脸、武器、骰子与数值仍可辨；无整屏白帧，新增近白区域不超过画面约15%。
4. 光峰快起缓退；无针状放射、硬圆边、雾片矩形接缝或贴图循环跳变。
5. 成功命中亮斑贴实际受击位置；多目标不互相叠成白团，无成功目标不伪造局部命中。
6. 正常结束及中断后自有滤镜归零、原滤镜仍正常；连续再播不残留雾层、增亮或累计染色。

依据：ModGuideDocs/README；11实战的层次、接入、验证、亮度/噪声分离段；02群体交锋章；VeliaCards.cs、CardInfo.xml；FarAreaEffect_ChristashaGoldenMass、FarAreaEffect_Steria_OceanWave及Slazeya shader/Builder/验证脚本。近期ec0cebe、f534aa7与云内聚设计提示应冻结现有云算法；这里只复用薄雾资产，不复制吸入风暴。

官方实现边界：[Unity2019.4 OnRenderImage](https://docs.unity3d.com/2019.4/Documentation/ScriptReference/MonoBehaviour.OnRenderImage.html)（同相机、串联顺序）；[Graphics.Blit](https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Graphics.Blit.html)（源/目标隔离、色彩状态）。逐骰及相机协议来自主线程本轮确认，本研究不重复反编译；本文约定视觉语义，实现接口合同由主线程负责。
