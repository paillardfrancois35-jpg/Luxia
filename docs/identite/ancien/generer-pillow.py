"""Identité visuelle de LuXia : icône et plaquette (logo).

Concept : le X de LuXia est formé de deux faisceaux qui se croisent — l'un « lumière » (chaud, continu, issu d'un
projecteur), l'autre « IA » (froid, fait de pixels, issu d'une puce) ; un point de lumière blanche naît à leur
intersection. « Lu » (lumière) en tons chauds, « ia » (intelligence artificielle) en tons froids.

Usage : python generer.py   (produit tous les fichiers dans ./sortie)
"""
import math
import os

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "sortie")
os.makedirs(OUT, exist_ok=True)

BG_TOP = (22, 27, 38)
BG_BOTTOM = (8, 10, 16)
WARM = [(255, 236, 170), (255, 170, 60), (255, 110, 40)]      # lumière : blanc chaud → ambre → orangé
COOL = [(150, 245, 255), (70, 190, 255), (140, 90, 255)]      # IA : cyan clair → bleu → violet
MARGIN = 0.17


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def ramp(colors, t):
    t = max(0.0, min(1.0, t))
    seg = t * (len(colors) - 1)
    i = min(int(seg), len(colors) - 2)
    return lerp(colors[i], colors[i + 1], seg - i)


def axis_gradient(size, p0, p1, colors, a0, a1):
    """Image RGBA : dégradé de couleur et d'opacité le long de l'axe p0 → p1."""
    w, h = size
    img = Image.new("RGBA", size)
    px = img.load()
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    L2 = dx * dx + dy * dy
    step = 2
    for y in range(0, h, step):
        for x in range(0, w, step):
            t = ((x - p0[0]) * dx + (y - p0[1]) * dy) / L2
            t = max(0.0, min(1.0, t))
            c = ramp(colors, t)
            a = int(a0 + (a1 - a0) * t)
            for yy in range(y, min(y + step, h)):
                for xx in range(x, min(x + step, w)):
                    px[xx, yy] = (*c, a)
    return img


def cone(p0, p1, w0, w1):
    """Polygone d'un faisceau : largeur w0 à la source p0, w1 à l'extrémité p1."""
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    L = math.hypot(dx, dy)
    nx, ny = -dy / L, dx / L
    return [(p0[0] + nx * w0 / 2, p0[1] + ny * w0 / 2), (p1[0] + nx * w1 / 2, p1[1] + ny * w1 / 2),
            (p1[0] - nx * w1 / 2, p1[1] - ny * w1 / 2), (p0[0] - nx * w0 / 2, p0[1] - ny * w0 / 2)]


def beam(size, p0, p1, w0, w1, colors, a0, a1, pixels=0):
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).polygon(cone(p0, p1, w0, w1), fill=255)
    if pixels:
        # Faisceau « IA » : fait de pixels carrés (numérique), plus serrés près de la source.
        grid = Image.new("L", size, 0)
        d = ImageDraw.Draw(grid)
        cell = pixels
        gap = max(1, int(cell * 0.22))
        for y in range(0, size[1], cell):
            for x in range(0, size[0], cell):
                d.rectangle([x + gap, y + gap, x + cell - gap, y + cell - gap], fill=255)
        mask = ImageChops.multiply(mask, grid)
    grad = axis_gradient(size, p0, p1, colors, a0, a1)
    alpha = ImageChops.multiply(grad.getchannel("A"), mask)
    grad.putalpha(alpha)
    return grad


def radial(size, center, radius, color, alpha):
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    steps = 60
    for i in range(steps, 0, -1):
        r = radius * i / steps
        a = int(alpha * (1 - i / steps) ** 2)
        d.ellipse([center[0] - r, center[1] - r, center[0] + r, center[1] + r], fill=(*color, a))
    return img


def add_glow(base, layer, blur):
    glow = layer.filter(ImageFilter.GaussianBlur(blur))
    base.alpha_composite(glow)
    base.alpha_composite(layer)


def draw_mark(S, simple=False, background=True):
    """Le X lumineux sur un carré arrondi (icône). S : taille de travail en pixels."""
    size = (S, S)
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    if background:
        bg = Image.new("RGBA", size)
        d = ImageDraw.Draw(bg)
        for y in range(S):
            d.line([(0, y), (S, y)], fill=(*lerp(BG_TOP, BG_BOTTOM, y / S), 255))
        mask = Image.new("L", size, 0)
        ImageDraw.Draw(mask).rounded_rectangle([0, 0, S - 1, S - 1], radius=int(S * 0.22), fill=255)
        img.paste(bg, (0, 0), mask)
    else:
        mask = Image.new("L", size, 255)

    m = S * MARGIN
    a0, a1 = (m, m), (S - m, S - m)       # lumière : haut gauche → bas droite (symétrique : croisement au centre)
    b0, b1 = (S - m, m), (m, S - m)       # IA : haut droite → bas gauche
    wide = S * (0.20 if not simple else 0.26)
    narrow = S * 0.03
    fade = 70 if not simple else 150

    layers = Image.new("RGBA", size, (0, 0, 0, 0))
    la = beam(size, a0, a1, narrow, wide, WARM, 255, fade)
    lb = beam(size, b0, b1, narrow, wide, COOL, 255, fade, pixels=0 if simple else max(4, int(S * 0.022)))
    layers.alpha_composite(la)
    # Croisement : les deux faisceaux s'additionnent (lumière + IA → blanc).
    layers = Image.alpha_composite(layers, lb)
    add = ImageChops.add(la, lb)
    add.putalpha(ImageChops.multiply(la.getchannel("A"), lb.getchannel("A")))
    layers.alpha_composite(add)

    add_glow(img, layers, S * 0.025)

    c = (S / 2, S * 0.50)
    img.alpha_composite(radial(size, c, S * 0.30, (255, 255, 255), 210))
    img.alpha_composite(radial(size, c, S * 0.10, (255, 255, 255), 255))

    # Sources : un projecteur (lumière) et une puce (IA).
    d = ImageDraw.Draw(img)
    r = S * 0.052
    d.ellipse([a0[0] - r, a0[1] - r, a0[0] + r, a0[1] + r], fill=(255, 214, 140, 255), outline=(255, 245, 220, 255), width=max(1, int(S * 0.008)))
    q = S * 0.05
    d.rounded_rectangle([b0[0] - q, b0[1] - q, b0[0] + q, b0[1] + q], radius=int(q * 0.3), fill=(90, 200, 255, 255), outline=(200, 250, 255, 255), width=max(1, int(S * 0.008)))
    if not simple:
        for k in (-1, 0, 1):   # pattes de la puce
            for side in (-1, 1):
                x = b0[0] + k * q * 0.55
                d.line([(x, b0[1] + side * q), (x, b0[1] + side * q * 1.45)], fill=(160, 235, 255, 255), width=max(1, int(S * 0.008)))
                y = b0[1] + k * q * 0.55
                d.line([(b0[0] + side * q, y), (b0[0] + side * q * 1.45, y)], fill=(160, 235, 255, 255), width=max(1, int(S * 0.008)))

    out = Image.new("RGBA", size, (0, 0, 0, 0))
    out.paste(img, (0, 0), mask)
    return out


def icon_files():
    big = draw_mark(2048)
    big.resize((1024, 1024), Image.LANCZOS).save(os.path.join(OUT, "luxia-icone-1024.png"))
    big.resize((512, 512), Image.LANCZOS).save(os.path.join(OUT, "luxia-icone-512.png"))
    simple = draw_mark(1024, simple=True)
    sizes = [16, 24, 32, 48, 64, 128, 256]
    frames = []
    for s in sizes:
        src = simple if s <= 48 else big
        frames.append(src.resize((s, s), Image.LANCZOS))
    frames[-1].save(os.path.join(OUT, "luxia.ico"), sizes=[(s, s) for s in sizes], append_images=frames[:-1])
    for s in (16, 32, 48, 256):
        frames[sizes.index(s)].save(os.path.join(OUT, f"luxia-icone-{s}.png"))
    return big


def font(size, weight="Bold"):
    f = ImageFont.truetype("C:/Windows/Fonts/bahnschrift.ttf", size)
    try:
        f.set_variation_by_name(weight)
    except Exception:
        pass
    return f


def gradient_text(text, fnt, colors):
    bbox = fnt.getbbox(text)
    w, h = bbox[2], bbox[3]
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).text((0, 0), text, font=fnt, fill=255)
    grad = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(grad)
    for x in range(w):
        d.line([(x, 0), (x, h)], fill=(*ramp(colors, x / max(1, w - 1)), 255))
    grad.putalpha(mask)
    return grad, bbox


def logo(W=2400, H=900, background=True, name="luxia-logo"):
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    if background:
        d = ImageDraw.Draw(img)
        for y in range(H):
            d.line([(0, y), (W, y)], fill=(*lerp(BG_TOP, BG_BOTTOM, y / H), 255))
    cap = int(H * 0.46)
    f = font(cap, "Bold")
    lu, lub = gradient_text("Lu", f, [WARM[1], WARM[0]])
    ia, iab = gradient_text("ia", f, [COOL[0], COOL[2]])
    # Le X occupe exactement la hauteur des capitales : sources au niveau du haut du « L », pieds sur la ligne de base.
    cap_top, baseline = lub[1], lub[3]
    # Hauteur utile un peu réduite : la largeur des faisceaux ne doit pas passer sous la ligne de base.
    inner = int((baseline - cap_top) * 0.88)
    xsize = int(inner / (1 - 2 * MARGIN))
    mark = draw_mark(xsize * 2, background=False).resize((xsize, xsize), Image.LANCZOS)
    off = int(xsize * MARGIN)
    gap = int(cap * 0.16)
    total = lu.width + gap + inner + gap + ia.width
    x0 = (W - total) // 2
    tagline_h = int(H * 0.075)
    block = (baseline - cap_top) + int(H * 0.12) + tagline_h
    base_y = (H - block) // 2 - cap_top
    img.alpha_composite(add_soft_glow(lu, cap), (x0, base_y))
    img.alpha_composite(mark, (x0 + lu.width + gap - off, base_y + cap_top + int((baseline - cap_top) * 0.04) - off))
    img.alpha_composite(add_soft_glow(ia, cap), (x0 + lu.width + gap + inner + gap, base_y))
    tag_y = base_y + baseline + int(H * 0.12)
    t = font(tagline_h, "SemiLight")
    d = ImageDraw.Draw(img)
    line1 = "LUMIÈRE  ×  INTELLIGENCE ARTIFICIELLE"
    tw = d.textlength(line1, font=t)
    d.text(((W - tw) / 2, tag_y), line1, font=t, fill=(200, 208, 220, 255))
    img.save(os.path.join(OUT, f"{name}.png"))
    return img


def add_soft_glow(layer, cap):
    pad = int(cap * 0.2)
    out = Image.new("RGBA", (layer.width + 2 * pad, layer.height + 2 * pad), (0, 0, 0, 0))
    out.alpha_composite(layer, (pad, pad))
    glow = out.filter(ImageFilter.GaussianBlur(cap * 0.05))
    glow.putalpha(glow.getchannel("A").point(lambda a: int(a * 0.6)))
    res = Image.new("RGBA", out.size, (0, 0, 0, 0))
    res.alpha_composite(glow)
    res.alpha_composite(out)
    return res.crop((pad, pad, pad + layer.width, pad + layer.height))


if __name__ == "__main__":
    icon_files()
    logo()
    logo(background=False, name="luxia-logo-transparent")
    print("fichiers dans", OUT)
