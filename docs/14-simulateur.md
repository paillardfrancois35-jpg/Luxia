# 14 – Simulateur (visualiseur)

> Cahier des charges – module **Simulateur** (préfixe `SIM`). Phase principale : **P3** (2D) ; 3D simple en **P12**.
> Références : [02 principe P5](02-principes-et-architecture-fonctionnelle.md), [10 (pilote Simulateur)](10-sortie-dmx-et-firmware.md), [13 (lieux)](13-installation-et-lieux.md).

---

## 1. Rôle

Permettre de **préparer, tester et répéter sans monter le matériel**. Le simulateur est une **sortie** : il reçoit les trames
DMX et les décode grâce au patch et aux définitions d'appareils. Ce qui s'y voit est donc ce qui serait réellement émis.

## 2. Sources affichées

| Source | Usage |
|---|---|
| **Sortie** (par défaut) | Trames réellement émises (pilote Simulateur, SORT-062) |
| **Aperçu** | Résultat de l'édition en mode aveugle (GEN-063) : on voit la scène en cours d'édition sans l'envoyer à la sortie |
| **Lecture** | Rejeu d'un enregistrement de trames (SORT-063) |

## 3. Représentation 2D (vue de dessus)

| Type d'appareil | Représentation |
|---|---|
| PAR, UV | Symbole de l'appareil + **tache de lumière** au sol dans la couleur résultante, luminosité = intensité ; UV : halo violet |
| Barre LED | Rangée de segments (cellules) colorés |
| Lyre | Symbole + **faisceau** projeté au sol selon Pan/Tilt (direction, longueur selon Tilt), couleur de la roue, motif de gobo symbolisé, taille selon faisceau |
| Effet multi-têtes | Symbole avec N têtes, faisceaux animés selon les rotations |
| Fumée | Nuage qui apparaît progressivement et se dissipe (inertie de quelques secondes) |
| Strobe | Clignotement **atténué** (voir SIM-012) |
| Programmes internes | Indication textuelle « Programme interne : fondu » + animation générique (le programme réel n'est pas connu) |

## 4. Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| SIM-001 | I | P3 | Affichage du plan du **lieu actif** avec tous les appareils présents, à leur position et orientation. | Revue. |
| SIM-002 | I | P3 | Rendu à 30 images/s minimum, synchronisé sur la dernière trame reçue ; le simulateur ne ralentit jamais le moteur (GEN-013). | Mesure. |
| SIM-003 | I | P3 | Décodage des trames via le patch : couleur résultante (mélange RGB/RGBW/UV, roue de couleur), intensité (y compris intensité virtuelle), strobe, Pan/Tilt, cellules. | Test comparatif : état attendu d'une trame de référence. |
| SIM-004 | I | P3 | Faisceau des lyres : direction calculée à partir de Pan/Tilt, des amplitudes du modèle (doc 12) et des options de montage (INST-021). | Pan 0 → 540° : un tour et demi. |
| SIM-005 | I | P3 | Survol / clic sur un appareil : nom, adresse, valeurs de ses attributs, plages courantes. | Revue. |
| SIM-006 | I | P3 | Choix de la source (Sortie / Aperçu / Lecture) visible en permanence. | Revue. |
| SIM-007 | M | P3 | Fenêtre **détachable** et **plein écran** (second écran). | Simulateur plein écran sur second moniteur. |
| SIM-008 | M | P3 | Affichage des **zones interdites** et des repères du lieu (piste, public). | — |
| SIM-009 | M | P3 | Mise en évidence des appareils **identifiés** (CMD-023) et des appareils en erreur de définition. | — |
| SIM-010 | M | P3 | Sélection d'appareils au clic / au lasso dans le simulateur, reprise par le programmeur (doc 16). | Sélectionner 2 PAR au lasso → sélection du programmeur. |
| SIM-011 | M | P7 | Affichage optionnel d'un bandeau musical : BPM, battement, énergie (pour juger la synchro en répétition). | — |
| SIM-012 | I | P3 | **Protection photosensible** : le strobe est rendu à fréquence et contraste réduits (ou par une icône), jamais en clignotement plein écran rapide. | Revue. |
| SIM-013 | S | P3 | Vue **de face** en plus de la vue de dessus (hauteur des faisceaux). | — |
| SIM-014 | S | P12 | **Prévisualisation 3D simple** : salle en volume, cônes de faisceau, fumée qui rend les faisceaux visibles. | — |

## 5. Tests

| Test | Type | Contenu |
|---|---|---|
| T-SIM-01 | Unitaire | Décodage : trames de référence → couleurs, intensités, directions attendues pour chaque modèle du parc. |
| T-SIM-02 | Performance | 30 images/s avec tout le parc animé ; aucune gigue induite sur le moteur. |
| T-SIM-03 | Manuel | Comparaison visuelle simulateur / matériel réel sur 10 scènes types (jalon 1). |

## 6. Notes de réalisation (P3)

> Écarts et précisions constatés au développement (doc 40 §6). Détail complet dans les fiches d'exigences.

| Sujet | Réalisation |
|---|---|
| SORT-062 (pilote Simulateur) | Pas de pilote `IOutputDriver` séparé : le simulateur lit `RenderEngine.CopyLastFrame` directement (même mécanisme non bloquant que le moniteur de la console). |
| SIM-003 | Décodage complet dans `Dmx.Patch.Rules.FixtureDecoder`, réutilisable hors du simulateur (couleurs, intensité virtuelle BIB-006, roues, strobe, cellules, Pan/Tilt). |
| SIM-004 | Vue de dessus uniquement : le Tilt ne raccourcit pas le faisceau à l'écran (la vue de face, SIM-013, n'est pas faite). |
| SIM-006 | Une seule source pour l'instant (« Sortie ») ; Aperçu (P4, mode aveugle) et Lecture (SORT-063, S) viendront plus tard. |
| SIM-009 | L'identification (CMD-023) se voit sans code dédié : elle agit par de vraies surcharges de canaux, décodées normalement. |
| SIM-012 | Strobe signalé par une icône fixe, jamais animé à la fréquence réelle (protection photosensible sans ambiguïté). |
| Non fait | SIM-007 (fenêtre détachable), SIM-008 (zones interdites / repères, dépend de P5), SIM-010 (sélection reprise par le programmeur, dépend de P4), SIM-013 (vue de face, S). |
| Écran | `Dmx.UI.Modules.Simulator` + contrôle `Dmx.UI.Controls.SimulatorCanvas`. |
