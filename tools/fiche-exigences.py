"""Création / mise à jour des fiches d'exigences (docs/exigences).

Usage (depuis la racine du dépôt), puis régénérer la matrice (tools/matrice-exigences.py) :
  python tools/fiche-exigences.py spec.json
L'historique d'une fiche est en ajout seul ; « statut » remplace le statut courant.
spec.json : liste d'objets
  {"id": "MOT-080", "statut": "Réalisé", "remarque": "...", "liens": "...",
   "realisation": ["..."], "tests": ["..."], "historique": [["2026-09-26","Claude","Développement","..."]],
   "titre": "(facultatif, sinon tiré du cahier des charges)", "creation": "(date de rédaction, facultative)"}
"""
import glob
import json
import os
import re
import sys

# Sortie en UTF-8 quelle que soit la console (PowerShell, cmd) : plus besoin de PYTHONIOENCODING.
sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")

ROOT = os.getcwd()
DOCS = os.path.join(ROOT, "docs")
EXI = os.path.join(DOCS, "exigences")


def find_requirement(rid):
    for path in sorted(glob.glob(os.path.join(DOCS, "*.md"))):
        name = os.path.basename(path)
        if name.startswith(("31-", "32-", "01-", "99-")):
            continue
        section = None
        with open(path, encoding="utf-8") as f:
            for line in f:
                if line.startswith("#"):
                    section = line.strip("# \n")
                if line.startswith(f"| {rid} |"):
                    cells = [c.strip() for c in line.strip().strip("|").split("|")]
                    return name, section, cells
    return None, None, None


def short_title(text):
    t = re.sub(r"\*\*|`", "", text)
    t = re.split(r"[:;.(]", t)[0].strip()
    return t[:90]


def create(spec):
    rid = spec["id"]
    name, section, cells = find_requirement(rid)
    if cells is None:
        raise SystemExit(f"{rid} introuvable dans les docs")
    if rid.startswith(("CMD-", "EVT-")):
        prio, phase = "I", cells[-1]
        text = f"Commande {cells[1]} ({cells[2]}), traitée par : {cells[3]}." if rid.startswith("CMD-") else f"Événement {cells[1]}, publié par {cells[2]}, reçu par {cells[3]}."
        crit = "—"
    else:
        prio, phase, text, crit = cells[1], cells[2], cells[3], cells[4] if len(cells) > 4 else "—"
    title = spec.get("titre") or short_title(text)
    anchor = section or ""
    lines = [
        f"# {rid} – {title}", "",
        "| Champ | Valeur |", "|---|---|",
        f"| **Statut** | {spec.get('statut', 'En cours')} |",
        f"| **Priorité** | {prio} |",
        f"| **Phase** | {phase} |",
        f"| **Source** | [doc {name[:2]} – {anchor}](../{name}) |",
        f"| **Remarque** | {spec.get('remarque', '—')} |",
        f"| **Liens** | {spec.get('liens', '—')} |", "",
        "## Description", "", f"> {text}", "",
        f"**Critère d'acceptation** : {crit}", "",
        "## Réalisation", "",
    ]
    lines += [f"- {r}" for r in spec.get("realisation", [])] or ["- —"]
    lines += ["", "## Tests", ""]
    lines += [f"- {t}" for t in spec.get("tests", [])] or ["- —"]
    lines += ["", "## Historique", "", "| Date | Par | Type | Entrée |", "|---|---|---|---|",
              f"| {spec.get('creation', '2026-09-24')} | Conception | Création | Exigence rédigée au cahier des charges (doc {name[:2]}, {anchor}). |"]
    lines += [f"| {d} | {p} | {t} | {e} |" for d, p, t, e in spec.get("historique", [])]
    return "\n".join(lines) + "\n"


def update(path, spec):
    s = open(path, encoding="utf-8").read()
    if "statut" in spec:
        s = re.sub(r"\| \*\*Statut\*\* \| .* \|", f"| **Statut** | {spec['statut']} |", s, count=1)
    if "remarque" in spec:
        s = re.sub(r"\| \*\*Remarque\*\* \| .* \|", lambda m: f"| **Remarque** | {spec['remarque']} |", s, count=1)
    if "liens" in spec:
        s = re.sub(r"\| \*\*Liens\*\* \| .* \|", lambda m: f"| **Liens** | {spec['liens']} |", s, count=1)
    for key, header in (("realisation", "## Réalisation"), ("tests", "## Tests")):
        items = spec.get(key, [])
        if not items:
            continue
        start = s.index(header)
        end = s.index("\n## ", start + 1)
        block = s[start:end].rstrip("\n")
        block = block.replace("\n- —", "")
        for item in items:
            if f"- {item}" not in block:
                block += f"\n- {item}"
        s = s[:start] + block + "\n" + s[end:]
    rows = "".join(f"| {d} | {p} | {t} | {e} |\n" for d, p, t, e in spec.get("historique", []))
    s = s.rstrip("\n") + "\n" + rows
    return s


def main():
    specs = json.load(open(sys.argv[1], encoding="utf-8"))
    for spec in specs:
        path = os.path.join(EXI, spec["id"] + ".md")
        if os.path.exists(path):
            content = update(path, spec)
            action = "mise à jour"
        else:
            content = create(spec)
            action = "créée"
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(content)
        print(spec["id"], action)


if __name__ == "__main__":
    main()
