# SHOW-026 – Supervision

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Bandeau « Show en cours » de l'écran de jeu (replié / déplié) et texte « Étape active / Ensuite » de l'éditeur ; étapes actives colorées dans les cartes et le diagramme. |
| **Liens** | — |

## Description

> **Supervision** : en Live et dans l'éditeur, étapes actives surlignées, transitions validées en attente de condition indiquées, temps restant avant quantification.

**Critère d'acceptation** : Revue.

## Réalisation

- src/Luxia.Show/Runtime/SequencerState.cs
- src/Luxia.Show/Runtime/ShowRun.cs
- src/Luxia.UI.Modules.Control/Sequencing/ShowBandViewModel.cs
- src/Luxia.UI.Modules.Control/Views/GameView.axaml

## Tests

- ShowExecutionTests.Drop_QuantizedOnTheBar_FiresAtTheNextBar_AndR5KeepsARepeatedScene
- SequencingScreensTests.ShowsColumn_LaunchesAShow_AndTheBandSuperviseIt
- SequencingScreensTests.Band_ForcesATransition_AtTheNextBar

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`) : `Sequencer.State` ; maquettes 9 et 10 (bandeau « Show en cours », Q44 solution C). |
| 2026-10-02 | Claude | Développement | P8 lot 5 (`4ceb412`) : maquettes 9 et 10. |
