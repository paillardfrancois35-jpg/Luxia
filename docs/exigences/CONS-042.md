# CONS-042 – Origine de la valeur au survol du moniteur

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P4 |
| **Source** | [doc 11 – 5. Moniteur de sortie](../11-console.md) |
| **Remarque** | Au survol du moniteur : couche et scène, surcharge d'attribut, blackout ou défaut. Les limites de sûreté (P5) s'y ajouteront. |
| **Liens** | GEN-043 |

## Description

> Au survol : **origine** de la valeur (couche/scène, surcharge, défaut, limite de sûreté) (GEN-043).

**Critère d'acceptation** : Revue.

## Réalisation

- `src/Luxia.UI.Modules.Console/ConsoleViewModel.cs`

## Tests

- Aucun test automatique : vérification par le guide de démonstration P4 (`docs/demos/P4-moteur-scenes.md`).

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 5. Moniteur de sortie). |
| 2026-09-26 | Claude | Développement | `7049044` feat(console): surcharges d'attributs en mode appareils, écart conservé en relatif, origine au survol |
| 2026-09-26 | Claude | Test | Vérifié par revue ; démonstration dans le guide P4. |
