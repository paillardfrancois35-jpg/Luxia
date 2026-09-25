# SORT-045 – Message appliqué seulement s'il est complet

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 6. Exigences – firmware](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | Firmware : application à la réception de 0xE7 seulement. |
| **Liens** | Q19 |

## Description

> Les valeurs d'un message ne sont appliquées qu'après réception complète et valide du message (pas de trame à moitié mise à jour).

**Critère d'acceptation** : Test : message tronqué → ancienne trame conservée.

## Réalisation

- `firmware/arduino-dmx/arduino-dmx.ino`

## Tests

- `EnttecProtocolTests.Parser_WrongEndByte_IsRejected`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 6. Exigences – firmware). |
| 2026-09-24 | Claude | Question | Q19 : compilation du firmware. |
| 2026-09-24 | Utilisateur | Réponse | Utilisateur : arduino-cli ; compilation libre ; **chaque téléversement sur la carte lui est demandé au préalable**. |
| 2026-09-24 | Claude | Développement | `ebcebf0` firmware: interface Enttec (labels 3, 6, 10, 77, 0x11), chien de garde 2 s |
