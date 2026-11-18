"""Copy reviewed Kenney CC0 masks unchanged; never regenerate or recolour them."""
import hashlib
import json
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parent
RESEARCH=ROOT.parents[2]/'output/velia-potential-observation/mask-research'
OUT=ROOT/'source_assets/light-masks'
ROLES=['surge-mask','soft-source','small-streak']

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    provenance=json.loads((RESEARCH/'provenance.json').read_text(encoding='utf-8'))
    files={}
    for role in ROLES:
        record=next(x for x in provenance['selected'] if x['role']==role)
        source=RESEARCH/'selected'/(role+'.png');target=OUT/source.name
        assert hashlib.sha256(source.read_bytes()).hexdigest()==record['sha256']
        shutil.copy2(source,target)
        files[target.name]={'sha256':record['sha256'],'source':record['source'],'channel':'R',
            'sRGB':False,'transform':'none; original PNG bytes','size':record['size']}
    shutil.copy2(RESEARCH/'provenance.json',OUT/'source-provenance.json')
    for name in ['particle-pack','light-masks']:
        license_dir=OUT/'licenses'/name;license_dir.mkdir(parents=True,exist_ok=True)
        shutil.copy2(RESEARCH/name/'License.txt',license_dir/'License.txt')
    (OUT/'manifest.json').write_text(json.dumps({'license':'CC0-1.0','files':files,
        'runtime':'Scalar red coverage in linear colour space; ignore PNG alpha; Blend One One; coverage multiplied once into RGB'},indent=2)+'\n',encoding='utf-8')
    print(json.dumps(files,indent=2))

if __name__=='__main__':main()
