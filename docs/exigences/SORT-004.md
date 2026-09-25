# SORT-004 – État publié par chaque pilote

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 3. Exigences – routage et supervision (côté PC)](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | — |

## Description

> Chaque pilote publie son état : `Déconnecté`, `Connexion…`, `Connecté`, `Erreur` (+ message), trames/s effectives, nombre d'erreurs.

**Critère d'acceptation** : Affichage dans l'indicateur permanent (GEN-104).

## Réalisation

- `src/Dmx.Messaging/Events/OutputEvents.cs`
- `src/Dmx.Output/OutputDriverStatus.cs`
- `src/Dmx.UI.Modules.Outputs/DriverStatusViewModel.cs`
- `src/Dmx.UI.Modules.Outputs/OutputsView.axaml`

## Tests

- `OutputRouterTests.StateChanges_ArePublishedOnBus`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 3. Exigences – routage et supervision (côté PC)). |
| 2026-09-24 | Claude | Décision | EVT-002 `TrameÉmise` non publié sur le bus : l'interface lit la dernière trame du moteur à son rythme (état observable, doc 02 §6.4). À publier si le simulateur (P3) en a besoin. |
| 2026-09-24 | Claude | Décision | Compteur d'erreurs incrémenté à chaque **entrée** dans l'état Erreur (pas à chaque tentative de reconnexion). |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-24 | Claude | Développement | `d5fdbac` feat(app): écran Sorties (état des pilotes, Arduino, test, enregistreur, cadence) |
