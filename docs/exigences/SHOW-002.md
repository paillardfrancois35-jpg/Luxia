# SHOW-002 – Placer des scènes sur les pistes par glisser-déposer, avec début et durée en mesures/temps

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 2.2 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Frise des pistes en mesures, glisser-déposer depuis la bibliothèque, déplacer / allonger à la souris, grille mesure / temps / ½ temps. |
| **Liens** | — |

## Description

> Placer des scènes sur les pistes par glisser-déposer, avec début et durée en mesures/temps ; grille magnétique (mesure, temps, ½ temps).

**Critère d'acceptation** : —

## Réalisation

- src/Luxia.UI.Modules.Control/Sequencing/SequenceEditorViewModel.cs
- src/Luxia.UI.Modules.Control/Views/SequenceTimeline.cs
- src/Luxia.UI.Modules.Control/Views/SequenceEditorWindow.axaml

## Tests

- SequencingScreensTests.NewSequence_DropMoveResize_UndoAndValidate
- SequencingScreensTests.SequenceEditor_Cancel_LeavesTheProjectUntouched

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 2.2 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 1 : maquette 11 (`docs/maquettes/maquette-11-edition-sequence.png`) soumise à l'utilisateur ; le modèle (début et durée en mesures décimales, grille mesure / temps / ½ temps = 1, 0,25, 0,125) est prêt. |
| 2026-10-02 | Claude | Développement | P8 lot 5 (`4ceb412`) : fenêtre d'édition d'une séquence (maquette 11) en brouillon ; une scène lâchée sur la piste d'une autre couche va sur la piste de sa couche (D39) ; le glisser-déposer est mené par la fenêtre (pas de l'API de glisser-déposer du système). À vérifier à l'essai (exemple 12). |
| 2026-10-02 | Utilisateur | Test | Ex. 14 (v1.010.091) : nouvelle séquence, glisser-déposer, déplacer, durée par le bord droit, grille, Ctrl+Z, Valider ✅. |
