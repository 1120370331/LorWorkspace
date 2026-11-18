# 发明状态图标

11张透明源图均由内置 imagegen 生成，每个素材单独调用；完整提示词、生成文件位置和源文件 SHA256 见 `generation.json`。风格沿用旧铜、铁与低高光处理；限定词使用简化的彩色角标。

`progress.png` 对应发明进度。装置主体依枚举顺序为 `mechanism`、`chain`、`defense`、`destruction`、`energy`；限定词依枚举顺序为 `odd`、`weird`、`fragile`、`mysterious`、`super`。角标分别使用双圆、眼睛、裂片、问号和双上箭头。

运行 `python AceTaffy/import_invention_icons.py` 编译为26张128×128 RGBA图标，保存于 `AceTaffyMod/Resource/ArtWork/`；构建脚本再暂存到 BaseMod 实际加载的根 `ArtWork/`。编译仅裁切透明边距、统一尺寸和装配生成的图层，不重画源图；右上角标避开右下层数区域。装配坐标及输出哈希见 `build-manifest.json`。

运行 `./AceTaffy/Verification/Reception/run_fixture.ps1 -IconsOnly`，使用实际 BaseMod 目录加载器、图标前置补丁与原生 `GetBufIcon` 验证26个生产资源键，并输出64／32／24像素渲染。`Verification/Reception/invention-icons.png` 是便于比较的构建预览。
