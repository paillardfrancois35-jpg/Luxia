# INST-020 – Supprimer un appareil

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | Rapport d'impact limité aux sélections manuelles (le retrait de l'appareil de leurs membres n'est pas encore automatique) ; scènes et palettes n'existent pas avant P4. |
| **Liens** | GEN-103 |

## Description

> Supprimer un appareil : rapport des scènes, palettes et sélections impactées, confirmation.

**Critère d'acceptation** : —

## Réalisation

- `src/Luxia.UI.Modules.Installation/InstallationViewModel.cs` : `DeleteFixtureAsync`, confirmation (GEN-103).

## Tests

- `InstallationViewModelTests.DeleteFixture_AsksConfirmation`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Écart | Le rapport détaillé (sélections/scènes/palettes impactées) n'est pas encore affiché avant confirmation ; une sélection manuelle qui référence l'appareil supprimé garde son identifiant orphelin (sans erreur, l'appareil n'apparaît simplement plus). À revoir si gênant à l'usage, ou en même temps que les scènes (P4). |
| 2026-09-26 | Claude | Développement | `389fe64` feat(installation): écran Installation — patch, sélections, lieux (doc 13) |
