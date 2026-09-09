## 横斩空间关系返修

采用内置 image_gen 单独重画 Slash，源图为 `raw/slash_repair.png`。原 `raw/attack.png` 仅继续供 Penetrate / Hit 使用。新图重新标定了颈根、肩肘腕、握刀分层及支撑脚，未通过挪动旧锚点掩盖源画结构错误。

```text
Use case: precise-object-edit / stylized-concept. Production correction of Ailierel's Slash body pose.
Reference 1 is the flawed pose to REPLACE, not anatomical truth. Its near arm crosses the collar like a detached tube and both arms have unclear shoulder origins. Reference 2 is her correct neutral outfit/character identity. Preserve the black cropped zip jacket, black top with modest narrow midriff, black pleated skirt, thigh-high dark stockings, black boots, dark gloves and exactly TWO short silver daggers with cold blue edge.
Create ONE new headless BODY-AND-DAGGERS pose on a square 1024x1024 ordinary RGB image, flat chroma magenta #FF00FF background. Entire body and both blade tips inside frame, at least 45px margin. No head, hair, face, cat ears, effects, labels, grid or shadows. A short neck stump only. Same short 2D battle-sprite proportions as the reference, fine charcoal outlines and restrained cel shading, not 3D.
Pose: a believable LEFTWARD DIAGONAL DAGGER SLASH FOLLOW-THROUGH, distinctly different from the straight stabbing pose. The torso is in mild three-quarter FRONT view facing LEFT; chest and pelvis turn together with a modest twist, not opposite impossible perspectives.
CRITICAL ARM CONSTRUCTION:
- The ATTACKING arm attaches clearly at the torso's screen-LEFT shoulder. Its upper arm slopes down-left to an identifiable elbow OUTSIDE the left ribcage silhouette. The forearm then continues outward/down-left toward the leading fist. The dagger extends along the hand's natural grip farther LEFT, slightly downward. Show a continuous visible shoulder -> upper arm -> elbow -> forearm -> wrist. Keep the chest and collar readable; do NOT create a horizontal sleeve tube across the neck/chest.
- The OTHER arm comes from the DISTINCT screen-RIGHT shoulder. Upper arm slopes down/right, elbow bends naturally backward, fist is held beside/right of the rear hip with the second dagger pointing safely backward-right. That hand/arm sits farther back than the attacking arm. No fused shoulders, no third limb, no blade emerging from a wrist, no reversed hand.
- Hands must visibly wrap the handles, with a coherent thumb position and neutral wrist, each blade continuing its own handle.
Feet: low balanced wide stance, front LEFT knee bent over its planted boot, rear RIGHT leg extended for support and its boot grounded. Hips remain between the support feet. Skirt pleats and jacket folds follow the torso twist. Do not copy the impossible old shoulder geometry. Do not alter outfit identity or add accessories.
```
