"""Extract unchanged production stamp/Apply/Restore/Flash hooks for the bounded UI adapter."""
from pathlib import Path
import hashlib
import json
repo = Path(__file__).resolve().parents[3]
fixture = Path(__file__).resolve().parent / 'ManagedFixture'
generated = fixture / 'Generated'
generated.mkdir(exist_ok=True)
source = (repo / 'SteriaBuild/FlowHandUI.cs').read_text(encoding='utf-8-sig')
body = source[source.index('    public sealed class FlowRangePresentation'):source.index('    [HarmonyPatch(typeof(BattleDiceCardBuf), "GetBufIcon")]')]
(generated / 'FlowCardHooks.cs').write_text('using System; using System.Collections.Generic; using UnityEngine; using UnityEngine.UI; using TMPro; using HarmonyLib;\nnamespace Steria {\n' + body + '\n}\n')
paths = ['SteriaBuild/FlowCardCurrentBorder.cs', 'SteriaBuild/FlowHandUI.cs',
         'SteriaBuild/FlowPlanning.cs', 'output/ocean-current-vfx/ui/candidate/Steria.dll']
out = repo / 'output/ocean-current-vfx/ui/managed'
out.mkdir(parents=True, exist_ok=True)
(out / 'source-hashes.json').write_text(json.dumps({path: hashlib.sha256((repo / path).read_bytes()).hexdigest() for path in paths}, indent=2))
print('Production border compiled directly; stamp + preview hooks copied verbatim. Managed transforms/UGUI record mesh output; no Unity GPU claim.')
