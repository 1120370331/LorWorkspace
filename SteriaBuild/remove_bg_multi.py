"""
生成多个不同阈值的版本供选择
"""
from PIL import Image
import numpy as np

input_path = r"c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\SteriaModFolder\Resource\ArtWork\荆棘_施法.png"
output_dir = r"c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\SteriaModFolder\Resource\ArtWork"

img = Image.open(input_path).convert('RGBA')
data = np.array(img)
r, g, b = data[:,:,0], data[:,:,1], data[:,:,2]
brightness = 0.299 * r + 0.587 * g + 0.114 * b

# 生成不同阈值的版本
thresholds = [15, 20, 25, 30, 35, 40]

for thresh in thresholds:
    result_data = data.copy()
    alpha_mask = np.where(brightness < thresh, 0, 255).astype(np.uint8)
    result_data[:,:,3] = alpha_mask
    result = Image.fromarray(result_data)
    output_path = f"{output_dir}/荆棘_施法_阈值{thresh}.png"
    result.save(output_path)
    print(f"已生成: 阈值{thresh} -> {output_path}")

print("\n请查看这些文件，选择效果最好的阈值!")
