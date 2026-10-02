# SHOW-007 – Aperçu

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P8 |
| **Source** | [doc 20 – 2.2 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Essai d'une séquence au métronome (BPM réglable) ou au tempo du direct, dans sa fenêtre d'édition, sur l'aperçu en aveugle. |
| **Liens** | — |

## Description

> Aperçu : lecture de la séquence au simulateur avec un **métronome** (BPM réglable) sans musique.

**Critère d'acceptation** : —

## Réalisation

- tools/Luxia.Tools.Headless/ProjectCommands.cs
- src/Luxia.UI.Modules.Control/Sequencing/SimulationViewModel.cs

## Tests

- ShowScenarioTests.Scenario_PlaysAShowWithSimulatedDrop_AndSummarizesItsSteps
- SequencingScreensTests.ShowEditor_BlindTrial_RunsOnThePreviewOnly_WithSimulatedMusic
- SequencingScreensTests.MetronomeOnTheOutput_GivesTheLiveTempoBack_WhenTheEditorCloses

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 2.2 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`) : `luxia-headless jouer --sequence "nom" --tempo 120` ; l'aperçu dans la fenêtre d'édition (maquette 11) viendra au lot 5. |
| 2026-10-02 | Claude | Développement | P8 lot 5 (`4ceb412`) : ▶ Jouer, case métronome, tête de lecture sur la frise ; en aveugle l'aperçu garde son propre tempo (`PreviewOwnTempo`). |
| 2026-10-02 | Claude | Développement | Seconde relecture : un essai au métronome sur la sortie laissait l'horloge du direct en tempo fixe après la fermeture de l'éditeur (elle ne suivait plus la musique écoutée) ; l'éditeur rend désormais la source et le tempo d'avant l'essai. |
