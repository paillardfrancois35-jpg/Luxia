# SORT-010 – Détection automatique de l'Arduino

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé, à valider sur matériel |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 4.1 Détection et connexion](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | T-SORT-04 (3 ports USB). Sonde les cartes Arduino et le dernier port ; option « tous les ports ». |
| **Liens** | — |

## Description

> **Détection automatique** : énumération des ports série ; pour chaque port candidat (priorité aux identifiants USB de l'Arduino Leonardo), envoi d'une requête d'identification ; le port qui répond correctement est retenu.

**Critère d'acceptation** : Brancher l'Arduino sur n'importe quel port USB → connecté sans réglage.

## Réalisation

- `src/Luxia.Output/Arduino/ArduinoOutputDriver.cs`
- `src/Luxia.UI.Modules.Outputs/OutputsView.axaml`

## Tests

- `ArduinoOutputDriverTests.Connect_DoesNotProbeNonArduinoPorts_ByDefault`
- `ArduinoOutputDriverTests.Connect_FindsProjectFirmwareAmongArduinoPorts`
- `ArduinoOutputDriverTests.Connect_ProbesAllPorts_WhenEnabled`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 4.1 Détection et connexion). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-24 | Claude | Écart | Écart assumé : par défaut seuls le dernier port et les cartes Arduino (VID 2341 / 2A03) sont sondés ; option « sonder tous les ports ». Raison : certains ports virtuels (Bluetooth) bloquent longtemps à l'ouverture, et envoyer des octets à un appareil inconnu n'est pas anodin. Chargeur de démarrage (PID 0036) ignoré. Consigné au doc 10 §9. |
| 2026-09-24 | Claude | Note | Identification : label 77 d'abord, à défaut label 10 (une vraie interface Enttec est alors acceptée « Compatible Enttec »), délai 400 ms. |
