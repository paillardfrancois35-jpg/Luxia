# SHOW-023 – Exécution conforme aux règles R1 à R6

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | R1 à R6 ; une évolution par tick (D39) ; fin : tenir (défaut), s'arrêter ou reprendre. |
| **Liens** | — |

## Description

> Exécution conforme aux règles R1 à R6.

**Critère d'acceptation** : Tests d'exécution en temps virtuel avec événements simulés.

## Réalisation

- src/Luxia.Show/Runtime/ShowRun.cs

## Tests

- ShowExecutionTests.OrDivergence_FirstTrueTransitionWins_ByOrder
- ShowExecutionTests.AndDivergenceAndConvergence_RunBranchesInParallel_ThenJoin
- ShowExecutionTests.EndOfShow_HoldsStopsOrRestarts
- ShowExecutionTests.MacroStep_WaitsForItsSubShowToEnd
- ReferenceShowP8Tests.EveryP8SequenceAndShow_ReplaysExactlyAsReference

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`). Décision prise au développement (D39 précisée) : R6 telle quelle arrêtait le show dès qu'il atteignait une étape finale, ses scènes avec (une étape « Final » n'aurait rien joué) ; par défaut le show **tient** ses dernières étapes jusqu'à ce qu'on l'arrête, « s'arrêter » et « reprendre au début » restent au choix. |
