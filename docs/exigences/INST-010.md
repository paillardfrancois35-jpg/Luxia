# INST-010 – Ajouter un appareil au patch

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | — |
| **Liens** | INST-011, INST-012, GEN-053 |

## Description

> Ajouter un appareil : choix du modèle (depuis la bibliothèque), du mode, de l'univers, de l'adresse, d'un nom.

**Critère d'acceptation** : Patcher un PAR.

## Réalisation

- `src/Dmx.UI.Modules.Installation/InstallationViewModel.cs` : `AddFixtureCommand` (modèle, mode, univers, adresse, nom de base) ; copie du modèle dans le projet (GEN-053) à l'ajout.
- `src/Dmx.UI.Modules.Installation/InstallationView.axaml` : panneau « Ajouter un appareil ».

## Tests

- `InstallationViewModelTests.AddFixture_Multiple_CreatesConsecutiveAddressesAndCopiesModelIntoProject`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Développement | `389fe64` feat(installation): écran Installation — patch, sélections, lieux (doc 13) |
| 2026-09-26 | Claude | Correction | Retour ergonomique de l'utilisateur : champs de saisie (univers, adresse, quantité, écart, nom) du panneau « Ajouter un appareil » et colonnes éditables de la liste du patch bien trop étroits (« à peine le chiffre »). Largeurs revues (au moins 110-140px selon le champ). Voir doc 03 §11 (pièges déjà rencontrés). |
