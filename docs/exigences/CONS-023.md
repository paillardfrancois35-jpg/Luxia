# CONS-023 – Clic sur une plage et balayage

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P3 |
| **Source** | [doc 11 – 4. Exigences – mode appareils (P3)](../11-console.md) |
| **Remarque** | Repris tel quel du test en direct de la bibliothèque (BIB-061, BIB-062), déjà réalisé en P2. |
| **Liens** | BIB-061, BIB-062, CONS-020 |

## Description

> Clic sur un nom de plage → la valeur médiane de la plage est émise ; un curseur permet de **balayer** la plage.

**Critère d'acceptation** : Test sur le canal Strobe d'un PAR.

## Réalisation

- `src/Luxia.UI.Modules.Console/FixtureFadersViewModel.cs` : `SelectRange`, `StartDiscovery` (réutilisés sans changement, le composant est le même en Console mode appareils qu'à la bibliothèque, GEN-004).

## Tests

- `ConsoleViewModelTests.Fader_PatchedChannelWithCapabilities_ShowsRangeNameInsteadOfPercent` (patch réel avec plages)
- Couverture complète héritée des tests BIB-061/BIB-062 (`LibraryViewModelTests`), même composant.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 4. Exigences – mode appareils (P3)). |
| 2026-09-26 | Claude | Décision | Aucun développement propre : le composant `FixtureFadersViewModel` (CONS-060) offre déjà ce comportement depuis P2 ; le mode appareils de la Console l'utilise tel quel (GEN-004). |
| 2026-09-26 | Claude | Développement | `5d60230` feat(console): mode appareils (CONS-020 à 024), CONS-007, CONS-043, CONS-092 |
