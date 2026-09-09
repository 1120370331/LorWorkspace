# R2 部件设计与生成

## 九动作挂坠逐张绘制

统一 U 形链圈在多种姿势中出现胸前长度、转向与遮挡瑕疵。九个动作都改由 `prepare_revision2.py:draw_pendant` 读取 `pendant_strokes.json` 的逐姿势笔画，直接绘制曲线、连接环、小十字和高光，并在前侧手臂下面烘焙。共用吊坠 PNG 粘贴已移除；本次为直接绘制，不重新生图，头部源图与动作配方没有改变。

- 参考身份：`SteriaBuild/SteriaModFolder/Resource/CharacterSkin/Velia.png`。
- 姿势参考：`SteriaBuild/Templates/CharacterSkin/CharacterSkin_Template_3x3_512_Modicos.png`，学习重心、支撑脚、弯肘与衣料张力，不复制其武器或服装。
- 原理参考：[Spine IK](https://esotericsoftware.com/spine-ik-constraints)、[Mesh deformation](https://esotericsoftware.com/spine-meshes)。未引入 Spine 运行时。
- 本轮采用内置 image_gen，3 组衣装姿势加 1 组头部母版，纯品红底。PNG 源文件和坐标均保存在本目录。
- 衣装组包含身体、袖子和手，先保证解剖与布料连续；组内再拆前侧手臂。头部先在母版固定五官关系，再共画布拆层。
- 依用户后续意见缩短母版可见颈段，固定十字挂坠合入每动作衣装。没有独立挂坠运行时层。

## 头部母版

```text
undefined
```
