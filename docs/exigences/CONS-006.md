# CONS-006 – Sélection multiple de faders

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P1 |
| **Source** | [doc 11 – 3. Exigences – mode canaux (P1)](../11-console.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Sélection multiple de faders (Ctrl/Maj+clic) : un déplacement agit sur tous (en relatif ou en absolu, au choix).

**Critère d'acceptation** : Régler R, G, B ensemble.

## Réalisation

- `src/Dmx.Messaging/Commands/OverrideChannelsCommand.cs`
- `src/Dmx.UI.Controls/Fader.cs`
- `src/Dmx.UI.Modules.Console/ConsoleViewModel.cs`

## Tests

- `ConsoleViewModelTests.ClickWithoutModifier_OutsideSelection_SelectsOnlyThatFader`
- `ConsoleViewModelTests.MultiSelection_Absolute_SetsSameValue`
- `ConsoleViewModelTests.MultiSelection_Relative_MovesAllByTheSameDelta`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 3. Exigences – mode canaux (P1)). |
| 2026-09-24 | Claude | Développement | `87237c9` feat(console): console en mode canaux, moniteur, instantanés, coquille de l'application |
| 2026-09-24 | Claude | Note | Ctrl+clic : ajouter / retirer ; Maj+clic : plage ; clic simple hors sélection : ce seul fader ; Échap : désélection ; mode relatif par défaut. |
