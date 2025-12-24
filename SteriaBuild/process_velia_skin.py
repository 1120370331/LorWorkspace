"""
处理Velia角色皮肤图片
- 分割4x2布局图片为8张独立图片
- 水平翻转闪避图片
- 抠图去除白色背景
"""
from PIL import Image
import os

# 源图片路径
SOURCE_IMAGE = r"SteriaModFolder\Resource\CharacterSkin\Velia.png"

# 输出目录
OUTPUT_DIR = r"SteriaModFolder\Char\Velia"

# 4x2布局对应的动作名称
# 第一行：待机、斩击、打击、突刺
# 第二行：防御、闪避（需翻转）、受击、专属群攻施法
ACTIONS = [
    ["Default", "Slash", "Hit", "Penetrate"],
    ["Guard", "Evade", "Damaged", "Special"]
]

def remove_white_background(img, threshold=240):
    """去除白色背景，转换为透明"""
    img = img.convert("RGBA")
    datas = img.getdata()

    new_data = []
    for item in datas:
        # 如果像素接近白色，设为透明
        if item[0] > threshold and item[1] > threshold and item[2] > threshold:
            new_data.append((255, 255, 255, 0))
        else:
            new_data.append(item)

    img.putdata(new_data)
    return img

def process_velia_skin():
    # 确保输出目录存在
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    # 打开源图片
    print(f"正在打开图片: {SOURCE_IMAGE}")
    img = Image.open(SOURCE_IMAGE)
    width, height = img.size
    print(f"图片尺寸: {width}x{height}")

    # 计算每个格子的尺寸
    cell_width = width // 4
    cell_height = height // 2
    print(f"每个格子尺寸: {cell_width}x{cell_height}")

    # 分割并处理每个格子
    for row in range(2):
        for col in range(4):
            action_name = ACTIONS[row][col]

            # 计算裁剪区域
            left = col * cell_width
            top = row * cell_height
            right = left + cell_width
            bottom = top + cell_height

            # 裁剪
            cell_img = img.crop((left, top, right, bottom))

            # 如果是闪避图片，水平翻转
            if action_name == "Evade":
                print(f"翻转闪避图片...")
                cell_img = cell_img.transpose(Image.FLIP_LEFT_RIGHT)

            # 去除白色背景
            print(f"处理 {action_name}...")
            cell_img = remove_white_background(cell_img)

            # 保存
            output_path = os.path.join(OUTPUT_DIR, f"{action_name}.png")
            cell_img.save(output_path, "PNG")
            print(f"已保存: {output_path}")

    print("\n所有图片处理完成!")
    print(f"输出目录: {OUTPUT_DIR}")

if __name__ == "__main__":
    process_velia_skin()
