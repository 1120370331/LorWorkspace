# 自律发明单位源图

25张透明主体图由内置 imagegen 分别生成。`generation.json` 保存每个类型／词条的完整提示词、原始生成路径与文件哈希；`unit_<词条>_<类型>.png` 为工程中的原始副本。

类型索引0..4：机关、连锁、防御、毁灭、能源。词条索引0..4：奇特、诡异、脆弱、神秘、超级。词条分别使用偏心双驱动、独眼感测器、开裂轻骨架、紫色封闭核心、重装强化外壳，保留各类型的主要轮廓。整体采用低高光的旧铜和深铁色。

`unit-manifest.json` 由 `generate_invention_units.py` 输出，包含25组HP/BP、六项抗性、皮肤和书页ID。`import_invention_units.py` 依据可见alpha范围与脚部位置导入512×512皮肤，固定50 PPU及接地线，九动作复用各自的刚体主体图；输出25套皮肤、五张类型书页图和五个词条光环图标。`build-manifest.json` 记录导入位置、尺寸和输出哈希。

构建总览：`AceTaffy/Verification/Reception/invention-unit-sheet.png`。完整机制与验收范围见 `AceTaffy/Verification/Reception/README.md`。
