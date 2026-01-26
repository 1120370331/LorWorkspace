"""
去除荆棘_施法.png的背景，生成透明PNG用于特效
"""
from rembg import remove
from PIL import Image
import os

# 路径配置
input_path = r"c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\SteriaModFolder\Resource\ArtWork\荆棘_施法.png"
output_path = r"c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\SteriaModFolder\Resource\ArtWork\荆棘_施法_透明.png"

print(f"正在处理: {input_path}")

# 读取图片
input_image = Image.open(input_path)
print(f"原图尺寸: {input_image.size}")
print(f"原图模式: {input_image.mode}")

# 使用rembg去除背景
output_image = remove(input_image)

# 保存结果
output_image.save(output_path)
print(f"已保存到: {output_path}")
print(f"输出尺寸: {output_image.size}")
print(f"输出模式: {output_image.mode}")

print("背景去除完成!")
