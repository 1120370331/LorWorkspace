"""
去除荆棘_施法.png的黑色背景 - 更激进的阈值
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

# 更激进的黑色阈值 - 提高到50
black_threshold = 50

# 创建透明度蒙版
# 亮度低于阈值的像素设为完全透明
alpha_mask = np.where(brightness < black_threshold, 0, 255).astype(np.uint8)

# 应用新的透明度
data[:,:,3] = alpha_mask

# 创建新图片
result = Image.fromarray(data)

# 保存结果
result.save(output_path)
print(f"已保存到: {output_path}")
print(f"黑色背景去除完成! (阈值: {black_threshold})")
