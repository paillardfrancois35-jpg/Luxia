# MIDI-011 – Blackout du contrôleur tant que maintenu (comme Daslight)

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 5. Exigences](../18b-controleurs-midi.md) |
| **Remarque** | Bouton de droite n° 1 de l'APC : appui = blackout, relâche = blackout annulé, même s'il avait été activé à l'écran ; bouton de l'écran et touche B inchangés (bascule) ; bascule au contrôleur possible par `midi.json` (`blackoutToggle`). |
| **Liens** | MIDI-002, MIDI-007, GEN-082, doc 99 (accroches des actions) |

## Description

> Le bouton **Blackout** du contrôleur agit **tant qu'il est maintenu** (comme Daslight) : appui = blackout, relâche = blackout annulé, **même s'il avait été activé à l'écran ou au clavier** (la note MIDI reprend le dessus). Le bouton de l'écran et la touche B restent des bascules. Une bascule reste possible au contrôleur par affectation (`blackoutToggle`, MIDI-007).

**Critère d'acceptation** : Écran : blackout ; note appuyée : blackout ; note relâchée : blackout annulé.

## Réalisation

- `src/Luxia.Midi/MidiAction.cs` (`Blackout`, `BlackoutToggle`)
- `src/Luxia.Midi/MidiController.cs`

## Tests

- `MidiControllerTests.BlackoutNote_IsMomentary_EvenIfBlackoutWasAlreadyOnFromTheScreen`
- `MidiControllerTests.BlackoutToggle_ByBinding_TogglesOnPressOnly`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 5. Exigences). |
| 2026-09-27 | Utilisateur | Question | Discussion Figer / Blackout : sur Daslight, une note MIDI pilotait le blackout — note jouée = blackout, note relâchée = annulé ; l'écran garde un bouton persistant ; la commande MIDI reprend le dessus quand la note est jouée (écran : blackout ; note appuyée : reste blackout ; note relâchée : blackout annulé). Utile pour des clignotements simples. |
| 2026-09-27 | Claude | Création | Exigence MIDI-011 ajoutée au doc 18b §5 (demande en session, doc 03 §10). Avant : le bouton Blackout de l'APC basculait à chaque appui. |
| 2026-09-27 | Claude | Décision | Action `blackout` du contrôleur = maintien (défaut) ; nouvelle action `blackoutToggle` pour une bascule au contrôleur si voulu. LED du bouton allumée tant que le blackout est actif, quelle qu'en soit l'origine. La question plus large des « accroches » de toutes les actions à des contrôles (comme Daslight) est notée au carnet d'idées, à discuter. |
