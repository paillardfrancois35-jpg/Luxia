# INST-003 – Vue barre d'univers

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 2. Univers](../13-installation-et-lieux.md) |
| **Remarque** | — |
| **Liens** | CONS-040 |

## Description

> Vue **barre d'univers** : 512 cases, colorées par appareil, avec nom au survol ; canaux libres visibles.

**Critère d'acceptation** : Revue.

## Réalisation

- `src/Dmx.UI.Controls/UniverseBar.cs` : nouveau contrôle, 512 cases proportionnelles, coloré par appareil patché (couleur de l'appareil), chevauchement encadré en rouge, canaux libres en gris sombre, nom au survol (`ChannelHovered`).
- `src/Dmx.UI.Modules.Installation/InstallationViewModel.cs` : `UniverseBarSegments`, recalculés à chaque modification du patch pour l'univers affiché.

## Tests

- Aucun test automatique (rendu visuel) : vérification par le guide de démonstration.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §2). |
| 2026-09-26 | Claude | Développement | `389fe64` feat(installation): écran Installation — patch, sélections, lieux (doc 13) |
