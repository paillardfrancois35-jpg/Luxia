# LIVE-002 – Colonnes de couches

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 3. Exigences – structure](../18-live.md) |
| **Remarque** | Une colonne par couche (ordre des priorités, couches masquées via live.json), scènes « visibles en Live » dans l'ordre de scènes.json, scène active encadrée de sa couleur avec progression et étape, stop et master de couche. |
| **Liens** | COU-001, SCN-009 |

## Description

> **Colonnes de couches** : pour chaque couche, les scènes **visibles en Live**, dans l'ordre de la couche ; la scène active est mise en évidence avec sa progression (étape, barre) ; bouton stop et master de couche.

**Critère d'acceptation** : Revue.

## Réalisation

- `src/Luxia.UI.Modules.Live/LiveItems.cs` (`LayerColumnViewModel`, `LiveSceneViewModel`)
- `src/Luxia.UI.Modules.Live/LiveView.axaml`

## Tests

- `LiveViewModelTests.Columns_AreTheLayers_WithTheirLiveScenes_InOrder`
- `LiveViewModelTests.ClickScene_Launches_ClickAgain_Stops`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 3. Exigences – structure). |
| 2026-09-26 | Claude | Développement | `dbb3b1e` feat(live): écran Live |
| 2026-09-27 | Utilisateur | Question | Avant de lancer l'exécutable : refaire une analyse des nouveaux écrans (lisible, saisissable, sans place perdue) — « la passe d'ergonomie à venir n'est pas une raison pour faire mal les nouveaux développements ». |
| 2026-09-27 | Claude | Test | Revue par captures en 1680 × 1050 et 1366 × 768, Live « en jeu » : scène active trop peu distincte ; colonnes coupées en 1366 ; bloc Stop + master trop haut. |
| 2026-09-27 | Claude | Développement | Scène active remplie de sa couleur, nom en gras, ▶ ; liseré de couleur à gauche sinon ; colonnes réparties sur la largeur (120 px minimum) ; arrêt et master sur une ligne ; boutons de 42 px ; nom complet en info-bulle. |
| 2026-09-27 | Utilisateur | Test | Essai P5, exemple 1 : colonnes du Live, scène active remplie de sa couleur — conforme (étapes 1 à 4). |
| 2026-09-27 | Utilisateur | Test | Essai P5, exemple 1 suite : positions des lyres (piste centre, plafond) sans toucher couleur ni intensité — ok ; Intensité 50 % — ok. **Étape 8 non conforme** : baisser le master de la colonne Couleurs n'a aucun effet. |
| 2026-09-27 | Claude | Note | Analyse de l'étape 8 : conforme au cahier des charges (MOT-033 : le master agit sur l'intensité, « sur tous les attributs » en option) — les scènes « couleur seule » n'ont pas d'intensité, le master n'a rien à atténuer. Erreur du guide (« les couleurs s'atténuent »). Manque réel : l'option « master sur tous les attributs » n'est pas dans la fenêtre Couches (COU-001). Proposition soumise à l'utilisateur : case « Master sur tout », cochée par défaut pour Couleurs. |
| 2026-09-27 | Claude | Note | Étape 8 (master Couleurs sans effet) : conforme, erreur du guide corrigée (décision utilisateur, voir COU-001). |
