# 03 – Règles de développement

> Document transverse : **comment** on écrit le code. Le **quoi** est dans les cahiers des charges (02, 10 à 23).
> Validé par l'utilisateur le 2026-09-24 (Q21, Q22). Toute évolution de ces règles est notée au §12.

---

## 1. Plateforme

| Élément | Choix |
|---|---|
| Framework | **.NET 10** (LTS), `net10.0` ; `net10.0-windows` uniquement pour ce qui dépend de Windows (application, API Windows) |
| Solution | **`LuXia.sln` au format classique** (jamais `.slnx`) : l'outil de l'utilisateur pour ouvrir/compiler le projet ne prend pas en charge le nouveau format XML (D25). Toute commande, script ou doc qui référence la solution utilise `LuXia.sln`. |
| Langage | C# de la version par défaut du SDK |
| Interface | Avalonia (version stable courante), MVVM avec **CommunityToolkit.Mvvm** |
| Assemblage | Projet `Luxia.Hosting` : assemblage explicite des modules, sans conteneur d'injection de dépendances pour l'instant (D20) |
| Journal | `Microsoft.Extensions.Logging` en façade, **Serilog** pour les fichiers tournants (GEN-110) |
| JSON | `System.Text.Json` uniquement |
| Série | `System.IO.Ports` |
| Firmware | Arduino (C++), compilé avec **arduino-cli** ; bibliothèque DMXSerial |

Toute nouvelle dépendance NuGet est justifiée dans le message de commit et ajoutée à `Directory.Packages.props`.
Pas de paquet payant ni de licence restrictive (ex. FluentAssertions ≥ 8 exclu).

## 2. Langue

| Élément | Langue |
|---|---|
| Identifiants (types, membres, variables, projets, fichiers de code) | **Anglais**, en reprenant la colonne « Anglais » du [glossaire](glossaire.md) (`Universe`, `Frame`, `Channel`, `Fixture`, `Layer`, `Palette`, `Override`…) |
| Commentaires, documentation XML | Français |
| Messages de journal, messages d'erreur, interface | Français |
| Messages de commit, documentation `docs/` | Français |
| **Échanges avec l'utilisateur** (discussion, diagnostics, consignes d'essai) | **Toujours en français**, sans exception (une réponse partie en anglais pendant l'essai P6, le 2026-09-28, relevée par l'utilisateur) |
| Noms des commandes / événements dans la doc | Français (`TesterSortie`) ; en code, l'équivalent anglais (`TestOutputCommand`) documenté par un commentaire citant l'identifiant (`CMD-024`) |

Lorsqu'un terme du glossaire n'a pas d'équivalent anglais établi, on en choisit un et **on l'ajoute au glossaire**.

## 3. Structure de la solution

```
LuXia/
├── LuXia.sln                  Solution
├── Directory.Build.props     Réglages communs (framework, nullable, avertissements…)
├── Directory.Packages.props  Versions centralisées des paquets
├── .editorconfig
├── src/                      Bibliothèques et application (doc 00 §7.2)
├── tools/                    Outils ligne de commande (Luxia.Tools.Headless…)
├── tests/                    Un projet de tests par projet testé : <Projet>.Tests ; tests/assets/
├── firmware/arduino-dmx/     Firmware Leonardo
├── samples/                  Show de référence, samples par mécanique (doc 41)
└── docs/                     Documentation (dont docs/demos/)
```

- On ne crée **que les projets utiles à la phase en cours** (pas de coquilles vides).
- Un projet = un module (doc 02 §5.2). Espace de noms racine = nom du projet (`Luxia.Output`) ; les dossiers suivent les espaces de noms.
- Un type public par fichier, fichier nommé comme le type.

### 3.1 Règles de dépendance

| Projet | Peut dépendre de |
|---|---|
| `Luxia.Core` | rien (BCL uniquement) |
| `Luxia.Messaging` | Core |
| `Luxia.Engine` | Core, Messaging — **jamais** d'un pilote concret, de l'UI, de l'audio |
| `Luxia.Output` | Core, Messaging |
| `Luxia.Persistence` | Core |
| `Luxia.Fixtures` | Core, Persistence (modèles d'appareils, validation, imports) |
| `Luxia.Patch` | Core, Persistence, Fixtures (installation, sélections, lieux, GEN-053) |
| `Luxia.Scenes` | Core, Messaging, Engine, Persistence, Fixtures, Patch (scènes, palettes, couches ; compilation vers le moteur, D26) |
| `Luxia.Midi` | Core, Messaging, Engine, Persistence (contrôleurs APC mini : profils, traduction en commandes, retour lumineux, ports Windows `winmm` ; ne connaît ni les couches ni les scènes du projet, l'hôte lui fournit la disposition du Live) |
| `Luxia.Hosting` | tous les projets non graphiques (assemblage, journal technique) |
| `Luxia.UI.Controls` | contrôles réutilisables (fader, moniteur, barre de plages, historique annuler / rétablir) ; aucune dépendance métier |
| `Luxia.UI.Modules.*` | un projet par écran ; tout sauf `Luxia.App` ; n'agit que par commandes (P3) |
| `Luxia.App`, `tools/*` | tout (composition) |

Ces règles sont **vérifiées par un test d'architecture** (`Luxia.Architecture.Tests`), qui échoue en cas de violation.

## 4. Style C#

- `.editorconfig` du dépôt = référence ; formatage vérifié par `dotnet format --verify-no-changes`.
- `Nullable` activé, `TreatWarningsAsErrors` activé, analyseurs .NET (`AnalysisLevel` latest, mode recommandé). Une suppression d'avertissement est locale et commentée.
- Espaces de noms au niveau du fichier ; `var` quand le type est évident ; accolades toujours.
- Nommage .NET standard : `PascalCase` pour types et membres, `_camelCase` pour champs privés, `I` pour les interfaces, `Async` pour les méthodes asynchrones.
- Types immuables par défaut (`record`, `readonly struct`, `init`) pour les données échangées entre modules.
- Pas d'`async void` (sauf gestionnaires d'événements d'interface) ; toute tâche de fond est supervisée (exceptions journalisées, GEN-093).

### 4.1 Temps réel

Le code exécuté à chaque tick (boucle moteur, routeur) :

- **n'alloue pas** en régime établi (tampons réutilisés, pas de LINQ, pas de closures) ;
- ne bloque jamais (pas d'E/S, pas de verrou tenu par un tiers lent) ;
- ne lit jamais l'heure murale : uniquement l'horloge injectée (`IClock`, GEN-033).

## 5. Commentaires

- Documentation XML (`///`) sur **toute l'API publique**, en français.
- Les commentaires expliquent le **pourquoi** (intention, contrainte, piège), pas le **quoi**.
- Le code qui réalise une exigence cite son identifiant : `// SORT-003 : seule la trame la plus récente est conservée.`
- Pas de code commenté laissé en place ; les `TODO` citent une phase ou une exigence (`// TODO(P5, GEN-083) : …`).

## 6. Tests

| Élément | Règle |
|---|---|
| Outils | **xUnit** + **Shouldly** |
| Emplacement | `tests/<Projet>.Tests` ; données dans `tests/assets/` |
| Nommage | `Methode_Condition_ResultatAttendu` (anglais) ; `DisplayName` en français si utile |
| Traçabilité | Chaque test lié à une exigence porte `[Trait("Exigence", "SORT-003")]` (plusieurs si besoin) |
| Matériel | Tests nécessitant l'Arduino : `[Trait("Categorie", "Materiel")]`, **exclus** de la commande courante |
| Commandes | `dotnet test --solution LuXia.sln -- --filter-not-trait "Categorie=Materiel"` (xUnit v3 sur Microsoft.Testing.Platform, `global.json`) |
| Intégration | `tests/Luxia.Integration.Tests` : scénarios bout en bout et **rejeu des exemples du show de référence** (DEMO-3) |
| Temps | Horloge injectée : aucun `Thread.Sleep` pour attendre un résultat dans un test unitaire |
| Couverture attendue | Toute exigence I testable automatiquement a au moins un test ; les autres sont couvertes par le guide de démonstration ou une check-list (doc 30) |

`dotnet build` et `dotnet test` doivent passer **sans avertissement** avant chaque commit.

## 7. Git

| Élément | Règle |
|---|---|
| Dépôt | Distant GitHub `origin` = `https://github.com/paillardfrancois35-jpg/Luxia.git` (câblé le 2026-09-26) |
| Distant | Poussé sur `origin` au fil de l'eau : chaque commit sur la branche de phase, et `main` + étiquettes après chaque fusion/validation |
| Responsabilité | **Les opérations Git sont à la charge de l'IA de développement** (décision de l'utilisateur, 2026-09-25) : commits au fil des étapes ; fusion dans `main` et étiquette de version **à chaque validation d'un passage important par l'utilisateur**. |
| Branches | `main` ne reçoit que du validé ; une branche par phase (`p0/fondations`, `p1/console`…) avec un commit par étape vérifiable ; fusion dans `main` (`--no-ff`) après validation |
| Commits | En français, format `type(module): résumé` ; types : `feat`, `fix`, `test`, `docs`, `refactor`, `build`, `chore`, `firmware` ; le corps cite les exigences (`Exigences : SORT-001, SORT-003`) |
| Étiquettes | Numérotation choisie par l'utilisateur le 2026-09-25 (remplace `p0`, `p1`…) : `v1.001`, `v1.002`… — une par **validation** de l'utilisateur, sur `main`, +1 à chaque fois. Annotée, message = ce qui a été validé. |
| Étiquettes intermédiaires | Pendant le développement d'un passage **non encore validé**, chaque commit notable sur sa branche est étiqueté `v1.0NN.MMM` (`NN` = le numéro de la prochaine validation attendue, `MMM` +1 à chaque étiquette). Objectif : distinguer d'un coup d'œil une version encore en cours de dev d'une version validée. À la validation, `v1.0NN.MMM` disparaît au profit de `v1.0NN` sur `main`, et `MMM` repart de `001` pour le passage suivant. |
| Fichiers exclus | `bin/`, `obj/`, `.vs/`, `*.user`, fichiers de build Arduino, journaux, enregistrements temporaires |

## 8. Données et fichiers

- JSON UTF-8 indenté, propriétés en `camelCase`, énumérations en texte (GEN-050).
- Chaque fichier porte `"formatVersion"` ; toute évolution de format ajoute une **migration** et un test de migration (GEN-051).
- Identifiants stables (GUID) pour les objets (GEN-052) dès qu'ils existent.
- Le format de chaque fichier est documenté au fil de l'eau dans `docs/50-format-des-donnees.md` (GEN-130).

## 9. Firmware

- Code dans `firmware/arduino-dmx/`, avec un `README.md` (bibliothèques et versions, cavaliers du shield, commande de compilation).
- Pas d'allocation dynamique (SORT-044) ; constante unique de version (SORT-047).
- **Compilation libre ; tout téléversement sur la carte est demandé à l'utilisateur au préalable** (Q19).

## 10. Conduite

- **Fiches d'exigences** (`docs/exigences/<ID>.md`, règles F1 à F7 de leur README) : une fiche par exigence travaillée ; toute question, réponse, décision, écart, développement, test ou validation qui la concerne y est ajouté (historique ajout seul). Revenir sur une décision = nouvelle entrée qui cite l'ancienne et explique ce qui a changé. La fiche fait foi pour le statut. **Ceci vaut aussi pour une fonctionnalité ou un correctif de comportement demandé à la volée en session (pas prévu au cahier des charges) dès qu'il devient du code livré** : lui donner un ID (nouvelle ligne au cahier des charges, doc concerné), pas seulement un commit Git (oubli vécu le 2026-09-26 : icône, menu À propos et verrou mono-instance faits sans fiche avant d'y revenir — régularisés en GEN-114 à 116). Une décision transverse qui ne change aucun comportement observable de l'application (le nom du projet lui-même, l'organisation Git) reste une **idée** (`docs/99`), pas une exigence.
- Divergence avec le cahier des charges : signalée, puis reportée dans le document concerné **et** dans la fiche de l'exigence.
- Nouvelle idée : `docs/99-idees.md`. Question : `docs/01-questions-ouvertes.md`, reposée jusqu'à réponse.
- Chaque étape se termine par : build + tests verts, ce qu'il faut vérifier, commit sur la branche d'étape.
- Chaque phase se livre avec ses démonstrations (doc 40 §7, doc 41).
- Essais de l'utilisateur : une discussion « test » séparée, sans correctif, qui note les résultats dans `docs/essais/Pn-resultats.md` ; la discussion « dev » corrige (doc 33).

## 11. Pièges déjà rencontrés

Liste vivante, alimentée à chaque fois qu'un même type d'erreur se reproduit. But : ne pas refaire deux fois la même faute d'inattention, en particulier sur l'ergonomie et sur les pièges Avalonia peu visibles à la relecture.

- **Champs de saisie trop étroits.** Un `NumericUpDown`/`TextBox` sans largeur explicite (ou avec une largeur « au pif » de 60-100px) n'affiche souvent qu'un chiffre ou deux, surtout dans une `Grid` à colonnes fixes. Un `NumericUpDown` a besoin de plus de marge qu'un simple `TextBox` : ses boutons +/- prennent de la place, et **110px reste trop juste** en pratique (confirmé deux fois par l'utilisateur le 2026-09-26, y compris après un premier correctif à 110px). Règle : largeur explicite d'au moins **140px** pour un `NumericUpDown`, 110-140px pour un `TextBox` court, jamais la largeur par défaut du contrôle. Vérifier à l'œil (capture ou test manuel), pas seulement à la compilation — et revérifier après correctif, ce n'est pas toujours bon du premier coup. (Signalé le 2026-09-26 sur l'écran Installation : panneau « Ajouter un appareil », liste du patch, onglet Lieux.)
- **`ObservableCollection<T>.Clear()` puis re-remplissage, quand la collection est l'`ItemsSource` d'un contrôle dont `SelectedItem` est lié en bidirectionnel à une propriété de type valeur non annulable (`int`, `enum`...).** Le `Clear()` fait transiter la sélection par `null` le temps de la reconstruction ; la liaison tente alors de repousser ce `null` vers l'`int`, ce qui lève `System.InvalidCastException: Could not convert '(null)' (null) to System.Int32.`. Corriger en ne touchant la collection que pour la différence réelle (retirer les éléments obsolètes, ajouter les manquants), jamais par un `Clear()` suivi d'un re-remplissage complet. (Rencontré le 2026-09-26 sur `InstallationViewModel.LoadAll()`, ComboBox « Univers affiché ».)
- **Un bouton bascule (« stop si déjà actif, sinon démarre ») qui appelle d'abord la méthode d'arrêt puis teste l'état.** Si la méthode d'arrêt remet l'état à sa valeur neutre (ex. `_identifyingFixtureId = null`), le test qui suit ne peut plus jamais être vrai : un second clic redémarre au lieu d'arrêter. Toujours capturer l'état AVANT d'appeler la méthode qui le modifie. (Rencontré le 2026-09-26 sur `InstallationViewModel.ToggleIdentifyFixture`.)
- **Pousser un seul canal d'intensité à 255 ne rend pas forcément un appareil visible.** Sur un appareil RVB (ou RVBW…), le gradateur général (`Intensity`) n'est qu'un multiplicateur : si le rouge/vert/bleu sont à 0, l'appareil reste éteint même gradateur au maximum — contrairement à un projecteur à lampe classique où l'intensité seule suffit. Toute fonctionnalité qui doit rendre un appareil visible (Identifier, CMD-023) doit aussi pousser les canaux émetteurs de couleur (`AttributeInfo.IsEmitter`), pas seulement la famille `Intensity`. (Rencontré le 2026-09-26 sur `IdentifyRules.IdentifyChannels`, retour utilisateur avec un PAR réel qui ne s'allumait pas.)
- **Un comportement minuté piloté par le rafraîchissement d'un écran (`IRefreshable.Refresh()`) s'arrête dès que cet écran n'est plus affiché.** La coquille (`MainWindowViewModel`) ne rafraîchit que l'écran actuellement sélectionné, pour ne pas gaspiller de calcul sur les écrans invisibles — ce qui est correct pour de l'affichage pur, mais fige toute surcharge réelle de canaux (Identifier, CMD-023) au dernier état si l'utilisateur change d'écran pendant qu'elle tourne (ex. pour vérifier au Simulateur, SIM-009). Un écran qui pilote une telle surcharge doit se signaler comme ayant besoin d'un rafraîchissement de fond (`IRefreshable.NeedsBackgroundRefresh`), sans quoi la coquille l'ignore dès qu'il quitte l'écran. (Rencontré le 2026-09-26, retour utilisateur : Identifier restait figé en passant sur le Simulateur.)
- **Une propriété calculée en lecture seule (`=> ...`, pas `init`) sur un `record` enregistré en JSON est sérialisée comme les autres, sans qu'on le veuille.** Le sérialiseur (réflexion, `DmxJson.Options`) inclut par défaut toute propriété publique lisible ; une propriété calculée (`FixtureType.DisplayName`, `FixtureMode.ChannelCount`, `Capability.Median`, `VenueSet.Active`, `TestOutputPreferences.ValueByte`…) se retrouve donc dupliquée dans le fichier à chaque enregistrement, sans jamais être relue (pas de `init`) — juste du bruit qui gonfle le fichier et nuit à la lisibilité (D12 : JSON lisible à la main). Mettre `[JsonIgnore]` sur toute propriété calculée d'un `record` persisté, dès sa création — pas seulement quand on le remarque dans un fichier. Sans danger pour les fichiers déjà enregistrés avec le champ en trop : il est simplement ignoré à la relecture, et disparaît de lui-même au prochain enregistrement. (Rencontré le 2026-09-26 en examinant une différence de `lieux.json` après un test utilisateur ; recherche élargie aux autres records persistés du même genre.)
- **Une plage de roue de couleur / macro sans couleur définie (`Capability.Colors` vide) n'est pas une absence de signal.** Une position « ouverte » (pas de gélatine devant la lampe) laisse passer la lumière blanche de la source : `FixtureDecoder` la traitait comme « pas de couleur » → noir, alors qu'elle doit être blanche. Distinguer « pas de couleur définie sur cette plage » (blanc, lumière non filtrée) de « appareil RVB dont les canaux sont à 0 » (réellement noir, ces deux cas ont une cause physique différente). (Rencontré le 2026-09-26 sur `FixtureDecoder.DecodeCell`, retour utilisateur : une lyre identifiée s'allumait réellement mais restait invisible au simulateur.)

- **Un test qui mesure du temps réel (gigue, latence) échoue de temps en temps quand la suite charge la machine.** Les assemblages de tests tournent en parallèle, chacun dans son processus : une série de mesures tombée pendant un pic de charge dépasse le seuil sans que le logiciel soit en cause. Règle : un tel test garde son seuil strict mais **recommence sa mesure jusqu'à trois fois** et ne retient que la dernière série ; les tests de temps réel d'un même assemblage sont regroupés dans une collection xUnit non parallélisée (`RealTimeTests`). La vraie mesure reste celle de l'outil (`luxia-headless gigue`) ou du matériel. (Rencontré le 2026-09-26 en P4 : `TickLoopTests` et `ConsoleLatencyTests`, jamais en échec avant que la suite ne s'alourdisse.)
- **Recommencer une mesure ne suffit pas : un test « instable » peut avoir une cause déterministe que seule la charge révèle.** `EnginePerformanceTests` (MOT-002) échouait parfois sur l'allocation (23 576 octets pour un plafond de 20 000) : ce n'était ni un autre fil (la mesure `GC.GetAllocatedBytesForCurrentThread` est par fil, donc exacte), ni la compilation à la volée, mais la `List` du capteur de test qui double sa capacité (1024 → 2048 entrées) **pendant la 3e série** — jouée seulement quand la charge avait ralenti les deux premières. Le moteur, lui, alloue 0 octet. `OutputRouterTests.SlowDriver…` (SORT-002/003) comptait des trames sur une fenêtre de temps réel (écriture de 200 ms, `Task.Delay(25)`), que la charge étire. Règles : (1) avant d'élargir une marge ou d'ajouter des essais, **mesurer** ce qui dépasse (chiffre exact, série par série) ; (2) une mesure d'allocation ne compte que le code mesuré : tout tampon de test qui grossit (liste, capteur) est vidé et dimensionné avant la série, et sa part est mesurée, pas estimée ; (3) un test de concurrence qui n'a pas besoin de temps réel remplace les délais par une **barrière** (`ManualResetEventSlim`) et des attentes de condition : pilote lent bloqué tant que le test ne le libère pas, résultat exact (ici 40 trames au rapide, exactement 2 au lent) — relâcher la barrière dans un `finally`, sinon l'arrêt attend indéfiniment. (Rencontré le 2026-09-28, série complète des tests.) **Complément (2026-09-29)** : pour une mesure de **durée** (pas de gigue), retenir la **meilleure** de plusieurs séries : la charge ne peut que ralentir, jamais accélérer ; `EnginePerformanceTests` : ≈ 1 ms par tick seul, 5,7 ms sous la charge de la série complète (plafond 5 ms), désormais meilleure de 5 séries. **Complément (2026-10-02, P8)** : avec un projet de tests de plus (`Luxia.Show.Tests`), la charge de la série complète dépassait la durée des cinq séries (5,47 ms mesurés, trois fois sur quatre passages) ; au-delà de cinq séries, le test en refait une par seconde pendant au plus 30 s (seuil et contrôle d'allocation inchangés ; passe seul du premier coup).
- **Une propriété calculée à la construction d'un `record` (`{ get; } = f(Mode)`) n'est pas recalculée par une copie `with`.** La copie recopie le champ déjà calculé : `info with { Mode = autre }` garde les canaux de l'ancien mode, sans erreur visible. Pour un objet dont des valeurs dérivent d'autres, préférer une classe avec constructeur (et une méthode `WithMode(...)` explicite), ou un calcul dans le getter. (Rencontré le 2026-09-26 sur `FixtureInfo`, rapport d'impact d'un changement de mode.)

- **Un enregistrement de fichier peut être refusé un court instant par le poste lui-même.** Sur le PC de l'utilisateur (outils de sécurité d'entreprise), environ 2 % des remplacements de fichier rapprochés échouent avec « accès refusé » (mesuré : 9 sur 500). Une écriture faite à chaque cran d'un champ numérique finit donc par tomber dessus, et Avalonia affiche l'exception sous le champ sans la journaliser. Règle : toute écriture de données passe par `VersionedJsonFile.Save`, qui réessaie (10 fois, attente croissante) ; un écran qui enregistre en continu attrape l'échec résiduel et l'affiche en message. (Rencontré le 2026-09-26, essai P4 avec l'utilisateur : champ Vitesse de l'écran Scènes.)

- **Compiler pendant que LuXia tourne n'a rien livré.** L'application ouverte verrouille ses DLL : la compilation échoue à la copie et l'utilisateur continue de tester l'ancienne version sans le savoir. Règle : demander de fermer LuXia **avant** de compiler, vérifier « 0 Erreur(s) » et annoncer le numéro de compilation obtenu (GEN-119) ; l'utilisateur le retrouve dans la barre de titre. (Rencontré le 2026-09-26, essai P4.) Le compteur est propre à chaque version de développement (`build/numero-de-compilation-<version>.txt`) : il repart de 1 après une validation. `dotnet format` recompile aussi : lire le numéro **après** la dernière commande. LuXia peut mettre quelques secondes à se fermer : si la copie échoue (« verrouillé par LuXia »), relancer. **Le compteur est partagé avec le prototype ergonomique** (`LuXia-Prototype`, qui affiche aussi son numéro) : après une compilation de toute la solution, le fichier contient le numéro du **dernier** exécutable compilé, souvent le prototype (+1). Lire le numéro réel de LuXia dans `src/Luxia.App/bin/Debug/net10.0/LuXia.dll` (version du produit), pas dans le fichier (annonce 1.006.041 au lieu de 1.006.040, essai P6).

- **Un écran (ou un contrôleur) qui décide d'après l'état relu du moteur a toujours un temps de retard.** L'interface relit l'instantané du moteur 20 fois par seconde, un contrôleur MIDI envoie des dizaines de messages par seconde : entre une commande et son traitement, l'état relu est **l'ancien**. Trois défauts de l'essai P5 en venaient : une bascule lancer / arrêter qui relançait la scène au lieu de l'arrêter (LIVE-003), un niveau affiché qui « sautait » 20 → 10 → 20 (LIVE-040), un fader MIDI lâché en descente rapide (MIDI-004). Règles : (1) une décision qui dépend de l'état (bascule, « si déjà actif ») est prise **par le moteur**, au traitement de la commande (ex. `LaunchSceneCommand.StopIfPlaying`) ; (2) une valeur réglée à l'écran est **gardée jusqu'à sa confirmation** par le moteur (`EngineEcho`, Luxia.UI.Controls) ; (3) un contrôleur compare l'état relu à **ses derniers envois**, pas au seul dernier (`SoftTakeover`). Un test doit reproduire la course (rafraîchissement **avant** le tick), sinon il passe à tort : en temps virtuel, le moteur a toujours un temps d'avance. (Rencontré le 2026-09-27, essai P5.)
- **La notice d'un appareil peut être fausse ou incomplète.** Le BeamZ BUV463 réel a un **8e canal** (« lissage du gradateur », ≈ 15 s à 255) absent de sa notice (7 canaux) : patché en 7 canaux, il lisait le gradateur de l'appareil suivant comme ce canal, d'où un « allumage lent » qui a coûté une journée, pendant qu'on soupçonnait LuXia, le firmware puis la ligne. Règles : quand un appareil réagit de façon inexplicable alors que la trame est juste, (1) vérifier ce que reçoit la ligne (journal `.journal.txt` d'un enregistrement, écoute avec le DVC4 Daslight : `docs/Equipements/DasLight/ecoute-ligne-dmx-dvc4.md`), (2) regarder les **canaux voisins** au-delà du dernier canal documenté, (3) comparer avec un appareil témoin à la même adresse. (Rencontré le 2026-09-27, essai P5.)

- **Surcharges et conversion implicite tableau → span (C# 14).** Une méthode privée `F(ReadOnlySpan<T>)` à côté d'une
  méthode `F(IReadOnlyList<T>)` : un appel `F(tableau)` choisit désormais la version span (conversion « de première
  classe »), sans erreur ni avertissement. Dans `SafetyLimiter`, cela sautait l'extension des zones touchant une butée,
  et une lyre pouvait se coller au bord interdit. Règle : ne pas surcharger par `ReadOnlySpan` une méthode qui prend une
  collection ; donner un autre nom à la variante interne. (Rencontré le 2026-09-28, zone permise, trouvé par un test.)
- **Espace de noms qui masque un type d'Avalonia.** Dans `Luxia.UI.Modules.Control.Views`, le nom `Control` désigne
  l'espace de noms `Luxia.UI.Modules.Control`, plus le type `Avalonia.Controls.Control` (erreur CS0118). Écrire
  `Avalonia.Controls.Control` dans ce module. (Rencontré le 2026-09-28.)

- **HTP : une scène d'intensité pleine masque les autres.** « Plein feu » tient tous les appareils à 100 % sur la couche Intensité (la plus haute valeur l'emporte) : un chenillard ou un flash d'intensité sur les mêmes appareils n'a aucun effet visible. Pour essayer un effet, utiliser une scène qui n'allume que ce qu'il faut (« Lyres allumées (sans les PAR) »). (Essai P7.)
- **Tout ce qui parle à Windows (COM, WASAPI, Bluetooth) est lent et peut échouer à tout moment.** Énumérer les périphériques (0,4 à 1 s), ouvrir une capture, arrêter une boucle sur un casque Bluetooth : jamais sur le chemin de démarrage ni sur le fil de l'interface, et jamais en attendant la fin d'un arrêt. (Essai P7 : démarrage ralenti, changement de sortie non suivi.)
- **Un objet COM libéré lève une `InvalidCastException`, pas une `ObjectDisposedException`.** Lire `MMDevice.ID` après un arrêt concurrent a fait tomber l'application (1.009.090). Lire une fois ce dont on a besoin à la création et le garder (`WasapiSource._deviceId`). (Essai P7.)
- **Aucune exception ne doit sortir d'un timer ni d'un fil de fond** : l'application s'arrête (« FTL »). Tout rappel de `System.Threading.Timer` passe par un garde-fou qui journalise (`AudioListener.Guard`). Vérifier aussi les abonnés d'événements appelés depuis un fil de capture. (Essai P7.)
- **Arrêter une source depuis son propre fil de capture l'attend elle-même.** `StopRecording` joint le fil de capture : appelé depuis `DataAvailable` ou un abonné, il se bloque. Libérer une source hors verrou et hors de son fil (`Task.Run`). (Essai P7.)
- **Une liste plafonnée ne se suit pas par son nombre d'éléments.** La liste d'événements de l'écran Audio (30 récents) ne se rafraîchissait plus une fois pleine : suivre un compteur total. (Essai P7, drop « jamais vu ».)
- **Un état de saisie lié au focus ne doit pas être remis à faux par la validation.** Le champ BPM gardait le focus après Entrée : le rafraîchissement (20 fois par seconde) écrasait la frappe suivante. L'état « saisie en cours » suit le focus. (Essai P7, E3.)
- **Recréer une liste d'éléments défilants remet le défilement en haut.** Mémoriser la position par colonne et la rétablir une fois le contenu mesuré (`ColumnsPanelView.OnColumnAttached`). (Essai P7.)

## 12. Historique

| Date | Modification |
|---|---|
| 2026-10-02 | §11 : pièges de P7 (HTP, Windows lent, objet COM libéré, exception dans un timer, arrêt depuis son propre fil, liste plafonnée, état de saisie et focus, défilement perdu). |
| 2026-09-29 | §11 : mesure de durée sous charge → meilleure de plusieurs séries. |
| 2026-09-28 | §2 : échanges avec l'utilisateur toujours en français. |
| 2026-09-28 | §11 : numéro de compilation partagé avec le prototype (lire la version de `LuXia.dll`). |
| 2026-09-28 | §11 : test « instable » à cause déterministe (tampon de test qui grossit pendant la mesure d'allocation ; délais réels remplacés par une barrière). |
| 2026-09-28 | §11 : surcharge par span choisie pour un tableau (C# 14) ; espace de noms `…Control` qui masque `Avalonia.Controls.Control`. |
| 2026-09-27 | Reliquats du renommage DMX → LuXia : `Dmx.sln` → `LuXia.sln` (§1, §6) ; `.editorconfig` visait encore `src/Dmx.UI.**` (réglage CA1822 des écrans sans effet) ; `dmx-headless` dans le `JOURNAL.md` du show de référence. Les entrées d'historique et décisions antérieures gardent les anciens noms (doc 02 §19). |
| 2026-09-27 | §11 : course écran / moteur (décision par le moteur, `EngineEcho`, historique des envois MIDI) ; notice d'appareil incomplète (8e canal du BUV463). Noms `Dmx.*` restants corrigés en `Luxia.*` dans les tables de ce document. |
| 2026-09-26 | §11 : écriture de fichier refusée un instant par le poste (nouvelles tentatives dans `VersionedJsonFile.Save`). |
| 2026-09-26 | §11 : tests de temps réel sensibles à la charge (trois essais, collection non parallélisée) ; copie `with` d'un `record` à propriété calculée à la construction. |
| 2026-09-26 | P4 : projet `Luxia.Scenes` (Core, Messaging, Engine, Persistence, Fixtures, Patch) pour les scènes, palettes et couches et leur compilation vers le moteur (D26) ; le moteur reste limité à Core et Messaging. |
| 2026-09-26 | §11 « Pièges déjà rencontrés » (largeurs de saisie, `ObservableCollection.Clear()` sur un `SelectedItem` non annulable, bascule stop/démarre, intensité seule ne suffit pas sur un appareil RVB) — demande explicite de l'utilisateur après des retours de test en direct sur l'écran Installation. |
| 2026-09-26 | Solution au format `.sln` classique, plus `.slnx` (D25) : l'utilisateur ne pouvait plus ouvrir/compiler le projet. |
| 2026-09-26 | `IRefreshable.NeedsBackgroundRefresh` : un écran qui identifie un appareil continue d'être rafraîchi même non affiché (§11, retour utilisateur Simulateur). |
| 2026-09-26 | P3 : projet `Dmx.Patch` (Core, Persistence, Fixtures) pour l'installation, les sélections et les lieux (doc 13, doc 00 §7.2). |
| 2026-09-24 | Version initiale (Q21, Q22). |
| 2026-09-25 | Fiches d'exigences `docs/exigences/` (demande de l'utilisateur) : suivi par exigence façon Redmine, source du statut de la matrice 31. |
| 2026-09-25 | P1-P2 : projets d'interface par écran (D22), CA1822 en suggestion pour les projets d'interface (liaisons), CA1309 désactivée (tris affichés en français), matrice `tools/matrice-exigences.py`. |
| 2026-09-24 | P0 : projet `Dmx.Hosting` (D20), commandes de test Microsoft.Testing.Platform, projet de tests d'intégration, test d'architecture par réflexion (sans NetArchTest). |
