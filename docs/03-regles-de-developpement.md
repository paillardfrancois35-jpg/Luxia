# 03 – Règles de développement

> Document transverse : **comment** on écrit le code. Le **quoi** est dans les cahiers des charges (02, 10 à 23).
> Validé par l'utilisateur le 2026-09-24 (Q21, Q22). Toute évolution de ces règles est notée au §12.

---

## 1. Plateforme

| Élément | Choix |
|---|---|
| Framework | **.NET 10** (LTS), `net10.0` ; `net10.0-windows` uniquement pour ce qui dépend de Windows (application, API Windows) |
| Langage | C# de la version par défaut du SDK |
| Interface | Avalonia (version stable courante), MVVM avec **CommunityToolkit.Mvvm** |
| Assemblage | Projet `Dmx.Hosting` : assemblage explicite des modules, sans conteneur d'injection de dépendances pour l'instant (D20) |
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
| Noms des commandes / événements dans la doc | Français (`TesterSortie`) ; en code, l'équivalent anglais (`TestOutputCommand`) documenté par un commentaire citant l'identifiant (`CMD-024`) |

Lorsqu'un terme du glossaire n'a pas d'équivalent anglais établi, on en choisit un et **on l'ajoute au glossaire**.

## 3. Structure de la solution

```
DMX/
├── Dmx.slnx                  Solution
├── Directory.Build.props     Réglages communs (framework, nullable, avertissements…)
├── Directory.Packages.props  Versions centralisées des paquets
├── .editorconfig
├── src/                      Bibliothèques et application (doc 00 §7.2)
├── tools/                    Outils ligne de commande (Dmx.Tools.Headless…)
├── tests/                    Un projet de tests par projet testé : <Projet>.Tests ; tests/assets/
├── firmware/arduino-dmx/     Firmware Leonardo
├── samples/                  Show de référence, samples par mécanique (doc 41)
└── docs/                     Documentation (dont docs/demos/)
```

- On ne crée **que les projets utiles à la phase en cours** (pas de coquilles vides).
- Un projet = un module (doc 02 §5.2). Espace de noms racine = nom du projet (`Dmx.Output`) ; les dossiers suivent les espaces de noms.
- Un type public par fichier, fichier nommé comme le type.

### 3.1 Règles de dépendance

| Projet | Peut dépendre de |
|---|---|
| `Dmx.Core` | rien (BCL uniquement) |
| `Dmx.Messaging` | Core |
| `Dmx.Engine` | Core, Messaging — **jamais** d'un pilote concret, de l'UI, de l'audio |
| `Dmx.Output` | Core, Messaging |
| `Dmx.Persistence` | Core |
| `Dmx.Fixtures` | Core, Persistence (modèles d'appareils, validation, imports) |
| `Dmx.Patch` | Core, Persistence, Fixtures (installation, sélections, lieux, GEN-053) |
| `Dmx.Hosting` | tous les projets non graphiques (assemblage, journal technique) |
| `Dmx.UI.Controls` | contrôles réutilisables (fader, moniteur, barre de plages, historique annuler / rétablir) ; aucune dépendance métier |
| `Dmx.UI.Modules.*` | un projet par écran ; tout sauf `Dmx.App` ; n'agit que par commandes (P3) |
| `Dmx.App`, `tools/*` | tout (composition) |

Ces règles sont **vérifiées par un test d'architecture** (`Dmx.Architecture.Tests`), qui échoue en cas de violation.

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
| Commandes | `dotnet test --solution Dmx.slnx -- --filter-not-trait "Categorie=Materiel"` (xUnit v3 sur Microsoft.Testing.Platform, `global.json`) |
| Intégration | `tests/Dmx.Integration.Tests` : scénarios bout en bout et **rejeu des exemples du show de référence** (DEMO-3) |
| Temps | Horloge injectée : aucun `Thread.Sleep` pour attendre un résultat dans un test unitaire |
| Couverture attendue | Toute exigence I testable automatiquement a au moins un test ; les autres sont couvertes par le guide de démonstration ou une check-list (doc 30) |

`dotnet build` et `dotnet test` doivent passer **sans avertissement** avant chaque commit.

## 7. Git

| Élément | Règle |
|---|---|
| Dépôt | **Local uniquement** (pas de distant) |
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

- **Fiches d'exigences** (`docs/exigences/<ID>.md`, règles F1 à F7 de leur README) : une fiche par exigence travaillée ; toute question, réponse, décision, écart, développement, test ou validation qui la concerne y est ajouté (historique ajout seul). Revenir sur une décision = nouvelle entrée qui cite l'ancienne et explique ce qui a changé. La fiche fait foi pour le statut.
- Divergence avec le cahier des charges : signalée, puis reportée dans le document concerné **et** dans la fiche de l'exigence.
- Nouvelle idée : `docs/99-idees.md`. Question : `docs/01-questions-ouvertes.md`, reposée jusqu'à réponse.
- Chaque étape se termine par : build + tests verts, ce qu'il faut vérifier, commit sur la branche d'étape.
- Chaque phase se livre avec ses démonstrations (doc 40 §7, doc 41).

## 11. Pièges déjà rencontrés

Liste vivante, alimentée à chaque fois qu'un même type d'erreur se reproduit. But : ne pas refaire deux fois la même faute d'inattention, en particulier sur l'ergonomie et sur les pièges Avalonia peu visibles à la relecture.

- **Champs de saisie trop étroits.** Un `NumericUpDown`/`TextBox` sans largeur explicite (ou avec une largeur « au pif » de 60-100px) n'affiche souvent qu'un chiffre ou deux, surtout dans une `Grid` à colonnes fixes. Règle : toute valeur numérique ou texte court saisie par l'utilisateur a une largeur explicite d'au moins 110-140px (de quoi afficher confortablement 3-4 caractères plus les flèches d'un NumericUpDown), jamais laissée à la largeur par défaut du contrôle. Vérifier à l'œil (capture ou test manuel), pas seulement à la compilation. (Signalé le 2026-09-26 sur l'écran Installation : panneau « Ajouter un appareil », liste du patch, onglet Lieux — tous corrigés le même jour.)
- **`ObservableCollection<T>.Clear()` puis re-remplissage, quand la collection est l'`ItemsSource` d'un contrôle dont `SelectedItem` est lié en bidirectionnel à une propriété de type valeur non annulable (`int`, `enum`...).** Le `Clear()` fait transiter la sélection par `null` le temps de la reconstruction ; la liaison tente alors de repousser ce `null` vers l'`int`, ce qui lève `System.InvalidCastException: Could not convert '(null)' (null) to System.Int32.`. Corriger en ne touchant la collection que pour la différence réelle (retirer les éléments obsolètes, ajouter les manquants), jamais par un `Clear()` suivi d'un re-remplissage complet. (Rencontré le 2026-09-26 sur `InstallationViewModel.LoadAll()`, ComboBox « Univers affiché ».)
- **Un bouton bascule (« stop si déjà actif, sinon démarre ») qui appelle d'abord la méthode d'arrêt puis teste l'état.** Si la méthode d'arrêt remet l'état à sa valeur neutre (ex. `_identifyingFixtureId = null`), le test qui suit ne peut plus jamais être vrai : un second clic redémarre au lieu d'arrêter. Toujours capturer l'état AVANT d'appeler la méthode qui le modifie. (Rencontré le 2026-09-26 sur `InstallationViewModel.ToggleIdentifyFixture`.)
- **Pousser un seul canal d'intensité à 255 ne rend pas forcément un appareil visible.** Sur un appareil RVB (ou RVBW…), le gradateur général (`Intensity`) n'est qu'un multiplicateur : si le rouge/vert/bleu sont à 0, l'appareil reste éteint même gradateur au maximum — contrairement à un projecteur à lampe classique où l'intensité seule suffit. Toute fonctionnalité qui doit rendre un appareil visible (Identifier, CMD-023) doit aussi pousser les canaux émetteurs de couleur (`AttributeInfo.IsEmitter`), pas seulement la famille `Intensity`. (Rencontré le 2026-09-26 sur `IdentifyRules.IdentifyChannels`, retour utilisateur avec un PAR réel qui ne s'allumait pas.)

## 12. Historique

| Date | Modification |
|---|---|
| 2026-09-26 | §11 « Pièges déjà rencontrés » (largeurs de saisie, `ObservableCollection.Clear()` sur un `SelectedItem` non annulable, bascule stop/démarre, intensité seule ne suffit pas sur un appareil RVB) — demande explicite de l'utilisateur après des retours de test en direct sur l'écran Installation. |
| 2026-09-26 | P3 : projet `Dmx.Patch` (Core, Persistence, Fixtures) pour l'installation, les sélections et les lieux (doc 13, doc 00 §7.2). |
| 2026-09-24 | Version initiale (Q21, Q22). |
| 2026-09-25 | Fiches d'exigences `docs/exigences/` (demande de l'utilisateur) : suivi par exigence façon Redmine, source du statut de la matrice 31. |
| 2026-09-25 | P1-P2 : projets d'interface par écran (D22), CA1822 en suggestion pour les projets d'interface (liaisons), CA1309 désactivée (tris affichés en français), matrice `tools/matrice-exigences.py`. |
| 2026-09-24 | P0 : projet `Dmx.Hosting` (D20), commandes de test Microsoft.Testing.Platform, projet de tests d'intégration, test d'architecture par réflexion (sans NetArchTest). |
