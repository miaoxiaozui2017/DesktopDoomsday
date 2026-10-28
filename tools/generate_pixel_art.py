#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Desktop Doomsday — 程序化像素美术生成器
=======================================
由代码绘制全部精灵并输出为 PNG，永久落库 Assets/Art/。
风格：对齐 SPEC 第 9 节（高清像素风 + 鲜明配色 + 科技感）。
所有精灵在「基准网格」上 1 像素绘制，再整数倍最近邻放大，保证 Unity 导入后锐利。

运行：
    python tools/generate_pixel_art.py
依赖：Pillow（已装入隔离 venv）。
可复跑：重复执行会覆盖同名文件，不破坏其它资源。
"""

import os
from PIL import Image, ImageDraw

# ---------------------------------------------------------------------------
# 调色板（对齐 SPEC 9.3）
# ---------------------------------------------------------------------------
PAL = {
    "bg_dark":   (11, 14, 20),
    "bg_panel":  (18, 24, 38),
    "cyan":      (34, 211, 238),   # 科技青（主强调）
    "amber":     (255, 176, 0),    # 琥珀（高亮）
    "green":     (61, 220, 132),   # 正常 / 状态好
    "red":       (255, 77, 77),    # 警告 / 虚弱
    "text":      (230, 241, 255),  # 文本主色
    "white":     (245, 250, 255),
    "dark":      (10, 12, 16),
    "wheel":     (20, 20, 24),
    "rim":       (120, 130, 140),
    "asphalt":   (34, 38, 46),
    "line":      (240, 220, 120),
    # 区域色调
    "suburb_sky":   (27, 58, 75),
    "suburb_ground":(42, 157, 143),
    "desert_sky":   (70, 56, 42),
    "desert_ground":(201, 162, 75),
    "tundra_sky":   (40, 60, 85),
    "tundra_ground":(191, 227, 242),
}

# 车辆车身色
VEH_COLOR = {
    "pickup": PAL["green"],
    "van":    PAL["amber"],
    "moto":   PAL["cyan"],
}

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Assets", "Art")


# ---------------------------------------------------------------------------
# 基础工具
# ---------------------------------------------------------------------------
def canvas(w, h, opaque=False):
    if opaque:
        img = Image.new("RGBA", (w, h), (0, 0, 0, 255))
    else:
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def upscale(img, factor):
    w, h = img.size
    return img.resize((w * factor, h * factor), Image.NEAREST)


def save(img, rel_path):
    out = os.path.join(ART, rel_path)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    img.save(out)
    print(f"  wrote {rel_path}  ({img.size[0]}x{img.size[1]})")


def rect(d, x, y, w, h, color):
    d.rectangle([x, y, x + w - 1, y + h - 1], fill=color)


def px(d, x, y, color):
    d.rectangle([x, y, x, y], fill=color)


def wheel(d, cx, cy, r, frame=0):
    # 轮胎
    for dx in range(-r, r + 1):
        for dy in range(-r, r + 1):
            if dx * dx + dy * dy <= r * r:
                px(d, cx + dx, cy + dy, PAL["wheel"])
    # 轮辋
    for dx in range(-(r - 1), (r - 1) + 1):
        for dy in range(-(r - 1), (r - 1) + 1):
            if dx * dx + dy * dy <= (r - 1) * (r - 1):
                px(d, cx + dx, cy + dy, PAL["rim"])
    # 轮毂（实心）
    for dx in range(-1, 2):
        for dy in range(-1, 2):
            px(d, cx + dx, cy + dy, PAL["dark"])
    # 辐条（转动帧）
    if frame == 0:
        px(d, cx + 2, cy, PAL["rim"]); px(d, cx - 2, cy, PAL["rim"])
        px(d, cx, cy + 2, PAL["rim"]); px(d, cx, cy - 2, PAL["rim"])
    else:
        px(d, cx + 1, cy + 1, PAL["rim"]); px(d, cx - 1, cy - 1, PAL["rim"])
        px(d, cx + 1, cy - 1, PAL["rim"]); px(d, cx - 1, cy + 1, PAL["rim"])


# ---------------------------------------------------------------------------
# 车辆（侧视，2 帧轮子）
# ---------------------------------------------------------------------------
def make_vehicle(kind, frame):
    W, H = 32, 20
    img, d = canvas(W, H)
    col = VEH_COLOR[kind]
    if kind == "moto":
        # 车身（细长）
        rect(d, 6, 9, 20, 4, col)
        rect(d, 10, 5, 8, 5, col)          # 油箱/座
        rect(d, 22, 6, 3, 4, PAL["dark"])  # 车把
        rect(d, 4, 8, 4, 3, col)           # 前叉
        wheel(d, 8, 15, 4, frame)
        wheel(d, 25, 15, 4, frame)
        rect(d, 14, 11, 2, 2, PAL["text"]) # 骑手一点
    else:
        # 主车身
        rect(d, 3, 8, 26, 7, col)
        # 车窗（暗）
        rect(d, 16, 4, 9, 5, PAL["bg_dark"])
        rect(d, 17, 5, 7, 3, PAL["cyan"])  # 窗反光（科技感）
        if kind == "van":
            # 厢式：更高更方
            rect(d, 3, 4, 22, 11, col)
            rect(d, 6, 5, 16, 5, PAL["bg_dark"])
            rect(d, 7, 6, 14, 3, PAL["cyan"])
        else:
            # 皮卡：车头
            rect(d, 24, 6, 5, 9, col)
            rect(d, 25, 7, 3, 3, PAL["bg_dark"])
        # 底盘阴影
        rect(d, 4, 15, 24, 1, PAL["dark"])
        wheel(d, 9, 15, 3, frame)
        wheel(d, 23, 15, 3, frame)
        # 科技描边（1px 高光）
        rect(d, 3, 8, 26, 1, PAL["white"])
    return upscale(img, 8)


def gen_vehicles():
    for kind in ("pickup", "van", "moto"):
        for f in (0, 1):
            save(make_vehicle(kind, f), f"Sprites/Vehicles/{kind}_{f}.png")


# ---------------------------------------------------------------------------
# 小人（idle / walk0 / walk1）
# ---------------------------------------------------------------------------
def make_survivor(frame):
    W, H = 12, 20
    img, d = canvas(W, H)
    # 头
    rect(d, 4, 1, 4, 4, PAL["text"])
    rect(d, 5, 2, 2, 2, PAL["dark"])      # 头盔阴影
    # 身体（科技外套）
    rect(d, 3, 5, 6, 7, PAL["green"])
    rect(d, 3, 5, 6, 1, PAL["cyan"])       # 领口描边
    # 背包
    rect(d, 1, 6, 2, 5, PAL["amber"])
    # 腿
    if frame == 0:
        rect(d, 4, 12, 2, 6, PAL["bg_panel"])
        rect(d, 7, 12, 2, 6, PAL["bg_panel"])
    elif frame == 1:
        rect(d, 3, 12, 2, 6, PAL["bg_panel"])
        rect(d, 8, 12, 2, 6, PAL["bg_panel"])
    else:  # idle
        rect(d, 4, 12, 2, 6, PAL["bg_panel"])
        rect(d, 7, 12, 2, 5, PAL["bg_panel"])
    return upscale(img, 8)


def gen_survivor():
    for f, name in ((0, "walk0"), (1, "walk1"), (2, "idle")):
        save(make_survivor(f), f"Sprites/Survivor/survivor_{name}.png")


# ---------------------------------------------------------------------------
# 宝箱（里程 / 随机 / 副本）
# ---------------------------------------------------------------------------
def make_chest(kind):
    W, H = 16, 14
    img, d = canvas(W, H)
    body = PAL["amber"] if kind != "dungeon" else PAL["cyan"]
    # 箱体
    rect(d, 2, 5, 12, 8, body)
    rect(d, 2, 5, 12, 1, PAL["white"])     # 高光边
    # 箱盖
    rect(d, 2, 2, 12, 3, tuple(min(255, c + 30) for c in body))
    # 锁 / 装饰
    rect(d, 7, 7, 2, 3, PAL["dark"])
    if kind == "mileage":
        # 公路里程碑：顶上小旗
        rect(d, 7, 0, 2, 2, PAL["green"])
        rect(d, 8, 0, 1, 4, PAL["text"])
    elif kind == "random":
        # 问号
        px(d, 7, 6, PAL["bg_dark"]); px(d, 8, 6, PAL["bg_dark"])
        px(d, 8, 7, PAL["bg_dark"]); px(d, 7, 8, PAL["bg_dark"])
        px(d, 8, 9, PAL["bg_dark"])
    else:  # dungeon 发光
        rect(d, 6, 8, 4, 2, PAL["white"])
        px(d, 5, 9, PAL["cyan"]); px(d, 10, 9, PAL["cyan"])
    return upscale(img, 8)


def gen_chests():
    for k in ("mileage", "random", "dungeon"):
        save(make_chest(k), f"Sprites/Chests/chest_{k}.png")


# ---------------------------------------------------------------------------
# 物资图标（食物/水/药/燃料/零件/蓝图）
# ---------------------------------------------------------------------------
def make_item(kind):
    W, H = 16, 16
    img, d = canvas(W, H)
    if kind == "food":
        rect(d, 4, 4, 8, 10, PAL["green"])
        rect(d, 4, 4, 8, 2, PAL["white"])
        rect(d, 6, 7, 4, 2, PAL["dark"])
        rect(d, 6, 10, 4, 2, PAL["dark"])
    elif kind == "water":
        rect(d, 5, 6, 6, 9, PAL["cyan"])
        rect(d, 6, 3, 4, 3, PAL["text"])   # 瓶盖
        rect(d, 6, 8, 4, 3, PAL["white"])
    elif kind == "medicine":
        rect(d, 7, 3, 2, 10, PAL["red"])
        rect(d, 3, 7, 10, 2, PAL["red"])   # 十字
    elif kind == "fuel":
        rect(d, 3, 5, 10, 9, PAL["amber"])
        rect(d, 4, 3, 3, 3, PAL["dark"])   # 注油口
        rect(d, 6, 8, 4, 2, PAL["bg_dark"])
    elif kind == "parts":
        # 齿轮
        cx, cy, r = 8, 8, 5
        for dx in range(-r, r + 1):
            for dy in range(-r, r + 1):
                if dx * dx + dy * dy <= r * r:
                    px(d, cx + dx, cy + dy, PAL["rim"])
        for dx in range(-2, 3):
            for dy in range(-2, 3):
                if dx * dx + dy * dy <= 1:
                    px(d, cx + dx, cy + dy, PAL["dark"])
        # 齿
        for a in range(0, 360, 45):
            import math
            x = cx + int(round(6 * math.cos(math.radians(a))))
            y = cy + int(round(6 * math.sin(math.radians(a))))
            px(d, x, y, PAL["rim"])
    elif kind == "blueprint":
        # 蓝图碎片（菱形 + 线）
        for i in range(8):
            px(d, 8 - i, 4 + i, PAL["cyan"])
            px(d, 8 + i, 4 + i, PAL["cyan"])
        rect(d, 6, 7, 4, 1, PAL["white"])
        rect(d, 7, 6, 1, 4, PAL["white"])
    return upscale(img, 8)


def gen_items():
    for k in ("food", "water", "medicine", "fuel", "parts", "blueprint"):
        save(make_item(k), f"Sprites/Items/item_{k}.png")


# ---------------------------------------------------------------------------
# 天气图标（晴/暴雨/沙暴/极寒）
# ---------------------------------------------------------------------------
def make_weather(kind):
    W, H = 16, 16
    img, d = canvas(W, H)
    if kind == "sunny":
        cx, cy, r = 8, 8, 4
        for dx in range(-r, r + 1):
            for dy in range(-r, r + 1):
                if dx * dx + dy * dy <= r * r:
                    px(d, cx + dx, cy + dy, PAL["amber"])
        for a in range(0, 360, 45):
            import math
            x = cx + int(round(7 * math.cos(math.radians(a))))
            y = cy + int(round(7 * math.sin(math.radians(a))))
            px(d, x, y, PAL["amber"])
    elif kind == "rain":
        rect(d, 3, 4, 10, 5, PAL["text"])    # 云
        rect(d, 4, 3, 8, 2, PAL["white"])
        for i in range(4):
            px(d, 4 + i * 2, 10 + (i % 2), PAL["cyan"])
            px(d, 5 + i * 2, 11 + (i % 2), PAL["cyan"])
    elif kind == "sandstorm":
        # 旋涡
        for r in (2, 4, 6):
            for a in range(0, 360, 30):
                import math
                x = 8 + int(round(r * math.cos(math.radians(a + r * 10))))
                y = 8 + int(round(r * math.sin(math.radians(a + r * 10))))
                px(d, x, y, PAL["amber"])
    elif kind == "blizzard":
        for i in range(6):
            x = 3 + (i * 3) % 11
            y = 3 + (i * 5) % 11
            px(d, x, y, PAL["white"])
            px(d, x + 1, y, PAL["white"])
            px(d, x, y + 1, PAL["white"])
    return upscale(img, 8)


def gen_weather():
    for k in ("sunny", "rain", "sandstorm", "blizzard"):
        save(make_weather(k), f"Sprites/Weather/weather_{k}.png")


# ---------------------------------------------------------------------------
# 区域：可平铺路带 + 远景（城郊/荒漠/冻原）
# ---------------------------------------------------------------------------
def make_road(zone):
    W, H = 32, 16
    img, d = canvas(W, H, opaque=True)
    ground = PAL[f"{zone}_ground"]
    # 路面主体（沥青），车辆在上面行驶
    rect(d, 0, 0, W, H, PAL["asphalt"])
    # 顶缘地平线高光
    rect(d, 0, 0, W, 1, tuple(min(255, c + 30) for c in ground))
    # 底部细路肩/地面
    rect(d, 0, H - 2, W, 2, ground)
    # 中线虚线（水平居中，左右留空保证平铺无缝）
    rect(d, 10, 7, 12, 2, PAL["line"])
    return upscale(img, 8)


def make_bg(zone):
    W, H = 64, 36
    img, d = canvas(W, H, opaque=True)
    sky = PAL[f"{zone}_sky"]
    ground = PAL[f"{zone}_ground"]
    rect(d, 0, 0, W, 22, sky)
    rect(d, 0, 22, W, H - 22, ground)
    if zone == "suburb":
        # 建筑剪影
        for i, (x, w, h) in enumerate([(4, 10, 12), (18, 8, 16), (30, 12, 10), (46, 9, 14)]):
            rect(d, x, 22 - h, w, h, tuple(max(0, c - 30) for c in sky))
            rect(d, x + 2, 22 - h + 2, 2, 2, PAL["amber"])  # 窗灯
    elif zone == "desert":
        # 沙丘
        for x in range(0, W, 16):
            for y in range(0, 8):
                rect(d, x, 20 - y, 14, 2, tuple(min(255, c + 10) for c in ground))
    else:  # tundra
        # 雪山
        for i, (x, h) in enumerate([(6, 14), (26, 18), (46, 12)]):
            for yy in range(h):
                ww = yy + 2
                rect(d, x - ww // 2, 22 - yy, ww, 1, PAL["white"])
    rect(d, 0, 22, W, 1, tuple(min(255, c + 25) for c in ground))
    return upscale(img, 6)


def gen_zones():
    for z in ("suburb", "desert", "tundra"):
        save(make_road(z), f"Sprites/Zones/road_{z}.png")
        save(make_bg(z), f"Sprites/Zones/bg_{z}.png")


# ---------------------------------------------------------------------------
# 调色板图 + 文本说明
# ---------------------------------------------------------------------------
def gen_palette():
    names = ["bg_dark", "bg_panel", "cyan", "amber", "green", "red",
             "text", "white", "suburb_ground", "desert_ground", "tundra_ground"]
    cols = 4
    cell = 32
    W = cols * cell
    H = ((len(names) + cols - 1) // cols) * cell
    img, d = canvas(W, H, opaque=True)
    rect(d, 0, 0, W, H, PAL["bg_dark"])
    for i, n in enumerate(names):
        x = (i % cols) * cell
        y = (i // cols) * cell
        rect(d, x + 2, y + 2, cell - 4, cell - 4, PAL[n])
        rect(d, x + 2, y + 2, cell - 4, 1, PAL["white"])
    save(img, "Palette/palette.png")

    with open(os.path.join(ART, "Palette", "palette.txt"), "w", encoding="utf-8") as f:
        f.write("# Desktop Doomsday 调色板（对齐 SPEC 第 9 节）\n")
        f.write("# 同语义同色值；区域/天气靠色调切换，不换精灵。\n\n")
        for n, c in PAL.items():
            f.write(f"{n:16s} = #{c[0]:02X}{c[1]:02X}{c[2]:02X}   rgb{c}\n")
    print("  wrote Palette/palette.txt")


# ---------------------------------------------------------------------------
def main():
    print("Desktop Doomsday — 生成像素美术 ...")
    gen_vehicles()
    gen_survivor()
    gen_chests()
    gen_items()
    gen_weather()
    gen_zones()
    gen_palette()
    print("完成。输出目录：", ART)


if __name__ == "__main__":
    main()
