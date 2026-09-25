# SORT-022 – Erreur d'écriture sans effet sur le moteur

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 4.2 Émission](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Une erreur d'écriture ne lève jamais d'erreur vers le moteur : elle bascule le pilote en `Erreur` puis en reconnexion.

**Critère d'acceptation** : Test d'arrachement du câble pendant l'émission.

## Réalisation

- `src/Dmx.Output/OutputDriver.cs`

## Tests

- `ArduinoOutputDriverTests.Unplug_ThenReplug_ReconnectsWithinThreeSeconds`
- `OutputRouterTests.WriteError_GoesToErrorThenReconnects`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 4.2 Émission). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
