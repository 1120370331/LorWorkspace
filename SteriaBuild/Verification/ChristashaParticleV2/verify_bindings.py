"""Reusable Christasha binding guard; optional task-start comparison for delivery acceptance."""
from pathlib import Path
import argparse
import collections
import hashlib
import json
import re
import xml.etree.ElementTree as ET

here = Path(__file__).resolve().parent
repo = here.parents[2]
p = argparse.ArgumentParser()
p.add_argument('--baseline', type=Path, help='Optional strict task-start SteriaBuild snapshot; never use HEAD for this task')
p.add_argument('--concurrent-changes', type=Path, help='Explicit receipt for independently owned exact XML changes in strict mode')
args = p.parse_args()
if args.concurrent_changes and not args.baseline:
    p.error('--concurrent-changes requires --baseline')
build = repo / 'SteriaBuild'
relative = 'SteriaModFolder/Data/CardInfo.xml'
after = ET.parse(build / relative).getroot()
contract = json.loads((here / 'binding-contract.json').read_text(encoding='utf-8'))
counts = collections.Counter()

def canonical(node):
    return [node.tag, dict(node.attrib), (node.text or '').strip(), [canonical(child) for child in node]]

def card_by_id(root, card_id):
    matches = root.findall("Card[@ID='" + card_id + "']")
    assert len(matches) == 1, 'missing or duplicate card ' + card_id
    return matches[0]

# Check exact bindings before removing only these visual fields. Other original
# card data, including Guard, Xiao actions and dice abilities, stays protected.
protected = ET.fromstring(ET.tostring(after))
for entry in contract['bindings']:
    die = card_by_id(protected, entry['card']).findall('./BehaviourList/Behaviour')[entry['behavior']]
    assert die.get('EffectRes') == entry['new'], entry
    del die.attrib['EffectRes']
    counts[entry['old']] += 1
xiyin = card_by_id(protected, '9009006').findall('./BehaviourList/Behaviour')
assert len(xiyin) == 1 and xiyin[0].get('ActionScript') == 'Steria_ChristashaXiyinV2'
del xiyin[0].attrib['ActionScript']
for card_id, expected in contract['protected_cards'].items():
    data = json.dumps(canonical(card_by_id(protected, card_id)), ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode('utf-8')
    assert hashlib.sha256(data).hexdigest() == expected, 'non-visual card fields changed: ' + card_id
assert sorted(counts.values()) == [2, 8, 8, 9, 19] and len(contract['protected_cards']) == 23

registry = (build / 'HarmonyPatches.cs').read_text(encoding='utf-8-sig')
if args.baseline:
    before = ET.parse(args.baseline / relative).getroot()
    for entry in contract['bindings']:
        die = card_by_id(before, entry['card']).findall('./BehaviourList/Behaviour')[entry['behavior']]
        assert die.get('EffectRes') == entry['old'], entry
        die.set('EffectRes', entry['new'])
    die = card_by_id(before, '9009006').findall('./BehaviourList/Behaviour')[0]
    assert die.get('ActionScript') == ''
    die.set('ActionScript', 'Steria_ChristashaXiyinV2')
    if args.concurrent_changes:
        receipt = json.loads(args.concurrent_changes.read_text(encoding='utf-8-sig'))
        for change in receipt['changes']:
            node = card_by_id(before, change['card']).findall('./BehaviourList/Behaviour')[change['behavior']]
            assert node.get(change['attribute']) == change['before'], change
            node.set(change['attribute'], change['after'])
    assert canonical(before) == canonical(after), 'XML changed outside task fields and explicitly receipted concurrent edits'
    old_registry = (args.baseline / 'HarmonyPatches.cs').read_text(encoding='utf-8-sig')
    def other_registrations(text):
        return '\n'.join(line for line in text.splitlines() if not re.search(r'SteriaCustomEffects\["Steria_Christasha(?:Dawn|V2)', line))
    assert other_registrations(old_registry) == other_registrations(registry), 'Harmony changed outside Christasha registry'

adapter = (build / 'DiceAttackEffect_Steria_ChristashaParticleV2.cs').read_text(encoding='utf-8-sig')
for effect in {entry['new'] for entry in contract['bindings']}:
    target = re.search(r'SteriaCustomEffects\["' + effect + r'"\]\s*=\s*typeof\((\w+)\)', registry)
    assert target, f'unregistered {effect}'
    assert re.search(r'class\s+' + target[1] + r'\s*:', adapter), f'undefined {target[1]}'
assert 'Steria_ChristashaDawn' not in registry
assert not any('Steria_ChristashaDawn' in die.get('EffectRes', '') for die in after.findall('.//Behaviour'))
for old in ('DiceAttackEffect_Steria_ChristashaDawnCombat.cs', 'BehaviourAction_ChristashaGoldenMass.cs', 'FarAreaEffect_ChristashaGoldenMass.cs'):
    assert not (build / old).exists(), f'old active runtime remains: {old}'
mass = (build / 'FarAreaEffect_Steria_ChristashaXiyinV2.cs').read_text(encoding='utf-8-sig')
assert not re.search(r'\.GiveDamage\s*\(|\.OnSucceedAttack\s*\(', mass), 'visual owns combat settlement'
assert 'HasIndependentAction' in mass and 'return false;' in mass
assert (build / 'BehaviourAction_Steria_ChristashaXiyinV2.cs').exists()
print('CHRISTASHA_V2_BINDINGS_PASS', dict(counts), '23 protected cards, five mappings, old runtime removed; strict task baseline=' + str(bool(args.baseline)))
