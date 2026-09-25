# SORT-047 – Version unique du firmware

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P0 |
| **Source** | [doc 10 – 6. Exigences – firmware](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | Constante FW_VERSION (1.0) renvoyée par les labels 3 et 77. |
| **Liens** | Q19 |

## Description

> La version du firmware est une constante unique, renvoyée par les messages 3 et 77.

**Critère d'acceptation** : Revue.

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
| 2026-09-24 | Claude | Développement | `ebcebf0` firmware: interface Enttec (labels 3, 6, 10, 77, 0x11), chien de garde 2 s |
