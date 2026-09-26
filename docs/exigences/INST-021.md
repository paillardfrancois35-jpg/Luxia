# INST-021 – Options de montage par appareil

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | M |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | Modèle et décodeur prêts (utilisés par le simulateur) ; pas d'éditeur dans l'écran Installation. |
| **Liens** | SIM-004 |

## Description

> Options par appareil : **inverser Pan**, **inverser Tilt**, **échanger Pan/Tilt**, décalage de Pan (°), bornes Pan/Tilt personnelles.

**Critère d'acceptation** : Lyre montée à l'envers → mouvements cohérents avec l'autre lyre.

## Réalisation

- `src/Dmx.Patch/Model/FixtureOptions.cs` : toutes les options du texte.
- `src/Dmx.Patch/Rules/FixtureDecoder.cs` : applique les inversions, l'échange et le décalage au décodage (utilisé par le simulateur, SIM-004).

## Tests

- `FixtureDecoderTests.Decode_InvertedPan_ReversesDirection`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Écart | Pas d'éditeur dans l'écran Installation (aucune case à cocher pour ces options) : le modèle et le décodeur les prennent en compte, mais rien ne permet encore de les régler depuis l'interface. À ajouter à la fiche de l'appareil (onglet à créer) quand une lyre réelle en aura besoin — pas testable utilement sans le moteur complet (P4) qui applique les bornes personnelles au rendu. |
| 2026-09-26 | Claude | Développement | `5ee2e57` feat(patch): nouveau projet Dmx.Patch, modèle de domaine de l'installation ; `b503eac` (décodeur) |
