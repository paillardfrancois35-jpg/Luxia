# SORT-043 – Messages du protocole pris en charge

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 6. Exigences – firmware](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | Protocole testé côté PC ; firmware à téléverser. |
| **Liens** | Q19 |

## Description

> Prise en charge des messages du §5.3 (labels 6, 10, 77 ; 3 en M ; 0x11 en S).

**Critère d'acceptation** : Tests de protocole (cf. §8).

## Réalisation

- —

## Tests

- `EnttecProtocolTests.EncodeSendDmx_ProducesLabel6WithStartCode`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 6. Exigences – firmware). |
| 2026-09-24 | Claude | Question | Q19 : compilation du firmware. |
| 2026-09-24 | Utilisateur | Réponse | Utilisateur : arduino-cli ; compilation libre ; **chaque téléversement sur la carte lui est demandé au préalable**. |
| 2026-09-24 | Claude | Développement | `ebcebf0` firmware: interface Enttec (labels 3, 6, 10, 77, 0x11), chien de garde 2 s |
