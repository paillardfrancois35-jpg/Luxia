# SHOW-027 – Mode simulation dans l'éditeur

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Panneau « Essai sans musique » de l'éditeur de show : métronome, Drop, Break, Montée, Silence, Morceau suivant, énergie, style. |
| **Liens** | — |

## Description

> **Mode simulation** dans l'éditeur : boutons pour provoquer les événements (drop, break, morceau changé, énergie…) et une horloge métronome, pour tester un show sans musique.

**Critère d'acceptation** : Tester l'exemple §3.4 sans musique.

## Réalisation

- src/Luxia.Engine/RenderEngine.Sequencer.cs
- src/Luxia.Hosting/Tools/Scenario.cs
- src/Luxia.UI.Modules.Control/Sequencing/SimulationViewModel.cs

## Tests

- ShowScenarioTests.Scenario_PlaysAShowWithSimulatedDrop_AndSummarizesItsSteps
- ShowExecutionTests.Conditions_TimeEnergyStyleTempoSongAndLogic
- SequencingScreensTests.ShowEditor_BlindTrial_RunsOnThePreviewOnly_WithSimulatedMusic

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lots 3 et 4 (`4c87f9e`, `d46142e`) : drop, break, montée, silence, reprise, morceau changé, énergie imposée, style simulé ; l'exemple du §3.4 est testé sans musique. |
| 2026-10-02 | Claude | Développement | P8 lot 5 (`4ceb412`). |
| 2026-10-02 | Claude | Développement | Relecture : cocher « Aveugle » pendant un essai arrêtait l'essai sur l'aperçu au lieu de la sortie ; corrigé (l'essai s'arrête du côté qu'on quitte). |
| 2026-10-02 | Utilisateur | Test | Ex. 12 (v1.010.085) : boutons Drop, Break, Montée, énergie ; carte et diagramme suivent ✅ ; interface jugée très complexe 💡. |
