# SHOW-006 – Une séquence est lançable depuis le Live, une étape de show, ou le Directeur

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 2.2 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Lançable depuis la colonne « Shows » de l'écran de jeu, une étape de show, le scénario ; le Directeur s'en servira en P10. |
| **Liens** | — |

## Description

> Une séquence est lançable depuis le Live, une étape de show, ou le Directeur.

**Critère d'acceptation** : —

## Réalisation

- src/Luxia.Show/Runtime/Sequencer.cs
- src/Luxia.Hosting/Tools/Scenario.cs
- src/Luxia.UI.Modules.Control/Sequencing/ShowsColumnViewModel.cs

## Tests

- SequencePlaybackTests.SequenceCommands_AreRejectedWithoutSequencer
- ShowExecutionTests.PlaySequence_RunsWhileTheStepIsActive_AndSequenceEndedMovesOn
- SequencingScreensTests.ShowsColumn_LaunchesAShow_AndTheBandSuperviseIt

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 2.2 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lots 3 et 4 (`4c87f9e`, `d46142e`). |
| 2026-10-02 | Claude | Développement | P8 lot 5 (`4ceb412`) : boutons de la colonne « Shows » (le « Live » est l'écran de jeu depuis « Contrôle 2 »). |
