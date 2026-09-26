# CONS-024 – Bouton Identifier par appareil

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P3 |
| **Source** | [doc 11 – 4. Exigences – mode appareils (P3)](../11-console.md) |
| **Remarque** | — |
| **Liens** | CMD-023, INST-019, SIM-009 |

## Description

> Bouton **Identifier** par appareil (CMD-023).

**Critère d'acceptation** : L'appareil clignote, les autres ne changent pas.

## Réalisation

- `src/Dmx.Patch/Rules/IdentifyRules.cs` : canaux et valeurs d'identification (`Identify` du canal, sinon canaux d'intensité au maximum ; jamais couleur ni position).
- `src/Dmx.UI.Modules.Console/FixtureFadersViewModel.cs` : `ToggleIdentifyCommand`, clignotement (400 ms) via des surcharges brutes classiques, libérées à l'arrêt.
- `src/Dmx.UI.Modules.Console/FixtureFadersView.axaml` : bouton « Identifier ».

## Tests

- `Dmx.Patch.Tests/IdentifyRulesTests.cs` (3 tests)
- `ConsoleViewModelTests.Identify_LightsIntensityChannel_WithoutTouchingColor_AndReleasesOnStop`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 4. Exigences – mode appareils (P3)). |
| 2026-09-26 | Claude | Décision | Implémenté par des surcharges de canaux (comme le reste de la console), pas par une nouvelle commande moteur : le clignotement est un aller-retour minuté côté interface (horloge réelle, comme la découverte BIB-062), pas un calcul déterministe du moteur. Voir CMD-023 pour la discussion complète. |
| 2026-09-26 | Claude | Développement | `5d60230` feat(console): mode appareils (CONS-020 à 024), CONS-007, CONS-043, CONS-092 |
