# SHOW-023 – Exécution conforme aux règles R1 à R6

| Champ | Valeur |
|---|---|
| **Statut** | Validé |
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
- ShowExecutionTests.EditingAPlayingShow_KeepsArmedTransitions_AndUpdatesTheActiveStep

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`). Décision prise au développement (D39 précisée) : R6 telle quelle arrêtait le show dès qu'il atteignait une étape finale, ses scènes avec (une étape « Final » n'aurait rien joué) ; par défaut le show **tient** ses dernières étapes jusqu'à ce qu'on l'arrête, « s'arrêter » et « reprendre au début » restent au choix. |
| 2026-10-02 | Claude | Développement | Relecture de fin de développement (avant l'essai) : modifier un show pendant qu'il joue (essai dans l'éditeur) ne fait plus oublier les transitions armées inchangées (un drop retenu jusqu'à la mesure), applique tout de suite les actions continues de l'étape active, et met à jour le sous-show d'une macro-étape. Test `ShowExecutionTests.EditingAPlayingShow_KeepsArmedTransitions_AndUpdatesTheActiveStep`. |
| 2026-10-02 | Claude | Développement | Exemple *Visite guidée (sans musique)* ajouté au show de référence : un show qui avance seul (durées en mesures, fins de séquences), pour l'essai sans musique (guide P8, exemple 5). |
| 2026-10-02 | Utilisateur | Test | Ex. 5 *Visite guidée* (v1.010.079) : étapes par durée et par fin de séquence, reprise après le final ✅. |
| 2026-10-02 | Utilisateur | Validation | Validé avec la phase P8 (v1.010, essai v1.010.079 → .104, contrôle final v1.010.116). ex. 5, 8. |
