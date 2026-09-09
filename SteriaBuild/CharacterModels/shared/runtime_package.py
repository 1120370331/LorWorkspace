"""Make reviewable guest/projection skin packages without writing into the game."""
import copy
import hashlib
import json
import shutil
import xml.etree.ElementTree as ET

from PIL import Image


def package(project):
    root=project.root;model=project.load_model();build=root/'build'
    manifest=json.loads((build/'build_manifest.json').read_text(encoding='utf-8'))
    verified=json.loads((build/'verification.json').read_text(encoding='utf-8'))
    if verified['result']!='PASS' or verified['candidate']!=manifest['candidate']:raise ValueError('Verify this exact build first.')
    for path,digest in manifest['sources'].items():
        if hashlib.sha256((root/path).read_bytes()).hexdigest()!=digest:raise ValueError('Stale candidate: '+path)
    actions=list(model['poses'])+list(model['aliases']);name=model['character'];projection=model['id']+'_Projection'
    out=build/'runtime';guest=out/'Resource/CharacterSkin'/name;wardrobe=out/'Resource/CharacterSkin'/projection
    for p in [guest/'ClothCustom',wardrobe/'ClothCustom']:p.mkdir(parents=True,exist_ok=True)
    for action in actions:
        shutil.copy2(build/'assembled'/(action+'.png'),guest/'ClothCustom'/(action+'.png'))
        Image.new('RGBA',(512,512)).save(guest/'ClothCustom'/(action+'_front.png'))
        for suffix in ['','_front']:shutil.copy2(build/projection/'ClothCustom'/(action+suffix+'.png'),wardrobe/'ClothCustom'/(action+suffix+'.png'))
    xml=ET.parse(build/projection/'ModInfo.Xml').getroot();g=copy.deepcopy(xml);g.find('ClothInfo/Name').text=name
    for action in actions:g.find('ClothInfo/'+action+'/Head').attrib.update(head_x='0',head_y='0',rotation='0',head_enable='False')
    for p,tree in [(guest,g),(wardrobe,xml)]:
        ET.indent(tree);ET.ElementTree(tree).write(p/'ModInfo.Xml',encoding='utf-8',xml_declaration=True)
        shutil.copy2(build/'assembled/Default.png',p/'Thumb.png')
    bindings={}
    mod=root.parents[1]/'SteriaModFolder'
    for filename in ['EquipPage_Enemy.xml','EquipPage_Librarian.xml']:
        books=ET.parse(mod/'Data'/filename).getroot()
        accepted_skins={name} if 'Enemy' in filename else {name,projection}
        bindings[filename]={b.get('ID'):(name if 'Enemy' in filename else projection) for b in books if b.findtext('CharacterSkin') in accepted_skins}
    result={'status':'packaged_not_deployed','art_candidate':manifest['candidate'],'skin_bindings':bindings,
        'files':{str(p.relative_to(out)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in (out/'Resource').rglob('*') if p.is_file()}}
    (out/'runtime_manifest.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(f"Packaged {name}: {len(result['files'])} files; no game files changed.")
