# Analyse ergonomique de fin de phase – P6 Effets

> Doc 32 §5.6. Rédigée le 2026-09-29 dans la discussion de développement, à partir de l'essai P6
> ([essais/P6-resultats.md](../essais/P6-resultats.md)), de la question Q37 (groupes et dimmers) et du document de
> réflexion de l'utilisateur [ergonomie-sujet-2.md](ergonomie-sujet-2.md) (éditer une scène dans une fenêtre dédiée).
> **À valider par l'utilisateur.**

## 1. Ce que l'essai P6 a montré

| Constat | Traité en P6 | Reste |
|---|---|---|
| Molettes difficiles à saisir, pas de saisie au clavier | ✅ ERG-028 révisé (1.006.059) | Tailles de cibles en général (§3) |
| Thèmes peu clairs, ✕ des modèles dangereux | ✅ ERG-031 (1.006.059) | — |
| En-têtes de couches perdus au défilement, ascenseur commun | ✅ ERG-030 | — |
| « ▶ Lancer » sans effet en ÉDITION, flèches ◀ ▶ ambiguës | ✅ ERG-031 | Symptôme du modèle à trois modes (§2) |
| Textes coupés dans les tranches de la Console | ✅ infobulle + ligne d'information (CONS-007) | — |
| Propriétés trop courtes, Colonnes inutiles pendant l'édition, modes à garder en tête, éléments trop petits | — | **Sujet 2** (§2) |
| Intensité par groupe d'appareils, master de couche qui « règle du dimmer » | — | **Q37** (§2) |

## 2. Deux sujets qui touchent le même écran

Le **sujet 2** (séparer *jouer* et *concevoir* : écran de jeu épuré, fenêtre d'édition de scène en brouillon avec
Valider / Annuler, aveugle en case à cocher) et le chantier **« Groupes et dimmers »** (Q37 : arbre de groupes, règle
proportionnelle, réglage de couche, platine MIDI n° 2) refont tous deux l'écran Contrôle et ses panneaux.

**Avis** : le sujet 2 est juste et va dans le sens du principe P9 (le Live ne pose jamais de question bloquante). Il
remplace trois modes à garder en tête par deux lieux évidents, et un brouillon validé ou annulé est plus sûr en soirée
qu'un enregistrement immédiat. Il fait disparaître d'eux-mêmes plusieurs défauts vus à l'essai (« ▶ Lancer » masqué
par l'étape éditée, Propriétés trop courtes, Colonnes encombrantes pendant l'édition).

**Recommandation** : un **seul chantier « Contrôle 2 »**, entre P6 et P7, qui réunit les deux sujets, pour ne refaire
l'écran qu'une fois :

1. **Écran de jeu** : colonnes (grandes cibles), masters de groupe (panneau « Groupes dimmer »), réglages de couche,
   looks, pilote, journal, verrou soirée. Plus de sélecteur de modes ; les retouches LIVE restent temporaires.
2. **Fenêtre d'édition de scène**, non bloquante, sur un second écran si l'on veut : plan, réglages, effets, propriétés,
   étapes ; **brouillon** ; **Appliquer** (voir sur scène sans fermer), **Valider**, **Annuler** (retour à l'état
   d'origine, sans question en spectacle) ; case **Aveugle** (aperçu au plan seulement).
3. **Configuration** (Installation, Bibliothèque) : ajout de l'onglet « Gestion des dimmers » (arbre de groupes).
4. **Tailles minimales** fixées dans la charte (doc 60) : boutons de scène ≥ 44 px de haut, boutons ≥ 32 px, molettes
   ≥ 70 px ; à vérifier en 1366 × 768.

Réponses proposées aux points à trancher du sujet 2 (§3) : (1) **non bloquante** ; (2) **aperçu direct du brouillon**
sur scène hors aveugle, **Appliquer** + **Valider** ; (3) sous-éditeurs en fenêtres (thème : déjà fait ; effet : à
venir) ; (4) retouches de dernière seconde par masters, groupes et couches ; (5) **ancrage gardé sur l'écran de jeu**, avec peu de panneaux et de grande taille (Colonnes, Groupes dimmer, Looks, Pilote, Journal) : placer les dimmers de groupe par rapport aux scènes, détacher un panneau sur un second écran (remarque de l'utilisateur, 2026-09-29) ; **disposition fixe** dans la fenêtre d'édition ; les dispositions Contrôle / Spectacle se réduisent à celle de l'écran de jeu ; à confirmer sur la maquette; (6) tailles du §2.4.

Exigences à reprendre : ERG-010, 011, 012 (remplacées), ERG-001, 024 (Dock, dispositions), ERG-028 à 031 (replacées),
GEN-063 et SCN-035 (reformulées), doc 60 §4.1 et E1.

## 3. Méthode

Comme pour le premier chantier ergonomique : **cadrage** (exigences, réponses ci-dessus), **maquettes** de l'écran de
jeu et de la fenêtre d'édition validées **avant** tout code, développement, essai en discussion test (doc 33), puis P7.

## 4. Pour clore P6

Ne plus retoucher les panneaux actuels (ils vont être déplacés) : seul le correctif de la Console (textes coupés) reste
à vérifier ; puis fusion de `p6/effets` dans `main` et étiquette **v1.006**.
