# INST-053 – Zones interdites par lyre, définies en visant à la main

| Champ | Valeur |
|---|---|
| **Statut** | En cours |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 13 – 5.2 Exigences](../13-installation-et-lieux.md) |
| **Remarque** | Format (`lieux.json`, `forbiddenZones`) et application par le moteur faits ; saisie à l'écran en visant (programmeur) à venir. |
| **Liens** | GEN-085, MOT-082, Q30 |

## Description

> **Zones interdites** par lyre, définies en visant à la main (pad Pan/Tilt) les limites à ne pas franchir ; appliquées par le moteur (GEN-085).

**Critère d'acceptation** : Scène visant le public → bornée.

## Réalisation

- `src/Luxia.Patch/Model/ForbiddenZone.cs`, `Venue.ForbiddenZones`

## Tests

- `SafetyCompilerTests.Zones_OfActiveVenue_TargetThePanTiltParameters`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, 5.2 Exigences). |
| 2026-09-26 | Claude | Développement | `63cbe3b` feat(surete): limiteurs de strobe et de fumée, zones interdites Pan/Tilt (étape 9) |
