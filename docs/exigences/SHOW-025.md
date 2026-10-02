# SHOW-025 – Un seul show principal actif à la fois

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Lancer un show principal arrête le précédent ; les scènes que le nouveau rejoue d'emblée ne sont pas coupées. |
| **Liens** | — |

## Description

> Un **seul show principal** actif à la fois (lancé à la main ou par le Directeur) ; lancer un show arrête le précédent (fondu).

**Critère d'acceptation** : —

## Réalisation

- src/Luxia.Show/Runtime/Sequencer.cs

## Tests

- ShowExecutionTests.OnlyOneMainShow_SecondaryShowsRunAlongside

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`). |
