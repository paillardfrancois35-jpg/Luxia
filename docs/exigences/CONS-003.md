# CONS-003 – Prise et libération d'un fader

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P1 |
| **Source** | [doc 11 – 3. Exigences – mode canaux (P1)](../11-console.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Toucher un fader le **prend** : sa valeur surcharge la sortie (étape 11 de la chaîne de rendu) jusqu'à ce qu'il soit **libéré**. Un fader pris est signalé visuellement.

**Critère d'acceptation** : Fader pris → valeur émise quelle que soit la scène active ; libéré → la scène reprend la main.

## Réalisation

- `src/Luxia.Engine/ChannelOverrides.cs`
- `src/Luxia.Engine/RenderEngine.cs`
- `src/Luxia.Messaging/Commands/OverrideChannelsCommand.cs`
- `src/Luxia.Messaging/Commands/ReleaseOverridesCommand.cs`
- `src/Luxia.UI.Controls/Fader.cs`
- `src/Luxia.UI.Modules.Console/ConsoleViewModel.cs`

## Tests

- `ChannelOverrideTests.Override_SetsChannelValueOnNextTick`
- `ChannelOverrideTests.Override_ToZero_IsStillAnOverride`
- `ChannelOverrideTests.Release_ReturnsChannelToChainValue`
- `ConsoleLatencyTests.Override_AppearsInRecordedFrame_OnNextTick`
- `ConsoleLatencyTests.Override_ReachesDriver_InLessThan50Milliseconds`
- `ConsoleViewModelTests.FaderRequest_OverridesChannel_AndFaderShowsEmittedValue`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 3. Exigences – mode canaux (P1)). |
| 2026-09-24 | Claude | Décision | Glisser **relatif** (pas de saut au point cliqué) ; un clic simple prend le fader à sa valeur actuelle. Saisie directe dans la case sous le fader (ou double-clic). |
| 2026-09-24 | Claude | Développement | `6f94990` feat(moteur): surcharges brutes de la console (CMD-020, CMD-022) |
| 2026-09-24 | Claude | Développement | `87237c9` feat(console): console en mode canaux, moniteur, instantanés, coquille de l'application |
