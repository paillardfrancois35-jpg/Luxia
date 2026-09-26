# INST-033 – Opérations d'ordre sur une sélection

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P3 |
| **Source** | [doc 13 – 4. Sélections](../13-installation-et-lieux.md) |
| **Remarque** | — |
| **Liens** | INST-030, INST-051 |

## Description

> Opérations de sélection : inverser l'ordre, pair / impair, moitié gauche / droite, **ordre selon la position au plan** (gauche→droite, avant→arrière, du centre vers l'extérieur).

**Critère d'acceptation** : Sélection triée par position.

## Réalisation

- `src/Dmx.Patch/Rules/SelectionRules.cs` : `Reverse`, `Odd`, `Even`, `FirstHalf`, `SecondHalf`, `OrderByPosition` (d'après le lieu actif).
- `src/Dmx.UI.Modules.Installation/InstallationViewModel.cs` : `ReorderSelectionCommand`.

## Tests

- `SelectionRulesTests` (5 tests)
- `InstallationViewModelTests.CreateSelection_FromCheckedFixtures_ThenReverse`
- `InstallationViewModelTests.ReorderSelection_ByPosition_UsesActiveVenuePlacements`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §4). |
| 2026-09-26 | Claude | Développement | `5ee2e57` feat(patch): nouveau projet Dmx.Patch, modèle de domaine de l'installation ; `18f4ff5` (tri par position relié à l'écran) |
