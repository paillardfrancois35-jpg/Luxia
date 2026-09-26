# INST-014 – Doublon volontaire (jumeaux)

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | — |
| **Liens** | INST-013 |

## Description

> **Doublon volontaire** : deux appareils peuvent partager la même adresse s'ils sont déclarés « jumeaux » (même modèle et mode) ; ils reçoivent alors les mêmes valeurs (utile si un appareil est chaîné en esclave).

**Critère d'acceptation** : Deux PAR jumeaux → pas d'erreur, pilotés ensemble.

## Réalisation

- `src/Dmx.Patch/Model/PatchedFixture.cs` : `TwinGroupId`.
- `src/Dmx.Patch/Rules/PatchRules.cs` : `AreTwins` (même groupe, même modèle, même mode).
- `src/Dmx.UI.Modules.Installation/InstallationViewModel.cs` : case « Jumeaux (même adresse) » à l'ajout multiple.

## Tests

- `PatchRulesTests.DetectOverlaps_Twins_SameAddress_NoOverlap`
- `PatchRulesTests.DetectOverlaps_SameGroupButDifferentMode_StillOverlaps`
- `InstallationViewModelTests.Twins_SameAddress_AreNotFlaggedAsOverlap`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Développement | `5ee2e57` feat(patch): nouveau projet Dmx.Patch, modèle de domaine de l'installation ; `389fe64` (écran) |
