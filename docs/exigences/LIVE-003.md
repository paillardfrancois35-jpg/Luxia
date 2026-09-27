# LIVE-003 – Un clic sur une scène la lance

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P5 |
| **Source** | [doc 18 – 3. Exigences – structure](../18-live.md) |
| **Remarque** | Clic = lancer ; clic sur la scène active = arrêter (ou relancer : `activeSceneClick` de live.json) ; appui maintenu sur une scène d'une couche Flash = flash, relâche = retour (la perte du pointeur relâche aussi). |
| **Liens** | CMD-010, CMD-011, CMD-014, COU-005 |

## Description

> Un clic sur une scène la lance (CMD-010) ; un clic sur la scène active l'arrête (réglable : arrête / relance). Clic maintenu sur une scène de couche Flash = flash.

**Critère d'acceptation** : Test.

## Réalisation

- `src/Luxia.UI.Modules.Live/LiveViewModel.cs` (`Press`, `Release`)
- `src/Luxia.UI.Modules.Live/LiveView.axaml.cs`

## Tests

- `LiveViewModelTests.ClickScene_Launches_ClickAgain_Stops`
- `LiveViewModelTests.FlashLayerScene_PlaysOnlyWhileHeld`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 18, 3. Exigences – structure). |
| 2026-09-26 | Claude | Développement | `dbb3b1e` feat(live): écran Live |
| 2026-09-27 | Utilisateur | Test | Essai P5, exemple 1 : clic sur une scène = lancement — conforme ; arrêt par un 2e clic et flash maintenu restent à essayer. |
| 2026-09-27 | Utilisateur | Test | Essai P5, exemple 1, étape 9 : clic sur la scène qui joue (Lyres : plafond) = arrêt — ok. |
| 2026-09-27 | Utilisateur | Test | Essai P5 : « Plein feu » puis clics répétés sur « UV plein » : allumages aléatoires des rampes, clics sans effet. Enregistrement des trames (essai A) : seulement 4 changements pour une dizaine de clics. « Contrôle mon ordre de commande : je clique sur UV plein, tu dois voir un allumage immédiat. » Le projecteur encaisse très bien les allumages rapides (piste de l'appareil écartée). |
| 2026-09-27 | Claude | Test | Journal technique : 5 lancements de « UV plein » d'affilée à 12:12:32-35 sans arrêt, puis 2 seulement pendant l'essai A, aucune commande refusée. Cause : **course écran / moteur**. L'écran décidait « lancer ou arrêter » d'après l'état relu du moteur (20 fois par seconde) ; un rafraîchissement arrivé avant que le moteur ait traité le clic remettait la scène à « arrêtée », et le clic suivant la **relançait** (fondu croisé de 0,5 s sur elle-même : rampes aléatoires) au lieu de l'arrêter. Les tests automatiques ne le voyaient pas (moteur toujours en avance sur l'écran en temps virtuel). |
| 2026-09-27 | Claude | Développement | Bascule tranchée par le moteur : `LaunchSceneCommand.StopIfPlaying` (arrête si la scène joue, sinon lance), utilisée par le Live et par l'APC ; l'écran affiche l'état attendu et ne le relit du moteur qu'après traitement du clic ; arrêts de scène écrits au journal technique. Test qui reproduit la course (`LiveUvRepeatTests`). |
| 2026-09-27 | Utilisateur | Test | v1.004.037 : toujours pas bon. Play/arrêt rapides : allumage « leeeent », arrêt immédiat ; effet aussi en testant depuis l'onglet Scènes ; maintien de 1 s ramené à 0 sans changement ; pas d'effet à la Console. |
| 2026-09-27 | Claude | Test | Journal technique : lancements et arrêts désormais bien alternés (bascule corrigée). Essai A relu : scène lancée à 12:30:44.041, trame 162-165 de 0 à 255 à +9,175 s de l'enregistrement démarré à 12:30:34.855, donc dans le même tick : la trame calculée par LuXia passe à 255 d'un coup. Reste à situer la lenteur entre la trame et l'appareil : journal entrelacé (SORT-066) et comparaison avec Daslight + DVC4 demandés. |
| 2026-09-27 | Utilisateur | Test | Écoute de la ligne avec Daslight + DVC4 Gold (port 2 en entrée) : les canaux 161 à 165 reçus sont commandés correctement. |
| 2026-09-27 | Utilisateur | Test | Écoute de la ligne avec Daslight 4 → Outils → Hardware Manager, univers 2 (prise en entrée), onglet Dmx In : « UV plein » coupé = 161 à 255, 162-165 à 0 ; allumé = 161 à 165 à 255 (procédure : docs/Equipements/DasLight/ecoute-ligne-dmx-dvc4.md). Essai de comparaison : PAR 160 W (WT05) à l'adresse 161, mêmes trames que l'UV 1 (rangées 1 à 3) : le PAR s'allume immédiatement, l'UV lentement. Une platine DMX manuelle, fader 0-255 poussé très vite, allume l'UV sans souci. |
| 2026-09-27 | Claude | Test | Conclusion : les commandes de LuXia et la ligne DMX sont correctes (trame calculée, trame reçue, PAR témoin). La lenteur est propre à la réception du BUV463. Seule différence mesurable avec la platine : LuXia saute de 0 à 255 d'une trame à l'autre, la platine passe par des valeurs intermédiaires. Essais proposés : fondu d'entrée de 0,1 s sur « UV plein » ; scène à fondu 0 envoyée par Daslight. |
