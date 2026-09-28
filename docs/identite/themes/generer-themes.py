"""Identités thématiques de LuXia : Halloween, Noël, mariage, anniversaire.

Même dessin que l'identité de référence (../generer.py : le X formé d'un faisceau « lumière » et d'un faisceau « IA »
qui se croisent), avec une palette et des décors propres à chaque fête. Rien n'est appliqué à l'exécutable : ce sont
des fichiers prêts à l'emploi (icône, .ico, logo, aperçu), à brancher plus tard si l'on veut un habillage saisonnier.

Usage (depuis ce dossier) : python generer-themes.py            → un sous-dossier par thème
                            python generer-themes.py noel        → un seul thème
Prérequis : ceux de ../generer.py (Inkscape, Pillow, police Bahnschrift).
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.dirname(HERE))
import generer as base  # noqa: E402  (géométrie et rendu de l'identité de référence)

from PIL import Image, ImageDraw, ImageFont  # noqa: E402

S = 1024          # côté de l'icône
LW, LH = 2400, 800  # logo


# ——— Petits dessins réutilisables (SVG) ———

def star(cx, cy, r, color, points=5, inner=0.45, rotation=-90, opacity=1.0):
    pts = []
    for i in range(points * 2):
        rr = r if i % 2 == 0 else r * inner
        a = math.radians(rotation + i * 180 / points)
        pts.append(f"{cx + rr * math.cos(a):.1f},{cy + rr * math.sin(a):.1f}")
    return f'<polygon points="{" ".join(pts)}" fill="{color}" opacity="{opacity}"/>'


def snowflake(cx, cy, r, color="#FFFFFF", opacity=0.9, width=None):
    w = width or max(r * 0.12, 1.2)
    out = [f'<g stroke="{color}" stroke-width="{w:.1f}" stroke-linecap="round" opacity="{opacity}">']
    for k in range(6):
        a = math.radians(k * 60)
        x, y = cx + r * math.cos(a), cy + r * math.sin(a)
        out.append(f'<line x1="{cx:.1f}" y1="{cy:.1f}" x2="{x:.1f}" y2="{y:.1f}"/>')
        for f in (0.55,):
            bx, by = cx + r * f * math.cos(a), cy + r * f * math.sin(a)
            for side in (-1, 1):
                b = a + side * math.radians(40)
                out.append(f'<line x1="{bx:.1f}" y1="{by:.1f}" x2="{bx + r * 0.3 * math.cos(b):.1f}" y2="{by + r * 0.3 * math.sin(b):.1f}"/>')
    out.append("</g>")
    return "".join(out)


def bat(cx, cy, w, color="#0B0610", opacity=1.0, flip=False):
    # Silhouette de chauve-souris (ailes festonnées), largeur w, centrée.
    s = w / 100
    d = ("M0,0 C-8,-10 -18,-12 -24,-8 C-30,-16 -42,-18 -50,-10 C-44,-8 -40,-2 -40,4 C-34,0 -28,2 -24,8 "
         "C-18,2 -10,4 -6,10 C-4,6 -2,4 0,4 C2,4 4,6 6,10 C10,4 18,2 24,8 C28,2 34,0 40,4 C40,-2 44,-8 50,-10 "
         "C42,-18 30,-16 24,-8 C18,-12 8,-10 0,0 Z M-4,-2 L-6,-9 L-2,-4 Z M4,-2 L6,-9 L2,-4 Z")
    sx = -s if flip else s
    return f'<path d="{d}" transform="translate({cx:.1f},{cy:.1f}) scale({sx:.3f},{s:.3f})" fill="{color}" opacity="{opacity}"/>'


def crescent(cx, cy, r, color, bg_offset=(0.38, -0.12), uid="moon"):
    ox, oy = bg_offset
    return (f'<mask id="{uid}"><rect x="{cx - r * 2:.0f}" y="{cy - r * 2:.0f}" width="{r * 4:.0f}" height="{r * 4:.0f}" fill="#FFF"/>'
            f'<circle cx="{cx + r * ox:.1f}" cy="{cy + r * oy:.1f}" r="{r * 0.92:.1f}" fill="#000"/></mask>'
            f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{r:.1f}" fill="{color}" mask="url(#{uid})"/>')


def heart(cx, cy, size, color, opacity=1.0, rotation=0):
    s = size / 100
    d = "M0,30 C-40,5 -50,-25 -25,-35 C-10,-40 0,-28 0,-20 C0,-28 10,-40 25,-35 C50,-25 40,5 0,30 Z"
    return f'<path d="{d}" transform="translate({cx:.1f},{cy:.1f}) rotate({rotation}) scale({s:.3f})" fill="{color}" opacity="{opacity}"/>'


def ring(cx, cy, r, color, width):
    return (f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{r:.1f}" fill="none" stroke="{color}" stroke-width="{width:.1f}"/>'
            f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{r:.1f}" fill="none" stroke="#FFFFFF" stroke-opacity="0.45" stroke-width="{width * 0.25:.1f}" '
            f'stroke-dasharray="{r * 0.6:.1f} {r * 5:.1f}"/>')


def balloon(cx, cy, r, color, string="#E6EDF3"):
    return (f'<path d="M{cx:.1f},{cy + r * 1.15:.1f} C{cx - r * 0.3:.1f},{cy + r * 1.7:.1f} {cx + r * 0.3:.1f},{cy + r * 2.1:.1f} {cx:.1f},{cy + r * 2.7:.1f}" '
            f'stroke="{string}" stroke-width="{r * 0.05:.1f}" fill="none" opacity="0.8"/>'
            f'<ellipse cx="{cx:.1f}" cy="{cy:.1f}" rx="{r * 0.86:.1f}" ry="{r:.1f}" fill="{color}"/>'
            f'<polygon points="{cx - r * 0.12:.1f},{cy + r * 1.12:.1f} {cx + r * 0.12:.1f},{cy + r * 1.12:.1f} {cx:.1f},{cy + r * 0.96:.1f}" fill="{color}"/>'
            f'<ellipse cx="{cx - r * 0.32:.1f}" cy="{cy - r * 0.38:.1f}" rx="{r * 0.18:.1f}" ry="{r * 0.28:.1f}" fill="#FFFFFF" opacity="0.45" '
            f'transform="rotate(-25 {cx - r * 0.32:.1f} {cy - r * 0.38:.1f})"/>')


def confetti(rng, n, x0, y0, x1, y1, colors, size, avoid=None):
    out = []
    for _ in range(n):
        x, y = rng.uniform(x0, x1), rng.uniform(y0, y1)
        if avoid and avoid(x, y):
            continue
        w, h = size * rng.uniform(0.6, 1.2), size * rng.uniform(0.25, 0.45)
        out.append(f'<rect x="{x - w / 2:.1f}" y="{y - h / 2:.1f}" width="{w:.1f}" height="{h:.1f}" rx="{h * 0.3:.1f}" '
                   f'fill="{rng.choice(colors)}" transform="rotate({rng.uniform(0, 180):.0f} {x:.1f} {y:.1f})" opacity="0.9"/>')
    return "".join(out)


def pumpkin(cx, cy, r):
    body = "".join(
        f'<ellipse cx="{cx + dx * r:.1f}" cy="{cy:.1f}" rx="{r * rx:.1f}" ry="{r * 0.82:.1f}" fill="{c}"/>'
        for dx, rx, c in ((-0.42, 0.5, "#D9661A"), (0.42, 0.5, "#D9661A"), (0, 0.55, "#F08A24")))
    face = (f'<polygon points="{cx - r * 0.42:.1f},{cy - r * 0.12:.1f} {cx - r * 0.2:.1f},{cy - r * 0.12:.1f} {cx - r * 0.31:.1f},{cy - r * 0.34:.1f}" fill="#FFE27A"/>'
            f'<polygon points="{cx + r * 0.2:.1f},{cy - r * 0.12:.1f} {cx + r * 0.42:.1f},{cy - r * 0.12:.1f} {cx + r * 0.31:.1f},{cy - r * 0.34:.1f}" fill="#FFE27A"/>'
            f'<path d="M{cx - r * 0.5:.1f},{cy + r * 0.12:.1f} Q{cx:.1f},{cy + r * 0.62:.1f} {cx + r * 0.5:.1f},{cy + r * 0.12:.1f} '
            f'L{cx + r * 0.3:.1f},{cy + r * 0.2:.1f} L{cx + r * 0.15:.1f},{cy + r * 0.1:.1f} L{cx:.1f},{cy + r * 0.22:.1f} '
            f'L{cx - r * 0.15:.1f},{cy + r * 0.1:.1f} L{cx - r * 0.3:.1f},{cy + r * 0.2:.1f} Z" fill="#FFE27A"/>')
    stem = f'<rect x="{cx - r * 0.08:.1f}" y="{cy - r * 1.05:.1f}" width="{r * 0.16:.1f}" height="{r * 0.3:.1f}" rx="{r * 0.05:.1f}" fill="#5A7A2A"/>'
    return stem + body + face


def santa_hat(cx, cy, r):
    # Bonnet posé sur la source ronde du faisceau « lumière » (cx, cy = centre du rond, r = son rayon).
    return (f'<path d="M{cx - r * 1.25:.1f},{cy - r * 0.55:.1f} Q{cx - r * 0.2:.1f},{cy - r * 3.1:.1f} {cx + r * 1.9:.1f},{cy - r * 2.1:.1f} '
            f'Q{cx + r * 0.6:.1f},{cy - r * 1.6:.1f} {cx + r * 1.25:.1f},{cy - r * 0.55:.1f} Z" fill="#D7263D"/>'
            f'<rect x="{cx - r * 1.4:.1f}" y="{cy - r * 0.85:.1f}" width="{r * 2.8:.1f}" height="{r * 0.62:.1f}" rx="{r * 0.31:.1f}" fill="#FFFFFF"/>'
            f'<circle cx="{cx + r * 1.95:.1f}" cy="{cy - r * 2.05:.1f}" r="{r * 0.42:.1f}" fill="#FFFFFF"/>')


def party_hat(cx, cy, r):
    # Chapeau pointu posé sur la puce (cx, cy = centre de la puce, r = demi-côté).
    return (f'<polygon points="{cx - r * 1.1:.1f},{cy - r * 1.05:.1f} {cx + r * 1.1:.1f},{cy - r * 1.05:.1f} {cx + r * 0.1:.1f},{cy - r * 3.6:.1f}" fill="#FF3D7F"/>'
            f'<line x1="{cx - r * 0.55:.1f}" y1="{cy - r * 1.75:.1f}" x2="{cx + r * 0.75:.1f}" y2="{cy - r * 1.4:.1f}" stroke="#FFE08A" stroke-width="{r * 0.25:.1f}"/>'
            f'<line x1="{cx - r * 0.2:.1f}" y1="{cy - r * 2.55:.1f}" x2="{cx + r * 0.45:.1f}" y2="{cy - r * 2.35:.1f}" stroke="#8CF6FF" stroke-width="{r * 0.22:.1f}"/>'
            f'<circle cx="{cx + r * 0.1:.1f}" cy="{cy - r * 3.65:.1f}" r="{r * 0.38:.1f}" fill="#FFE08A"/>')


# ——— Les thèmes ———
# Chaque thème : palette (fond, faisceau lumière chaud, faisceau IA froid, cœur), textes du logo, et deux fonctions de
# décor : l'une pour l'icône (repère 1024 × 1024), l'autre pour le logo (2400 × 800, repère du X donné).

def m():
    """Points clés du X dans le repère de l'icône : source lumière (rond), source IA (puce), centre."""
    mm = S * base.MARGIN
    return (mm, mm), (S - mm, mm), (S / 2, S / 2)


def halloween_icon(rng):
    light, chip, _ = m()
    out = crescent(S * 0.5, S * 0.14, S * 0.075, "#F5E6B8", uid="hmoon")
    out += bat(S * 0.30, S * 0.12, S * 0.16, "#8A62B0") + bat(S * 0.72, S * 0.22, S * 0.11, "#8A62B0", flip=True)
    out += pumpkin(S * 0.5, S * 0.86, S * 0.075)
    # Toile d'araignée dans le coin bas gauche.
    web = "".join(f'<line x1="0" y1="{S}" x2="{S * 0.3 * math.cos(math.radians(a)):.1f}" y2="{S - S * 0.3 * math.sin(math.radians(a)):.1f}"/>' for a in (0, 22, 45, 68, 90))
    arcs = "".join(f'<path d="M{r:.1f},{S} Q{r * 0.75:.1f},{S - r * 0.75:.1f} 0,{S - r:.1f}" fill="none"/>' for r in (S * 0.1, S * 0.18, S * 0.26))
    out += f'<g stroke="#B7A7C9" stroke-width="{S * 0.004:.1f}" opacity="0.55">{web}{arcs}</g>'
    return out


def halloween_logo(rng, mark_box):
    out = crescent(LW * 0.14, LH * 0.24, LH * 0.09, "#F5E6B8", uid="hlmoon")
    for x, y, w, flip in ((0.23, 0.17, 150, False), (0.79, 0.2, 120, True), (0.87, 0.11, 80, False)):
        out += bat(LW * x, LH * y, w, "#8A62B0", flip=flip)
    out += pumpkin(LW * 0.13, LH * 0.8, LH * 0.075) + pumpkin(LW * 0.87, LH * 0.8, LH * 0.06)
    return out


def noel_icon(rng):
    light, chip, center = m()
    out = ""
    for _ in range(26):
        x, y = rng.uniform(0, S), rng.uniform(0, S)
        if abs(x - y) < S * 0.18 or abs(x + y - S) < S * 0.18:
            continue  # pas de flocon sur les faisceaux
        out += f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{rng.uniform(2.5, 7):.1f}" fill="#FFFFFF" opacity="{rng.uniform(0.35, 0.85):.2f}"/>'
    out += snowflake(S * 0.5, S * 0.12, S * 0.055) + snowflake(S * 0.13, S * 0.52, S * 0.04) + snowflake(S * 0.87, S * 0.55, S * 0.045)
    out += star(center[0], center[1], S * 0.07, "#FFF6D0", points=8, inner=0.35, rotation=-90)
    out += santa_hat(light[0], light[1], S * 0.052)
    # Houx en bas.
    out += (f'<ellipse cx="{S * 0.45:.1f}" cy="{S * 0.9:.1f}" rx="{S * 0.06:.1f}" ry="{S * 0.025:.1f}" fill="#1E7A46" transform="rotate(-20 {S * 0.45:.1f} {S * 0.9:.1f})"/>'
            f'<ellipse cx="{S * 0.55:.1f}" cy="{S * 0.9:.1f}" rx="{S * 0.06:.1f}" ry="{S * 0.025:.1f}" fill="#23924F" transform="rotate(20 {S * 0.55:.1f} {S * 0.9:.1f})"/>'
            + "".join(f'<circle cx="{S * x:.1f}" cy="{S * y:.1f}" r="{S * 0.017:.1f}" fill="#E0223A"/>' for x, y in ((0.5, 0.88), (0.48, 0.905), (0.525, 0.905))))
    return out


def noel_logo(rng, mark_box):
    out = ""
    for _ in range(70):
        x, y = rng.uniform(0, LW), rng.uniform(0, LH)
        if LH * 0.22 < y < LH * 0.62 and LW * 0.2 < x < LW * 0.8:
            continue
        out += f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{rng.uniform(2, 6):.1f}" fill="#FFFFFF" opacity="{rng.uniform(0.3, 0.8):.2f}"/>'
    for x, y, r in ((0.12, 0.3, 42), (0.9, 0.28, 36), (0.2, 0.78, 26), (0.82, 0.8, 30), (0.5, 0.86, 22)):
        out += snowflake(LW * x, LH * y, r, width=3)
    # Bonnet sur la source ronde du X (repère du logo donné par mark_box : x, y, échelle).
    mx, my, scale = mark_box
    light, _, _ = m()
    out += santa_hat(mx + light[0] * scale, my + light[1] * scale, S * 0.052 * scale)
    return out


def mariage_icon(rng):
    _, _, center = m()
    out = ring(center[0] - S * 0.05, center[1] + S * 0.02, S * 0.085, "#E3B872", S * 0.018)
    out += ring(center[0] + S * 0.05, center[1] + S * 0.02, S * 0.085, "#F2D39B", S * 0.018)
    for x, y, sz, rot in ((0.5, 0.1, 70, 0), (0.12, 0.5, 40, -20), (0.88, 0.5, 40, 20), (0.5, 0.9, 50, 0)):
        out += heart(S * x, S * y, sz, "#E8A0A8", opacity=0.85, rotation=rot)
    for _ in range(18):
        x, y = rng.uniform(0, S), rng.uniform(0, S)
        if abs(x - y) < S * 0.16 or abs(x + y - S) < S * 0.16:
            continue
        out += f'<ellipse cx="{x:.1f}" cy="{y:.1f}" rx="{rng.uniform(5, 10):.1f}" ry="{rng.uniform(3, 5):.1f}" fill="#F4C6CC" opacity="0.7" transform="rotate({rng.uniform(0, 180):.0f} {x:.1f} {y:.1f})"/>'
    return out


def mariage_logo(rng, mark_box):
    out = ""
    for x, y, sz, rot in ((0.14, 0.3, 90, -15), (0.87, 0.3, 80, 15), (0.1, 0.72, 50, -25), (0.9, 0.74, 55, 20)):
        out += heart(LW * x, LH * y, sz, "#E8A0A8", opacity=0.8, rotation=rot)
    mx, my, scale = mark_box
    _, _, center = m()
    cx, cy = mx + center[0] * scale, my + center[1] * scale
    out += ring(cx - S * 0.05 * scale, cy + S * 0.02 * scale, S * 0.085 * scale, "#E3B872", S * 0.018 * scale)
    out += ring(cx + S * 0.05 * scale, cy + S * 0.02 * scale, S * 0.085 * scale, "#F2D39B", S * 0.018 * scale)
    for _ in range(40):
        x, y = rng.uniform(0, LW), rng.uniform(0, LH)
        if LH * 0.2 < y < LH * 0.64 and LW * 0.2 < x < LW * 0.8:
            continue
        out += f'<ellipse cx="{x:.1f}" cy="{y:.1f}" rx="{rng.uniform(6, 12):.1f}" ry="{rng.uniform(3, 6):.1f}" fill="#F4C6CC" opacity="0.65" transform="rotate({rng.uniform(0, 180):.0f} {x:.1f} {y:.1f})"/>'
    return out


PARTY = ["#FF3D7F", "#FFE08A", "#8CF6FF", "#B06BFF", "#6BFF9E", "#FF9A3D"]


def anniversaire_icon(rng):
    _, chip, _ = m()
    out = confetti(rng, 60, 0, 0, S, S, PARTY, S * 0.04, avoid=lambda x, y: abs(x - y) < S * 0.15 or abs(x + y - S) < S * 0.15)
    out += balloon(S * 0.12, S * 0.62, S * 0.06, "#FF3D7F") + balloon(S * 0.88, S * 0.66, S * 0.055, "#8CF6FF")
    out += party_hat(chip[0], chip[1], S * 0.05)
    return out


def anniversaire_logo(rng, mark_box):
    out = confetti(rng, 140, 0, 0, LW, LH, PARTY, 22, avoid=lambda x, y: LH * 0.2 < y < LH * 0.64 and LW * 0.2 < x < LW * 0.8)
    out += balloon(LW * 0.1, LH * 0.3, 55, "#FF3D7F") + balloon(LW * 0.15, LH * 0.36, 45, "#FFE08A")
    out += balloon(LW * 0.88, LH * 0.28, 52, "#8CF6FF") + balloon(LW * 0.93, LH * 0.35, 42, "#B06BFF")
    mx, my, scale = mark_box
    _, chip, _ = m()
    out += party_hat(mx + chip[0] * scale, my + chip[1] * scale, S * 0.05 * scale)
    return out


THEMES = {
    "halloween": {
        "title": "Halloween", "bg": ("#1F1030", "#07040B"),
        "warm": ["#FFE0A0", "#FF8C1A", "#D9480F"], "cool": ["#C8FF7A", "#6BE33A", "#8E3CFF"],
        "core": "#FFE8C0", "icon": halloween_icon, "logo": halloween_logo, "text_glow": 0.6,
        "idea": "Citrouille (faisceau lumière orange), sortilège (faisceau IA vert poison → violet), lune, chauves-souris, toile d'araignée.",
    },
    "noel": {
        "title": "Noël", "bg": ("#0F2140", "#040914"),
        "warm": ["#FFF3C4", "#FFD166", "#E0A31A"], "cool": ["#D6FFE6", "#35D07F", "#D7263D"],
        "core": "#FFFFFF", "icon": noel_icon, "logo": noel_logo, "text_glow": 0.6,
        "idea": "Guirlande dorée (lumière), sapin vert → rouge (IA), étoile au croisement, bonnet sur le projecteur, neige, houx.",
    },
    "mariage": {
        "title": "Mariage", "bg": ("#FFF9F1", "#F1E2CE"),
        "warm": ["#D98C73", "#B8664F", "#8E4A3A"], "cool": ["#8F9BE6", "#7078CF", "#6A4DB5"],
        "core": "#FFE7B0", "icon": mariage_icon, "logo": mariage_logo, "text_glow": 0.35, "light": True,
        "idea": "Fond ivoire clair, or rose (lumière) et perle lavande (IA), deux alliances entrelacées au croisement, cœurs et pétales.",
    },
    "anniversaire": {
        "title": "Anniversaire", "bg": ("#22103A", "#0A0716"),
        "warm": ["#FFE08A", "#FF7AB8", "#FF3D7F"], "cool": ["#8CF6FF", "#4DA3FF", "#B06BFF"],
        "core": "#FFFFFF", "icon": anniversaire_icon, "logo": anniversaire_logo, "text_glow": 0.6,
        "idea": "Bonbon (lumière jaune → rose), néon (IA cyan → violet), confettis, ballons, chapeau de fête sur la puce.",
    },
}


def themed(theme, key):
    t = THEMES[theme]
    base.BG_TOP, base.BG_BOTTOM = t["bg"]
    base.WARM, base.COOL = t["warm"], t["cool"]
    rng = random.Random(theme)  # décor identique d'une génération à l'autre
    if key == "icon":
        svg = base.icon_svg()
        deco = t["icon"](rng)
        svg = svg.replace("\n  </g>\n</svg>", f"\n    <g id=\"decor\">{deco}</g>\n  </g>\n</svg>")
    else:
        svg = base.logo_svg(background=(key == "logo"))
        box = mark_box()
        deco = t["logo"](rng, box)
        svg = svg.replace("\n</svg>", f"\n  <g id=\"decor\">{deco}</g>\n</svg>")
        if t.get("light"):
            # Fond clair : textes plus soutenus et halo discret pour rester lisibles.
            svg = svg.replace('opacity="0.55">Lu', f'opacity="{t["text_glow"]}">Lu').replace('opacity="0.55">ia', f'opacity="{t["text_glow"]}">ia')
    # Cœur du croisement à la couleur du thème (blanc invisible sur fond clair).
    svg = svg.replace('stop-color="#FFF6E6"', f'stop-color="{t["core"]}"')
    if t.get("light"):
        svg = svg.replace('<stop offset="0" stop-color="#FFFFFF" stop-opacity="1"/><stop offset="0.12" stop-color="#FFFFFF" stop-opacity="0.95"/>',
                          f'<stop offset="0" stop-color="#FFFFFF" stop-opacity="1"/><stop offset="0.12" stop-color="{t["core"]}" stop-opacity="0.95"/>')
    return svg


def mark_box():
    """Position (x, y) et échelle du X dans le logo, calculées comme dans base.logo_svg."""
    size = 420
    f = ImageFont.truetype("C:/Windows/Fonts/bahnschrift.ttf", size)
    f.set_variation_by_name("Bold")
    asc, _ = f.getmetrics()
    cap_h = asc - f.getbbox("Lu")[1]
    lu_w, ia_w = f.getlength("Lu"), f.getlength("ia")
    inner = cap_h * 0.88
    side = inner / (1 - 2 * base.MARGIN)
    gap = size * 0.16
    total = lu_w + gap + inner + gap + ia_w
    x0 = (LW - total) / 2
    baseline = (LH + cap_h) / 2
    cap_top = baseline - cap_h
    return x0 + lu_w + gap - side * base.MARGIN, cap_top + cap_h * 0.04 - side * base.MARGIN, side / 1024


def build(theme):
    folder = os.path.join(HERE, theme)
    svg_dir, out_dir = os.path.join(folder, "svg"), os.path.join(folder, "sortie")
    os.makedirs(svg_dir, exist_ok=True)
    os.makedirs(out_dir, exist_ok=True)
    name = f"luxia-{theme}"

    def write(file, content):
        path = os.path.join(svg_dir, file)
        with open(path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(content)
        return path

    def png(src, out, width):
        base.inkscape([src, "--export-type=png", f"--export-width={width}", f"--export-filename={os.path.join(out_dir, out)}"])

    icon = write(f"{name}-icone.svg", themed(theme, "icon"))
    logo = write(f"{name}-logo.svg", themed(theme, "logo"))
    logo_t = write(f"{name}-logo-transparent.svg", themed(theme, "logo-transparent"))
    for size in (1024, 256, 128, 64, 48, 32, 24, 16):
        png(icon, f"{name}-icone-{size}.png", size)
    sizes = [16, 24, 32, 48, 64, 128, 256]
    frames = [Image.open(os.path.join(out_dir, f"{name}-icone-{s}.png")).convert("RGBA") for s in sizes]
    frames[-1].save(os.path.join(out_dir, f"{name}.ico"), sizes=[(s, s) for s in sizes], append_images=frames[:-1])
    png(logo, f"{name}-logo.png", 2400)
    png(logo, f"{name}-logo-1200.png", 1200)
    png(logo_t, f"{name}-logo-transparent.png", 2400)
    for src, out in ((icon, f"{name}-icone.svg"), (logo, f"{name}-logo.svg")):
        base.inkscape([src, "--export-plain-svg", "--export-text-to-path", f"--export-filename={os.path.join(out_dir, out)}"])
    preview(theme, out_dir, name)
    print("thème", theme, ":", out_dir)


def preview(theme, out_dir, name):
    """Planche d'aperçu : logo, puis l'icône à 256, 64, 32 et 16 px sur fond clair et foncé."""
    logo = Image.open(os.path.join(out_dir, f"{name}-logo-1200.png")).convert("RGBA")
    sheet = Image.new("RGBA", (1200, 400 + 330), "#F0F0F0")
    sheet.paste(logo, (0, 0), logo)
    draw = ImageDraw.Draw(sheet)
    draw.rectangle((0, 565, 1200, 730), fill="#1E1E1E")
    x = 40
    for size in (256, 64, 32, 16):
        icon = Image.open(os.path.join(out_dir, f"{name}-icone-{size}.png")).convert("RGBA")
        small = icon if size < 256 else icon.resize((150, 150), Image.LANCZOS)
        for y, _ in ((420, "clair"), (575, "foncé")):
            sheet.paste(small, (x, y + (150 - small.height) // 2), small)
        x += 220
    sheet.convert("RGB").save(os.path.join(HERE, theme, f"apercu-{theme}.png"))


if __name__ == "__main__":
    wanted = sys.argv[1:] or list(THEMES)
    for theme in wanted:
        build(theme)
