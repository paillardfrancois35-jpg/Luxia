# LIVE-021 – Commandes de tempo à l'écran de jeu

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P7 |
| **Source** | [doc 18 – 4. Exigences – musique et automatique](../18-live.md) |
| **Remarque** | TAP (touche T, bouton allumé), ×2, ÷ 2, − et +, « 1 ici », interrupteur Audio et saisie directe du BPM (Entrée). Fiche créée à la revue de fin de P7. |
| **Liens** | — |

## Description

> Tap tempo, ×2, ÷2, ±, recalage de phase (« le 1 est maintenant »), choix de la source.

**Critère d'acceptation** : —

## Réalisation

- `src/Luxia.UI.Modules.Control/TempoBarViewModel.cs`
- `src/Luxia.UI.Modules.Control/Views/GameView.axaml`

## Tests

- `TempoBarTests.Tap_FourTaps_SwitchToTheTapSource`
- `TempoBarTests.TimesTwoAndDivideByTwo_ChangeTheEngineTempo_AndTheBarFollows`
- `TempoBarTests.ResyncBar_MakesTheCurrentBeatTheFirst`
- `TempoBarTests.TypingTheBpm_AfterEnter_IsNotOverwrittenByTheRefresh`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 4. Exigences – musique et automatique). |
| 2026-09-30 | Claude | Développement | P7 lot 5 : boutons TAP, ×2, ÷ 2, ±, « 1 ici », choix de la source. |
| 2026-10-01 | Utilisateur | Test | Essai P7 et re-vérifications 1 à 3 : tap, ×2 / ÷ 2 (tiennent en Audio), 1 ici, saisie du BPM, TAP bref : conformes. |
| 2026-10-02 | Claude | Note | Fiche créée à la revue de fin de P7 (elle manquait). |
