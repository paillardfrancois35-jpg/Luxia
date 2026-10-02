# SHOW-031 – Shows secondaires parallèles

| Champ | Valeur |
|---|---|
| **Statut** | Validé |
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
- ShowExecutionTests.Stop_KeepsSecondaryShows_ButStopEverythingDoesNot

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`) ; exemple *Ambiance UV et fumée (secondaire)* (`722853b`). |
| 2026-10-02 | Claude | Développement | Relecture : « ■ Stop » (qui épargne les couches protégées) arrêtait aussi le show secondaire, donc l'UV et la fumée qu'il tient ; il le garde désormais (`StopShowCommand.KeepSecondary`), « ■ Tout stopper » arrête tout. |
| 2026-10-02 | Utilisateur | Test | Ex. 10 (v1.010.085) : show secondaire en parallèle, gardé par ■ Stop, arrêté par ■ Tout stopper ✅ ; fumée non testée (appareil non branché) ⏸. |
| 2026-10-02 | Utilisateur | Validation | Validé avec la phase P8 (v1.010, essai v1.010.079 → .104, contrôle final v1.010.116). ex. 10 ; fumée non testée au matériel (appareil non branché). |
