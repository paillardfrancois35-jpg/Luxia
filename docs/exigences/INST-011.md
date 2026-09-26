# INST-011 – Ajout multiple d'appareils identiques

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | — |
| **Liens** | INST-010 |

## Description

> Ajout **multiple** : N appareils identiques, adresses consécutives (ou avec écart réglable), noms numérotés automatiquement (« PAR 1 » … « PAR 4 »).

**Critère d'acceptation** : 4 × PAR 7CH depuis 1 → 1, 8, 15, 22.

## Réalisation

- `src/Dmx.Patch/Rules/PatchRules.cs` : `PlanMultiple` (adresses consécutives + écart, numérotation).
- `src/Dmx.UI.Modules.Installation/InstallationViewModel.cs` : champs « Quantité » et « Écart » du formulaire d'ajout.

## Tests

- `PatchRulesTests.PlanMultiple_FourParsSevenChannels_GivesReferenceShowAddresses`
- `PatchRulesTests.PlanMultiple_WithGap_LeavesReserve`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Développement | `5ee2e57` feat(patch): nouveau projet Dmx.Patch, modèle de domaine de l'installation |
