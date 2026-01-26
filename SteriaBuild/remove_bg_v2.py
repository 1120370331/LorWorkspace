"""
去除荆棘_施法.png的黑色背景，使用颜色阈值方法
"""
from PIL import Image
import numpy as np

# 路径配置
input_path = r"c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\SteriaModFolder\Resource\ArtWork\荆棘_施法.png"
output_path = r"c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\SteriaModFolder\Resource\ArtWork\荆棘_施法_透明.png"

print(f"正在处理: {input_path}")

# 读取图片
img = Image.open(input_path).convert('RGBA')
print(f"原图尺寸: {img.size}")

# 转换为numpy数组
data = np.array(img)

# 获取RGB通道
r, g, b, a = data[:,:,0], data[:,:,1], data[:,:,2], data[:,:,3]

# 计算亮度 (使用加权平均)
brightness = 0.299 * r + 0.587 * g + 0.114 * b

# 黑色阈值 - 亮度低于此值的像素将被设为透明
# 可以调整这个值来控制去除黑色的程度
black_threshold = 30

# 创建透明度蒙版：亮度低于阈值的像素设为透明
# 使用渐变透明度，让边缘更自然
alpha_mask = np.clip((brightness - black_threshold) * 255 / (255 - black_threshold), 0, 255).astype(np.uint8)

# 对于非常暗的像素，直接设为完全透明
alpha_mask[brightness < black_threshold] = 0

# 应用新的透明度
data[:,:,3] = alpha_mask

# 创建新图片
result = Image.fromarray(data, 'RGBA')

# 保存结果
result.save(output_path)
print(f"已保存到: {output_path}")
print(f"输出尺寸: {result.size}")
print("黑色背景去除完成!")
