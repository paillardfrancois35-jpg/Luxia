# 11 – Console DMX

> Cahier des charges – module **Console** (préfixe `CONS`). Phase principale : **P1** (mode canaux), **P3** (mode appareils).
> Références : [02 §9 (chaîne de rendu, étapes 5 et 11)](02-principes-et-architecture-fonctionnelle.md), [10](10-sortie-dmx-et-firmware.md).

---

## 1. Rôle

Outil de **test et de dépannage** : agir à la main sur la sortie, canal par canal (mode *canaux*) ou attribut par attribut
d'un appareil patché (mode *appareils*), et **voir** ce qui est réellement émis (moniteur).

Deux usages principaux :
1. **Mise au point** : vérifier une définition d'appareil, trouver ce que fait un canal, tester une plage.
2. **Dépannage en soirée** : reprendre la main sur un canal précis (ex. couper une lyre qui se comporte mal).

## 2. Écran

```
┌ Console ─────────────────────────────────────────────────────────────────────────────────────┐
│ Mode : (•) Canaux  ( ) Appareils     Univers : [1 ▾]   Page : ◀ 1-32 ▶   [Tout libérer] [Figer]│
├──────────────────────────────────────────────────────────────────────────────────────────────┤
│  1    2    3    4    5    6    7    8   …  32                                                │
│ PAR1 PAR1 PAR1 PAR1 PAR1 PAR1 PAR1 PAR2 …        ← nom de l'appareil patché (si connu)       │
│  Dim  R    G    B   Strb Fn   Vit  Dim  …        ← attribut                                   │
│ ┃██┃ ┃  ┃ ┃██┃ ┃  ┃ ┃  ┃ ┃  ┃ ┃  ┃ ┃██┃ …        ← faders (bordure orange = surchargé)        │
│ 255   0   200   0    0    0    0   180 …        ← valeur (saisie directe au double-clic)      │
│ 100%       78%            « ouvert »            ← % ou nom de plage                           │
├──────────────────────────────────────────────────────────────────────────────────────────────┤
│ Moniteur de sortie (512 cases)  ■■■□□□■■…   survol : « Canal 5 – PAR1 Strobe – 0 – source : Couche Strobe »│
└──────────────────────────────────────────────────────────────────────────────────────────────┘
```

## 3. Exigences – mode canaux (P1)

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| CONS-001 | I | P1 | Affichage de faders par pages (16, 32 ou 48 par page selon la largeur), numérotés 1 à 512, pour l'univers choisi. | Parcours des 16 pages de 32. |
| CONS-002 | I | P1 | Réglage de chaque fader à la souris (glisser), à la molette (±1, Maj+molette ±10), au clavier (flèches ±1, Page ±10, Début/Fin = 255/0) et par **saisie directe** de la valeur. | Test de chaque mode de saisie. |
| CONS-003 | I | P1 | Toucher un fader le **prend** : sa valeur surcharge la sortie (étape 11 de la chaîne de rendu) jusqu'à ce qu'il soit **libéré**. Un fader pris est signalé visuellement. | Fader pris → valeur émise quelle que soit la scène active ; libéré → la scène reprend la main. |
| CONS-004 | I | P1 | Commandes : libérer un fader, libérer la page, **tout libérer** ; mettre la page à 0 ; mettre la page à 255. | Test. |
| CONS-005 | I | P1 | Le fader affiche en permanence la **valeur réellement émise**, qu'il soit pris ou non (il suit les scènes quand il est libre). | Scène en cours → faders libres animés. |
| CONS-006 | I | P1 | Sélection multiple de faders (Ctrl/Maj+clic) : un déplacement agit sur tous (en relatif ou en absolu, au choix). | Régler R, G, B ensemble. |
| CONS-007 | M | P1 | Si le canal appartient à un appareil patché, affichage du nom de l'appareil, de l'attribut, et du **nom de la plage** courante (ex. « Strobe : lent→rapide, 42 % »). | Test avec un appareil patché (dès P3). |
| CONS-008 | M | P1 | Les surcharges de la console restent soumises au blackout et aux limites de sûreté (GEN-042). | Fumée à 255 → coupée à la limite de durée. |
| CONS-009 | M | P1 | Choix de l'univers affiché (si plusieurs). | — |
| CONS-010 | S | P1 | Mémoriser / rappeler des « instantanés » de console (état de tous les faders pris) pour les tests répétitifs. | Rappel d'un instantané. |

## 4. Exigences – mode appareils (P3)

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| CONS-020 | I | P3 | Les faders sont regroupés **par appareil patché** (bandeau avec le nom et la couleur de l'appareil), dans l'ordre du patch ou d'une sélection. | Vue par appareil. |
| CONS-021 | I | P3 | Les attributs sont présentés avec l'outil adapté : fader pour les intensités et canaux simples ; sélecteur de **plages** (boutons) pour les canaux à plages ; **couleur** (pastille + faders R/G/B/W) ; **Pan/Tilt** (pad XY, 16 bits combinés). | Revue avec chaque type d'appareil du parc. |
| CONS-022 | I | P3 | En mode appareils, une modification surcharge l'**attribut** (étape 5 de la chaîne de rendu) : elle passe donc par le Grand Master, le blackout, les masters et les limites de sûreté. | Grand Master à 50 % → intensité surchargée divisée par 2. |
| CONS-023 | M | P3 | Clic sur un nom de plage → la valeur médiane de la plage est émise ; un curseur permet de **balayer** la plage. | Test sur le canal Strobe d'un PAR. |
| CONS-024 | M | P3 | Bouton **Identifier** par appareil (CMD-023). | L'appareil clignote, les autres ne changent pas. |
| CONS-025 | M | P4 | **Capturer** : créer une scène (ou une étape d'une scène existante) à partir des surcharges en cours (seuls les attributs surchargés sont enregistrés). | Capture → scène qui reproduit l'état. |

## 5. Moniteur de sortie

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| CONS-040 | I | P1 | Grille de 512 cases par univers, la couleur/luminosité de chaque case représentant la valeur émise ; valeurs numériques affichables. | Revue. |
| CONS-041 | I | P1 | Au survol d'une case : numéro de canal, valeur, appareil et attribut (si patché). | Revue. |
| CONS-042 | M | P4 | Au survol : **origine** de la valeur (couche/scène, surcharge, défaut, limite de sûreté) (GEN-043). | Revue. |
| CONS-043 | M | P1 | Les canaux appartenant à un même appareil sont délimités visuellement ; les canaux surchargés sont marqués. | Revue. |
| CONS-044 | S | P1 | Le moniteur peut s'ouvrir dans une fenêtre séparée. | — |

## 6. Intégration dans d'autres écrans

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| CONS-060 | I | P2 | Le composant « faders d'un appareil » est réutilisable dans l'éditeur de bibliothèque pour le **test en direct** d'un modèle (BIB-060) (GEN-004). | Même composant, même comportement. |
| CONS-061 | S | P5 | Une page de console peut être affectée aux faders d'un APC mini. | Faders physiques → canaux. |

## 7. Notes de réalisation (P1, P3)

| Sujet | Réalisation |
|---|---|
| CONS-001 | 16, 32 ou 48 faders par page selon la largeur (tranches de 42 px) ; pages en boucle. |
| CONS-002 | Glisser **relatif** (pas de saut au point cliqué) ; un clic simple prend le fader à sa valeur actuelle. Saisie directe dans la case sous le fader (ou double-clic), `%` accepté. |
| CONS-006 | Ctrl+clic : ajouter / retirer ; Maj+clic : plage ; clic simple hors sélection : sélection de ce seul fader ; Échap : désélection. Mode relatif par défaut. |
| CONS-007, CONS-041 (P3) | Nom d'appareil, attribut et nom de plage (au lieu du %) dès qu'un patch existe ; recalculés à chaque rafraîchissement (`Luxia.Patch.Rules.PatchLookup`). |
| CONS-008 | P4 : le blackout coupe aussi les surcharges brutes des canaux d'intensité et de ceux qui suivent l'intensité. Limites de sûreté : P5 (`TODO(P5, GEN-083)` dans le moteur). |
| CONS-010 | Réalisé (demandé par les démonstrations P1) : instantanés rangés dans le projet (`console.json`, doc 50) ; un rappel remplace les faders pris de l'univers. |
| CONS-020 à 024 (P3) | Mode « Appareils » : un `FixtureFadersViewModel` (CONS-060) par appareil patché de l'univers affiché, via `Attach`/`Detach` (n'écrit pas de valeurs par défaut, ne libère pas les surcharges en quittant — à la différence de `Start`/`Stop`, réservées au test en direct jetable de la bibliothèque). Bouton Identifier commun aux deux usages. |
| CONS-022 | P3 : surcharge de canal faute de chaîne. **P4** : surcharge d'**attribut** (CMD-021, étape 5), soumise au Grand Master et au blackout ; fader « Intensité (virtuelle) » pour un appareil sans gradateur ; colorier un tel appareil l'allume à 100 % tant que son intensité n'est pas prise. Bouton « Libérer » par appareil ; « Tout libérer » libère aussi les attributs. |
| CONS-025 (P4) | Écart : pas de bouton dans l'écran Console ; la capture se fait depuis le programmeur de l'écran Scènes (« Capturer la sortie »), surcharges de la console comprises. |
| CONS-042 (P4) | Au survol du moniteur : couche et scène, surcharge d'attribut, blackout ou défaut (limites de sûreté en P5). |
| CONS-091 (P4) | Valeur virtuelle par fader sélectionné en relatif, gardée à travers plusieurs glissés ; oubliée au changement de sélection, à « Désélectionner » et à la libération. |
| CONS-043 (P3) | Canaux surchargés encadrés en orange ; appareils délimités par un trait (`Luxia.UI.Controls.OutputMonitor.FixtureBoundaries`). |
| CONS-044 (S) | Non réalisé. |
| CONS-092 (P3) | Cadre immédiat sur la case survolée (numéro et valeur l'étaient déjà, via `HoverText`, pas un `ToolTip` standard). |
| Test de sortie | Tant qu'il est actif, le chenillard de test remplace aussi les faders pris (D21). |
| Figer | Le bouton « Figer » de la maquette viendra avec CMD-003 (P5). |
| CONS-060 | Réalisé en P2 : `FixtureFadersView` (module Console), utilisé par le test en direct de la bibliothèque ; réutilisé une seconde fois en P3 pour la console en mode appareils (GEN-004). |
| Rafraîchissement | L'interface lit la trame et les surcharges du moteur 20 fois par seconde ; après une action, la valeur demandée reste affichée 150 ms, le temps que le moteur l'applique (pas de retour en arrière visible). |

## 8. Tests

| Test | Type | Contenu |
|---|---|---|
| T-CONS-01 | Unitaire | Prise / libération : fader pris → surcharge ; libération → retour à la valeur de la chaîne. |
| T-CONS-02 | Unitaire | Surcharge en mode appareils soumise au Grand Master ; surcharge en mode canaux soumise au blackout. |
| T-CONS-03 | Intégration | Réglage d'un fader → octet correct dans la trame de l'Enregistreur au tick suivant. |
| T-CONS-04 | Performance | Latence fader → sortie < 50 ms (GEN-090). |
| T-CONS-05 | Manuel | Sur matériel : piloter chaque canal de chaque appareil du parc (check-list). |
