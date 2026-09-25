# SORT-048 – Firmware versionné et documenté

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P0 |
| **Source** | [doc 10 – 6. Exigences – firmware](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | firmware/arduino-dmx + README (bibliothèques, cavaliers, commandes). |
| **Liens** | Q19, Q20 |

## Description

> Le firmware est **versionné dans le dépôt** (`firmware/arduino-dmx`) avec la liste des bibliothèques requises et la configuration des cavaliers du shield.

**Critère d'acceptation** : Revue.

## Réalisation

- —

## Tests

- Aucun test automatique : vérification par le guide de démonstration ou sur le matériel.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 6. Exigences – firmware). |
| 2026-09-24 | Claude | Question | Q19 : compilation du firmware. |
| 2026-09-24 | Utilisateur | Réponse | Utilisateur : arduino-cli ; compilation libre ; **chaque téléversement sur la carte lui est demandé au préalable**. |
| 2026-09-24 | Claude | Question | Q20 : sort du POC `Arduino/dmx_poc`. |
| 2026-09-24 | Utilisateur | Réponse | Utilisateur : POC versionné au premier commit puis remplacé par `firmware/arduino-dmx` (historique conservé). |
| 2026-09-24 | Claude | Développement | `ebcebf0` firmware: interface Enttec (labels 3, 6, 10, 77, 0x11), chien de garde 2 s |
| 2026-09-24 | Claude | Note | arduino-cli installé depuis downloads.arduino.cc (winget bloqué) ; DMXSerial 1.5.3, cœur arduino:avr 1.8.8. Compilation : 5822 octets (20 %), RAM 48 %. Non téléversé. |
