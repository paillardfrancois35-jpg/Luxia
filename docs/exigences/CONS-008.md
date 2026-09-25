# CONS-008 – Surcharges soumises au blackout et à la sûreté

| Champ | Valeur |
|---|---|
| **Statut** | Reporté (P4-P5) |
| **Priorité** | M |
| **Phase** | P1 |
| **Source** | [doc 11 – 3. Exigences – mode canaux (P1)](../11-console.md) |
| **Remarque** | Blackout (P4) et limites de sûreté (P5) pas encore implémentés ; TODO dans RenderEngine. |
| **Liens** | GEN-042 |

## Description

> Les surcharges de la console restent soumises au blackout et aux limites de sûreté (GEN-042).

**Critère d'acceptation** : Fumée à 255 → coupée à la limite de durée.

## Réalisation

- —

## Tests

- Aucun test automatique : vérification par le guide de démonstration ou sur le matériel.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 3. Exigences – mode canaux (P1)). |
| 2026-09-24 | Claude | Écart | Reporté : le blackout (P4) et les limites de sûreté (P5) n'existent pas encore ; `TODO(P4, GEN-042)` dans `RenderEngine`. |
