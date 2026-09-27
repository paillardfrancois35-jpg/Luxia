# MIDI-005 – Les deux contrôleurs peuvent être branchés simultanément, avec des affectations différente

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 5. Exigences](../18b-controleurs-midi.md) |
| **Remarque** | Chaque contrôleur a son propre état ; affectations différentes par modèle via `model` dans `midi.json`. Essai à deux contrôleurs : automatique seulement (l'utilisateur n'en branchera qu'un, Q31). |
| **Liens** | GEN-072, MIDI-007, Q31 |

## Description

> Les deux contrôleurs peuvent être branchés **simultanément**, avec des affectations différentes (ex. MK2 = couches, MK1 = palettes rapides et actions).

**Critère d'acceptation** : Test avec les deux.

## Réalisation

- `src/Luxia.Midi/MidiService.cs`
- `src/Luxia.Midi/MidiBinding.cs` (`Model`)

## Tests

- `MidiServiceTests.BothModels_AreDetected_AndDriveTheEngine`
- `MidiControllerTests.Binding_ReplacesTheDefault_ForItsModelOnly`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 5. Exigences). |
| 2026-09-27 | Claude | Développement | `2f9a30e` feat(midi): APC mini MK1 et MK2 |
