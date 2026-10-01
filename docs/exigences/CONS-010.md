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

- `src/Luxia.Core/Snapshots/ConsoleSnapshot.cs`
- `src/Luxia.Hosting/LuxiaRuntime.cs`
- `src/Luxia.Messaging/Commands/OverrideChannelsCommand.cs`
- `src/Luxia.UI.Modules.Console/ConsoleView.axaml`
- `src/Luxia.UI.Modules.Console/ConsoleViewModel.cs`
- `src/Luxia.UI.Modules.Console/SnapshotViewModel.cs`

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
| 2026-09-24 | Claude | Note | 6 instantanés dans le show de référence (PAR, lyres, UV), établis d'après les notices ; un test vérifie qu'aucun ne touche la fumée (180) ni le Reset des lyres. |
| 2026-09-25 | Claude | Note | BIB-095 : la lyre Tomshine a bien un canal Reset, mais en dernière position du mode (121 et 136, pas 120 et 135 comme dans la définition d'origine, fausse). Protection mise à jour en conséquence. |
| 2026-09-25 | Claude | Écart | Un essai en direct de l'utilisateur (instantané « 3-RGB », fader 120 laissé à 205) a été écrit par l'application dans le fichier échantillon `samples/Show de référence/console.json` (le dossier était ouvert comme projet de test), puis committé par mégarde (`32996cf`) : 2 tests d'intégration en échec. Retiré, tests remis au vert. À l'avenir : ouvrir un dossier de travail séparé pour les essais manuels, jamais le show de référence livré. |
