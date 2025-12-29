#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
将设定文件夹内所有.md文件组装成一份总设定文件
"""

import os
from pathlib import Path

# 设定文件夹路径
SETTINGS_DIR = Path(__file__).parent

# 输出文件名
OUTPUT_FILE = "总设定.md"

# 文件排序优先级（越小越靠前，未列出的文件按文件名排序放在最后）
FILE_ORDER = {
    "战斗机制.md": 0,
    "安希尔.md": 1,
    "斯拉泽雅.md": 2,
    "司流者教徒.md": 3,
    "薇莉亚.md": 4,
    "提布.md": 5,
}

def get_sort_key(filename: str) -> tuple:
    """获取文件排序键"""
    if filename in FILE_ORDER:
        return (0, FILE_ORDER[filename], filename)
    return (1, 0, filename)

def main():
    # 获取所有.md文件（排除输出文件本身）
    md_files = [
        f for f in SETTINGS_DIR.glob("*.md")
        if f.name != OUTPUT_FILE
    ]

    # 按优先级排序
    md_files.sort(key=lambda f: get_sort_key(f.name))

    # 组装内容
    contents = []
    contents.append("# Steria Mod 总设定\n")
    contents.append("这是一个游戏「废墟图书馆」的同人mod\n")
    contents.append("---\n")

    for md_file in md_files:
        print(f"正在处理: {md_file.name}")
        with open(md_file, "r", encoding="utf-8") as f:
            content = f.read().strip()
        contents.append(content)
        contents.append("\n\n---\n")

    # 写入输出文件
    output_path = SETTINGS_DIR / OUTPUT_FILE
    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(contents))

    print(f"\n组装完成！输出文件: {output_path}")
    print(f"共处理 {len(md_files)} 个文件")

if __name__ == "__main__":
    main()
