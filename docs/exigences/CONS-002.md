# CONS-002 – Modes de saisie des faders

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P1 |
| **Source** | [doc 11 – 3. Exigences – mode canaux (P1)](../11-console.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Réglage de chaque fader à la souris (glisser), à la molette (±1, Maj+molette ±10), au clavier (flèches ±1, Page ±10, Début/Fin = 255/0) et par **saisie directe** de la valeur.

**Critère d'acceptation** : Test de chaque mode de saisie.

## Réalisation

- `src/Luxia.UI.Controls/Fader.cs`
- `src/Luxia.UI.Modules.Console/ConsoleViewModel.cs`

## Tests

- `ConsoleViewModelTests.TypedValue_Valid_IsApplied_Invalid_IsRejected`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 3. Exigences – mode canaux (P1)). |
| 2026-09-24 | Claude | Décision | Glisser **relatif** (pas de saut au point cliqué) ; un clic simple prend le fader à sa valeur actuelle. Saisie directe dans la case sous le fader (ou double-clic). |
| 2026-09-24 | Claude | Développement | `87237c9` feat(console): console en mode canaux, moniteur, instantanés, coquille de l'application |
