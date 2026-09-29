# Chantier ergonomie — sujet 2 : éditer une scène dans une fenêtre dédiée

> Statut : **idée à cadrer** (réflexion utilisateur, aucun développement). Rédigé à partir d'une discussion de réflexion, le 2026-09-29.
> Sources : docs/60-ergonomie.md, docs/02-principes-et-architecture-fonctionnelle.md (P9), docs/16-scenes-et-effets.md, docs/32-passation.md.

## 1. Constat

Ce qu'on produit, une fois la configuration posée (patch, bibliothèque), c'est **une scène**. Tout le reste sert à la fabriquer (effets, palettes, thèmes, plan, propriétés) ou à la jouer (colonnes, couches, masters).

Aujourd'hui l'écran Contrôle mélange deux tâches qui n'ont pas les mêmes besoins, comme Daslight (« un module qui touche à tout ») :

| Tâche | Besoins |
|---|---|
| **Jouer** (spectacle) | grosses cibles, rapidité, aucun risque de fausse manœuvre, vue d'ensemble |
| **Concevoir** (une scène) | place, détail (plan + effets + propriétés en même temps), droit d'essayer puis d'annuler |

Ce qui gêne, et où :
- **Volet Propriétés** : trop petit en hauteur, on ne voit pas tout.
- **Panneau de sélection des scènes (Colonnes)** : inutile pendant qu'on édite, donc de la place perdue au moment où il en faudrait le plus.
- **Trois modes ÉDITION / AVEUGLE / LIVE** (doc 60 §4.1) : un sélecteur à gérer en permanence, avec des états (surcharge, valeur non enregistrée, aveugle) que l'utilisateur doit garder en tête.
- **Modules trop polyvalents** : Effets, Propriétés, Plan, Réglages, Dock ancrable et dispositions Contrôle / Spectacle. L'écran fait tout, donc rien de façon confortable.
- **Éléments trop petits** (remarque générale de l'utilisateur sur l'ergonomie actuelle).

## 2. Ce qu'on veut obtenir

Séparer clairement **jouer** et **concevoir**.

**Écran principal = écran de jeu**, épuré : scènes (colonnes, couches), masters, looks, pilote, journal. Il reste verrouillable (verrou soirée).

**Fenêtre d'édition de scène** (ouverte à la demande, même en spectacle) qui contient tout le nécessaire : plan des appareils, effets, propriétés, réglages. Elle dispose de toute la place, peut être posée sur un second écran, et fonctionne comme une **transaction** :
- on travaille sur une **copie** (brouillon) de la scène ;
- fermer sans valider : rien n'est pris en compte ;
- valider : la scène est enregistrée, et si elle est en cours de jeu elle est rejouée modifiée immédiatement ;
- le mode **aveugle** est une case de cette fenêtre (aperçu direct sur scène, ou aperçu au simulateur seulement).

### Devenir des notions actuelles

| Notion | Devenir |
|---|---|
| **LIVE** | n'est plus un mode : c'est l'état normal de l'écran principal. Les surcharges live (masters, couleur rapide) restent des retouches temporaires, non enregistrées. |
| **ÉDITION** | devient « la fenêtre d'édition est ouverte ». Le sélecteur disparaît. |
| **AVEUGLE** | case à cocher dans la fenêtre d'édition. Le moteur d'aperçu (GEN-063) et la commande CMD-017 sont conservés. |
| **Spectacle** (disposition) | c'est l'écran principal lui-même, plus besoin d'une disposition à part. |
| **Contrôle** (disposition) | largement absorbé par la fenêtre d'édition. |
| **Dock ancrable** | perd de son intérêt : à garder dans la fenêtre d'édition, ou à remplacer par une disposition fixe plus simple (à trancher). |

Trois lieux au lieu d'un sélecteur de modes : **jeu**, **édition de scène**, **configuration** (patch, bibliothèque, qui reste à part).

## 3. Idées et points à trancher

Aucune maquette pour l'instant. Points à décider avant de cadrer :

1. **Modale ou non bloquante ?** Peut-on déclencher autre chose pendant qu'on édite une scène en direct ? Le principe **P9** (« le Live ne pose jamais de question bloquante ») pousse vers une fenêtre non bloquante, qui ne s'ouvre jamais toute seule et dont l'annulation ne demande pas de confirmation en spectacle. La sortie DMX continue dans tous les cas.
2. **Que part-il sur scène pendant l'édition, hors aveugle ?** Proposition : un aperçu direct de la copie, avec restauration de l'état d'origine à l'annulation. Un bouton **Appliquer** (sans fermer) à côté de **Valider** permettrait de régler à l'œil.
3. **Ce qui n'est pas une scène.** Patch et bibliothèque restent de la configuration. Les thèmes, palettes et effets réutilisables ont besoin de leur propre éditeur (l'éditeur de thème en cours de développement est déjà une fenêtre de ce type). La fenêtre de scène doit pouvoir ouvrir ces sous-éditeurs.
4. **Retouches de dernière seconde en live** : elles passent par les masters et les couches, sans ouvrir l'éditeur.
5. **Dock et dispositions** : que garde-t-on dans la fenêtre d'édition ?
6. **Taille des éléments** : fixer des tailles minimales de cibles (l'utilisateur les juge trop petites) dans les deux lieux.

Coût pressenti (non vérifié dans le code) : les panneaux actuels sont des vues MVVM réutilisables. Le vrai travail est de leur fournir un brouillon de scène (clone puis validation ou annulation) et de simplifier la fenêtre principale.

## 4. Écrans touchés

- **Écran Contrôle** : refonte en écran de jeu ; sélecteur de modes supprimé ; dispositions Contrôle / Spectacle et Dock revus.
- **Panneaux** Propriétés, Plan, Réglages, Effets : déplacés dans la fenêtre d'édition.
- **Colonnes** : restent sur l'écran de jeu, absentes de la fenêtre d'édition.
- **Nouvelle fenêtre** d'édition de scène (avec sous-éditeurs : thème, effet).
- **Simulateur** : lien avec l'aperçu en aveugle (doc 14).
- **Écran Scènes** (ancien) : à réexaminer.

## 5. Exigences concernées

À faire évoluer ou remplacer si le chantier est lancé (la fiche devra dire lesquelles) :

- **ERG-010** (mode LIVE), **ERG-011** (mode ÉDITION), **ERG-012** (mode AVEUGLE) : marquées « Validé », remplacées par le modèle fenêtre d'édition.
- **GEN-063** et **SCN-035** (aveugle) : conservées sur le fond (moteur d'aperçu), reformulées.
- **ERG-028 à ERG-031** (panneaux Effets, molettes, en-têtes de couches, éditeur de thème) : à replacer dans la nouvelle organisation.
- Doc 60 §4.1 (modèle d'édition à trois modes) et question E1 : à réviser.

## 6. Recommandation

En faire un **chantier d'architecture à part entière**, à décider **avant** de multiplier les correctifs ergonomiques sur les panneaux actuels, qui risquent d'être déplacés. Prochaine étape : cadrage (fiche d'exigence, réponse aux points de la section 3, maquettes de l'écran de jeu et de la fenêtre d'édition) et chiffrage par rapport à la P6.
