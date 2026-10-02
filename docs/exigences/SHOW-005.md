# SHOW-005 – Modes de lecture

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
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

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 2.2 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 3 (`4c87f9e`). |
