"""Small static package gate for the actual BaseMod/game loader paths."""
import argparse
import hashlib
import sys
import unicodedata
from pathlib import Path
from xml.etree import ElementTree as ET

from PIL import Image

ACTIONS = ("Default", "Move", "Slash", "Penetrate", "Hit", "Guard", "Evade", "Damaged", "Special")
CARDS = ("Ignition", "BrassRivet", "ExhaustRecycle", "PistonChain", "FlueReversal", "SafetyValve", "BoilerArmor", "Supercritical", "SteamDetonation", "Improvise") + tuple(f"Device_{card_id}" for card_id in range(1883500, 1883510))
STORY = ("WorkshopInvitation",)
NATIVE_STORY_KEYS = {"ch1_library_inside"}  # original Sprite pathId 22179, Resources.Load StoryResource/BgSprites
PORTRAITS = ("Taffy_Default", "Taffy_Happy", "Taffy_Confused")
BUILTIN_CHARACTERS = ("앤젤라", "롤랑")
UI_TEXT_XMLS = (
    "Data/CardInfo.xml",
    "Data/PassiveList.xml",
    "Localize/cn/BattlesCards/BattleCardDesc_AceTaffy.xml",
    "Localize/cn/BattleCardAbilities/BattleCardAbilities_AceTaffy.xml",
    "Localize/cn/PassiveDesc/PassiveDesc.xml",
    "Localize/cn/EffectTexts/AceTaffy_Effects.xml",
)
CARD_DESC_XML = "Localize/cn/BattlesCards/BattleCardDesc_AceTaffy.xml"
OLD_CARD_DESC_XML = "Localize/cn/BattleCards/BattleCardDesc_AceTaffy.xml"
CARD_DESC_IDS = set(range(1883001, 1883011)) | set(range(1883500, 1883510))
EMPTY_CARD_ABILITIES = {1883002, 1883004, 1883500, 1883501, 1883502, 1883504, 1883505, 1883506, 1883508, 1883509}
def owner_id(element: ET.Element, parents: dict[ET.Element, ET.Element]) -> str:
    current = element
    while current in parents:
        current = parents[current]
        if "ID" in current.attrib:
            return current.attrib["ID"]
    return "?"

def card_desc_structure_issues(root: ET.Element) -> list[str]:
    """Validate the exact BaseMod BattleCardDescRoot_V2 loader contract."""
    if root.tag != "BattleCardDescRoot":
        return [f"expected BattleCardDescRoot, got {root.tag}"]
    lists = root.findall("cardDescList")
    if len(lists) != 1:
        return [f"expected one cardDescList, got {len(lists)}"]
    cards = lists[0].findall("BattleCardDesc")
    issues = []
    if len(cards) != len(CARD_DESC_IDS):
        issues.append(f"expected {len(CARD_DESC_IDS)} BattleCardDesc rows, got {len(cards)}")
    seen = set()
    for card in cards:
        raw_id = card.attrib.get("ID", "")
        try:
            card_id = int(raw_id)
        except ValueError:
            issues.append(f"BattleCardDesc ID={raw_id!r} is not numeric")
            continue
        if card_id in seen:
            issues.append(f"duplicate BattleCardDesc ID={card_id}")
        seen.add(card_id)
        if card.find("Name") is not None or card.find("Desc") is not None:
            issues.append(f"ID={card_id} uses old Name/Desc fields")
        names = card.findall("LocalizedName")
        abilities = card.findall("Ability")
        if len(names) != 1 or not (names[0].text or "").strip():
            issues.append(f"ID={card_id} needs one nonempty LocalizedName")
        if len(abilities) != 1:
            issues.append(f"ID={card_id} needs one Ability element")
        elif card_id not in EMPTY_CARD_ABILITIES and not (abilities[0].text or "").strip():
            issues.append(f"ID={card_id} Ability is unexpectedly empty")
    if seen != CARD_DESC_IDS:
        issues.append(f"card IDs differ: missing={sorted(CARD_DESC_IDS - seen)}, extra={sorted(seen - CARD_DESC_IDS)}")
    return issues

def verify(package: Path, allow_incomplete_assets: bool) -> list[str]:
    errors = []
    def required(path: Path, asset: bool = False):
        if not path.is_file() and (not asset or not allow_incomplete_assets):
            errors.append(f"missing: {path.relative_to(package)}")

    required(package / "Assemblies/AceTaffy.dll")
    stage = package / "StageModInfo.xml"
    required(stage)
    if stage.is_file():
        try:
            invitation = ET.parse(stage).getroot()
            workshop_id = invitation.findtext("Workshop/ID")
            if workshop_id != "AceTaffy":
                errors.append(f"Workshop ID is {workshop_id!r}, expected AceTaffy")
            for entry in invitation.findall("InvitationFile/*"):
                if entry.attrib.get("Exist", "false").lower() == "true":
                    path = (entry.findtext("Path") or "").lstrip("\\/").replace("\\", "/")
                    required(package / path)
        except ET.ParseError as exc:
            errors.append(f"StageModInfo.xml invalid: {exc}")

    skins = [("AceTaffy", "False"), ("AceTaffy_Projection", "True"), ("AceTaffy_Workshop", "False")]
    skins += [(f"AceTaffy_Invention_{q}_{k}", "False") for q in range(5) for k in range(5)]
    for skin, head_enabled in skins:
        skin_root = package / "Char" / skin
        info = skin_root / "ModInfo.xml"
        required(info)
        if info.is_file():
            try:
                root = ET.parse(info).getroot()
                cloth = root.find("ClothInfo")
                if cloth is None or cloth.findtext("Name") != skin:
                    errors.append(f"Char/{skin}/ModInfo.xml ClothInfo/Name mismatch")
                else:
                    for action in ACTIONS:
                        node = cloth.find(action)
                        if node is None or node.find("Pivot") is None or node.find("Head") is None:
                            errors.append(f"Char/{skin} missing {action} motion fields")
                        elif node.find("Head").attrib.get("head_enable") != head_enabled:
                            errors.append(f"Char/{skin} {action} head_enable mismatch")
            except ET.ParseError as exc:
                errors.append(f"Char/{skin}/ModInfo.xml invalid: {exc}")
        for action in ACTIONS:
            required(skin_root / "ClothCustom" / f"{action}.png", asset=True)
    for card in CARDS:
        required(package / "Resource/CombatPageArtwork" / f"AceTaffy_{card}.png", asset=True)
    for icon in ("AceTaffy_Pressure", "AceTaffy_Guard"):
        required(package / "ArtWork" / f"{icon}.png", asset=True)
    invention_icons = ["AceTaffy_InventionProgress"] + [
        f"AceTaffy_Invention_{q}_{k}" for q in range(5) for k in range(5)]
    invention_icons += [f"AceTaffy_InventionAura_{q}" for q in range(5)]
    for icon in invention_icons:
        source = package / "Resource/ArtWork" / f"{icon}.png"
        staged = package / "ArtWork" / f"{icon}.png"
        required(source, asset=True)
        required(staged, asset=True)
        if source.is_file() and staged.is_file():
            if source.read_bytes() != staged.read_bytes():
                errors.append(f"ArtWork/{icon}.png differs from its source")
            with Image.open(staged) as image:
                if image.size != (128, 128) or image.mode != "RGBA" or image.getchannel("A").getextrema() != (0, 255):
                    errors.append(f"ArtWork/{icon}.png must be visible transparent 128x128 RGBA")
    for effect in ("SteamDetonation", "SteamPressureArc"):
        required(package / "Resource/Effects/AceTaffy" / f"{effect}.png", asset=True)

    # Both StorySerializer's Bg and BaseMod's CG load Resource/StoryBgSprite.
    expected_story_keys = {f"AceTaffy_{name}" for name in STORY}
    for name in STORY:
        source = package / "Resource/Story" / f"{name}.png"
        staged = package / "Resource/StoryBgSprite" / f"AceTaffy_{name}.png"
        required(source, asset=True)
        required(staged, asset=True)
        for path in (source, staged):
            if path.is_file():
                try:
                    with Image.open(path) as image:
                        if image.format != "PNG" or image.mode not in ("RGB", "RGBA"):
                            errors.append(f"{path.relative_to(package)} must be an RGB/RGBA PNG")
                        image.load()
                except Exception as exc:
                    errors.append(f"{path.relative_to(package)} cannot be decoded: {exc}")
        if source.is_file() and staged.is_file():
            if hashlib.sha256(source.read_bytes()).digest() != hashlib.sha256(staged.read_bytes()).digest():
                errors.append(f"Resource/StoryBgSprite/AceTaffy_{name}.png differs from its source")

    for name in PORTRAITS:
        source = package / "Resource/Story/Characters" / f"{name}.png"
        staged = package / "Resource/StoryStanding" / f"{name}.png"
        required(source, asset=True)
        required(staged, asset=True)
        for path in (source, staged):
            if path.is_file():
                try:
                    with Image.open(path) as image:
                        if image.format != "PNG" or image.mode != "RGBA":
                            errors.append(f"{path.relative_to(package)} must be a transparent RGBA PNG")
                        else:
                            min_alpha, max_alpha = image.getchannel("A").getextrema()
                            if min_alpha != 0 or max_alpha == 0:
                                errors.append(f"{path.relative_to(package)} needs visible art and a transparent background")
                        image.load()
                except Exception as exc:
                    errors.append(f"{path.relative_to(package)} cannot be decoded: {exc}")
        if source.is_file() and staged.is_file():
            if hashlib.sha256(source.read_bytes()).digest() != hashlib.sha256(staged.read_bytes()).digest():
                errors.append(f"Resource/StoryStanding/{name}.png differs from its source")

    story_effect = package / "Data/StoryEffect"
    referenced_portraits = set()
    for path in sorted(story_effect.glob("*.xml")):
        try:
            root = ET.parse(path).getroot()
            for element in root.iter():
                if element.tag in ("Bg", "CG"):
                    key = element.attrib.get("Src", "")
                    if key not in expected_story_keys and key not in NATIVE_STORY_KEYS:
                        errors.append(f"{path.relative_to(package)} {element.tag} Src={key!r} is neither staged nor the verified original library background")
                if element.tag == "Character":
                    name = element.attrib.get("Name", "")
                    if name in PORTRAITS:
                        referenced_portraits.add(name)
                    elif name not in BUILTIN_CHARACTERS:
                        errors.append(f"{path.relative_to(package)} Character Name={name!r} is neither a staged portrait nor a known built-in")
        except ET.ParseError as exc:
            errors.append(f"{path.relative_to(package)} invalid: {exc}")
    if story_effect.is_dir() and set(PORTRAITS) - referenced_portraits:
        errors.append(f"StoryEffect does not reference portraits: {sorted(set(PORTRAITS) - referenced_portraits)}")

    # Native Chinese punctuation is intentional. Keep machine delimiters and
    # numbers ASCII-width, while permitting the game's Chinese prose and numerals.
    chinese_symbols = set("，。；：！？、（）“”‘’～—…·ⅠⅡⅢⅣⅤⅥ")
    if (package / OLD_CARD_DESC_XML).exists():
        errors.append(f"stale singular card localization path: {OLD_CARD_DESC_XML}")
    for name in UI_TEXT_XMLS:
        path = package / name
        required(path)
        if not path.is_file():
            continue
        try:
            root = ET.parse(path).getroot()
            if name == CARD_DESC_XML:
                errors.extend(f"{name} {issue}" for issue in card_desc_structure_issues(root))
            parents = {child: parent for parent in root.iter() for child in parent}
            for element in root.iter():
                if element.tag not in ("Name", "Desc", "ScriptDesc", "LocalizedName", "Ability") or not element.text:
                    continue
                symbols = sorted({char for char in element.text
                                  if ord(char) > 127 and unicodedata.category(char)[0] in "PSN"
                                  and char not in chinese_symbols})
                if symbols:
                    errors.append(f"{name} {element.tag} ID={owner_id(element, parents)} has non-ASCII digits or symbols: "
                                  + ", ".join(f"U+{ord(char):04X}" for char in symbols))
        except ET.ParseError as exc:
            errors.append(f"{name} invalid: {exc}")

    # The package must never ship dependencies supplied by the game/BaseMod.
    for dependency in ("Assembly-CSharp.dll", "BaseMod.dll", "0Harmony.dll", "UnityEngine.dll"):
        if (package / "Assemblies" / dependency).exists():
            errors.append(f"unexpected copied dependency: Assemblies/{dependency}")
    return errors

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--package", type=Path, required=True)
    parser.add_argument("--allow-incomplete-assets", action="store_true")
    args = parser.parse_args()
    errors = verify(args.package.resolve(), args.allow_incomplete_assets)
    for error in errors:
        print("ERROR", error, file=sys.stderr)
    print(f"Package verification: {'FAIL' if errors else 'PASS'} ({len(errors)} errors)")
    sys.exit(bool(errors))
