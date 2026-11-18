"""Rebuild the two AceTaffy promotional PNGs from approved project art."""

from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
OUT = Path(__file__).resolve().parent
ACTIONS = ROOT / "AceTaffyMod/Char/AceTaffy/ClothCustom"
BACKDROP = OUT / "display-backdrop.png"
SQUARE_BACKDROP = OUT / "display-backdrop-square.png"
SOURCE_LAYERS = ROOT / "ArtSource/Character/slender-v2"
sys.path.insert(0, str(ROOT))
import import_character_sheets as character_import


def clean_alpha(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    alpha = image.getchannel("A").point(lambda value: 0 if value < 16 else value)
    image.putalpha(alpha)
    return image


def visible_bounds(image: Image.Image) -> tuple[int, int, int, int]:
    return image.getchannel("A").point(lambda value: 255 if value >= 16 else 0).getbbox()


def high_resolution_default() -> Image.Image:
    """Assemble the shipped Default pose from its body and head masters."""
    source = character_import
    body = clean_alpha(Image.open(SOURCE_LAYERS / "body-sheet.png"))
    head = clean_alpha(Image.open(SOURCE_LAYERS / "head.png"))
    runs = source.opaque_runs(body)
    first_boundary = (runs[0][1] + runs[1][0]) // 2
    cell_width = body.width // 3
    body_tile = body.crop((0, 0, cell_width, first_boundary))
    x_offset = round(256 - cell_width * source.LAYERED_BODY_SCALE / 2)
    y_offset = round(source.LAYERED_GROUND_Y - (runs[0][1] + 1) * source.LAYERED_BODY_SCALE)
    neck_x = round(x_offset + source.LAYERED_NECKS[0][0] * source.LAYERED_BODY_SCALE)
    neck_y = round(y_offset + source.LAYERED_NECKS[0][1] * source.LAYERED_BODY_SCALE)

    factor = 5
    canvas = Image.new("RGBA", (512 * factor, 512 * factor), (0, 0, 0, 0))
    body_scale = source.LAYERED_BODY_SCALE * factor
    body_tile = body_tile.resize((round(body_tile.width * body_scale),
                                  round(body_tile.height * body_scale)), Image.Resampling.LANCZOS)
    canvas.alpha_composite(body_tile, (x_offset * factor, y_offset * factor))
    head_scale = source.LAYERED_HEAD_SCALE * factor
    head = head.resize((round(head.width * head_scale), round(head.height * head_scale)),
                       Image.Resampling.LANCZOS)
    head_x = neck_x * factor - round(source.HEAD_ANCHOR[0] * head_scale)
    head_y = neck_y * factor - round(source.HEAD_ANCHOR[1] * head_scale)
    canvas.alpha_composite(head, (head_x, head_y))
    return canvas.crop(visible_bounds(canvas))


def build_display() -> None:
    background = Image.open(BACKDROP).convert("RGBA")
    portrait = high_resolution_default()
    portrait.save(OUT / "taffy-default-action-transparent.png", optimize=True)
    desired_height = round(background.height * 0.70)
    desired_width = round(portrait.width * desired_height / portrait.height)
    portrait = portrait.resize((desired_width, desired_height), Image.Resampling.LANCZOS)
    x = (background.width - desired_width) // 2
    foot_y = round(background.height * 0.795)
    y = foot_y - desired_height

    shadow = Image.new("RGBA", background.size)
    draw = ImageDraw.Draw(shadow)
    radius_x = round(background.width * 0.172)
    draw.ellipse((background.width // 2 - radius_x, foot_y - 15,
                  background.width // 2 + radius_x, foot_y + 20), fill=(0, 0, 0, 75))
    background = Image.alpha_composite(background, shadow.filter(ImageFilter.GaussianBlur(13)))
    background.alpha_composite(portrait, (x, y))
    background.convert("RGB").save(OUT / "taffy-default-display.png", optimize=True)


def build_square_display() -> None:
    background = Image.open(SQUARE_BACKDROP).convert("RGBA")
    portrait = high_resolution_default()
    desired_height = round(background.height * 0.72)
    desired_width = round(portrait.width * desired_height / portrait.height)
    portrait = portrait.resize((desired_width, desired_height), Image.Resampling.LANCZOS)
    x = (background.width - desired_width) // 2
    foot_y = round(background.height * 0.807)
    y = foot_y - desired_height

    shadow = Image.new("RGBA", background.size)
    draw = ImageDraw.Draw(shadow)
    radius_x = round(background.width * 0.17)
    draw.ellipse((background.width // 2 - radius_x, foot_y - 13,
                  background.width // 2 + radius_x, foot_y + 19), fill=(0, 0, 0, 75))
    background = Image.alpha_composite(background, shadow.filter(ImageFilter.GaussianBlur(13)))
    background.alpha_composite(portrait, (x, y))
    background.convert("RGB").save(OUT / "taffy-default-display-square.png", optimize=True)


def build_actions() -> None:
    names = ("Default", "Move", "Slash", "Penetrate", "Hit", "Guard", "Evade", "Special")
    cell = 512
    sheet = Image.new("RGBA", (cell * 4, cell * 2), (0, 0, 0, 0))
    scale = 1.46
    for index, name in enumerate(names):
        pose = clean_alpha(Image.open(ACTIONS / f"{name}.png"))
        pose = pose.crop(visible_bounds(pose))
        pose = pose.resize((round(pose.width * scale), round(pose.height * scale)), Image.Resampling.LANCZOS)
        col, row = index % 4, index // 4
        x = col * cell + (cell - pose.width) // 2
        y = row * cell + 454 - pose.height
        sheet.alpha_composite(pose, (x, y))
    sheet.save(OUT / "taffy-actions-2x4-transparent.png", optimize=True)


if __name__ == "__main__":
    build_display()
    build_square_display()
    build_actions()
