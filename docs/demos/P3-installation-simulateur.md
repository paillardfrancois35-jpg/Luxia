# Démonstration P3 – Installation + Simulateur

> Guide de découverte de la phase P3 (doc 40 §7, doc 41 §11). Durée : 45 minutes à 1 h.
> Matériel : PC seul (tout est jouable au simulateur) ; Arduino branché pour comparer au matériel réel si vous le souhaitez.
> Branche Git : `p3/installation-simulateur`.

## Ce que livre P3

| Élément | Où |
|---|---|
| Écran **Installation** : univers et patch, sélections, lieux, fiche d'installation | `DMX.exe` → Installation |
| Écran **Simulateur** : plan 2D du lieu actif, appareils en couleur/mouvement | `DMX.exe` → Simulateur |
| Console en **mode Appareils** (faders groupés par appareil, identifier) | `DMX.exe` → Console → bascule « Mode » |
| Moniteur de sortie : appareils délimités, cadre immédiat au survol | Console → moniteur, en bas |
| Test de sortie : **canaux maintenus** (SORT-008) | Sorties → « Canaux maintenus » |
| Parc entièrement patché et placé dans un lieu | `samples/Show de référence/installation.json`, `lieux.json` |

## Préparer : ouvrir le show de référence

1. Copiez `samples/Show de référence` en `samples/Show de travail` si ce n'est pas déjà fait (doc 32 §4) : **jamais** le dossier original.
2. Lancez l'application, **Projet → Ouvrir…** → `samples/Show de travail`.
3. Écran **Installation**, onglet **Univers et patch** : les 14 appareils du parc apparaissent, adresses conformes au plan (doc 41 §2) :
   PAR 1-4 (1, 8, 15, 22), Gros PAR 1-2 (31, 41 — modèle LPC120 provisoire, **Q27** à trancher), Barre 1-2 (51, 81),
   Lyre 1-2 (111, 126), Effet multi-têtes (141), UV 1-2 (161, 168), Fumée (180).
4. La **barre d'univers** en haut de l'écran montre les 14 blocs colorés, sans chevauchement (pas de ⚠).

---

## Exemple 1 – Patcher un appareil (INST-010 à 014)

1. **Ajouter un appareil** : modèle « Betopper LPC008S », mode « 7 canaux », univers 1, cliquez **Libre** (adresse proposée après le parc existant, ex. 181), quantité 1, nom « Test ».
2. **Ajouter** : la ligne apparaît dans la liste, aucun chevauchement.
3. Essayez maintenant de le déplacer à l'adresse **1** (déjà prise par PAR 1) : la ligne PAR 1 **et** la nouvelle ligne affichent ⚠.
4. **Supprimer** l'appareil « Test » (confirmation demandée, GEN-103).

## Exemple 2 – Changer de mode et mettre à jour depuis la bibliothèque (INST-016, GEN-053)

1. Sur **PAR 1** (7 canaux), changez le mode en **3 canaux** puis cliquez **Changer de mode**.
2. Le message de confirmation liste les canaux perdus (« Intensité, Strobe, Sélecteur de fonction perdus ») : **annulez**.
3. Bouton **Mettre à jour** sur un appareil : sans modification de la bibliothèque partagée, le message indique « Aucune mise à jour disponible ».

## Exemple 3 – Sélections (INST-030 à 033)

1. Onglet **Sélections**. Dans **Univers et patch**, cochez les 4 PAR (colonne « Sél. »).
2. Nommez « PAR » puis **Créer** : la sélection apparaît, ordre PAR 1 → 2 → 3 → 4.
3. **Inverser** : PAR 4 → 1. **Pairs** : PAR 2, PAR 4 seuls.
4. **Gauche→droite** : trie d'après la position au lieu actif (les PAR du totem gauche d'abord).
5. En bas, les **sélections automatiques** (« Tous » = 14, « Tous les PAR » = 4, « Tous les Betopper LPC008S » = 4…) se mettent à jour seules : patchez un appareil de plus et regardez leur nombre changer sans rien enregistrer.

## Exemple 4 – Lieu et présence (INST-050 à 052)

1. Onglet **Lieux** : lieu « Générique » actif, 12 × 8 m.
2. Décochez **Absent** → cochez-le pour « Lyre 2 » puis **Enregistrer le lieu**.
3. Passez à l'écran **Simulateur** : la lyre 2 a disparu du plan (SIM-001, INST-052). Revenez la décocher.
4. **Dupliquer** le lieu en « Salon », réduisez la largeur à 4 m : utile pour essayer chez vous avec un PAR et une lyre posés sur le bureau (doc 41 §3).

## Exemple 5 – Console en mode Appareils (CONS-020 à 024)

1. Écran **Console**, bascule **Mode : Appareils**.
2. Un groupe de faders apparaît par appareil de l'univers affiché, avec son nom réel (« PAR 1 », pas juste « Betopper LPC008S »).
3. Montez **Rouge** du PAR 1 : le fader se comporte comme en mode Canaux (même surcharge réelle) — repassez en mode **Canaux** : le canal 2 est toujours pris.
4. Sur le PAR 1, cliquez **Identifier** : son canal Gradation clignote (les autres appareils ne bougent pas). Le simulateur (si ouvert à côté) montre le même clignotement, sans rien de plus à faire (SIM-009).
5. Sur un canal à plages (Sélecteur de fonction), le fader affiche maintenant son **nom de plage** au lieu du % (CONS-007).

## Exemple 6 – Moniteur : appareils délimités, survol immédiat (CONS-043, CONS-092)

1. Mode Canaux, moniteur de sortie en bas : un trait fin sépare chaque appareil de son voisin (12 blocs de 7 canaux, 3, 24…).
2. Survolez une case : elle est **encadrée** aussitôt, et le texte au-dessus indique l'appareil, l'attribut, la valeur — sans attendre d'info-bulle.

## Exemple 7 – Simulateur (SIM-001 à 006, 012)

1. Écran **Simulateur** : le plan du lieu « Générique » avec les 14 appareils, en gris (rien émis).
2. Repassez en Console, mode Canaux, montez Rouge + Gradation du PAR 1 (canaux 1-2) : le point correspondant au PAR 1 s'allume en rouge dans le simulateur, en temps réel.
3. Sur la Lyre 1 (adresse 111), montez le Gradateur puis le Pan (canal 111) de 0 à 255 : le trait du faisceau tourne d'un tour et demi (540°, SIM-004).
4. Sur le canal Strobe d'un PAR, choisissez une plage « Strobe » : une icône ⚡ apparaît à côté de l'appareil, sans clignotement rapide à l'écran (SIM-012, protection photosensible).
5. Survolez un appareil : nom, modèle, univers, adresse (SIM-005).
6. La ligne « Source : Sortie (trames réellement émises) » est toujours visible (SIM-006) — Aperçu et Lecture arriveront avec le mode aveugle (P4) et le lecteur d'enregistrements (S).

## Exemple 8 – Canaux maintenus du test de sortie (SORT-008)

1. Écran **Sorties**, section Test de sortie : canaux **1-30** (les 4 PAR), canaux maintenus **1, 8, 15, 22** (les 4 gradateurs).
2. **Démarrer le test** : chaque canal s'allume tour à tour, **et** les 4 gradateurs restent allumés en continu — sans eux, les canaux Rouge/Vert/Bleu du chenillard resteraient invisibles (l'appareil est à gradateur maître).
3. **Arrêter**.

## Ce qui n'est pas encore là

| Élément | Statut / quand |
|---|---|
| Éditeur de plan par glisser-déposer, alignement/répartition (INST-051) | Partiel : positions par champs numériques pour l'instant |
| Options de montage (inverser/échanger Pan-Tilt, INST-021) : pas d'écran dédié | Modèle et simulateur déjà prêts ; éditeur à ajouter |
| Sélection de cellules (segments de barre, INST-034) | Reporté après P6 (effets par cellule) |
| Fenêtre détachable / plein écran du simulateur (SIM-007), zones interdites et repères (SIM-008), vue de face (SIM-013) | Non réalisés (M/S) |
| Sélection au clic/lasso reprise par le programmeur (SIM-010) | P4 (le programmeur n'existe pas encore) |
| Écraser un modèle à l'import (BIB-098), corrections d'ergonomie de la Bibliothèque (BIB-093, 094, 096, 097, 099, 100) | Reliquats P2, traités séparément (voir doc 32) |

## Grille de retour

| Exemple | Correct | À revoir | Idée / remarque |
|---|---|---|---|
| 1 – Patcher | ☐ | ☐ | |
| 2 – Mode / mise à jour | ☐ | ☐ | |
| 3 – Sélections | ☐ | ☐ | |
| 4 – Lieu et présence | ☐ | ☐ | |
| 5 – Console mode Appareils | ☐ | ☐ | |
| 6 – Moniteur | ☐ | ☐ | |
| 7 – Simulateur | ☐ | ☐ | |
| 8 – Canaux maintenus | ☐ | ☐ | |
