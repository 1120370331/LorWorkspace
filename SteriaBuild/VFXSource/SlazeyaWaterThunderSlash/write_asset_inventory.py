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
'| L1 水刃、发散尾流与旋动四方白纹 | L1_WaterBlade.asset | L1_WaterBlade.mat | L1_WaterBody.png / L1_WaterMask.png / L1_TailFanMask.png / L1_WhiteFluidQuadMask.png | L1_WaterBlade.prefab |',
'| L2 流体水滴与断开水丝 | L2_SplashWake.asset | L2_SplashWake.mat | L2_Splash.png / L2_SplashMask.png / L2_WaterNormal.png（Linear） | L2_SplashWake.prefab |',
'| L3 雷电弧光 | L3_Lightning.asset | L3_Lightning.mat | L3_Lightning.png / L3_LightningMask.png | L3_Lightning.prefab |',
'| L4 前端亮芯 | L4_BladeCore.asset | L4_BladeCore.mat | L4_Core.png / L4_CoreMask.png | L4_BladeCore.prefab |',
'| 水纹溶解与蒸发散点 | EvaporationScatter.asset | EvaporationWaterScatter.mat | Evap_WaterScatter.png（Linear；R=主体溶解、G=水滴溶解、A=散点覆盖）；余纹扩散上浮 | WaterEvaporationScatter.prefab |',
'| 命中水花（38粒） | Unity原生Billboard | ImpactWater.mat | L2_Splash.png / L2_SplashMask.png | WaterThunderImpactParticles.prefab 内 Hit_WaterSpray |',
'| 命中紫电（14粒） | ImpactArcShard.asset | ImpactElectric.mat | L3_Lightning.png / L3_LightningMask.png | 同上 Hit_LightningSparks |',
'| 命中压闪（2粒） | Unity原生Billboard | ImpactFlash.mat | Impact_Flash.png | 同上 Hit_ContactFlash |','',
'组合动画预制体：`Prefabs/SlazeyaWaterThunderSlash.prefab`。静态原生资源包有四主层和两个支持Prefab，不含自定义播放脚本；组合与驱动保留在制作工程。','',
'共享自定义Shader：`Shaders/WaterThunderFormedArc.shader`保持辅助层兼容；主刃使用沿pathT运动的Clamp刷形楔形蒙版，Reveal固定为1。`Shared_SweepBrushMask.png`控制扫过亮部和增强；`L1_TailFanMask.png`分离向外发散的水丝，材质TailSpread可调。`Evap_WaterScatter.png`同时驱动水体柔和破散与一张散点残影的扩散。无可见大圆圈。场景另使用Unity内置 `Unlit/Color`。','',
'共享数据贴图：`Shared_FlowRepeat.png`、`Shared_CausticsRepeat.png`、`Shared_ErosionRepeat.png`。旧噪声侵蚀入口保留兼容，本版退场不使用它。','',
'白色四方流纹仅在水体内旋动，WhiteFluidStrength和WhiteFluidSpin可调。L3为一条电主干、5条连接分支及少数二级枝；UV2.x为扫过/着电时间，UV2.y为局部能量储备。Shader做充能、阻尼脉冲和放电，旧区段与低储备小枝先衰减。命中碎电通过局部时钟与Mesh Color.b储备使用同一模型，VoltageDecay可调。尾流水滴80颗，出生点和法向速度收紧以贴近刀痕。','',
'水滴和命中水花启用Liquid材质模式：线性法线、各粒子独立相位的衰减表面波动、局部动态高光与水丝收颈；WaterDropletMotion.cs计算阻力/重力飞行和顺速度拉伸回圆。水滴顶点/粒子Color.r=seed，g=归一化年龄，b=水丝标记，a=透明度；其它层保留原始Color含义。LiquidWobble、LiquidShine可在材质中调整。','',
'溶解淡出：主体按水纹R通道、水滴按较柔和的G通道阈值逐步侵蚀，后段再做短alpha收尾；同一RGBA的A通道显示散点残影。蒸发散点材质UseShapeRGB=0，用BaseColor染青蓝色，避免把RG数据当颜色读取。EvapDissolveSoftness调边缘柔度，LiquidDissolve调水滴溶解开关。','',
'所有纹理/Mask为项目Python原创技术资产；水刃、飞沫、紫电、亮芯和支持几何为项目原创。Shader为本机skill ContractV1兼容派生，来源记录见 `provenance.json`。本轮未使用ImageGen；没有游戏原图或公开仓库图片依赖。','',
'## 按类型逐项列出','']
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
