"""Read-only story image and scene-state probe; no game process is started."""

from __future__ import annotations

import hashlib
import argparse
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
SOURCE_PACKAGE = ROOT / "AceTaffyMod"
PACKAGE = SOURCE_PACKAGE
OUT = Path(__file__).resolve().parent
GENERATED_SHOTS = {"WorkshopInvitation"}
NATIVE_BACKGROUND = "ch1_library_inside"
TAFFY_PORTRAITS = {"Taffy_Default", "Taffy_Happy", "Taffy_Confused"}
NATIVE_FACES = {
    "앤젤라": {"normal", "smile", "sigh"},
    "롤랑": {"nomal", "worry", "sigh", "happy"},
}


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def native_assets(assets_path: Path) -> tuple[Image.Image, dict[tuple[str, str], Image.Image]]:
    import UnityPy

    environment = UnityPy.load(str(assets_path))
    results = {}
    background = None
    for item in environment.objects:
        if item.type.name == "Sprite" and item.path_id == 22179:
            sprite = item.read()
            assert sprite.m_Name == NATIVE_BACKGROUND
            background = sprite.image.convert("RGB")
            assert background.size == (1920, 1080)
            break
    assert background is not None, f"Original background absent: {NATIVE_BACKGROUND}"
    names = {"앤젤라": ("normal", "sigh"), "롤랑": ("nomal", "worry")}
    for name, faces in names.items():
        matching = []
        for item in environment.objects:
            if item.type.name != "GameObject":
                continue
            game_object = item.read()
            if game_object.m_Name != name:
                continue
            transform = game_object.m_Component[0].component.read()
            children = {child.read().m_GameObject.read().m_Name: child.read() for child in transform.m_Children}
            if "body_nomal" in children and all("face_" + face in children for face in faces):
                matching.append((item.path_id, children))
        assert matching, f"Original story portrait prefab absent: {name}"
        _, children = matching[0]
        for face in faces:
            canvas = Image.new("RGBA", (500, 900), (0, 0, 0, 0))
            for part in ("body_nomal", "face_" + face):
                transform = children[part]
                game_object = transform.m_GameObject.read()
                renderer = next(component.component.read() for component in game_object.m_Component if component.component.read().__class__.__name__ == "SpriteRenderer")
                sprite = renderer.m_Sprite.read().image.convert("RGBA")
                x = round(250 + transform.m_LocalPosition.x * 100 - sprite.width / 2)
                y = round(900 - transform.m_LocalPosition.y * 100 - sprite.height / 2)
                canvas.alpha_composite(sprite, (x, y))
            results[(name, face)] = canvas
    return background, results


def story_states(name: str) -> tuple[list[tuple[int, str, str]], list[tuple[int, str]]]:
    text = ET.parse(PACKAGE / "Data" / "StoryText" / f"AceTaffy_{name}.xml").getroot()
    effects = ET.parse(PACKAGE / "Data" / "StoryEffect" / f"AceTaffy_{name}.xml").getroot()
    dialogs = text.findall("./Group/Episode/Place/Dialog")
    effect_rows = effects.findall("Dialogue")
    assert effects.findtext("storyBranch") == "None"
    first = effects.find("CG")
    assert first is not None
    assert first.get("Src") == ("AceTaffy_WorkshopInvitation" if name == "Start" else NATIVE_BACKGROUND)
    assert [int(node.get("ID")) for node in dialogs] == list(range(len(dialogs)))
    assert all(node.find("Filter") is not None and node.find("Bgm") is not None for node in effect_rows)
    assert all(node.find("Bgm").get("Src") == "None" and node.find("Bgm").get("PrevBgm") == "Continue" for node in effect_rows)
    effects_by_id = {int(node.get("ID")): node for node in effect_rows}
    assert len(effects_by_id) == len(effect_rows)
    changes_by_id = {number: node.find("Bg").get("Src") for number, node in effects_by_id.items() if node.find("Bg") is not None}
    assert 0 in changes_by_id
    expected_changes = {"Start": {0: "AceTaffy_WorkshopInvitation", 6: NATIVE_BACKGROUND}, "End": {0: NATIVE_BACKGROUND}}
    assert changes_by_id == expected_changes[name], (name, changes_by_id)
    assert first.get("Src") == changes_by_id[0]
    state = None
    visible = []
    active_characters = set()
    for node in dialogs:
        number = int(node.get("ID"))
        effect = effects_by_id.get(number)
        if effect is not None:
            for character in effect.findall("./CharacterList/Character"):
                key = character.get("Name")
                assert key in TAFFY_PORTRAITS or key in NATIVE_FACES, (name, number, key)
                if key in NATIVE_FACES:
                    assert character.get("Face") in NATIVE_FACES[key]
                    assert character.get("Body") == "nomal"
                else:
                    assert character.get("Face") == "none"
                if character.find("Deactive") is not None:
                    active_characters.discard(key)
                if character.find("Active") is not None:
                    active_characters.add(key)
        model = node.get("Model", "")
        if model:
            assert model in active_characters, (name, number, model)
        assert len(active_characters & TAFFY_PORTRAITS) <= 1, (name, number, active_characters)
        state = changes_by_id.get(number, state)
        assert state is not None and (state == NATIVE_BACKGROUND or state.removeprefix("AceTaffy_") in GENERATED_SHOTS)
        visible.append((number, state, node.findtext("Content") or ""))
    return visible, sorted(changes_by_id.items())


def main() -> None:
    global PACKAGE
    parser = argparse.ArgumentParser()
    parser.add_argument("--package", type=Path, default=SOURCE_PACKAGE)
    parser.add_argument("--game-assets", type=Path, required=True)
    args = parser.parse_args()
    PACKAGE = args.package.resolve()
    lines = []
    lines.append(f"Candidate package: {PACKAGE}")
    transitions = {}
    for name, expected in (("Start", 28), ("End", 17)):
        states, changes = story_states(name)
        assert len(states) == expected
        transitions[name] = (states, changes)
        lines.append(f"{name}: {len(states)} lines; camera changes {changes}")
        lines.append(f"{name}: every line has a resolved image; direct seek restores the latest prior change")

    images = {}
    for key in sorted(GENERATED_SHOTS):
        source = SOURCE_PACKAGE / "Resource" / "Story" / f"{key}.png"
        loaded = PACKAGE / "Resource" / "StoryBgSprite" / f"AceTaffy_{key}.png"
        assert source.is_file(), f"Missing source image: {source}"
        assert loaded.is_file(), f"Missing native loader image: {loaded}"
        assert digest(source) == digest(loaded), f"Deployment copy differs: {key}"
        with Image.open(loaded) as image:
            image.load()
            assert image.format == "PNG" and image.width >= 640 and image.height >= 360
            assert abs(image.width / image.height - 16 / 9) < 0.02
            images[key] = image.convert("RGB")
            lines.append(f"{key}: decoded PNG {image.width}x{image.height}; sha256 {digest(loaded)}")

    taffy_images = {}
    for key in sorted(TAFFY_PORTRAITS):
        source = SOURCE_PACKAGE / "Resource" / "Story" / "Characters" / f"{key}.png"
        loaded = PACKAGE / "Resource" / "StoryStanding" / f"{key}.png"
        assert source.is_file() and loaded.is_file(), f"Missing portrait source or native loader copy: {key}"
        assert digest(source) == digest(loaded), f"Portrait deployment copy differs: {key}"
        with Image.open(loaded) as portrait:
            portrait.load()
            rgba = portrait.convert("RGBA")
            lo, hi = rgba.getchannel("A").getextrema()
            assert portrait.format == "PNG" and lo == 0 and hi >= 250
            taffy_images[key] = rgba
            lines.append(f"{key}: transparent PNG {portrait.width}x{portrait.height}; sha256 {digest(loaded)}")

    native_background, native_images = native_assets(args.game_assets)
    images[NATIVE_BACKGROUND] = native_background
    lines.append(f"Original background {NATIVE_BACKGROUND}: Sprite pathId 22179, 1920x1080; loaded from {args.game_assets}")
    lines.append(f"Original portrait prefabs decoded from {args.game_assets}: {sorted(native_images)}")

    font_path = Path("C:/Windows/Fonts/msyh.ttc")
    font = ImageFont.truetype(str(font_path), 22) if font_path.exists() else ImageFont.load_default()
    canvas = Image.new("RGB", (1280, 720), "#10131a")
    draw = ImageDraw.Draw(canvas, "RGBA")
    samples = (
        ("WorkshopInvitation", transitions["Start"][0][0][2], None, None),
        (NATIVE_BACKGROUND, transitions["Start"][0][8][2], "Taffy_Confused", ("앤젤라", "normal")),
        (NATIVE_BACKGROUND, transitions["Start"][0][26][2], "Taffy_Happy", ("롤랑", "worry")),
        (NATIVE_BACKGROUND, transitions["End"][0][2][2], None, ("앤젤라", "normal")),
    )
    for index, (key, line, taffy, original) in enumerate(samples):
        left, top = (index % 2) * 640, (index // 2) * 360
        image = images[key].resize((640, 360), Image.Resampling.LANCZOS)
        canvas.paste(image, (left, top))
        for actor, side in ((native_images.get(original), "left"), (taffy_images.get(taffy), "right")):
            if actor is None:
                continue
            actor = actor.copy()
            actor.thumbnail((300, 315), Image.Resampling.LANCZOS)
            x = left + 25 if side == "left" else left + 640 - actor.width - 20
            canvas.paste(actor, (x, top + 350 - actor.height), actor)
        draw.rectangle((left, top + 255, left + 640, top + 360), fill=(10, 12, 18, 185))
        draw.text((left + 14, top + 263), key, font=font, fill="white")
        draw.text((left + 14, top + 300), line[:27], font=font, fill="white")
    preview = OUT / "story-preview.png"
    canvas.save(preview)
    lines.append(f"Preview: {preview}")
    lines.append("Native portraits: 앤젤라 / 롤랑 keys and selected face nodes verified in game resources.assets.")
    lines.append("Runtime limit: StoryManager.ChangeVisual was source traced; Unity story UI was not launched.")
    log = OUT / "main.log"
    log.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(log.read_text(encoding="utf-8"))


if __name__ == "__main__":
    main()
