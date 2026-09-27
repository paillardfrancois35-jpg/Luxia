# Identité visuelle de LuXia

Icône et logo de l'application, préparés le 2026-09-27 à la demande de l'utilisateur (discussion de la phase P5), en dehors
du dépôt. À intégrer dans le projet par la discussion de développement (voir « Intégration dans LuXia »).

## 1. Le nom et le concept

- **Origine** : nom construit sur le latin *lux*, la lumière (l'utilisateur pense à **Lucia**, sainte « porteuse de
  lumière » dont le nom vient de *lux*).
- **Lecture retenue par l'utilisateur** : **Lu** (lumière) + **X** + **ia** (intelligence artificielle : LuXia est pensée
  pour que les shows soient construits par l'IA). Le **X majuscule est volontaire** : à l'origine le X de « dmX », il
  représente **le croisement de deux mondes**, la lumière et l'IA.
- **Dessin** : le X est formé de **deux faisceaux qui se croisent** :
  - faisceau **lumière** : continu, tons chauds, issu d'un **projecteur** (rond, en haut à gauche) ;
  - faisceau **IA** : fait de **pixels**, tons froids, issu d'une **puce** (carré à pattes, en haut à droite) ;
  - un **point de lumière blanche** naît à leur intersection.
- **Logotype** : « Lu » en dégradé chaud, le X dessiné, « ia » en dégradé froid. **Pas de phrase d'accroche** (choix de
  l'utilisateur : le nom et le X parlent d'eux-mêmes).

## 2. Charte

| Élément | Valeur |
|---|---|
| Fond (dégradé vertical) | `#161B26` → `#080A10` (proche du thème sombre de l'application) |
| Lumière (chaud) | `#FFECAA` (blanc chaud) → `#FFAA3C` (ambre) → `#FF6E28` (orangé) |
| IA (froid) | `#96F5FF` (cyan clair) → `#46BEFF` (bleu) → `#8C5AFF` (violet) |
| Croisement | blanc `#FFFFFF`, halo radial |
| Police du logotype | **Bahnschrift Bold** (police de Windows) ; les SVG de `sortie/` ont le texte converti en tracés (aucune police requise) |
| Icône | carré aux coins arrondis (rayon 22 %), sources et extrémités des faisceaux à 17 % des bords, croisement au centre |
| Petites tailles (16-48 px) | version simplifiée : faisceau IA plein (les pixels deviennent du bruit), faisceaux un peu plus larges, sans pattes de puce |

## 3. Fichiers

| Dossier / fichier | Contenu |
|---|---|
| `svg/` | **Sources vectorielles** générées (texte en police : à éditer dans Inkscape si besoin) : `luxia-icone.svg`, `luxia-icone-petite.svg`, `luxia-logo.svg`, `luxia-logo-transparent.svg` |
| `sortie/luxia.ico` | **Icône Windows** multi-tailles (16, 24, 32, 48, 64, 128, 256 px) pour l'exécutable et la fenêtre |
| `sortie/luxia-icone-{16…1024}.png` | Icône en PNG (16 à 48 : version simplifiée ; 64 à 1024 : version complète) |
| `sortie/luxia-icone.svg` | Icône vectorielle autonome |
| `sortie/luxia-logo.png` | **Logo** 2400 × 800, fond sombre ; `luxia-logo-1200.png` : 1200 × 400 |
| `sortie/luxia-logo-transparent.png` | Logo sur fond transparent (à poser sur un fond sombre) |
| `sortie/luxia-logo.svg`, `luxia-logo-transparent.svg` | Logo vectoriel autonome (texte converti en tracés) : pour l'impression, tout agrandissement |
| `apercu-tailles.png` | Contrôle visuel de l'icône à 16, 24, 32, 48, 64 et 256 px sur fond clair et foncé |
| `generer.py` | Génère tout (voir §4) |
| `ancien/generer-pillow.py` | Premier jet (dessin direct en pixels), conservé pour mémoire |

## 4. Régénérer

Prérequis : **Inkscape** (installé : `C:\Program Files\Inkscape\bin\inkscape.exe`, version 1.4.4), Python avec **Pillow**
(assemblage du `.ico`), police **Bahnschrift** (Windows).

```bash
python generer.py
```

Les SVG de `svg/` sont écrits par le script (géométrie calculée, texte placé d'après les mesures de la police), puis
Inkscape en tire les PNG et les SVG autonomes. Pour modifier une couleur ou une proportion : constantes en tête de
`generer.py` (`WARM`, `COOL`, `BG_TOP`, `BG_BOTTOM`, `MARGIN`), puis relancer.

À noter pour une retouche : ne pas utiliser de mode de fusion (`mix-blend-mode`) sur les faisceaux, Inkscape le calcule
sur un calque rectangulaire qui laisse un carré visible autour du croisement.

## 5. Intégration dans LuXia (faite le 2026-09-28, exigence ERG-009 ; le point 5 « captures » : docs/maquettes/captures)

1. **Icône de l'application** : remplacer `src/Luxia.App/Assets/luxia.ico` par `sortie/luxia.ico` (même nom : l'exécutable
   et la fenêtre l'utilisent déjà ; l'ancienne icône « lampe + 3 points RVB » disparaît).
2. **Fenêtre Aide → À propos** : ajouter `sortie/luxia-icone-256.png` (ou `luxia-logo-transparent.png`, réduit) dans
   `src/Luxia.App/Assets/` et l'afficher en tête de la fenêtre, à gauche du nom et de la version.
3. **Fenêtre de démarrage** (`SplashWindow`, GEN-065) : remplacer le titre texte « LuXia » par le logo
   (`luxia-logo-transparent.png`, largeur ≈ 360 px), fond de la fenêtre inchangé.
4. **Documentation** : logo en tête du `README.md` du dépôt (`luxia-logo-1200.png`), éventuellement copie de ce dossier
   dans `docs/identite/` (sources SVG + sorties) pour que tout soit versionné.
5. **Traçabilité** : créer l'exigence et sa fiche (identité visuelle, demande de l'utilisateur du 2026-09-27), vérifier au
   rendu (outil de captures) que l'icône et le logo restent nets aux tailles utilisées.
