# SHOW-006 – Une séquence est lançable depuis le Live, une étape de show, ou le Directeur

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 2.2 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Lançable par commande (CMD-052), par une étape de show, par le scénario ; bouton de la colonne « Shows » au lot 5 ; Directeur en P10. |
| **Liens** | — |

## Description

> Une séquence est lançable depuis le Live, une étape de show, ou le Directeur.

**Critère d'acceptation** : —

## Réalisation

- src/Luxia.Show/Runtime/Sequencer.cs
- src/Luxia.Hosting/Tools/Scenario.cs

## Tests

- SequencePlaybackTests.SequenceCommands_AreRejectedWithoutSequencer
- ShowExecutionTests.PlaySequence_RunsWhileTheStepIsActive_AndSequenceEndedMovesOn

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 2.2 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lots 3 et 4 (`4c87f9e`, `d46142e`). |
