"""Focused pure-visual contract guard; Unity readback is the runtime authority."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
source = (root / 'ChristashaParticleVisual.cs').read_text(encoding='utf-8-sig')
for token in ['namespace Steria.Vfx', 'class ChristashaParticleVisual', 'Play(GameObject instance, uint seed, float playbackSpeed = 1f)', 'void Advance(float deltaTime)', 'float Duration', 'void StopAndClear()', '.Simulate(', '.Pause(', 'useAutoRandomSeed = false']:
    assert token in source, token
for token in ['BattleUnit', 'DiceAttackEffect', 'FarAreaEffect', 'Camera.', 'AudioSource', 'ChristashaDawn', 'Renderer.material', 'new Mesh(', 'void Update(', 'AddComponent<', 'Instantiate(']:
    assert token not in source, token
for name in ['Slash','Hit','Pierce','Cast','Area','Point','Projectile']:
    assert '"' + name + '"' in source, name
assert re.search(r'\.Simulate\([^;]*false, false, false\)', source), 'one explicit simulation clock'
print('CHRISTASHA_V2_SOURCE_PASS: pure driver API, seven recipes, manual clock, no old effect or game dependencies')
