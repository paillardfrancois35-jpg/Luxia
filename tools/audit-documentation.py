"""Contrôle de la documentation (revue de fin de P7) : liens relatifs des documents, fiches d'exigences (tests et fichiers cités qui existent,
historique chronologique, statut Validé avec son entrée de validation). À lancer depuis n'importe où : python tools/audit-documentation.py
(les faux positifs connus : GEN-030 et GEN-031 — dates hors ordre dans l'historique ; GEN-115 — LuXia.exe cité comme test)."""
import os, re, glob, sys, subprocess
sys.stdout.reconfigure(encoding='utf-8')
os.chdir(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))

# --- 1. Liens relatifs des documents
broken = []
for f in glob.glob('docs/**/*.md', recursive=True) + ['README.md']:
    if not os.path.exists(f):
        continue
    text = open(f, encoding='utf-8').read()
    base = os.path.dirname(f)
    for m in re.finditer(r'\]\(([^)#\s]+)(#[^)]*)?\)', text):
        t = m.group(1)
        if t.startswith(('http', 'mailto')):
            continue
        t = t.replace('%20', ' ')
        path = os.path.normpath(os.path.join(base, t))
        if not os.path.exists(path):
            broken.append((f, t))
print('LIENS CASSÉS :', len(broken))
for b in broken[:60]:
    print('  ', b)

# --- 2. Fiches
test_text = ''
for f in glob.glob('tests/**/*.cs', recursive=True):
    if '/obj/' in f.replace('\\', '/') or '/bin/' in f.replace('\\', '/'):
        continue
    test_text += open(f, encoding='utf-8', errors='replace').read() + '\n'
src_files = set()
problems = []
stats = {}
for f in sorted(glob.glob('docs/exigences/*.md')):
    if f.endswith('README.md'):
        continue
    s = open(f, encoding='utf-8').read()
    rid = os.path.basename(f)[:-3]
    st = re.search(r'\| \*\*Statut\*\* \| (.*) \|', s)
    ph = re.search(r'\| \*\*Phase\*\* \| (.*) \|', s)
    status = st.group(1) if st else '?'
    stats.setdefault(status, []).append(rid)
    # tests cités
    sec = re.search(r'## Tests\n(.*?)(\n## |\Z)', s, re.S)
    if sec:
        for m in re.finditer(r'`([A-Za-z0-9_]+)\.([A-Za-z0-9_]+)`', sec.group(1)):
            cls, meth = m.groups()
            if not re.search(r'\b' + meth + r'\b', test_text):
                problems.append((rid, 'test introuvable', f'{cls}.{meth}'))
    # fichiers cités
    sec = re.search(r'## Réalisation\n(.*?)(\n## |\Z)', s, re.S)
    if sec:
        for m in re.finditer(r'`((?:src|tools|tests|samples|docs)/[^`]+)`', sec.group(1)):
            p = m.group(1)
            if '*' in p:
                continue
            if not os.path.exists(p):
                problems.append((rid, 'fichier introuvable', p))
    # historique
    hist = re.search(r'## Historique\n(.*?)(\n## |\Z)', s, re.S)
    rows = [r for r in (hist.group(1).split('\n') if hist else []) if r.startswith('| 20')]
    dates = [r.split('|')[1].strip() for r in rows]
    if dates != sorted(dates):
        problems.append((rid, 'historique non chronologique', ''))
    types = [r.split('|')[3].strip() for r in rows]
    if status == 'Validé' and 'Validation' not in types and not re.search(r'[Vv]alidé', ' '.join(rows)):
        problems.append((rid, 'Validé sans entrée Validation', ''))
    if status in ('Réalisé', 'Validé', 'Partiel') and 'Développement' not in types and 'Création' not in types and 'Livraison' not in types:
        problems.append((rid, 'sans entrée Développement', status))
    if not rows:
        problems.append((rid, 'historique vide', ''))
print()
print('STATUTS :', {k: len(v) for k, v in stats.items()})
print('PROBLÈMES DE FICHES :', len(problems))
for p in problems:
    print('  ', p)
print()
for k in ('Réalisé, à valider sur matériel', 'Partiel', 'En cours'):
    print(k, stats.get(k))
