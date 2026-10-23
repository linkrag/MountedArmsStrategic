# Texture generator for Mounted Arms: Strategic Missiles (same style as Mounted Arms' gen_tex.py).
# Usage: python tools/gen_tex.py 1.6/Textures/MountedArmsStrategic
import math, sys, os
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1]
S = 4                 # supersampling
N = 256               # final texture size
W = N * S

BLACK  = (24, 26, 29, 255)
STEEL  = (96, 103, 110, 255)
STEEL_L= (142, 150, 157, 255)
STEEL_D= (58, 63, 69, 255)
GUN    = (52, 57, 62, 255)
OLIVE  = (82, 94, 62, 255)
OLIVE_L= (112, 126, 84, 255)
OLIVE_D= (54, 63, 41, 255)
YELLOW = (214, 178, 40, 255)
RED    = (150, 48, 40, 255)
WHITE  = (214, 218, 220, 255)
LW = int(2.0 * S)

def canvas():
    return Image.new("RGBA", (W, W), (0, 0, 0, 0))

def sc(v):
    return [int(round(x * S)) for x in v]

def rect(d, x0, y0, x1, y1, fill, r=3, outline=BLACK, hl=True):
    d.rounded_rectangle(sc((x0, y0, x1, y1)), radius=int(r * S), fill=fill, outline=outline, width=LW)
    if hl and (x1 - x0) > 8:
        lx0 = x0 + 2.2; lx1 = min(x0 + 2.2 + max(1.5, (x1 - x0) * 0.16), x1 - 2.2)
        c = tuple(min(255, int(v * 1.3)) for v in fill[:3]) + (255,)
        d.rounded_rectangle(sc((lx0, y0 + 2.4, lx1, y1 - 2.4)), radius=int(1.2 * S), fill=c)

def circ(d, cx, cy, r, fill, outline=BLACK, w=LW):
    d.ellipse(sc((cx - r, cy - r, cx + r, cy + r)), fill=fill, outline=outline, width=w)

def line(d, p0, p1, width, fill):
    d.line(sc((p0[0], p0[1], p1[0], p1[1])), fill=fill, width=int(width * S))

def poly(d, pts, fill, outline=BLACK):
    d.polygon([tuple(sc(p)) for p in pts], fill=fill)
    d.line([tuple(sc(p)) for p in pts] + [tuple(sc(pts[0]))], fill=outline, width=LW, joint="curve")

def rivets(d, pts, r=2.6):
    for (x, y) in pts:
        circ(d, x, y, r, STEEL_L, outline=BLACK, w=int(1.2 * S))

def shadow(img, offset=(5, 6), blur=7, alpha=120):
    a = img.split()[3]
    sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
    black = Image.new("RGBA", img.size, (0, 0, 0, alpha))
    sh.paste(black, (offset[0] * S, offset[1] * S), a)
    sh = sh.filter(ImageFilter.GaussianBlur(blur * S / 2))
    sh.alpha_composite(img)
    return sh

def finish(img, path):
    img.resize((N, N), Image.LANCZOS).save(path)

# ------------------------------------------------------------------ pieces
def missile(d, cx, y_nose, y_tail, r, body=WHITE, band=OLIVE):
    """Missile seen from above, nose UP."""
    poly(d, [(cx, y_nose), (cx + r, y_nose + r * 2.6), (cx - r, y_nose + r * 2.6)], body)
    rect(d, cx - r, y_nose + r * 2.4, cx + r, y_tail, body, r=2)
    rect(d, cx - r, y_nose + r * 3.2, cx + r, y_nose + r * 4.6, band, r=1, hl=False)
    # tail fins
    poly(d, [(cx - r, y_tail - r * 3), (cx - r * 2.2, y_tail + 2), (cx - r, y_tail)], STEEL_D)
    poly(d, [(cx + r, y_tail - r * 3), (cx + r * 2.2, y_tail + 2), (cx + r, y_tail)], STEEL_D)
    circ(d, cx, y_tail + 1, r * 0.6, BLACK)

# ------------------------------------------------------------------ launcher
def base_launcher():
    img = canvas(); d = ImageDraw.Draw(img)
    # concrete pad with hazard border
    rect(d, 14, 14, 242, 242, STEEL_D, r=10, hl=False)
    for i in range(16):
        x = 14 + i * 14.25
        poly(d, [(x, 14), (x + 7, 14), (x + 1, 22), (x - 6, 22)], YELLOW if i % 2 == 0 else BLACK, outline=BLACK)
        poly(d, [(x, 234), (x + 7, 234), (x + 1, 242), (x - 6, 242)], YELLOW if i % 2 == 0 else BLACK, outline=BLACK)
    rect(d, 26, 26, 230, 230, STEEL, r=8, hl=False)
    rivets(d, [(36, 36), (220, 36), (36, 220), (220, 220), (128, 36), (36, 128), (220, 128)])
    # turntable
    circ(d, 128, 128, 62, STEEL_D); circ(d, 128, 128, 52, GUN); circ(d, 128, 128, 24, STEEL_D)
    # outriggers / jacks
    for (x, y) in ((40, 60), (216, 60), (40, 196), (216, 196)):
        rect(d, x - 12, y - 12, x + 12, y + 12, OLIVE_D, r=3)
        circ(d, x, y, 5, BLACK)
    # control cabin at the operator side (south)
    rect(d, 96, 196, 160, 228, OLIVE, r=5)
    rect(d, 104, 202, 152, 214, (30, 60, 80, 255), r=2, hl=False)
    return shadow(img)

def top_launcher(loaded=2):
    img = canvas(); d = ImageDraw.Draw(img)
    # erector frame (points UP) with two rails; draws `loaded` missiles (left rail fires last)
    rect(d, 92, 30, 164, 214, OLIVE_D, r=6)
    for x in (110, 146):
        rect(d, x - 6, 34, x + 6, 210, GUN, r=2, hl=False)   # empty rail
    for y in (70, 120, 170):
        line(d, (92, y), (164, y), 3, BLACK)
    if loaded >= 1:
        missile(d, 110, 18, 206, 11)
    if loaded >= 2:
        missile(d, 146, 18, 206, 11)
    # hydraulic arm and pivot
    rect(d, 120, 150, 136, 230, STEEL, r=3)
    circ(d, 128, 128, 14, STEEL_D)
    return img

def icon(base, top, k):
    n = int(W * k)
    big = top.resize((n, n), Image.LANCZOS)
    layer = canvas(); layer.paste(big, ((W - n) // 2, (W - n) // 2), big)
    a = base.copy(); a.alpha_composite(layer)
    return a

# ------------------------------------------------------------------ missile item / projectile
def missile_item():
    img = canvas(); d = ImageDraw.Draw(img)
    # transport cradle + missile lying diagonally
    rect(d, 40, 180, 216, 200, OLIVE_D, r=4)
    big = canvas(); bd = ImageDraw.Draw(big)
    missile(bd, 128, 20, 236, 16)
    big = big.rotate(-45, resample=Image.BICUBIC)
    img.alpha_composite(big)
    return shadow(img, (3, 4), 4, 100)

def missile_projectile():
    img = canvas(); d = ImageDraw.Draw(img)
    missile(d, 128, 8, 246, 22)
    return img

os.makedirs(OUT, exist_ok=True)
b = base_launcher()
finish(b, os.path.join(OUT, "TacticalLauncher_Base.png"))
# one top per loaded count (LoadedTopsExtension); turret tops point EAST
for n in (0, 1, 2):
    tn = top_launcher(n).rotate(-90, resample=Image.BICUBIC)
    finish(shadow(tn, (2, 3), 4, 90), os.path.join(OUT, f"TacticalLauncher_Top{n}.png"))
    if n == 2:
        t = tn
finish(icon(b, t, 3.4 / 3.0), os.path.join(OUT, "TacticalLauncher_Icon.png"))
finish(missile_item(), os.path.join(OUT, "TacticalMissile_Item.png"))
finish(missile_projectile(), os.path.join(OUT, "TacticalMissile_Projectile.png"))
print("ok")
