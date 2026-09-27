# MIDI-001 – Détection automatique des APC mini MK1 et MK2

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, MK2 validé, MK1 à valider |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 5. Exigences](../18b-controleurs-midi.md) |
| **Remarque** | Reconnaissance par le nom du port MIDI (« APC MINI », « APC mini mk2 », port secondaire MIDIIN2 écarté), profils en fichiers de données. MK2 : numéros **confirmés** par le protocole officiel d'AKAI (déposé le 2026-09-27). MK1 : numéros encore issus du protocole connu, à confirmer avec `luxia-headless midi` si le MK1 est branché un jour. |
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
| 2026-09-27 | Utilisateur | Note | Notice « APC mini mk2 – User Guide v1.7 » déposée (après un premier dépôt par erreur du guide de l'APC40 mkII, autre appareil). |
| 2026-09-27 | Claude | Note | La notice confirme la disposition (bas = Clip Stop, droite = Scene Launch, Shift, 9 faders, port 2 réservé aux notes — écarté) mais ne donne toujours pas les numéros de notes ni les codes des LED, publiés dans un document séparé d'AKAI (« APC mini mk2 Communication Protocol »). Vérification sur l'appareil avec `luxia-headless midi` maintenue. |
| 2026-09-27 | Utilisateur | Note | « APC mini mk2 - Communication Protocol - v1.0.pdf » déposé dans docs/Equipements/AKAI/Mini MK2. |
| 2026-09-27 | Claude | Test | Profil MK2 comparé au protocole : grille 0x00-0x3F (0x00 en bas à gauche), bas 0x64-0x6B (LED rouges), droite 0x70-0x77 (LED vertes), Shift 0x7A, faders CC 0x30-0x38, canaux des LED (luminosité / pulsation / clignotement), LED des boutons 0/1/2, 16 couleurs de palette : **tout concorde**, aucune correction. Écart du doc 18b levé pour le MK2. |
| 2026-09-27 | Utilisateur | Test | Exemple 10 avec l'APC mini MK2 : détection, grille (couleurs des scènes, faible / fort), pads ↔ Live dans les deux sens, boutons de droite (blackout maintenu, flash, strobe, fumée, figer, tout arrêter), boutons d'arrêt de couche, branchement à chaud : ok. |
| 2026-09-27 | Utilisateur | Test | MK2 : détection et pastille ok. MK1 pas encore branché. |
