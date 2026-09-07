# 潮雾晨光：移动光束与施放朝向交付

2026-09-07。按用户要求新增光束本身的缓慢摆动，并使友方施放时朝敌方区域照射；已同步游戏目录。

## 最终行为与接入

三束围绕原方向缓慢摆动，使用整卡Elapsed和3.6秒周期，摆幅10°/7.5°/5.3°，相位-1.68/-1.62/-1.55；角度按屏幕宽高比换算。跨骰不重置相位，原r8起亮、保持、.72/.77秒总时长、.14秒局部命中和.40秒淡出保持。

host创建会话时缓存`Player XOR AllyFormationDirection.LEFT`：常规Player在右时，特效镜像朝左侧Enemy区域；Enemy在左时保持朝右；左右交换队形也对应反转。依据是游戏`BattleUnitModel.formationCellPos`与`StageController.AllyFormationDirection`。stage缺失使用常规RIGHT约定。

只镜像特效fxUV；相机原图sourceUV、人物保护和实际命中screenUV保持。纯Unity controller仅增加布尔方向和材质参数，game类型止于host。云图、atlas、亮度层次和原有.65云影漂移保持；无新素材、额外结算或滤镜状态。

## 实施与视觉验收

Einstein设计，主线程确认游戏方向合同并冻结参数；Lorentz实施；James对first-r9-enemy、first-r9-player分别返回PASS。主线程阅读10个源码文件的变更，检查双方起亮、保持、命中、退出帧以及被测量标记的frame59/60相邻帧。

两个原生场景分别构造Enemy施放者在左/目标在右、Player施放者在右/目标在左，实际投影人物和命中坐标，没有翻转渲染后的源画面。各205帧、1280×720、60fps、3.416667秒，回调frame36/117，第二Begin99。

恒定亮度+.18到+.48秒的实际像素剖面显示：在y=.25，Enemy内/中束第一段位移+35/+28px，第二段-21/-26px；Player反号。稳定内/中束连续跟踪每帧0–3px，方向一致、没有交换顺序。可辨主束/暗隙、冷蓝云腹、暖白日心、人物与数字保持，源方向探针未反转。

外束frame59→60出现最强亮峰取样切换28px；主线程和独立复审补看相邻帧，未见整束跳位。验收以稳定内/中束为移动证据，不把这次外束取峰切换解释成真实角度位移。原始数据及方法在`first-r9-evidence/measure_native_rays.py`、双方`native-ray-motion.json`及`native-ray-tracks.csv`。

## 最终门禁

- 源验证、真实game API编译、最终`git diff --check`通过。修改前新增原生镜像检查使旧r8失败，新候选通过默认值、材质uniform、原始命中/保护坐标、跨骰相位及暂停检查；r8时序检查保持通过。
- Unity2019.3.15f1/Built-in/Gamma/D3D11只构建一次正式Bundle、只加载一次材质，分别导出`reviewed-r9-enemy`和`reviewed-r9-player`。单material根和shader/两贴图依赖、真实RGBA32回读及alpha一致性通过。
- 两方向各20张正式/已审代表帧比较最大通道差1/255，单帧最多2/8个像素差异，未见可见变化。双方退出像素差0，新增近白最大3716/3717除921600，约0.40%。两个视频全帧编码/解码及时间检查通过。
- 主线程47项原生流程检查全部PASS：原34项生命周期，加常规Player方向、效果镜像的真实光栅对照、非对称源探针保持、实际命中而非反射命中位置、四组阵营/队形及缓存、stage缺失回退。最终证据`output/velia-tide-mist/acceptance/run-20260907-121350`与`main-acceptance.txt`。
- 首轮夹具在Envelope=0时读取尚未Apply的材质默认值，两个镜像组合误报。依据filter零包络直通逻辑改为首个有效渲染帧采样后复验通过；生产代码未因此改变。原始失败证据保留`run-20260907-121119`。
- 隔离DLL构建0错误、7个既有警告。相较上次编译输入仅controller/host改变，交付前全部编译输入及原生受测文件哈希稳定。其他卡牌、数值、XML、素材与无关VFX不纳入修改。

视觉审查是原生帧、相邻帧和轨迹抽样，未完整观看连续视频；不认证所有瞬时闪烁、真实游戏/HUD或GPU性能。原生流程使用生产host/controller/filter和最小游戏接口替身，不能冒称实际战斗录屏。

## 产物与同步

12:16同步仓库Mod和C/D两个游戏逻辑路径的DLL、Bundle及.ab，12个逻辑文件复核通过，所有CardInfo.xml前后哈希保持。C游戏路径是D安装的Junction。

- DLL：`C3480E1A123B55C435620A89BB2B458CC41BC7FBAB3407CD350397164F23CA35`
- Bundle：`2E390F63478FC285A89AB1D77264A292CA754E03C0BC5398BDF9C0802F8F88FE`
- Player视频：`7DD28D8616C4C2C108EF0D449FA7BD6FD6FABE6D75987EA703E7AC925035954A`
- Enemy视频：`5113664D721B0040AC8F12A14298E2918864B0A9B65723764840575D1CC079D9`

回执：`output/velia-tide-mist/moving-beams-facing/deployment-receipt.json`；备份：`output/velia-tide-mist/deployment-backup-20260907-121621`；隔离候选：`output/velia-tide-mist/delivery-20260907-121120`。正式双向视频和完整source/bundle/facing/timing/像素对照证据在`preview_exports/velia_tide_mist/reviewed-r9-enemy/`与`reviewed-r9-player/`。

按模块README用新的PreviewName复现；最终`-Mode Reviewed -Facing Both`一次构建后输出两个方向。游戏端需本次DLL和Bundle配套，已全部部署。
