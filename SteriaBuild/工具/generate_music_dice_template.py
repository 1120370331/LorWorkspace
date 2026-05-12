"""
生成乐章型骰子 UI 用的两张模板 PNG：
  - music_dice_face.png  : 蓝色骰子面板（与拼点 UI 的 img_diceFace 对应）
  - music_dice_glow.png  : 白色软光（与拼点 UI 的 img_diceFaceLinearDodge 对应）

直接运行：
    python 工具/generate_music_dice_template.py

会同时输出到：
  SteriaModFolder/Resource/ArtWork/music_dice_face.png
  SteriaModFolder/Resource/ArtWork/music_dice_glow.png
  SteriaModFolder/Resource/ArtWork/music_dice_face_template.png   <- 带辅助线的描图底图
  SteriaModFolder/Resource/ArtWork/music_dice_glow_template.png   <- 带辅助线的描图底图

mod 代码里 MusicDiceSpriteFactory 会自动优先读取这里的 PNG。
"""

from PIL import Image, ImageDraw, ImageFilter, ImageFont
from pathlib import Path
import os

SIZE = 256                  # 输出像素，足够清晰即可，运行时会等比缩放
CORNER_RADIUS = int(SIZE * 0.18)
SAFE_MARGIN = int(SIZE * 0.06)  # 安全边距，画面元素留出的内缩

OUT_DIR = Path(__file__).resolve().parent.parent / "SteriaModFolder" / "Resource" / "ArtWork"
OUT_DIR.mkdir(parents=True, exist_ok=True)


def _rounded_mask(size, radius):
    """返回一张圆角矩形的灰度 mask（255 = 不透明）。"""
    mask = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(mask)
    d.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def build_face():
    """蓝色骰子面板：自上而下浅蓝 -> 深蓝的垂直渐变 + 圆角。"""
    top = (158, 219, 255, 255)   # 浅蓝白 ~ #9EDBFF
    bottom = (46, 92, 200, 255)  # 深蓝   ~ #2E5CC8

    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    px = img.load()
    for y in range(SIZE):
        t = y / (SIZE - 1)
        r = int(top[0] + (bottom[0] - top[0]) * t)
        g = int(top[1] + (bottom[1] - top[1]) * t)
        b = int(top[2] + (bottom[2] - top[2]) * t)
        for x in range(SIZE):
            px[x, y] = (r, g, b, 255)

    mask = _rounded_mask(SIZE, CORNER_RADIUS)
    # 应用圆角 alpha
    alpha = mask
    img.putalpha(alpha)

    # 顶部加一点白色 highlight，更像 vanilla 骰子
    highlight = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    hd = ImageDraw.Draw(highlight)
    hd.rounded_rectangle(
        (SAFE_MARGIN, SAFE_MARGIN, SIZE - SAFE_MARGIN, int(SIZE * 0.45)),
        radius=int(CORNER_RADIUS * 0.7),
        fill=(255, 255, 255, 60),
    )
    highlight = highlight.filter(ImageFilter.GaussianBlur(radius=SIZE * 0.04))
    img = Image.alpha_composite(img, highlight)
    img.putalpha(mask)  # 保持外轮廓圆角

    return img


def build_glow():
    """白色软光：内部透明，外缘高亮，模拟 linearDodge 边缘发光。"""
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    cx = cy = (SIZE - 1) / 2.0
    max_d = SIZE * 0.5
    px = img.load()
    for y in range(SIZE):
        for x in range(SIZE):
            # 归一化半径 0~1
            dx = (x - cx) / max_d
            dy = (y - cy) / max_d
            d = (dx * dx + dy * dy) ** 0.5
            d = max(0.0, min(1.0, d))
            # 边缘 0.65~1.0 时强度提升
            edge = max(0.0, (d - 0.65) / 0.35)
            edge = edge * edge
            a = int(min(1.0, edge) * 220)
            px[x, y] = (255, 255, 255, a)

    # 圆角剪裁
    mask = _rounded_mask(SIZE, CORNER_RADIUS)
    img.putalpha(Image.eval(mask, lambda v: v).point(lambda v: v))
    # 合成现有 alpha 与圆角 mask（取最小）
    a1, _, _, _ = img.getchannel("A"), None, None, None
    base = img.getchannel("A")
    final_a = Image.eval(base, lambda v: v)
    # 用 mask 限制四角不超出圆角
    final_a = Image.eval(mask, lambda m: m).point(lambda v: v)
    # 实际做"取最小"：用 ImageChops
    from PIL import ImageChops
    final_a = ImageChops.multiply(base, mask).point(lambda v: v)
    # 上面写法等价于 round(base*mask/255)，效果即"两者都不透明才算"
    img.putalpha(final_a)

    # 高斯模糊柔化
    img = img.filter(ImageFilter.GaussianBlur(radius=SIZE * 0.015))
    return img


def add_guides(base, label):
    """在 base 上叠加描图辅助线，输出 *_template.png。"""
    img = base.copy()
    overlay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(overlay)

    # 外圆角参考线
    d.rounded_rectangle(
        (1, 1, SIZE - 2, SIZE - 2),
        radius=CORNER_RADIUS,
        outline=(255, 0, 0, 180),
        width=2,
    )
    # 安全边距
    d.rounded_rectangle(
        (SAFE_MARGIN, SAFE_MARGIN, SIZE - 1 - SAFE_MARGIN, SIZE - 1 - SAFE_MARGIN),
        radius=int(CORNER_RADIUS * 0.7),
        outline=(0, 255, 0, 160),
        width=1,
    )
    # 中心十字
    d.line((SIZE // 2, 0, SIZE // 2, SIZE), fill=(255, 255, 255, 120), width=1)
    d.line((0, SIZE // 2, SIZE, SIZE // 2), fill=(255, 255, 255, 120), width=1)
    # 1/3 三分线
    for k in (1, 2):
        d.line((SIZE * k // 3, 0, SIZE * k // 3, SIZE), fill=(255, 255, 255, 60), width=1)
        d.line((0, SIZE * k // 3, SIZE, SIZE * k // 3), fill=(255, 255, 255, 60), width=1)

    # 标注（不依赖系统字体也能跑）
    try:
        font = ImageFont.truetype("arial.ttf", 14)
    except Exception:
        font = ImageFont.load_default()
    d.text((6, 6), f"{label}  {SIZE}x{SIZE}", fill=(255, 255, 255, 220), font=font)
    d.text((6, SIZE - 22), "red=corner  green=safe area", fill=(255, 255, 0, 220), font=font)

    return Image.alpha_composite(img, overlay)


def main():
    face = build_face()
    glow = build_glow()

    face_path = OUT_DIR / "music_dice_face.png"
    glow_path = OUT_DIR / "music_dice_glow.png"
    face_tpl = OUT_DIR / "music_dice_face_template.png"
    glow_tpl = OUT_DIR / "music_dice_glow_template.png"

    face.save(face_path)
    glow.save(glow_path)
    add_guides(face, "FACE").save(face_tpl)
    add_guides(glow, "GLOW").save(glow_tpl)

    print(f"[OK] face   -> {face_path}")
    print(f"[OK] glow   -> {glow_path}")
    print(f"[OK] face_t -> {face_tpl}")
    print(f"[OK] glow_t -> {glow_tpl}")


if __name__ == "__main__":
    main()
