# 乐章骰子：干净自绘图标交付

- 最终合同：同一自绘乐章中心，清除旧斩击剪影，边缘衔接自然，保留卡色显示修复。
- 删除旧攻击图像采样、清白和扩散补底。图形完全自绘：圆滑单线六边框、仅全局高度变化的暗蓝灰底、柔白连音符；无旧攻击像素输入、局部补色或光晕。
- 卡面和详情共用完整图标，动作两个中心和伤害三层共用透明音符；恢复逻辑、Guard/Standby、稀有度框、费用、正文和战斗机制保持既有行为。
- 完整绘制源和离线再生成入口：SteriaBuild/VFXSource/MusicDiceUI/。自有PNG位于SteriaBuild/VisualAssets/MusicDice/，嵌入程序集；运行时只解码和缓存。
- 设计/实施/独立视觉复核：music_visual_design、music_visual_impl、music_visual_review。主线程检查源码、裸底框、新旧图、混合骰，执行Unity验收、计时、构建和部署。
- 独立视觉PASS对应绘图源D92B1892FBAC2822ED3AC793851A5832A5950EF3A586A7F4D2D5EA024A32500C；无遮挡底框确认旧Slash残影消失。新方向更平整、线条化。
- 最终运行时源码SHA256：92036E71316BD7C19012999DC911C14BBAE4155291F1BA13E2A9FFF02A4E3176；csproj：83FE3304CB2359B283D9CC60D8CA98E3A12869FE5AE891DBD112E470187EA22C。
- 主线程最终Unity运行（2026-09-07 17:51）44项PASS：真实manifest加载、三图RGBA与已接受图完全一致、裸底色场、连接alpha、统一中心和状态恢复。加载优化复用完全相同图像的视觉PASS。
- 冷进程实测卡图首次18458→10.14ms，中心首次4502→1.42ms，缓存卡图0.021ms；证据为implementation/factory-timing-loaded.log。
- 最终Release构建0错误、7个既有警告；从实际Steria.dll读取三份manifest资源并核对SHA256与源资产一致。无AssetBundle变更。
- 已更新仓库模组包和C、D两处既有游戏目录；三处DLL SHA256均为B45B1DCA9468F933681FC754597CABD6E28DFD01F52F4F99F900CA9E3616A8FC。备份和回执：output/music-dice-ui/delivery-clean-20260907-175406/。
- 复现：output/music-dice-ui/implementation/run_preview.ps1。主线程为旧Unity启动0xc0000005设置临时环境OPENSSL_ia32cap=:~0x20000000，没有修改应用环境设置。
- 证据边界：真实Unity渲染和生产视觉/加载代码；游戏模型、字体布局和完整交互状态机为fixture代理，未声称游戏内手动联调。原有其他工作树改动保留；DLL由完整工作树构建，源提交仅含本任务改动。
