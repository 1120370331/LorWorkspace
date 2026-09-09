# 战斗单位分层模型

统一规范见 [Guide 12](../../ModGuideDocs/12_战斗单位分层建模与核心书页投影.md)。

| 模型 | 当前内容 | 游戏状态 |
|---|---|---|
| [薇莉亚](Velia_Layered_v1/README.md) | R2、底盘校准、逐动作挂坠、SP 闭眼祈祷 | 已同步 |
| [希维尔](Sivier_Layered_v1/README.md) | 细剑防御/援护，九动作、分层头部与比例修正 | 已同步，待游戏实战 |
| [艾莉蕾尔](Ailierel_Layered_v1/README.md) | 双短刀、后撤闪避重绘、头位与比例修正，九动作与分层头部 | 已同步，待游戏实战 |

`shared/layered_model.py` 统一装配与抗锯齿、鞋底基线换算、衣装前层、预览及导出。`shared/asset_preparation.py` 准备新模型的配准母版与姿势；角色独有坐标放在各自 `source_layout.json`。

不要把不同角色强行装到同一组姿势坐标上，也不要复制三份落地计算。修改共用工具后，应核对已有模型的像素结果；本次迁移核对确认薇莉亚只有 Special/S1 因闭眼要求发生改变。

## 双角色部署

2026-09-09 艾莉蕾尔与希维尔全部新版皮肤已同步至仓库模组及实际游戏目录 `D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`，每个实际目标核验 105 文件。C 盘 Steam 入口解析至同一 D 盘目录，仅备份写入一次，入口核验同样通过。[部署报告](deployments/20260909T044355Z-ailierel-sivier-package-fix/deployment.json) 状态为 `deployed_verified`，[备份](deployments/20260909T043402Z-ailierel-sivier/backup/) 保留部署前文件。

敌方书页 8/9 保持 `Ailierel` / `Sivier`，完整外观已烘焙角色头部并关闭原生头部；司书书页 99000007/99000008 使用各自 `Layered_v1_Projection`，仅穿戴衣装与武器并保留司书头部。DLL、敌方书页文件和 CardInfo 未改写。艾莉蕾尔已做制作端浏览器交互检查；希维尔已做静态预览和 12 动作验证。两者均尚未游戏实战，游戏下次启动加载新资源。

仓库根目录重跑以下命令，默认只做 dry-run；核对计划后追加 `--apply` 才执行备份与部署。工具自动包含仓库模组目标，默认每次使用新的时间戳报告目录，避免覆盖历史备份。

```powershell
python SteriaBuild/CharacterModels/shared/deploy_runtime_models.py --model SteriaBuild/CharacterModels/Ailierel_Layered_v1 --model SteriaBuild/CharacterModels/Sivier_Layered_v1 --game-mod "D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder"
```
