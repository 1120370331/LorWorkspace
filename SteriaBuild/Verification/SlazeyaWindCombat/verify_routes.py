"""Exact task-start delta; no whole-repository baseline rewriting."""
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
START = ROOT / 'output/slazeya-wind-combat/task-start'
PLAN = json.loads((START / 'route-targets.json').read_text(encoding='utf-8'))


def check():
    relative = Path('SteriaBuild/SteriaModFolder/Data/CardInfo.xml')
    before = (START / relative).read_text(encoding='utf-8-sig')
    expected = before
    for card in sorted({row['card'] for row in PLAN}):
        import re
        match = re.search(r'<Card\b[^>]*ID="' + str(card) + r'"[^>]*>.*?</Card>', expected, re.S)
        assert match, card
        segment = match.group()
        for suffix in ('Slash', 'Hit', 'Pierce', 'Guard', 'FarHit'):
            segment = segment.replace('Steria_OceanCurrent' + suffix, 'Steria_SlazeyaWind' + suffix)
        expected = expected[:match.start()] + segment + expected[match.end():]
    actual = (ROOT / relative).read_text(encoding='utf-8-sig')
    assert actual == expected, 'XML differs beyond exact seven-card substitutions'
    old_tree, new_tree = ET.fromstring(before), ET.fromstring(actual)
    changed = []
    for old_card, new_card in zip(old_tree.findall('Card'), new_tree.findall('Card')):
        old_dice, new_dice = old_card.findall('./BehaviourList/Behaviour'), new_card.findall('./BehaviourList/Behaviour')
        for index, (old, new) in enumerate(zip(old_dice, new_dice)):
            if old.attrib.get('EffectRes') != new.attrib.get('EffectRes'):
                changed.append((int(old_card.attrib['ID']), index, old.attrib['EffectRes'], new.attrib['EffectRes']))
    assert changed == [(r['card'], r['die'], r['before'], r['after']) for r in PLAN], changed
    relative = Path('SteriaBuild/CardAbilities.cs')
    before = (START / relative).read_text(encoding='utf-8-sig')
    expected = before.replace('EffectRes = "Steria_OceanCurrentGuard"', 'EffectRes = "Steria_SlazeyaWindGuard"', 1)
    assert (ROOT / relative).read_text(encoding='utf-8-sig') == expected, 'dynamic Guard has unrelated edits'
    relative = Path('SteriaBuild/HarmonyPatches.cs')
    before = (START / relative).read_text(encoding='utf-8-sig')
    added = ''.join('            SteriaCustomEffects["Steria_SlazeyaWind' + s + '"] = typeof(DiceAttackEffect_Steria_SlazeyaWind' + s + ');\n' for s in ('Slash', 'Hit', 'Pierce', 'Guard', 'FarHit'))
    anchor = '            SteriaCustomEffects["Steria_WindSlash"] = typeof(DiceAttackEffect_Steria_WindSlash);\n'
    assert (ROOT / relative).read_text(encoding='utf-8-sig') == before.replace(anchor, anchor + added, 1), 'factory changes exceed five registrations'
    for name in ('Steria.csproj', 'DiceAttackEffect_Steria_WindSlash.cs'):
        assert (ROOT / 'SteriaBuild' / name).read_bytes() == (START / 'SteriaBuild' / name).read_bytes(), name + ' must stay unchanged'
    print('SLAZEYA_WIND_ROUTES_PASS: 15 XML + 1 dynamic Guard; all other shared bytes/fields equivalent')
    for row in PLAN:
        print(str(row['card']) + ':' + str(row['die']) + ' ' + row['before'] + ' -> ' + row['after'])


if __name__ == '__main__':
    check()
