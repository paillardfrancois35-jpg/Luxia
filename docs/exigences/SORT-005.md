# SORT-005 – Moteur actif sans aucune sortie

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 3. Exigences – routage et supervision (côté PC)](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Le moteur continue de fonctionner quand aucun pilote n'est connecté.

**Critère d'acceptation** : Démarrage sans Arduino : moteur actif, simulateur fonctionnel.

## Réalisation

- —

## Tests

- `OutputRouterTests.Submit_WithoutAnyDriver_DoesNothing`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 3. Exigences – routage et supervision (côté PC)). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
