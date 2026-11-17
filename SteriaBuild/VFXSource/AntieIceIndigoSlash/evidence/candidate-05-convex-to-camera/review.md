# 横斩凸刃朝屏幕、凹侧抬起：单人复核

本轮按用户澄清修改前后/凸凹方向，撤销上一版X左右镜像。单人实现后独立作一轮复核，不声明独立人员审查。本轮只交付预览，不部署。

候选：candidate-05-convex-to-camera，build-candidate-07 DLL，native-09原生模拟。

- 切面沿横刃长轴翻转180°，由Pitch 74°改为−106°。凸弧及刃口指向屏幕−Z，内凹侧略向+Y抬起，同时远离屏幕+Z，倾角仍为16°。
- 固定侧视的peak-right-grey.png呈凹侧朝上的浅弧；top-view.png中屏幕位于下方，凸弧朝下、内凹位于上方，验证真实空间前后方向，而不是修改相机或左右翻图。
- 宽度向量、前锋辉光与切面一致。扫光、内部流动、尾部裁短、溶解时钟保持，未修改四张用户原图。
- 游戏父级人物转向会翻Z；仅横斩在初始化和更新时保留屏幕方向，两个角色朝向都检查实际凸弧−Z/−Y以及内凹边缘+Y/+Z。攻击X朝向、锚点、原生倍率独立保留。
- 竖斩77帧与candidate-01一致；双刀77帧与candidate-04-horizontal-mirror一致。资源包冷加载峰值与制作端像素一致，实际构建DLL和AB经游戏工厂验证。
- 正常＋0.5x视频和侧视/俯视方向图同步交付，视频完成解码检查。

证据：convex-screen-direction.png、angle-before-after.png、three-profile-stages.png、../native-09/native-contexts.png和checks.txt。

边界：原生夹具使用真实API和合成角色，未做完整接待实战；游戏安装文件不写入。
