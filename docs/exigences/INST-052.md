# INST-052 – Appareil absent

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 5.2 Exigences (lieux)](../13-installation-et-lieux.md) |
| **Remarque** | « Canaux à 0 » et « scènes jouées sans erreur » ne sont vérifiables qu'à partir de P4 (pas de scènes en P3) ; le simulateur ignore déjà l'appareil absent. |
| **Liens** | INST-050, SIM-001 |

## Description

> Marquer un appareil **absent** : il n'est plus émis (ses canaux restent à 0), n'apparaît plus au simulateur ni dans les sélections actives, et les scènes l'ignorent sans erreur.

**Critère d'acceptation** : Lyre absente → aucune erreur, show inchangé pour les autres.

## Réalisation

- `src/Dmx.Patch/Model/FixturePlacement.cs` : `Absent`.
- `src/Dmx.UI.Modules.Simulator/SimulatorViewModel.cs` : un appareil absent (ou non placé) n'apparaît pas au simulateur.

## Tests

- `SimulatorViewModelTests.Refresh_AbsentFixture_IsHidden`
- `InstallationViewModelTests.SavePlacements_PersistsPositionAndAbsence`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §5.2). |
| 2026-09-26 | Claude | Écart | « Canaux à 0 » et « scènes qui l'ignorent sans erreur » ne peuvent pas encore être vérifiés : le moteur (P4) ne connaît pas encore le patch ni les scènes. Le volet simulateur / sélections actives est réalisé et testé dès P3. |
| 2026-09-26 | Claude | Développement | `389fe64` (écran) ; `7b86422` feat(simulateur): écran Simulateur 2D (doc 14) |
