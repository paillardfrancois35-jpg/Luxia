# MIDI-002 – Affectation par défaut du §3

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 5. Exigences](../18b-controleurs-midi.md) |
| **Remarque** | Affectation par défaut du doc 18b §3 : colonnes = couches du Live, lignes = scènes ; bas = arrêter la couche ; droite = Blackout, Flash, Strobe, Fumée, (Tap P7), Figer, (Auto P10), Tout arrêter ; faders 1-8 = masters, 9 = Grand Master ; Shift + bas 1/2 = pages de scènes, 3/4 = pages de couches. Commandes d'origine MIDI. |
| **Liens** | GEN-070, LIVE-002 |

## Description

> Affectation par défaut du §3 ; tous les messages traduits en commandes avec l'origine `MIDI`.

**Critère d'acceptation** : Journal : origine MIDI.

## Réalisation

- `src/Luxia.Midi/MidiController.cs` (`Handle`, `ActionOf`)
- `src/Luxia.Scenes/Rules/LiveRules.cs` (disposition commune au Live)

## Tests

- `MidiControllerTests.Pad_LaunchesTheSceneOfItsColumnAndRow_WithMidiOrigin`
- `MidiControllerTests.BottomButton_StopsItsLayer_AndRightButtons_AreTheLiveActions`
- `MidiControllerTests.ShiftBottom_ChangesTheScenePage`
- `MidiControllerTests.Pad_OfFlashLayer_FlashesWhileHeld`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 5. Exigences). |
| 2026-09-27 | Claude | Décision | Le 8e bouton de droite (« Show : transition », P8) fait « Tout arrêter » en attendant ; Tap (5e) et Auto (7e) restent sans effet jusqu'à leur phase. Pages : Shift + bas 1 / 2 (scènes), 3 / 4 (couches), le doc 18b ne précisant pas lesquels. |
| 2026-09-27 | Claude | Développement | `2f9a30e` feat(midi): APC mini MK1 et MK2 |
