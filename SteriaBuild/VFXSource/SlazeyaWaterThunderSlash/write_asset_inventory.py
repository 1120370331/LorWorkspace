"""Full delivered Unity asset inventory and human-readable role/dependency list."""
from pathlib import Path
from collections import Counter
import csv,hashlib,json,sys,re
from PIL import Image

HERE=Path(__file__).resolve().parent
out=Path(sys.argv[1]).resolve()
root=HERE/'UnityProject'
types={'.png':'Texture / Mask','.asset':'Mesh','.mat':'Material','.prefab':'Prefab','.shader':'Shader','.cs':'C# source','.unity':'Scene'}
items=[]
for p in sorted((root/'Assets/WaterThunder').rglob('*')):
    if not p.is_file() or p.suffix=='.meta':continue
    if '/Backups/' in p.as_posix():continue
    meta=Path(str(p)+'.meta')
    record={'type':types.get(p.suffix,p.suffix),'path':p.relative_to(root).as_posix(),'size_bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'unity_meta':meta.exists(),'dimensions':'','data_space':'','wrap':''}
    if p.suffix=='.png':
        image=Image.open(p);record['dimensions']=f'{image.width}x{image.height} {image.mode}'
        text=meta.read_text(encoding='utf-8-sig') if meta.exists() else ''
        srgb=re.search(r'sRGBTexture:\s*(\d)',text);wrap=re.search(r'wrapU:\s*(\d)',text)
        record['data_space']='sRGB' if srgb and srgb[1]=='1' else 'Linear'
        record['wrap']='Repeat' if wrap and wrap[1]=='0' else 'Clamp'
    assert meta.exists(),f'Missing Unity metadata: {p}'
    items.append(record)
counts=Counter(i['type'] for i in items)
(out/'asset-inventory.json').write_text(json.dumps({'project':str(root),'counts':counts,'assets':items},ensure_ascii=False,indent=2),encoding='utf-8')
with (out/'asset-inventory.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=list(items[0]));writer.writeheader();writer.writerows(items)
lines=['# 水雷纵斩：全部资产清单','',f'工程：`{root}`','',f'本清单含 {len(items)} 项 Unity 资产/源文件，每项均有配套 `.meta`；不含 Library、临时文件和历史场景备份。SHA256、文件尺寸与纹理导入设置见同目录CSV/JSON。','',
'## 角色与引用','',
'| 职责 | Mesh | Material | Texture / Mask | Prefab |','| --- | --- | --- | --- | --- |',
'| L1 水刃、发散尾流与旋动四方白纹 | L1_WaterBlade.asset | L1_WaterBlade.mat | L1_WaterVolume.png / L1_WaterDepthFlow.png / L1_WaterMask.png / L1_TailFanMask.png / L1_UserWaterNormal.png（用户提供原图） / L1_WhiteFluidQuadMask.png | L1_WaterBlade.prefab |',
'| L2 流体水滴与断开水丝 | L2_SplashWake.asset | L2_SplashWake.mat | L2_Splash.png / L2_SplashMask.png / L2_WaterNormal.png（Linear） | L2_SplashWake.prefab |',
'| L3 雷电弧光 | L3_Lightning.asset | L3_Lightning.mat | L3_Lightning.png / L3_LightningMask.png | L3_Lightning.prefab |',
'| L4 前端亮芯 | L4_BladeCore.asset | L4_BladeCore.mat | L4_Core.png / L4_CoreMask.png | L4_BladeCore.prefab |',
'| 水纹溶解与蒸发散点 | EvaporationScatter.asset | EvaporationWaterScatter.mat | Evap_WaterScatter.png（Linear；R=域扭曲多尺度溶解、G=水滴溶解、B=独立微细侵蚀、A=散点覆盖）；余纹扩散上浮 | WaterEvaporationScatter.prefab |',
'| 泡沫点缀（7粒，错峰出现） | Unity原生Billboard | ImpactFoam.mat | Impact_Foam.png | WaterThunderImpactParticles.prefab 内 Hit_FoamAccents |',
'| 单点线型光针（16–22粒） | Unity原生定宽Billboard | ImpactLightCone.mat | Shared_WaterNoiseRepeat.png / Shared_LightFlowRepeat.png / Impact_ConeShadowMask.png | 同上 Hit_RadialNeedles |',
'| 下段/内缘浪沫（28粒错峰） | Unity原生Billboard | WaterEdgeFoam.mat | EdgeFoam_WaveNoise.png | WaterEdgeFoam.prefab |',
'| 局部命中压闪（1粒） | Unity原生Billboard | ImpactFlash.mat | Impact_Flash.png | 同上 Hit_ContactFlash |','',
'组合动画预制体：`Prefabs/SlazeyaWaterThunderSlash.prefab`。静态原生资源包有四主层和两个支持Prefab，不含自定义播放脚本；组合与驱动保留在制作工程。','',
'刀光Shader：`Shaders/WaterThunderFormedArc.shader`保持辅助层兼容；主刃使用沿pathT运动的Clamp刷形楔形蒙版，Reveal固定为1；光锥另用简洁的WaterThunderLightCone.shader，白光加UV蓝暗面，不使用圆柱凹槽和反光贴图。`Shared_SweepBrushMask.png`控制扫过亮部和增强；`L1_TailFanMask.png`分离向外发散的水丝，材质TailSpread可调。`Evap_WaterScatter.png`同时驱动水体柔和破散与一张散点残影的扩散。无可见大圆圈。场景另使用Unity内置 `Unlit/Color`。','',
'共享数据贴图：`Shared_FlowRepeat.png`、`Shared_CausticsRepeat.png`、`Shared_ErosionRepeat.png`。旧噪声侵蚀入口保留兼容，本版退场不使用它。','',
'白色四方流纹仅在水体内旋动，WhiteFluidStrength和WhiteFluidSpin可调。L3为一条电主干、5条连接分支及少数二级枝；UV2.x为扫过/着电时间，UV2.y为局部能量储备。Shader做充能、阻尼脉冲和放电，旧区段与低储备小枝先衰减。旧命中碎电材质保留兼容，本候选命中使用蓝白针束。VoltageDecay控制L3电枝衰减。尾流水滴80颗，出生点和法向速度收紧以贴近刀痕。','',
'尾流水滴启用Liquid材质模式（本候选命中主体另用蓝白针束、泡沫仅为点缀）：线性法线、各粒子独立相位的衰减表面波动、局部动态高光与水丝收颈；WaterDropletMotion.cs计算阻力/重力飞行和顺速度拉伸回圆。水滴顶点/粒子Color.r=seed，g=归一化年龄，b=水丝标记，a=透明度；其它层保留原始Color含义。LiquidWobble、LiquidShine可在材质中调整。','',
'溶解淡出：主体按R通道的大/中尺度场叠加B通道细碎场、水滴按较柔和的G通道阈值逐步侵蚀，后段再做短alpha收尾；同一RGBA的A通道显示散点残影。蒸发散点材质UseShapeRGB=0，用BaseColor染青蓝色，避免把RG数据当颜色读取。EvapDissolveSoftness调边缘柔度，LiquidDissolve调水滴溶解开关。','',
'刀光纹理和Mask由项目重新绘制；L1_UserWaterNormal.png为用户提供原图，其SHA256见provenance.json；其余辅助纹理由项目Python生成。水刃、飞沫、紫电、亮芯和支持几何为项目原创。Shader为本机skill ContractV1兼容派生，来源记录见 `provenance.json`。本轮未使用ImageGen；没有游戏原图或公开仓库图片依赖。','',
'## 本轮预览变化（未部署）\n\n主体水体无静态镂空。L1_WaterVolume.png提供厚度、波峰、暗部与透光色面；L1_WaterDepthFlow.png的RG存流向、B存高度、A存厚度。颜色UV沿切线双相推进，高度引导侧向偏移，并限制在U±0.10、V±0.035；覆盖率仍采原始UV，避免边缘漂移。Shared_LightFlowRepeat.png增强移动亮带与蓝暗部，仅调内部颜色，不改变光针几何。Shared_WaterNoiseRepeat.png通过旋转/拉伸/平移控制流动明暗；水层与亮层共用边界，水层向内留0.028单位余量，材质禁止摆动向外越界。L1_UserWaterNormal.png逐字节使用用户提供的法线图；双相流动在Clamp范围内采样，改变水面法线和高光。L1_RippleNormalRepeat.png保留作兼容输入。五条随机飘带使用独立宽度/端点/摆动相位，Mask仅用于尾迹。主弧统一圆心，刃体加宽约15%，退尾45°，175–340ms完成。亮层尾端只向前裁短，并与水体蒙版同步溶解，最后才淡出。定宽直光针由同一原点四周射出，16–22根、近似正态长度、72%主弧半径范围，白光直线仅由UV亮度衰减呈现前后收尖，通过UV阴影形成暗蓝面，噪点旋转拉伸形成内部流动；先加速，再受空气阻力减速，泡沫作为周围稀疏点缀。另有下段/内缘浪沫尾缀，出生点复用水层同一网格三角形、展开遮罩与边缘切线，贴边后短暂跟随，再受轻微重力下降并消散。旧ImpactLightCone.asset已停用；ImpactLightCone.mat和WaterThunderLightCone.shader保留历史文件名，实际Shader为LightNeedle，只绘制UV光晕直线，不使用锥体几何。旧ImpactWater/ImpactElectric/ImpactNeedle材质、旧针束贴图及ImpactArcShard网格作为历史兼容资源保留，不再被本候选命中预制体引用。\n\n## 按类型逐项列出','']
for kind,count in counts.items():
    lines += [f'### {kind}（{count}）','', '| 文件 | 规格 / 导入 |','| --- | --- |']
    for item in items:
        if item['type']!=kind:continue
        p=root/item['path'];label=item['path'].replace('Assets/WaterThunder/','')
        target=p.as_posix();detail=' · '.join(x for x in [item['dimensions'],item['data_space'],item['wrap']] if x)
        lines.append(f'| [{label}]({target}) | {detail or "Unity原生资产 / 可编辑源码"} |')
    lines.append('')
(out/'asset-inventory.md').write_text('\n'.join(lines),encoding='utf-8')
print('ASSET_INVENTORY_READY',len(items),dict(counts))
