# LIVE-003 – Un clic sur une scène la lance

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 3. Exigences – structure](../18-live.md) |
| **Remarque** | Clic = lancer ; clic sur la scène active = arrêter (ou relancer : `activeSceneClick` de live.json) ; appui maintenu sur une scène d'une couche Flash = flash, relâche = retour (la perte du pointeur relâche aussi). |
| **Liens** | CMD-010, CMD-011, CMD-014, COU-005 |

## Description

> Un clic sur une scène la lance (CMD-010) ; un clic sur la scène active l'arrête (réglable : arrête / relance). Clic maintenu sur une scène de couche Flash = flash.

**Critère d'acceptation** : Test.

## Réalisation

- `src/Luxia.UI.Modules.Live/LiveViewModel.cs` (`Press`, `Release`)
- `src/Luxia.UI.Modules.Live/LiveView.axaml.cs`

## Tests

- `LiveViewModelTests.ClickScene_Launches_ClickAgain_Stops`
- `LiveViewModelTests.FlashLayerScene_PlaysOnlyWhileHeld`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 3. Exigences – structure). |
| 2026-09-26 | Claude | Développement | `dbb3b1e` feat(live): écran Live |
| 2026-09-27 | Utilisateur | Test | Essai P5, exemple 1 : clic sur une scène = lancement — conforme ; arrêt par un 2e clic et flash maintenu restent à essayer. |
