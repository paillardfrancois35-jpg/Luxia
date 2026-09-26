# INST-012 – Première adresse libre proposée

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | — |
| **Liens** | INST-010, INST-013 |

## Description

> Proposition automatique de la **première adresse libre** suffisante pour le mode choisi.

**Critère d'acceptation** : —

## Réalisation

- `src/Dmx.Patch/Rules/PatchRules.cs` : `FindFreeAddress`.
- `src/Dmx.UI.Modules.Installation/InstallationViewModel.cs` : `SuggestAddressCommand` (bouton « Libre »).

## Tests

- `PatchRulesTests.FindFreeAddress_SkipsOccupiedRanges`
- `PatchRulesTests.FindFreeAddress_NoRoomLeft_ReturnsNull`
- `InstallationViewModelTests.SuggestAddress_ProposesFirstFreeAddress`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Développement | `5ee2e57` feat(patch): nouveau projet Dmx.Patch, modèle de domaine de l'installation |
