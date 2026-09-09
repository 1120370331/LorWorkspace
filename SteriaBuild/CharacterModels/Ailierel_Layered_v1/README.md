# 艾莉蕾尔 · 分层模型 v1

黑蓝短发、天然猫耳、黄绿色眼、短黑夹克与黑裙、长袜短靴、双短刀。动作强调低重心、侧闪、收刀和连续刺击；猫耳与固定发夹保留在头部母版中。

按 Guide 12 的同一制作方法完成：以当前角色作身份参考，三个衣装/握持姿势组和一个配准头部母版；纯品红背景处理后分层，未把效果光圈或粒子烘焙进人物。

## 查看与编辑

- [九动作预览](build/poses.png) · [原版对照与换头](build/comparison.png) · [锚点检查](build/anchors.png)
- [交互预览](build/preview.html)：切动作、头部、衣装、武器、朝向与锚点。
- [源图坐标与分层区域](source_layout.json)：衣领、支撑脚、肩肘腕、武器与头部层的作者数据。
- [生成提示词](sources/generation.md) · [源图与头部配准结果](sources/provenance.json)
- [模型配置](model.json) · [技术校验结果](build/verification.json)

头部的前后发、脸型、眼眉鼻嘴使用同一画布和变换；正常、攻击、受击表情沿用同一头部轮廓。衣装与武器先共同绘制保证握持自然，再分层。固定扣件和花纹已经画入衣装，独立饰品槽为空。

本次返修将头部（含猫耳与头发）围绕原颈根缩小约 13%，统一缩放由 `0.235` 改为 `0.205`。原横斩穿戴母版的肩臂空间关系不成立，已用 [单动作补绘](sources/raw/slash_repair.png) 替换：攻击臂向左下展开，后手收在后侧，肩肘腕与刀柄连成完整动作链。其他动作的衣装源图保持不变。

2026-09-09 头部挂接修正：闪避 `Evade` 的颈根改为 `[211,395]`、头部角度为 `-8°`，移动 `Move` 改为 `[1183,401]`、`-18°`，受击 `Damaged` 改为 `[720,339]`、`+4°`。整头随颈根下沉，让闪避与移动的前倾跟随躯干，并收敛受击的后仰；统一缩放仍为 `0.205`。头部各层共同变换，身份原画、五官配准、所有衣装与武器源像素、鞋底以及其他六动作保持不变。当时仅更新可审阅预览和运行时包；后续版本现已部署，见下方游戏部署记录。

2026-09-09 闪避后续重绘：参考罗兰与漆黑噤默的收臂、错步和后撤重心，用内置图像工具新绘 [Evade 无头衣装双刀母版](sources/raw/evade_redraw.png)，替换原来的宽半蹲；改为右后脚接重、左前脚抬跟回收、屈肘收刀并朝左警戒。依据新原图重新标定 Evade 的衣领、肩肘腕、刀刃与脚底，实际配方以 `source_layout.json` 为准；[生成记录](sources/evade_redraw_generation.md) 保存准确提示词与来源。头部身份与 `0.205` 缩放、其余八动作（含已修正的 Move / Damaged）保留。重绘通过独立视觉审查，预览、运行时包与正式游戏资源现已同步。

底盘采用已核实的 Bada 默认鞋底 `+0.06` 单位基线。测量只读取身体与鞋底，排除刀剑；每动作 Pivot 和原生头部坐标同步换算。所有攻击朝左，由游戏自动翻转。

Special 为低重心蓄势伏击姿势，S1 明确复用此动作。

## 重建

在仓库根目录运行，依赖 Python + Pillow，无需再次生图：

```powershell
python SteriaBuild/CharacterModels/Ailierel_Layered_v1/prepare_model.py
python SteriaBuild/CharacterModels/Ailierel_Layered_v1/build_model.py
python SteriaBuild/CharacterModels/Ailierel_Layered_v1/verify_model.py
python SteriaBuild/CharacterModels/Ailierel_Layered_v1/package_model.py
```

修改原画或 `source_layout.json` 后执行 prepare，再 build。仅重新导出已准备好的模型时，从 build 开始即可。装配、抗锯齿、原版落地换算与预览共用 [shared](../shared/) 中的实现。

## 交付范围

9 个姿势：Default / Guard / Evade / Damaged / Slash / Penetrate / Hit / Move / Special；3 个明确别名：S1=Special、Fire=Penetrate、Aim=Guard。12 个条目的 RGBA、边界、换头隔离、母版还原与原版鞋底基线已验证。

[运行时包](build/runtime/) 已部署：敌方书页 8 保持 `Ailierel`，使用完整外观并关闭原生头部；司书书页 99000007 已映射为 `Ailierel_Layered_v1_Projection`，使用衣装/武器并保留司书头部。正式皮肤与司书书页映射已同步，DLL、敌方书页文件及战斗卡牌数据未改写。

已完成制作端浏览器闪避、左右翻转、司书换头和 head 显隐检查，仍未做游戏内实战。只提供 Front 视图；身体肤色仍随原画，技能粒子沿用独立运行时系统。

## 游戏部署

2026-09-09 与希维尔一起部署，当前艺术候选为 `9d5dc2097f9f2077bfeeac54d931d8b70a741a1a76840a8c76a4621cbaf210de`。[部署报告](../deployments/20260909T044355Z-ailierel-sivier-package-fix/deployment.json) 状态为 `deployed_verified`，[原文件备份](../deployments/20260909T043402Z-ailierel-sivier/backup/) 已逐文件校验。仓库 `SteriaBuild/SteriaModFolder` 与实际游戏目录 `D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder` 各核验 105 文件；C 盘 Steam 入口解析至同一 D 盘目录并通过相同核验，只备份写入一次。

重复打包现可识别已启用的司书投影映射。刷新工具来源版本后，104 个皮肤文件与首轮部署逐字节一致；再次部署核验通过且无需改写文件。原皮肤备份仍保留在首轮部署目录。

游戏下次启动将加载新资源；文件部署通过不代表游戏实战通过。需要重跑时，使用[双角色部署命令](../README.md#双角色部署)，默认先 dry-run，再显式加 `--apply`。

## 本轮经验

- 持械角色的剑尖不能参与鞋底测量，也不能被预览的固定窄裁切截掉。
- 头部可以按整头轮廓配准；五官不得分别缩放找位置。
- 武器和手先联合绘制再拆层，避免独立生成后手指、护手、刀柄接不上。
- 共享工具负责共同规则，角色姿势、轮廓、比例仍各自独立设计。
