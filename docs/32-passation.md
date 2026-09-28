# 32 – Passation entre discussions

> Point d'entrée pour reprendre le développement dans une **nouvelle discussion** sans relire tout l'historique.
> À tenir à jour à chaque fin de phase (section 1, 4 et 5). Dernière mise à jour : 2026-09-28, **chantier ergonomique validé**
> (`main`, étiquette **`v1.005`**) ; **P6 – Effets développée** sur `p6/effets` (version de développement **1.006**,
> étiquettes `v1.006.NNN`), en autonomie par délégation de l'utilisateur (Q36). **En attente de l'essai de l'utilisateur** :
> guide [demos/P6-effets.md](demos/P6-effets.md) ; au matériel, l'effet multi-têtes doit être réglé en **64 canaux à
> l'adresse 181** (seul changement d'adresse).

## 1. Où en est-on

| Phase | État | Branche | Guide | Validation utilisateur |
|---|---|---|---|---|
| P0 – Fondations | Développée | `p0/fondations` (fusionnée dans `main`, `v1.001`) | [demos/P0-fondations.md](demos/P0-fondations.md) | ⏳ essais matériel prévus |
| P1 – Console | Développée | `p1/console` (fusionnée dans `main`, `v1.001`) | [demos/P1-console.md](demos/P1-console.md) | ⏳ |
| P2 – Bibliothèque | Développée | `p2/bibliotheque` (fusionnée dans `main`, `v1.001`) | [demos/P2-bibliotheque.md](demos/P2-bibliotheque.md) | ⏳ |
| P3 – Installation + Simulateur | Validée | `p3/installation-simulateur` (fusionnée dans `main`, `v1.002`) | [demos/P3-installation-simulateur.md](demos/P3-installation-simulateur.md) | ✅ 2026-09-26, matériel réel (4 PAR + 1 lyre) |
| P4 – Moteur + Scènes | Validée | `p4/moteur-scenes` (fusionnée dans `main`, `v1.003`) | [demos/P4-moteur-scenes.md](demos/P4-moteur-scenes.md) | ✅ 2026-09-26, matériel réel (4 PAR + lyre 1 + barre 1), exemples 1 à 12 |
| P5 – Couches, Palettes, Live, MIDI | Validée | `p5/couches-palettes-live` (fusionnée dans `main`, `v1.004`) | [demos/P5-couches-palettes-live.md](demos/P5-couches-palettes-live.md) | ✅ 2026-09-27, matériel réel (4 PAR, lyre 1, UV 1, barre 1, APC mini MK2 et MK1), exemples 1 à 13 |
| Chantier ergonomique – écran Contrôle | Validé | `ergo/analyse` (fusionnée dans `main`, `v1.005`) | [demos/ERG-controle.md](demos/ERG-controle.md) | ✅ 2026-09-28, matériel réel, guide §0 à §7 (1.005.192 → 1.005.237) |
| P6 – Effets | Développée | `p6/effets` (depuis `main` `v1.005` + `e1dfa56`) | [demos/P6-effets.md](demos/P6-effets.md) | ⏳ essai de l'utilisateur (exemples 1 à 13) |

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
- **P5 développée le 2026-09-26/27** (branche `p5/couches-palettes-live`, version de développement **1.004**) : questions Q28 à Q32
  tranchées avec l'utilisateur au démarrage (Q28 plages sans strobe, Q27 gros PAR = LPC120 8CH, Q29 fumée non branchée, Q30 zones,
  Q31 MK2 et winmm, **Q32 : ordre de la phase et report des exigences qui ajoutent des panneaux**). Livré, dans l'ordre :
  **sûreté** (strobe, fumée, zones interdites ; D29, `sûreté.json`, zones dans `lieux.json`), **couches** (flash, figer, fumée
  manuelle, tout arrêter, scène de repos, éditeur « Couches… », familles COU-008), **positions par lieu** et saisie des zones en
  visant, **écran Live** (nouveau module, premier écran), **APC mini MK1/MK2** (nouveau projet `Luxia.Midi`, D30, winmm, profils
  en données, `midi.json`, `luxia-headless midi`), **fiabilité** (versions du projet, reprise après arrêt brutal, fondu au noir,
  CPU, D31), « Plein feu » par défaut, contenu P5 du show de référence et trames de référence `P5-scenes.txt`, doc 50 + schémas.
  **Bilan P5 : 75 exigences** : 43 Réalisé, 14 Réalisé à valider sur matériel, 6 Partiel (GEN-071, INST-072, LIVE-001, LIVE-004,
  LIVE-040 : attendent P7/P8/P10 ou l'assistant ; MOT-082 : option d'intensité pendant la traversée), 11 reportées au chantier
  d'ergonomie, PAL-010 en P6. Tests : 490 environ, tous verts. **Écarts** : numéros MIDI absents des notices (pris des protocoles
  AKAI, à confirmer) ; figer pris avant le blackout (MOT-073) ; « sauvegarde auto » = versions (D31).
- **Remarque de l'utilisateur à la validation de P4 (à garder en tête pour toute la suite)** : l'ergonomie est jugée « un beau bazar » —
  tout se mélange, trop de place, trop de défilement ; formats d'affichage et formes à revoir. **Non bloquant** tant que le fonctionnel
  tient : l'utilisateur compte sur l'IA pour **construire les shows** (GEN-130 à 134) et lui expliquer ce qu'il veut. Chantier
  d'ergonomie conséquent noté en [doc 99](99-idees.md), à planifier plus tard (analyse approfondie de tous les écrans). En attendant :
  ne pas ajouter de désordre (panneaux compacts, repliables si possible), et soigner tout ce qui aide l'IA à produire des shows
  (formats documentés, `valider`, `jouer`, `scenario`, import de scènes).
- **Fiches d'exigences** : [exigences/](exigences/README.md) — une fiche par exigence travaillée, avec statut et **historique complet** (questions, décisions et leur pourquoi, écarts, commits, tests, validations). **Lire la fiche avant de toucher à une exigence.**
- **Statut exigence par exigence** : [31-matrice-exigences.md](31-matrice-exigences.md), générée depuis les fiches. Bilan P3 : 47 exigences, 34 Réalisé, 6 Partiel, 7 Non réalisé.
- **Reliquats P0-P2 traités en P3** : SORT-008, CONS-007/020 à 024/041/043/092 (patch et mode appareils), BIB-093/096/097/098/099/100. **BIB-094** (plage « Fondu » du LPC008S) reste ouverte : une réserve honnête a été ajoutée au libellé, mais la correction exacte attend une vérification en direct sur l'appareil. **CONS-091** reste correctement en P4 (non traité maintenant, cohérent avec le doc 40).
- **À faire pour valider P5** : dérouler le guide P5 **un exemple à la fois** avec l'utilisateur (régénérer d'abord le show de
  travail) ; **confirmer les numéros MIDI** sur l'APC (exemple 10, `luxia-headless midi --leds`) ; relever CPU, démarrage,
  latence. Puis, **avant la validation définitive** (fusion dans `main`, étiquette `v1.004`, `LuxiaDevVersion` vidé et
  `Version` = 1.004), **demander à l'utilisateur s'il veut la phase d'analyse ergonomique** dans la même discussion (§5.6).
- **Reste à faire sur les phases développées** (hors validation matérielle) :
  - P4 partiels restants : GEN-023 (tempo fixe 120 BPM → P7), GEN-113 (enregistrement des trames depuis l'appli), PAL-007
    (grilles de palettes), SCN-030 (sélection au plan), SCN-031 (roue chromatique, pad Pan/Tilt), MOT-054 (couleurs par teinte → P6).
    GEN-040, GEN-042, GEN-112 et COU-006 ont été terminées en P5.
  - Chantier d'ergonomie (Q32, doc 99) : LIVE-006/007/011/041, INST-070/071, MIDI-008/009, GEN-057/074, CONS-061 reportées.
  - MOT-103 : `scenario` non rejoué à la main (tests automatiques seulement).
  - Lyre 2 : positions des palettes proposées, à calibrer quand elle sera raccordée.
  - CONS-021 (pastille couleur, pad Pan/Tilt XY combinés) → reste canal par canal ; à revoir avec le programmeur (P5).
  - INST-021 (options de montage : pas d'éditeur dans l'écran), INST-034 (sélection de cellules), INST-051 (glisser-déposer sur le plan) → partiels, voir leurs fiches.
  - SIM-007 (fenêtre détachable), SIM-008 (zones interdites/repères), SIM-010 (sélection reprise par le programmeur) → non réalisés, dépendent de P4/P5.
  - GEN-104 : indicateurs blackout (P4) et mode auto (P10) affichés « — ».
  - Non réalisés (priorité S) : CONS-044, GEN-058, GEN-108, BIB-027 (partiel), BIB-084, SIM-013, SORT-063, SORT-064.
  - **Mesure de gigue de 15 min (D23)** : `luxia-headless gigue`, veille bloquée par l'application (GEN-096) ; **toujours à faire**, prévue avec l'utilisateur quand il aura le temps.
- **Questions ouvertes** : [01-questions-ouvertes.md](01-questions-ouvertes.md) — seule **Q25** reste 🟡 (effet WZYBUTA : réglage
  20/64 canaux et vitesse du canal 2 ; l'utilisateur laisse l'IA choisir, préférence 64CH, qui oblige à réadresser UV et fumée :
  décision au rebranchement de l'effet). Q27 à Q32 tranchées le 2026-09-26.
- **Méthode d'essai avec l'utilisateur (rodée en P4, à reprendre)** : dérouler le guide **un exemple à la fois** dans la discussion, avec des consignes cliquables pas à pas (onglet, bouton, libellé exact) ; tracer chaque exemple dans les fiches (entrées « Utilisateur | Test » puis « Validation ») + matrice + commit/push avant de passer au suivant. **Pendant les essais**, avant toute compilation, demander à l'utilisateur de fermer LuXia (verrou mono-instance, fichiers verrouillés) puis lui annoncer le numéro de version à vérifier dans la barre de titre. **Pendant le développement** (décision utilisateur du 2026-09-26, début P5), Claude compile en autonomie et peut arrêter `LuXia.exe` au besoin ; les essais de l'utilisateur ont lieu à la fin, une fois tout le développement de la phase réalisé. Les commandes de terminal se donnent en blocs `bash` séparés (bouton Run) avec chemins absolus.
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

- **P5 validée par l'utilisateur le 2026-09-27** : guide déroulé pas à pas au matériel (exemples 1 à 13 ; l'exemple 13, sans interface,
  déroulé par Claude à la demande de l'utilisateur ; étape 3 de l'exemple 6 — définir une zone interdite — reportée à la refonte
  ergonomique). Bilan : 79 exigences P5, **37 Validé**, 26 Réalisé (couverts par les tests), 4 Partiel, 11 reportées au chantier
  ergonomie, 1 en P6. Faits marquants, chacun avec sa fiche :
  - **course écran / moteur** corrigée trois fois (bascule lancer/arrêter tranchée par le moteur `StopIfPlaying` ; `EngineEcho` pour
    les masters ; reprise douce MIDI qui retient les dernières valeurs envoyées) — à garder en tête pour tout nouvel écran ;
  - **UV BeamZ BUV463 : 8e canal « lissage » non documenté** (cause de l'« allumage lent »), UV 2 déplacé en 169 ; gros PAR = générique
    **WT05** 7 canaux (pas des LPC120) ; écoute de la ligne possible avec le DVC4 Daslight (`docs/Equipements/DasLight/ecoute-ligne-dmx-dvc4.md`) ;
  - **journal de l'enregistrement** `.journal.txt` (SORT-066 : clics IHM, commandes moteur, canaux DMX, touches) ;
  - repos de fumée proportionnel (GEN-084 précisé), décomptes de sûreté, fenêtre de démarrage (GEN-065), ↑ ↓ = master de couche,
    Problèmes du projet = règles de `valider` ; test instable stabilisé (règle docs/03 §11). 505 tests verts.
  - **Prochain chantier : analyse ergonomique** (demande de l'utilisateur) : charte d'interaction et catalogue de composants communs,
    contrôles 2D sur mesure (plan Pan/Tilt, roue de couleurs, effets), modules détachables / réagençables (ancrage type Dock),
    place réservée à la 3D (prototype isolé), captures Daslight 4/5 comme sources de principes. Idées déjà notées dans le doc 99.
- **Chantier ergonomique — analyse validée le 2026-09-27** (branche `ergo/analyse`, poussée ; `main` a reçu la mise à jour
  documentaire) : [60 – Ergonomie](60-ergonomie.md). Principes retenus de Daslight 4 / 5, constat des 8 écrans, **charte
  d'interaction** (modes ÉDITION / AVEUGLE / LIVE, enregistrement immédiat + annuler, états visibles, clic droit partout,
  couleurs à sens fixe, verrou soirée), **déclencheurs** (une entrée clavier / MIDI / DMX → plusieurs actions ; **looks**
  réutilisés par le mode automatique), **composants 2D communs** (fader, grille Pan/Tilt, sélecteur de couleur, molette,
  bouton de scène, plan des appareils, bande d'étapes, galerie), **modules ancrables et détachables** (*Dock*, dispositions
  Contrôle / Installation / Spectacle / deux écrans). **Toutes les décisions E1-E8 et F1-F10 acceptées** (Q34). 3D écartée.
  - **Étape suivante (nouvelle discussion)** : §7.2 **prototype technique** — ancrage *Dock* sous Avalonia 12, grille Pan/Tilt,
    sélecteur de couleur, galerie de composants, enregistrement de la disposition ; puis §7.3 **maquettes** en images de la
    disposition « Contrôle » (scènes + réglages des appareils + zones), à faire valider **avant** tout développement.
  - Finalité rappelée par l'utilisateur : **mode automatique** (titre → style → show ; looks en surcouche quand la musique
    se calme ou repart) ; LuXia = porte d'entrée vers la création de shows **par l'IA** ; l'écran « Spectacle / pilote
    automatique » est la cible, l'édition fine doit surtout être **compréhensible** (aide « ? », vocabulaire unique).
  - Les exigences d'ergonomie à créer (fiches) le seront au fil du chantier ; les 11 exigences « Reporté (chantier
    ergonomie) » de P5 y sont rattachées (LIVE-006/007/011/041, MIDI-008/009, GEN-057/074, INST-070/071, CONS-061).
- **Chantier ergonomique — prototype et maquettes (2026-09-27, branche `ergo/analyse`, `v1.005.001` puis `v1.005.002`)** :
  reliquats du renommage DMX → LuXia corrigés (`.editorconfig` visait encore `src/Dmx.UI.**`, `Dmx.sln` au doc 03,
  `dmx-headless` au `JOURNAL.md`). Exigences **ERG-001 à ERG-007** (doc 60 §9, famille et phase « ERG » dans la matrice :
  `python tools/matrice-exigences.py P0 P1 P2 P3 P4 P5 ERG`).
  - **Prototype** `tools/Luxia.Tools.Prototype` (`LuXia-Prototype.exe`, **séparé de LuXia**, décision utilisateur) : Dock
    12.1.0.6 (MIT), dispositions Contrôle / Spectacle, panneaux détachables, menu Panneaux, enregistrement automatique
    (`%AppData%\LuXia\prototype`), galerie, mesures, aide « ? ». **Composants durables** dans `Luxia.UI.Controls` :
    `PanTiltGrid` (+ `PanTiltGeometry`), `ColorPicker` (+ `ColorPickerLayout`), `LightColor` (pas `HsvColor` : homonyme
    d'Avalonia). Écarts : Dock perd le groupe d'origine d'un panneau fermé après relecture (contourné, test) ; menus de
    Dock en anglais (à franciser au développement).
  - **Maquettes** « Contrôle » rendues par Avalonia (décision utilisateur) : `docs/maquettes/` (LIVE, ÉDITION, AVEUGLE,
    zones) + galerie ; `LuXia-Prototype --maquettes <dossier>` ; bouton **Maquettes ▾** du prototype.
  - **À faire** : dérouler le guide [demos/ERG-prototype-et-maquettes.md](demos/ERG-prototype-et-maquettes.md) avec
    l'utilisateur (un point à la fois), relever les mesures (ERG-006), faire trancher **Q35** ; puis développement par
    lots (charte et composants → Contrôle → Live / Spectacle → Installation / Bibliothèque → Affectations).
- **Nuit du 2026-09-27 au 28 — développement par délégation** (« prends les décisions qui te semblent les plus
  pertinentes, présente-moi tes choix à la fin » ; « va le plus loin que tu peux, pousse les tests au maximum, réalise
  des configurations d'exemple ») :
  - **Écran « Contrôle »** (nouveau projet `Luxia.UI.Modules.Control`, en tête de la navigation ; Live et Scènes
    restent, C5) : `ControlSession` (modes **LIVE / ÉDITION / AVEUGLE**, sélection partagée, un geste = une
    annulation, écrit 0,5 s après le dernier mouvement), panneaux ancrables Dock (**Colonnes**, **Propriétés**,
    **Plan des appareils**, **Réglages des appareils**, **Journal**, **Looks**, **Pilote automatique**), dispositions
    **Contrôle** et **Spectacle** enregistrées sur le poste, menus Dock en français, **verrou soirée**, **F1-F12 =
    looks**.
  - **Moteur** : **zone permise** (`allowed` dans `lieux.json`, F7) ; défaut corrigé (surcharge span choisie pour un
    tableau, doc 03 §11).
  - **Données** : `looks.json` (ERG-023), `uiScale` des préférences (taille 100 / 125 / 150 %), 8e couche par défaut
    **« Libre »** (ERG-008, fader 8 de l'APC).
  - **Identité visuelle** (dossier de l'utilisateur → `docs/identite`) : icône, logo au démarrage, dans « À propos »,
    en tête de la navigation, README.
  - **Démo** : `tools/generer-demo-controle.py` → `samples/Démo Contrôle` (ignoré par Git) ; captures réelles dans
    `docs/maquettes/captures`.
  - Exigences **ERG-008 à ERG-024** (fiches), Q35 close par délégation. Tests : 597, tous verts ; 0 avertissement de
    compilation ; 0 avertissement au journal technique au démarrage réel.
  - **Reste du chantier** (doc 60 §7.4) : affectation MIDI / clavier par surcouche (E6, MIDI-008), déclencheurs MIDI
    des looks, molette, mixeur live, retrait des écrans Live et Scènes après validation, Installation / Bibliothèque
    en panneaux.
- **Essai de l'écran Contrôle par l'utilisateur (2026-09-28, au matériel, 1.005.192 → 1.005.226)** : guide
  `docs/demos/ERG-controle.md` §0 à §7 déroulé pas à pas, **tout validé**. Corrigé au fil de l'eau, chaque fois avec sa
  fiche : ◀ ▶ grisés pour une scène à une étape, stop net (le fondu vient de la scène), barre d'avancement discrète,
  barre LED du plan, avertissement « intensité à 0 » et légende des pastilles, menu **Panneaux** (s'ouvrait vide),
  ✎ qui rouvre Propriétés, panneau détaché remis à sa place (groupe recréé), double-clic d'agrandissement, zones
  (plus petite prise, liste, renommage), looks sans Grand Master, **■ Stop / ■ Tout stopper**. Puis analyse de fin
  d'essai : **scènes resserrées** (ERG-025), **marges** (ERG-027 : menu et Grand Master sur une ligne, lignes du « ? »
  réutilisées, Journal serré). Choix C12 à C14 (doc 60 §11). Compteur de compilation par version (repart de 1 après
  validation). Deux tests de temps (moteur, routeur de sorties) échouent parfois sous la charge de la série complète
  et passent seuls : à fiabiliser.
- **P6 développée le 2026-09-28** (branche `p6/effets`, version **1.006**) en autonomie : l'utilisateur, amateur, a délégué
  les choix (« je te laisse avancer avec tes décisions […] on testera ensemble une fois le travail réalisé ») : Q36 (cadrage,
  retenu tel que proposé) et Q25 close (WZYBUTA en **64 canaux à l'adresse 181**, après la fumée : aucun autre appareil ne
  bouge). Décisions **D32** (effets rangés dans l'étape, compilés en formes et tables ; bibliothèque copiée) et **D33**
  (aperçu par CMD-017). Livré, dans l'ordre des commits :
  - **moteur** : `EngineEffect` / `EffectChannel` sur les étapes, `EffectShapes` (formes pures, sans allocation), relatif /
    absolu (relatif sur la valeur **sous-jacente** si l'étape ne règle pas l'attribut), entrée / sortie avec le fondu,
    addition (EFF-009), aléatoire reproductible, **fondu par la teinte** (MOT-054), commande **CMD-017 `MontrerÉtape`** ;
  - **données et compilation** : `SceneStep.Effects` (`SceneEffect`, **plusieurs cibles**, option par cellule),
    `EffectCompiler` (phases linéaire / miroir / groupes / aléatoire, degrés → course du modèle, couleurs → tables par
    canal), thèmes `PaletteKind.Theme` (6 livrés, ajoutés à un projet qui n'en a aucun), `effets.json` (19 modèles),
    `valider` (effets, bibliothèque), schémas JSON ;
  - **écran Contrôle** : panneau **Effets** (ERG-029 : bibliothèque, effets de l'étape, dessin animé `EffectPreview`,
    **molettes** `Dial` ERG-028), aperçu en direct en ÉDITION / AVEUGLE (EFF-006), « Enregistrer comme modèle »,
    « Enregistrer ces couleurs comme thème », case « Fondu par la teinte », **assistants** de génération d'étapes (SCN-014) ;
    ordre des membres = plan de gauche à droite (colonnes d'un mètre), puis ordre du patch ;
  - **Installation** : sélections « En cellules » / « Par appareil » (INST-034) ;
  - **contenu** : 10 scènes « Phase P6 » (dont le piège du grand cercle), trames `P6-scenes.txt` ; trames P4 / P5
    régénérées (scènes visant tout le parc : l'effet multi-têtes a changé de canaux) ; guide, docs 15, 16, 50, 41, 13, 17,
    02, glossaire, doc 99 (4 idées).
  - Bilan : **15 exigences P6 Réalisé** (+ PAL-010, INST-034, MOT-054 reportées et réalisées, CMD-017). Tests : 675,
    tous verts ; 0 avertissement.
  - **Essai en cours (2026-09-28)** : exemples 1 à 3 conformes (vague et miroir ramenés de 10 à 100 % à la demande de l'utilisateur) ; **exemple 4 (WZYBUTA 64 canaux, adresse 181, canal 182 / Q25) en attente** : appareil pas encore installé, à reprendre dès qu'il l'est.
  - **À faire pour valider P6** : dérouler le guide P6 **un exemple à la fois** avec l'utilisateur (régénérer d'abord le show
    de travail ; faire régler l'effet multi-têtes en 64 canaux, adresse 181 ; vérifier la vitesse du canal 2, Q25) ; puis
    proposer l'analyse ergonomique de fin de phase (§5.6) avant la fusion dans `main` et l'étiquette `v1.006`.

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
| `Luxia.Engine` | Moteur : chaîne de rendu sur un modèle compilé (D26), lectures de scènes, **effets**, fusion des couches, surcharges, masters, blackout, étape montrée (CMD-017), test de sortie ; boucle 40 Hz | `RenderEngine` (`Tick`, `LoadShow`, `Snapshot`, `CommandLog`), `Playback`, `EffectShapes`, `Model/ShowModel`, `Model/EngineEffect`, `TickLoop` |
| `Luxia.Output` | Routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec, `.dmxrec` | `OutputRouter`, `OutputDriver`, `ArduinoOutputDriver` |
| `Luxia.Persistence` | JSON versionné, migrations, préférences, projet | `VersionedJsonFile`, `ProjectStore`, `DataPaths` |
| `Luxia.Fixtures` | Modèles d'appareils, règles, validation, bibliothèque, imports, éditions | `FixtureType`, `FixtureRules`, `FixtureValidator`, `FixtureLibrary`, `FixtureEdits`, `DmxConversion` |
| `Luxia.Patch` | Installation (patch, univers, sélections), lieux, copie de bibliothèque du projet (GEN-053), décodage pour le simulateur | `Installation`, `PatchRules`, `AutoSelections`, `SelectionRules`, `Venue`, `ProjectFixtureLibrary`, `FixtureDecoder` |
| `Luxia.Scenes` | Scènes (effets compris), palettes (thèmes compris), couches, bibliothèque d'effets (fichiers `scènes.json`, `palettes.json`, `couches.json`, `effets.json`) ; compilation vers le moteur ; couleurs ; règles du programmeur ; assistants ; rapports d'utilisation ; import | `ShowCompiler`, `EffectCompiler`, `ValueResolver`, `PatchContext`, `ColorConversion`, `ProgrammerRules`, `EffectRules`, `SceneWizards`, `DefaultEffects`, `SceneUsage`, `SceneImport` |
| `Luxia.Midi` | Contrôleurs APC mini (D30) : profils en données, traduction en commandes, LED, reprise douce, ports `winmm`, branchement à chaud | `MidiService`, `MidiController`, `ControllerProfiles`, `WinMmMidiPorts`, `MidiSettings` (`midi.json`) |
| `Luxia.Hosting` | Assemblage (D20), journal, session de projet, recompilation du moteur, moteur d'aperçu, outils sans interface, versions du projet, reprise, MIDI | `LuxiaRuntime` (`Engine`, `Preview`, `Midi`, `PendingResume`), `ProjectSession`, `ShowService`, `ProjectVersions`, `Tools/ProjectValidator`, `Tools/ScenarioRunner` |
| `Luxia.UI.Controls` | Fader, **molette**, dessin d'effet, moniteur, barre de plages, barre d'univers, simulateur 2D, grille Pan/Tilt, sélecteur de couleur, historique annuler / rétablir, dialogues | `Fader`, `Dial`, `EffectPreview`, `OutputMonitor`, `UniverseBar`, `SimulatorCanvas`, `RangeBar`, `UndoHistory` |
| `Luxia.UI.Modules.*` | Un écran par projet : **Live** (premier écran), Console (+ faders d'appareil), Library, Outputs, Installation, Simulator, Scenes (+ fenêtres Couches…, Zones interdites…) | `LiveViewModel`, `ConsoleViewModel`, `FixtureFadersViewModel`, `LibraryViewModel`, `InstallationViewModel`, `SimulatorViewModel`, `ScenesViewModel` (+ `ProgrammerViewModel`, `SceneEditorViewModel`, `PalettesViewModel`, `LayersEditorViewModel`, `ZonesEditorViewModel`) |
| `Luxia.UI.Modules.Control` | Écran **Contrôle** (doc 60) : session d'édition LIVE / ÉDITION / AVEUGLE, panneaux ancrables Dock (Colonnes, Propriétés, Plan, Réglages, **Effets**, Journal, Looks, Pilote), dispositions Contrôle / Spectacle, verrou soirée | `ControlSession`, `ControlViewModel`, `*PanelViewModel` (dont `EffectsPanelViewModel`), `Docking/ControlDockFactory`, `Docking/ControlLayoutStore`, `Views/ControlView` |
| `Luxia.App` | Coquille Avalonia (navigation, menu Projet, menu Aide/À propos, barre d'état, verrou mono-instance) | `App`, `MainWindowViewModel`, `Program` |
| `tools/Luxia.Tools.Headless` | `luxia-headless` : ports, **midi**, lancer, endurance, gigue, relire, projet, **valider**, **jouer**, **scenario** | `Commands`, `ProjectCommands` |
| `tools/Luxia.Tools.Prototype` | Prototype ergonomique **séparé de LuXia** (`LuXia-Prototype.exe`) : ancrage Dock, dispositions, galerie, mesures, maquettes « Contrôle » ; captures sans écran | `ShellViewModel`, `Docking/PrototypeDockFactory`, `Docking/LayoutStore`, `Panels/*`, `Mockups/*`, `HeadlessCaptures` |
| `tools/Luxia.Tools.Captures` | Rendu hors écran de la fenêtre principale en PNG (Avalonia.Headless), sur une copie du projet : vérifier une mise en page sans lancer LuXia | `Program.cs` |
| `tools/fiche-exigences.py` | Crée / met à jour les fiches `docs/exigences/*.md` à partir d'une liste JSON (statut, historique en ajout seul) ; puis `tools/matrice-exigences.py` | en-tête du script |
| `firmware/arduino-dmx` | Firmware Leonardo 1.0 (Enttec) | `arduino-dmx.ino` |

Tests : un projet par module + `Luxia.Integration.Tests` (rejeu du show de référence) + `Luxia.UI.Tests` (modèles de vue sans interface).

## 4. Commandes utiles

```bash
dotnet build Luxia.sln
dotnet test --solution Luxia.sln -- --filter-not-trait "Categorie=Materiel"
dotnet format Luxia.sln --verify-no-changes
python tools/matrice-exigences.py P0 P1 P2 P3 P4 P5 ERG P6
python tools/fiche-exigences.py spec.json   # crée / met à jour des fiches (voir l'en-tête du script)
dotnet run --project src/Luxia.App -- "samples/Show de référence"
dotnet run --project tools/Luxia.Tools.Headless -- valider "samples/Show de référence"
dotnet run --project tools/Luxia.Tools.Headless -- jouer "samples/Show de référence" --scene "Chenillard 4 couleurs" --duree 3
dotnet run --project tools/Luxia.Tools.Captures -- "samples/Show de référence" "<dossier des images>"
python tools/generer-demo-controle.py                  # samples/Démo Contrôle (essai de l'écran Contrôle)
dotnet run --project tools/Luxia.Tools.Prototype                              # prototype ergonomique
dotnet run --project tools/Luxia.Tools.Prototype -- --maquettes docs/maquettes # maquettes « Contrôle » en PNG
```

- **Trames de référence P4, P5 et P6** (`tests/assets/golden/P4-scenes.txt`, `P5-scenes.txt`, `P6-scenes.txt`) : après un changement **voulu et vérifié** du rendu des scènes,
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
| 2026-09-27 | P5 | Couches, Palettes, Live, MIDI développés sur `p5/couches-palettes-live` (questions Q28 à Q32 tranchées au démarrage, développement en autonomie, essais de l'utilisateur à la fin) : sûreté (D29), couches complètes, positions par lieu, zones interdites, écran Live, APC mini (`Luxia.Midi`, D30), fiabilité (D31), contenu et trames de référence P5, guide P5. 75 exigences P5 (43 Réalisé, 14 à valider sur matériel, 6 Partiel, 12 reportées). **En attente des essais de l'utilisateur**, puis proposition de l'analyse ergonomique avant `v1.004`. |
| 2026-09-27 | P5 | Guide P5 déroulé pas à pas avec l'utilisateur au matériel (exemples 1 à 13). Corrigés au fil de l'eau, chacun avec sa fiche : trois courses écran / moteur, cellule des sélections automatiques, Problèmes du projet, fumée, clavier, MIDI rapide, fenêtre de démarrage ; parc réel corrigé (BUV463 8 canaux, WT05). 505 tests verts. **Validée, fusionnée dans `main`, étiquette `v1.004`.** Prochaine étape : analyse ergonomique. |
| 2026-09-27 | Ergonomie | Documentation remise à jour après P5 (anciens noms `Dmx.*` corrigés, notes de modules, doc 03 §11). Analyse ergonomique menée dans la discussion de P5 : lecture Daslight 4 / 5, captures des écrans, doc 60 (charte, déclencheurs et looks, composants, modules) ; **validée par l'utilisateur** (E1-E8, F1-F10). Outil `tools/fiche-exigences.py` versé au dépôt. Suite dans une nouvelle discussion : prototype technique puis maquettes. |
| 2026-09-27 | Ergonomie | Prototype technique (Dock, grille Pan/Tilt, sélecteur de couleur, galerie, disposition enregistrée) et maquettes « Contrôle » sur `ergo/analyse` (`v1.005.001`, `v1.005.002`), ERG-001 à ERG-007, Q35 ; reliquats du renommage corrigés. **En attente des essais et de la validation de l'utilisateur.** |
| 2026-09-28 | Ergonomie | Nuit de développement par délégation sur `ergo/analyse` (`v1.005.003`, `v1.005.004`) : écran Contrôle dans LuXia (modes, panneaux ancrables, dispositions Contrôle / Spectacle, verrou soirée, looks, F1-F12), zone permise, 8e couche « Libre », identité visuelle, taille de l'interface, démo `samples/Démo Contrôle` et guide `docs/demos/ERG-controle.md`. ERG-008 à ERG-024. **En attente de l'essai de l'utilisateur ; choix C1-C11 à rediscuter à l'usage.** |
| 2026-09-28 | Ergonomie | Essai de l'écran Contrôle au matériel avec l'utilisateur (guide §0 à §7, tout validé ; 1.005.192 → 1.005.226), corrections au fil de l'eau avec leurs fiches ; analyse de fin d'essai : scènes resserrées (ERG-025), Stop / Tout stopper (ERG-026), marges (ERG-027) ; C12-C14 ; compteur de compilation par version. |
| 2026-09-28 | Ergonomie | Vérifications de fin d'essai (scènes resserrées, marges) conformes en 1.005.237. **Chantier ergonomique validé par l'utilisateur, `ergo/analyse` fusionnée dans `main`, étiquette `v1.005`.** Reste du chantier (doc 60 §7.4 : affectations MIDI / clavier E6, déclencheurs MIDI des looks, retrait de Live / Scènes après usage, Installation / Bibliothèque en panneaux) à reprendre plus tard. Prochaine étape proposée : P6 – Effets. |
| 2026-09-28 | P6 | Effets développés sur `p6/effets` (1.006) en autonomie (Q36, Q25) : moteur d'effets, données et compilation, panneau Effets de l'écran Contrôle (molettes, dessin, aperçu CMD-017), thèmes, bibliothèque, assistants, sélections en cellules, fondu par la teinte ; WZYBUTA en 64 canaux à 181 ; 10 scènes « Phase P6 », guide P6. **En attente de l'essai de l'utilisateur.** |
