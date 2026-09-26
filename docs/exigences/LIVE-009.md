# LIVE-009 – Journal défilant des derniers événements

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P5 |
| **Source** | [doc 18 – 3. Exigences – structure](../18-live.md) |
| **Remarque** | Journal des événements (scènes lancées / arrêtées avec l'origine, limites de sûreté, sortie, commandes refusées), le plus récent en haut, 60 lignes. |
| **Liens** | GEN-112 |

## Description

> **Journal** défilant des derniers événements (morceaux, décisions, limites, erreurs).

**Critère d'acceptation** : —

## Réalisation

- `src/Luxia.UI.Modules.Live/LiveViewModel.cs` (abonnements au bus, `DrainJournal`)

## Tests

- `LiveViewModelTests.Journal_ShowsSceneStarts`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 3. Exigences – structure). |
| 2026-09-26 | Claude | Développement | `dbb3b1e` feat(live): écran Live |
