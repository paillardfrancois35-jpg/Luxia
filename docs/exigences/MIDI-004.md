# MIDI-004 – Reprise douce des faders

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 5. Exigences](../18b-controleurs-midi.md) |
| **Remarque** | Le fader ne prend la main qu'en croisant (ou rejoignant à 2 % près) la valeur courante ; il la perd si la valeur change ailleurs. |
| **Liens** | — |

## Description

> **Reprise douce des faders** : un fader physique ne prend le contrôle d'un master que lorsqu'il **croise** la valeur courante (pas de saut brutal).

**Critère d'acceptation** : Master à 100 % à l'écran, fader physique à 0 → aucun effet jusqu'au croisement.

## Réalisation

- `src/Luxia.Midi/SoftTakeover.cs`

## Tests

- `MidiControllerTests.Fader_TakesOverOnlyAfterCrossingTheCurrentValue`
- `MidiControllerTests.Fader_LosesControl_WhenTheValueIsChangedElsewhere`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 5. Exigences). |
| 2026-09-27 | Claude | Développement | `2f9a30e` feat(midi): APC mini MK1 et MK2 |
