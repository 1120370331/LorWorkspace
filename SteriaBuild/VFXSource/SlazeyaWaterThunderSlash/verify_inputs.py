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
    if 'Mask' not in p.name and 'Repeat' not in p.name and 'Normal' not in p.name:
        assert a.shape[2]==4 and a[:,:,3].min()<32 and a[:,:,3].max()>128,p
shader=(HERE/'UnityProject/Assets/WaterThunder/Shaders/WaterThunderFormedArc.shader').read_text()
for prop in ['_Reveal','_Retreat','_Phase','_ShapeTex','_MaskTex','_Dissolve','_FlowTex','_SrcBlend','_DstBlend']:
    assert prop in shader,prop
print('INPUT_CHECK_PASS: hashes, role texture alpha, Repeat seams, FormedArc contract')
