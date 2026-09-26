# MIDI-007 – Affectations modifiables et enregistrées dans le projet

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P5 |
| **Source** | [doc 18 – 5. Exigences](../18b-controleurs-midi.md) |
| **Remarque** | Affectations modifiées dans `midi.json` (contrôle « pad c l » / « bas n » / « droite n » / « fader n » → action, scène, couche, modèle facultatif), vérifiées par `valider` ; pas d'éditeur à l'écran (Q32). |
| **Liens** | MIDI-008, Q32 |

## Description

> Affectations modifiables et enregistrées dans le projet : chaque pad / bouton / fader → une commande avec paramètres.

**Critère d'acceptation** : —

## Réalisation

- `src/Luxia.Midi/MidiSettings.cs`, `MidiBinding.cs`, `MidiControl.cs`, `MidiStore.cs`
- `src/Luxia.Hosting/Tools/ProjectValidator.cs` (`CheckMidi`)

## Tests

- `MidiControllerTests.Binding_ReplacesTheDefault_ForItsModelOnly`
- `MidiControllerTests.Control_Parses`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 5. Exigences). |
| 2026-09-27 | Claude | Développement | `2f9a30e` feat(midi): APC mini MK1 et MK2 |
