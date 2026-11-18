# 潜力观测 S2 来源

Generator: built-in `image_gen`; transparent RGBA output. Normal builds use saved PNGs through `prepare_observation.py`, not the remote generator. The original registered head and all old poses remain unchanged. `mouth_smile.png` uses the same 512×610 head canvas and shared head transform; `anchors.json` records output-pixel anatomical right palm.

The first arm was rejected by the user as too long/thin. `body_overhead_rejected_long.png` is retained only as history; it is not a build input. R2 was generated with the full character visible to establish a compact shoulder-up/bent-elbow silhouette, then only the head/hair were removed for the established layered model. `pose_context_r2_raw.png` is that full context reference; the final body input is `body_overhead_raw.png` (SHA256 `56d7fcd682954230182326c1644d1a9b11ab14881db50d105430c2497ad22ea5`). The independent pose review accepts `S2-proportion-r2.png`, matching rebuilt `build/assembled/S2.png` SHA256 `9d4afe5f902513840fd5ddadaee0fc8603ba3c99d6c26fcfd6e91014ad0b54cd1`.

## Final R2 context prompt

References: prior assembled S2 (rejected arm, same head/body), `build/assembled/Guard.png` (hand scale/style).

> Edit first image: fix her raised RIGHT ARM proportions. Keep this exact character, large head size, hair shape, face smiling with open mouth, narrow white dress, pendant, bare feet, body scale and position. Current viewer-left raised arm is too long and thin, like an antenna. Redraw a COMPACT overhead casting gesture: lift her right shoulder beside her cheek, bend right elbow outward to viewer left at about cheek/ear height, then forearm nearly vertical upward. Palm center should be ONLY SLIGHTLY ABOVE her head crown, not far above it. Fingers reach a little higher. Give the raised forearm clear tapered width and an elbow, not a narrow shaft, and an open hand with natural size matching the second image's Guard hand. Shorten shoulder-to-elbow-to-wrist chain by about25%; shoulder lift and elbow bend let her hand still reach overhead. Draping sleeve should be visible to the LEFT of her hair at cheek/neck level, clearly joining the raised shoulder and elbow, not entirely hidden behind hair. Five relaxed fingers, anatomical right hand. Keep BOTH arms visibly correctly connected. No effects, no labels, no extra views. Full single character sprite transparent RGBA, enough margin, same front-three-quarter facing as first.

## Final head-removal prompt

Reference: `pose_context_r2_raw.png`.

> Erase the character's head and all hair to transparency. Change absolutely nothing else. Preserve exact original canvas, pixel positions, scale, raised right hand, bent arm, robe, feet and margins. Do not redraw or resize body. This is only a head/hair removal edit, with invisible regions under hair reconstructed minimally as white robe shoulder. The body must remain identical in size and location to reference. No new content.

## Smile prompt

No image reference. Saved as `smile_raw.png`; alpha bounds are downsampled and placed on the common head canvas during source preparation.

> Create ONE tiny anime game facial feature sprite: a gently open happy smiling mouth, viewed almost front-on with slight three-quarter tilt to the viewer's left. Simple thin charcoal outline, corners lifted, open dark warm brown mouth cavity, a tiny soft dusty-pink tongue inside lower part, subtle ivory upper teeth line. Width about twice its height. Pleasant relaxed open-mouth smile, not a scream or laughing wide mouth. Fine hand-drawn illustration style, very few details, readable when downsized to 9x5 pixels. Only mouth feature, NO face, NO skin, NO lips shading outside outline, NO head, NO hair, NO eyes, NO text, NO background. Transparent RGBA background, one isolated centered mouth with ample transparent space.
