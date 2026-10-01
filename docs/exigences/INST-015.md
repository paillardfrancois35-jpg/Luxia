# INST-015 – Déplacer un appareil

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | Pas encore de scène à préserver (P4) : le critère se vérifie déjà (l'identifiant de l'appareil ne change pas). |
| **Liens** | GEN-052 |

## Description

> Modifier l'adresse ou l'univers d'un appareil (saisie ou glisser dans la barre d'univers) sans impact sur les scènes.

**Critère d'acceptation** : Déplacer un PAR → scènes intactes, nouvelle adresse émise.

## Réalisation

- `src/Luxia.UI.Modules.Installation/InstallationViewModel.cs` : `MoveFixture` (saisie numérique univers/adresse par ligne) ; l'identifiant stable de l'appareil (GEN-052) est conservé.
- `src/Luxia.UI.Modules.Installation/InstallationView.axaml` : champs univers/adresse éditables par ligne, bouton « Déplacer ».

## Tests

- `InstallationViewModelTests.RenameAndMove_UpdateThePatchedFixture`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Décision | Saisie directe (numérique) plutôt que glisser dans la barre d'univers : plus simple et plus fiable à tester ; le glisser reste une amélioration possible (comme INST-051, non prioritaire tant que l'assistant d'installation n'existe pas, P5). |
| 2026-09-26 | Claude | Développement | `389fe64` feat(installation): écran Installation — patch, sélections, lieux (doc 13) |
