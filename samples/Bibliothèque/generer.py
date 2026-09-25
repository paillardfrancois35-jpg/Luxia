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
        cap(101, 150, "Fondu", "program"), cap(151, 200, "Transition", "program"),
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

# 4. Tomshine mini lyre à gobos (9 / 11 canaux)
SHUTTER = [
    cap(0, 7, "Éteint", "closed", "closed"), cap(8, 15, "Allumé", "open", "open"),
    cap(16, 131, "Strobe lent → rapide", "progressive", "strobe", PCT), cap(132, 139, "Allumé", "open", "open"),
    cap(140, 181, "Fermeture rapide, ouverture lente", "progressive", "pulse"), cap(182, 189, "Allumé", "open", "open"),
    cap(190, 231, "Ouverture rapide, fermeture lente", "progressive", "pulse"), cap(232, 239, "Allumé", "open", "open"),
    cap(240, 247, "Strobe aléatoire", "fixed", "random"), cap(248, 255, "Allumé", "open", "open")]
fixture("Tomshine", "Mini lyre gobo", "movingHead", [
    ch("pan", "Pan", "pan", [cap(0, 255, "Pan 0 → 540°", "progressive", param=("angle", 0, 540, "°"))], resolution="bit16", default=128),
    ch("tilt", "Tilt", "tilt", [cap(0, 255, "Tilt 0 → 180°", "progressive", param=("angle", 0, 180, "°"))], resolution="bit16", default=128),
    ch("color", "Roue de couleur", "colorWheel", [
        cap(0, 127, "Choix de couleur (emplacements à relever)", "wheelSlot"), cap(128, 189, "Rotation rapide → lente", "rotation"),
        cap(190, 193, "Arrêt de la roue"), cap(194, 255, "Rotation lente → rapide", "rotation")],
       notes="Les emplacements de couleur (nombre, couleurs, bornes) sont à relever avec le mode découverte."),
    ch("gobo", "Roue de gobos", "gobo", [
        cap(0, 63, "Choix du gobo (emplacements à relever)", "wheelSlot"), cap(64, 127, "Gobo tremblant"),
        cap(128, 189, "Rotation rapide → lente", "rotation"), cap(190, 193, "Arrêt de la roue"),
        cap(194, 255, "Rotation lente → rapide", "rotation")]),
    ch("shutter", "Obturateur / strobe", "shutter", SHUTTER, rest=0, identify=12),
    ch("dim", "Gradateur", "intensity"),
    ch("speed", "Vitesse Pan/Tilt", "panTiltSpeed", [cap(0, 255, "Rapide → lente", "progressive", param=("vitesse", 100, 0, "%"))]),
    ch("control", "Contrôle", "reset", [
        cap(0, 69, "Sans fonction", "noFunction"), cap(70, 79, "Noir pendant les mouvements X/Y"), cap(80, 89, "Sans fonction", "noFunction"),
        cap(90, 99, "Noir pendant la roue de couleur"), cap(100, 109, "Sans fonction", "noFunction"),
        cap(110, 119, "Noir pendant la roue de gobos"), cap(120, 199, "Sans fonction", "noFunction"), cap(200, 209, "Reset"),
        cap(210, 249, "Sans fonction", "noFunction"), cap(250, 255, "Contrôle par le son")],
       notes="Ne jamais animer : 200-209 = reset de la lyre."),
    ch("mode", "Mode d'effet", "mode", [
        cap(0, 20, "Effets standard"), cap(21, 40, "Effets scène"), cap(41, 60, "Effets TV"), cap(61, 80, "Effets architecture"),
        cap(81, 100, "Effets théâtre"), cap(101, 255, "Standard (défaut)")]),
], [mode("9 canaux", "9CH", "CH9", "pan", "tilt", "color", "gobo", "shutter", "dim", "speed", "control", "mode"),
    mode("11 canaux", "11CH", "CH11", "pan", fine("pan"), "tilt", fine("tilt"), "color", "gobo", "shutter", "dim", "speed", "control", "mode")],
    "Mini lyre à gobos, LED blanche. Faisceau 11°, Pan 540°, Tilt 180°. En 11 canaux, Pan et Tilt sont en 16 bits.",
    {"sourceType": "LED blanche", "beamAngle": 11, "panRange": 540, "tiltRange": 180},
    EQ + "Gobo/Tomshine/81ej8MIZpBL.pdf")

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

# 6. WZYBUTA 150 W, effet 4 têtes (16 canaux) — tableau ambigu, à vérifier (Q25)
fixture("WZYBUTA", "Effet 4 têtes 150 W", "effect", [
    ch("x", "Moteur X (rotation)", "rotation", notes="« X motor running » : rotation du plateau (à vérifier)."),
    *[ch("y%d" % i, "Moteur Y%d (tête %d)" % (i, i), "rotation", cell=i) for i in range(1, 5)],
    ch("master", "Interrupteur général", "intensity", notes="« Master switch » : gradateur ou marche/arrêt (à vérifier)."),
    ch("flash", "Flash des LED", "shutter", [cap(0, 254, "Flash lent → rapide (à vérifier)", "progressive", "strobe"), cap(255, 255, "Reset (« 255 active reset »)")],
       notes="255 = reset d'après la notice."),
    ch("auto", "Programmes des LED", "program", [cap(0, 5, "Sans fonction", "noFunction"), cap(6, 255, "Défilement automatique des LED", "program")]),
    ch("xyauto", "Mouvement automatique XY", "program"),
    ch("autospeed", "Vitesse des programmes", "programSpeed"),
    ch("r", "Rouge", "red", notes="« 16-255 » et « 1-9 brightness » dans la notice : à vérifier."),
    ch("g", "Vert", "green"), ch("b", "Bleu", "blue"),
    ch("w", "Blanc", "white", notes="« 0-127 » dans la notice : à vérifier."),
    ch("blank", "Vide", "noFunction"),
    ch("total", "« Totalise »", "generic", notes="Fonction inconnue (traduction) : à découvrir en direct."),
], [mode("16 canaux", "16CH", "à vérifier sur l'appareil", "x", "y1", "y2", "y3", "y4", "master", "flash", "auto", "xyauto", "autospeed",
         "r", "g", "b", "w", "blank", "total")],
    "Effet à moteur tournant + 4 têtes pivotantes, 150 W. Tableau DMX ambigu (traduction) : définition À VÉRIFIER avec le mode découverte (Q25).",
    {"sourceType": "LED RGBW", "power": 150},
    EQ + "WZYBUTA/WZYBUTA 150W Moving Head LED Party Light User Manual/WZYBUTA 150W Moving Head LED Party Light User Manual.pdf")
