from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image
HERE=Path(__file__).resolve().parent
record=json.loads((HERE/'provenance.json').read_text(encoding='utf-8'))
for item in record['inputs']:
    p=HERE/item['path']
    assert hashlib.sha256(p.read_bytes()).hexdigest()==item['sha256'],p
    if p.suffix!='.png':continue
    a=np.array(Image.open(p))
    assert a.max()>a.min(),p
    if 'Repeat' in p.name:
        assert np.abs(a[0].astype(int)-a[-1].astype(int)).max()<=1,p
        assert np.abs(a[:,0].astype(int)-a[:,-1].astype(int)).max()<=1,p
    if p.name in ['L1_WaterBody.png','L1_WaterVolume.png']:
        assert a.shape[2]==4 and a[:,:,3].min()>0,'Water density stays continuous; silhouette uses the separate mask'
    elif 'Mask' not in p.name and 'Repeat' not in p.name and 'Normal' not in p.name and 'Depth' not in p.name:
        assert a.shape[2]==4 and a[:,:,3].min()<32 and a[:,:,3].max()>128,p
shader=(HERE/'UnityProject/Assets/WaterThunder/Shaders/WaterThunderFormedArc.shader').read_text()
for prop in ['_Reveal','_Retreat','_Phase','_ShapeTex','_MaskTex','_Dissolve','_FlowTex','_SrcBlend','_DstBlend']:
    assert prop in shader,prop
print('INPUT_CHECK_PASS: hashes, role texture alpha, Repeat seams, FormedArc contract')

water_mask=np.array(Image.open(HERE/'UnityProject/Assets/WaterThunder/Textures/L1_WaterMask.png'))
h,w=water_mask.shape[:2]
assert water_mask[int(h*.15):int(h*.85),int(w*.03):int(w*.97),:3].min()>=250,'Main water body must have continuous coverage without authored holes'
