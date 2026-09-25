# CONS-043 – Délimitation des appareils et surcharges dans le moniteur

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | M |
| **Phase** | P1 |
| **Source** | [doc 11 – 5. Moniteur de sortie](../11-console.md) |
| **Remarque** | Canaux surchargés marqués ; délimitation par appareil en P3. |
| **Liens** | — |

## Description

> Les canaux appartenant à un même appareil sont délimités visuellement ; les canaux surchargés sont marqués.

**Critère d'acceptation** : Revue.

## Réalisation

- `src/Dmx.UI.Controls/OutputMonitor.cs`

## Tests

- Aucun test automatique : vérification par le guide de démonstration ou sur le matériel.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 5. Moniteur de sortie). |
| 2026-09-24 | Claude | Développement | `87237c9` feat(console): console en mode canaux, moniteur, instantanés, coquille de l'application |
