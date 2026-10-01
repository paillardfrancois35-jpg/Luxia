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

- `src/Luxia.Core/Dmx/IFrameSink.cs`
- `src/Luxia.Output/OutputDriver.cs`

## Tests

- `OutputRouterTests.SlowDriver_DoesNotDelayOthers_AndKeepsOnlyLatestFrame`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 3. Exigences – routage et supervision (côté PC)). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-28 | Claude | Anomalie | Test instable dans la série complète : il comptait des trames sur une fenêtre de temps réel (écriture lente de 200 ms, 25 ms entre trames) que la charge étire (docs/03 §11). |
| 2026-09-28 | Claude | Développement | `1c4add3` fix(tests): pilote lent bloqué sur une barrière (critère d'acceptation « pilote volontairement bloqué ») ; le rapide doit recevoir les 40 trames une à une, dépôts sans attente. |
| 2026-09-28 | Claude | Test | Série complète verte 6 fois de suite ; test seul vert 15 fois de suite. |
