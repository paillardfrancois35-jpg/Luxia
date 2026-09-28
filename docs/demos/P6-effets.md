# Démonstration P6 – Effets

> Guide de découverte de la phase P6 (doc 40 §7, doc 41 §11). Durée : 1 h à 1 h 30.
> Matériel : PC seul (tout est jouable au simulateur et sur le plan de l'écran Contrôle) ; Arduino et appareils branchés
> pour le matériel réel. Branche Git : `p6/effets`. Version à vérifier dans la barre de titre : **v1.006.NNN**.

## Ce que livre P6

| Élément | Où |
|---|---|
| **Effets générés** : une forme (vague, carré, cercle, huit, arc-en-ciel…) qui tourne en boucle sur des appareils, décalée de l'un à l'autre | Moteur ; rangés dans l'étape d'une scène |
| Panneau **Effets** de l'écran Contrôle : bibliothèque, effets de l'étape, **dessin animé**, **molettes** (cycle, taille, centre, décalage, part allumée) | Contrôle → onglet **Effets** (à côté de « Réglages des appareils ») |
| **Aperçu en direct** : en ÉDITION l'effet joue sur la sortie, en AVEUGLE sur l'aperçu seulement | Écran Contrôle |
| **Bibliothèque d'effets** : 19 modèles (Vague douce, Chenillard on/off, Cercle lent, Arc-en-ciel…), « Enregistrer comme modèle » | Panneau Effets ; fichier `effets.json` |
| **Effets par cellule** : les 8 sections des barres, les **12 têtes** de l'effet multi-têtes | Case « Cellules » ; Installation → sélections « En cellules » |
| **Thèmes de couleurs** (Latino, Froid, Chaud, Disco, Club, Tricolore) et création d'un thème | Panneau Effets |
| **Fondu par la teinte** (rouge → vert en passant par le jaune) | Propriétés → étape choisie |
| **Assistants** : chenillard de couleurs, alternance de 2 couleurs, balayage de positions | Propriétés → « Assistant : générer les étapes » |
| 10 scènes « Phase P6 » sur le parc réel, dont un piège | `samples/Show de référence`, colonnes **Effets** et **Mouvements** |

## Préparer

### Effet multi-têtes en 64 canaux (réglage sur l'appareil)

Pour piloter les 12 têtes une à une, l'effet multi-têtes WZYBUTA passe en **64 canaux**. Il est placé **après la fumée** :
**aucun autre appareil ne change d'adresse**.

| Appareil | Avant | Maintenant | À faire sur l'appareil |
|---|---|---|---|
| Effet multi-têtes WZYBUTA | 20 canaux, adresse 141 | **64 canaux, adresse 181** (181 à 244) | Menu : mode **64 canaux** (mode 2), adresse **181** |
| Tous les autres | — | inchangés | rien |

### Régénérer le show de travail

Le show de référence a changé : **régénérez la copie de travail** (doc 32 §4), jamais l'original. Dans PowerShell :

```powershell
Remove-Item -Recurse -Force "D:\Develop\Claude\CSharp\DMX\samples\Show de travail"
```

```powershell
Copy-Item -Recurse "D:\Develop\Claude\CSharp\DMX\samples\Show de référence" "D:\Develop\Claude\CSharp\DMX\samples\Show de travail"
```

Renommez `"name"` en « Show de travail » dans `samples\Show de travail\projet.json`, lancez LuXia, **Projet → Ouvrir…** →
`samples\Show de travail`. **Projet → Problèmes du projet…** : un seul avertissement attendu (« Lyres sur 3 positions »,
déjà connu en P5).

Dans l'écran **Contrôle**, l'onglet **Effets** apparaît de lui-même en bas, entre « Réglages des appareils » et « Journal ».

---

## Exemple 1 – Vague sur les PAR, de gauche à droite (EFF-002, EFF-005, MOT-060)

1. Écran **Contrôle**, colonne **Effets** : cliquez **Vague sur les PAR (gauche → droite)**.
2. **Observez** (plan et appareils) : les 4 PAR en bleu montent et descendent l'un après l'autre, PAR 1 → PAR 2 → PAR 3 → PAR 4, un tour toutes les 2 s.
3. **Ce que ça illustre** : une seule étape, un seul effet, pas de programmation pas à pas : le décalage (360° répartis sur 4 PAR = un quart de cycle chacun) fait la vague.

## Exemple 2 – Miroir (EFF-005)

1. Arrêtez la vague ; lancez **Miroir sur les PAR (du centre vers les bords)**.
2. **Observez** : PAR 2 et PAR 3 (le centre de la sélection « PAR gauche → droite ») s'allument ensemble, puis PAR 1 et PAR 4.

## Exemple 3 – Barres : arc-en-ciel et chenillard des segments (EFF-004, EFF-008)

1. Lancez **Arc-en-ciel sur les barres (cellules)** : les 8 sections des deux barres passent par toutes les couleurs, décalées d'une section à l'autre (4 s par tour).
2. Lancez **Chenillard des segments (on/off)** (même colonne : il remplace l'arc-en-ciel) : une seule section rouge allumée à la fois, qui parcourt les deux barres.
3. **Ce que ça illustre** : la case **Cellules** fait de chaque section un membre de l'effet.

## Exemple 4 – Les 12 têtes de l'effet multi-têtes (EFF-008, matériel)

1. *Sur le matériel* : l'appareil doit être en **64 canaux à l'adresse 181** (voir « Préparer »).
2. Lancez **Têtes décalées (effet multi-têtes)** : thème « Disco » (magenta, cyan, jaune, vert) en alternance franche, chaque tête décalée.
3. **À vérifier avec moi** : les 12 têtes ont bien des couleurs différentes qui tournent ; la vitesse du canal 2 dans chaque sens (question restée ouverte, Q25).

## Exemple 5 – Lyres : cercle et huit (EFF-003, MOT-061)

1. Lancez **Plein feu** (Intensité) et **Bleu sur tout le parc** (Couleurs) pour voir les faisceaux.
2. Colonne **Mouvements** : **Cercle des lyres (autour de Piste centre)** : les deux lyres tracent un cercle de 60° autour de leur position « Piste centre ».
3. **Huit des lyres (opposées)** : huit couché autour de la même position, les deux lyres à l'opposé l'une de l'autre.
4. **Ce que ça illustre** : la taille se règle en **degrés** ; l'effet est posé autour d'une palette de position (qui suit le lieu actif).

## Exemple 6 – Piège : grand cercle et zone interdite (DEMO-4, MOT-082)

1. Gardez Plein feu ; lancez **Piège : grand cercle de la lyre 1 (zone interdite)** : un cercle de 250° qui passerait sur le public.
2. **Observez** : à chaque tour, la lyre 1 longe le **bord** de la zone interdite au lieu d'y entrer ; le journal signale la limite une fois.
3. **Ce que ça illustre** : la sûreté passe **après** les effets ; aucun effet ne la contourne.

## Exemple 7 – Créer un effet soi-même (EFF-001, EFF-006, EFF-007)

1. Colonne **Effets** : **+ scène**, nommez-la « Mon effet » ; choisissez-la avec la bande **✎**, passez en **ÉDITION**.
2. Sur le plan, sélectionnez PAR 1 à PAR 4 (clic sur le premier, puis Ctrl + clic sur les autres ; l'ordre de l'effet sera celui du plan, de gauche à droite).
3. Onglet **Effets** : choisissez le modèle **Chenillard on/off**, **+ Ajouter à la sélection**. **Observez** : l'effet tourne tout de suite, sur la sortie ; le dessin montre la forme et un point par PAR (le blanc est le premier).
4. Tournez les molettes : **Cycle** (glisser vers le haut = plus lent), **Allumé %**, **Décalage** (0 = tous ensemble). Double-clic sur une molette = valeur par défaut. Le changement se voit ~0,5 s après le dernier mouvement.
5. **Forme** : essayez « Scintillement », puis « Sinus » ; **Répartition** : « Miroir », « Par groupes » (2 = pairs / impairs).
6. **Ctrl+Z** : annule le dernier réglage. **Enregistrer comme modèle** : il rejoint la bibliothèque du projet.
7. **👁 AVEUGLE** : même chose, mais la sortie ne bouge pas ; seul le plan (aperçu) montre l'effet.

## Exemple 8 – Thèmes de couleurs (PAL-010)

1. Lancez **Plein feu**, puis **Alternance Latino (pairs / impairs)** : jaune, orange, rouge, un PAR sur deux.
2. Dans « Mon effet » (ÉDITION), forme **Alternance de couleurs** : thème « Latino » ; ou cochez vos couleurs (dans l'ordre des pastilles), puis **Enregistrer ces couleurs comme thème**.

## Exemple 9 – Scintillement UV (EFF-002, MOT-004)

1. Lancez **Scintillement UV** : l'intensité des deux UV varie au hasard, différemment pour chacun.
2. **Ce que ça illustre** : le hasard est **reproductible** (même graine de session, mêmes trames) : un enregistrement se rejoue à l'identique.

## Exemple 10 – Fondu par la teinte (MOT-054)

1. Choisissez **Chenillard 4 couleurs** (✎), ÉDITION ; dans Propriétés, étape 1 : **Fondu (s)** = 2, cochez **Fondu par la teinte** ; faites de même pour les étapes 2 à 4.
2. Revenez en LIVE et lancez la scène : du rouge au vert, la couleur passe par le **jaune** au lieu d'un brun terne. Décochez pour comparer ; Ctrl+Z pour tout remettre.

## Exemple 11 – Assistants de création (SCN-014)

1. Nouvelle scène « Chenillard généré » (colonne Couleurs), ✎, sélectionnez les 4 PAR au plan.
2. Propriétés → **Assistant : générer les étapes** : « Chenillard de couleurs », cochez Rouge, Vert, Bleu, Jaune, maintien 0,5 s → **Générer les étapes** : 4 étapes, les couleurs tournent d'un PAR à l'autre. Ctrl+Z revient.
3. Essayez « Balayage de positions » sur les lyres avec deux ou trois palettes de position.

## Exemple 12 – Sélection en cellules (INST-034)

1. Écran **Installation**, sélections manuelles : créez une sélection « Barres » avec les deux barres, puis **En cellules** : 8 éléments (Barre 1 #1 … Barre 2 #4) ; **Inverser**, **Pairs**… agissent sur les cellules ; **Par appareil** regroupe.

## Exemple 13 – Sans interface (pour l'IA de conception)

```powershell
dotnet run --project D:\Develop\Claude\CSharp\DMX\tools\Luxia.Tools.Headless -- valider "D:\Develop\Claude\CSharp\DMX\samples\Show de travail"
```

```powershell
dotnet run --project D:\Develop\Claude\CSharp\DMX\tools\Luxia.Tools.Headless -- jouer "D:\Develop\Claude\CSharp\DMX\samples\Show de travail" --scene "Vague sur les PAR (gauche → droite)" --duree 2 --pas 0.5
```

Le format des effets est décrit au doc 50 §10.1 (et `effets.json` au §12d-ter) : une IA peut écrire des scènes à effets,
les vérifier avec `valider` et les écouter avec `jouer`.

---

## Grille de retour

| Exemple | Correct | À revoir | Idée |
|---|---|---|---|
| 1 Vague | | | |
| 2 Miroir | | | |
| 3 Barres | | | |
| 4 Multi-têtes 64 canaux | | | |
| 5 Lyres | | | |
| 6 Piège zone interdite | | | |
| 7 Créer un effet | | | |
| 8 Thèmes | | | |
| 9 Scintillement | | | |
| 10 Fondu par la teinte | | | |
| 11 Assistants | | | |
| 12 Sélection en cellules | | | |
| 13 Sans interface | | | |
