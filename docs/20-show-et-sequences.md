# 20 – Show (séquenceur de type Grafcet) et séquences en mesures

> Cahier des charges – module **Show** (préfixe `SHOW`). Phase principale : **P8**.
> Références : [02 §6](02-principes-et-architecture-fonctionnelle.md), [15](15-moteur-de-rendu.md), [16](16-scenes-et-effets.md), [17](17-couches-et-palettes.md), [19](19-audio-et-tempo.md), [22](22-directeur-automatique.md).

---

## 1. Pourquoi

Dans Daslight, l'enchaînement de scènes se faisait avec des scènes « d'aiguillage » ultra-courtes qui lançaient N scènes, dont
l'aiguillage suivant. C'est **un Grafcet codé avec des scènes** : ça marche, mais le nombre de scènes explose et la navigation devient
illisible. Le module Show sépare :

- le **contenu** : scènes, effets, séquences (docs 16-17) ;
- la **logique d'enchaînement** : étapes, transitions, conditions (ce document).

## 2. Séquences en mesures

### 2.1 Principe

Une **séquence** est un enchaînement de scènes placées sur des **pistes** (une piste par couche) et exprimé en **mesures / temps**.
Elle se rejoue sur **n'importe quel morceau** en suivant l'horloge musicale. Ex. « Montée 16 mesures » : 8 mesures de couleurs lentes,
4 mesures de mouvements rapides, 4 mesures de strobe croissant.

```
 Mesure :   1       5       9       13      17
 Couleurs   [Bleu lent   ][Arc-en-ciel  ][Blanc    ]
 Mouvements [Balayage    ][Cercle rapide          ]
 Effets                            [Strobe ↗      ]
 Intensité  [Vague       ][Plein                  ]
```

### 2.2 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| SHOW-001 | I | P8 | Une séquence a une **longueur** en mesures, une **mesure** (4 temps par défaut, 3 en option – GEN-025), et des **pistes** correspondant à des couches. | — |
| SHOW-002 | I | P8 | Placer des scènes sur les pistes par glisser-déposer, avec début et durée en mesures/temps ; grille magnétique (mesure, temps, ½ temps). | — |
| SHOW-003 | I | P8 | Sur une piste, un bloc lance sa scène dans la couche de la piste au début du bloc et l'arrête (ou laisse la suivante la remplacer) à la fin ; l'exclusivité de la couche est respectée. | Test moteur : commandes émises aux bons temps. |
| SHOW-004 | I | P8 | Blocs d'**actions** en plus des scènes : régler un master (valeur fixe ou rampe), fumée (rafale), flash, blackout court. | Rampe de master sur 4 mesures. |
| SHOW-005 | I | P8 | Modes de lecture : une fois, boucle ; **démarrage quantifié** (prochaine mesure / phrase). | — |
| SHOW-006 | I | P8 | Une séquence est lançable depuis le Live, une étape de show, ou le Directeur. | — |
| SHOW-007 | M | P8 | Aperçu : lecture de la séquence au simulateur avec un **métronome** (BPM réglable) sans musique. | — |
| SHOW-008 | S | P8 | Variation de la vitesse relative (séquence en « demi-temps » / « double temps »). | — |

## 3. Shows

### 3.1 Éléments

| Élément | Grafcet | Description |
|---|---|---|
| **Étape** | Étape | État actif du show. Porte des **actions**. Une étape **initiale** au moins. |
| **Action continue** | Action continue | Active tant que l'étape est active : lancer une scène ou une séquence (arrêtée quand l'étape se désactive), imposer un master, poser un verrou… |
| **Action mémorisée** | Action S / R | Effet qui persiste après la désactivation de l'étape : « lancer et laisser », « arrêter la couche X ». |
| **Action impulsionnelle** | Action à l'activation | Exécutée une fois à l'activation : flash, rafale de fumée, recalage. |
| **Transition** | Transition + réceptivité | Condition de passage d'une ou plusieurs étapes vers une ou plusieurs étapes. |
| **Divergence en OU** | Sélection de séquence | Plusieurs transitions sortantes : la première vraie l'emporte (par ordre de priorité), ou **tirage au sort pondéré** parmi les vraies. |
| **Divergence / convergence en ET** | Séquences simultanées | Activation parallèle de branches (ex. couleurs et mouvements évoluant à des rythmes différents) ; synchronisation au retour. |
| **Macro-étape** | Macro-étape | Étape qui contient un sous-show réutilisable (« Bloc refrain »). |

### 3.2 Réceptivités (conditions de transition)

| Famille | Conditions |
|---|---|
| **Temps** | Après N secondes d'activité de l'étape ; après N temps / N mesures ; au prochain début de phrase (4 / 8 / 16 mesures) |
| **Scène** | Fin de la scène / séquence X (lancée par l'étape) ; X a bouclé N fois |
| **Musique** | Morceau changé ; break ; drop ; montée ; silence ; énergie franchit un seuil (montant / descendant) ; niveau d'énergie = … ; style = … ; tempo dans une plage |
| **Manuel** | Bouton du Live, touche, pad MIDI (CMD-051) |
| **Aléatoire** | Probabilité p à chaque mesure (ex. 25 % de chances de changer à chaque phrase) |
| **Logique** | ET / OU / NON entre conditions ; « toujours vrai » (enchaînement immédiat) |

**Quantification** : toute transition peut être **synchronisée** au prochain temps, à la prochaine mesure ou à la prochaine phrase :
la condition est évaluée, puis le franchissement attend la frontière musicale. C'est ce qui rend les changements « musicaux ».

### 3.3 Règles d'évolution

| # | Règle |
|---|---|
| R1 | Une transition est **validée** quand toutes ses étapes amont sont actives ; elle est **franchie** quand elle est validée et que sa réceptivité est vraie (après quantification). |
| R2 | Le franchissement désactive les étapes amont et active les étapes aval **dans le même tick**. |
| R3 | Les transitions simultanément franchissables et indépendantes sont franchies ensemble. |
| R4 | En divergence OU, si plusieurs transitions sont vraies en même temps : **priorité** (ordre défini) ou **tirage pondéré** (réglage de la divergence). |
| R5 | Les actions continues des étapes désactivées sont arrêtées avec le fondu de leur couche ; si l'étape suivante relance la même scène, elle **n'est pas interrompue** (pas de coupure visible). |
| R6 | Une étape sans transition sortante est une **fin** : le show se termine quand toutes les étapes actives sont des fins (réglage : ou reboucle sur l'étape initiale). |

### 3.4 Exemple

```
        ┌──────────────┐
        │ 0  Intro     │  Couleurs: Bleu lent · Mouvements: Balayage lent
        └──────┬───────┘
               ┼  énergie ≥ Groove  (quantif. mesure)
        ┌──────┴───────┐
        │ 1  Couplet   │  Séquence « Groove 8 mesures » (boucle)
        └──────┬───────┘
      ┌────────┼────────────────────┐
      ┼ drop    ┼ montée              ┼ 16 mesures (60 % → 2a, 40 % → 2b)
 ┌────┴─────┐ ┌─┴──────────┐   ┌──────┴────┐  ┌───────────┐
 │ 3 Refrain│ │ 4 Montée   │   │ 2a Var. A │  │ 2b Var. B │
 │ Flash +  │ │ Séq. « Mon-│   └─────┬─────┘  └─────┬─────┘
 │ Strobe ↗ │ │ tée 16 m » │         ┼ 8 mesures       ┼ 8 mesures
 └────┬─────┘ └─┬──────────┘         └──▶ retour 1 ◀──┘
      ┼ break   ┼ drop ─▶ 3
      └──▶ 1
```

### 3.5 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| SHOW-020 | I | P8 | Éditeur graphique : poser des étapes, des transitions, les relier ; divergences / convergences OU et ET ; macro-étapes. | Construire l'exemple du §3.4. |
| SHOW-021 | I | P8 | Actions continues, mémorisées, impulsionnelles (§3.1) avec les commandes du catalogue (lancer / arrêter scène, séquence, couche ; master ; flash ; fumée ; verrou ; vitesse). | — |
| SHOW-022 | I | P8 | Réceptivités du §3.2, combinables en ET / OU / NON ; quantification par transition. | Tests par condition. |
| SHOW-023 | I | P8 | Exécution conforme aux règles R1 à R6. | Tests d'exécution en temps virtuel avec événements simulés. |
| SHOW-024 | I | P8 | **Validation** à l'édition : étape initiale absente, étape inatteignable, transition sans condition dans une boucle (risque de boucle infinie dans un même tick → interdit), scène référencée supprimée. | Jeu de shows invalides → erreurs attendues. |
| SHOW-025 | I | P8 | Un **seul show principal** actif à la fois (lancé à la main ou par le Directeur) ; lancer un show arrête le précédent (fondu). | — |
| SHOW-026 | I | P8 | **Supervision** : en Live et dans l'éditeur, étapes actives surlignées, transitions validées en attente de condition indiquées, temps restant avant quantification. | Revue. |
| SHOW-027 | I | P8 | **Mode simulation** dans l'éditeur : boutons pour provoquer les événements (drop, break, morceau changé, énergie…) et une horloge métronome, pour tester un show sans musique. | Tester l'exemple §3.4 sans musique. |
| SHOW-028 | M | P8 | Tirage pondéré : poids par transition ; option « éviter de refaire la même branche deux fois de suite ». | Distribution conforme sur 10 000 tirages (± 2 %). |
| SHOW-029 | M | P8 | Variables simples du show (compteurs : « nombre de refrains joués ») utilisables dans les conditions. | Après 3 refrains → variante finale. |
| SHOW-030 | M | P8 | Métadonnées du show pour le Directeur : styles visés, plage d'énergie, poids, durée max, rôle (principal / transition / attente / slow) (doc 22). | — |
| SHOW-031 | S | P8 | Shows **secondaires** parallèles (ex. un show « ambiance UV/fumée » indépendant du show principal). | — |

## 4. Modèle de données (résumé)

```
Séquence : id, nom, couleur, longueur (mesures), mesure (3/4, 4/4), pistes[couche → blocs[début, durée, scène | action]]
Show     : id, nom, métadonnées Directeur, variables, étapes[id, nom, initiale?, actions[type, commande, paramètres]],
           transitions[amont[], aval[], réceptivité (arbre ET/OU/NON), quantification, priorité, poids], macro-étapes
```

## 5. Tests

| Test | Type | Contenu |
|---|---|---|
| T-SHOW-01 | Unitaire | Séquences : commandes émises aux bons temps à 90, 120, 140 BPM ; 3/4 ; quantification. |
| T-SHOW-02 | Unitaire | Règles R1-R6 : divergence/convergence OU et ET, macro-étapes, fin, rebouclage. |
| T-SHOW-03 | Unitaire | Chaque réceptivité ; quantification ; combinaisons logiques. |
| T-SHOW-04 | Unitaire | Validation : jeu de shows invalides. |
| T-SHOW-05 | Statistique | Tirage pondéré et anti-répétition. |
| T-SHOW-06 | Intégration | Show complet joué sur un fichier audio de référence (événements réels), trames enregistrées et comparées à une référence. |
| T-SHOW-07 | Manuel | Construire 3 shows (calme, groove, énergique) et les répéter au simulateur sur une playlist. |

## 6. Notes de réalisation (P8)

| Sujet | Réalisation |
|---|---|
| Architecture (D37) | Projet `Luxia.Show` : modèle (`Sequence`, `ShowDefinition`), fichiers `séquences.json` et `shows.json` (doc 50 §12g, §12h), validation `ShowRules` (SHOW-024, branchée sur `valider`), exécution `Sequencer` / `SequenceRun` / `ShowRun`. Le moteur définit `ISequencer` et l'appelle à chaque tick, après ses commandes et avant l'avancement des scènes ; le séquenceur agit par les commandes du catalogue (origine `Show`). Un séquenceur par moteur (sortie, aperçu). |
| Événements musicaux (D38, révisé par D40 en P9) | **P9 : le changement de titre réel (lecture en cours de Windows) est la source de « au morceau suivant » tant qu'un morceau est suivi ; la détection ci-dessous reste le repli ; la condition « style » compare le style identifié (nom de famille, ou une partie de son nom).** Drop, break, montée, silence, reprise et niveau d'énergie arrivent au moteur par `IAudioFeed` ; « morceau changé » = reprise après silence ou saut de tempo de plus de 15 % hors octave. EVT-020 / 021 non publiés sur le bus ; EVT-024 publié au changement réel. Mode simulation : `SimulateMusicCommand` (CMD-053). |
| Séquences (D39) | Positions en mesures décimales ; bloc lancé quand la position de l'horloge franchit son début (sauf scène déjà en jeu), arrêté à sa fin sauf relais au même instant sur la même piste (même scène ou couche exclusive) ou `end: keep` ; boucle sans coupure ; rampes appliquées à chaque tick, journalisées au début et à la fin seulement ; vitesse ¼ à 4. |
| Shows | Une évolution par tick ; transition quantifiée **armée** quand sa condition devient vraie, franchie à la frontière si elle est encore validée ; divergence OU : priorité (ordre) ou tirage pondéré avec anti-répétition ; macro-étape = sous-show dont la fin valide les transitions sortantes ; R5 par comparaison des scènes et séquences voulues avant / après. **R6 précisée** : par défaut le show **tient** ses dernières étapes (`atEnd: hold`), sinon `stop` ou `restart`. Un show sans étape initiale ou avec une boucle sans condition est **refusé au lancement**. « Tout arrêter » (écran, MIDI) arrête aussi shows et séquences. |
| Écart | « X a bouclé N fois » n'existe que pour une séquence (`sequenceLoops`), pas pour une scène. Les réceptivités combinées (ET / OU / NON) s'écrivent dans le fichier ; l'éditeur les affiche sans les modifier. Verrous (SHOW-021) et Directeur : P10. Style : simulé seulement avant P9. |
| Interface (Q44 solution C, Q45) | Colonne « Shows » de l'écran de jeu (`ShowsColumnViewModel`), bandeau « Show en cours » (`ShowBandViewModel`, état publié dix fois par seconde par `Sequencer.State`), fenêtres d'édition en brouillon (`SequenceEditorViewModel` + frise `SequenceTimeline`, `ShowEditorViewModel` + cartes `StepCardViewModel` + diagramme `ShowDiagram`, essai `SimulationViewModel`) ; le brouillon est joué par les séquenceurs (`LuxiaRuntime.SetSequencingDraft`), sur l'aperçu seulement en aveugle. |
| Outils | `luxia-headless jouer --show / --sequence` ; verbes du scénario `show`, `arreter-show`, `sequence`, `arreter-sequence`, `forcer`, `simuler`, `energie`, `style` (doc 50 §13). |
| Tests | T-SHOW-01 `SequencePlaybackTests` ; T-SHOW-02 et 03 `ShowExecutionTests` ; T-SHOW-04 `ShowRulesTests` ; T-SHOW-05 `WeightedDrawTests` (10 000 tirages, ± 2 %) ; T-SHOW-06 remplacé par `ReferenceShowP8Tests` (événements **simulés** au lieu d'un fichier audio, trames `P8-shows.txt`) ; écrans : `SequencingScreensTests`. |

