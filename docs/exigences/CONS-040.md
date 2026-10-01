# CONS-040 – Moniteur de sortie 512 cases

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P1 |
| **Source** | [doc 11 – 5. Moniteur de sortie](../11-console.md) |
| **Remarque** | OutputMonitor, 512 cases, valeurs affichables. |
| **Liens** | — |

## Description

> Grille de 512 cases par univers, la couleur/luminosité de chaque case représentant la valeur émise ; valeurs numériques affichables.

**Critère d'acceptation** : Revue.

## Réalisation

- `src/Luxia.UI.Controls/OutputMonitor.cs`
- `src/Luxia.UI.Modules.Console/ConsoleView.axaml`

## Tests

- Aucun test automatique : vérification par le guide de démonstration ou sur le matériel.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 5. Moniteur de sortie). |
| 2026-09-24 | Claude | Développement | `87237c9` feat(console): console en mode canaux, moniteur, instantanés, coquille de l'application |
