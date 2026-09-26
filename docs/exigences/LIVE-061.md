# LIVE-061 – Latence clic → sortie < 50 ms

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 6. Performances](../18-live.md) |
| **Remarque** | Un clic envoie la commande tout de suite ; le moteur l'applique au tick suivant (≤ 25 ms à 40 Hz, GEN-010) : latence de calcul < 50 ms par construction. Mesure de bout en bout (jusqu'à la lampe) à faire sur le matériel. |
| **Liens** | GEN-090, GEN-010 |

## Description

> Latence clic → sortie < 50 ms (GEN-090).

**Critère d'acceptation** : Mesure.

## Réalisation

- —

## Tests

- —

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 6. Performances). |
| 2026-09-26 | Claude | Note | Pas d'étape intermédiaire entre le clic et le moteur (commande directe, pas d'attente du rafraîchissement de l'écran). |
