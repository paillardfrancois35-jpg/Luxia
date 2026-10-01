# CONS-020 – Faders regroupés par appareil patché

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 11 – 4. Exigences – mode appareils (P3)](../11-console.md) |
| **Remarque** | — |
| **Liens** | CONS-060, GEN-004 |

## Description

> Les faders sont regroupés **par appareil patché** (bandeau avec le nom et la couleur de l'appareil), dans l'ordre du patch ou d'une sélection.

**Critère d'acceptation** : Vue par appareil.

## Réalisation

- `src/Luxia.UI.Modules.Console/ConsoleViewModel.cs` : `IsDeviceMode`, `DeviceFixtures` (un `FixtureFadersViewModel` par appareil patché de l'univers affiché, trié par adresse — CONS-060, GEN-004).
- `src/Luxia.UI.Modules.Console/ConsoleView.axaml` : bascule « Mode : Canaux / Appareils », panneau des groupes.
- `src/Luxia.UI.Modules.Console/FixtureFadersView.axaml` : bandeau nom + bouton Identifier ajouté (partagé avec le test en direct de la bibliothèque).

## Tests

- `ConsoleViewModelTests.DeviceMode_BuildsOneGroupPerPatchedFixture_AndFaderOverridesTheAttribute`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 4. Exigences – mode appareils (P3)). |
| 2026-09-26 | Claude | Décision | Regroupement par ordre du patch (adresse croissante) dans l'univers affiché ; le tri par sélection viendra avec les sélections de l'écran Installation. |
| 2026-09-26 | Claude | Développement | `5d60230` feat(console): mode appareils (CONS-020 à 024), CONS-007, CONS-043, CONS-092 |
