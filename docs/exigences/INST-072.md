# INST-072 – Calibration des positions

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | M |
| **Phase** | P5 |
| **Source** | [doc 13 – 6. Assistant d'installation sur site](../13-installation-et-lieux.md) |
| **Remarque** | Calibration faite sans écran dédié : sélectionner les deux lyres au programmeur (faders communs, « Coller en miroir »), viser, puis « Mettre à jour une palette avec le programmeur » → enregistré pour le lieu actif (PAL-004). L'écran de calibration de l'assistant est reporté (Q32). |
| **Liens** | PAL-004, INST-070, Q32 |

## Description

> Calibration des positions : pour chaque palette de position, pad Pan/Tilt par lyre, **les deux lyres pouvant être visées ensemble** (miroir optionnel).

**Critère d'acceptation** : Calibrer 4 positions en < 3 min.

## Réalisation

- `src/Luxia.UI.Modules.Scenes/PalettesViewModel.cs` (mise à jour dans le lieu actif)

## Tests

- —

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, 6. Assistant d'installation sur site). |
| 2026-09-27 | Claude | Décision | Q32 : pas d'écran de calibration ; le programmeur et la mise à jour de palette (par lieu depuis P5) suffisent. Le critère « 4 positions en moins de 3 min » sera mesuré à l'essai. |
