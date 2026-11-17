from pathlib import Path
import hashlib,json
from PIL import Image
HERE=Path(__file__).resolve().parent
r=json.loads((HERE/'provenance.json').read_text(encoding='utf-8'))
assert len(r['inputs'])==4
for item in r['inputs']:
    p=HERE/item['path'];original=HERE/item['source_copy']
    assert hashlib.sha256(p.read_bytes()).hexdigest()==item['sha256']==hashlib.sha256(original.read_bytes()).hexdigest(),p
    im=Image.open(p);assert list(im.size)==item['size'] and im.mode==item['mode']
print('ANTIE_INPUTS_PASS: 4 unchanged user sources, duplicate radial mask deduplicated')
