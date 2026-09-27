# MIDI-003 – Retour lumineux du §4, mis à jour à chaque changement d'état

| Champ | Valeur |
|---|---|
| **Statut** | Validé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 5. Exigences](../18b-controleurs-midi.md) |
| **Remarque** | LED recalculées toutes les 40 ms d'après l'état du moteur (donc aussi pour les actions à la souris ou d'un autre contrôleur), seuls les changements envoyés ; boutons stop allumés quand la couche joue ; blackout, figé, fumée, boutons maintenus ; toutes éteintes à la fermeture. |
| **Liens** | MIDI-010 |

## Description

> Retour lumineux du §4, mis à jour à chaque changement d'état (latence < 100 ms), y compris pour les actions venant de la souris ou du Directeur.

**Critère d'acceptation** : Lancer une scène à la souris → pad allumé.

## Réalisation

- `src/Luxia.Midi/MidiController.cs` (`Leds`)
- `src/Luxia.Midi/MidiService.cs` (`UpdateLeds`, minuterie 40 ms)

## Tests

- `MidiControllerTests.Leds_Mk1_YellowAvailable_GreenActive_BlinkingWhileFadingIn`
- `MidiControllerTests.Leds_Mk2_UseTheSceneColor_DimWhenAvailable_FullWhenActive`
- `MidiServiceTests.Dispose_TurnsAllLedsOff`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 5. Exigences). |
| 2026-09-27 | Claude | Développement | `2f9a30e` feat(midi): APC mini MK1 et MK2 |
| 2026-09-27 | Utilisateur | Test | Exemple 10 avec l'APC mini MK2 (v1.004.070) : tout ok. Validé. |
