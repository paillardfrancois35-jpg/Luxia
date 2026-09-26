# INST-050 – Créer, dupliquer, activer un lieu

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 5.2 Exigences (lieux)](../13-installation-et-lieux.md) |
| **Remarque** | — |
| **Liens** | INST-051, INST-052 |

## Description

> Créer, **dupliquer**, renommer, supprimer un lieu ; choisir le **lieu actif**. Un lieu « Générique » existe par défaut.

**Critère d'acceptation** : Dupliquer « Salle X » en « Salle Y ».

## Réalisation

- `src/Dmx.Patch/Model/Venue.cs` : `VenueSet` (lieu « Générique » par défaut).
- `src/Dmx.Patch/VenueStore.cs` : `lieux.json`.
- `src/Dmx.UI.Modules.Installation/InstallationViewModel.cs` : `CreateVenueCommand`, `DuplicateVenueCommand`, `ActivateVenueCommand`, `DeleteVenueAsync` (renommer : champ `Name` de la ligne, enregistré avec « Enregistrer le lieu »).
- `src/Dmx.UI.Modules.Installation/VenueRowViewModel.cs` (`IsActive`) et `InstallationView.axaml` : le lieu actif est mis en évidence dans la liste (point vert + « (actif) »).

## Tests

- `StoresTests.VenueStore_Missing_ReturnsDefaultGenericVenue`
- `StoresTests.VenueStore_SaveThenLoad_KeepsActiveVenue`
- `InstallationViewModelTests.Venues_HaveAGenericVenueByDefault`
- `InstallationViewModelTests.CreateVenue_ThenActivate_ChangesActiveVenue`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §5.2). |
| 2026-09-26 | Claude | Développement | `5ee2e57` feat(patch): nouveau projet Dmx.Patch, modèle de domaine de l'installation ; `389fe64` (écran) |
| 2026-09-26 | Claude | Correction | Retour utilisateur : après « Activer », rien ne distinguait visuellement le lieu actif dans la liste (confirmé seulement en allant voir le Simulateur). Ajout d'un repère (point vert + « (actif) ») sur la ligne du lieu actif. |
