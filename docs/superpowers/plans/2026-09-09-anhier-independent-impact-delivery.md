# 安希尔独立攻击特效交付

用户最终边界：独立原创特效；原版只作为尺度、时序与挂点参考，不复制或绑定巴达皮肤；直接模拟，无需游戏内操作。

## 结果

候选 `a3c8437f4f93287847e331edf14f8d02ca60cc35e3714863c140f5b77ede2df2`，主线程 ACCEPT，独立视觉评审 PASS。海潮/回忆六类表现及原有18颗骰子绑定保留，寒地后勤仍属回忆，回忆守备保留。主展示为独立绘制的中性人形，两个尺度和左右方向；没有角色自带金色效果或巴达皮肤像素。生产核心/适配器无 Bada 或 char_418 身份依赖，104张运行时PNG均与原创清单相符，未直接使用原版导出图片。

近战继承完整动作挂点与力量尺度，首帧呈现主体、保持约250ms后退去；纵斩采用新原创素材，水平弧用于打击。远程区分发射与目标水花，目标只提供位置。动画初始化时钟与后续销毁时间分离，避免错误缩放播放节奏。所有角色皮肤、核心书页与书页数据未修改。

## 验收

- 主线程核验1001个候选指纹，亲自运行源验证并检查主体、守备、远程与反向中性人形画面。
- 精确生产适配器、播放核心和原版基类逻辑在受控托管变换夹具中执行，215项检查通过。它是CPU模拟，不是Unity GPU截图；两套本机Unity2019编辑器的现有许可证阻止了引擎启动，未更改许可证。用户明确接受直接模拟，此项不作为完成阻塞。
- 引用实际游戏DLL的Release编译通过，0错误、7个已有警告。
- 独立视觉评审没有必须返修项，报告见同目录 `2026-09-09-anhier-independent-impact-review.md`。
- 主媒体：`SteriaBuild/VFXSource/AnhierTextureCombat/previews/independent-all-six-60fps.mp4`（60fps、540帧、9秒）及`independent-all-six-30fps.gif`（3秒循环，帧延时至少30ms）。旧Bada/站姿预览均非本次主交付。

## 同步与重现

已同步实际D盘安装的112个文件副本（110项原创资源、游戏DLL、仓库发布DLL），逐文件哈希一致；CardInfo未变。DLL SHA256：`3a1fe8b13d39098ca403f06f94ed4a2efddfa34f183e40bd7076a52b270f7f35`。部署与备份记录在 `output/anhier-native-impact/acceptance/deployment.json`；备份目录 `C:\Users\rog\WorkSpace\projects\games\lor\output\anhier-native-impact\acceptance\backup-20260909-091302`。早期研究启动的游戏已关闭，存档前后哈希无变化。

在仓库根执行：

```powershell
powershell -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/AnhierTextureCombat/run_managed_preview.ps1 -BuildRelease
```

此命令不打开游戏或Unity。保持未修改母图时无需重新生图或派生素材。
