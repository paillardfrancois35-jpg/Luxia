# SHOW-022 – Réceptivités du §3

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Toutes les réceptivités du §3.2 sauf « X a bouclé N fois » pour une scène (fait pour une séquence) ; style simulé avant P9 ; morceau changé provisoire (D38). |
| **Liens** | — |

## Description

> Réceptivités du §3.2, combinables en ET / OU / NON ; quantification par transition.

**Critère d'acceptation** : Tests par condition.

## Réalisation

- src/Luxia.Show/Runtime/ShowRun.cs
- src/Luxia.Show/Rules/ShowTexts.cs

## Tests

- ShowExecutionTests.Conditions_TimeEnergyStyleTempoSongAndLogic
- ShowExecutionTests.Drop_QuantizedOnTheBar_FiresAtTheNextBar_AndR5KeepsARepeatedScene
- ShowExecutionTests.ManualTransition_IsOnlyForced_AtItsQuantization
- ShowExecutionTests.RandomCondition_DrawsOncePerBar
- ShowRulesTests.BadConditions_AreReported

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`) : une transition quantifiée est armée quand sa condition devient vraie et part à la frontière suivante (D39). Écart : « la scène X a bouclé N fois » n'est fait que pour les séquences (`sequenceLoops`). |
| 2026-10-02 | Utilisateur | Test | Ex. 13 avec musique (v1.010.091, morceau « Animals », traces CSV) : couplet, montée, refrain aux drops, retour aux breaks ✅ ; 3e refrain coupé par un break puis silence : le Final n'arrive jamais 💡. |
| 2026-10-02 | Claude | Correction | *Couplet / Refrain / Drop* : « au silence » mène au Final depuis Couplet, Refrain et Montée ; « à la reprise du son » ramène du Final à l'Intro, qui remet le compteur de refrains à zéro. |
