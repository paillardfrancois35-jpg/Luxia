# LIVE-022 – Affichage du morceau en cours et du style détecté avec sa confiance

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P9 |
| **Source** | [doc 18 – 4. Exigences – musique et automatique](../18-live.md) |
| **Remarque** | Bloc « Morceau en cours » sous le bloc BPM de l'écran de jeu : titre, artiste, application, position, style identifié avec sa confiance (vert ≥ 80 %, orange ≥ 50 %, rouge en dessous, gris : inconnu), mention « imposé ». Boutons « Corriger ▾ » (ce titre ou cet artiste, 14 familles), « Imposer ▾ » (jusqu'à la fin du morceau, ou retour à la détection) et « Saisir… ». Le morceau et son style figurent aussi dans l'écran Audio. À juger à l'essai. |
| **Liens** | [MUS-024](MUS-024.md) · [MUS-026](MUS-026.md) · [MUS-007](MUS-007.md) · [MUS-025](MUS-025.md) |

## Description

> Affichage du morceau en cours et du **style détecté** avec sa confiance ; bouton **Corriger le style** (liste des styles) → correction appliquée à ce titre ou à cet artiste, mémorisée (MUS).

**Critère d'acceptation** : Corriger → style changé, base musicale mise à jour.

## Réalisation

- `src/Luxia.UI.Modules.Control/NowPlayingBarViewModel.cs`
- `src/Luxia.UI.Modules.Control/Views/GameView.axaml`, `GameView.axaml.cs` (menus construits au clic)
- `src/Luxia.UI.Modules.Audio/AudioViewModel.cs` (`NowPlayingText`)
- `tools/Luxia.Tools.Captures/Program.cs` (captures « morceau identifié », « morceau inconnu, style imposé », « titre long »)

## Tests

- `NowPlayingBarTests.IdentifiedTrack_ShowsTitleArtistStyleAndConfidence`
- `NowPlayingBarTests.UnknownTrack_IsGray_AndTheStyleIsUnknown`
- `NowPlayingBarTests.WithoutTrack_ExplainsWhatToDo_AndOffersNoCorrection`
- `NowPlayingBarTests.Menus_ListTheFamiliesOfTheTaxonomy`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 4. Exigences – musique et automatique). |
| 2026-10-03 | Claude | Développement | P9 lot 4 : bloc de l'écran de jeu (hauteur fixe de 46 px, textes coupés avec info-bulle), lignes « ♫ » et « ♪ » au Journal, morceau dans l'écran Audio. Le panneau « Pilote automatique » garde ses champs « Titre en cours : — » et « Style : — » (P10, avec « Show choisi »). |
| 2026-10-03 | Utilisateur | Test | Second essai 1.011.075, ex. 1 à 8 : bloc « Morceau en cours » conforme ; demande : garder Corriger ▾ et Imposer ▾, les rapprocher de la pastille de style et les encadrer avec elle (Base… et Saisir… restent à droite) ; dans « Saisir… », l'artiste avant le titre. |
| 2026-10-03 | Claude | Développement | Second essai : pastille, Corriger ▾ et Imposer ▾ dans un même cadre ; « Saisir… » : champ Artiste puis champ Titre. |
