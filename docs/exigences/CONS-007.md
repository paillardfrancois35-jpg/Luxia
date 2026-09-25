# CONS-007 – Appareil, attribut et plage sur le fader

| Champ | Valeur |
|---|---|
| **Statut** | Reporté (P3) |
| **Priorité** | M |
| **Phase** | P1 |
| **Source** | [doc 11 – 3. Exigences – mode canaux (P1)](../11-console.md) |
| **Remarque** | Nom d'appareil / attribut / plage : nécessite le patch. |
| **Liens** | — |

## Description

> Si le canal appartient à un appareil patché, affichage du nom de l'appareil, de l'attribut, et du **nom de la plage** courante (ex. « Strobe : lent→rapide, 42 % »).

**Critère d'acceptation** : Test avec un appareil patché (dès P3).

## Réalisation

- `src/Dmx.UI.Modules.Console/ChannelViewModel.cs`

## Tests

- Aucun test automatique : vérification par le guide de démonstration ou sur le matériel.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 3. Exigences – mode canaux (P1)). |
