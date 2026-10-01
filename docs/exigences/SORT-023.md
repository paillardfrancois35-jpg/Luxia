# SORT-023 – Mesures du pilote (durée, trames/s)

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P0 |
| **Source** | [doc 10 – 4.2 Émission](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | Durée d'écriture et trames/s affichées dans l'écran Sorties. |
| **Liens** | — |

## Description

> Le pilote mesure et publie la durée d'écriture et les trames réellement émises par seconde.

**Critère d'acceptation** : Affichage.

## Réalisation

- `src/Luxia.Output/OutputDriverStatus.cs`

## Tests

- Aucun test automatique : vérification par le guide de démonstration ou sur le matériel.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 4.2 Émission). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
