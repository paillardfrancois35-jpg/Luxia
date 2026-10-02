# SHOW-028 – Tirage pondéré

| Champ | Valeur |
|---|---|
| **Statut** | Validé |
| **Priorité** | M |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Tirage pondéré entre transitions vraies d'une même étape, anti-répétition. |
| **Liens** | — |

## Description

> Tirage pondéré : poids par transition ; option « éviter de refaire la même branche deux fois de suite ».

**Critère d'acceptation** : Distribution conforme sur 10 000 tirages (± 2 %).

## Réalisation

- src/Luxia.Show/Runtime/ShowRun.cs

## Tests

- WeightedDrawTests.Distribution_FollowsTheWeights_Within2Percent
- WeightedDrawTests.AvoidRepeat_NeverTakesTheSameBranchTwiceInARow

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`) : 10 000 tirages à 60 / 40, part mesurée à ± 2 %. |
| 2026-10-02 | Utilisateur | Test | Ex. 9 *Tirage au sort* (v1.010.085) ✅ ; le guide doit dire « Base → A ou B → Base » (corrigé au guide). |
| 2026-10-02 | Utilisateur | Validation | Validé avec la phase P8 (v1.010, essai v1.010.079 → .104, contrôle final v1.010.116). ex. 9. |
