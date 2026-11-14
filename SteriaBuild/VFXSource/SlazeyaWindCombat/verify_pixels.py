"""Reject blank or effectively missing effects in the existing native GPU captures."""
from pathlib import Path
import json
import sys
import numpy as np
from PIL import Image

root=Path(sys.argv[1])
minimum={'Slash':800,'Hit':400,'Pierce':450,'Guard':600,'Projectile':700}
results=[]
for folder in sorted(root.iterdir()):
    if not folder.is_dir() or not (folder/'frame_036.png').exists() or folder.name.endswith('-miss'):
        continue
    kind=folder.name.split('-')[0]
    if kind not in minimum:
        continue
    baseline=np.array(Image.open(folder/('frame_048.png' if kind=='Pierce' else 'frame_036.png')).convert('RGB'),dtype=np.int16)
    best=(0,0)
    for frame in range(1,19):
        pixels=np.array(Image.open(folder/f'frame_{frame:03}.png').convert('RGB'),dtype=np.int16)
        changed=np.max(np.abs(pixels-baseline),axis=2)>8
        count=int(changed.sum())
        if count>best[0]:best=(count,frame)
    result={'case':folder.name,'peakChangedPixels':best[0],'peakFrame':best[1],
            'minimum':minimum[kind],'pass':best[0]>=minimum[kind]}
    results.append(result)
assert len(results)==20, f'Expected five kinds x two facings x two backgrounds, got {len(results)}'
(root/'readability.json').write_text(json.dumps({'scope':'native GPU pixel visibility, not subjective art acceptance','cases':results},indent=2)+'\n')
failed=[x for x in results if not x['pass']]
assert not failed, f'Missing or unreadable effect: {failed}'
print('SLAZEYA_NATIVE_VISIBILITY_PASS',len(results),'cases')
