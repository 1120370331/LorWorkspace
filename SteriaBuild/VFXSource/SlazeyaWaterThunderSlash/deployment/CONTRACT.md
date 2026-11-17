# 水雷近战：当前接入与部署边界

用户授权单人完成、部署，并按实测反馈绑定人物前方，作为斯拉泽雅核心书页基本近战效果。

- 斩、打、突复用同一四层水雷组合；原三个Steria_SlazeyaWind资源键仍指向水雷三类。
- 额外在RencounterManager.PrintEffect按播放快照匹配核心SteriaBuilding:99000002和SteriaBuilding:2，为普通/借用战斗书页选择近战默认资源。直接CreateAttackEffect复用判断。禁止通过可转移被动或孤立数字ID匹配角色。
- 只处理Near的Atk斩/打/突；Far、FarArea、FarAreaEach、守备、Fire/F、显式None均保留。原卡牌和核心XML不写，不新增竞争的CreateBehaviourEffect前缀。
- 根挂攻击者characterRotationCenter，固定局部前方(-1.3,-0.18,0)。素材+X转换成原生-X仅一次，角色朝向自然继承。近战2倍和骰子倍率各一次，艺术归一化0.50。无目标世界锚点，无每帧世界位置纠偏。
- L4为已扫过的固定跨度亮芯，覆盖中段；尾端只向前裁短，禁止动态拉宽/延迟补亮，和水体同步蒙版溶解。
- 主弧统一圆心、末端短收尖；175–340ms由后向前退尾45°。命中仍190ms、单一原点；余下水体继续按水纹溶解。
- 成功爆发仍由原生同帧、同目标、同攻击种类确认。空挥不产生成功爆发，取消/结束清理粒子和实例Mesh；落地检查使用TransformPoint。
- AB仅含原生静态组件，当前包含四主层和蒸发/命中/内缘浪沫三个支持Prefab；第七Prefab由游戏加载器显式绑定。共享Runtime由实际Steria.dll编译；Unity2019.3.15f1 / Built-in / Gamma / D3D11。
- release-07从已安装release-06的匹配DLL/PDB源码重建，只更新游戏挂点Y高度，103份其他源码、13份内嵌资源以及全部AB/贴图/材质保持。工作区其他改动不进入发布包。
- 原生夹具使用实际候选DLL/游戏程序集/AB，走真实PrintEffect、直接工厂、Update和GPU渲染；189项检查通过。完整接待实战未运行。
- 发布前guard旧DLL/AB/说明哈希，备份全部被替换文件，原子写入；D盘真实目录、C盘别名和仓库包核验。XML及旧远程/守备AB保持。
