# LIVE-005 – Palettes rapides

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 3. Exigences – structure](../18-live.md) |
| **Remarque** | Sélections automatiques (tous, par catégorie) et enregistrées ; palettes couleur, position (du lieu actif) et intensité ; surcharge des attributs jusqu'à « Libérer » (Échap), qui ne rend que ce que les palettes rapides ont forcé. |
| **Liens** | PAL-004, CMD-021, CMD-022 |

## Description

> **Palettes rapides** : choix d'une sélection d'appareils puis clic sur une palette → surcharge des attributs correspondants (programmeur live) ; bouton « Libérer » pour rendre la main aux couches.

**Critère d'acceptation** : Forcer les lyres sur « Boule » puis libérer.

## Réalisation

- `src/Luxia.UI.Modules.Live/LiveViewModel.cs` (`ApplyPalette`, `ReleaseQuick`)

## Tests

- `LiveViewModelTests.QuickPalette_OverridesTheSelection_ThenReleaseGivesBack`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 3. Exigences – structure). |
| 2026-09-26 | Claude | Développement | `dbb3b1e` feat(live): écran Live |
