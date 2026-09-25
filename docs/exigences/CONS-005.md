# CONS-005 – Le fader affiche la valeur émise

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P1 |
| **Source** | [doc 11 – 3. Exigences – mode canaux (P1)](../11-console.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Le fader affiche en permanence la **valeur réellement émise**, qu'il soit pris ou non (il suit les scènes quand il est libre).

**Critère d'acceptation** : Scène en cours → faders libres animés.

## Réalisation

- `src/Dmx.UI.Controls/Fader.cs`
- `src/Dmx.UI.Modules.Console/ChannelViewModel.cs`

## Tests

- `ConsoleViewModelTests.FaderRequest_OverridesChannel_AndFaderShowsEmittedValue`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 11, 3. Exigences – mode canaux (P1)). |
| 2026-09-24 | Claude | Décision | Après une action, la valeur demandée reste affichée 150 ms, le temps que le moteur l'applique (sinon retour visible à l'ancienne valeur pendant un tick). |
| 2026-09-24 | Claude | Développement | `87237c9` feat(console): console en mode canaux, moniteur, instantanés, coquille de l'application |
