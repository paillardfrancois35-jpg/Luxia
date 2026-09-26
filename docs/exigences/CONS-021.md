# CONS-021 – Outil adapté par type d'attribut

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 11 – 4. Exigences – mode appareils (P3)](../11-console.md) |
| **Remarque** | Fader et boutons de plages réalisés (réutilisés du test en direct, BIB-060/061) ; pastille couleur (R/G/B/W combinés) et pad Pan/Tilt XY restent à faire. |
| **Liens** | CONS-020, BIB-060, BIB-061 |

## Description

> Les attributs sont présentés avec l'outil adapté : fader pour les intensités et canaux simples ; sélecteur de **plages** (boutons) pour les canaux à plages ; **couleur** (pastille + faders R/G/B/W) ; **Pan/Tilt** (pad XY, 16 bits combinés).

**Critère d'acceptation** : Revue avec chaque type d'appareil du parc.

## Réalisation

- `src/Dmx.UI.Modules.Console/FixtureFadersView.axaml`, `FixtureChannelViewModel.cs` : fader + `RangeBar` + boutons de plages (repris de BIB-060/061), un widget par **canal** (pas encore par attribut composite).

## Tests

- Couvert indirectement par `ConsoleViewModelTests.DeviceMode_*` (fader par canal).

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 4. Exigences – mode appareils (P3)). |
| 2026-09-26 | Claude | Développement | `5d60230` feat(console): mode appareils (CONS-020 à 024), CONS-007, CONS-043, CONS-092 |
| 2026-09-26 | Claude | Écart | Les canaux R/G/B/W et Pan/Tilt (grossier + fin) restent affichés canal par canal, comme le test en direct de la bibliothèque (BIB-060) : pas de pastille couleur ni de pad XY combiné. Complément prévu avec les palettes / le programmeur (P4-P5), où la manipulation par attribut composite devient nécessaire pour les scènes. Laissé « Partiel » : le pilotage manuel reste possible canal par canal en attendant. |
