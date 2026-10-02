# SHOW-004 – Blocs d'actions en plus des scènes

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 2.2 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Niveau de couche et Grand Master en rampe, fumée, flash, noir ; seuls début et fin d'une rampe vont au journal des commandes. |
| **Liens** | — |

## Description

> Blocs d'**actions** en plus des scènes : régler un master (valeur fixe ou rampe), fumée (rafale), flash, blackout court.

**Critère d'acceptation** : Rampe de master sur 4 mesures.

## Réalisation

- src/Luxia.Show/Runtime/SequenceRun.cs

## Tests

- SequencePlaybackTests.LayerLevelRamp_OverFourBars
- SequencePlaybackTests.FlashAndBlackoutBlocks_LastTheirBlock
- ReferenceShowP8Tests.Rise_RampsTheColorsLayerOverEightBars

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 2.2 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 3 (`4c87f9e`) : piste d'actions ; rampe de `from` (ou du niveau courant) à `to` sur la durée du bloc, le niveau reste ensuite. |
