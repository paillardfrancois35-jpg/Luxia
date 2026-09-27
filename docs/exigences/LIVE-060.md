# LIVE-060 – L'écran Live se rafraîchit à ≥ 20 images/s sans affecter le moteur

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 6. Performances](../18-live.md) |
| **Remarque** | Rafraîchissement de l'écran à 20 images/s par la minuterie de la fenêtre ; un rafraîchissement du Live (tick du moteur compris) mesuré bien sous 10 ms en test ; le moteur tourne sur son propre fil (MOT-003). |
| **Liens** | MOT-003 |

## Description

> L'écran Live se rafraîchit à ≥ 20 images/s sans affecter le moteur.

**Critère d'acceptation** : Mesure.

## Réalisation

- `src/Luxia.App/MainWindow.axaml.cs` (minuterie 50 ms)

## Tests

- `LiveViewModelTests.Refresh_IsFarUnderTheFrameBudget`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 6. Performances). |
| 2026-09-26 | Claude | Développement | `dbb3b1e` feat(live): écran Live ; mesure ajoutée ensuite. |
