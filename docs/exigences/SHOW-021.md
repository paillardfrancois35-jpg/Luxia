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
| 2026-10-02 | Utilisateur | Test | Ex. 8 (v1.010.085) : strobe de l'étape « Éclat » du *Bloc refrain* vu ≈ 1 s au lieu de 4 s ❌. |
| 2026-10-02 | Claude | Correction | Contenu : l'étape jouait le strobe des PAR sans couleur (le strobe n'ouvre que l'obturateur : PAR à LED noirs hors flash de 0,5 s). Étape complétée de « Blanc sur tous les PAR (couleur seule) » ; la durée de l'étape (4 s) et le limiteur étaient corrects. |
| 2026-10-02 | Utilisateur | Test | Re-vérification v1.010.104, ex. 8 : strobe blanc des PAR visible ≈ 4 s dans « Éclat » ✅. |
