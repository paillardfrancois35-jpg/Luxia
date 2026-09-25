# Génère docs/31-matrice-exigences.md (doc 30 §7) : exigences des phases demandées ↔ tests automatiques.
# Les tests déclarent les exigences couvertes par [Trait("Exigence", "XXX-000")].
# Usage : python tools/matrice-exigences.py P0 P1 P2
import os
import re
import sys
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PHASES = sys.argv[1:] or ["P0", "P1", "P2"]

# 1. Exigences : lignes de tableau « | ID | Pri. | Phase | Exigence | Critère | ».
req_line = re.compile(r"^\|\s*((?:GEN|SORT|CONS|BIB)-\d{3})\s*\|\s*([IMS])\s*\|\s*(P\d+)\s*\|\s*(.+?)\s*\|")
requirements = {}
for name in sorted(os.listdir(os.path.join(ROOT, "docs"))):
    if not name.endswith(".md"):
        continue
    with open(os.path.join(ROOT, "docs", name), encoding="utf-8") as f:
        for line in f:
            m = req_line.match(line)
            if m and m.group(3) in PHASES:
                text = m.group(4)
                requirements[m.group(1)] = (m.group(2), m.group(3), text[:90] + ("…" if len(text) > 90 else ""), name)

# 2. Tests : méthode suivant des attributs Trait("Exigence", …).
tests = defaultdict(list)
trait = re.compile(r'\[Trait\("Exigence",\s*"([A-Z]+-\d{3})"\)\]')
method = re.compile(r"public\s+(?:async\s+)?(?:void|Task)\s+(\w+)\s*\(")
for folder, _, files in os.walk(os.path.join(ROOT, "tests")):
    if os.sep + "bin" in folder or os.sep + "obj" in folder:
        continue
    for file in files:
        if not file.endswith(".cs"):
            continue
        pending = []
        with open(os.path.join(folder, file), encoding="utf-8") as f:
            for line in f:
                pending += trait.findall(line)
                m = method.search(line)
                if m and pending:
                    for req in pending:
                        tests[req].append(f"{file[:-3]}.{m.group(1)}")
                    pending = []

# 2 bis. Statut de réalisation, tenu à la main (à mettre à jour à chaque livraison).
#   Sans entrée : « Réalisé » si un test automatique existe, sinon « À vérifier ».
#   Valeurs : Réalisé · Réalisé, à valider sur matériel · Partiel · Reporté (Pn) · Non réalisé
STATUS = {
    "GEN-002": ("Réalisé", "Revue : toute action passe par une commande (test de sortie = CMD-024, console = CMD-020/022)."),
    "GEN-030": ("Réalisé, à valider sur matériel", "Mesure 1 h à refaire, veille du PC désactivée (`dmx-headless gigue --duree 3600`)."),
    "GEN-031": ("Réalisé, à valider sur matériel", "p99 0,8-0,9 ms sur 25 min ; mesure 1 h à refaire."),
    "GEN-080": ("Réalisé, à valider sur matériel", "Chien de garde 2 s du firmware (T-SORT-06) ; firmware à téléverser."),
    "GEN-081": ("Réalisé, à valider sur matériel", "Idem GEN-080 (tuer le processus)."),
    "GEN-091": ("Réalisé, à valider sur matériel", "Testé avec faux port série ; T-SORT-05 sur matériel."),
    "GEN-110": ("Réalisé", "Serilog, Documents\\DMX\\Journaux, 5 Mo × 20 fichiers."),
    "GEN-120": ("Réalisé", "Revue : aucune fonction n'utilise le réseau."),
    "SORT-010": ("Réalisé, à valider sur matériel", "T-SORT-04 (3 ports USB). Sonde les cartes Arduino et le dernier port ; option « tous les ports »."),
    "SORT-013": ("Réalisé, à valider sur matériel", "T-SORT-05 (10 débranchements)."),
    "SORT-023": ("Réalisé", "Durée d'écriture et trames/s affichées dans l'écran Sorties."),
    "SORT-040": ("Réalisé, à valider sur matériel", "Firmware compilé (DMXSerial), non téléversé."),
    "SORT-041": ("Réalisé, à valider sur matériel", "Firmware : tous canaux à 0 au démarrage."),
    "SORT-042": ("Réalisé, à valider sur matériel", "Firmware : chien de garde 2 s."),
    "SORT-043": ("Réalisé, à valider sur matériel", "Protocole testé côté PC ; firmware à téléverser."),
    "SORT-044": ("Réalisé", "Revue du firmware : tampon unique de 513 octets, pas d'allocation."),
    "SORT-045": ("Réalisé, à valider sur matériel", "Firmware : application à la réception de 0xE7 seulement."),
    "SORT-046": ("Réalisé, à valider sur matériel", "LED : clignote / fixe / éteinte."),
    "SORT-047": ("Réalisé", "Constante FW_VERSION (1.0) renvoyée par les labels 3 et 77."),
    "SORT-048": ("Réalisé", "firmware/arduino-dmx + README (bibliothèques, cavaliers, commandes)."),
    "SORT-049": ("Réalisé, à valider sur matériel", "maxChannel = canaux reçus (24 minimum)."),
    "CONS-007": ("Reporté (P3)", "Nom d'appareil / attribut / plage : nécessite le patch."),
    "CONS-008": ("Reporté (P4-P5)", "Blackout (P4) et limites de sûreté (P5) pas encore implémentés ; TODO dans RenderEngine."),
    "CONS-009": ("Réalisé", "Liste des univers configurés (un seul aujourd'hui)."),
    "CONS-040": ("Réalisé", "OutputMonitor, 512 cases, valeurs affichables."),
    "CONS-043": ("Partiel", "Canaux surchargés marqués ; délimitation par appareil en P3."),
    "CONS-044": ("Non réalisé", "Priorité S."),
    "GEN-100": ("Réalisé", "Interface en français."),
    "GEN-101": ("Réalisé", "Thème sombre Fluent, panneaux sombres."),
    "GEN-104": ("Partiel", "Sortie, trames/s et enregistrement affichés ; blackout (P4) et mode auto (P10) affichés « — »."),
    "GEN-107": ("Partiel", "Glisser-déposer des positions d'un mode ; patch, plan, couches à venir (P3+)."),
    "GEN-108": ("Non réalisé", "Priorité S."),
    "GEN-105": ("Réalisé", "Recherche et filtres dans la bibliothèque (seule liste > 20 éléments à ce jour)."),
    "GEN-058": ("Non réalisé", "Priorité S : chemins de notice des modèles d'exemple relatifs au dépôt."),
    "BIB-001": ("Réalisé", "Parc décrit sauf barre LCB803 (notice incomplète, Q24) ; WZYBUTA à vérifier (Q25)."),
    "BIB-027": ("Partiel", "Chemin de la notice saisissable ; ouverture de la notice / photo non réalisée (S)."),
    "BIB-060": ("Réalisé, à valider sur matériel", "T-BIB-05 : vérification plage par plage de chaque appareil."),
    "BIB-061": ("Réalisé, à valider sur matériel", "T-BIB-05."),
    "BIB-080": ("Réalisé", "Testé sur 5 fichiers rédigés au format OFL ; à confirmer avec de vrais fichiers téléchargés."),
    "BIB-081": ("Réalisé", "Testé sur 3 fichiers rédigés au format QLC+ ; à confirmer avec de vrais fichiers."),
    "BIB-084": ("Non réalisé", "Priorité S (export OFL)."),
}

# 3. Matrice.
out = ["# 31 – Matrice exigences ↔ tests", "",
       f"> Générée par `python tools/matrice-exigences.py {' '.join(PHASES)}` (doc 30 §7). Ne pas modifier à la main.",
       "> Statut : « Réalisé » (fait, testé automatiquement ou revu), « Réalisé, à valider sur matériel », « Partiel »,",
       "> « Reporté (Pn) », « Non réalisé ». Le statut est tenu dans le dictionnaire STATUS du script ; la colonne Tests liste",
       "> les tests qui portent [Trait(\"Exigence\", …)]. La validation finale est donnée par l'utilisateur à la livraison.", ""]
for phase in PHASES:
    rows = sorted((k, v) for k, v in requirements.items() if v[1] == phase)
    auto = sum(1 for k, _ in rows if tests.get(k))
    counts = defaultdict(int)
    for k, _ in rows:
        counts[STATUS.get(k, ("Réalisé" if tests.get(k) else "À vérifier", ""))[0]] += 1
    summary = " · ".join(f"{name} : {n}" for name, n in sorted(counts.items()))
    out += [f"## {phase} – {len(rows)} exigences, {auto} couvertes par des tests automatiques", "", f"> {summary}", "",
            "| Exigence | Pri. | Énoncé (début) | Statut | Remarque | Tests automatiques |", "|---|---|---|---|---|---|"]
    for key, (prio, _, text, doc) in rows:
        t = sorted(set(tests.get(key, [])))
        status, remark = STATUS.get(key, ("Réalisé" if t else "À vérifier", ""))
        shown = "<br>".join(t[:4]) + (f"<br>(+{len(t) - 4})" if len(t) > 4 else "")
        out.append(f"| {key} | {prio} | {text.replace('|', '/')} | {status} | {remark} | {shown} |")
    out.append("")

path = os.path.join(ROOT, "docs", "31-matrice-exigences.md")
with open(path, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(out))
print(path)
for phase in PHASES:
    rows = [k for k, v in requirements.items() if v[1] == phase]
    print(phase, len(rows), "exigences,", sum(1 for k in rows if tests.get(k)), "automatiques")
