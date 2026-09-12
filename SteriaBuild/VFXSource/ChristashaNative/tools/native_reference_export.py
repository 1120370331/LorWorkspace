"""Export seven original LoR prefab references from local game data; never ship the output."""
from pathlib import Path
import copy
import hashlib
import json
import sys
import argparse

HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--game-data', type=Path, help='LibraryOfRuina_Data directory; defaults to the known local D: installation')
parser.add_argument('--output-dir', type=Path, help='Reference-only output directory; defaults to repo/output/christasha-native-redesign/native-reference-export')
parser.add_argument('--python-deps', type=Path, help='Optional directory containing UnityPy 1.25.3; otherwise use installed package or known local dependency directory')
parser.add_argument('--repo-root', type=Path, help='Optional repository root; normally found by walking up to SteriaBuild')
args = parser.parse_args()

REPO = args.repo_root.resolve() if args.repo_root else next((p for p in HERE.parents if (p / 'SteriaBuild').is_dir()), None)
if REPO is None or not (REPO / 'SteriaBuild').is_dir():
    parser.error('Cannot find repository root containing SteriaBuild; supply --repo-root.')
if args.python_deps:
    if not args.python_deps.is_dir():
        parser.error('--python-deps directory does not exist: ' + str(args.python_deps))
    sys.path.insert(0, str(args.python_deps.resolve()))
else:
    try:
        import UnityPy
    except ImportError:
        known_deps = REPO / 'output/music-dice-ui/python-deps'
        if known_deps.is_dir():
            sys.path.insert(0, str(known_deps))
try:
    import UnityPy
except ImportError:
    parser.error('UnityPy 1.25.3 unavailable. Install this tools/requirements.txt into your Python environment or pass --python-deps.')
if UnityPy.__version__ != '1.25.3':
    parser.error('This verified exporter requires UnityPy 1.25.3, found ' + UnityPy.__version__ + '. Use requirements.txt.')

GAME = (args.game_data or Path('D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data')).resolve()
if not (GAME / 'resources.assets').is_file():
    parser.error('resources.assets is missing from ' + str(GAME) + '; supply --game-data pointing at your local LibraryOfRuina_Data.')
OUTPUT_DIR = (args.output_dir or REPO / 'output/christasha-native-redesign/native-reference-export').resolve()
# Original game resources are reference inputs, not repository/source assets.
if (REPO / 'SteriaBuild').resolve() in [OUTPUT_DIR, *OUTPUT_DIR.parents]:
    parser.error('--output-dir must be outside SteriaBuild; exported original game art must not enter production/source assets.')
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
TEMPLATE = HERE / 'unity2019_schema_only.bundle'
if not TEMPLATE.is_file():
    parser.error('Missing bundled schema-only bootstrap: ' + str(TEMPLATE))
NAME = 'native_reference_christasha_sources'
ROOT = 43475
ROOTS = [(43475, 'fx_pc_rolrang_slash_down'), (46130, 'fx_pc_rolrang_dagger'),
         (39230, 'fx_pc_rolrang_greatsword'), (29620, 'fx_pc_rolrang_glovetrail_cross'),
         (30442, 'fx_mon_xiao_areatrail1'), (40270, 'fx_illusioncard_4_n_orchestra_start'),
         (42336, 'fx_pc_rolrang_shadowslsah')]
ASSETS = ['assets/native_reference/' + name + '.prefab' for _, name in ROOTS]
SKIP = {'MonoBehaviour', 'MonoScript'}
files = {}
data = {}
new_ids = {}
excluded = []
inline = []
queue = []

def file(key):
    if key not in files:
        env = UnityPy.load(str(GAME / key))
        files[key] = next(iter(env.files.values()))
    return files[key]

def destination(owner, pointer):
    pid = pointer['m_PathID']
    if not pid:
        return None
    fid = pointer['m_FileID']
    if not fid:
        return owner, pid
    external = file(owner).externals[fid - 1].path.replace('\\', '/')
    if external.startswith('Library/'):
        external = 'Resources/' + external[len('Library/'):]
    return external, pid

def include(key):
    if key is None:
        return None
    source = file(key[0]).objects.get(key[1])
    if source is None:
        raise KeyError('Unresolved native dependency ' + repr(key))
    if source.type.name in SKIP:
        if list(key) not in excluded:
            excluded.append(list(key))
        return None
    if key not in new_ids:
        # PathID 1 is reserved for the AssetBundle's native main object.
        new_ids[key] = key[1] if key[0] == 'resources.assets' and key[1] != 1 else 600000000 + len(new_ids)
        queue.append(key)
    return new_ids[key]

def walk(owner, node):
    if isinstance(node, dict):
        if 'm_FileID' in node and 'm_PathID' in node and len(node) == 2:
            target = include(destination(owner, node))
            return {'m_FileID': 0, 'm_PathID': target or 0}
        result = {}
        for key, value in node.items():
            if key == 'm_Component':
                # Remove script slots rather than preserving missing-script entries.
                value = [v for v in value if include(destination(owner, v['component'])) is not None]
            result[key] = walk(owner, value)
        return result
    if isinstance(node, (list, tuple)):
        return [walk(owner, v) for v in node]
    return node

source = file('resources.assets')
if source.unity_version != '2019.3.15f1':
    parser.error('Expected the verified LoR Unity 2019.3.15f1 data layout; found ' + source.unity_version)
for root_id, name in ROOTS:
    if root_id not in source.objects or source.objects[root_id].read_typetree().get('m_Name', '').lower() != name:
        parser.error('Local game asset revision does not match verified reference ' + name + ' at pathID ' + str(root_id))
    include(('resources.assets', root_id))
index = 0
while index < len(queue):
    key = queue[index]; index += 1
    obj = file(key[0]).objects[key[1]]
    original = obj.read_typetree()
    transformed = walk(key[0], original)
    streaming = transformed.get('m_StreamData')
    if isinstance(streaming, dict) and streaming.get('size', 0):
        path = GAME / streaming['path']
        with path.open('rb') as stream:
            stream.seek(streaming['offset']); block = stream.read(streaming['size'])
        assert len(block) == streaming['size']
        if obj.type.name == 'Texture2D':
            transformed['image data'] = block
        elif obj.type.name == 'Mesh':
            transformed['m_VertexData']['m_DataSize'] = block
        else:
            raise ValueError('Unhandled streaming object ' + str(key) + ': ' + obj.type.name)
        inline.append({'source': list(key), 'path': streaming['path'], 'offset': streaming['offset'], 'size': streaming['size'], 'sha256': hashlib.sha256(block).hexdigest()})
        transformed['m_StreamData'] = {'offset': 0, 'size': 0, 'path': ''}
    data[key] = (obj, original, transformed, hashlib.sha256(obj.get_raw_data()).hexdigest())

template_env = UnityPy.load(str(TEMPLATE))
bundle = next(iter(template_env.files.values()))
template_file = next(v for v in bundle.files.values() if hasattr(v, 'objects'))
assert len(bundle.files) == 1 and len(template_file.objects) == 1, 'Bootstrap must contain only its AssetBundle main object, never art'
template_ab = next(v for v in template_file.objects.values() if v.type.name == 'AssetBundle')
template_types = {typ.class_id: typ for typ in template_file.types}
ab_tree = template_ab.read_typetree()
ab_tree['m_Name'] = ab_tree['m_AssetBundleName'] = NAME
ab_tree['m_PreloadTable'] = [{'m_FileID': 0, 'm_PathID': new_ids[key]} for key in queue]
ab_tree['m_Container'] = [(asset, {'preloadIndex': 0, 'preloadSize': len(queue), 'asset': {'m_FileID': 0, 'm_PathID': root_id}}) for (root_id, _), asset in zip(ROOTS, ASSETS)]
ab_tree['m_MainAsset'] = {'preloadIndex': 0, 'preloadSize': len(queue), 'asset': {'m_FileID': 0, 'm_PathID': ROOT}}
ab_tree['m_Dependencies'] = []
ab_tree['m_SceneHashes'] = []

# Retain the original player serialized-file layout/version. ObjectReader data
# and native class hashes remain original; only dependency IDs, removed script
# slots and inline streamed texture bytes require serialization changes.
selected = {}
types = []
type_indices = {}
receipt = []
for key in queue:
    obj, original, transformed, raw_hash = data[key]
    new_obj = copy.copy(obj)
    original_type = obj.serialized_type
    typ_key = id(original_type)
    if typ_key not in type_indices:
        typ = copy.copy(original_type)
        # Player resources omit type trees. Preserve the explicit native layout
        # metadata of the now-verified Editor loading path.
        # Prefer the same-revision real bundle tree for shared classes; otherwise
        # recover the native schema from UnityPy's versioned class database.
        native_template = template_types.get(typ.class_id)
        if native_template is not None:
            assert native_template.old_type_hash == original_type.old_type_hash
            typ.node = native_template.node
        else:
            typ.node = obj._get_typetree_node()
            for node in typ.node.traverse():
                # Class-database nodes predate the modern blob flag/hash fields.
                # These native (non managed-reference) trees use a zero ref hash.
                if node.m_TypeFlags is None:
                    node.m_TypeFlags = 1 if node.m_Type == 'Array' else 0
                if node.m_RefTypeHash is None:
                    node.m_RefTypeHash = 0
        typ.type_dependencies = []
        type_indices[typ_key] = len(types); types.append(typ)
    new_obj.type_id = type_indices[typ_key]
    new_obj.serialized_type = types[new_obj.type_id]
    new_obj.path_id = new_ids[key]
    new_obj.assets_file = source
    if original != transformed:
        # Use the originating class database while rewriting, before the target
        # file's stripped native type table replaces its original table.
        new_obj.save_typetree(transformed)
    else:
        new_obj.set_raw_data(obj.get_raw_data())
    selected[new_obj.path_id] = new_obj
    receipt.append({'source': list(key), 'targetPathID': new_obj.path_id, 'type': obj.type.name, 'name': original.get('m_Name', ''),
                    'originalRawSHA256': raw_hash, 'importRawSHA256': hashlib.sha256(new_obj.data if new_obj.data is not None else new_obj.get_raw_data()).hexdigest(), 'changedSerialization': original != transformed})

ab = copy.copy(template_ab)
ab.save_typetree(ab_tree)
# Verified by the five-way background Unity probe: the same full transplant
# changes from incompatible to LOAD_OK when this native main object uses ID 1.
ab.path_id = 1
ab.assets_file = source
ab.type_id = len(types)
types.append(ab.serialized_type)
selected[ab.path_id] = ab
source.objects = selected
source.types = types
source._enable_type_tree = True
source.script_types = []
source.externals = []
source.ref_types = []
source.userInformation = 'Reference-only original Library of Ruina Slash_Down. Never ship.'
source.flags = 4
cab = 'CAB-' + hashlib.md5(NAME.encode()).hexdigest()
source.name = cab
source.parent = bundle
bundle.files = {cab: source}
output = OUTPUT_DIR / NAME
output.write_bytes(bundle.save('lz4'))

verify = UnityPy.load(str(output))
vfile = next(v for v in next(iter(verify.files.values())).files.values() if hasattr(v, 'objects'))
assert vfile.objects[ROOT].read_typetree()['m_Name'] == 'FX_PC_RolRang_Slash_Down'
for root_id, name in ROOTS:
    assert vfile.objects[root_id].read_typetree()['m_Name'].lower() == name
assert 4715 in vfile.objects and vfile.objects[4715].type.name == 'Mesh'
assert not any(o.type.name in SKIP for o in vfile.objects.values())
for row in receipt:
    assert row['importRawSHA256'] == hashlib.sha256(vfile.objects[row['targetPathID']].get_raw_data()).hexdigest()
for obj in vfile.objects.values():
    tree = obj.read_typetree()
    def check(node):
        if isinstance(node, dict):
            if 'm_FileID' in node and 'm_PathID' in node and len(node) == 2:
                assert node['m_FileID'] == 0
                assert node['m_PathID'] == 0 or node['m_PathID'] in vfile.objects, node
            for value in node.values(): check(value)
        elif isinstance(node, (list, tuple)):
            for value in node: check(value)
    check(tree)
    if obj.type.name in ('Mesh', 'Texture2D'):
        assert not tree.get('m_StreamData', {}).get('size', 0)

verified_gpu_sha = 'd1d0f9117093ad1d9646db1cc261c88bd990f612fbdc9c41a9b64bcd8d4274ef'
report = {'status': 'SERIALIZED_READBACK_PASS', 'unityVersion': source.unity_version, 'bundle': str(output), 'assets': ASSETS,
          'roots': [{'sourcePathID':root_id,'asset':asset,'name':name} for (root_id,name),asset in zip(ROOTS,ASSETS)],
          'compatibilityEvidence': 'The prior five-way background Unity 2019.3.15f1 probe isolated the failure to AssetBundle object pathID != 1. The same seven-root bundle was subsequently GPU-verified by the main thread.',
          'typeTreeEnabled': source._enable_type_tree, 'typeTreeRecovery': 'Exact-hash same-revision bundle types where available; versioned Unity native class database for Animator/SpriteRenderer/AnimatorController/AnimationClip. Original native type hashes retained.',
          'bundleBytes': output.stat().st_size, 'bundleSHA256': hashlib.sha256(output.read_bytes()).hexdigest(),
          'originalShaderPolicy': 'Original native compiled Shader objects copied, no approximation or Shader.Find substitution.',
          'previouslyGpuVerifiedBundleSHA256': verified_gpu_sha,
          'matchesPreviouslyGpuVerifiedBundle': hashlib.sha256(output.read_bytes()).hexdigest() == verified_gpu_sha,
          'schemaBootstrapSHA256': hashlib.sha256(TEMPLATE.read_bytes()).hexdigest(),
          'excludedMono': excluded, 'inlineStreaming': inline, 'objects': receipt}
(OUTPUT_DIR / 'sources-import-receipt.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k:report[k] for k in ('status','bundle','assets','bundleBytes','bundleSHA256')},indent=2))
print('Types:', {t:sum(r['type']==t for r in receipt) for t in sorted({r['type'] for r in receipt})})
