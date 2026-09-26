# SORT-008 – Canaux maintenus pendant le test de sortie

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P3 |
| **Source** | [doc 10 – 3. Exigences – routage et supervision (côté PC)](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | CMD-024, D19, D21, Q23, SORT-007 |

## Description

> Test de sortie : liste de **canaux maintenus** à la valeur de test pendant tout le chenillard (ex. `1, 8, 15, 22` = gradateurs maîtres des 4 PAR), pour voir réagir les appareils à gradateur maître ; jamais un canal exclu.

**Critère d'acceptation** : PAR en 7 canaux, canal 1 maintenu : le chenillard sur 2, 3, 4 donne rouge, vert, bleu.

## Réalisation

- `src/Dmx.Messaging/Commands/TestOutputCommand.cs` : `HeldChannels`.
- `src/Dmx.Engine/TestPattern.cs` : maintien à la valeur de test, exclusions toujours respectées.
- `src/Dmx.Core/Settings/Preferences.cs` : `TestOutputPreferences.HeldChannels`.
- `src/Dmx.Hosting/DmxRuntime.cs` : validation et transmission.
- `src/Dmx.UI.Modules.Outputs/OutputsView.axaml`, `OutputsViewModel.cs` : champ « Canaux maintenus ».

## Tests

- `RenderEngineTests.TestPattern_HeldChannels_StayLitForTheWholeChase`
- `RenderEngineTests.TestPattern_HeldChannels_RespectExcludedChannels`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Claude | Question | Q23 : un chenillard canal par canal n'allume pas un appareil à gradateur maître (PAR en 7 canaux : le maître est à 0 quand la couleur passe). Proposition : liste de canaux maintenus. |
| 2026-09-25 | Utilisateur | Question | Demande de précision sur Q23 ; question reformulée avec l'exemple du PAR en 7 canaux. |
| 2026-09-25 | Utilisateur | Réponse | Oui, en P3. |
| 2026-09-25 | Conception | Création | Exigence SORT-008 ajoutée au doc 10 §3 (M, P3) ; P3 du doc 40 complétée. |
| 2026-09-25 | Claude | Décision | Un canal à la fois exclu **et** maintenu reste exclu (la sûreté prime : fumée, Reset). |
| 2026-09-26 | Claude | Développement | `264de27` feat(sortie): SORT-008, canaux maintenus pendant le test de sortie |
