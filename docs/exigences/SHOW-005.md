# SHOW-005 – Modes de lecture

| Champ | Valeur |
|---|---|
| **Statut** | Validé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 2.2 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Une fois ou en boucle ; départ quantifié (temps, mesure, phrase de 4, 8 ou 16 mesures ; mesure par défaut). |
| **Liens** | — |

## Description

> Modes de lecture : une fois, boucle ; **démarrage quantifié** (prochaine mesure / phrase).

**Critère d'acceptation** : —

## Réalisation

- src/Luxia.Show/Runtime/SequenceRun.cs
- src/Luxia.Show/Runtime/Sequencer.cs

## Tests

- SequencePlaybackTests.Launch_IsQuantizedToTheNextBar
- SequencePlaybackTests.Loop_KeepsTheSceneWithoutRelaunch_AndHandsOverOnTheSameTrack
- SequencePlaybackTests.Loop_ABlockCoveringThePass_KeepsItsSceneAcrossTheLoop

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 2.2 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 3 (`4c87f9e`). |
| 2026-10-02 | Claude | Correction | Essai P8 2a/2b (v1.010.079) : un bloc qui couvre tout le passage était arrêté puis relancé au rebouclage (noir d'un tick, scène rythmée décalée) ; il se relaie désormais à lui-même. Exemple *Groove 8 mesures* refait (couleur, vague et intensité séparées). |
| 2026-10-02 | Utilisateur | Test | Ex. 2d (v1.010.085) : reprise au début sans noir, la couleur suit son cours ✅. |
| 2026-10-02 | Utilisateur | Validation | Validé avec la phase P8 (v1.010, essai v1.010.079 → .104, contrôle final v1.010.116). ex. 2d. |
