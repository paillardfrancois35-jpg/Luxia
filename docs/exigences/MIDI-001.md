# MIDI-001 – Détection automatique des APC mini MK1 et MK2

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 5. Exigences](../18b-controleurs-midi.md) |
| **Remarque** | Reconnaissance par le nom du port MIDI (« APC MINI », « APC mini mk2 », port secondaire MIDIIN2 écarté), profils en fichiers de données. Numéros de notes et de LED pris dans les protocoles publiés par AKAI, les notices du dépôt ne les donnant pas (écart) : à confirmer avec `luxia-headless midi`. |
| **Liens** | GEN-072, Q31, D30 |

## Description

> Détection automatique des APC mini MK1 et MK2 (nom du périphérique MIDI) et chargement du profil correspondant (GEN-072).

**Critère d'acceptation** : Brancher chaque modèle → reconnu.

## Réalisation

- `src/Luxia.Midi/Profiles/apc-mini-mk1.json`, `apc-mini-mk2.json`
- `src/Luxia.Midi/ControllerProfiles.cs`, `ControllerProfile.cs`
- `tools/Luxia.Tools.Headless/Commands.cs` (`midi`)

## Tests

- `MidiControllerTests.Profiles_RecognizeBothModels`
- `MidiServiceTests.BothModels_AreDetected_AndDriveTheEngine`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 5. Exigences). |
| 2026-09-27 | Claude | Écart | Doc 18b §2 : les numéros devaient être « relevés dans les notices présentes ». Or le PDF du MK1 n'a pas de table MIDI et celui du MK2 ne contient que 2 pages (couverture). Numéros repris des protocoles de communication publiés par AKAI pour les deux modèles, marqués « à confirmer sur l'appareil » dans les profils ; outil `luxia-headless midi` ajouté pour les vérifier en appuyant sur chaque contrôle. Reporté dans le doc 18b (notes de réalisation). |
| 2026-09-27 | Utilisateur | Réponse | Q31 : un seul contrôleur branché à la fois, modèle au choix de Claude ; accord pour winmm sans dépendance. |
| 2026-09-27 | Claude | Développement | `2f9a30e` feat(midi): APC mini MK1 et MK2 |
