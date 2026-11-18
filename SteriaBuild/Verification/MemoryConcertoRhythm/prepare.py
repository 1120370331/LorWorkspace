"""Extract exact native queue methods, not a second implementation of them."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'output/memory-concerto-rhythm'
SCRIPTS = ['Steria_MemoryConcerto_Long', 'Steria_MemoryConcerto_Medium', 'Steria_MemoryConcerto_Final']


def block(source, declaration):
    start = source.index(declaration)
    opening = source.index('{', start)
    depth = 1
    end = opening + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


def check_memory_concerto():
    tree = ET.parse(ROOT / 'SteriaBuild/SteriaModFolder/Data/CardInfo.xml')
    dice = tree.find("Card[@ID='9001004']").findall('BehaviourList/Behaviour')
    assert [d.get('ActionScript') for d in dice] == SCRIPTS + [''], 'Exact three ActionScript bindings required; evasion stays empty'
    wanted = [('2', '7', 'Atk', 'Slash', 'J', 'MemorySlash'), ('3', '6', 'Atk', 'Hit', 'H', 'MemoryHit'),
              ('4', '8', 'Atk', 'Penetrate', 'Z', 'MemoryPierce'), ('1', '6', 'Def', 'Evasion', 'E', '')]
    for d, w in zip(dice, wanted):
        assert tuple(d.get(k) for k in ['Min', 'Dice', 'Type', 'Detail', 'Motion']) == w[:5]
        assert d.get('EffectRes') == ('Steria_AnhierTexture' + w[5] if w[5] else '')
        assert d.get('Script') == ''


def prepare(christasha_only=False):
    if not christasha_only:
        check_memory_concerto()
    source = (ROOT / 'SteriaBuild/Assembly-CSharp/RencounterManager.cs').read_text(encoding='utf-8-sig')
    declarations = ['public List<RencounterManager.MovingAction> GetMovingAction(']
    declarations += ['private List<RencounterManager.MovingAction> ' + name + '(' for name in [
        'GetMovingActionByWin', 'GetMovingActionWinFar', 'GetMovingActionWinMelee',
        'GetMovingActionByDraw', 'GetMovingActionDrawFar', 'GetMovingActionDrawMelee',
        'GetMovingActionByLose', 'GetMovingActionLoseFar', 'GetMovingActionLoseMelee']]
    declarations += ['private bool InMeleeRange(', 'public struct ActionAfterBehaviour', 'public class MovingAction']
    extracted = {d: block(source, d) for d in declarations}
    generated = 'using System;\nusing System.Collections.Generic;\nusing LOR_DiceSystem;\nusing UnityEngine;\npublic class RencounterManager {\n' + '\n'.join(extracted.values()) + '\n}\n'
    (OUT / 'generated').mkdir(parents=True, exist_ok=True)
    (OUT / 'generated/NativeRencounter.cs').write_text(generated, encoding='utf8')
    (OUT / 'native-extraction.json').write_text(json.dumps({
        'source': 'SteriaBuild/Assembly-CSharp/RencounterManager.cs',
        'sourceSha256': hashlib.sha256(source.encode()).hexdigest(),
        'extractedSha256': {d: hashlib.sha256(b.encode()).hexdigest() for d, b in extracted.items()},
        'boundary': 'Native queue methods/types exact; Unity positions and battle result containers shimmed. MoveRoutine/game renderer not executed.'
    }, indent=2), encoding='utf8')
    if christasha_only:
        print('SOURCE_PASS: extracted 13 native blocks for Christasha melee')
    else:
        print('SOURCE_PASS: exact three bindings, unchanged four dice contracts; extracted 13 native blocks')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--christasha-only', action='store_true', help='Prepare the native queues for the Christasha-only fixture')
    prepare(parser.parse_args().christasha_only)
