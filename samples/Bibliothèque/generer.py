# Génère les définitions des appareils du parc (doc 12 annexe A) au format de la bibliothèque DMX (doc 50).
# Sources : notices dans docs/Equipements (tableaux lus en P2). Relancer : python "samples/Bibliothèque/generer.py"
# Les fichiers produits sont ensuite modifiables dans l'éditeur ; ce script ne sert qu'à l'amorçage.
import json
import os
import uuid

ROOT = os.path.dirname(os.path.abspath(__file__))
EQ = "docs/Equipements/"


def uid(name):
    return str(uuid.uuid5(uuid.NAMESPACE_URL, "dmx/bibliotheque/" + name))


def cap(mn, mx, label, kind="fixed", strobe=None, param=None, colors=None, slot=None):
    c = {"min": mn, "max": mx, "kind": kind, "label": label}
    if strobe:
        c["strobe"] = strobe
    if param:
        c["parameter"] = {"nature": param[0], "start": param[1], "end": param[2]}
        if len(param) > 3:
            c["parameter"]["unit"] = param[3]
    if colors:
        c["colors"] = colors
    if slot:
        c["wheelSlot"] = slot
    return c


def ch(key, name, attr, caps=None, **kw):
    d = {"key": key, "name": name, "attribute": attr}
    d.update(kw)
    if caps:
        d["capabilities"] = caps
    return d


def fine(key):
    return ("fine", key)


def mode(name, short, setting, *chs):
    slots = []
    for c in chs:
        if isinstance(c, tuple):
            slots.append({"channel": c[1], "part": "fine"})
        else:
            slots.append({"channel": c})
    return {"name": name, "shortName": short, "deviceSetting": setting, "channels": slots}


def fixture(manu, model, cat, channels, modes, notes, physical=None, manual=None, reference=None):
    d = {"formatVersion": 1, "id": uid(manu + "/" + model), "manufacturer": manu, "model": model, "category": cat,
         "version": 1, "author": "DMX (depuis la notice)", "source": "manual", "notes": notes}
    if reference:
        d["reference"] = reference
    if manual:
        d["manual"] = manual
    if physical:
        d["physical"] = physical
    d["channels"] = channels
    d["modes"] = modes
    path = os.path.join(ROOT, manu, model + ".json")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(d, ensure_ascii=False, indent=2) + "\n")
    print(path)


PCT = ("vitesse", 0, 100, "%")

# 1. Betopper LPC008S (PAR RGB) — fiche docs/Equipements/PAR/BETOPPER/LPC008S/betopper-lpc008s.md
fixture("Betopper", "LPC008S", "par", [
    ch("dim", "CH1 Gradation générale", "intensity"),
    ch("r", "Rouge", "red"), ch("g", "Vert", "green"), ch("b", "Bleu", "blue"),
    ch("strobe", "Strobe général", "shutter", [cap(0, 255, "Strobe lent → rapide", "progressive", "strobe", PCT)],
       notes="La notice ne donne pas de plages : 0 = pas de strobe probablement (à vérifier en direct)."),
    ch("fn", "Sélecteur de fonction", "mode", [
        cap(0, 50, "Gradation DMX"), cap(51, 100, "Sortie couleur (8 couleurs, choix par CH7)", "program"),
        cap(101, 150, "Fondu (à vérifier : le rendu observé ne correspond pas exactement, BIB-094)", "program"), cap(151, 200, "Transition", "program"),
        cap(201, 250, "Pulsation", "program"), cap(251, 255, "Audio (sensibilité par CH7)", "program")]),
    ch("speed", "Vitesse", "programSpeed", [cap(0, 255, "Vitesse des fonctions lent → rapide (ou choix de couleur)", "progressive", param=PCT)]),
], [mode("3 canaux", "3CH", "d001", "r", "g", "b"),
    mode("7 canaux", "7CH", "A001", "dim", "r", "g", "b", "strobe", "fn", "speed")],
    "PAR LED RGB 54 × 3 W. Menu d001 = mode 3 canaux, A001 = mode 7 canaux (le menu d'adresse choisit le mode).",
    {"sourceType": "LED RGB 3-en-1", "power": 180},
    EQ + "PAR/BETOPPER/LPC008S/LPC007-LPC008_LPC08-H_BETOPPER_DJ_PAR_Light_User_Manual.pdf")

# 2. Betopper LPC010 (gros PAR RGBW)
fixture("Betopper", "LPC010", "par", [
    ch("dim", "CH1 Gradation générale", "intensity"),
    ch("r", "Rouge", "red"), ch("g", "Vert", "green"), ch("b", "Bleu", "blue"), ch("w", "Blanc", "white"),
    ch("strobe", "Strobe", "shutter", [cap(0, 255, "Strobe lent → rapide", "progressive", "strobe", PCT)],
       notes="Plages non détaillées par la notice (0 = sans strobe ? à vérifier)."),
    ch("effect", "Effet", "mode", [
        cap(0, 10, "Gradation 0-100 %"), cap(11, 50, "9 couleurs haut IRC (2800 K → 8000 K)", "program"),
        cap(51, 100, "Sortie couleur", "program"), cap(101, 150, "Saut de couleurs", "program"),
        cap(151, 200, "Fondu de couleurs", "program"), cap(201, 250, "Pulsation", "program"),
        cap(251, 255, "Contrôle par le son", "program")]),
    ch("control", "Contrôle", "maintenance", [
        cap(0, 10, "Réservé", "noFunction"), cap(11, 20, "Courbe de gradation : linéaire"), cap(21, 30, "Courbe : carré"),
        cap(31, 40, "Courbe : carré inverse"), cap(41, 50, "Courbe : en S"), cap(51, 60, "Ventilateur : auto"),
        cap(61, 70, "Ventilateur : studio"), cap(71, 80, "Réservé", "noFunction"),
        cap(81, 255, "Vitesse de l'effet CH7 0-100 %", "progressive", param=PCT)]),
], [mode("4 canaux", "4CH", "d001", "r", "g", "b", "w"),
    mode("8 canaux", "8CH", "A001", "dim", "r", "g", "b", "w", "strobe", "effect", "control")],
    "PAR LED RGBW 54 × 4 W (120 W). Menu d001 = 4 canaux, A001 = 8 canaux. Tableau lu dans la notice (rendu image, P2).",
    {"sourceType": "LED RGBW 4-en-1", "power": 120},
    EQ + "PAR/BETOPPER/LPC010/Betopper_54x4w_RGBW_4-in-1_High_CRI_Par_Light_LPC010_LPC010-H_User_Manual.pdf")

# 3. Betopper LPC120 (gros PAR RGBW)
fixture("Betopper", "LPC120", "par", [
    ch("dim", "CH1 Gradation totale", "intensity"),
    ch("r", "Rouge", "red"), ch("g", "Vert", "green"), ch("b", "Bleu", "blue"), ch("w", "Blanc", "white"),
    ch("strobe", "Strobe", "shutter", [cap(0, 255, "Strobe lent → rapide", "progressive", "strobe", PCT)],
       notes="Plages non détaillées par la notice (0 = sans strobe ? à vérifier)."),
    ch("effect", "Effet", "mode", [
        cap(0, 10, "Gradation DMX"), cap(11, 50, "7 couleurs haut IRC", "program"), cap(51, 100, "16 couleurs", "program"),
        cap(101, 150, "Saut de 16 couleurs", "program"), cap(151, 200, "Dégradé multicolore", "program"),
        cap(201, 250, "Pulsation multicolore", "program"), cap(251, 255, "Son, 9 couleurs", "program")]),
    ch("param", "Paramètre de l'effet", "programSpeed",
       [cap(0, 255, "Selon CH7 : couleur (pas de 16), vitesse, effet sonore", "progressive", param=("paramètre", 0, 100, "%"))]),
], [mode("4 canaux", "4CH", "d001", "r", "g", "b", "w"),
    mode("8 canaux", "8CH", "A001", "dim", "r", "g", "b", "w", "strobe", "effect", "param")],
    "PAR LED RGBW 60 × 2 W (120 W). d001 = 4 canaux, A001 = 8 canaux. Tableau lu dans la notice (rendu image, P2).",
    {"sourceType": "LED RGBW 4-en-1", "power": 120},
    EQ + "PAR/BETOPPER/LPC120/LPC120_-Betopper_60x2W_RGBW_4-in-1_DJ_Wedding_Par_Lights-manual.pdf")

# 4. Tomshine mini lyre à gobos (9 / 11 canaux) — refait le 2026-09-25 d'après la photo du tableau réel de la notice
#    (docs/Equipements/Gobo/Tomshine/20260925_212331.jpg) : la version précédente (roue de couleur/gobo à emplacements,
#    canal Contrôle avec reset 200-209) ne correspondait pas à cet appareil, qui n'a pas de canal de reset séparé.
SHUTTER = [
    cap(0, 7, "Sans fonction (obturateur ouvert, pas de strobe)", "open", "open"),
    cap(8, 99, "Strobe lent → rapide", "progressive", "strobe"), cap(100, 149, "Strobe rapide → lent", "progressive", "strobe"),
    cap(150, 199, "Strobe, effet 1", "progressive", "strobe"), cap(200, 245, "Strobe, effet 2", "progressive", "strobe"),
    cap(246, 255, "Strobe, effet le plus rapide", "progressive", "strobe")]
fixture("Tomshine", "Mini lyre gobo", "movingHead", [
    ch("pan", "Pan (horizontale)", "pan", [cap(0, 255, "Pan 0 → 540°", "progressive", param=("angle", 0, 540, "°"))], resolution="bit16", default=128),
    ch("tilt", "Tilt (inclinaison)", "tilt", [cap(0, 255, "Tilt 0 → 180°", "progressive", param=("angle", 0, 180, "°"))], resolution="bit16", default=128),
    ch("speed", "Vitesse Pan/Tilt", "panTiltSpeed", [cap(0, 255, "Rapide → lente", "progressive", param=("vitesse", 100, 0, "%"))]),
    ch("dim", "Gradateur", "intensity"),
    ch("shutter", "Obturateur / strobe", "shutter", SHUTTER, default=0, rest=0,
       notes="Notice : pas de fermeture séparée, la lyre éclaire dès que le gradateur monte (défaut 0 = pas de strobe)."),
    ch("color", "Roue de couleur", "colorWheel", [
        cap(0, 7, "Sans fonction", "noFunction"),
        cap(8, 255, "Couleur : rotation automatique (emplacements et vitesse à vérifier en direct)", "program")],
       notes="La notice ne détaille pas les emplacements de couleur dans cette plage : à relever avec le mode découverte."),
    ch("gobo", "Motif (roue de gobos)", "gobo", [cap(0, 255, "Sélection / défilement des gobos (emplacements à vérifier en direct)", "program")],
       notes="La notice ne donne aucune plage détaillée pour ce canal : à relever avec le mode découverte."),
    ch("son", "Son", "mode", [
        cap(0, 7, "Sans fonction", "noFunction"),
        cap(8, 199, "Couleur : vitesse de la rotation automatique (à vérifier en direct)", "program"),
        cap(200, 255, "Contrôle par le son")],
       notes="Traduction ambiguë de la notice : ce canal semble régler la vitesse de la rotation automatique de couleur "
             "puis basculer en contrôle par le son au-delà de 200 (à vérifier en direct)."),
    ch("vitesse", "Vitesse (effets)", "programSpeed", [cap(0, 255, "Vitesse des effets (fonction exacte à vérifier en direct)", "progressive")]),
], [mode("9 canaux", "9CH", "CH9", "pan", "tilt", "speed", "dim", "shutter", "color", "gobo", "son", "vitesse"),
    mode("11 canaux", "11CH", "CH11", "pan", fine("pan"), "tilt", fine("tilt"), "speed", "dim", "shutter", "color", "gobo", "son", "vitesse")],
    "Mini lyre à gobos, LED blanche. Faisceau 11°, Pan 540°, Tilt 180°. En 11 canaux, Pan et Tilt sont en 16 bits. "
    "Pas de canal de reset séparé sur cet appareil (à la différence d'autres lyres) : aucune protection de type "
    "« Reset » n'est nécessaire pour ce modèle.",
    {"sourceType": "LED blanche", "beamAngle": 11, "panRange": 540, "tiltRange": 180},
    EQ + "Gobo/81ej8MIZpBL.pdf")

# 5. BeamZ BUV463 (UV)
fixture("BeamZ", "BUV463", "uv", [
    ch("dim", "Maître", "intensity"),
    *[ch("uv%d" % i, "LED UV rangée %d" % i, "uv", cell=i) for i in range(1, 5)],
    ch("strobe", "Strobe / vitesse", "shutter", [
        cap(0, 0, "Pas de strobe", "open", "open"),
        cap(1, 255, "Strobe lent → rapide (vitesse des programmes en auto)", "progressive", "strobe", PCT)]),
    ch("auto", "Programmes", "program", [
        cap(0, 50, "Sans fonction", "noFunction"), cap(51, 100, "Programme auto 1", "program"), cap(101, 150, "Programme auto 2", "program"),
        cap(151, 200, "Programme auto 3", "program"), cap(201, 255, "Contrôle par le son", "program")]),
], [mode("7 canaux", "7CH", "A001", "dim", "uv1", "uv2", "uv3", "uv4", "strobe", "auto")],
    "Projecteur UV 24 × 3 W (85 W), 4 rangées de LED pilotables (cellules 1 à 4).",
    {"sourceType": "LED UV", "power": 85}, EQ + "UV/BeamZ/BUV463/BeamZ-BUV463.pdf")

# 6. WZYBUTA Moving Head 150 W — d'après la définition ScanLibrary (Daslight) de l'utilisateur (captures dans
#    docs/Equipements/WZYBUTA/…, 2026-09-25) : mode 1 = 20 canaux, mode 2 = 64 canaux. 4 barrettes amovibles
#    (Tilt 1-4) de 3 projecteurs RGBW chacune = 12 cellules en 64 canaux. La notice (tableau 16 canaux) ne correspond pas.
WZ_EFFECT = [cap(0, 5, "Sans fonction", "noFunction"), cap(6, 128, "Effets automatiques 01-04", "program"),
             cap(129, 255, "Effets au son", "program")]
WZ_MOTOR = [cap(0, 15, "Sans fonction", "noFunction"), cap(16, 128, "Effets automatiques des moteurs 01-04", "program"),
            cap(129, 255, "Effets des moteurs au son", "program")]
WZ_LASER = "Commande du laser optionnel, absent de l'appareil du parc : inutilisé."
fixture("WZYBUTA", "Effet 4 têtes 150 W", "effect", [
    ch("pan", "Pan (plateau)", "pan", notes="Position du plateau ; amplitude inconnue."),
    ch("panmotor", "Rotation continue du plateau", "panContinuous", [
        cap(0, 85, "Sans rotation", "noFunction"),
        cap(86, 171, "Rotation sens 1, vitesse variable", "progressive", param=("vitesse", 0, 100, "%")),
        cap(172, 255, "Rotation sens 2, vitesse variable", "progressive", param=("vitesse", 0, 100, "%"))],
       notes="ScanLibrary : « Rotation of X Motor 1 / 2 ». Sens de variation de la vitesse à vérifier."),
    *[ch("tilt%d" % i, "Tilt barrette %d" % i, "tilt", notes="Barrette %d = projecteurs %d à %d." % (i, 3 * i - 2, 3 * i))
      for i in range(1, 5)],
    ch("speed", "Vitesse Pan / Tilt", "panTiltSpeed"),
    ch("dim", "Gradateur maître", "intensity"),
    ch("strobe", "Strobe", "shutter", [cap(0, 9, "Pas de strobe", "open", "open"),
                                       cap(10, 249, "Strobe lent → rapide", "progressive", "strobe"),
                                       cap(250, 255, "Pas de strobe", "open", "open")]),
    ch("effect", "Effets des LED", "program", WZ_EFFECT),
    ch("motoreffect", "Effets des moteurs", "program", WZ_MOTOR),
    ch("effectspeed", "Vitesse des effets / sensibilité au son", "programSpeed"),
    ch("r", "Rouge (tous les projecteurs)", "red"), ch("g", "Vert (tous les projecteurs)", "green"),
    ch("b", "Bleu (tous les projecteurs)", "blue"), ch("w", "Blanc (tous les projecteurs)", "white"),
    *[c for n in range(1, 13) for c in (
        ch("r%d" % n, "Rouge %d" % n, "red", cell=n), ch("g%d" % n, "Vert %d" % n, "green", cell=n),
        ch("b%d" % n, "Bleu %d" % n, "blue", cell=n), ch("w%d" % n, "Blanc %d" % n, "white", cell=n))],
    *[ch("x%d" % n, "Laser %d (option absente)" % n, "noFunction", notes=WZ_LASER) for n in range(1, 4)],
    ch("reset", "Reset", "reset", [cap(0, 250, "Sans fonction", "noFunction"), cap(251, 255, "Reset")],
       notes="Ne jamais animer : 251-255 = reset de l'appareil."),
], [mode("20 canaux", "20CH", "mode 1 (réglage du menu à noter)", "pan", "panmotor", "tilt1", "tilt2", "tilt3", "tilt4", "speed",
         "dim", "strobe", "effect", "motoreffect", "effectspeed", "r", "g", "b", "w", "x1", "x2", "x3", "reset"),
    mode("64 canaux", "64CH", "mode 2 (réglage du menu à noter)", "pan", "panmotor", "tilt1", "tilt2", "tilt3", "tilt4", "speed",
         "dim", "strobe", "effect", "motoreffect", "effectspeed",
         *["%s%d" % (c, n) for n in range(1, 13) for c in "rgbw"], "x1", "x2", "x3", "reset")],
    "Effet « moving head » 150 W : plateau rotatif (Pan + rotation continue), 4 barrettes amovibles inclinables (Tilt 1-4) "
    "de 3 projecteurs RGBW. Mode 20 canaux : couleur commune ; mode 64 canaux : RGBW par projecteur (12 cellules). "
    "Source : définition ScanLibrary (Daslight) de l'utilisateur. Canaux 17-19 (20CH) / 61-63 (64CH) : laser optionnel, absent (inutilisés).",
    {"sourceType": "LED RGBW", "power": 150},
    EQ + "WZYBUTA/WZYBUTA 150W Moving Head LED Party Light User Manual/WZYBUTA 150W Moving Head LED Party Light User Manual.pdf")

# 7. BeamZ LCB803 (barre 80 × RGB 3-en-1) — notice pages 27-29 fournies le 2026-09-25 (Q24).
#    Sections de 6 canaux (gradateur, strobe, R, G, B, programme) ; 2, 4 ou 8 sections selon le mode.
def lcb_section(n, cell):
    s = "" if cell == 0 else "%d" % n
    lbl = "" if cell == 0 else " (section %d)" % n
    return [
        ch("dim" + s, "Gradateur" + lbl, "intensity" if cell == 0 else "cellIntensity", cell=cell),
        ch("strobe" + s, "Strobe" + lbl, "shutter", [cap(0, 255, "Strobe rapide → lent", "progressive", "strobe", ("vitesse", 100, 0, "%"))], cell=cell,
           notes="Notice : « Strobe (Fast to slow) » sans plage d'arrêt : 0 = sans strobe probablement (à vérifier en direct)."),
        ch("r" + s, "Rouge" + lbl, "red", cell=cell), ch("g" + s, "Vert" + lbl, "green", cell=cell), ch("b" + s, "Bleu" + lbl, "blue", cell=cell),
        ch("auto" + s, "Programme auto" + lbl, "program", [cap(0, 255, "Programme auto rapide → lent", "program", param=("vitesse", 100, 0, "%"))], cell=cell,
           notes="Notice : « Auto program (Fast to slow) » : 0 = programme arrêté probablement (à vérifier en direct)."),
    ]


LCB_CHANNELS = [ch("r-3ch", "Rouge (mode 3 canaux)", "red"), ch("g-3ch", "Vert (mode 3 canaux)", "green"), ch("b-3ch", "Bleu (mode 3 canaux)", "blue")]
LCB_CHANNELS += lcb_section(0, 0)
for n in range(1, 9):
    LCB_CHANNELS += lcb_section(n, n)


def lcb_mode(sections):
    keys = [k + str(n) for n in range(1, sections + 1) for k in ("dim", "strobe", "r", "g", "b", "auto")]
    return mode("%d canaux" % (6 * sections), "%dCH" % (6 * sections), "ChNd %dCh + A001" % (6 * sections), *keys)


fixture("BeamZ", "LCB803", "ledBar", LCB_CHANNELS, [
    mode("3 canaux", "3CH", "ChNd 3Ch + A001", "r-3ch", "g-3ch", "b-3ch"),
    mode("6 canaux", "6CH", "ChNd 6Ch + A001", "dim", "strobe", "r", "g", "b", "auto"),
    lcb_mode(2), lcb_mode(4), lcb_mode(8)],
    "Barre LED 80 × RGB 3-en-1 (60 W), 986 mm. Mode choisi par le menu ChNd, adresse par Addr. "
    "12 / 24 / 48 canaux = 2 / 4 / 8 sections indépendantes (cellules). Show de référence : 24 canaux (4 sections) "
    "tient dans la réserve du plan d'adresses (51-74 et 81-104) ; 48 canaux ne tient pas.",
    {"sourceType": "LED RGB 3-en-1", "power": 60},
    EQ + "LED BAR/BeamZ/LCB803/Mode d'emploi BeamZ LCB803 (Français - 32 des pages) - P27.pdf",
    reference="150.561")
