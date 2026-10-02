# SHOW-008 – Variation de la vitesse relative

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | S |
| **Phase** | P8 |
| **Source** | [doc 20 – 2.2 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Vitesse relative de la séquence : ½ temps, normale, double (bornée de ¼ à 4). |
| **Liens** | — |

## Description

> Variation de la vitesse relative (séquence en « demi-temps » / « double temps »).

**Critère d'acceptation** : —

## Réalisation

- src/Luxia.Show/Runtime/SequenceRun.cs

## Tests

- SequencePlaybackTests.DoubleTime_PlaysTwiceAsFast

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 2.2 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 3 (`4c87f9e`). |
| 2026-10-02 | Claude | Développement | Exemple *Pulsation couleurs (double temps)* ajouté au show de référence (guide P8, exemple 4). |
