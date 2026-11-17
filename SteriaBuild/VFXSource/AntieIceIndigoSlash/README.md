# 安蒂冰蓝靛紫斩击

**已部署：candidate-05-convex-to-camera / release-02。** [安装与验证记录](deployment/DELIVERY.md)。司书与敌方安蒂核心的打/斩/突分别使用竖斩/横斩/双刀，横斩凸刃朝屏幕、凹侧朝上略抬。

- [视频：三款正常速度＋0.5x慢放](evidence/candidate-05-convex-to-camera/review-60fps.mp4)
- [侧视与俯视方向图](evidence/candidate-05-convex-to-camera/convex-screen-direction.png) · [横斩凸凹朝向修正前后](evidence/candidate-05-convex-to-camera/angle-before-after.png) · [三种造型阶段图](evidence/candidate-05-convex-to-camera/three-profile-stages.png)
- [原生挂点与两朝向](evidence/native-09/native-contexts.png) · [原生检查](evidence/native-09/checks.txt) · [预览复核](evidence/candidate-05-convex-to-camera/review.md)
- [全部Unity资产清单](evidence/candidate-05-convex-to-camera/asset-inventory.md) / [CSV](evidence/candidate-05-convex-to-camera/asset-inventory.csv) / [JSON与SHA256](evidence/candidate-05-convex-to-camera/asset-inventory.json)
- [Unity项目](UnityProject) · [设计与参数差异](design.md) · [素材来源](provenance.json)

| 骰子 | 效果 | 动画与预览 |
| --- | --- | --- |
| 打击 H | 原竖直下斩，保持不变 | [视频](evidence/candidate-05-convex-to-camera/HitVertical/slash-60fps.mp4) / HitVertical.anim |
| 斩击 J | 横斩凸刃朝屏幕，凹侧朝上略微抬起 | [视频](evidence/candidate-05-convex-to-camera/SlashHorizontal/slash-60fps.mp4) / SlashHorizontal.anim |
| 突刺 Z | 双刀翻转正反，其中一条凸弧朝敌方 | [视频](evidence/candidate-05-convex-to-camera/PierceDual/slash-60fps.mp4) / PierceDual.anim |

独立Mesh/曲线，宽度、流速、内部UV偏移和扫出时长分别调整，共用冰蓝锋面、靛紫拖尾。第二刀延迟65ms。四张用户原图保留字节和分辨率，没有修改或重新生成。

## 打开与播放

用Unity **2019.3.15f1 / Built-in** 打开本目录`UnityProject`，打开`Assets/AntieIceIndigo/Scenes/AntieAuthoring.unity`。另外三张场景分别演示竖、横、双刀。菜单`Steria > Antie Ice Indigo`可重建并打开对应场景；Edit模式循环预览，Play模式由独立Animator/AnimationClip单次播放。

`Assets/AntieIceIndigo`共有33项Unity资产/源码：4张纹理/遮罩、5个Mesh（含辉光Quad）、2个Shader、2个Material、6个Prefab（3制作＋3原生）、3个AnimationClip、3个AnimatorController、4个Scene、4个C#源文件。

## 单位接入与交付边界

仅匹配`SteriaBuilding:99000011`（司书）和`:13`（敌方）核心，三形态及借用书页共用，不按共享皮肤识别。真正的F/Fire、群攻、守备保留原路径；Far书页内的普通J/H/Z按骰子类型使用新效果。共享Water注册和书页XML不变。

- [AntieIceIndigoCombat.cs](../../AntieIceIndigoCombat.cs)：核心路由、角色前方挂点和原生AB加载。
- [HarmonyPatches.cs](../../HarmonyPatches.cs)：注册3个安蒂专用资源键。
- [ModInitializer.cs](../../ModInitializer.cs)：直接攻击入口；[Steria.csproj](../../Steria.csproj)：编译共用AntieSlash.cs。
- [原生资源包](evidence/candidate-05-convex-to-camera/bundle/steria_antie_ice_indigo_combat)：自包含3款原生Prefab，不含自定义脚本。

资源包已安装到`Assemblies/AB/steria_antie_ice_indigo_combat`，并同步更新仅含安蒂增量的发布DLL。实际D盘游戏目录、C盘Steam别名和仓库发布包逐项校验一致。斯拉泽雅资源和既有游戏数据保持原样；工作区其他未发布代码不进入本次DLL。

## 复现

```powershell
python -X utf8 SteriaBuild/VFXSource/AntieIceIndigoSlash/verify_inputs.py
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/AntieIceIndigoSlash/build_preview.ps1 -Revision candidate-next
dotnet build SteriaBuild/Steria.csproj -c Release --no-restore -o output/antie-ice-indigo/build-candidate-07
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/AntieIceIndigoSlash/verification/run_native.ps1 -Revision native-next
```

离屏预览来自真实Unity 2019.3.15f1 / D3D11 GPU，并检查AB冷加载像素一致。原生集成夹具使用实际构建DLL、游戏API和合成角色变换；不代表完整接待实战。候选、证据与已知限制记录在`preview-candidate.json`。
