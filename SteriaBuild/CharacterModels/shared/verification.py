"""Verify exported model contracts and source-layer registration."""
import hashlib
import json
import xml.etree.ElementTree as ET

from PIL import Image, ImageChops


def require(condition,message):
    if not condition:raise ValueError(message)


def verify(project):
    root=project.root;model=project.load_model();out=root/'build'
    manifest=json.loads((out/'build_manifest.json').read_text(encoding='utf-8'))
    for path,digest in manifest['sources'].items():
        require(hashlib.sha256((root/path).read_bytes()).hexdigest()==digest,'Stale build: '+path)
    projection=model.get('projection_skin',model['id']+'_Projection')
    xml=ET.parse(out/projection/'ModInfo.Xml').getroot()
    actions=list(model['poses'])+list(model['aliases'])
    source=json.loads((root/'source_layout.json').read_text(encoding='utf-8'))
    for action in actions:
        body=project.render(model,action,domain='wearing')
        other=project.render(model,action,preset='librarian',domain='wearing')
        require(body.tobytes()==other.tobytes(),'Head changes the clothing: '+action)
        expected=project.render(model,action)
        require(expected.tobytes()!=project.render(model,action,preset='librarian').tobytes(),'Head swap has no effect')
        for directory,suffix,wanted in [('assembled','',expected),(projection+'/ClothCustom','',project.render(model,action,domain='wearing',plane='main')),
               (projection+'/ClothCustom','_front',project.render(model,action,domain='wearing',plane='front'))]:
            with Image.open(out/directory/(action+suffix+'.png')) as im:
                require(im.mode=='RGBA' and im.size==(512,512),'Invalid PNG: '+action)
                require(im.tobytes()==wanted.tobytes(),'Export differs from renderer: '+action)
                box=im.getchannel('A').point(lambda a:255 if a>32 else 0).getbbox()
                require(box is None or (box[0]>0 and box[1]>0 and box[2]<512 and box[3]<512),'Clipped output: '+action)
        data=project.ground_contact(model,action)
        node=xml.find('ClothInfo/'+action)
        require(node.find('Head').get('head_enable')=='True','Projection suppresses native head: '+action)
        y=512-(float(node.find('Pivot').get('pivot_y'))+512)/2
        require(abs((y-data['contact_canvas_y'])/50-.06)<1e-6,'Wrong native sole baseline: '+action)
    # Source parts must reconstruct the connected body/weapon drawing exactly.
    for action in model['poses']:
        master=Image.open(root/f'sources/body/{action}_master.png').convert('RGBA')
        reconstructed=Image.new('RGBA',master.size)
        for suffix in ['body','front','weapon']:
            reconstructed.alpha_composite(Image.open(root/f'sources/body/{action}_{suffix}.png').convert('RGBA'))
        require(reconstructed.tobytes()==master.tobytes(),'Source split changes artwork: '+action)
    h=source['head'];master=Image.open(root/'sources/head/master_normal.png').convert('RGBA')
    composed=Image.new('RGBA',master.size)
    for part in sorted([p for p in model['parts'] if p['domain']=='head'],key=lambda p:p['z']):
        require(Image.open(root/part['path']).size==master.size,'Head layer canvas mismatch')
        composed.alpha_composite(Image.open(root/part['path']).convert('RGBA'))
    top=h['skin_cut_y']-h['crop'][1]
    require(composed.crop((0,0,master.width,top)).tobytes()==master.crop((0,0,master.width,top)).tobytes(),
            'Head split changed the registered face above the neck cut')
    require(model['aliases']['S1']=='Special','Special/S1 mapping missing')
    require(not model['wearing']['accessories'],'Decorative details became independent accessories')
    report={'result':'PASS','candidate':manifest['candidate'],'actions_checked':len(actions),
        'head_and_wearing_isolation':'PASS','head_master_registration':'PASS','source_split_reconstruction':'PASS',
        'native_sole_baseline':'PASS','png_alpha_and_bounds':'PASS','Special_and_S1_mapping':'PASS',
        'game_runtime':'NOT_RUN'}
    (out/'verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2))
    return report
