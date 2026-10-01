# SORT-012 – Jamais 1200 bauds, DTR actif

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 4.1 Détection et connexion](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Le port n'est **jamais ouvert à 1200 bauds** (cette vitesse déclenche le mode programmation du Leonardo). Le signal DTR est activé à l'ouverture.

**Critère d'acceptation** : Revue ; aucune remise à zéro intempestive observée.

## Réalisation

- `firmware/arduino-dmx/arduino-dmx.ino`
- `src/Luxia.Output/Arduino/ISerialPortProvider.cs`
- `src/Luxia.Output/Arduino/SystemSerialPortProvider.cs`

## Tests

- `EnttecProtocolTests.SerialPort_IsNeverOpenedAt1200Baud`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 4.1 Détection et connexion). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-25 | Claude | Développement | `1a37b05` feat(bibliotheque): valeurs normalisées et unités d'affichage ; matrice de traçabilité |
| 2026-09-25 | Claude | Test | Test ajouté : la vitesse d'ouverture (115200) n'est jamais 1200 bauds. DTR activé à l'ouverture. |
