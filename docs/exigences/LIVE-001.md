# LIVE-001 – Bandeau d'état permanent

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 3. Exigences – structure](../18-live.md) |
| **Remarque** | Bandeau : sortie, figé, fumée en cours, lieu actif, limites de sûreté, positions non calibrées ; le blackout et le Grand Master sont dans l'en-tête permanent de la fenêtre. Morceau, style, BPM, énergie et mode auto viendront avec P7, P9 et P10 (non affichés tant qu'ils n'existent pas, pour ne pas encombrer). |
| **Liens** | LIVE-008, LIVE-010, Q32 |

## Description

> **Bandeau d'état** permanent : état de la sortie, blackout, figé, mode auto, et (dès disponibles) morceau, style, BPM + battement, énergie.

**Critère d'acceptation** : Revue.

## Réalisation

- `src/Luxia.UI.Modules.Live/LiveViewModel.cs`, `LiveView.axaml` (bandeau)

## Tests

- `LiveViewModelTests.StatusBand_AndCommandJournal`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 3. Exigences – structure). |
| 2026-09-26 | Claude | Décision | Pas d'indicateur « vide » pour ce qui n'existe pas encore (tempo, style, auto) : chaque élément du bandeau apparaît avec sa phase (remarque d'ergonomie de fin de P4, Q32). Blackout et Grand Master ne sont pas dupliqués : l'en-tête de la fenêtre les montre déjà sur tous les écrans. |
| 2026-09-26 | Claude | Développement | `dbb3b1e` feat(live): écran Live |
