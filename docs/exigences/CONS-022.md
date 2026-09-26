# CONS-022 – Surcharge d'attribut soumise à la chaîne de rendu

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 11 – 4. Exigences – mode appareils (P3)](../11-console.md) |
| **Remarque** | Le moteur ne calcule pas encore les étapes 1 à 9 de la chaîne (couches, Grand Master, blackout, sûreté — P4/P5) : la surcharge passe directement en canal brut, comme en mode canaux. `TODO(P4, GEN-042)` déjà noté dans `RenderEngine`. |
| **Liens** | CONS-008, GEN-040 |

## Description

> En mode appareils, une modification surcharge l'**attribut** (étape 5 de la chaîne de rendu) : elle passe donc par le Grand Master, le blackout, les masters et les limites de sûreté.

**Critère d'acceptation** : Grand Master à 50 % → intensité surchargée divisée par 2.

## Réalisation

- `src/Dmx.UI.Modules.Console/FixtureFadersViewModel.cs` : `SetValue` envoie une surcharge de canal brut (`CMD-020`), comme CONS-008 pour le mode canaux.

## Tests

- Aucun test possible avant l'existence du Grand Master / blackout (P4).

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 4. Exigences – mode appareils (P3)). |
| 2026-09-26 | Claude | Décision | Même limite que CONS-008 (mode canaux) : les étapes 1 à 9 de la chaîne de rendu (doc 02 §9) n'existent pas avant P4. La surcharge d'attribut est donc, en pratique, une surcharge de canal comme en mode canaux. Rien à faire de plus en P3 ; à revoir avec le moteur complet (P4). |
| 2026-09-26 | Claude | Développement | `5d60230` feat(console): mode appareils (CONS-020 à 024), CONS-007, CONS-043, CONS-092 |
