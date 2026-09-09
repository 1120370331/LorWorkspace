# 原版贴图攻击特效参考库

从本机正版《Library of Ruina》安装目录读取原始资源，保留真实 alpha、Sprite pivot / PPU、材质、层级、动画曲线和资源绑定关系。原版图片只供本地研究，`references/` 已被仓库忽略，不能放进 Mod 的 `Resource`、`Assemblies/AB` 或其他发布目录。

## 生成与复核

Python 3.12，依赖见 `requirements.txt`。可复用已有隔离依赖目录；不必修改系统 Python：

```powershell
python -m pip install --target output/anhier-texture-vfx/native-reference/python-deps -r SteriaBuild/VFXSource/NativeAttackReference/requirements.txt

python SteriaBuild/VFXSource/NativeAttackReference/extract_native_attack_reference.py `
  --game-data 'D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data' `
  --python-deps output/anhier-texture-vfx/native-reference/python-deps

python SteriaBuild/VFXSource/NativeAttackReference/extract_native_attack_reference.py `
  --verify-only
```

本次实际使用已有的 `--python-deps output/music-dice-ui/python-deps`（UnityPy 1.25.3 / Pillow 12.3.0），未安装或升级系统依赖。默认输出为本目录的 `references/all/`。

可用 `--root-regex '/(dawn_[jzh]|philipego_[jzh]|basic_[jzh]|kurokumo_[jzh])$' --output ...` 快速生成子集；`coverage.json` 会保留筛选表达式，子集不能冒充完整库。

`--verify-only` 复核原图、联系表和元数据文件的 SHA-256，以及每个特效根索引引用的图片是否实际存在。源游戏文件的 SHA-256 存在 `manifest.json` 的 `inputs` 中；游戏更新后应生成新的库重新比较。

## 浏览顺序

1. `coverage.json`：先看范围、错误、缺失和不透明组件数量。
2. `contact_sheets/slash_*.png`、`thrust_*.png`、`hit_*.png`：分组索引，每页最多 30 张，按原始资源名标注；`all_sprites_*.png` 和 `all_textures_*.png` 为完整图像索引。`theme_fire`、`theme_ink_purple`、`theme_blue` 汇总 Philip/Xiao/Liu、Kurokumo/Purple Tear、Argalia 的代表图，仅按这些已知资源名筛选，不把色系分类当作玩法归属。
3. `manifest.json`：由 `effect_res_mapping` 查书页 XML 中的 `EffectRes`，由 `roots[].resource_path` 查 prefab，再查 `images`、`animations`、`objects`。
4. `metadata/`：按 `objects[].metadata` 找到真实材质、Transform、SpriteRenderer、AnimatorController、AnimationClip、粒子时序及 ParticleSystemRenderer 的渲染模式/排序。动画 JSON 保存原始 Unity 曲线，包括打包后的 dense/streamed 数据；不是重建的动画截图。Unity 非有限浮点数写成明确的 `{"unity_float":"Infinity"}` 等值，保留含义同时使文件符合标准 JSON。
5. `images/Sprite/` 是 UnityPy 按原始打包信息解码的 Sprite，`images/Texture2D/` 是依赖的整张纹理。文件名附带 `asset_file + path_id`，不会因原生同名资源互相覆盖。

Sprite 的 `pivot` 是 Unity 原始归一化坐标，`rect` 是 Unity 原图矩形，`texture_rect_offset` 是图集裁切偏移；PNG 已由 UnityPy 按原始 packing 信息解码，不能把矩形偏移再次盲目应用到 PNG。联系表只缩放显示，不修改导出的原图。图片尺寸和图案方向不等于游戏内大小和方向，应结合 PPU、prefab Transform 和攻击挂点检查。

## 覆盖依据

权威入口是 `globalgamemanagers` 内的 `ResourceManager.m_Container`。对这种 loose player assets，`UnityPy.Environment.container` 为空，不能据此判断游戏没有资源表。

选取四个实际注册资源根：

| 资源前缀 | 用途 |
| --- | --- |
| `prefabs/battle/diceattackeffects/` | 贴图攻击和特殊攻击主库 |
| `prefabs/battle/dicedamagedeffects/` | 三类受击效果 |
| `prefabs/battle/creatureeffect/` | 异想体相关补充参考，可能包含非攻击效果 |
| `prefabs/battle/specialeffect/` | 特殊效果补充参考，可能包含非攻击效果 |

不是按图片文件名猜测归属：脚本从这些真实注册根遍历 GameObject 组件、子 Transform、SpriteRenderer、材质贴图、AnimatorController / AnimationClip 引用、粒子贴图和 UV 序列。跳过 Transform 的父级以免逸出 prefab；图的回边仍保留并去重。先遍历依赖再建立每个根的传递闭包索引，共用图片只导出一次。

`Xml/AttackEffectPathInfo` 原样导出，保留 `EffectRes` 到 prefab 的 887 个映射（具体数量以当前库为准）。该关系依据仓库反编译代码 `AttackEffectInfoManager.GetPath()`，不代表角色皮肤全局覆盖关系。某个角色皮肤的动作挂点、某张战斗书页的 `EffectRes`、某个特殊能力主动生成的效果是三个不同入口，必须另行确认调用方。

分组沿用原版 `DiceAttackEffect.Initialize()` 的命名约定：`_H` 对应 `ActionDetail.Hit`，`_J` 对应 `Slash`，`_Z` 对应 `Penetrate`，`_G` 对应 `Guard`。这是行为挂点分类，并不保证图片本身一定是横向或纵向。

## 时间与外观限制

- 原版基类 `DiceAttackEffect.Initialize()` 设置 `animator.speed = 1 / (destroyTime + addedDestroyTime)`；动画的原始采样秒数不能直接当作战斗中秒数。
- 对脚本类恰为 `DiceAttackEffect` 且二进制长度恰为 80 字节的 Unity 2019 原版组件，脚本按仓库已验证字段顺序额外解析 `offset`、`additionalScale`、`spr`、`animator`、`addedDestroyTime`。不同长度或派生脚本不猜测布局。
- 原始 `MonoBehaviour` 自定义类型树大多被游戏构建剥离。其他组件只取得 Unity 通用头部，并写入 `opaque_components`；由隐藏字段引用的图片、运行时拼接 `Resources.Load` 路径生成的效果可能未被本库覆盖。**全部注册攻击根已遍历，不等于证明所有动态攻击资产已穷尽。**
- Mesh、音频、编译后 Shader 等仅列为终止依赖；此工具不导出这些资产。Additive 和 AlphaBlend 材质的实际外观须结合材质元数据与真实游戏检查，PNG 索引不等于最终合成画面。
- PNG 保留原始 alpha，不做背景抠除、重绘、补色或锐化；深色效果在深背景上难看清时，应先换预览底色，不要修改原始参考。
- 文件以只读流加载，纹理按需解码，UnityPy 的 Sprite 图集缓存限制为每个序列化文件最多四项；不一次性读取整个 3.9 GB `resources.assets.resS` 到内存。

本机版本的 Basic、Dawn、Philip EGO、Kurokumo 常规攻击共用 `resources.assets:7048`（`Anim_Default`）。动画长约 0.6667 秒，60 fps，绑定 `SpriteRenderer.m_Color.a`（字段 CRC32 为 304273561）；alpha 在 0 和 0.25 秒为 1，然后平滑退到 0.6667 秒的 0。也就是说，这组原版是完整笔触短暂停留后淡出，不是逐段揭示或逐帧重画。可把它作为颜色/笔触与持续时间基线，同时为新效果明确设计更有方向的出现与回收。

## 新特效的使用边界

新资产应从这里学习大形、亮暗比例、笔触断裂、峰值停留、命中重音和回收方式，而不能复用原图作为 Mod 成品。设计时把贴图与时间脚本一起确定：可以使用不同职责的多张原创图或原创帧序列，不限制在单格图；每张图应说明揭示方向、峰值时刻和尾迹消退方式。原版参考库只提供证据，不替代新效果的实机预览验收。
