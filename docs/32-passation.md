# 32 – Passation entre discussions

> Point d'entrée pour reprendre le développement dans une **nouvelle discussion** sans relire tout l'historique.
> À tenir à jour à chaque fin de phase (section 1, 4 et 5). Dernière mise à jour : 2026-09-26, P3 validée et fusionnée.

## 1. Où en est-on

| Phase | État | Branche | Guide | Validation utilisateur |
|---|---|---|---|---|
| P0 – Fondations | Développée | `p0/fondations` (fusionnée dans `main`, `v1.001`) | [demos/P0-fondations.md](demos/P0-fondations.md) | ⏳ essais matériel prévus |
| P1 – Console | Développée | `p1/console` (fusionnée dans `main`, `v1.001`) | [demos/P1-console.md](demos/P1-console.md) | ⏳ |
| P2 – Bibliothèque | Développée | `p2/bibliotheque` (fusionnée dans `main`, `v1.001`) | [demos/P2-bibliotheque.md](demos/P2-bibliotheque.md) | ⏳ |
| P3 – Installation + Simulateur | Validée | `p3/installation-simulateur` (fusionnée dans `main`, `v1.002`) | [demos/P3-installation-simulateur.md](demos/P3-installation-simulateur.md) | ✅ 2026-09-26, matériel réel (4 PAR + 1 lyre) |

- **P0, P1, P2 validées par l'utilisateur le 2026-09-25** (avec le matériel réel) et fusionnées dans `main` (`v1.001`).
- **P3 validée par l'utilisateur le 2026-09-26**, tour de test complet en direct (4 PAR + 1 lyre) mené pas à pas dans la discussion : crash (Univers affiché), Identifier (couleur puis fige d'écran), largeurs de champs, sélections, lieux (création/activation/mise en évidence), simulateur (corps toujours visible, roue de couleur sans couleur définie), Sorties/SORT-008, version en barre de titre. Tous corrigés au fil de l'eau, 297 tests verts. Fusionnée dans `main`, étiquette `v1.002`.
- **Fiches d'exigences** : [exigences/](exigences/README.md) — une fiche par exigence travaillée, avec statut et **historique complet** (questions, décisions et leur pourquoi, écarts, commits, tests, validations). **Lire la fiche avant de toucher à une exigence.**
- **Statut exigence par exigence** : [31-matrice-exigences.md](31-matrice-exigences.md), générée depuis les fiches. Bilan P3 : 47 exigences, 34 Réalisé, 6 Partiel, 7 Non réalisé.
- **Reliquats P0-P2 traités en P3** : SORT-008, CONS-007/020 à 024/041/043/092 (patch et mode appareils), BIB-093/096/097/098/099/100. **BIB-094** (plage « Fondu » du LPC008S) reste ouverte : une réserve honnête a été ajoutée au libellé, mais la correction exacte attend une vérification en direct sur l'appareil. **CONS-091** reste correctement en P4 (non traité maintenant, cohérent avec le doc 40).
- **Reste à faire sur les phases développées** (hors validation matérielle) :
  - CONS-008 (surcharges soumises au blackout et à la sûreté) → P4 / P5 ; `TODO(P4, GEN-042)` dans `RenderEngine`.
  - CONS-091 (écart conservé au-delà des bornes en relatif, retour utilisateur du 25/09) → P4.
  - CONS-021 (pastille couleur, pad Pan/Tilt XY combinés) → reste canal par canal ; à revoir avec le programmeur (P4-P5).
  - INST-021 (options de montage : pas d'éditeur dans l'écran), INST-034 (sélection de cellules), INST-051 (glisser-déposer sur le plan) → partiels, voir leurs fiches.
  - SIM-007 (fenêtre détachable), SIM-008 (zones interdites/repères), SIM-010 (sélection reprise par le programmeur) → non réalisés, dépendent de P4/P5.
  - GEN-104 : indicateurs blackout (P4) et mode auto (P10) affichés « — ».
  - Non réalisés (priorité S) : CONS-044, GEN-058, GEN-108, BIB-027 (partiel), BIB-084, SIM-013, SORT-063, SORT-064.
  - **Mesure de gigue de 15 min (D23)** : `dmx-headless gigue`, veille bloquée par l'application (GEN-096) ; **toujours à faire**, prévue avec l'utilisateur quand il aura le temps.
- **Questions ouvertes** : [01-questions-ouvertes.md](01-questions-ouvertes.md) — Q25 (tableau WZYBUTA, points restants) ; **Q27 (nouvelle)** : modèle réel des gros PAR (Betopper LPC010 ou LPC120 ?) — le show de référence patche provisoirement en LPC120 8 canaux.
- **Renommage en « LuXia » effectué le 2026-09-26** (avant P4, décision utilisateur) : solution `LuXia.sln`, namespaces `Luxia.*`, exécutable
  `LuXia.exe`, outil `luxia-headless`, dossiers de données (`%AppData%\LuXia`, `Documents\LuXia`, migration automatique au premier lancement
  depuis les anciens dossiers `DMX`), docs. Le sous-espace de noms `Luxia.Core.Dmx` (protocole) et le format `.dmxrec` restent inchangés :
  « DMX » y désigne le protocole DMX-512, pas l'application.

## 2. Lire avant de coder (dans cet ordre)

1. [README](README.md), [02 – Principes](02-principes-et-architecture-fonctionnelle.md) (dont le **registre des décisions** §19, D1 à D23), [glossaire](glossaire.md).
2. [03 – Règles de développement](03-regles-de-developpement.md) (langue, style, structure, tests, Git).
3. Ce document, puis la matrice [31](31-matrice-exigences.md) et les [fiches d'exigences](exigences/README.md) concernées.
4. [40 – Feuille de route](40-feuille-de-route.md) §2 et §7 pour la phase visée, [41 – Show de référence](41-show-de-reference.md) §11.
5. Le cahier des charges du module de la phase (doc 10 à 23) **et ses « Notes de réalisation »** (doc 10 §9, 11 §7, 12 §10).
6. [50 – Format des données](50-format-des-donnees.md) si la phase touche aux fichiers.

## 3. Carte du code

| Projet | Rôle | Points d'entrée |
|---|---|---|
| `Dmx.Core` | Trame, plages de canaux, horloges, préférences, projet, instantanés | `DmxFrame`, `IClock`, `Preferences` |
| `Dmx.Messaging` | Commandes (CMD-020, 022, 024) et bus d'événements | `Command`, `ICommandSink`, `EventBus` |
| `Dmx.Engine` | Moteur (tick, surcharges, test de sortie), boucle 40 Hz | `RenderEngine.Tick`, `TickLoop` |
| `Dmx.Output` | Routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec, `.dmxrec` | `OutputRouter`, `OutputDriver`, `ArduinoOutputDriver` |
| `Dmx.Persistence` | JSON versionné, migrations, préférences, projet | `VersionedJsonFile`, `ProjectStore`, `DataPaths` |
| `Dmx.Fixtures` | Modèles d'appareils, règles, validation, bibliothèque, imports, éditions | `FixtureType`, `FixtureRules`, `FixtureValidator`, `FixtureLibrary`, `FixtureEdits`, `DmxConversion` |
| `Dmx.Patch` | Installation (patch, univers, sélections), lieux, copie de bibliothèque du projet (GEN-053), décodage pour le simulateur | `Installation`, `PatchRules`, `AutoSelections`, `SelectionRules`, `Venue`, `ProjectFixtureLibrary`, `FixtureDecoder` |
| `Dmx.Hosting` | Assemblage (D20), journal, session de projet | `DmxRuntime`, `ProjectSession`, `TechnicalLog` |
| `Dmx.UI.Controls` | Fader, moniteur, barre de plages, barre d'univers, simulateur 2D, historique annuler / rétablir, dialogues | `Fader`, `OutputMonitor`, `UniverseBar`, `SimulatorCanvas`, `RangeBar`, `UndoHistory` |
| `Dmx.UI.Modules.*` | Un écran par projet : Console (+ faders d'appareil), Library, Outputs, Installation, Simulator | `ConsoleViewModel`, `FixtureFadersViewModel`, `LibraryViewModel`, `InstallationViewModel`, `SimulatorViewModel` |
| `Dmx.App` | Coquille Avalonia (navigation, menu Projet, barre d'état) | `App`, `MainWindowViewModel` |
| `tools/Dmx.Tools.Headless` | `dmx-headless` : ports, lancer, endurance, gigue, relire, projet | `Commands` |
| `firmware/arduino-dmx` | Firmware Leonardo 1.0 (Enttec) | `arduino-dmx.ino` |

Tests : un projet par module + `Dmx.Integration.Tests` (rejeu du show de référence) + `Dmx.UI.Tests` (modèles de vue sans interface).

## 4. Commandes utiles

```bash
dotnet build Dmx.sln
dotnet test --solution Dmx.sln -- --filter-not-trait "Categorie=Materiel"
dotnet format Dmx.sln --verify-no-changes
python tools/matrice-exigences.py P0 P1 P2 P3
dotnet run --project src/Dmx.App -- "samples/Show de référence"
```

- `LUXIA_DOSSIER_DONNEES=<dossier>` : toutes les données de l'application sous ce dossier (essais sans toucher aux vraies données).
- arduino-cli : `%LOCALAPPDATA%\Programs\arduino-cli\arduino-cli.exe` ; **tout téléversement se fait avec l'accord de l'utilisateur**.
- PyMuPDF est disponible pour rendre en images les notices PDF sans texte.
- **Essais manuels avec l'utilisateur (démos)** : ouvrir `samples/Show de travail` (copie de `samples/Show de référence`, ignorée par Git,
  régénérable avec `cp -r "samples/Show de référence" "samples/Show de travail"`), **jamais l'original**. Un instantané mémorisé pendant
  un essai écrit dans le projet ouvert : si c'est le show de référence, ça pollue l'échantillon livré et casse `ReferenceShowP1Tests`
  (vécu le 2026-09-25, corrigé en `8d6284c`).

## 5. Démarrer une phase dans une nouvelle discussion

1. Vérifier que la phase précédente est **validée** (tableau §1) ; sinon, demander.
2. Créer la branche `pN/<nom>` depuis la dernière branche validée (ou `main` une fois fusionnée).
3. Lister les exigences de la phase (doc du module, colonne Phase) et les points « Reporté (PN) » de la matrice ; ouvrir une fiche par exigence (modèle dans `exigences/README.md`) au moment d'y travailler.
4. Développer par étapes vérifiables, un commit par étape (doc 03 §7), tests verts et sans avertissement.
5. Livrer : guide `docs/demos/PN-*.md`, ajouts au show de référence + `JOURNAL.md`, notes de réalisation dans le doc du module,
   fiches d'exigences à jour (statut + historique), matrice et index régénérés (`python tools/matrice-exigences.py …`), **ce document mis à jour**.

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
