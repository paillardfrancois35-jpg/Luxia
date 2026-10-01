# SORT-011 – Dernier port essayé en premier

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 4.1 Détection et connexion](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Le dernier port utilisé est mémorisé et essayé en premier.

**Critère d'acceptation** : Démarrage plus rapide (< 1 s) quand le port n'a pas changé.

## Réalisation

- `src/Luxia.Core/Settings/Preferences.cs`
- `src/Luxia.Output/Arduino/ArduinoOutputDriver.cs`

## Tests

- `ArduinoOutputDriverTests.Connect_TriesLastPortFirst_AndReportsSelectedPort`
- `PreferencesAndProjectTests.Preferences_Update_IsPersisted`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 4.1 Détection et connexion). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-24 | Claude | Développement | `5271ed9` feat(persistance): fichiers JSON versionnés, migrations, préférences, projet |
| 2026-09-24 | Claude | Développement | `e7ca543` feat(hote): journal technique, assemblage P0 et outil dmx-headless |
| 2026-09-24 | Claude | Note | Le port retenu est mémorisé dans `preferences.json` (`lastPort`) via l'événement `PortSelected` du pilote. |
