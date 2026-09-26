# CONS-007 – Appareil, attribut et plage sur le fader

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P1 |
| **Source** | [doc 11 – 3. Exigences – mode canaux (P1)](../11-console.md) |
| **Remarque** | — |
| **Liens** | CONS-041, INST-001 |

## Description

> Si le canal appartient à un appareil patché, affichage du nom de l'appareil, de l'attribut, et du **nom de la plage** courante (ex. « Strobe : lent→rapide, 42 % »).

**Critère d'acceptation** : Test avec un appareil patché (dès P3).

## Réalisation

- `src/Dmx.Patch/Rules/PatchLookup.cs` : résolution canal → appareil patché → définition de canal.
- `src/Dmx.UI.Modules.Console/ChannelViewModel.cs` : `Caption` rendue observable.
- `src/Dmx.UI.Modules.Console/ConsoleViewModel.cs` : `Caption` et `PercentText` (nom de plage au lieu du %) recalculés à chaque rafraîchissement via `DmxConversion.Describe`.

## Tests

- `ConsoleViewModelTests.MonitorHover_PatchedChannel_ShowsFixtureAndAttribute`
- `ConsoleViewModelTests.Fader_PatchedChannelWithCapabilities_ShowsRangeNameInsteadOfPercent`
- `PatchLookupTests.FindChannel_ResolvesChannelWithinFixtureRange`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 3. Exigences – mode canaux (P1)). |
| 2026-09-24 | Claude | Décision | Reporté à P3 (nécessite le patch, doc 13). |
| 2026-09-26 | Claude | Développement | `5d60230` feat(console): mode appareils (CONS-020 à 024), CONS-007, CONS-043, CONS-092 ; `6c50fd6` feat(console): CONS-007, nom de plage affiché sur le fader. |
