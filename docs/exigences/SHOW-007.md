# SHOW-007 – Aperçu

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | M |
| **Phase** | P8 |
| **Source** | [doc 20 – 2.2 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Le moteur joue une séquence au tempo fixe choisi (métronome) ; boutons d'essai de la fenêtre d'édition au lot 5. |
| **Liens** | — |

## Description

> Aperçu : lecture de la séquence au simulateur avec un **métronome** (BPM réglable) sans musique.

**Critère d'acceptation** : —

## Réalisation

- tools/Luxia.Tools.Headless/ProjectCommands.cs

## Tests

- ShowScenarioTests.Scenario_PlaysAShowWithSimulatedDrop_AndSummarizesItsSteps

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 2.2 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`) : `luxia-headless jouer --sequence "nom" --tempo 120` ; l'aperçu dans la fenêtre d'édition (maquette 11) viendra au lot 5. |
