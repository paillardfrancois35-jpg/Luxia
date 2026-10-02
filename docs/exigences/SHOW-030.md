# SHOW-030 – Métadonnées du show pour le Directeur

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Rôle, styles, plage d'énergie, poids, durée maximale enregistrés ; utilisés par le Directeur en P10. |
| **Liens** | — |

## Description

> Métadonnées du show pour le Directeur : styles visés, plage d'énergie, poids, durée max, rôle (principal / transition / attente / slow) (doc 22).

**Critère d'acceptation** : —

## Réalisation

- src/Luxia.Show/Model/ShowDefinition.cs

## Tests

- ShowRulesTests.Stores_RoundTrip_SequencesAndShows

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 2 (`683cfc8`). |
