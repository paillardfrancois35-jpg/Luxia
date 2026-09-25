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

# 3. Matrice.
out = ["# 31 – Matrice exigences ↔ tests", "",
       f"> Générée par `python tools/matrice-exigences.py {' '.join(PHASES)}` (doc 30 §7). Ne pas modifier à la main.",
       "> « Automatique » : au moins un test référence l'exigence. « Manuel » : vérification par le guide de démonstration",
       "> de la phase (docs/demos) ou sur le matériel. Le statut « validé » est donné par l'utilisateur lors de la livraison.", ""]
for phase in PHASES:
    rows = sorted((k, v) for k, v in requirements.items() if v[1] == phase)
    auto = sum(1 for k, _ in rows if tests.get(k))
    out += [f"## {phase} – {len(rows)} exigences, {auto} couvertes par des tests automatiques", "",
            "| Exigence | Pri. | Énoncé (début) | Couverture | Tests |", "|---|---|---|---|---|"]
    for key, (prio, _, text, doc) in rows:
        t = sorted(set(tests.get(key, [])))
        cover = "Automatique" if t else "Manuel"
        shown = "<br>".join(t[:4]) + (f"<br>(+{len(t) - 4})" if len(t) > 4 else "")
        out.append(f"| {key} | {prio} | {text.replace('|', '/')} | {cover} | {shown} |")
    out.append("")

path = os.path.join(ROOT, "docs", "31-matrice-exigences.md")
with open(path, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(out))
print(path)
for phase in PHASES:
    rows = [k for k, v in requirements.items() if v[1] == phase]
    print(phase, len(rows), "exigences,", sum(1 for k in rows if tests.get(k)), "automatiques")
