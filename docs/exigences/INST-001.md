# INST-001 – Un ou plusieurs univers, numérotés et nommables

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 2. Univers](../13-installation-et-lieux.md) |
| **Remarque** | — |
| **Liens** | INST-002, INST-003 |

## Description

> L'installation comporte **un ou plusieurs univers** (numérotés, nommables). Le parc actuel n'en utilise qu'un.

**Critère d'acceptation** : Créer 2 univers.

## Réalisation

- `src/Luxia.Patch/Model/PatchUniverse.cs`, `Installation.cs`.
- `src/Luxia.Patch/InstallationStore.cs` : `installation.json`.
- `src/Luxia.UI.Modules.Installation/InstallationViewModel.cs` : un univers créé automatiquement au patch d'un appareil dans un univers inconnu.

## Tests

- `StoresTests.InstallationStore_SaveThenLoad_RoundTrips` (2 univers)
- `StoresTests.InstallationStore_Missing_ReturnsDefaultWithOneUniverse`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §2). |
| 2026-09-26 | Claude | Développement | `5ee2e57` feat(patch): nouveau projet Dmx.Patch, modèle de domaine de l'installation |
| 2026-09-26 | Claude | Développement | `389fe64` feat(installation): écran Installation — patch, sélections, lieux (doc 13) |
