# INST-030 – Sélection manuelle ordonnée

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 4. Sélections](../13-installation-et-lieux.md) |
| **Remarque** | — |
| **Liens** | INST-032 |

## Description

> Une **sélection** est une liste **ordonnée** d'appareils (ou de cellules d'appareils), nommée et colorée. L'ordre sert aux chenillards et aux décalages de phase.

**Critère d'acceptation** : Sélection « PAR gauche → droite ».

## Réalisation

- `src/Dmx.Patch/Model/Selection.cs`, `SelectionItem.cs`.
- `src/Dmx.UI.Modules.Installation/InstallationViewModel.cs` : `CreateSelectionCommand`.

## Tests

- `InstallationViewModelTests.CreateSelection_FromCheckedFixtures_ThenReverse`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §4). |
| 2026-09-26 | Claude | Développement | `5ee2e57` feat(patch): nouveau projet Dmx.Patch, modèle de domaine de l'installation ; `389fe64` (écran) |
