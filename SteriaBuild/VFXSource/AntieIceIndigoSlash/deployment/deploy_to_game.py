"""Deploy only the verified Antie bundle, notice and scoped DLL, with rollback."""
from pathlib import Path
import datetime,hashlib,json,os,shutil,subprocess,sys,tempfile
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[3]
release=ROOT/'output/antie-ice-indigo'/sys.argv[1]
proof=json.loads((release/'release.json').read_text(encoding='utf-8'))
assert proof['status']=='RELEASE_VERIFIED'
payload=release/'payload';game=Path(proof['game_root']).resolve(strict=True)
alias=Path('C:/Program Files (x86)/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder').resolve(strict=True)
repo=(ROOT/'SteriaBuild/SteriaModFolder').resolve(strict=True)
assert game==alias and game!=repo
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest() if p.is_file() else None
def contained(root,rel):
    p=(root/rel).resolve();assert p.is_relative_to(root),p;return p
def atomic(path,data):
    path.parent.mkdir(parents=True,exist_ok=True);fd,tmp=tempfile.mkstemp(prefix='.'+path.name,suffix='.tmp',dir=path.parent)
    try:
        with os.fdopen(fd,'wb') as f:f.write(data);f.flush();os.fsync(f.fileno())
        os.replace(tmp,path)
    finally:
        if os.path.exists(tmp):os.unlink(tmp)
def idle():
    result=subprocess.run(['powershell','-NoProfile','-Command',"Get-Process | Where-Object {$_.ProcessName -match 'LibraryOfRuina|Library Of Ruina'} | Select-Object -ExpandProperty ProcessName"],capture_output=True,text=True,check=True)
    assert not result.stdout.strip(),'Game is running; close it before deployment'
idle()
stamp=datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
run=ROOT/'output/antie-ice-indigo/deployments'/stamp;run.mkdir(parents=True)
receipt={'status':'PREPARING','release':str(release),'art_candidate':proof['art_candidate'],'game_root':str(game),'alias_root':str(alias),'items':[],
         'preserved':proof['preserved'],'native_result':proof['native_result'],'unchanged_payload':[]};written=[]
def save():atomic(run/'receipt.json',(json.dumps(receipt,ensure_ascii=False,indent=2)+'\n').encode())
try:
    order=proof['payload_order'];assert order[-1]=='Assemblies/Steria.dll'
    for rel in order:assert sha(payload/rel)==proof['payload'][rel],rel
    for label,root in [('game',game),('repo',repo)]:
        for rel,value in proof['preserved'][label].items():assert sha(contained(root,rel))==value,(label,rel,'preserved file drifted')
        for rel in order:
            p=contained(root,rel);before=sha(p)
            assert before==proof['before_payload'][label][rel],(label,rel,'unexpected target baseline')
            if before==proof['payload'][rel]:receipt['unchanged_payload'].append({'root':label,'relative':rel,'sha256':before});continue
            backup=run/'backup'/label/rel
            if before is not None:
                backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,backup);assert sha(backup)==before
            receipt['items'].append({'root':label,'relative':rel,'path':str(p),'before':before,'after':proof['payload'][rel],'backup':str(backup) if before else None})
    receipt['status']='BACKED_UP';save();idle()
    for item in receipt['items']:
        if item['relative']=='Assemblies/Steria.dll':idle()
        p=Path(item['path']);assert sha(p)==item['before'],'Target changed since backup: '+str(p)
        data=(payload/item['relative']).read_bytes();assert hashlib.sha256(data).hexdigest()==item['after']
        atomic(p,data);written.append(item);assert sha(p)==item['after'];save()
    for label,root in [('game',game),('repo',repo)]:
        for rel,value in proof['preserved'][label].items():assert sha(contained(root,rel))==value,(label,rel)
    for root in [game,repo,alias]:
        for rel,value in proof['payload'].items():assert sha(contained(root,rel))==value,(root,rel)
    receipt['status']='DEPLOYED_VERIFIED';receipt['finished_utc']=datetime.datetime.now(datetime.timezone.utc).isoformat();save()
except BaseException as exc:
    receipt['status']='FAILED';receipt['error']=str(exc)
    for item in reversed(written):
        p=Path(item['path'])
        if sha(p)!=item['after']:continue
        if item['backup']:atomic(p,Path(item['backup']).read_bytes())
        else:p.unlink()
    save();raise
print('DEPLOYED_VERIFIED',run/'receipt.json')
