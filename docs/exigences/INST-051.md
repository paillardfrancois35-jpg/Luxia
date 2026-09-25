# INST-051 – Éditeur de plan

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 5.2 Exigences (lieux)](../13-installation-et-lieux.md) |
| **Remarque** | Position, hauteur, orientation et montage réglables par champs numériques ; pas de glisser-déposer ni d'alignement/répartition automatique. |
| **Liens** | INST-050, SIM-001 |

## Description

> Éditeur de plan : placer les appareils par glisser-déposer, les orienter, régler leur hauteur et leur montage ; aligner / répartir.

**Critère d'acceptation** : Placer tout le parc en < 5 min.

## Réalisation

- `src/Dmx.Patch/Model/FixturePlacement.cs`.
- `src/Dmx.UI.Modules.Installation/PlacementRowViewModel.cs`, `InstallationViewModel.cs` (`SavePlacementsCommand`) : un appareil par ligne, champs X, Y, hauteur, orientation, suspendu.

## Tests

- `InstallationViewModelTests.SavePlacements_PersistsPositionAndAbsence`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §5.2). |
| 2026-09-26 | Claude | Écart | Pas de glisser-déposer sur un plan visuel ni d'alignement/répartition automatique : positionnement par champs numériques (X, Y en mètres). Suffisant pour patcher tout le parc et le voir au simulateur, mais plus lent que prévu par le critère (< 5 min) pour beaucoup d'appareils. Un vrai éditeur de plan (glisser sur le rendu du simulateur) est une amélioration naturelle à faire quand le besoin se fera sentir en usage réel — noté dans `docs/99-idees.md`. |
| 2026-09-26 | Claude | Développement | `389fe64` feat(installation): écran Installation — patch, sélections, lieux (doc 13) |
