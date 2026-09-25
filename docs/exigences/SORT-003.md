# SORT-003 – Seule la trame la plus récente

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 3. Exigences – routage et supervision (côté PC)](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Si un pilote n'a pas fini d'émettre la trame précédente, seule **la plus récente** est conservée (pas de file d'attente qui grossit).

**Critère d'acceptation** : Test : pilote ralenti → latence bornée, pas de croissance mémoire.

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
