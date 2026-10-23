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

# ------------------------------------------------------------------ ICBM silo (top-down, round: turret rotation must not show)
def base_silo():
    img = canvas(); d = ImageDraw.Draw(img)
    rect(d, 10, 10, 246, 246, STEEL_D, r=8, hl=False)          # concrete apron
    rect(d, 20, 20, 236, 236, (120, 122, 118, 255), r=6, hl=False)
    for (x, y) in ((34, 34), (222, 34), (34, 222), (222, 222)):
        circ(d, x, y, 6, STEEL_D)                              # anchor bolts
    rect(d, 96, 208, 160, 234, OLIVE, r=4)                     # service hatch
    circ(d, 128, 128, 96, BLACK)                               # silo ring
    circ(d, 128, 128, 90, YELLOW, outline=BLACK)
    for i in range(16):                                        # hazard ring
        if i % 2 == 0:
            d.pieslice(sc((38, 38, 218, 218)), i * 22.5, i * 22.5 + 22.5, fill=BLACK)
    circ(d, 128, 128, 80, STEEL_D)
    return shadow(img, (3, 4), 5, 100)

def top_silo(loaded):
    img = canvas(); d = ImageDraw.Draw(img)
    circ(d, 128, 128, 78, STEEL)                               # hatch rim
    circ(d, 128, 128, 66, BLACK)                               # open tube
    if loaded:
        circ(d, 128, 128, 52, WHITE)                           # missile body seen from above
        circ(d, 128, 128, 40, (190, 194, 196, 255), outline=BLACK, w=int(1.2 * S))
        circ(d, 128, 128, 22, OLIVE, outline=BLACK, w=int(1.2 * S))   # nose band
        circ(d, 128, 128, 8, WHITE)                            # nose tip
    else:
        circ(d, 128, 128, 52, (16, 17, 19, 255), outline=(16, 17, 19, 255))
        for k in range(4):                                     # guide rails inside the tube
            a = math.radians(45 + 90 * k)
            line(d, (128 + 50 * math.cos(a), 128 + 50 * math.sin(a)), (128 + 62 * math.cos(a), 128 + 62 * math.sin(a)), 4, STEEL_D)
    return img

def icbm_item():
    img = canvas(); d = ImageDraw.Draw(img)
    rect(d, 30, 190, 226, 208, OLIVE_D, r=4)
    big = canvas(); bd = ImageDraw.Draw(big)
    missile(bd, 128, 6, 248, 18, band=RED)
    big = big.rotate(-45, resample=Image.BICUBIC)
    img.alpha_composite(big)
    return shadow(img, (3, 4), 4, 100)

def icbm_projectile():
    img = canvas(); d = ImageDraw.Draw(img)
    missile(d, 128, 4, 248, 26, band=RED)
    return img

# ------------------------------------------------------------------ missile defense battery (Patriot-like)
def base_interceptor():
    img = canvas(); d = ImageDraw.Draw(img)
    # trailer bed with four outriggers and a radar/generator box at the operator side
    rect(d, 46, 26, 210, 230, OLIVE_D, r=8)
    rect(d, 54, 34, 202, 222, OLIVE, r=6, hl=False)
    for (x, y) in ((30, 46), (226, 46), (30, 210), (226, 210)):
        line(d, (128, 128), (x, y), 8, BLACK); line(d, (128, 128), (x, y), 5, STEEL_D)
        rect(d, x - 10, y - 10, x + 10, y + 10, STEEL_D, r=3); circ(d, x, y, 4, BLACK)
    circ(d, 128, 128, 46, STEEL_D); circ(d, 128, 128, 36, GUN); circ(d, 128, 128, 16, STEEL_D)
    rect(d, 92, 196, 164, 226, STEEL, r=4)
    for x in range(100, 160, 8):
        line(d, (x, 202), (x, 220), 2, STEEL_D)                # radiator fins
    return shadow(img)

def top_interceptor(loaded):
    img = canvas(); d = ImageDraw.Draw(img)
    # 2x2 block of square canisters, front (muzzles) UP; fire order: front-left, front-right, rear-left, rear-right
    rect(d, 70, 40, 186, 214, OLIVE_D, r=6)
    cells = [(76, 46), (130, 46), (76, 130), (130, 130)]
    for i, (x, y) in enumerate(cells):
        rect(d, x, y, x + 50, y + 78, OLIVE, r=3)
        if i < loaded:
            rect(d, x + 6, y + 6, x + 44, y + 40, WHITE, r=2, hl=False)          # frangible cover intact
            line(d, (x + 6, y + 23), (x + 44, y + 23), 1.5, STEEL_D)
        else:
            rect(d, x + 6, y + 6, x + 44, y + 40, BLACK, r=2, hl=False)          # empty, cover blown
    rect(d, 110, 200, 146, 236, STEEL, r=4)                                       # elevation ram
    circ(d, 128, 128, 10, STEEL_D)
    return img

def interceptor_item():
    img = canvas(); d = ImageDraw.Draw(img)
    rect(d, 60, 40, 196, 220, OLIVE_D, r=6)
    rect(d, 70, 50, 186, 210, OLIVE, r=4, hl=False)
    rect(d, 80, 58, 176, 100, WHITE, r=3, hl=False)
    for y in (130, 170):
        line(d, (70, y), (186, y), 3, BLACK)
    return shadow(img, (3, 4), 4, 100)

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
sb = base_silo()
finish(sb, os.path.join(OUT, "ICBMSilo_Base.png"))
for n in (0, 1):
    finish(top_silo(n), os.path.join(OUT, f"ICBMSilo_Top{n}.png"))
finish(icon(sb, top_silo(1), 2.6 / 3.0), os.path.join(OUT, "ICBMSilo_Icon.png"))
finish(icbm_item(), os.path.join(OUT, "ICBM_Item.png"))
finish(icbm_projectile(), os.path.join(OUT, "ICBM_Projectile.png"))
ib = base_interceptor()
finish(ib, os.path.join(OUT, "Interceptor_Base.png"))
for n in range(5):
    tn = top_interceptor(n).rotate(-90, resample=Image.BICUBIC)   # turret tops point EAST
    finish(shadow(tn, (2, 3), 4, 90), os.path.join(OUT, f"Interceptor_Top{n}.png"))
    if n == 4:
        finish(icon(ib, tn, 2.6 / 3.0), os.path.join(OUT, "Interceptor_Icon.png"))
finish(interceptor_item(), os.path.join(OUT, "Interceptor_Item.png"))
print("ok")
