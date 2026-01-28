# 角色皮肤动作模板（3x3 · 512）

描述：
- 画布尺寸 `1536x1536`，白色背景
- 每格 `512x512`，共 3x3
- 同目录提供：`Blank`（纯白）、`Guide`（网格+标签）、`Modicos`（示例拼合）

动作顺序（按行、从左到右）：
- 第一行：Default / Guard / Evade
- 第二行：Damaged / Slash / Penetrate
- 第三行：Hit / Move / S1

切割原则：
- 切割坐标：`x = col*512`，`y = row*512`（`col/row` 从 0 开始）
- 导出文件名与动作名一致：Default、Guard、Evade、Damaged、Slash、Penetrate、Hit、Move、S1
- 建议角色主体保持在格子中心，便于后续 pivot 微调
- 需要透明背景时，可用现有抠白脚本处理

Split script (ASCII):
- Script: `SteriaBuild\\Templates\\split_skin_sheet.py`
- Default row-major order: Default, Guard, Evade, Damaged, Slash, Penetrate, Hit, Move, S1
- Example (resize to 1536x1536 then split with prefix):
  `python split_skin_sheet.py -i sheet.png -o out --prefix Ailierel_ --rows 3 --cols 3 --cell 512 --target-size 1536x1536`
- Custom names:
  `--names Default,Guard,Evade,Damaged,Slash,Penetrate,Hit,Move,S1`

Promopts:
                                                                      
  目标：生成《废墟图书馆》散图皮肤用的九宫格动作版图（ClothCustom）                                                                      
  画布：1536x1536，PNG，纯白背景(#FFFFFF)，无透明通道                                                                                    
  网格：3x3，每格 512x512，按“行优先”顺序摆放                                                                                            
  动作顺序（从左到右、从上到下）：                                                                                                       
  1) Default  2) Guard  3) Evade                                                                                                         
  4) Damaged  5) Slash  6) Penetrate                                                                                                     
  7) Hit      8) Move   9) S1                                                                                                            
                                                                                                                                         
  统一方向：所有动作朝同一方向（默认朝右），视角一致                                                                                     
  比例一致：角色体型比例与九宫格参考模板一致，保持同一地面线与高度占比                                                                   
  拆分友好：单格内容不跨格；四周留白，避免切割时截断                                                                                     
  设计要求：仅参考动作力度与Q版比例，完全重做动作线、武器、特效、服装、发型/饰品等角色特征                                               
  动作要求：                                                                                                                             
  - Default：站姿                                                                                                                        
  - Guard：防御姿态                                                                                                                      
  - Evade：回避姿态（轻盈、重心偏移）                                                                                                    
  - Damaged：受击姿态                                                                                                                    
  - Slash：斩击动作（自定义动作线/刀光）                                                                                                 
  - Penetrate：突刺动作（自定义动作线/特效）                                                                                             
  - Hit：打击动作（自定义动作线/特效）                                                                                                   
  - Move：移动姿态                                                                                                                       
  - S1：专属大招/特殊动作（自定义动作线与特效）                                                                                          
                                                                                                                                         
  负面约束：不要文字/水印/签名/边框/透视不一致/杂色背景                                                                                  
                                                                               