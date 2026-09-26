# LIVE-008 – Indication visible de toute limite de sûreté active et de tout verrou

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 3. Exigences – structure](../18-live.md) |
| **Remarque** | Pastille « Sûreté » du bandeau : aucune limite, ou les limites en train d'agir (appareil et détail) et « strobe interdit » ; orange quand une limite agit. |
| **Liens** | GEN-086, MOT-083 |

## Description

> Indication visible de toute **limite de sûreté** active et de tout **verrou** (GEN-086).

**Critère d'acceptation** : —

## Réalisation

- `src/Luxia.UI.Modules.Live/LiveViewModel.cs` (`RefreshLimits`)
- `src/Luxia.Engine/EngineSnapshot.cs` (`ActiveLimits`)

## Tests

- `LiveViewModelTests.StatusBand_AndCommandJournal`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 3. Exigences – structure). |
| 2026-09-26 | Claude | Développement | `dbb3b1e` feat(live): écran Live |
