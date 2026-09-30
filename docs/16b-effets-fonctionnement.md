# 16b – Les effets : principe de fonctionnement

> Document **fonctionnel** de référence sur les effets, pour l'utilisateur et pour l'IA qui en génère (doc 22, doc 50 §10.1).
> Il décrit ce qu'est un effet et comment plusieurs effets cohabitent ; le détail des paramètres et des exigences reste au [doc 16 §6](16-scenes-et-effets.md), le moteur au [doc 15](15-moteur-de-rendu.md), le format JSON au [doc 50](50-format-des-donnees.md) (§10.1 `effects`, §12d-ter `effets.json`).

## 1. Ce qu'est un effet

Un **effet** est une **forme** (une courbe ou une trajectoire) **parcourue en boucle** et appliquée à un ou plusieurs appareils. Il vit **dans une étape** de scène (`SceneStep.Effects`) : il joue tant que l'étape joue, avec ses propres cibles, sa vitesse et ses réglages.

- **Modèle** (bibliothèque `effets.json`, 19 modèles livrés) : un effet type, nommé, rangé par catégorie, **sans appareils**. Choisir un modèle et « Ajouter à la sélection » crée un effet dans l'étape.
- **Effet** (dans l'étape) : une copie du modèle **liée à des appareils**. Il est ensuite libre : le modifier ne change pas le modèle. Son nom reste celui du modèle, suivi de « (modifié) » dans la liste quand sa forme a été changée.

## 2. Les trois types (familles)

Le **type** d'un effet est fixé par sa forme et **ne change pas en cours d'édition** : pour un autre type, on ajoute un autre effet depuis un autre modèle.

| Type | Formes | Ce que l'effet écrit | Appareils concernés |
|---|---|---|---|
| **Intensité** | Sinus, Triangle, Carré, Dent de scie montante / descendante, Impulsion, Scintillement | Un **attribut au choix** qui varie : intensité (défaut), rouge, vert, bleu, blanc, ambre, UV, zoom, focus | Tout appareil ayant cet attribut |
| **Mouvement** | Cercle, Huit, Balayage horizontal / vertical, Errance | **Pan / Tilt** | Lyres et appareils motorisés |
| **Couleur** | Arc-en-ciel, Alternance, Dégradé | La **couleur** (toujours en valeur absolue, traduite pour chaque appareil : RVB, RVBW, roue…) | Appareils ayant une couleur |

Un effet dont aucun appareil ciblé n'a l'attribut voulu ne produit rien ; l'éditeur le signale (« aucun membre n'a de Pan/Tilt », « de couleur », « cet attribut »).

## 3. Appareils ciblés et décalage

- Les **cibles** d'un effet sont les appareils (ou groupes, cellules) qui étaient sélectionnés au plan **au moment de l'ajout**, **dans l'ordre de sélection**. « Reprendre la sélection du plan » les remplace.
- La forme est parcourue **décalée d'un appareil à l'autre** : `Spread` (en degrés, 360 = un tour complet réparti sur les membres) avec `PhaseMode` : linéaire, miroir, par groupes (`GroupSize`), aléatoire. `Direction` inverse ou alterne le sens. Décalage 0 : tous ensemble.
- L'ordre de sélection fait donc l'effet (chenillard de gauche à droite ou l'inverse).

## 4. Plusieurs effets dans une même étape

Une étape porte **une liste d'effets**, chacun avec **ses propres cibles**. Exemple : *sélectionner les PAR 1 et 2, ajouter « Chenillard on/off » ; puis sélectionner les PAR 3 et 4, ajouter « Arc-en-ciel en vague »*. Les deux effets jouent en même temps, chacun sur ses deux PAR, avec sa vitesse. Ajouter ne remplace jamais : on supprime un effet explicitement (ou on le **duplique**, la copie est indépendante).

Quand plusieurs effets touchent le **même appareil et le même attribut** :

- deux effets **absolus** (forme intensité absolue, couleur) : **le dernier de la liste l'emporte**, pondéré par son poids (fondus, §6) ;
- deux effets **relatifs** (ajoutés à la valeur de base, typiquement Pan/Tilt) : ils **s'additionnent** ;
- un effet relatif s'ajoute à la valeur de l'étape pour cet attribut, à défaut à la valeur sous-jacente (autre couche).

Ajouter un effet d'intensité à des appareils sans couleur leur donne le blanc (EFF-011) ; ajouter un effet de couleur allume les appareils ciblés (MOT-041).

## 5. Réglages principaux

- **Vitesse** : durée d'un cycle, en secondes ou en temps / mesures (suit le tempo).
- **Taille** : amplitude (% pour intensité et couleur, degrés pour le mouvement) ; **Centre** : niveau moyen d'une courbe absolue ; **Durée allumée** (`DutyCycle`) : part haute d'un carré ou d'une impulsion.
- **Relatif / absolu** : relatif = s'ajoute à la valeur en place ; absolu = la remplace.
- **Par cellule** (`PerCell`) : les cellules d'un appareil multicellulaire sont traitées comme des membres.
- **Couleurs** : liste de couleurs de palette ou **thème** (palettes de type thème) pour les effets de couleur.
- **Palette de position** (`PositionPaletteId`) : point de départ d'un mouvement.

## 6. Dans le temps

- L'effet **continue sans coupure d'une étape à l'autre** si l'étape suivante contient l'effet de **même identifiant** (dupliquer une étape conserve les identifiants) : on ne repart pas du début du cycle.
- Un effet **entrant** démarre au début de son cycle ; son poids monte avec le fondu de l'étape, puis retombe avec le fondu de sortie de la scène.
- L'aléatoire (Scintillement, Errance, phases aléatoires) est **reproductible** (graine fixée par l'effet).

## 7. Pour l'IA qui génère des effets

1. Choisir le **type** d'après l'objectif (faire vivre la lumière = intensité ; bouger = mouvement ; changer les couleurs = couleur) et vérifier le patch (Pan/Tilt, couleur présents).
2. Ne mettre dans `Shape` que des formes du type choisi (§2) ; `Attribute` n'a de sens que pour l'intensité.
3. Donner **des cibles distinctes à chaque effet** quand des groupes d'appareils doivent jouer autre chose (§4) ; éviter deux effets absolus sur les mêmes appareils et le même attribut.
4. Ordonner les cibles comme le déplacement voulu ; régler `Spread` (0 = ensemble, 360 = un tour réparti).
5. Garder les identifiants d'effets d'une étape à l'autre pour la continuité.
