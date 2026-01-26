"""
从2x3排列的6帧图片中提取各帧，并去除黑色背景
"""
from PIL import Image
import numpy as np
import os

input_path = r"c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\SteriaModFolder\Resource\ArtWork\荆棘囚笼6帧.png"
output_dir = r"c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\SteriaModFolder\Resource\ArtWork"

# 读取图片
img = Image.open(input_path)
print(f"原图尺寸: {img.size}")
print(f"原图模式: {img.mode}")

width, height = img.size

# 3x2排列：3列2行
cols = 3
rows = 2
frame_width = width // cols
frame_height = height // rows

print(f"每帧尺寸: {frame_width} x {frame_height}")

# 黑色背景阈值
BLACK_THRESHOLD = 15

# 提取并处理每一帧
for row in range(rows):
    for col in range(cols):
        frame_num = row * cols + col + 1

        # 计算裁剪区域
        left = col * frame_width
        top = row * frame_height
        right = left + frame_width
        bottom = top + frame_height

        # 裁剪帧
        frame = img.crop((left, top, right, bottom))
        frame = frame.convert('RGBA')

        # 去除黑色背景
        data = np.array(frame)
        r, g, b = data[:,:,0], data[:,:,1], data[:,:,2]
        brightness = 0.299 * r + 0.587 * g + 0.114 * b

        # 低于阈值的设为透明
        alpha = np.where(brightness < BLACK_THRESHOLD, 0, 255).astype(np.uint8)
        data[:,:,3] = alpha

        result = Image.fromarray(data)

        # 保存
        output_path = f"{output_dir}/荆棘囚笼_帧{frame_num}.png"
        result.save(output_path)
        print(f"已保存: 帧{frame_num} -> {output_path}")

print("\n6帧提取完成!")
