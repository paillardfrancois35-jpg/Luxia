# SHOW-026 – Supervision

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | État de supervision publié dix fois par seconde (étapes actives, transitions validées ou armées avec temps restant, parcours, variables) ; bandeau de l'écran de jeu au lot 5. |
| **Liens** | — |

## Description

> **Supervision** : en Live et dans l'éditeur, étapes actives surlignées, transitions validées en attente de condition indiquées, temps restant avant quantification.

**Critère d'acceptation** : Revue.

## Réalisation

- src/Luxia.Show/Runtime/SequencerState.cs
- src/Luxia.Show/Runtime/ShowRun.cs

## Tests

- ShowExecutionTests.Drop_QuantizedOnTheBar_FiresAtTheNextBar_AndR5KeepsARepeatedScene

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`) : `Sequencer.State` ; maquettes 9 et 10 (bandeau « Show en cours », Q44 solution C). |
