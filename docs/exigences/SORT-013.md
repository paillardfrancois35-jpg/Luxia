# SORT-013 – Reconnexion automatique

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 4.1 Détection et connexion](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | T-SORT-05 (10 débranchements). |
| **Liens** | — |

## Description

> **Reconnexion automatique** : en cas de perte (débranchement, erreur d'écriture), tentative toutes les 1 s, sans action de l'utilisateur ; reprise en < 3 s après rebranchement (GEN-091).

**Critère d'acceptation** : Test débranchement / rebranchement.

## Réalisation

- `src/Dmx.Output/OutputDriver.cs`

## Tests

- `ArduinoOutputDriverTests.Unplug_ThenReplug_ReconnectsWithinThreeSeconds`
- `OutputRouterTests.WriteError_GoesToErrorThenReconnects`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 4.1 Détection et connexion). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
