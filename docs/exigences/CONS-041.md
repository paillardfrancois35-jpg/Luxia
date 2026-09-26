# CONS-041 – Infos au survol du moniteur

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P1 |
| **Source** | [doc 11 – 5. Moniteur de sortie](../11-console.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Au survol d'une case : numéro de canal, valeur, appareil et attribut (si patché).

**Critère d'acceptation** : Revue.

## Réalisation

- `src/Dmx.UI.Controls/OutputMonitor.cs`
- `src/Dmx.UI.Modules.Console/ConsoleViewModel.cs`

## Tests

- `ConsoleViewModelTests.MonitorHover_DescribesChannel`
- `ConsoleViewModelTests.MonitorHover_PatchedChannel_ShowsFixtureAndAttribute`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 5. Moniteur de sortie). |
| 2026-09-24 | Claude | Développement | `87237c9` feat(console): console en mode canaux, moniteur, instantanés, coquille de l'application |
| 2026-09-26 | Claude | Développement | `5d60230` feat(console): mode appareils (CONS-020 à 024), CONS-007, CONS-043, CONS-092 — appareil et attribut ajoutés au survol dès qu'un patch existe (CONS-007). |
