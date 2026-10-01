# SORT-015 – Version du firmware

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P0 |
| **Source** | [doc 10 – 4.1 Détection et connexion](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> L'identification récupère la **version du firmware** et l'affiche ; une version trop ancienne produit un avertissement (pas un blocage).

**Critère d'acceptation** : Affichage de la version.

## Réalisation

- `src/Luxia.Output/Arduino/ArduinoOutputDriver.cs`

## Tests

- `ArduinoOutputDriverTests.OldFirmware_IsAcceptedWithWarning`
- `EnttecProtocolTests.TryParseIdentity_ReadsVersionAndChannels`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 4.1 Détection et connexion). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-24 | Claude | Note | Identification : label 77 d'abord, à défaut label 10 (une vraie interface Enttec est alors acceptée « Compatible Enttec »), délai 400 ms. |
| 2026-09-24 | Claude | Note | Version minimale 1.0 ; une version plus ancienne est acceptée avec la mention « firmware ancien ». |
