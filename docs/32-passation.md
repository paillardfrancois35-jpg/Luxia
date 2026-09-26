# 32 – Passation entre discussions

> Point d'entrée pour reprendre le développement dans une **nouvelle discussion** sans relire tout l'historique.
> À tenir à jour à chaque fin de phase (section 1, 4 et 5). Dernière mise à jour : 2026-09-26, P4 validée par l'utilisateur ; prochaine étape : **P5 – Couches, Palettes, Live, MIDI** (jalon 1).

## 1. Où en est-on

| Phase | État | Branche | Guide | Validation utilisateur |
|---|---|---|---|---|
| P0 – Fondations | Développée | `p0/fondations` (fusionnée dans `main`, `v1.001`) | [demos/P0-fondations.md](demos/P0-fondations.md) | ⏳ essais matériel prévus |
| P1 – Console | Développée | `p1/console` (fusionnée dans `main`, `v1.001`) | [demos/P1-console.md](demos/P1-console.md) | ⏳ |
| P2 – Bibliothèque | Développée | `p2/bibliotheque` (fusionnée dans `main`, `v1.001`) | [demos/P2-bibliotheque.md](demos/P2-bibliotheque.md) | ⏳ |
| P3 – Installation + Simulateur | Validée | `p3/installation-simulateur` (fusionnée dans `main`, `v1.002`) | [demos/P3-installation-simulateur.md](demos/P3-installation-simulateur.md) | ✅ 2026-09-26, matériel réel (4 PAR + 1 lyre) |
| P4 – Moteur + Scènes | Validée | `p4/moteur-scenes` (fusionnée dans `main`, `v1.003`) | [demos/P4-moteur-scenes.md](demos/P4-moteur-scenes.md) | ✅ 2026-09-26, matériel réel (4 PAR + lyre 1 + barre 1), exemples 1 à 12 |
| P5 – Couches, Palettes, Live, MIDI | **À démarrer** | `p5/…` à créer depuis `main` | — | — |

- **P0, P1, P2 validées par l'utilisateur le 2026-09-25** (avec le matériel réel) et fusionnées dans `main` (`v1.001`).
- **P3 validée par l'utilisateur le 2026-09-26**, tour de test complet en direct (4 PAR + 1 lyre) mené pas à pas dans la discussion : crash (Univers affiché), Identifier (couleur puis fige d'écran), largeurs de champs, sélections, lieux (création/activation/mise en évidence), simulateur (corps toujours visible, roue de couleur sans couleur définie), Sorties/SORT-008, version en barre de titre. Tous corrigés au fil de l'eau, 297 tests verts. Fusionnée dans `main`, étiquette `v1.002`.
- **P4 développée le 2026-09-26** (branche `p4/moteur-scenes`, poussée) : moteur complet (D26 : calcul sur un modèle compilé),
  nouveau projet `Luxia.Scenes` (scènes, palettes, couches, compilation), écran **Scènes** (programmeur, étapes, palettes, aveugle),
  blackout / Grand Master toujours visibles, Console en surcharges d'attributs (CONS-022, CONS-091, CONS-042), outils sans interface
  (`valider`, `jouer`, `scenario`), 10 scènes « Phase P4 » dans le show de référence avec trames de référence. 408 tests verts.
  Bilan P4 : 84 exigences, 75 Réalisé, 8 Partiel (GEN-023 tempo fixe, GEN-040/042 sûreté P5, GEN-112/113, PAL-007, SCN-030, SCN-031),
  1 Non réalisé (MOT-054, M). **Décisions** D26 à D28 (doc 02 §19) ; **écart** : couches par défaut avancées en P4 (COU-006 partiel).
- **P4 validée par l'utilisateur le 2026-09-26** : guide déroulé pas à pas dans la discussion, au matériel (4 PAR LPC008S, lyre 1 « 111 »,
  barre 1 « 51 » en 24 canaux ; lyre 2 non raccordée). Corrigé ou ajouté au fil de l'essai, chacun avec sa fiche :
  blanc chaud recalé (100 / 42 / 0 %) ; vitesse de scène appliquée en direct (`Playback.Bind`) ; enregistrement robuste aux verrous
  passagers de fichier (GEN-118, antivirus) ; **toute exception journalisée** dans le journal global, y compris les erreurs de liaison
  Avalonia, avec message dans la barre d'état (GEN-117) ; Ctrl+Z / Ctrl+Y dans l'écran Scènes ; bouton visible « ✎ Mettre à jour une
  palette avec le programmeur » (les réglages partaient dans les étapes) et lyre 1 calibrée dans les palettes de position ; case
  Aveugle dans l'en-tête du programmeur ; **numéro de compilation** affiché en développement (GEN-119 : `v1.00N.NNN`, compteur hors
  dépôt dans `build/numero-de-compilation.txt`). Fusionnée dans `main`, étiquette `v1.003`.
  Bilan final P4 : 94 exigences, **36 Validé**, 49 Réalisé (couvertes par les tests automatiques, non rejouées à la main), 8 Partiel
  (GEN-023, GEN-040, GEN-042, GEN-112, GEN-113, PAL-007, SCN-030, SCN-031), 1 Non réalisé (MOT-054).
- **Remarque de l'utilisateur à la validation de P4 (à garder en tête pour toute la suite)** : l'ergonomie est jugée « un beau bazar » —
  tout se mélange, trop de place, trop de défilement ; formats d'affichage et formes à revoir. **Non bloquant** tant que le fonctionnel
  tient : l'utilisateur compte sur l'IA pour **construire les shows** (GEN-130 à 134) et lui expliquer ce qu'il veut. Chantier
  d'ergonomie conséquent noté en [doc 99](99-idees.md), à planifier plus tard (analyse approfondie de tous les écrans). En attendant :
  ne pas ajouter de désordre (panneaux compacts, repliables si possible), et soigner tout ce qui aide l'IA à produire des shows
  (formats documentés, `valider`, `jouer`, `scenario`, import de scènes).
- **Fiches d'exigences** : [exigences/](exigences/README.md) — une fiche par exigence travaillée, avec statut et **historique complet** (questions, décisions et leur pourquoi, écarts, commits, tests, validations). **Lire la fiche avant de toucher à une exigence.**
- **Statut exigence par exigence** : [31-matrice-exigences.md](31-matrice-exigences.md), générée depuis les fiches. Bilan P3 : 47 exigences, 34 Réalisé, 6 Partiel, 7 Non réalisé.
- **Reliquats P0-P2 traités en P3** : SORT-008, CONS-007/020 à 024/041/043/092 (patch et mode appareils), BIB-093/096/097/098/099/100. **BIB-094** (plage « Fondu » du LPC008S) reste ouverte : une réserve honnête a été ajoutée au libellé, mais la correction exacte attend une vérification en direct sur l'appareil. **CONS-091** reste correctement en P4 (non traité maintenant, cohérent avec le doc 40).
- **Reste à faire sur les phases développées** (hors validation matérielle) :
  - CONS-008 / GEN-042 : blackout appliqué aux surcharges (fait en P4) ; **limites de sûreté → P5**.
  - P4 partiels : GEN-023 (tempo fixe 120 BPM → P7), GEN-040 (sûreté dans la chaîne → P5), GEN-112 (journal des commandes affiché → Live, P5), GEN-113 (enregistrement des trames depuis l'appli), PAL-007 (grilles de palettes), SCN-030 (sélection au plan), SCN-031 (roue chromatique, pad Pan/Tilt), MOT-054 (couleurs par teinte → P6), COU-006 (couches par défaut, éditeur en P5).
  - MOT-103 : `scenario` non rejoué à la main (tests automatiques seulement).
  - Lyre 2 : positions des palettes proposées, à calibrer quand elle sera raccordée.
  - CONS-021 (pastille couleur, pad Pan/Tilt XY combinés) → reste canal par canal ; à revoir avec le programmeur (P5).
  - INST-021 (options de montage : pas d'éditeur dans l'écran), INST-034 (sélection de cellules), INST-051 (glisser-déposer sur le plan) → partiels, voir leurs fiches.
  - SIM-007 (fenêtre détachable), SIM-008 (zones interdites/repères), SIM-010 (sélection reprise par le programmeur) → non réalisés, dépendent de P4/P5.
  - GEN-104 : indicateurs blackout (P4) et mode auto (P10) affichés « — ».
  - Non réalisés (priorité S) : CONS-044, GEN-058, GEN-108, BIB-027 (partiel), BIB-084, SIM-013, SORT-063, SORT-064.
  - **Mesure de gigue de 15 min (D23)** : `dmx-headless gigue`, veille bloquée par l'application (GEN-096) ; **toujours à faire**, prévue avec l'utilisateur quand il aura le temps.
- **Questions ouvertes** : [01-questions-ouvertes.md](01-questions-ouvertes.md) — Q25 (tableau WZYBUTA, points restants) ; Q27 : modèle réel des gros PAR (Betopper LPC010 ou LPC120 ?) — le show de référence patche provisoirement en LPC120 8 canaux ; **Q28 (P4)** : plage « sans strobe » du LPC008S et de la LCB803 (décodées « strobe » à 0 par le simulateur et par `luxia-headless jouer`) — **à trancher tôt en P5**, qui traite justement la sûreté du strobe.
- **Méthode d'essai avec l'utilisateur (rodée en P4, à reprendre)** : dérouler le guide **un exemple à la fois** dans la discussion, avec des consignes cliquables pas à pas (onglet, bouton, libellé exact) ; tracer chaque exemple dans les fiches (entrées « Utilisateur | Test » puis « Validation ») + matrice + commit/push avant de passer au suivant. **Avant toute compilation**, demander à l'utilisateur de fermer LuXia (verrou mono-instance, fichiers verrouillés) puis lui annoncer le numéro de version à vérifier dans la barre de titre. Les commandes de terminal se donnent en blocs `bash` séparés (bouton Run) avec chemins absolus.
- **Renommage en « LuXia » effectué le 2026-09-26** (avant P4, décision utilisateur) : solution `LuXia.sln`, namespaces `Luxia.*`, exécutable
  `LuXia.exe`, outil `luxia-headless`, dossiers de données (`%AppData%\LuXia`, `Documents\LuXia`, migration automatique au premier lancement
  depuis les anciens dossiers `DMX`), docs. Le sous-espace de noms `Luxia.Core.Dmx` (protocole) et le format `.dmxrec` restent inchangés :
  « DMX » y désigne le protocole DMX-512, pas l'application. **Carte du code (§3) et commandes (§4) mises à jour en conséquence.**
- **Menu Aide → À propos (2026-09-26)** : boîte de dialogue copiable (exécutable, dossiers, préférences, dernier projet en mémoire vs projet
  réellement ouvert) — très utile pour tout diagnostic à distance, à réutiliser/étendre si besoin en P4.
- **Verrou mono-instance (2026-09-26, `Program.cs`)** : deux `LuXia.exe` lancés en même temps se disputaient le port Arduino et pouvaient
  faire perdre le « dernier projet » des préférences (constaté en le reproduisant). Une seconde instance affiche maintenant un message et
  se ferme sans rien toucher.
- **Piège vécu (2026-09-26)** : `samples/Show de travail` est une copie fichier de `Show de référence` (`cp -r`) — son `projet.json` garde
  donc `"name": "Show de référence"` tant qu'on ne le renomme pas à la main après une régénération. Le nom affiché dans le titre ne dit pas
  quel dossier est réellement ouvert : se fier au champ « Dossier du projet » de **Aide → À propos**, pas au nom affiché.

## 2. Lire avant de coder (dans cet ordre)

1. [README](README.md), [02 – Principes](02-principes-et-architecture-fonctionnelle.md) (dont le **registre des décisions** §19, D1 à D28), [glossaire](glossaire.md).
2. [03 – Règles de développement](03-regles-de-developpement.md) (langue, style, structure, tests, Git).
3. Ce document, puis la matrice [31](31-matrice-exigences.md) et les [fiches d'exigences](exigences/README.md) concernées.
4. [40 – Feuille de route](40-feuille-de-route.md) §2 et §7 pour la phase visée, [41 – Show de référence](41-show-de-reference.md) §11.
5. Le cahier des charges du module de la phase (doc 10 à 23) **et ses « Notes de réalisation »** (doc 10 §9, 11 §7, 12 §10).
6. [50 – Format des données](50-format-des-donnees.md) si la phase touche aux fichiers.

## 3. Carte du code

| Projet | Rôle | Points d'entrée |
|---|---|---|
| `Luxia.Core` | Trame, plages de canaux, horloges, préférences, projet, instantanés | `DmxFrame`, `IClock`, `Preferences` (namespace `Luxia.Core.Dmx` pour le protocole) |
| `Luxia.Messaging` | Commandes (CMD-020, 022, 024) et bus d'événements | `Command`, `ICommandSink`, `EventBus` |
| `Luxia.Engine` | Moteur : chaîne de rendu sur un modèle compilé (D26), lectures de scènes, fusion des couches, surcharges, masters, blackout, test de sortie ; boucle 40 Hz | `RenderEngine` (`Tick`, `LoadShow`, `Snapshot`, `CommandLog`), `Playback`, `Model/ShowModel`, `TickLoop` |
| `Luxia.Output` | Routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec, `.dmxrec` | `OutputRouter`, `OutputDriver`, `ArduinoOutputDriver` |
| `Luxia.Persistence` | JSON versionné, migrations, préférences, projet | `VersionedJsonFile`, `ProjectStore`, `DataPaths` |
| `Luxia.Fixtures` | Modèles d'appareils, règles, validation, bibliothèque, imports, éditions | `FixtureType`, `FixtureRules`, `FixtureValidator`, `FixtureLibrary`, `FixtureEdits`, `DmxConversion` |
| `Luxia.Patch` | Installation (patch, univers, sélections), lieux, copie de bibliothèque du projet (GEN-053), décodage pour le simulateur | `Installation`, `PatchRules`, `AutoSelections`, `SelectionRules`, `Venue`, `ProjectFixtureLibrary`, `FixtureDecoder` |
| `Luxia.Scenes` | Scènes, palettes, couches (fichiers `scènes.json`, `palettes.json`, `couches.json`) ; compilation vers le moteur ; couleurs ; règles du programmeur ; rapports d'utilisation ; import | `ShowCompiler`, `ValueResolver`, `PatchContext`, `ColorConversion`, `ProgrammerRules`, `SceneUsage`, `SceneImport` |
| `Luxia.Hosting` | Assemblage (D20), journal, session de projet, recompilation du moteur, moteur d'aperçu, outils sans interface | `LuxiaRuntime` (`Engine`, `Preview`), `ProjectSession`, `ShowService`, `Tools/ProjectValidator`, `Tools/ScenarioRunner` |
| `Luxia.UI.Controls` | Fader, moniteur, barre de plages, barre d'univers, simulateur 2D, historique annuler / rétablir, dialogues | `Fader`, `OutputMonitor`, `UniverseBar`, `SimulatorCanvas`, `RangeBar`, `UndoHistory` |
| `Luxia.UI.Modules.*` | Un écran par projet : Console (+ faders d'appareil), Library, Outputs, Installation, Simulator, **Scenes** | `ConsoleViewModel`, `FixtureFadersViewModel`, `LibraryViewModel`, `InstallationViewModel`, `SimulatorViewModel`, `ScenesViewModel` (+ `ProgrammerViewModel`, `SceneEditorViewModel`, `PalettesViewModel`) |
| `Luxia.App` | Coquille Avalonia (navigation, menu Projet, menu Aide/À propos, barre d'état, verrou mono-instance) | `App`, `MainWindowViewModel`, `Program` |
| `tools/Luxia.Tools.Headless` | `luxia-headless` : ports, lancer, endurance, gigue, relire, projet, **valider**, **jouer**, **scenario** | `Commands`, `ProjectCommands` |
| `tools/Luxia.Tools.Captures` | Rendu hors écran de la fenêtre principale en PNG (Avalonia.Headless), sur une copie du projet : vérifier une mise en page sans lancer LuXia | `Program.cs` |
| `firmware/arduino-dmx` | Firmware Leonardo 1.0 (Enttec) | `arduino-dmx.ino` |

Tests : un projet par module + `Luxia.Integration.Tests` (rejeu du show de référence) + `Luxia.UI.Tests` (modèles de vue sans interface).

## 4. Commandes utiles

```bash
dotnet build Luxia.sln
dotnet test --solution Luxia.sln -- --filter-not-trait "Categorie=Materiel"
dotnet format Luxia.sln --verify-no-changes
python tools/matrice-exigences.py P0 P1 P2 P3 P4 P5
dotnet run --project src/Luxia.App -- "samples/Show de référence"
dotnet run --project tools/Luxia.Tools.Headless -- valider "samples/Show de référence"
dotnet run --project tools/Luxia.Tools.Headless -- jouer "samples/Show de référence" --scene "Chenillard 4 couleurs" --duree 3
dotnet run --project tools/Luxia.Tools.Captures -- "samples/Show de référence" "<dossier des images>"
```

- **Trames de référence P4** (`tests/assets/golden/P4-scenes.txt`) : après un changement **voulu et vérifié** du rendu des scènes,
  les régénérer avec `LUXIA_GOLDEN_UPDATE=1 dotnet test --project tests/Luxia.Integration.Tests`, puis relire le résumé de
  `luxia-headless jouer` avant de committer.
- **Captures d'écran** : l'outil ne lance pas l'application (pas de conflit avec le verrou mono-instance, GEN-115) ; relire les PNG
  avant de montrer un écran modifié à l'utilisateur (piège des largeurs, doc 03 §11).

- `LUXIA_DOSSIER_DONNEES=<dossier>` : toutes les données de l'application sous ce dossier (essais sans toucher aux vraies données).
- arduino-cli : `%LOCALAPPDATA%\Programs\arduino-cli\arduino-cli.exe` ; **tout téléversement se fait avec l'accord de l'utilisateur**.
- PyMuPDF est disponible pour rendre en images les notices PDF sans texte.
- **Essais manuels avec l'utilisateur (démos)** : ouvrir `samples/Show de travail` (copie de `samples/Show de référence`, ignorée par Git,
  régénérable avec `cp -r "samples/Show de référence" "samples/Show de travail"`), **jamais l'original**. Un instantané mémorisé pendant
  un essai écrit dans le projet ouvert : si c'est le show de référence, ça pollue l'échantillon livré et casse `ReferenceShowP1Tests`
  (vécu le 2026-09-25, corrigé en `8d6284c`). Après une régénération, penser à renommer `"name"` dans son `projet.json` en
  « Show de travail » (sinon le titre de l'appli affiche encore « Show de référence », vécu le 2026-09-26 — se fier au champ
  « Dossier du projet » de **Aide → À propos** en cas de doute, pas au nom affiché).

## 5. Démarrer une phase dans une nouvelle discussion

1. Vérifier que la phase précédente est **validée** (tableau §1) ; sinon, demander.
2. Créer la branche `pN/<nom>` depuis la dernière branche validée (ou `main` une fois fusionnée).
3. Lister les exigences de la phase (doc du module, colonne Phase) et les points « Reporté (PN) » de la matrice ; ouvrir une fiche par exigence (modèle dans `exigences/README.md`) au moment d'y travailler.
4. Développer par étapes vérifiables, un commit par étape (doc 03 §7), tests verts et sans avertissement.
5. Livrer : guide `docs/demos/PN-*.md`, ajouts au show de référence + `JOURNAL.md`, notes de réalisation dans le doc du module,
   fiches d'exigences à jour (statut + historique), matrice et index régénérés (`python tools/matrice-exigences.py …`), **ce document mis à jour**.
6. **Avant la validation définitive de la version** : demander à l'utilisateur s'il souhaite une **phase d'analyse ergonomique**
   menée dans la même discussion (connaissance complète de ce qui a été ajouté) ; le chantier d'ergonomie se poursuit ensuite dans
   une nouvelle discussion (Q32, doc 99).

Modèle de message pour ouvrir une discussion :

> Nous reprenons le projet LuXia. Lis `docs/32-passation.md` puis les documents qu'il indique. Objectif : phase PN – <nom>.
> Respecte les règles de `docs/03` ; toute divergence est signalée et reportée ; questions dans `docs/01`, idées dans `docs/99`.

## 6. Historique de la passation

| Date | Fin de | Remarque |
|---|---|---|
| 2026-09-25 | P2 | P0-P2 développées d'une traite, validation matérielle en attente. |
| 2026-09-25 | P2 | Fiches d'exigences créées pour les 110 exigences travaillées (historique reconstitué depuis les discussions et Git). |
| 2026-09-25 | P2 | P0, P1, P2 validées par l'utilisateur avec le matériel réel ; fusion dans `main`, étiquette `v1.001`. |
| 2026-09-26 | P3 | Installation + Simulateur développés d'une traite (nouveau projet `Dmx.Patch`, écrans Installation et Simulateur, mode appareils de la Console) ; reliquats P0-P2 traités (SORT-008, CONS-007/020 à 024/041/043/092, BIB-093/096/097/098/099/100) ; show de référence patché et placé dans un lieu ; 47 exigences P3 (34 Réalisé, 6 Partiel, 7 Non réalisé). Reste sur `p3/installation-simulateur`, en attente de la revue de l'utilisateur avant fusion et étiquette. |
| 2026-09-26 | P3 | Tour de test complet en direct avec l'utilisateur (4 PAR + 1 lyre), pas à pas dans la discussion. Corrigés au fil de l'eau : crash `InvalidCastException` (Univers affiché), Identifier (canaux couleur, puis fige au changement d'écran), largeurs de champs (deux passes), lieu actif non mis en évidence, simulateur trop sombre (corps toujours visible) et roue de couleur sans couleur définie décodée en noir, propriétés calculées dupliquées dans le JSON (`JsonIgnore`), fenêtre non maximisée au démarrage, version absente de la barre de titre. Solution repassée en `.sln` classique (D25, VS 2022 17.8 de l'utilisateur trop ancien pour .NET 10 — Claude compile seul désormais). 297 tests verts. **Validée par l'utilisateur, fusionnée dans `main`, étiquette `v1.002`.** |
| 2026-09-26 | Avant P4 | Renommage transverse « DMX » → « LuXia » (branche `chore/renommage-luxia`, fusionnée) : solution, 25 projets/namespaces, exécutable, outil, dossiers de données avec migration automatique, docs. GitHub câblé (`origin`). Icône de l'exécutable créée. Nettoyage : 0 avertissement de build. Ajouts pendant la vérification avec l'utilisateur : menu Aide → À propos (diagnostic copiable) et verrou mono-instance (`Program.cs`) après avoir constaté que deux `LuXia.exe` simultanés se disputaient le port Arduino et pouvaient vider le « dernier projet » des préférences. 300 tests verts. Tout validé par l'utilisateur avec le matériel réel, committé et poussé (`main` = `origin/main`). |
| 2026-09-26 | P4 | Moteur + Scènes développés sur `p4/moteur-scenes` : moteur sur modèle compilé (D26-D28), `Luxia.Scenes`, écran Scènes, aveugle et aperçu, Console en attributs, outils sans interface, contenu P4 du show de référence et trames de référence, fiches (90 créées), guide P4. 408 tests verts. Tests de temps réel rendus robustes à la charge. **En attente de la revue de l'utilisateur** avant fusion et `v1.003`. |
| 2026-09-26 | P4 | Guide P4 déroulé pas à pas avec l'utilisateur sur le matériel (exemples 1 à 12, tous conformes). Corrigés/ajoutés au fil de l'eau, chacun avec sa fiche : blanc chaud, vitesse en direct, enregistrement robuste (GEN-118), exceptions journalisées (GEN-117), Ctrl+Z/Y, bouton de mise à jour de palette, case Aveugle, numéro de compilation (GEN-119). Remarque utilisateur : ergonomie d'ensemble à reprendre plus tard (doc 99), non bloquante. **Validée, fusionnée dans `main`, étiquette `v1.003`.** Prochaine étape : P5. |
