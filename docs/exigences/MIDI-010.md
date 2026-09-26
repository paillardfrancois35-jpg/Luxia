# MIDI-010 – Sur MK2, la couleur des pads reprend la couleur des scènes

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | M |
| **Phase** | P5 |
| **Source** | [doc 18 – 5. Exigences](../18b-controleurs-midi.md) |
| **Remarque** | MK2 : couleur de la scène approchée dans une palette de couleurs franches (luminosité mise de côté) ; faible luminosité si disponible, pleine si active, pulsation en fondu d'entrée. |
| **Liens** | GEN-106, MIDI-003 |

## Description

> Sur MK2, la couleur des pads reprend la **couleur des scènes** (GEN-106), approchée dans la palette de couleurs du contrôleur.

**Critère d'acceptation** : —

## Réalisation

- `src/Luxia.Midi/MidiController.cs` (`NearestPaletteIndex`, `PadLed`)

## Tests

- `MidiControllerTests.NearestPalette_IgnoresBrightness`
- `MidiControllerTests.Leds_Mk2_UseTheSceneColor_DimWhenAvailable_FullWhenActive`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 5. Exigences). |
| 2026-09-27 | Claude | Développement | `2f9a30e` feat(midi): APC mini MK1 et MK2 |
