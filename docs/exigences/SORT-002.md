# SORT-002 – Pilotes indépendants

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 3. Exigences – routage et supervision (côté PC)](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Chaque pilote tourne indépendamment : un pilote lent ou en erreur ne retarde ni le moteur ni les autres pilotes.

**Critère d'acceptation** : Test : pilote volontairement bloqué → autres pilotes à 40 trames/s.

## Réalisation

- `src/Dmx.Core/Dmx/IFrameSink.cs`
- `src/Dmx.Output/OutputDriver.cs`

## Tests

- `OutputRouterTests.SlowDriver_DoesNotDelayOthers_AndKeepsOnlyLatestFrame`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 3. Exigences – routage et supervision (côté PC)). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
