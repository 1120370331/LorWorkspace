# 爆轰专属贴图生成记录

工具：内置 `image_gen`。主线程负责素材；生成日期：2026-09-28。

## 生成与修订

首次主图与压弧的基础提示词见上级目录 `ultimate-design-v1.md` 的两段完整英文提示词。主图首次完全使用其中主图提示词，保存为 `SteamDetonation-draft.png`。首次主图轮廓偏高，随后以该图为唯一编辑输入，使用以下精确提示词，输出 `SteamDetonation-master.png`：

> Edit this game VFX texture. Keep the left-facing broad blunt ivory pressure front, cool grey painterly compressed steam, subtle brass chips and right-pointing narrow rear tail. Keep 1536x1024 RGBA canvas. REQUIRED CHANGES: remove ALL diffuse glow, fog halo and blurred backdrop around the painted steam AND inside its middle opening: those empty areas must have alpha exactly zero. This is a sharply readable game sprite with truly empty negative space, not a luminous illustration. Redraw the form substantially flatter and compact vertically: entire visible sprite fits from approximately x=140 to1390 and y=235 to800 (content width/height about2.2), preserving generous transparent margin. Upper and lower unequal ribbons enclose a broad irregular see-through channel; remove the central fog so the channel is genuinely transparent all the way between ribbons. Short compact dry-brush front, not a starburst of long spikes. Put a broken open notch in the upper-left front so it will not cover a victim's face. Very fine antialiasing on painted edges only, no soft glow, no blurred aura, no backdrop, no shadow, no black matte, no checkerboard, no new detached arcs, no characters or objects or text. Preserve rich cool-grey opaque folds and ivory/brass brush paint themselves, only empty space becomes transparent. This is the cleaned production master, one asset.

压弧采用设计文档的完整压弧提示词，并追加以下约束，输出 `SteamPressureArc-master.png`：

> The material is painterly compressed vapor, NOT solid metal, not a sword slash. Exact alpha cutout, no diffuse glow outside the strokes.

同时将原提示词中 `At least 75 percent of the bounding interior must remain genuinely transparent.` 明确为 `At least 75 percent of the bounding interior must remain genuinely transparent with alpha zero.`。

## 导入与检查

保留全部原始 RGBA，直接复制到生产路径；没有抠亮度、重画、重采样或改色。工具展示中的 RGB 灰色区域可能对应 alpha 为 0，不能据预览背景判断为实心雾；透明与遮挡以 alpha 数据和实际 Unity 渲染为准。

按 alpha≥8 测量，主图可见宽高比为 2.430，压弧为 0.335；压弧包围盒内约 75.78% 像素的 alpha＜8。主线程接受主图相对提示词的宽高偏差，运行时以统一缩放满足世界尺寸，不非等比拉伸。可见像素均未贴画布边；压弧边缘仅有最高 1／255 的舍入残值。实际留白小于提示词的理想比例，最终仍须检查双向渲染和退场。

生成源路径、尺寸、可见包围盒、边缘 alpha 和 SHA256 见 `asset-manifest.json`。旧运行时效果与预览保存在 `Verification/Vfx/ultimate-v1/baseline/`。贴图阶段通过不等同于动态效果已经验收。
