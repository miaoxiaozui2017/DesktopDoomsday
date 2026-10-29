#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修正 Assets/Resources/Art/Sprites 下 PNG 的 .meta 导入设置：
  textureType: 8 (Sprite) / spriteMode: 1 (Single) /
  filterMode: 0 (Point) / enableMipMap: 0 / textureCompression: 0 (Uncompressed)
用于首次导入时像素导入器尚未编译完成、导致被当成 Default 纹理的情况。
Unity 检测到 meta 变化会自动重新导入。
"""
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BASE = os.path.join(ROOT, "Assets", "Resources", "Art", "Sprites")

REPL = [
    ("textureType: 0", "textureType: 8"),
    ("spriteMode: 0", "spriteMode: 1"),
    ("filterMode: 1", "filterMode: 0"),
    ("enableMipMap: 1", "enableMipMap: 0"),
    ("textureCompression: 1", "textureCompression: 0"),
]


def main():
    n = 0
    for dirpath, _, files in os.walk(BASE):
        for f in files:
            if not f.endswith(".png.meta"):
                continue
            p = os.path.join(dirpath, f)
            with open(p, encoding="utf-8") as fh:
                t = fh.read()
            orig = t
            for a, b in REPL:
                t = t.replace(a, b)
            if t != orig:
                with open(p, "w", encoding="utf-8", newline="\n") as fh:
                    fh.write(t)
                n += 1
    print(f"patched {n} meta files")


if __name__ == "__main__":
    main()
