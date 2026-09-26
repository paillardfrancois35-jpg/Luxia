# MIDI-006 – Débranchement / rebranchement à chaud

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 5. Exigences](../18b-controleurs-midi.md) |
| **Remarque** | Recherche des ports toutes les 2 s : un contrôleur débranché est fermé, un rebranché est rouvert et toutes ses LED renvoyées ; une erreur d'envoi (débranché entre deux recherches) est absorbée. |
| **Liens** | GEN-073 |

## Description

> Débranchement / rebranchement à chaud (GEN-073) ; le retour lumineux est restauré au rebranchement.

**Critère d'acceptation** : Test.

## Réalisation

- `src/Luxia.Midi/MidiService.cs` (`Scan`)

## Tests

- `MidiServiceTests.Unplug_ThenReplug_RestoresTheLeds`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 5. Exigences). |
| 2026-09-27 | Claude | Développement | `2f9a30e` feat(midi): APC mini MK1 et MK2 |
