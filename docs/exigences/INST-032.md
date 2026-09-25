# INST-032 – Sélections manuelles créées et réordonnées

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 4. Sélections](../13-installation-et-lieux.md) |
| **Remarque** | Création par cochage dans la liste réalisée ; sélection au plan et réordonnancement par glisser non faits (les opérations d'ordre d'INST-033 couvrent le besoin autrement). |
| **Liens** | INST-030, INST-033 |

## Description

> Sélections **manuelles**, créées par sélection au plan ou dans la liste ; réordonnables par glisser.

**Critère d'acceptation** : —

## Réalisation

- `src/Dmx.UI.Modules.Installation/InstallationViewModel.cs` : `CreateSelectionCommand` (cases à cocher dans la liste du patch, onglet « Univers et patch »).

## Tests

- `InstallationViewModelTests.CreateSelection_FromCheckedFixtures_ThenReverse`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §4). |
| 2026-09-26 | Claude | Écart | Pas de sélection au clic dans le plan du lieu (le lieu n'a pas encore d'éditeur de plan interactif, seulement des champs numériques, INST-051) ni de réordonnancement par glisser (les boutons d'ordre d'INST-033 couvrent les besoins courants : inverser, pair/impair, moitiés, position). Sélection au plan à ajouter avec un éditeur de plan interactif (idée notée, `docs/99-idees.md`). |
| 2026-09-26 | Claude | Développement | `389fe64` feat(installation): écran Installation — patch, sélections, lieux (doc 13) |
