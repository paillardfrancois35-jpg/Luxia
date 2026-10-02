# SHOW-027 – Mode simulation dans l'éditeur

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Commande SimulerMusique (CMD-053) et verbes du scénario ; boutons de la fenêtre d'édition au lot 5. |
| **Liens** | — |

## Description

> **Mode simulation** dans l'éditeur : boutons pour provoquer les événements (drop, break, morceau changé, énergie…) et une horloge métronome, pour tester un show sans musique.

**Critère d'acceptation** : Tester l'exemple §3.4 sans musique.

## Réalisation

- src/Luxia.Engine/RenderEngine.Sequencer.cs
- src/Luxia.Hosting/Tools/Scenario.cs

## Tests

- ShowScenarioTests.Scenario_PlaysAShowWithSimulatedDrop_AndSummarizesItsSteps
- ShowExecutionTests.Conditions_TimeEnergyStyleTempoSongAndLogic

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lots 3 et 4 (`4c87f9e`, `d46142e`) : drop, break, montée, silence, reprise, morceau changé, énergie imposée, style simulé ; l'exemple du §3.4 est testé sans musique. |
