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

- `src/Luxia.Core/Dmx/IFrameSink.cs`
- `src/Luxia.Output/OutputDriver.cs`

## Tests

- `OutputRouterTests.SlowDriver_DoesNotDelayOthers_AndKeepsOnlyLatestFrame`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 3. Exigences – routage et supervision (côté PC)). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-28 | Claude | Anomalie | Même test instable que SORT-002 (fenêtre de temps réel sensible à la charge). |
| 2026-09-28 | Claude | Développement | `1c4add3` fix(tests): le pilote lent libéré n'écrit que la trame en cours puis la plus récente — exactement 2 trames (1 puis 40), vérifié au lieu d'un « moins de 15 ». |
| 2026-09-28 | Claude | Test | Série complète verte 6 fois de suite ; test seul vert 15 fois de suite. |
