# CONS-004 – Commandes de libération et de page

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P1 |
| **Source** | [doc 11 – 3. Exigences – mode canaux (P1)](../11-console.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Commandes : libérer un fader, libérer la page, **tout libérer** ; mettre la page à 0 ; mettre la page à 255.

**Critère d'acceptation** : Test.

## Réalisation

- `src/Luxia.Messaging/Commands/ReleaseOverridesCommand.cs`
- `src/Luxia.UI.Modules.Console/ConsoleViewModel.cs`

## Tests

- `ChannelOverrideTests.ReleaseAll_ClearsEveryUniverse`
- `ConsoleViewModelTests.PageToFull_ThenReleasePage`
- `ConsoleViewModelTests.ReleaseSelection_OnlyReleasesSelectedChannels`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 3. Exigences – mode canaux (P1)). |
| 2026-09-24 | Claude | Développement | `6f94990` feat(moteur): surcharges brutes de la console (CMD-020, CMD-022) |
| 2026-09-24 | Claude | Développement | `87237c9` feat(console): console en mode canaux, moniteur, instantanés, coquille de l'application |
