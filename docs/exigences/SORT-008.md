# SORT-008 – Canaux maintenus pendant le test de sortie

| Champ | Valeur |
|---|---|
| **Statut** | À faire |
| **Priorité** | M |
| **Phase** | P3 |
| **Source** | [doc 10 – 3. Exigences – routage et supervision (côté PC)](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | Extension de CMD-024 (liste de canaux maintenus) et de l'écran Sorties ; préférences `testOutput`. |
| **Liens** | CMD-024, D19, D21, Q23, SORT-007 |

## Description

> Test de sortie : liste de **canaux maintenus** à la valeur de test pendant tout le chenillard (ex. `1, 8, 15, 22` = gradateurs maîtres des 4 PAR), pour voir réagir les appareils à gradateur maître ; jamais un canal exclu.

**Critère d'acceptation** : PAR en 7 canaux, canal 1 maintenu : le chenillard sur 2, 3, 4 donne rouge, vert, bleu.

## Réalisation

- —

## Tests

- Aucun test automatique : à écrire en P3.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Claude | Question | Q23 : un chenillard canal par canal n'allume pas un appareil à gradateur maître (PAR en 7 canaux : le maître est à 0 quand la couleur passe). Proposition : liste de canaux maintenus. |
| 2026-09-25 | Utilisateur | Question | Demande de précision sur Q23 ; question reformulée avec l'exemple du PAR en 7 canaux. |
| 2026-09-25 | Utilisateur | Réponse | Oui, en P3. |
| 2026-09-25 | Conception | Création | Exigence SORT-008 ajoutée au doc 10 §3 (M, P3) ; P3 du doc 40 complétée. |
| 2026-09-25 | Claude | Décision | Un canal à la fois exclu **et** maintenu reste exclu (la sûreté prime : fumée, Reset). |
