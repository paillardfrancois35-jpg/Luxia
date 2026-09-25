# 32 – Passation entre discussions

> Point d'entrée pour reprendre le développement dans une **nouvelle discussion** sans relire tout l'historique.
> À tenir à jour à chaque fin de phase (section 1, 4 et 5). Dernière mise à jour : 2026-09-25, fin de P2.

## 1. Où en est-on

| Phase | État | Branche | Guide | Validation utilisateur |
|---|---|---|---|---|
| P0 – Fondations | Développée | `p0/fondations` | [demos/P0-fondations.md](demos/P0-fondations.md) | ⏳ essais matériel prévus |
| P1 – Console | Développée | `p1/console` | [demos/P1-console.md](demos/P1-console.md) | ⏳ |
| P2 – Bibliothèque | Développée | `p2/bibliotheque` | [demos/P2-bibliotheque.md](demos/P2-bibliotheque.md) | ⏳ |
| P3 – Installation + Simulateur | À faire | — | — | — |

- **Fiches d'exigences** : [exigences/](exigences/README.md) — une fiche par exigence travaillée, avec statut et **historique complet** (questions, décisions et leur pourquoi, écarts, commits, tests, validations). **Lire la fiche avant de toucher à une exigence.**
- **Statut exigence par exigence** : [31-matrice-exigences.md](31-matrice-exigences.md), générée depuis les fiches.
- **Reste à faire sur les phases développées** (hors validation matérielle) :
  - CONS-007 et la délimitation des appareils dans le moniteur (CONS-043) → P3 (patch).
  - SORT-008 (canaux maintenus pendant le test de sortie, Q23) → P3.
  - CONS-008 (surcharges soumises au blackout et à la sûreté) → P4 / P5 ; `TODO(P4, GEN-042)` dans `RenderEngine`.
  - GEN-104 : indicateurs blackout (P4) et mode auto (P10) affichés « — ».
  - Non réalisés (priorité S) : CONS-044, GEN-058, GEN-108, BIB-027 (partiel), BIB-084.
  - Mesure de gigue de **15 min** (D23) : `dmx-headless gigue`, veille bloquée par l'application (GEN-096) ; **prévue le 2026-09-26 au matin** avec l'utilisateur.
- **Questions ouvertes** : [01-questions-ouvertes.md](01-questions-ouvertes.md) — Q25 (tableau WZYBUTA : points restants à vérifier en direct par l'utilisateur).

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
| `Dmx.Hosting` | Assemblage (D20), journal, session de projet | `DmxRuntime`, `ProjectSession`, `TechnicalLog` |
| `Dmx.UI.Controls` | Fader, moniteur, barre de plages, historique annuler / rétablir, dialogues | `Fader`, `OutputMonitor`, `RangeBar`, `UndoHistory` |
| `Dmx.UI.Modules.*` | Un écran par projet : Console (+ faders d'appareil), Library, Outputs | `ConsoleViewModel`, `FixtureFadersViewModel`, `LibraryViewModel` |
| `Dmx.App` | Coquille Avalonia (navigation, menu Projet, barre d'état) | `App`, `MainWindowViewModel` |
| `tools/Dmx.Tools.Headless` | `dmx-headless` : ports, lancer, endurance, gigue, relire, projet | `Commands` |
| `firmware/arduino-dmx` | Firmware Leonardo 1.0 (Enttec) | `arduino-dmx.ino` |

Tests : un projet par module + `Dmx.Integration.Tests` (rejeu du show de référence) + `Dmx.UI.Tests` (modèles de vue sans interface).

## 4. Commandes utiles

```bash
dotnet build Dmx.slnx
dotnet test --solution Dmx.slnx -- --filter-not-trait "Categorie=Materiel"
dotnet format Dmx.slnx --verify-no-changes
python tools/matrice-exigences.py P0 P1 P2
dotnet run --project src/Dmx.App -- "samples/Show de référence"
```

- `DMX_DOSSIER_DONNEES=<dossier>` : toutes les données de l'application sous ce dossier (essais sans toucher aux vraies données).
- arduino-cli : `%LOCALAPPDATA%\Programs\arduino-cli\arduino-cli.exe` ; **tout téléversement se fait avec l'accord de l'utilisateur**.
- PyMuPDF est disponible pour rendre en images les notices PDF sans texte.

## 5. Démarrer une phase dans une nouvelle discussion

1. Vérifier que la phase précédente est **validée** (tableau §1) ; sinon, demander.
2. Créer la branche `pN/<nom>` depuis la dernière branche validée (ou `main` une fois fusionnée).
3. Lister les exigences de la phase (doc du module, colonne Phase) et les points « Reporté (PN) » de la matrice ; ouvrir une fiche par exigence (modèle dans `exigences/README.md`) au moment d'y travailler.
4. Développer par étapes vérifiables, un commit par étape (doc 03 §7), tests verts et sans avertissement.
5. Livrer : guide `docs/demos/PN-*.md`, ajouts au show de référence + `JOURNAL.md`, notes de réalisation dans le doc du module,
   fiches d'exigences à jour (statut + historique), matrice et index régénérés (`python tools/matrice-exigences.py …`), **ce document mis à jour**.

Modèle de message pour ouvrir une discussion :

> Nous reprenons le projet DMX. Lis `docs/32-passation.md` puis les documents qu'il indique. Objectif : phase PN – <nom>.
> Respecte les règles de `docs/03` ; toute divergence est signalée et reportée ; questions dans `docs/01`, idées dans `docs/99`.

## 6. Historique de la passation

| Date | Fin de | Remarque |
|---|---|---|
| 2026-09-25 | P2 | P0-P2 développées d'une traite, validation matérielle en attente. |
| 2026-09-25 | P2 | Fiches d'exigences créées pour les 110 exigences travaillées (historique reconstitué depuis les discussions et Git). |
