# CONS-022 – Surcharge d'attribut soumise à la chaîne de rendu

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 11 – 4. Exigences – mode appareils (P3)](../11-console.md) |
| **Remarque** | En mode appareils, surcharge de l'attribut (CMD-021) : Grand Master et blackout s'appliquent (sûreté en P5). Fader d'intensité virtuelle pour un appareil sans gradateur. |
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
| 2026-09-26 | Claude | Décision | Reprise de la décision du 2026-09-26 (P3) maintenant que la chaîne existe : un réglage en mode appareils devient une surcharge d'**attribut** (étape 5). Un appareil sans gradateur (RVB 3 canaux) reçoit un fader « Intensité (virtuelle) » (D27 : 0 par défaut) ; régler une couleur alors que cette intensité n'est pas prise l'allume à 100 %, pour garder le comportement de P3. |
| 2026-09-26 | Claude | Développement | `7049044` feat(console): surcharges d'attributs en mode appareils, écart conservé en relatif, origine au survol |
| 2026-09-26 | Claude | Test | Critère vérifié : Grand Master 50 % → rouge surchargé 200 émis à 100 (`DeviceMode_BuildsOneGroupPerPatchedFixture_AndFaderOverridesTheAttribute`). |
