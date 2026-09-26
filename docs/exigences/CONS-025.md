# CONS-025 – Capturer les surcharges dans une scène

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P4 |
| **Source** | [doc 11 – 4. Exigences – mode appareils (P3)](../11-console.md) |
| **Remarque** | Fait depuis le programmeur de l'écran Scènes : « Capturer la sortie » reprend ce qui est émis (surcharges de la console comprises) pour les appareils choisis, puis on l'enregistre dans une étape. |
| **Liens** | SCN-037 |

## Description

> **Capturer** : créer une scène (ou une étape d'une scène existante) à partir des surcharges en cours (seuls les attributs surchargés sont enregistrés).

**Critère d'acceptation** : Capture → scène qui reproduit l'état.

## Réalisation

- `src/Luxia.UI.Modules.Scenes/ProgrammerViewModel.cs`

## Tests

- Aucun test automatique : vérification par le guide de démonstration P4 (`docs/demos/P4-moteur-scenes.md`).

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 4. Exigences – mode appareils (P3)). |
| 2026-09-26 | Claude | Écart | Pas de bouton « Capturer » dans l'écran Console lui-même : la capture passe par le programmeur (même résultat, avec choix des appareils). Reporté au doc 11 §7. |
| 2026-09-26 | Claude | Développement | `f5865f4` feat(scenes-ui): écran Scènes (programmeur, étapes, palettes), blackout et Grand Master, aperçu en aveugle |
| 2026-09-26 | Claude | Test | Vérifié par revue ; démonstration dans le guide P4. |
