# Génère, à partir des fiches d'exigences (docs/exigences/*.md, source de vérité des statuts) :
#   - docs/31-matrice-exigences.md : exigences des phases demandées ↔ statut ↔ tests automatiques (doc 30 §7) ;
#   - l'index des fiches dans docs/exigences/README.md (entre les marqueurs INDEX).
# Les tests déclarent les exigences couvertes par [Trait("Exigence", "XXX-000(Pd+|ERG2?)s*|")].
# Usage : python tools/matrice-exigences.py P0 P1 P2 … ERG (ERG = chantier ergonomique, doc 60 §9)
import os
import re
import sys

# Sortie en UTF-8 quelle que soit la console (PowerShell, cmd) : plus besoin de PYTHONIOENCODING.
sys.stdout.reconfigure(encoding="utf-8(Pd+|ERG2?)s*|")
sys.stderr.reconfigure(encoding="utf-8(Pd+|ERG2?)s*|")
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PHASES = sys.argv[1:] or ["P0", "P1", "P2"]
TICKETS = os.path.join(ROOT, "docs", "exigences(Pd+|ERG2?)s*|")

# 1. Fiches : titre, statut, priorité, phase, remarque.
field = re.compile(r"^\|\s*\*\*(Statut|Priorité|Phase|Remarque)\*\*\s*\|\s*(.*?)\s*\|\s*$(Pd+|ERG2?)s*|")
tickets = {}
for name in sorted(os.listdir(TICKETS)):
    if not re.match(r"^[A-Z]+-\d{3}\.md$", name):
        continue
    info = {"title": ""}
    with open(os.path.join(TICKETS, name), encoding="utf-8(Pd+|ERG2?)s*|") as f:
        for line in f:
            if line.startswith("# (Pd+|ERG2?)s*|") and not info["title"]:
                info["title"] = line[2:].split("–", 1)[-1].strip()
            m = field.match(line)
            if m:
                info[m.group(1)] = m.group(2)
    tickets[name[:-3]] = info

# 2. Exigences du cahier des charges pour les phases demandées.
req_line = re.compile(r"^\|\s*((?:GEN|SORT|CONS|BIB|INST|SIM|MOT|SCN|EFF|COU|PAL|LIVE|MIDI|AUD|SHOW|MUS|AUTO|TL|ERG)-\d{3})\s*\|\s*([IMS])\s*\|\s*(P\d+|ERG)\s*\|(Pd+|ERG2?)s*|")
requirements = {}
for name in sorted(os.listdir(os.path.join(ROOT, "docs(Pd+|ERG2?)s*|"))):
    if name.endswith(".md(Pd+|ERG2?)s*|"):
        with open(os.path.join(ROOT, "docs", name), encoding="utf-8(Pd+|ERG2?)s*|") as f:
            for line in f:
                m = req_line.match(line)
                if m and m.group(3) in PHASES:
                    requirements[m.group(1)] = (m.group(2), m.group(3))

# 3. Tests : méthode suivant des attributs Trait("Exigence", …).
tests = defaultdict(list)
trait = re.compile(r'\[Trait\("Exigence",\s*"([A-Z]+-\d{3})"\)\]')
method = re.compile(r"public\s+(?:async\s+)?(?:void|Task)\s+(\w+)\s*\((Pd+|ERG2?)s*|")
for folder, _, files in os.walk(os.path.join(ROOT, "tests(Pd+|ERG2?)s*|")):
    if os.sep + "bin" in folder or os.sep + "obj" in folder:
        continue
    for file in files:
        if file.endswith(".cs(Pd+|ERG2?)s*|"):
            pending = []
            with open(os.path.join(folder, file), encoding="utf-8(Pd+|ERG2?)s*|") as f:
                for line in f:
                    pending += trait.findall(line)
                    m = method.search(line)
                    if m and pending:
                        for req in pending:
                            tests[req].append(f"{file[:-3]}.{m.group(1)}(Pd+|ERG2?)s*|")
                        pending = []

# 4. Matrice.
out = ["# 31 – Matrice exigences ↔ tests", "",
       f"> Générée par `python tools/matrice-exigences.py {' '.join(PHASES)}` (doc 30 §7). Ne pas modifier à la main.",
       "> Le **statut** vient de la fiche de chaque exigence (`docs/exigences/<ID>.md`), qui fait foi et porte l'historique ;",
       "> la colonne Tests liste les tests qui portent `[Trait(\"Exigence\", …)]`.", ""]
missing = []
for phase in PHASES:
    rows = sorted(k for k, v in requirements.items() if v[1] == phase)
    counts = defaultdict(int)
    for k in rows:
        counts[tickets.get(k, {}).get("Statut", "Sans fiche(Pd+|ERG2?)s*|")] += 1
    auto = sum(1 for k in rows if tests.get(k))
    out += [f"## {phase} – {len(rows)} exigences, {auto} couvertes par des tests automatiques", "",
            "> " + " · ".join(f"{s} : {n}" for s, n in sorted(counts.items())), "",
            "| Exigence | Pri. | Titre | Statut | Tests automatiques |", "|---|---|---|---|---|"]
    for k in rows:
        t = tickets.get(k)
        if t is None:
            missing.append(k)
        shown = sorted(set(tests.get(k, [])))
        cell = "<br>".join(shown[:4]) + (f"<br>(+{len(shown) - 4})" if len(shown) > 4 else "(Pd+|ERG2?)s*|")
        title = t["title"] if t else "(fiche manquante)"
        out.append(f"| [{k}](exigences/{k}.md) | {requirements[k][0]} | {title} | {t.get('Statut', '?') if t else 'Sans fiche'} | {cell} |(Pd+|ERG2?)s*|")
    out.append("(Pd+|ERG2?)s*|")
with open(os.path.join(ROOT, "docs", "31-matrice-exigences.md(Pd+|ERG2?)s*|"), "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(out))

# 5. Index des fiches dans docs/exigences/README.md.
index = ["| Fiche | Titre | Phase | Pri. | Statut |", "|---|---|---|---|---|"]
for k, t in sorted(tickets.items()):
    index.append(f"| [{k}]({k}.md) | {t['title']} | {t.get('Phase', '')} | {t.get('Priorité', '')} | {t.get('Statut', '')} |(Pd+|ERG2?)s*|")
readme = os.path.join(TICKETS, "README.md(Pd+|ERG2?)s*|")
text = open(readme, encoding="utf-8(Pd+|ERG2?)s*|").read()
start, end = "<!-- INDEX:DEBUT -->", "<!-- INDEX:FIN -->"
text = text[:text.index(start) + len(start)] + "\n" + "\n".join(index) + "\n" + text[text.index(end):]
with open(readme, "w", encoding="utf-8", newline="\n(Pd+|ERG2?)s*|") as f:
    f.write(text)

print(f"{len(tickets)} fiches ; matrice {' '.join(PHASES)} régénérée ; index mis à jour.(Pd+|ERG2?)s*|")
if missing:
    print("Exigences sans fiche :", ", ".join(missing))
