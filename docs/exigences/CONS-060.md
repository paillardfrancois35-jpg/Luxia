# CONS-060 – Composant « faders d'un appareil » réutilisable

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P2 |
| **Source** | [doc 11 – 6. Intégration dans d'autres écrans](../11-console.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Le composant « faders d'un appareil » est réutilisable dans l'éditeur de bibliothèque pour le **test en direct** d'un modèle (BIB-060) (GEN-004).

**Critère d'acceptation** : Même composant, même comportement.

## Réalisation

- `src/Luxia.UI.Modules.Console/FixtureChannelViewModel.cs`
- `src/Luxia.UI.Modules.Console/FixtureFadersView.axaml.cs`
- `src/Luxia.UI.Modules.Console/FixtureFadersViewModel.cs`
- `src/Luxia.UI.Modules.Library/LibraryViewModel.cs`

## Tests

- `LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 6. Intégration dans d'autres écrans). |
| 2026-09-25 | Claude | Décision | Composant « faders d'un appareil » placé dans le module Console (et non Bibliothèque) pour être réutilisé par la console en mode appareils (P3). |
| 2026-09-25 | Claude | Développement | `175370e` feat(bibliotheque): écran Bibliothèque, éditeur, test en direct, découverte |
