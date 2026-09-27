# INST-053 – Zones interdites par lyre, définies en visant à la main

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 13 – 5.2 Exigences](../13-installation-et-lieux.md) |
| **Remarque** | Fenêtre « Zones interdites… » (palettes de l'écran Scènes, non modale) : sélectionner la lyre au programmeur, viser un coin puis l'autre ; bornes modifiables en %. Zones propres au lieu actif. Affichage au simulateur (SIM-008) non réalisé. |
| **Liens** | GEN-085, MOT-082, Q30, SIM-008 |

## Description

> **Zones interdites** par lyre, définies en visant à la main (pad Pan/Tilt) les limites à ne pas franchir ; appliquées par le moteur (GEN-085).

**Critère d'acceptation** : Scène visant le public → bornée.

## Réalisation

- `src/Luxia.Patch/Model/ForbiddenZone.cs`, `Venue.ForbiddenZones`
- `src/Luxia.UI.Modules.Scenes/ZonesEditorViewModel.cs`, `ZoneRowViewModel.cs`, `ZonesWindow.axaml`

## Tests

- `SafetyCompilerTests.Zones_OfActiveVenue_TargetThePanTiltParameters`
- `ZonesEditorViewModelTests.AimTwoCorners_Save_ThenTheEngineKeepsTheLyreOut`
- `ZonesEditorViewModelTests.AddWithoutLyre_ExplainsWhatToDo`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, 5.2 Exigences). |
| 2026-09-26 | Claude | Développement | `63cbe3b` feat(surete): limiteurs de strobe et de fumée, zones interdites Pan/Tilt (étape 9) |
| 2026-09-26 | Claude | Décision | Pas de pad Pan/Tilt dédié (SCN-031 reste partiel) : on vise avec les faders Pan/Tilt du programmeur, déjà là. Fenêtre à part, non modale, pour ne pas charger l'écran Scènes (Q32). |
| 2026-09-26 | Claude | Développement | `4070786` feat(palettes): positions par lieu avec repli sur Générique, zones interdites saisies en visant |
| 2026-09-27 | Utilisateur | Question | Essai P5, exemple 6 étape 3 : « hyper complexe, pas ergonomique du tout ». Incompréhensions : à quoi est rattaché le programmeur quand une scène joue (agit-il sur la sortie ?) ; la fenêtre « Zones interdites… » n'est pas modale ; pointer la lyre donne un point, comment en fait-on un rectangle ? « Il me faut un bon manuel d'utilisation… et une ergonomie à revoir, genre un graphique X/Y ». |
| 2026-09-27 | Claude | Question | Explications données dans la discussion ; proposition d'un graphique Pan/Tilt (X/Y) dans la fenêtre des zones : position de la lyre en point, zones en rectangles, tracé au glisser. Décision de l'utilisateur attendue (maintenant ou dans l'analyse ergonomique de fin de phase). |
| 2026-09-27 | Utilisateur | Question | Décision : on passe l'étape 3 (définition d'une zone) ; revalidation après la refonte ergonomique (graphique Pan/Tilt, doc 99). |
