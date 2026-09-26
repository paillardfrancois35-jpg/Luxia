# LIVE-040 – Raccourcis du tableau ci-dessus

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 5. Raccourcis clavier (proposition)](../18-live.md) |
| **Remarque** | B, F, S, Z, G, Échap, 1-9 (couche encadrée), ← →, Page ↑↓ : fait, actifs quel que soit le focus hors saisie de texte, répétition automatique ignorée, relâche toujours traitée. Espace (tap, P7), Ctrl+A (auto, P10) et N (show, P8) avec leur phase. |
| **Liens** | GEN-071 |

## Description

> Raccourcis du tableau ci-dessus.

**Critère d'acceptation** : Test manuel.

## Réalisation

- `src/Luxia.UI.Modules.Live/LiveKey.cs`
- `src/Luxia.UI.Modules.Live/LiveViewModel.cs` (`OnKey`)
- `src/Luxia.App/MainWindow.axaml.cs`

## Tests

- `LiveViewModelTests.Keys_FlashHeldWithAutoRepeat_ThenReleased`
- `LiveViewModelTests.Keys_ArrowsChooseLayer_DigitsLaunchItsScenes_GFreezes_PageDownLowersMaster`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 5. Raccourcis clavier (proposition)). |
| 2026-09-26 | Claude | Développement | `dbb3b1e` feat(live): écran Live |
