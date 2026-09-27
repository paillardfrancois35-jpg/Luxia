"""Identité visuelle de LuXia, en SVG (référence) rendu par Inkscape.

Concept : le X de LuXia est formé de deux faisceaux qui se croisent — « lumière » (chaud, continu, issu d'un projecteur)
et « IA » (froid, fait de pixels, issu d'une puce) ; un point de lumière blanche naît à leur intersection.
« Lu » (lumière) en tons chauds, « ia » (intelligence artificielle) en tons froids.

Usage : python generer.py   → svg/ (sources vectorielles) et sortie/ (PNG, ICO)
Prérequis : Inkscape (C:\\Program Files\\Inkscape\\bin\\inkscape.exe), Pillow (assemblage du .ico), police Bahnschrift.
"""
import math
import os
import subprocess

from PIL import Image, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
SVG = os.path.join(HERE, "svg")
OUT = os.path.join(HERE, "sortie")
INKSCAPE = r"C:\Program Files\Inkscape\bin\inkscape.exe"
os.makedirs(SVG, exist_ok=True)
os.makedirs(OUT, exist_ok=True)

BG_TOP, BG_BOTTOM = "#161B26", "#080A10"
WARM = ["#FFECAA", "#FFAA3C", "#FF6E28"]   # lumière : blanc chaud → ambre → orangé
COOL = ["#96F5FF", "#46BEFF", "#8C5AFF"]   # IA : cyan clair → bleu → violet
MARGIN = 0.17                               # sources et extrémités des faisceaux, en fraction du carré


def cone(p0, p1, w0, w1):
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    L = math.hypot(dx, dy)
    nx, ny = -dy / L, dx / L
    pts = [(p0[0] + nx * w0 / 2, p0[1] + ny * w0 / 2), (p1[0] + nx * w1 / 2, p1[1] + ny * w1 / 2),
           (p1[0] - nx * w1 / 2, p1[1] - ny * w1 / 2), (p0[0] - nx * w0 / 2, p0[1] - ny * w0 / 2)]
    return " ".join(f"{x:.1f},{y:.1f}" for x, y in pts)


def mark(uid, S=1024, simple=False):
    """Défs et dessin du X lumineux dans un carré S × S (sans fond). Renvoie (defs, corps)."""
    m = S * MARGIN
    a0, a1 = (m, m), (S - m, S - m)
    b0, b1 = (S - m, m), (m, S - m)
    narrow, wide = S * 0.03, S * (0.26 if simple else 0.20)
    end_opacity = 0.6 if simple else 0.28
    cell = S * 0.022
    sq = cell * 0.78
    defs = f"""
    <linearGradient id="{uid}warm" gradientUnits="userSpaceOnUse" x1="{a0[0]}" y1="{a0[1]}" x2="{a1[0]}" y2="{a1[1]}">
      <stop offset="0" stop-color="{WARM[0]}"/><stop offset="0.5" stop-color="{WARM[1]}" stop-opacity="0.9"/>
      <stop offset="1" stop-color="{WARM[2]}" stop-opacity="{end_opacity}"/></linearGradient>
    <linearGradient id="{uid}cool" gradientUnits="userSpaceOnUse" x1="{b0[0]}" y1="{b0[1]}" x2="{b1[0]}" y2="{b1[1]}">
      <stop offset="0" stop-color="{COOL[0]}"/><stop offset="0.5" stop-color="{COOL[1]}" stop-opacity="0.95"/>
      <stop offset="1" stop-color="{COOL[2]}" stop-opacity="{min(1, end_opacity + 0.15)}"/></linearGradient>
    <radialGradient id="{uid}core" gradientUnits="userSpaceOnUse" cx="{S/2}" cy="{S/2}" r="{S*0.30}">
      <stop offset="0" stop-color="#FFFFFF" stop-opacity="1"/><stop offset="0.12" stop-color="#FFFFFF" stop-opacity="0.95"/>
      <stop offset="0.4" stop-color="#FFF6E6" stop-opacity="0.35"/><stop offset="1" stop-color="#FFFFFF" stop-opacity="0"/></radialGradient>
    <filter id="{uid}glow" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation="{S*0.022}"/></filter>
    <pattern id="{uid}grid" patternUnits="userSpaceOnUse" width="{cell}" height="{cell}">
      <rect x="{(cell-sq)/2:.2f}" y="{(cell-sq)/2:.2f}" width="{sq:.2f}" height="{sq:.2f}" rx="{sq*0.12:.2f}" fill="#FFFFFF"/></pattern>
    <mask id="{uid}pix" maskUnits="userSpaceOnUse" x="0" y="0" width="{S}" height="{S}">
      <rect x="0" y="0" width="{S}" height="{S}" fill="url(#{uid}grid)"/></mask>"""
    beam_a = f'<polygon points="{cone(a0, a1, narrow, wide)}" fill="url(#{uid}warm)"/>'
    beam_b_shape = f'<polygon points="{cone(b0, b1, narrow, wide)}" fill="url(#{uid}cool)"/>'
    beam_b = beam_b_shape if simple else f'<g mask="url(#{uid}pix)">{beam_b_shape}</g>'
    r = S * 0.052
    q = S * 0.05
    sw = S * 0.008
    pins = ""
    if not simple:
        for k in (-1, 0, 1):
            for side in (-1, 1):
                x = b0[0] + k * q * 0.55
                pins += f'<line x1="{x:.1f}" y1="{b0[1]+side*q:.1f}" x2="{x:.1f}" y2="{b0[1]+side*q*1.45:.1f}"/>'
                y = b0[1] + k * q * 0.55
                pins += f'<line x1="{b0[0]+side*q:.1f}" y1="{y:.1f}" x2="{b0[0]+side*q*1.45:.1f}" y2="{y:.1f}"/>'
    body = f"""
    <g filter="url(#{uid}glow)" opacity="0.9">{beam_a}{beam_b_shape}</g>
    {beam_a}
    {beam_b}
    <circle cx="{S/2}" cy="{S/2}" r="{S*0.30}" fill="url(#{uid}core)"/>
    <circle cx="{a0[0]}" cy="{a0[1]}" r="{r:.1f}" fill="#FFD68C" stroke="#FFF5DC" stroke-width="{sw:.1f}"/>
    <g stroke="#A0EBFF" stroke-width="{sw:.1f}" stroke-linecap="round">{pins}</g>
    <rect x="{b0[0]-q:.1f}" y="{b0[1]-q:.1f}" width="{2*q:.1f}" height="{2*q:.1f}" rx="{q*0.3:.1f}" fill="#5AC8FF" stroke="#C8FAFF" stroke-width="{sw:.1f}"/>"""
    return defs, body


def icon_svg(simple=False):
    S = 1024
    defs, body = mark("i", S, simple)
    rx = S * 0.22
    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{S}" height="{S}" viewBox="0 0 {S} {S}">
  <title>LuXia</title>
  <defs>{defs}
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{BG_TOP}"/><stop offset="1" stop-color="{BG_BOTTOM}"/></linearGradient>
    <clipPath id="clip"><rect x="0" y="0" width="{S}" height="{S}" rx="{rx}"/></clipPath>
  </defs>
  <g clip-path="url(#clip)">
    <rect x="0" y="0" width="{S}" height="{S}" fill="url(#bg)"/>{body}
  </g>
</svg>
"""


def logo_svg(background=True):
    # Mesures de la police (Bahnschrift Bold) pour placer « Lu », le X et « ia » au pixel près.
    size = 420
    f = ImageFont.truetype("C:/Windows/Fonts/bahnschrift.ttf", size)
    f.set_variation_by_name("Bold")
    asc, _ = f.getmetrics()
    lu_bbox, ia_bbox = f.getbbox("Lu"), f.getbbox("ia")
    cap_h = asc - lu_bbox[1]                 # du haut du « L » à la ligne de base
    lu_w, ia_w = f.getlength("Lu"), f.getlength("ia")
    inner = cap_h * 0.88                     # hauteur utile du X (les pieds restent au-dessus de la ligne de base)
    S = inner / (1 - 2 * MARGIN)
    gap = size * 0.16
    total = lu_w + gap + inner + gap + ia_w
    W, H = 2400, 800
    x0 = (W - total) / 2
    baseline = (H + cap_h) / 2
    cap_top = baseline - cap_h
    mx = x0 + lu_w + gap - S * MARGIN
    my = cap_top + cap_h * 0.04 - S * MARGIN
    ix = x0 + lu_w + gap + inner + gap
    defs, body = mark("m", 1024, False)
    scale = S / 1024
    bg = f'<rect x="0" y="0" width="{W}" height="{H}" fill="url(#bg)"/>' if background else ""
    text_style = f'font-family:Bahnschrift;font-weight:700;font-size:{size}px'
    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">
  <title>LuXia</title>
  <defs>{defs}
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{BG_TOP}"/><stop offset="1" stop-color="{BG_BOTTOM}"/></linearGradient>
    <linearGradient id="lu" gradientUnits="userSpaceOnUse" x1="{x0:.1f}" y1="0" x2="{x0+lu_w:.1f}" y2="0">
      <stop offset="0" stop-color="{WARM[1]}"/><stop offset="1" stop-color="{WARM[0]}"/></linearGradient>
    <linearGradient id="ia" gradientUnits="userSpaceOnUse" x1="{ix:.1f}" y1="0" x2="{ix+ia_w:.1f}" y2="0">
      <stop offset="0" stop-color="{COOL[0]}"/><stop offset="1" stop-color="{COOL[2]}"/></linearGradient>
    <filter id="textglow" x="-10%" y="-20%" width="120%" height="140%"><feGaussianBlur stdDeviation="{size*0.035:.1f}"/></filter>
  </defs>
  {bg}
  <g style="{text_style}">
    <text x="{x0:.1f}" y="{baseline:.1f}" fill="url(#lu)" filter="url(#textglow)" opacity="0.55">Lu</text>
    <text x="{ix:.1f}" y="{baseline:.1f}" fill="url(#ia)" filter="url(#textglow)" opacity="0.55">ia</text>
    <text x="{x0:.1f}" y="{baseline:.1f}" fill="url(#lu)">Lu</text>
    <text x="{ix:.1f}" y="{baseline:.1f}" fill="url(#ia)">ia</text>
  </g>
  <g transform="translate({mx:.1f},{my:.1f}) scale({scale:.5f})">{body}
  </g>
</svg>
"""


def write(name, content):
    path = os.path.join(SVG, name)
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(content)
    return path


def inkscape(args):
    subprocess.run([INKSCAPE, *args], check=True, capture_output=True)


def png(svg_path, out_name, width):
    inkscape([svg_path, "--export-type=png", f"--export-width={width}", f"--export-filename={os.path.join(OUT, out_name)}"])


def plain(svg_path, out_name):
    """Copie SVG autonome (texte converti en tracés : aucune police requise pour l'afficher)."""
    inkscape([svg_path, "--export-plain-svg", "--export-text-to-path", f"--export-filename={os.path.join(OUT, out_name)}"])


if __name__ == "__main__":
    icon = write("luxia-icone.svg", icon_svg())
    small = write("luxia-icone-petite.svg", icon_svg(simple=True))
    logo = write("luxia-logo.svg", logo_svg())
    logo_t = write("luxia-logo-transparent.svg", logo_svg(background=False))

    for s in (1024, 512, 256, 128, 64):
        png(icon, f"luxia-icone-{s}.png", s)
    for s in (48, 32, 24, 16):
        png(small, f"luxia-icone-{s}.png", s)
    sizes = [16, 24, 32, 48, 64, 128, 256]
    frames = [Image.open(os.path.join(OUT, f"luxia-icone-{s}.png")).convert("RGBA") for s in sizes]
    frames[-1].save(os.path.join(OUT, "luxia.ico"), sizes=[(s, s) for s in sizes], append_images=frames[:-1])

    png(logo, "luxia-logo.png", 2400)
    png(logo_t, "luxia-logo-transparent.png", 2400)
    png(logo, "luxia-logo-1200.png", 1200)
    plain(icon, "luxia-icone.svg")
    plain(logo, "luxia-logo.svg")
    plain(logo_t, "luxia-logo-transparent.svg")
    print("fichiers dans", OUT)
