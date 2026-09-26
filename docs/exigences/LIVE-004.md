# LIVE-004 – Actions permanentes toujours visibles

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 3. Exigences – structure](../18-live.md) |
| **Remarque** | FLASH, STROBE et FUMÉE à maintenir, rafale, FIGER, tout arrêter ; blackout et Grand Master dans l'en-tête. Tap / ×2 / ÷2 (P7) et Auto (P10) viendront avec leur phase. |
| **Liens** | MOT-072, MOT-073, CMD-030, LIVE-040 |

## Description

> **Actions permanentes** toujours visibles : Blackout, Flash, Strobe (maintien), Fumée (maintien + rafale), Tap / ×2 / ÷2, Figer, Auto, Grand Master.

**Critère d'acceptation** : Revue.

## Réalisation

- `src/Luxia.UI.Modules.Live/LiveViewModel.cs` (`Flash`, `Strobe`, `Smoke`, `SmokeBurst`, `ToggleFreeze`, `StopAll`)

## Tests

- `LiveViewModelTests.Keys_FlashHeldWithAutoRepeat_ThenReleased`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 3. Exigences – structure). |
| 2026-09-26 | Claude | Décision | Les boutons FLASH et STROBE jouent en flash une scène : celle de `live.json` (`flashSceneId`, `strobeSceneId`), sinon « Flash blanc » et la première scène « Strobe… » d'une couche Flash. Ainsi une IA de conception décide de leur contenu dans les données, sans code. Bouton grisé si aucune scène. |
| 2026-09-26 | Claude | Développement | `dbb3b1e` feat(live): écran Live |
