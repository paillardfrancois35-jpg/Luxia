"""Fabrique « samples/Démo Contrôle » : le show de référence, plus de quoi essayer l'écran Contrôle (doc 60, ERG-020).

Usage (depuis la racine du dépôt) :
  PYTHONIOENCODING=utf-8 python tools/generer-demo-controle.py

Le dossier produit est une copie de travail (ignorée par Git, comme « Show de travail ») : on peut y écrire sans
risque, et le régénérer quand on veut. Le show de référence n'est jamais modifié.

Ajouts par rapport au show de référence :
  - couche « Libre » (8e couche par défaut, ERG-008) : « Effet multi-têtes seul », « Barre 1 : arc-en-ciel » ;
  - couche Couleurs : « Accueil ambre / bleu », deux étapes nommées ;
  - couche Mouvements : « Balayage doux des lyres », quatre étapes nommées réglées en Pan / Tilt ;
  - lieu Générique : une zone **permise** « Limites » sur chaque lyre (F7), en plus de la zone interdite « Public » ;
  - looks (ERG-023) : « Temps mort », « Retour de piste », « Ambiance UV ».
"""
import json
import os
import shutil
import uuid

ROOT = os.getcwd()
SOURCE = os.path.join(ROOT, "samples", "Show de référence")
TARGET = os.path.join(ROOT, "samples", "Démo Contrôle")
NS = uuid.UUID("7c1a0001-0000-4000-8000-00000000d3e0")

FREE = "7c1a0001-0000-4000-8000-000000000007"
COLORS = "7c1a0001-0000-4000-8000-000000000002"
MOVEMENTS = "7c1a0001-0000-4000-8000-000000000003"
PAL = {"Ambre": "9a1e0001-0000-4000-8000-000000000005", "Bleu": "9a1e0001-0000-4000-8000-000000000009"}
FX = {
    "PAR 1": "743c5068-fe12-5d88-980c-db6cf1fe5ef8",
    "PAR 2": "2088fa7c-c3d0-5387-9ee3-7f5a445dea62",
    "PAR 3": "242c361d-79e2-54c4-9d00-6617e4e1d139",
    "PAR 4": "268997d7-d91d-5ff2-9e0e-6ba60b5aedba",
    "Barre 1": "6c3d353d-218e-5902-b165-b31626a27f7d",
    "Lyre 1": "c58c8e40-21f6-5d28-80bf-dc4230557bf0",
    "Lyre 2": "d8aa4fab-b25f-5a75-927f-65381d097b08",
    "Effet": "a1e1a6f1-5746-5c85-9aaa-3f622e9ade19",
}


def sid(name):
    return str(uuid.uuid5(NS, name))


def dur(seconds):
    return {"value": seconds, "unit": "seconds"}


def fixture(name, cell=0):
    return {"fixtureId": FX[name], "cell": cell}


def step(name, fade, hold, values):
    return {"name": name, "fade": dur(fade), "hold": dur(hold), "curve": "linear", "switch": "start", "values": values}


def scene(name, layer, color, notes, steps):
    return {
        "id": sid(name), "name": name, "color": color, "category": "Démo Contrôle", "notes": notes,
        "visibleInLive": True, "layerId": layer, "loop": "infinite", "loopCount": 1, "end": "stop", "speed": 1,
        "steps": steps,
    }


def rgb(r, g, b):
    return {"r": r, "g": g, "b": b}


def main():
    if os.path.exists(TARGET):
        shutil.rmtree(TARGET)
    shutil.copytree(SOURCE, TARGET, ignore=shutil.ignore_patterns("Enregistrements"))

    project = os.path.join(TARGET, "projet.json")
    data = json.load(open(project, encoding="utf-8"))
    data["name"] = "Démo Contrôle"
    json.dump(data, open(project, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

    scenes_path = os.path.join(TARGET, "scènes.json")
    scenes = json.load(open(scenes_path, encoding="utf-8"))
    rainbow = [(1, 0, 0), (1, 1, 0), (0, 1, 1), (0.5, 0, 1)]
    scenes["scenes"] += [
        scene("Effet multi-têtes seul", FREE, "#3FB950",
              "Couche Libre : un appareil piloté à part (l'effet), à 100 %, sans toucher aux autres couches.",
              [step("Allumé", 0.5, 1, [{"target": fixture("Effet"), "attribute": "intensity", "level": 1.0},
                                      {"target": fixture("Effet"), "color": rgb(1, 1, 1)}])]),
        scene("Barre 1 : arc-en-ciel", FREE, "#DB61A2",
              "Couche Libre : une couleur par cellule de la barre 1 (4 cellules, chacune avec son gradateur).",
              [step("Arc-en-ciel", 1, 2, [{"target": fixture("Barre 1", i + 1), "color": rgb(*c)} for i, c in enumerate(rainbow)]
                    + [{"target": fixture("Barre 1", i + 1), "attribute": "cellIntensity", "level": 1.0} for i in range(len(rainbow))])]),
        scene("Accueil ambre / bleu", COLORS, "#FFB000",
              "Deux étapes nommées de 3 s avec fondu de 1 s : les PAR passent de l'ambre au bleu. À éditer en ÉDITION.",
              [
                  step("Ambre", 1, 2, [{"target": fixture(p), "paletteId": PAL["Ambre"]} for p in ("PAR 1", "PAR 2", "PAR 3", "PAR 4")]
                       + [{"target": fixture(p), "attribute": "intensity", "level": 1.0} for p in ("PAR 1", "PAR 2", "PAR 3", "PAR 4")]),
                  step("Bleu", 1, 2, [{"target": fixture(p), "paletteId": PAL["Bleu"]} for p in ("PAR 1", "PAR 2", "PAR 3", "PAR 4")]
                       + [{"target": fixture(p), "attribute": "intensity", "level": 1.0} for p in ("PAR 1", "PAR 2", "PAR 3", "PAR 4")]),
              ]),
        scene("Balayage doux des lyres", MOVEMENTS, "#58A6FF",
              "Quatre positions nommées réglées en Pan / Tilt (fondu 2 s) : à retoucher sur la grille Pan / Tilt en ÉDITION.",
              [
                  step(n, 2, 1, [
                      {"target": fixture("Lyre 1"), "attribute": "pan", "level": p1},
                      {"target": fixture("Lyre 1"), "attribute": "tilt", "level": t},
                      {"target": fixture("Lyre 2"), "attribute": "pan", "level": p2},
                      {"target": fixture("Lyre 2"), "attribute": "tilt", "level": t},
                      {"target": fixture("Lyre 1"), "attribute": "intensity", "level": 1.0},
                      {"target": fixture("Lyre 2"), "attribute": "intensity", "level": 1.0},
                  ])
                  for n, p1, p2, t in (("Gauche", 0.35, 0.45, 0.55), ("Centre", 0.45, 0.55, 0.6), ("Droite", 0.55, 0.65, 0.55), ("Haut", 0.45, 0.55, 0.75))
              ]),
    ]
    json.dump(scenes, open(scenes_path, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

    venues_path = os.path.join(TARGET, "lieux.json")
    venues = json.load(open(venues_path, encoding="utf-8"))
    for venue in venues["venues"]:
        zones = venue.setdefault("forbiddenZones", [])
        for lyre in ("Lyre 1", "Lyre 2"):
            zones.append({"fixtureId": FX[lyre], "name": "Limites", "panMin": 0.1, "panMax": 0.9, "tiltMin": 0.2, "tiltMax": 0.95, "allowed": True})
    json.dump(venues, open(venues_path, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

    by_name = {x["name"]: x["id"] for x in scenes["scenes"]}

    def launch(name):
        return {"kind": "launchScene", "sceneId": by_name[name]}

    looks = {"formatVersion": 1, "looks": [
        {"id": sid("look Temps mort"), "name": "Temps mort", "color": "#FFB000",
         "notes": "Pause, discours : tout s'arrête, lumière ambre douce à 40 %.",
         "actions": [{"kind": "stopAll"}, launch("Plein feu"), launch("Ambre – couleur seule"), {"kind": "grandMaster", "level": 0.4}]},
        {"id": sid("look Retour de piste"), "name": "Retour de piste", "color": "#DB61A2",
         "notes": "La musique repart : chenillard, lyres en mouvement, pleine intensité.",
         "actions": [{"kind": "stopAll"}, launch("Plein feu"), launch("Chenillard 4 couleurs"), launch("Lyres sur 3 positions"), {"kind": "grandMaster", "level": 1.0}]},
        {"id": sid("look Ambiance UV"), "name": "Ambiance UV", "color": "#8957E5",
         "notes": "Tout s'arrête sauf l'ambiance ; UV seuls.",
         "actions": [{"kind": "stopAll"}, launch("UV plein"), {"kind": "grandMaster", "level": 1.0}]},
    ]}
    json.dump(looks, open(os.path.join(TARGET, "looks.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=2)

    with open(os.path.join(TARGET, "JOURNAL.md"), "w", encoding="utf-8") as f:
        f.write("# Démo Contrôle\n\nCopie de travail fabriquée par `tools/generer-demo-controle.py` (show de référence + démonstration de "
                "l'écran Contrôle). À régénérer à volonté ; guide : `docs/demos/ERG-controle.md`.\n")
    print("Fabriqué :", TARGET)


if __name__ == "__main__":
    main()
