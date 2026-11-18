"""Archive original UI art once, then derive stable game-sized PNGs from masters."""
from pathlib import Path
import shutil

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
RESOURCE = ROOT / "AceTaffyMod/Resource"
MASTERS = ROOT / "ArtSource"
PREVIEW = ROOT / "Verification/card-contact-sheet.png"

CARDS = (
    "Ignition", "BrassRivet", "ExhaustRecycle", "PistonChain",
    "FlueReversal", "SafetyValve", "BoilerArmor", "Supercritical", "SteamDetonation", "Improvise",
) + tuple(f"Device_{card_id}" for card_id in range(1883500, 1883510))
ICONS = ("AceTaffy_Pressure", "AceTaffy_Guard")

def normalize_one(filename: str, resource_dir: Path, master_dir: Path, size: tuple[int, int]):
    source = resource_dir / filename
    master = master_dir / filename
    if not master.is_file():
        if not source.is_file():
            print(f"PENDING {source.relative_to(ROOT)}")
            return False
        with Image.open(source) as im:
            original_size = im.size
            if size == (512, 384) and original_size[0] * 3 != original_size[1] * 4:
                raise ValueError(f"{source}: card master must be exactly 4:3, got {original_size}")
            if size == (128, 128) and original_size[0] != original_size[1]:
                raise ValueError(f"{source}: icon master must be square, got {original_size}")
            if original_size[0] <= size[0] or original_size[1] <= size[1]:
                raise ValueError(f"{source}: missing high-resolution master; refusing to archive an output-sized PNG")
        master_dir.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, master)  # exact, unchanged source bytes are preserved
        print(f"ARCHIVED {master.relative_to(ROOT)} ({original_size[0]}x{original_size[1]})")

    with Image.open(master) as opened:
        if size == (512, 384) and opened.width * 3 != opened.height * 4:
            raise ValueError(f"{master}: card master is not exactly 4:3")
        if size == (128, 128) and opened.width != opened.height:
            raise ValueError(f"{master}: icon master is not square")
        if opened.width <= size[0] or opened.height <= size[1]:
            raise ValueError(f"{master}: master is output-sized; original source may have been lost")
        resized = opened.resize(size, Image.Resampling.LANCZOS)
        resource_dir.mkdir(parents=True, exist_ok=True)
        resized.save(source, format="PNG", optimize=True)
    print(f"NORMALIZED {source.relative_to(ROOT)} -> {size[0]}x{size[1]}")
    return True

def contact_sheet():
    tile_w, tile_h, gap, caption = 512, 384, 16, 28
    columns = 4
    rows = (len(CARDS) + columns - 1) // columns
    canvas = Image.new("RGB", (columns * tile_w + (columns + 1) * gap, rows * (tile_h + caption) + (rows + 1) * gap), "#30343b")
    draw = ImageDraw.Draw(canvas)
    for index, name in enumerate(CARDS):
        x = gap + (index % columns) * (tile_w + gap)
        y = gap + (index // columns) * (tile_h + caption + gap)
        path = RESOURCE / "CombatPageArtwork" / f"AceTaffy_{name}.png"
        if path.is_file():
            with Image.open(path) as im:
                if im.size != (tile_w, tile_h):
                    raise ValueError(f"{path}: contact sheet needs normalized 512x384 card")
                canvas.paste(im.convert("RGB"), (x, y))
        else:
            draw.rectangle((x, y, x + tile_w - 1, y + tile_h - 1), fill="#474b53")
            draw.text((x + 12, y + 12), "PENDING", fill="white")
        draw.text((x + 4, y + tile_h + 6), f"{index + 1}. {name}", fill="white")
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(PREVIEW)
    print(f"CONTACT {PREVIEW.relative_to(ROOT)}")

if __name__ == "__main__":
    complete = True
    for name in CARDS:
        complete &= normalize_one(f"AceTaffy_{name}.png", RESOURCE / "CombatPageArtwork", MASTERS / "Cards-redraw", (512, 384))
    for name in ICONS:
        complete &= normalize_one(f"{name}.png", RESOURCE / "ArtWork", MASTERS / "Icons", (128, 128))
    contact_sheet()
    print("UI art complete" if complete else "UI art incomplete; rerun after the pending originals arrive")
