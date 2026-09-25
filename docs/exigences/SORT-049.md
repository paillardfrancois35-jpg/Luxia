# SORT-049 – Trame DMX ajustée au nombre de canaux reçus

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | S |
| **Phase** | P0 |
| **Source** | [doc 10 – 6. Exigences – firmware](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | maxChannel = canaux reçus (24 minimum). |
| **Liens** | Q19 |

## Description

> Le nombre de canaux émis sur la ligne est ajusté à la longueur du dernier message reçu (fréquence DMX plus élevée pour les petits parcs).

**Critère d'acceptation** : Mesure : 120 canaux → > 100 trames DMX/s.

## Réalisation

- `firmware/arduino-dmx/arduino-dmx.ino`

## Tests

- Aucun test automatique : vérification par le guide de démonstration ou sur le matériel.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 6. Exigences – firmware). |
| 2026-09-24 | Claude | Question | Q19 : compilation du firmware. |
| 2026-09-24 | Utilisateur | Réponse | Utilisateur : arduino-cli ; compilation libre ; **chaque téléversement sur la carte lui est demandé au préalable**. |
| 2026-09-24 | Claude | Décision | Le firmware émet au moins **24 canaux** par trame DMX même si le message en contient moins (trames très courtes mal supportées par certains appareils). |
| 2026-09-24 | Claude | Développement | `ebcebf0` firmware: interface Enttec (labels 3, 6, 10, 77, 0x11), chien de garde 2 s |
