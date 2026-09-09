# 生图与导入记录

使用内置 image_gen。当前版本美术按参考重新制作，原版部件仅用于换头对照。

衣装/发型组源图为 `generated_parts_rgba.png`，保存已有真实 alpha；面部组最终源图为 `face_kit_magenta.png`，通过 `prepare_face_sources.py` 去除纯品红底，再拆成脸型和三组五官。未使用整个人物的抠白或 rembg。

本轮包含透明背景试错。后续生成默认直接选用不与部件重色的纯色底，不依赖接口产出 RGBA；参考图、提示词、色键参数和裁切坐标一起保留。原版 Sprite 在 `provenance.json` 中记录来源，面部组另见 `face_provenance.json`。

## 衣装与发型部件组

```text
Use case: stylized-concept.
Asset type: a SINGLE transparent modular cut-paper character parts atlas for a Library of Ruina 2D combat sprite, NOT a finished full-body character or action sheet.
Input image 1: current Velia Default sprite, identity/style reference only. Input image 2: older Velia sheet, reference only for white nightgown, long pale white hair, small silver cross necklace and pale hands/feet. Do not reproduce its blue effects.
Create exactly SIX clearly SEPARATED components, centered in six equal cells of an invisible 3 columns by 2 rows grid. Landscape 1536x1024 canvas, each cell 512x512, generous transparent gutters. No component may cross its own cell. Every pixel outside parts truly alpha-transparent, no white/green background and NO painted checkerboard. No text, gridlines, shadows or labels.
Row 1 left: only the HEADLESS and ARMLESS white ankle-length robe torso, rounded shoulders with sleeve attachment stubs, round empty neckline at top, gently tapering upper torso, soft draped widening hem, two tiny bare pale feet visible beneath hem. No hair, no face, no arms, no necklace. Three-quarter front view slightly toward viewer's LEFT. Robe has convincing gray-blue folds with white highlights, simpler ink detail, matching original miniature character proportions. This torso is for jointed sprite assembly.
Row 1 middle: separate near ARM ONLY, a long white loose bell sleeve with a small relaxed pale hand protruding. Shoulder end at top, hand at bottom, straight down orientation, generous rounded overlap at shoulder for rotating it in a sprite rig. No torso.
Row 1 right: separate far ARM ONLY, a matching long white loose bell sleeve with a small open casting pale hand protruding. Shoulder end at top, hand at bottom, straight down orientation, rounded overlap at shoulder. No torso.
Row 2 left: BACK HAIR layer only, long flowing pale silver-white hair silhouette from crown to waist, behind-the-head hair mass with long softly waving tapered tresses. Front-view/slight LEFT three-quarter. NO face/head/skin/neck/body, center may be solid hair as a background layer. No eyes.
Row 2 middle: FRONT HAIR layer only, crown/fringe with long jagged sweeping bangs and thin face-framing locks, exact same pale silver-white hair as back layer. The face opening underneath bangs is fully transparent, no head or face or eyes. Compact top-hair silhouette, no shoulder-length solid lower hair mass.
Row 2 right: separate NECKLACE ONLY, dark fine cord forming a narrow U and small simple silver cross pendant, front view, no neck/body.
Style: current character's restrained pale palette, authentic hand-painted 2D cel/shaded game sprite, gray-blue shadow shapes, thin dark gray irregular contour lines, no 3D plastic. Preserve the modest simple white robe and long pale-haired caster identity. All components must be usable individually with alpha intact. Do not assemble the person. No magic circles, aura, weapons, new decorations, motion trails, display board or scenery.
```

## 脸型与表情组

```text
Use case: stylized-concept. Asset: a modular FACE KIT for the existing Velia 2D game character, on a genuine transparent alpha PNG. Art quality and coherent expression are the priority.
Reference 1 is current Velia: preserve her gentle, timid, slightly weary expression, pale mint-gray eyes, soft childlike cheeks and pale cool skin. Reference 2 is her new modular white hair/robe atlas: match its fine irregular charcoal outlines, muted cool cel-painted shadows and delicacy. Do not use generic stock faces. DO NOT include hair, clothes, necklace, body, scenery, or text.
Output ONE square 1024x1024 sheet with FOUR separated equal 512x512 cells, no visible grid. All four cells share exactly the same imaginary head placement and scale so they can be cropped and overlaid:
Top-left cell: ONLY the blank facemodel / skin silhouette of a chibi young woman in slight three-quarter front view facing LEFT. Large smooth oval cranium, soft small jaw and chin, small ears, pale cool ivory skin with subtle cel shading at temples and under chin. NO eyes, NO eyebrows, NO nose, NO mouth, NO hair. In this cell the head is roughly x=110..410, y=105..435; chin at (245,435).
Top-right cell: ONLY normal facial features as an isolated transparent overlay: two softly open pale gray-mint eyes with visible pupils and a tiny moist glint, delicate neutral/sad eyebrows, a minimal tiny nose tick and small closed neutral mouth. Gaze LEFT. Eyes centered roughly (195,330) and (305,325), each about 68x42. Brows at y=291. Tiny nose near (237,365); mouth near (244,392). No skin or face contour. Every space between eyes/mouth/brows transparent.
Bottom-left cell: ONLY attack/focused facial features at EXACT SAME positions and scale as top-right, modestly resolute brows and focused mint eyes, mouth slightly parted. No angry grin. No skin, hair or outline.
Bottom-right cell: ONLY damaged facial features at SAME positions and scale: eyes squeezed shut, worried brows, small pained open mouth. Keep elegant readable lines, no tears or effects. No skin, hair or outline.
Keep all feature groups geometrically consistent between the three overlay cells. No drop shadows, no glows, no checkerboard drawing. Actual transparent background with clean alpha; all pale skin and eye whites inside art remain opaque. The 4 cells are parts to assemble, NOT four finished heads.
```

## 面部组改为纯品红底

```text
Use case: precise-object-edit. Edit only the background of this four-cell FACE KIT. Replace ALL gray checkerboard with a perfectly flat pure vivid chroma magenta RGB(255,0,255), #FF00FF. This is a color-key production sprite sheet. Keep the blank facemodel and all the isolated normal / attack / damaged eye, brow, nose and mouth artwork, their exact size, positions and colors unchanged. Fill the entire empty background including ALL gaps between individual facial features with #FF00FF. NO gradients, NO checkerboard, NO shadows, NO glow, NO white scribbles, NO reflected magenta or magenta tint on the art. Preserve the pale skin, gray-mint irises, white eyeballs, charcoal ink lines and muted rose mouth. Output an ordinary RGB PNG with this FLAT MAGENTA background; do not try to create transparency.
```


