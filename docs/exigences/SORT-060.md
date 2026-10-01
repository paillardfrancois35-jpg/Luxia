# SORT-060 – Enregistreur de trames

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 7. Pilotes Simulateur, Enregistreur, Art-Net](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> **Enregistreur** : écrit chaque trame avec son horodatage (temps écoulé depuis le début, en ms) dans un fichier compact ; en-tête avec version, univers, fréquence.

**Critère d'acceptation** : Enregistrer 10 min, relire : trames et horodatages identiques.

## Réalisation

- `src/Luxia.Output/Drivers/RecorderOutputDriver.cs`
- `src/Luxia.Output/Recording/RecordingFormat.cs`
- `src/Luxia.Output/Recording/RecordingReader.cs`
- `src/Luxia.Output/Recording/RecordingWriter.cs`
- `src/Luxia.UI.Modules.Outputs/OutputsView.axaml`

## Tests

- `ConsoleLatencyTests.Override_AppearsInRecordedFrame_OnNextTick`
- `RecordingTests.IdenticalFrames_AreStoredCompactly`
- `RecordingTests.RecorderDriver_AttachedHot_WritesFramesToFile`
- `RecordingTests.WriteThenRead_RoundTripsFramesAndTimestamps`
- `ReferenceShowP0Tests.Engine_InVirtualTime_ReproducesReferenceRecording`
- `ReferenceShowP0Tests.ReferenceRecording_LightsChannels1To179InOrder_NeverSmoke`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 7. Pilotes Simulateur, Enregistreur, Art-Net). |
| 2026-09-24 | Claude | Décision | Format `.dmxrec` binaire : longueur 0 = trame identique à la précédente (univers au repos : 6 octets par trame). Fin de fichier tronquée tolérée. Documenté au doc 50. |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-24 | Claude | Développement | `4050b85` docs(p0): démonstration P0, show de référence, format des données |
| 2026-09-24 | Claude | Note | Enregistrement de démonstration `samples/Show de référence/Enregistrements/P0-chenillard-1-180.dmxrec` (180 exclu, 50 %, 250 ms), rejoué en temps virtuel par un test de non-régression. |
