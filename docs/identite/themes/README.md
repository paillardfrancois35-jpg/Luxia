# Identités thématiques de LuXia

Variantes saisonnières et festives de l'identité de référence ([../README.md](../README.md)) : même dessin (le X formé
d'un faisceau « lumière » et d'un faisceau « IA » qui se croisent), autres couleurs, décors en plus. Créées le
2026-09-28 à la demande de l'utilisateur, **pour le plaisir : rien n'est appliqué à l'exécutable**.

| Thème | Dossier | L'idée |
|---|---|---|
| Halloween | [halloween/](halloween/) | Faisceau lumière orange citrouille, faisceau IA vert poison → violet, lune, chauves-souris, citrouilles, toile d'araignée. |
| Noël | [noel/](noel/) | Lumière dorée (guirlande), IA vert sapin → rouge, étoile au croisement, bonnet sur le projecteur, neige, houx. |
| Mariage | [mariage/](mariage/) | Fond ivoire clair, or rose et lavande perlée, deux alliances entrelacées au croisement, cœurs et pétales. |
| Anniversaire | [anniversaire/](anniversaire/) | Lumière jaune → rose bonbon, IA néon cyan → violet, confettis, ballons, chapeau de fête sur la puce. |

Chaque dossier contient :

| Fichier | Contenu |
|---|---|
| `apercu-<thème>.png` | Planche de contrôle : logo, puis l'icône à 256, 64, 32 et 16 px sur fond clair et foncé |
| `sortie/luxia-<thème>.ico` | Icône Windows multi-tailles (16 à 256 px) |
| `sortie/luxia-<thème>-icone-{16…1024}.png` | Icône en PNG |
| `sortie/luxia-<thème>-logo.png`, `-logo-1200.png`, `-logo-transparent.png` | Logo 2400 × 800 (fond du thème), 1200 × 400, fond transparent |
| `sortie/luxia-<thème>-icone.svg`, `-logo.svg` | Vectoriels autonomes (texte converti en tracés) |
| `svg/` | Sources générées (texte en police Bahnschrift) |

## Régénérer

```bash
python generer-themes.py            # les quatre thèmes
python generer-themes.py noel       # un seul
```

Le script réutilise la géométrie de `../generer.py` et ne change que la palette (`THEMES` : fond, faisceaux, cœur du
croisement) et les décors (fonctions `<thème>_icon` et `<thème>_logo`). Les décors aléatoires (neige, confettis,
pétales) sont tirés avec une graine fixe : le résultat est le même à chaque génération. Prérequis : ceux de
`../generer.py` (Inkscape, Pillow, police Bahnschrift).

## Si un jour on veut les appliquer

Piste notée au carnet d'idées (doc 99) : un réglage « Habillage » (automatique selon la date, ou choisi pour la
soirée) qui remplacerait le logo de la fenêtre de démarrage, de « À propos » et de la navigation. L'icône de
l'exécutable, elle, est figée à la compilation : la changer à chaud n'est possible que pour la fenêtre, pas pour le
fichier `LuXia.exe`.
