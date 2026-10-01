# INST-017 – Nom, couleur et numéro court

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | Couleur affectée automatiquement (palette fixe) ; pas encore d'éditeur de couleur dédié dans l'écran. |
| **Liens** | INST-018 |

## Description

> Chaque appareil a un nom, une couleur d'affichage, et un **numéro court** (1, 2, 3…) utilisé dans les listes et au simulateur.

**Critère d'acceptation** : —

## Réalisation

- `src/Luxia.Patch/Model/PatchedFixture.cs` : `Name`, `Number`, `Color`.
- `src/Luxia.Patch/Rules/PatchRules.cs` : `PlanMultiple` attribue le numéro à la création.
- Repris dans la barre d'univers (INST-003), le moniteur de sortie (CONS-043) et le simulateur (SIM-001).

## Tests

- `PatchRulesTests.PlanMultiple_FourParsSevenChannels_GivesReferenceShowAddresses` (numéros)

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Développement | `5ee2e57` feat(patch): nouveau projet Dmx.Patch, modèle de domaine de l'installation |
