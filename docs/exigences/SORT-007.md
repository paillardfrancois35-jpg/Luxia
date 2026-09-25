# SORT-007 – Écran « Sorties » et test de sortie

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P0 |
| **Source** | [doc 10 – 3. Exigences – routage et supervision (côté PC)](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | CMD-024, D19, D21, GEN-002, Q16, Q17, Q18, Q23 |

## Description

> Écran « Sorties » : liste des pilotes, état, port, trames/s, bouton reconnecter, bouton test (chenillard canal par canal via la commande `TesterSortie`, CMD-024) avec **plage réglable** (par défaut 1-16), **liste de canaux exclus** réglable (par défaut : 180, fumée) et **valeur de test** réglable (par défaut 50 %), afin de ne déclencher ni la fumée ni les canaux Reset/contrôle (Q16).

**Critère d'acceptation** : Revue.

## Réalisation

- `src/Dmx.Core/Settings/Preferences.cs`
- `src/Dmx.Engine/TestPattern.cs`
- `src/Dmx.Messaging/Commands/TestOutputCommand.cs`
- `src/Dmx.Messaging/Commands/TestPatternMode.cs`
- `src/Dmx.UI.Modules.Outputs/DriverStatusViewModel.cs`
- `src/Dmx.UI.Modules.Outputs/OutputsView.axaml`
- `src/Dmx.UI.Modules.Outputs/OutputsViewModel.cs`

## Tests

- `ReferenceShowP0Tests.ReferenceRecording_LightsChannels1To179InOrder_NeverSmoke`
- `RenderEngineTests.TestPattern_OnlyCurrentChannelIsLit_WithConfiguredValue`
- `RenderEngineTests.TestPattern_SkipsExcludedChannels`
- `RenderEngineTests.TestPattern_WalksChannelsUsingElapsedTime`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 3. Exigences – routage et supervision (côté PC)). |
| 2026-09-24 | Claude | Question | Q16 : plage du chenillard (1-16 selon SORT-007, 1-180 selon le doc 41). |
| 2026-09-24 | Utilisateur | Réponse | Utilisateur : plage réglable (1-16 par défaut, 1-180 pour l'enregistrement de démonstration) + **liste de canaux exclus** (180 = fumée par défaut) + **valeur de test** réglable (50 % par défaut), pour ne déclencher ni fumée ni canaux Reset / contrôle. Reporté dans SORT-007 et le doc 41. |
| 2026-09-24 | Claude | Question | Q17 : source du chenillard alors que le moteur est « vide » en P0. |
| 2026-09-24 | Utilisateur | Réponse | Utilisateur : commande `TesterSortie` traitée par le moteur, ajoutée au catalogue (CMD-024) et soumise aux limiteurs de sûreté quand ils existeront (P5) → décision D19. |
| 2026-09-24 | Claude | Question | Q18 : interface en P0. |
| 2026-09-24 | Utilisateur | Réponse | Utilisateur : fenêtre Avalonia minimale limitée à l'écran Sorties (shell et thème en P1). |
| 2026-09-24 | Claude | Décision | D21 : tant que le test est actif, il **remplace** la restitution de l'univers testé, faders pris compris (un seul canal allumé) : c'est un outil de diagnostic, pas une couche. |
| 2026-09-24 | Claude | Décision | Forme « rampe » ajoutée (tous les canaux varient) pour le test d'endurance T-SORT-07 (`dmx-headless endurance`), plutôt qu'un générateur hors moteur qui aurait contourné GEN-002. |
| 2026-09-24 | Claude | Question | Q23 (ouverte) : un chenillard canal par canal n'allume pas un appareil à gradateur maître (PAR en 7 canaux : le maître est à 0 quand la couleur passe). Proposition : liste de canaux « maintenus » à la valeur de test. En attendant, le guide P0 conseille le mode 3 canaux. |
| 2026-09-24 | Claude | Développement | `ac6a80b` feat(moteur): boucle cadencée à 40 Hz, commandes et test de sortie |
| 2026-09-24 | Claude | Développement | `e7ca543` feat(hote): journal technique, assemblage P0 et outil dmx-headless |
| 2026-09-24 | Claude | Développement | `d5fdbac` feat(app): écran Sorties (état des pilotes, Arduino, test, enregistreur, cadence) |
| 2026-09-24 | Claude | Développement | `a719192` feat(moteur): mode rampe du test de sortie et commande d'endurance |
| 2026-09-24 | Claude | Note | Enregistrement de démonstration `samples/Show de référence/Enregistrements/P0-chenillard-1-180.dmxrec` (180 exclu, 50 %, 250 ms), rejoué en temps virtuel par un test de non-régression. |
| 2026-09-25 | Utilisateur | Question | Q23 : demande de précision (« quelle est la question exactement ? ») ; question reformulée avec un exemple (PAR en 7 canaux à l'adresse 1 : maintenir le canal 1 à 50 % pendant que le chenillard passe sur 2, 3, 4). En attente de réponse. |
