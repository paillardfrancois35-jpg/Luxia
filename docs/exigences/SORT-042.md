# SORT-042 – Chien de garde du firmware (2 s)

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 6. Exigences – firmware](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | Firmware : chien de garde 2 s. |
| **Liens** | Q19 |

## Description

> **Chien de garde** : sans message DMX valide pendant **2 s**, tous les canaux passent à 0 (GEN-080).

**Critère d'acceptation** : Débrancher l'USB → noir en ≤ 2 s.

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
| 2026-09-25 | Utilisateur | Test | Débranchement de l'USB (perte d'alimentation de la carte, pas le chien de garde) : PAR éteint en ~3,5 s ; le même essai en débranchant seulement le câble DMX au PAR (Arduino toujours alimenté) donne le même délai 3-4 s. **Confirmé : ce délai vient du PAR lui-même** (comportement propre à sa perte de signal), pas du firmware. |
