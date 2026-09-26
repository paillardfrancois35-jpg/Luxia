# LIVE-010 – Alerte non bloquante et visible si la sortie est déconnectée ou si un module est en erreur

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 3. Exigences – structure](../18-live.md) |
| **Remarque** | Pastille de sortie rouge « SORTIE DÉCONNECTÉE… reconnexion automatique » ; retour au vert et ligne au journal à la reconnexion. À essayer en débranchant l'Arduino. |
| **Liens** | GEN-093, SORT-004 |

## Description

> Alerte non bloquante et visible si la sortie est déconnectée ou si un module est en erreur (GEN-093).

**Critère d'acceptation** : Débrancher l'Arduino → alerte rouge, reconnexion affichée.

## Réalisation

- `src/Luxia.UI.Modules.Live/LiveViewModel.cs` (`RefreshOutput`)

## Tests

- `LiveViewModelTests.StatusBand_AndCommandJournal`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 3. Exigences – structure). |
| 2026-09-26 | Claude | Développement | `dbb3b1e` feat(live): écran Live |
