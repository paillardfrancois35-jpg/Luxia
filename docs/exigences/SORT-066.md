# SORT-066 – Journal de l'enregistrement : actions, commandes et canaux entrelacés

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P5 |
| **Source** | [doc 10 – 7. Pilotes Simulateur, Enregistreur, Art-Net](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | SORT-060, SORT-065, LIVE-003 |

## Description

> **Journal de l'enregistrement** : à côté de chaque fichier `.dmxrec`, un fichier texte `.journal.txt` entrelace sur la même horloge (heure à la milliseconde) les actions de l'utilisateur (« IHM – onglet – action » : clics de bouton, choix dans les listes, appuis sur les scènes du Live), les commandes traitées par le moteur (noms des scènes, couches, appareils et palettes en clair) et, trame par trame, les canaux qui changent (`canal:avant→après`). Les actions IHM vont aussi au journal technique.

**Critère d'acceptation** : Test : un clic sur une scène apparaît, puis sa commande, puis le changement DMX, dans cet ordre.

## Réalisation

- `src/Luxia.Output/Drivers/RecorderOutputDriver.cs` (`Note`, journal `.journal.txt`)
- `src/Luxia.Engine/RenderEngine.cs` (événement `CommandApplied`)
- `src/Luxia.Hosting/LuxiaRuntime.cs` (`TraceUi`, commandes nommées)
- `src/Luxia.App/MainWindow.axaml.cs`, `Services/UiLabel.cs` (clics et choix tracés sur tous les écrans)
- `src/Luxia.UI.Modules.Live/LiveViewModel.cs` (appuis sur les scènes, FLASH, STROBE)

## Tests

- `tests/Luxia.UI.Tests/LiveUvRepeatTests.cs` — `Recording_JournalInterleavesClicksCommandsAndChannelChanges`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 7. Pilotes Simulateur, Enregistreur, Art-Net). |
| 2026-09-27 | Utilisateur | Question | Essai P5 (allumage lent de « UV plein », extinction immédiate, pas d'effet à la Console) : « dans le fichier d'enregistrement, il faut que tu traces les événements de l'appli, comme mes clics. Un événement de type IHM – <NOM_DE_L_ONGLET> – Action réalisée. Si tu traces bien tous les envois DMX et ces actions glissées à l'intérieur, tu devrais bien voir que la sortie ne passe pas à 255 directement. » |
| 2026-09-27 | Claude | Création | Exigence SORT-066 ajoutée au doc 10 (demande en session, doc 03 §10). |
| 2026-09-27 | Claude | Développement | Journal `.journal.txt` à côté du `.dmxrec` : IHM (clics, choix, appuis Live), MOTEUR (commandes traitées, noms en clair, refus), DMX (canaux qui changent, avant→après). Clics tracés globalement depuis la fenêtre principale, sans code propre à chaque écran ; actions IHM aussi au journal technique. |
| 2026-09-27 | Claude | Test | Test automatique vert (497 tests) : appui « UV plein » → commande → 162:0→255 dans la même trame ; second appui → 162:255→0. |
