# SHOW-001 – Une séquence a une longueur en mesures, une mesure

| Champ | Valeur |
|---|---|
| **Statut** | Partiel |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 2.2 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Séquence en mesures, pistes = couches ou piste d'actions (D39) ; mesure à 3 temps reportée (GEN-025, Q46). |
| **Liens** | — |

## Description

> Une séquence a une **longueur** en mesures, une **mesure** (4 temps par défaut, 3 en option – GEN-025), et des **pistes** correspondant à des couches.

**Critère d'acceptation** : —

## Réalisation

- src/Luxia.Show/Model/Sequence.cs
- src/Luxia.Show/SequenceStore.cs
- src/Luxia.Show/Runtime/SequenceRun.cs

## Tests

- SequencePlaybackTests.Blocks_StartAndStopOnTheirBars_AtAnyTempo
- ShowRulesTests.Sequence_SceneOnTheWrongTrack_AndBlocksOutOfBounds_AreReported
- ShowRulesTests.Stores_RoundTrip_SequencesAndShows

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 2.2 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lots 2 et 3 (`683cfc8`, `4c87f9e`) : `séquences.json` (doc 50 §12g), longueur en mesures décimales, pistes de couche (seules les scènes de la couche, erreur sinon) et piste d'actions. Mesure à 4 temps seulement : GEN-025 reportée (Q46, l'utilisateur ne joue pas de valses). |
| 2026-10-02 | Utilisateur | Test | Ex. 1 *Montée 16 mesures* (v1.010.079, revu en .085) : départ à la mesure, avancement « mesure N / 16 », déroulé du catalogue ✅. |
| 2026-10-02 | Utilisateur | Validation | Validé avec la phase P8 (v1.010, essai v1.010.079 → .104, contrôle final v1.010.116). Reste « Partiel » : mesure à 3 temps reportée (GEN-025, Q46). |
