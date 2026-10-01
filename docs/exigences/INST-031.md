# INST-031 – Sélections automatiques

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 4. Sélections](../13-installation-et-lieux.md) |
| **Remarque** | — |
| **Liens** | D24 |

## Description

> Sélections **automatiques** : tous les appareils ; par modèle ; par catégorie (tous les PAR, toutes les lyres…). Elles se mettent à jour seules.

**Critère d'acceptation** : Ajouter un PAR → présent dans « Tous les PAR ».

## Réalisation

- `src/Luxia.Patch/Rules/AutoSelections.cs` : `Build` (jamais persistées, D24).
- `src/Luxia.UI.Modules.Installation/InstallationViewModel.cs` : `AutoSelections`, recalculées à chaque `LoadAll`.

## Tests

- `AutoSelectionsTests` (3 tests)
- `InstallationViewModelTests.AutoSelections_IncludeAllAndByCategory`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §4). |
| 2026-09-26 | Claude | Décision | D24 : jamais enregistrées, toujours recalculées. |
| 2026-09-26 | Claude | Développement | `5ee2e57` feat(patch): nouveau projet Dmx.Patch, modèle de domaine de l'installation ; `389fe64` (écran) |
| 2026-09-27 | Claude | Développement | Titre des sélections automatiques accordé au féminin : « Toutes les lyres », « Toutes les barres LED », « Toutes les machines à fumée » (vu à la revue du Live). |
