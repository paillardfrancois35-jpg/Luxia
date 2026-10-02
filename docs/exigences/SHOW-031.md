# SHOW-031 – Shows secondaires parallèles

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | S |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Shows secondaires en parallèle du show principal. |
| **Liens** | — |

## Description

> Shows **secondaires** parallèles (ex. un show « ambiance UV/fumée » indépendant du show principal).

**Critère d'acceptation** : —

## Réalisation

- src/Luxia.Show/Runtime/Sequencer.cs

## Tests

- ShowExecutionTests.OnlyOneMainShow_SecondaryShowsRunAlongside

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`) ; exemple *Ambiance UV et fumée (secondaire)* (`722853b`). |
