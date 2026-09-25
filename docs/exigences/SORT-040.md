# SORT-040 – Rafraîchissement DMX continu par le firmware

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 6. Exigences – firmware](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | Firmware compilé (DMXSerial), non téléversé. |
| **Liens** | Q19 |

## Description

> La ligne DMX est rafraîchie **en continu par le firmware** (bibliothèque DMXSerial en mode contrôleur), indépendamment du rythme d'arrivée des messages du PC.

**Critère d'acceptation** : Oscilloscope / renifleur : trames DMX continues même si le PC n'envoie qu'1 message/s.

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
