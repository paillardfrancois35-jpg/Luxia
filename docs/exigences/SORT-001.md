# SORT-001 – Univers vers plusieurs pilotes

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 3. Exigences – routage et supervision (côté PC)](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Chaque univers est associé à **zéro, un ou plusieurs** pilotes. Une même trame peut partir simultanément vers l'Arduino et le simulateur.

**Critère d'acceptation** : Univers 1 → Arduino + Simulateur : les deux reçoivent des trames identiques.

## Réalisation

- `src/Luxia.Core/Settings/Preferences.cs`
- `src/Luxia.Output/OutputRouter.cs`

## Tests

- `OutputRouterTests.Submit_OneUniverseToTwoDrivers_BothReceiveIdenticalFrames`
- `OutputRouterTests.Submit_OtherUniverse_IsNotRouted`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 3. Exigences – routage et supervision (côté PC)). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
