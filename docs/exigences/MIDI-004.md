# MIDI-004 – Reprise douce des faders

| Champ | Valeur |
|---|---|
| **Statut** | Validé |
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
| 2026-09-27 | Utilisateur | Test | Faders : au démarrage, faders physiques à 0 et masters à 100 % (attendu : reprise douce). Défaut : un fader descendu vite prend la main puis la perd (bloqué vers 93 %) ; il faut revenir vers 93 % et redescendre lentement. Même chose pour tous les faders. |
| 2026-09-27 | Claude | Développement | Course écran / moteur, côté MIDI : la valeur relue du moteur a du retard sur les messages du fader ; comparée au seul dernier envoi, elle faisait croire à un changement ailleurs et le fader était lâché. `SoftTakeover` retient les 128 dernières valeurs envoyées : « changée ailleurs » seulement si la valeur du moteur ne correspond à aucune. Test `Fader_MovedFast_KeepsControl_WhileTheEngineLagsBehind` (échoue sans la correction). |
| 2026-09-27 | Utilisateur | Test | Exemple 10 avec l'APC mini MK2 (v1.004.070) : tout ok. Validé. |
