# INST-013 – Détection des chevauchements en temps réel

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | — |
| **Liens** | INST-012, INST-014, CONS-043 |

## Description

> **Détection des chevauchements** en temps réel (erreur visible dans la liste et la barre d'univers).

**Critère d'acceptation** : Chevauchement signalé immédiatement.

## Réalisation

- `src/Luxia.Patch/Rules/PatchRules.cs` : `DetectOverlaps` (ignore les jumeaux, INST-014).
- `src/Luxia.UI.Modules.Installation/InstallationViewModel.cs`, `PatchRowViewModel.cs` : `HasOverlap`, recalculé après chaque modification du patch.
- `src/Luxia.UI.Modules.Installation/InstallationView.axaml` : icône ⚠ sur la ligne concernée.

## Tests

- `PatchRulesTests.DetectOverlaps_FindsOverlappingRange`
- `PatchRulesTests.DetectOverlaps_DifferentUniverses_NoOverlap`
- `InstallationViewModelTests.OverlappingFixtures_AreFlagged`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Développement | `389fe64` feat(installation): écran Installation — patch, sélections, lieux (doc 13) |
