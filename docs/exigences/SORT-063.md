# SORT-063 – Lecteur d'enregistrements

| Champ | Valeur |
|---|---|
| **Statut** | Non réalisé |
| **Priorité** | S |
| **Phase** | P3 |
| **Source** | [doc 10 – 7. Pilotes Simulateur, Enregistreur, Art-Net](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | `dmx-headless relire` existe déjà pour relire un `.dmxrec` hors interface ; il manque le lien vers le simulateur comme source (« Lecture », SIM-006). |
| **Liens** | SORT-060, SIM-006 |

## Description

> **Lecteur** : rejoue un fichier de l'Enregistreur à vitesse réelle (ou accélérée) vers le simulateur.

**Critère d'acceptation** : Rejouer une soirée enregistrée.

## Réalisation

- —

## Tests

- —

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, §7). |
| 2026-09-26 | Claude | Décision | Priorité S, non fait en P3 : la lecture d'un fichier `.dmxrec` existe déjà en ligne de commande (`dmx-headless relire`) pour l'analyse ; relier ce mécanisme au simulateur comme troisième source (« Lecture », SIM-006) est une amélioration ergonomique reportée tant qu'aucun besoin concret ne la réclame. |
