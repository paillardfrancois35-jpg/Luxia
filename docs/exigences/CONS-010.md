# CONS-010 – Instantanés de console

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | S |
| **Phase** | P1 |
| **Source** | [doc 11 – 3. Exigences – mode canaux (P1)](../11-console.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Mémoriser / rappeler des « instantanés » de console (état de tous les faders pris) pour les tests répétitifs.

**Critère d'acceptation** : Rappel d'un instantané.

## Réalisation

- `src/Dmx.Core/Snapshots/ConsoleSnapshot.cs`
- `src/Dmx.Hosting/DmxRuntime.cs`
- `src/Dmx.Messaging/Commands/OverrideChannelsCommand.cs`
- `src/Dmx.UI.Modules.Console/ConsoleView.axaml`
- `src/Dmx.UI.Modules.Console/ConsoleViewModel.cs`
- `src/Dmx.UI.Modules.Console/SnapshotViewModel.cs`

## Tests

- `ConsoleViewModelTests.Snapshot_SaveReleaseRecall_RestoresOverrides`
- `ReferenceShowP1Tests.ReferenceShow_LoadsWithSnapshots`
- `ReferenceShowP1Tests.Snapshot_RecalledByEngine_ProducesExactlyItsChannels`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 3. Exigences – mode canaux (P1)). |
| 2026-09-24 | Claude | Décision | Réalisé dès P1 bien que de priorité S : les démonstrations P1 (doc 40 §7.3) exigent des instantanés. Rangés dans le projet (`console.json`) ; un rappel remplace les faders pris de l'univers. |
| 2026-09-24 | Claude | Développement | `87237c9` feat(console): console en mode canaux, moniteur, instantanés, coquille de l'application |
| 2026-09-24 | Claude | Développement | `bff6da6` docs(p1): démonstration P1, instantanés du show de référence, notes de réalisation |
| 2026-09-24 | Claude | Note | 6 instantanés dans le show de référence (PAR, lyres, UV), établis d'après les notices ; un test vérifie qu'aucun ne touche la fumée (180) ni le Reset des lyres (120, 135). |
