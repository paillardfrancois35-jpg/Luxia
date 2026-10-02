# SHOW-021 – Actions continues, mémorisées, impulsionnelles

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Actions continues (jouer une scène, une séquence), mémorisées (lancer, arrêter, couche, niveau, vitesse), impulsionnelles (flash, fumée, noir court, variable) ; verrous en P10. |
| **Liens** | — |

## Description

> Actions continues, mémorisées, impulsionnelles (§3.1) avec les commandes du catalogue (lancer / arrêter scène, séquence, couche ; master ; flash ; fumée ; verrou ; vitesse).

**Critère d'acceptation** : —

## Réalisation

- src/Luxia.Show/Runtime/ShowRun.cs

## Tests

- ShowExecutionTests.Launch_ActivatesTheInitialStepAndPlaysItsScenes
- ShowExecutionTests.LeavingAStep_StopsItsContinuousScenes
- ShowExecutionTests.PulseActions_FlashAndShortBlackout_EndOnTheirOwn
- ShowExecutionTests.Variables_CountChoruses_ThenTheFinalVariant

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`). Le verrou (« poser un verrou ») attend le Directeur (P10). |
