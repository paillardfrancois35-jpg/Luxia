# INST-002 – Lien univers → pilotes dans les préférences

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 2. Univers](../13-installation-et-lieux.md) |
| **Remarque** | Déjà vrai depuis P0 (SORT-006) : rien à faire de plus en P3. |
| **Liens** | SORT-006 |

## Description

> Le lien univers → pilotes de sortie relève des préférences du poste (SORT-006) ; l'installation ne contient que les univers.

**Critère d'acceptation** : —

## Réalisation

- `src/Luxia.Core/Settings/Preferences.cs` : `OutputPreferences.Assignments` (SORT-006, réalisé en P0).
- `src/Luxia.Patch/Model/Installation.cs` : ne contient que le numéro et le nom des univers, aucune référence à un pilote.

## Tests

- Couvert par `PreferencesAndProjectTests` (P0) et `StoresTests.InstallationStore_*` (aucun champ de pilote dans `Installation`).

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §2). |
| 2026-09-26 | Claude | Décision | Rien à développer spécifiquement : la séparation existe depuis SORT-006 (P0) et `Installation` (P3, `Dmx.Patch`) n'a jamais porté de référence à un pilote. |
