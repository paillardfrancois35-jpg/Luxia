# SHOW-020 – Éditeur graphique

| Champ | Valeur |
|---|---|
| **Statut** | Non réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Modèle et exécution prêts (divergences OU / ET, macro-étapes) ; éditeur par cartes et diagramme automatique (Q45) au lot 5. |
| **Liens** | — |

## Description

> Éditeur graphique : poser des étapes, des transitions, les relier ; divergences / convergences OU et ET ; macro-étapes.

**Critère d'acceptation** : Construire l'exemple du §3.4.

## Réalisation

- src/Luxia.Show/Model/ShowDefinition.cs
- src/Luxia.Show/Runtime/ShowRun.cs

## Tests

- ShowExecutionTests.AndDivergenceAndConvergence_RunBranchesInParallel_ThenJoin
- ShowExecutionTests.MacroStep_WaitsForItsSubShowToEnd

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lots 1, 2 et 4 : maquette 12 soumise ; `shows.json` (doc 50 §12h) ; l'exemple du doc 20 §3.4 est écrit dans le show de référence (*Couplet / Refrain / Drop*, `722853b`). |
