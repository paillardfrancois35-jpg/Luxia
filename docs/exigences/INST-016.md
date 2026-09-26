# INST-016 – Changer le mode d'un appareil patché

| Champ | Valeur |
|---|---|
| **Statut** | Validé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | Rapport des canaux perdus/gagnés et, depuis P4, des valeurs de scènes qui ne s'appliqueront plus (SC-03) ; les scènes sont conservées. |
| **Liens** | GEN-053 |

## Description

> **Changer le mode** d'un appareil patché : rapport des attributs perdus / gagnés, et des scènes impactées ; confirmation requise.

**Critère d'acceptation** : Passer un PAR de 7CH à 3CH → rapport « Strobe, Programme perdus ; 3 scènes impactées ».

## Réalisation

- `src/Dmx.Patch/Rules/FixtureUpdateImpact.cs` : `ForModeChange` (canaux perdus/gagnés par nom).
- `src/Dmx.UI.Modules.Installation/InstallationViewModel.cs` : `ChangeModeAsync`, confirmation (GEN-103) si impact non vide.

## Tests

- `FixtureUpdateImpactTests.ForModeChange_FromRichToSimpleMode_ReportsLostChannels`
- `FixtureUpdateImpactTests.ForModeChange_SameMode_IsEmpty`
- `InstallationViewModelTests.ChangeMode_WithImpact_AsksConfirmation`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Écart | Le volet « scènes impactées » du rapport n'est pas calculable avant P4 (les scènes n'existent pas) : le rapport se limite aux canaux perdus/gagnés pour l'instant. À compléter en P4. |
| 2026-09-26 | Claude | Développement | `5ee2e57` feat(patch): nouveau projet Dmx.Patch, modèle de domaine de l'installation ; `389fe64` (écran) |
| 2026-09-26 | Claude | Développement | `04f98b0` feat(simulateur,installation): aperçu en aveugle au simulateur, impact d'un changement de mode sur les scènes |
| 2026-09-26 | Claude | Note | SC-03 (P3-P4) : le changement de mode liste en plus les valeurs de scènes qui seront ignorées dans le nouveau mode ; les scènes sont conservées (elles visent des attributs, pas des canaux). Test `ChangeMode_UsedByThreeScenes_ReportsImpact_AndKeepsScenes`. |
| 2026-09-26 | Utilisateur | Test | Essai P4, exemple 12 : scène « Essai strobe » (Strobe du PAR 1), PAR 1 passé de 7 à 3 canaux : la confirmation liste la valeur de strobe de cette scène et pas les scènes « Phase P4 » ; annulation, PAR 1 resté en 7 canaux. |
| 2026-09-26 | Utilisateur | Validation | Validé (écart P3 levé). |
