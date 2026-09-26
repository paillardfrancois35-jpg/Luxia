# INST-018 – Fiche d'installation

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | Affichage texte simple ; export PDF / impression non fait (aucun mécanisme d'impression dans l'application pour l'instant). |
| **Liens** | INST-017 |

## Description

> **Fiche d'installation** affichable et imprimable : pour chaque appareil, nom, modèle, **réglage à faire sur l'appareil** (ex. « A008 » / « 9CH, d017 »), univers, câblage suggéré dans l'ordre de la chaîne.

**Critère d'acceptation** : Fiche lisible en PDF / impression.

## Réalisation

- `src/Dmx.UI.Modules.Installation/InstallationViewModel.cs` : `InstallationSheet` (une ligne par appareil : nom, modèle, mode, réglage sur l'appareil, univers, adresse).
- `src/Dmx.UI.Modules.Installation/InstallationView.axaml` : onglet « Fiche d'installation ».

## Tests

- Aucun test automatique (texte affiché) : vérification par le guide de démonstration.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Écart | Export PDF / impression non fait : l'application n'a pas encore de mécanisme d'impression (aucun autre écran n'en a). La fiche est lisible et copiable à l'écran ; l'export sera ajouté avec un besoin d'impression plus large (ex. show ou setlist, phase à définir). |
| 2026-09-26 | Claude | Développement | `389fe64` feat(installation): écran Installation — patch, sélections, lieux (doc 13) |
