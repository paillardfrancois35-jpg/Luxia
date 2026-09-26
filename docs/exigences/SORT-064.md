# SORT-064 – Pilote Art-Net

| Champ | Valeur |
|---|---|
| **Statut** | Non réalisé |
| **Priorité** | S |
| **Phase** | P3 |
| **Source** | [doc 10 – 7. Pilotes Simulateur, Enregistreur, Art-Net](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | GEN-122 |

## Description

> **Art-Net** : émet des paquets ArtDmx (UDP 6454) en diffusion ou vers une adresse donnée ; numéro d'univers Art-Net réglable.

**Critère d'acceptation** : Réception dans un visualiseur Art-Net tiers.

## Réalisation

- —

## Tests

- —

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, §7). |
| 2026-09-26 | Claude | Décision | Priorité S, non fait en P3 : aucun besoin concret pour le parc actuel (Arduino + Simulateur suffisent) ; le principe qui l'autorise (GEN-122) est en revanche déjà respecté par construction. À faire si un visualiseur tiers (ex. QLC+ en visualiseur, ou un second poste) devient utile. |
