"""Run the exact staged V3 with native game APIs in an isolated Unity player fixture."""
import argparse
from pathlib import Path
import shutil
import subprocess
from xml.sax.saxutils import escape

from release import GAME, HERE, ORDER, ROOT, WORK, hashfile, read, write

GAME_EXE = GAME.parents[2]
GAME_DATA = GAME.parent.parent
MONO_LIBS = ROOT / 'SteriaBuild/Libs'
FRAMEWORK = GAME_EXE.parents[1] / 'workshop/content/1256670/2603522001/Assemblies/1FrameworkAssemblies'
PLAYER_TEMPLATE = Path('C:/Program Files/Unity/Editor/Data/PlaybackEngines/windowsstandalonesupport/Variations/win64_nondevelopment_mono/WindowsPlayer.exe')


def compile_fixture(folder, file, name, framework, refs):
    folder.mkdir(parents=True)
    shutil.copy2(HERE / file, folder / 'Program.cs')
    references = ''.join(f'<Reference Include="{p.stem}"><HintPath>{escape(str(p))}</HintPath></Reference>' for p in refs)
    output = 'Exe' if framework == 'net8.0' else 'Library'
    project = folder / 'Fixture.csproj'
    project.write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><AssemblyName>{name}</AssemblyName>'
                       f'<TargetFramework>{framework}</TargetFramework><OutputType>{output}</OutputType>'
                       '</PropertyGroup><ItemGroup>' + references +
                       '</ItemGroup></Project>', encoding='utf-8')
    subprocess.run(['dotnet', 'build', str(project), '-c', 'Release', '--nologo', '-v:q'], check=True)
    return folder / 'bin/Release' / framework / (name + '.dll')


def copy_player():
    player = WORK / 'isolated-vfx-player'
    exe = player / 'StarlancePlayer.exe'
    data = player / 'StarlancePlayer_Data'
    if not exe.is_file():
        player.mkdir(parents=True, exist_ok=True)
        data.mkdir()
        for directory in ('MonoBleedingEdge',):
            shutil.copytree(GAME_EXE / directory, player / directory)
        for directory in ('Resources', 'Plugins', 'Managed'):
            shutil.copytree(GAME_DATA / directory, data / directory)
        for file in GAME_DATA.iterdir():
            if file.is_file() and (file.name.startswith(('globalgamemanagers', 'resources.', 'sharedassets0.')) or
                                   file.name in ('boot.config', 'app.info', 'level0')):
                shutil.copy2(file, data / file.name)
        for file in ('UnityPlayer.dll', 'UnityCrashHandler64.exe'):
            shutil.copy2(GAME_EXE / file, player / file)
        # Use the editor's stock Unity test launcher and only the initial scene.
        # The Steam executable and playable scenes are absent from this fixture.
        shutil.copy2(PLAYER_TEMPLATE, exe)
    return exe, data


def run(release, evidence):
    evidence.mkdir(parents=True)
    meta = read(release / 'built-metadata.json')
    candidate = Path(meta['dll'])
    staged = evidence / 'StagedMod'
    (staged / 'Assemblies').mkdir(parents=True)
    shutil.copy2(candidate, staged / ORDER[-1])
    for rel in ORDER[:-1]:
        (staged / rel).parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(release / 'payload' / rel, staged / rel)
    for rel in ('Assemblies/AB/steria_christasha_light_strike',
                'Resource/Effects/ChristashaNative/tide-brush-imagegen-v1.png'):
        (staged / rel).parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(GAME / rel, staged / rel)
    shutil.copy2(GAME / 'Resource/CharacterSkin/Christasha/ClothCustom/Penetrate.png', evidence / 'actor.png')
    shutil.copy2(ROOT / 'AceTaffy/AceTaffyMod/Resource/CharacterSkin/AceTaffy_Invention_0_3/ClothCustom/Damaged.png', evidence / 'target.png')
    exe, data = copy_player()
    original = GAME_DATA / 'Managed/Assembly-CSharp.dll'
    refs = [original, candidate, MONO_LIBS / '0Harmony.dll']
    refs += [GAME_DATA / 'Managed' / ('UnityEngine.' + name + 'Module.dll') for name in ('Core', 'ImageConversion', 'Animation')]
    fixture = compile_fixture(evidence / 'fixture', 'NativeSmoke.cs.txt', 'StarlanceNativeFixture', 'net472', refs)
    patcher = compile_fixture(evidence / 'bootstrap', 'BootstrapFixture.cs.txt', 'StarlanceBootTool', 'net8.0', [MONO_LIBS / 'Mono.Cecil.dll'])
    managed = data / 'Managed'
    shutil.copy2(fixture, managed / fixture.name)
    subprocess.run(['dotnet', str(patcher), str(original), str(fixture), str(managed / original.name)], check=True)
    deps = managed / 'SmokeDependencies'
    deps.mkdir(exist_ok=True)
    for file in ('0Harmony.dll', 'BaseMod.dll', 'Mono.Cecil.dll', 'Mono.Cecil.Rocks.dll', 'Mono.Cecil.Mdb.dll',
                 'Mono.Cecil.Pdb.dll', 'MonoMod.Utils.dll', 'MonoMod.RuntimeDetour.dll', 'MonoMod.Common.dll'):
        shutil.copy2(MONO_LIBS / file, deps / file)
    for file in ('LorIdExtensions.dll', 'NAudio.dll', 'BaseBridge.dll', 'MyJsonTool.dll', 'UnitRenderUtils.dll'):
        shutil.copy2(FRAMEWORK / file, deps / file)
    write(evidence / 'inputs.json', dict(dll_sha256=hashfile(staged / ORDER[-1]),
          atlases={rel: hashfile(staged / rel) for rel in ORDER[:-1]}, unity_player_sha256=hashfile(exe.parent / 'UnityPlayer.dll'),
          fixture_sha256=hashfile(fixture), original_game_assembly_sha256=hashfile(original),
          test_game_assembly_sha256=hashfile(managed / original.name)))
    log = evidence / 'player.log'
    args = [str(exe), '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '720', '-screen-height', '405',
            '-job-worker-count', '4', '-logFile', str(log), '-starlanceOutput', str(evidence)]
    startup = subprocess.STARTUPINFO(dwFlags=subprocess.STARTF_USESHOWWINDOW, wShowWindow=subprocess.SW_HIDE)
    result = subprocess.run(args, cwd=exe.parent, startupinfo=startup, timeout=90)
    if result.returncode or not (evidence / 'checks.txt').is_file():
        if log.is_file():
            print(log.read_text(encoding='utf-8', errors='replace')[-11000:])
        raise RuntimeError(f'Isolated player returned {result.returncode}: {evidence}')
    print((evidence / 'checks.txt').read_text(encoding='utf-8').strip())


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('release')
    parser.add_argument('evidence')
    args = parser.parse_args()
    run(WORK / args.release, WORK / args.evidence)
