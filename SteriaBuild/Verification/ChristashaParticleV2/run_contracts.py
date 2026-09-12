"""Source-linked native lifecycle checks. This does not claim Unity GPU execution."""
from pathlib import Path
import hashlib
import json
import subprocess
import re
import argparse

parser = argparse.ArgumentParser()
parser.add_argument('--melee-only', action='store_true', help='Run only attachment, lifetime and SlashH/V posture contracts')
args = parser.parse_args()

here = Path(__file__).resolve().parent
repo = here.parents[2]
build = repo / 'SteriaBuild'
out = repo / ('output/christasha-particle-v2/integration/managed-melee' if args.melee_only else 'output/christasha-particle-v2/integration/managed')
generated = out / 'generated'
generated.mkdir(parents=True, exist_ok=True)
inputs = [build / name for name in ('DiceAttackEffect_Steria_ChristashaParticleV2.cs', 'BehaviourAction_Steria_ChristashaXiyinV2.cs', 'FarAreaEffect_Steria_ChristashaXiyinV2.cs')]
for relative in ('Battle/DiceAttackEffect/DiceAttackEffect.cs', 'FarAreaEffect.cs'):
    source = build / 'Assembly-CSharp' / relative
    inputs.append(source)
    (generated / source.name).write_text('using Object = UnityEngine.Object;\n' + source.read_text(encoding='utf-8-sig'), encoding='utf-8')
source = build / 'Assembly-CSharp/BattleFarAreaPlayManager.cs'
inputs.append(source)
text = source.read_text(encoding='utf-8-sig')
start = text.index('private void ActionPhase(float deltaTime)')
brace = text.index('{', start)
depth, end = 1, brace + 1
while depth:
    depth += (text[end] == '{') - (text[end] == '}')
    end += 1
setter = re.search(r'public void SetActionDelay\([^)]*\)\s*\{[^}]*\}', text).group(0)
reset = re.search(r'this\._actionPreDelay = 1f;\s*this\._actionPostDelay = 0\.5f;', text).group(0)
assert len(re.findall(r'this\._actionPreDelay = 1f;\s*this\._actionPostDelay = 0\.5f;', text)) == 2, 'native Start and End both restore action defaults'
(generated / 'NativeActionPhase.cs').write_text('using System.Collections.Generic;\nusing UnityEngine;\nusing Object = UnityEngine.Object;\npublic partial class BattleFarAreaPlayManager {\n' + text[start:end] + '\n' + setter + '\npublic void NextCardNativeResetFixture() { ' + reset + ' }\n}', encoding='utf-8')
(out / 'candidate.json').write_text(json.dumps({str(path.relative_to(repo)): hashlib.sha256(path.read_bytes()).hexdigest() for path in inputs}, indent=2), encoding='utf-8')
subprocess.run(['dotnet', 'run', '--project', str(here / 'Fixture.csproj'), '--configuration', 'Release', '-p:GeneratedRoot=' + str(generated), '--', str(out)] + (['melee'] if args.melee_only else []), check=True)
