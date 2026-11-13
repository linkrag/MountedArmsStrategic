"""Generate the Steam Workshop / mod-menu preview image for Mounted Arms: Strategic.

Output: About/Preview.png (1280x720 RGB PNG, < 1 MB), built from the mod's own
textures in 1.6/Textures/MountedArmsStrategic over a dark "radar / world map"
background with a ballistic arc ending in a target reticle.

Run from the mod folder:
    python tools/gen_preview.py

Deterministic (no randomness). Needs Python 3 + Pillow and the Windows fonts
Bahnschrift (fallback Segoe UI). Paths are relative to the mod folder.
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

MOD = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TEX = os.path.join(MOD, "1.6", "Textures", "MountedArmsStrategic")
OUT = os.path.join(MOD, "About", "Preview.png")

W, H = 1280, 720
S = 2                      # supersampling factor; everything is drawn at 2x
ACCENT = (0xD2, 0x45, 0x3A)
TITLE = "MOUNTED ARMS: STRATEGIC"
TAGLINE = "Ballistic missiles, artillery & missile defense  |  Requires Combat Extended"
BADGE = "RimWorld 1.6 \u00b7 1.7"

HEADER_H = 132
ACCENT_H = 6
FOOTER_H = 64
BAND = (0x15, 0x17, 0x1A)


def s(v):
    return int(round(v * S))


# ---------------------------------------------------------------- fonts
def font(size, weight):
    try:
        f = ImageFont.truetype(r"C:\Windows\Fonts\bahnschrift.ttf", s(size))
        try:
            f.set_variation_by_name(weight)
        except Exception:
            pass
        return f
    except OSError:
        name = "segoeuib.ttf" if weight in ("Bold", "SemiBold") else "segoeui.ttf"
        return ImageFont.truetype(os.path.join(r"C:\Windows\Fonts", name), s(size))


# ---------------------------------------------------------------- sprites
def tex(name):
    return Image.open(os.path.join(TEX, name + ".png")).convert("RGBA")


def building(base, top):
    im = tex(base)
    im.alpha_composite(tex(top))
    return im


def fit(im, size):
    """Crop to alpha bbox and scale (LANCZOS) so the longer side == size (in 1x px)."""
    im = im.crop(im.getchannel("A").getbbox())
    k = s(size) / max(im.size)
    return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)


def paste_shadow(canvas, im, cx, cy, off=(8, 10), blur=10, alpha=0.45):
    x, y = s(cx) - im.width // 2, s(cy) - im.height // 2
    a = im.getchannel("A").point(lambda v: int(v * alpha))
    pad = s(blur) * 3
    sh = Image.new("L", (im.width + pad * 2, im.height + pad * 2), 0)
    sh.paste(a, (pad, pad))
    sh = sh.filter(ImageFilter.GaussianBlur(s(blur)))
    black = Image.new("RGBA", sh.size, (0, 0, 0, 255))
    black.putalpha(sh)
    canvas.alpha_composite(black, (x - pad + s(off[0]), y - pad + s(off[1])))
    canvas.alpha_composite(im, (x, y))


def glow_layer(size, draw_fn, blur):
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    draw_fn(ImageDraw.Draw(layer))
    return layer.filter(ImageFilter.GaussianBlur(s(blur)))


# ---------------------------------------------------------------- geometry
P0 = (205, 505)            # launch point (silo)
P2 = (1125, 400)           # target
APEX_Y = -70               # control-point height of the quadratic arc
P1 = ((P0[0] + P2[0]) / 2, APEX_Y)


def arc(t):
    u = 1 - t
    x = u * u * P0[0] + 2 * u * t * P1[0] + t * t * P2[0]
    y = u * u * P0[1] + 2 * u * t * P1[1] + t * t * P2[1]
    return x, y


def arc_tan(t):
    dx = 2 * (1 - t) * (P1[0] - P0[0]) + 2 * t * (P2[0] - P1[0])
    dy = 2 * (1 - t) * (P1[1] - P0[1]) + 2 * t * (P2[1] - P1[1])
    return dx, dy


# ---------------------------------------------------------------- background
def background():
    bg = Image.new("RGBA", (s(W), s(H)), (0, 0, 0, 255))
    # radial gradient, a bit lighter towards the centre-right
    small = Image.new("RGB", (W // 8, H // 8))
    px = small.load()
    cx, cy = W * 0.55 / 8, H * 0.52 / 8
    for y in range(small.height):
        for x in range(small.width):
            d = math.hypot((x - cx) / (W / 8), (y - cy) / (H / 8))
            k = max(0.0, 1 - d * 1.35)
            px[x, y] = (int(7 + 12 * k), int(12 + 20 * k), int(22 + 30 * k))
    bg.paste(small.resize(bg.size, Image.BICUBIC))

    d = ImageDraw.Draw(bg, "RGBA")
    grid = (70, 120, 170, 26)
    # latitude lines (gently curved, like a world projection)
    for i in range(-1, 12):
        y0 = 150 + i * 52
        pts = [(s(x), s(y0 + 10 * math.cos((x - W / 2) / W * math.pi))) for x in range(0, W + 20, 20)]
        d.line(pts, fill=grid, width=s(1))
    # meridians, bowed outwards from the centre
    for i in range(-12, 13):
        x0 = W / 2 + i * 62
        bow = (x0 - W / 2) * 0.10
        pts = [(s(x0 + bow * math.sin((y - 138) / (H - 200) * math.pi)), s(y)) for y in range(138, H + 20, 20)]
        d.line(pts, fill=grid, width=s(1))

    # friendly radar rings around the launch site (cool blue vs. red target rings)
    for r, a in ((160, 40), (290, 28), (430, 18)):
        d.ellipse([s(P0[0] - r), s(P0[1] - r), s(P0[0] + r), s(P0[1] + r)],
                  outline=(90, 170, 230, a), width=s(1.5))

    # radar rings around the target
    for r, a in ((70, 55), (130, 38), (200, 26), (285, 16), (385, 10)):
        d.ellipse([s(P2[0] - r), s(P2[1] - r), s(P2[0] + r), s(P2[1] + r)],
                  outline=(ACCENT[0], ACCENT[1], ACCENT[2], a), width=s(1.5))
    return bg


# ---------------------------------------------------------------- trajectory
def trajectory(canvas, t_missile):
    size = canvas.size
    # flown part: soft exhaust trail
    n = 160
    flown = [arc(t_missile * i / n) for i in range(n + 1)]

    def seg_line(dr, i, col, w):
        dr.line([(s(flown[i][0]), s(flown[i][1])), (s(flown[i + 1][0]), s(flown[i + 1][1]))],
                fill=col, width=s(w), joint="curve")

    def smoke(dr):              # billowing smoke puffs, older (lower) ones larger
        for i in range(0, n, 4):
            k = i / n
            x, y = flown[i]
            r = 16 + 34 * (1 - k) + 6 * math.sin(i * 1.7)
            ox = 8 * math.sin(i * 0.9) * (1 - k)
            dr.ellipse([s(x + ox - r), s(y - r), s(x + ox + r), s(y + r)],
                       fill=(165, 172, 185, int(60 + 50 * (1 - k))))

    def fire(dr):               # hot core, brightest next to the missile
        for i in range(n):
            k = i / n
            seg_line(dr, i, (255, int(140 + 90 * k), int(60 + 100 * k), int(30 + 200 * k * k)), 4 + 12 * k)
    canvas.alpha_composite(glow_layer(size, smoke, 7))
    canvas.alpha_composite(glow_layer(size, fire, 8))
    canvas.alpha_composite(glow_layer(size, fire, 2))

    # remaining part: dashed predicted path in accent colour
    def dashes(dr, width, alpha):
        t, seg, gap = t_missile + 0.07, 0.018, 0.012
        while t < 0.985:
            t2 = min(t + seg, 0.985)
            pts = [arc(t + (t2 - t) * j / 6) for j in range(7)]
            dr.line([(s(x), s(y)) for x, y in pts], fill=ACCENT + (alpha,), width=s(width))
            t = t2 + gap
    canvas.alpha_composite(glow_layer(size, lambda dr: dashes(dr, 9, 150), 6))
    dr = ImageDraw.Draw(canvas)
    dashes(dr, 4, 255)


def reticle(canvas):
    x, y = P2
    glow = glow_layer(canvas.size, lambda dr: dr.ellipse(
        [s(x - 60), s(y - 60), s(x + 60), s(y + 60)], outline=ACCENT + (200,), width=s(10)), 10)
    canvas.alpha_composite(glow)
    d = ImageDraw.Draw(canvas)
    col = (240, 96, 84, 255)
    for r, w in ((54, 5), (24, 3)):
        d.ellipse([s(x - r), s(y - r), s(x + r), s(y + r)], outline=col, width=s(w))
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        d.line([s(x + dx * 32), s(y + dy * 32), s(x + dx * 80), s(y + dy * 80)], fill=col, width=s(5))
    d.ellipse([s(x - 6), s(y - 6), s(x + 6), s(y + 6)], fill=col)


# ---------------------------------------------------------------- bands
def bands(canvas):
    d = ImageDraw.Draw(canvas, "RGBA")
    d.rectangle([0, 0, s(W), s(HEADER_H)], fill=BAND + (255,))
    d.rectangle([0, s(HEADER_H), s(W), s(HEADER_H + ACCENT_H)], fill=ACCENT + (255,))
    d.rectangle([0, s(H - FOOTER_H), s(W), s(H)], fill=BAND + (224,))

    ft = font(86, "Bold")
    tb = d.textbbox((0, 0), TITLE, font=ft)
    ty = s(HEADER_H) // 2 - (tb[1] + tb[3]) // 2
    d.text((s(48), ty), TITLE, font=ft, fill=(0xF2, 0xF2, 0xF2))

    fy = H - FOOTER_H / 2
    fs = font(28, "SemiBold")
    sb = d.textbbox((0, 0), TAGLINE, font=fs)
    d.text((s(48), s(fy) - (sb[1] + sb[3]) // 2), TAGLINE, font=fs, fill=(0xD8, 0xD8, 0xD8))

    fb = font(24, "SemiBold")
    bb = d.textbbox((0, 0), BADGE, font=fb)
    bw, bh = bb[2] - bb[0], bb[3] - bb[1]
    px, py = s(18), s(9)
    x1 = s(W - 48)
    x0 = x1 - bw - 2 * px
    y0 = s(fy) - bh // 2 - py
    y1 = s(fy) + bh // 2 + py
    d.rounded_rectangle([x0, y0, x1, y1], radius=(y1 - y0) // 2, outline=ACCENT + (255,), width=s(2.5))
    d.text((x0 + px - bb[0], s(fy) - (bb[1] + bb[3]) // 2), BADGE, font=fb, fill=(0xF2, 0xF2, 0xF2))
    return sb[2] - sb[0], x0


# ---------------------------------------------------------------- main
def main():
    canvas = background()

    t_missile = 0.34
    reticle(canvas)

    # launch silo (big, left) - the arc starts from its hatch
    silo = fit(building("ICBMSilo_Base", "ICBMSilo_Top0"), 250)
    paste_shadow(canvas, silo, P0[0], P0[1] + 10)
    trajectory(canvas, t_missile)

    # faint artillery / rocket arcs from the MLRS and the M777 towards the target
    def lob(a, b, h):
        c = ((a[0] + b[0]) / 2, min(a[1], b[1]) - h)
        return lambda t: ((1 - t) ** 2 * a[0] + 2 * (1 - t) * t * c[0] + t * t * b[0],
                          (1 - t) ** 2 * a[1] + 2 * (1 - t) * t * c[1] + t * t * b[1])
    d = ImageDraw.Draw(canvas, "RGBA")
    lobs = [lob((575, 545), (1080, 432), 300)]
    for f in lobs:
        for i in range(5, 43):
            x, y = f(i / 50)
            x2, y2 = f((i + 0.5) / 50)
            d.line([s(x), s(y), s(x2), s(y2)], fill=(225, 230, 240, 110), width=s(2.5))
    # one rocket riding the MLRS arc
    rt = 0.42
    rx, ry = lobs[0](rt)
    ex, ey = lobs[0](rt + 0.01)
    rang = math.degrees(math.atan2(-(ey - ry), ex - rx))
    rocket = fit(tex("TacticalMissile_Projectile"), 96).rotate(rang - 90, resample=Image.BICUBIC, expand=True)
    paste_shadow(canvas, rocket, rx, ry, off=(8, 10), blur=6, alpha=0.4)

    # row of supporting launchers
    row = [
        (building("TacticalLauncher_Base", "TacticalLauncher_Top2"), 150),
        (building("MLRS_Base", "MLRS_Top6"), 140),
        (building("M777_Base", "M777_Top"), 150),
        (building("CruiseVLS_Base", "CruiseVLS_Top4"), 130),
        (building("Interceptor_Base", "Interceptor_Top4"), 140),
    ]
    xs = [420, 575, 730, 880, 1025]
    for (im, sz), x in zip(row, xs):
        paste_shadow(canvas, fit(im, sz), x, 572, blur=8)

    # hero missile, aligned to the arc tangent, with exhaust flame
    mx, my = arc(t_missile)
    dx, dy = arc_tan(t_missile)
    ang = math.degrees(math.atan2(-dy, dx))          # screen -> math angle
    flame = Image.new("RGBA", (s(120), s(300)), (0, 0, 0, 0))
    fd = ImageDraw.Draw(flame)
    fd.ellipse([s(25), s(0), s(95), s(300)], fill=(255, 140, 50, 170))
    fd.ellipse([s(40), s(0), s(80), s(180)], fill=(255, 220, 140, 230))
    fd.ellipse([s(50), s(0), s(70), s(90)], fill=(255, 255, 240, 255))
    flame = flame.filter(ImageFilter.GaussianBlur(s(9)))
    missile = fit(tex("ICBM_Projectile"), 330)
    # stack missile above the flame (nose up), then rotate as one sprite
    combo = Image.new("RGBA", (max(missile.width, flame.width), missile.height + flame.height - s(40)), (0, 0, 0, 0))
    combo.alpha_composite(flame, ((combo.width - flame.width) // 2, missile.height - s(40)))
    combo.alpha_composite(missile, ((combo.width - missile.width) // 2, 0))
    # rotate around the missile body's centre
    pivot_y = missile.height // 2
    padded = Image.new("RGBA", (combo.width, 2 * max(pivot_y, combo.height - pivot_y)), (0, 0, 0, 0))
    padded.alpha_composite(combo, (0, padded.height // 2 - pivot_y))
    rot = padded.rotate(ang - 90, resample=Image.BICUBIC, expand=True)
    paste_shadow(canvas, rot, mx, my, off=(14, 18), blur=12, alpha=0.4)

    bands(canvas)

    out = canvas.convert("RGB").resize((W, H), Image.LANCZOS)
    out.save(OUT, optimize=True)
    if os.path.getsize(OUT) > 1_000_000:
        out.quantize(colors=256, method=Image.MEDIANCUT, dither=Image.FLOYDSTEINBERG).save(OUT, optimize=True)
    chk = Image.open(OUT)
    print(OUT, chk.size, chk.mode, os.path.getsize(OUT), "bytes")


if __name__ == "__main__":
    main()
