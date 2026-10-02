# SHOW-003 – Sur une piste, un bloc lance sa scène dans la couche de la piste au début du bloc et l'arr

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 2.2 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Blocs lancés et arrêtés au tick près sur l'horloge ; relais sur la même piste sans noir ; exclusivité de la couche respectée. |
| **Liens** | — |

## Description

> Sur une piste, un bloc lance sa scène dans la couche de la piste au début du bloc et l'arrête (ou laisse la suivante la remplacer) à la fin ; l'exclusivité de la couche est respectée.

**Critère d'acceptation** : Test moteur : commandes émises aux bons temps.

## Réalisation

- src/Luxia.Show/Runtime/SequenceRun.cs
- src/Luxia.Engine/RenderEngine.Sequencer.cs

## Tests

- SequencePlaybackTests.Blocks_StartAndStopOnTheirBars_AtAnyTempo
- SequencePlaybackTests.Loop_KeepsTheSceneWithoutRelaunch_AndHandsOverOnTheSameTrack
- SequencePlaybackTests.SameSceneOnConsecutiveBlocks_IsNotInterrupted

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 2.2 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 3 (`4c87f9e`) : le séquenceur tourne dans le tick du moteur (D37) ; un bloc lance sa scène (sauf si elle joue déjà) et l'arrête à sa fin, sauf relais au même instant sur la même piste (même scène, ou couche exclusive) ou fin « laisser ». Défaut trouvé par les tests et corrigé : un bloc posé après la fin de la séquence empêchait l'arrêt du précédent. |
