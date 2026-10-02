# 60 – Ergonomie : charte d'interaction, composants, modules

> Chantier ouvert le 2026-09-27, à la validation de `v1.004` (branche `ergo/analyse`). **Analyse validée par l'utilisateur le
> 2026-09-27 : toutes les propositions du §8 (E1-E8) et du §8.1 (F1-F10) sont acceptées.** Suite : prototype technique puis
> maquettes (§7), dans une nouvelle discussion.

## 1. Cadrage

Demandé par l'utilisateur (doc 99, 2026-09-26 et 2026-09-27) :

1. **Harmonie des mécanismes** : application en direct ou par « Enregistrer », pastilles de modifications en cours, conditions
   d'affichage et de verrouillage des boutons… **les mêmes partout**.
2. **Tirer parti d'Avalonia en 2D** : plan Pan / Tilt, roue de couleurs, effets graphiques, icônes, panneaux déplaçables.
3. **Penser « module »** : pouvoir détacher et réagencer les parties de l'application à sa guise.

**Écarté pour le moment** : la 3D (décision de l'utilisateur). Le simulateur reste en 2D.

**Finalité rappelée par l'utilisateur (2026-09-27)** : le but est un **mode automatique** — écoute du titre en cours,
détermination du style, choix d'un show adapté à ce style, et **réactions en surcouche** à la musique (le morceau se calme :
changement d'ambiance ; il repart : un show est réactivé). La platine ne sert alors qu'à **quelques interventions** : couleur
fixe (temps mort), coup de strobe, blackout… L'utilisateur n'est ni DJ ni éclairagiste : **LuXia est une porte d'entrée vers
la création de shows par l'IA**, l'utilisateur ne faisant que des retouches. Mais les notions de fond (équipements, lieux,
palettes, scènes) doivent rester **compréhensibles**.

Ce qui en découle pour l'ergonomie : (a) l'écran le plus important à terme est celui du **spectacle automatique**
(ce qui joue, pourquoi, et quelques boutons d'intervention) ; (b) l'édition fine est secondaire mais doit être **claire** ;
(c) chaque notion doit s'expliquer d'elle-même (aide intégrée, vocabulaire unique).

Contraintes qui ne changent pas : Live toujours prioritaire et jamais interrompu (GEN-062), blackout et Grand Master toujours
visibles, tout se fait aussi sans souris pour le jeu (clavier, APC mini), et **l'IA construit les shows** (GEN-130 à 134) :
l'écran sert surtout à jouer, régler et vérifier, pas à tout saisir à la main.

## 2. Ce que l'on retient de Daslight (principes, pas copie)

Sources : `docs/Daslight4/` (manuel FR, MyDMX 3.0 = même famille, captures de l'utilisateur), `docs/Daslight 5/` (manuel v1.4,
guide rapide, plaquette).

| Principe | Daslight | Intérêt pour LuXia |
|---|---|---|
| **Trois écrans seulement** | D5 : *Setup* (patch), *Control* (programmer **et** jouer, fusion de Edit + Live de D4), *Touch* (interface perso) | Réduire la navigation : on joue et on corrige au même endroit |
| **Bouton de scène en deux zones** | Grande zone = jouer ; bande étroite à droite = sélectionner pour éditer ; clic droit = renommer, dupliquer, couleur… | Plus besoin d'aller dans un autre écran pour éditer la scène qui joue |
| **Banques = colonnes** | Scènes rangées en banques empilables, une scène à la fois par banque, suivante / précédente / pause par banque | C'est déjà nos **couches** (colonnes du Live) : confirme le choix |
| **Un seul sélecteur de modes d'édition** | `EDIT` (les réglages s'écrivent **dans la scène**, et sortent) · `👁 aveugle` (s'écrivent sans sortir) · `LIVE` (surcharges temporaires, non enregistrées) | Répond directement au flou « programmeur / aveugle / surcharge » vécu en P4-P5 |
| **Pastille par canal** | Point **vert** = canal utilisé par la scène ; **jaune** = surcharge live ; éteint = non utilisé (le canal n'agit pas) | Rend visible « qu'est-ce qui est enregistré, qu'est-ce qui est temporaire » |
| **Deux vues des mêmes réglages** | *Features* (onglets Intensité, Couleur, Position, Gobo, Faisceau…, avec grille XY, roue HSV, boutons de préréglages) et *Faders* (tous les canaux 0-255) | Nos outils du programmeur et notre Console, réunis dans un même panneau |
| **Plan 2D des appareils = la sélection** | Fenêtre en bas à gauche, partout : rectangle, lasso, trait, ½, ⅓, ¼, index, sélections enregistrées, bouton `FX` | Une seule sélection, partagée par tous les outils (au lieu des cases à cocher du programmeur) |
| **Limites dessinées** | Plage de gradateur (curseur min / max) et **rectangle de mouvement à 8 poignées** sur une grille Pan / Tilt | Exactement notre besoin « zones interdites » (inversé : zone permise ou interdite) |
| **Effets graphiques** | Move FX : forme dessinée sur la grille XY (points ajoutés, déplacés, pivotés) ; Colour FX : palette de couleurs éditable ; Curve FX : forme d'onde | Modèle pour P6 (effets) |
| **Réglages live par scène** | 4 molettes : Intensité, Vitesse, Phase, Taille, sans modifier la scène | Notre master de couche et notre vitesse, en plus riche |
| **Mixeur live par groupe** | Gradateur, teinte, saturation, strobe, plein, flash, blackout par groupe d'appareils | Idée forte pour le Live (et pour le mode auto) |
| **Affectation par surcouche** | Bouton « affecter MIDI / clavier / Touch » : tout ce qui est affectable se colore, on clique puis on appuie sur la touche | Réalise MIDI-008 (apprentissage) et LIVE-041 (raccourcis) avec **un seul** mécanisme |
| **Touch : écran composé** | Grille où l'on dépose boutons, faders, molettes, roue de couleur, grille XY ; Alt + clic sur n'importe quel contrôle l'y ajoute ; fenêtre détachable, plein écran | Notre « disposition personnalisable » (LIVE-006) |
| **Verrou** | Verrouiller Setup / Control / Touch, avec mot de passe | Utile en soirée (anti-fausse manipulation) |
| **Indicateurs discrets** | Petits voyants d'entrées (MIDI, clavier…) et barre de charge CPU en haut à droite | Notre barre d'état, à rendre plus parlante |

À ne **pas** reprendre : la densité extrême de Daslight (tout sur un écran, polices minuscules) et les fenêtres de réglage
MIDI très techniques. LuXia vise moins de fonctions visibles à la fois, mais toujours au même endroit.

## 3. Constat sur LuXia v1.004

Captures : `tools/Luxia.Tools.Captures` (8 écrans). Remarques de l'utilisateur depuis P3 : doc 99 et fiches.

| Écran | Ce qui va | Ce qui ne va pas |
|---|---|---|
| **Live** | Colonnes = couches, couleurs des scènes, pastilles d'état, clavier, APC | Beaucoup de place perdue (colonnes vides), palettes rapides et journal en bas peu lisibles, pas de réglage par scène, texte d'aide des touches en petit |
| **Scènes** | Tout y est (identité, lecture, étapes, programmeur, palettes) | **Tout est empilé** et défile ; le programmeur est un « brouillon » dont on ne sait pas s'il agit sur la sortie (il agit) ; « Charger / Remplacer / Fusionner / Nouvelle étape » : quatre notions pour une ; cases à cocher d'appareils au lieu d'un plan ; zones interdites dans une fenêtre obscure |
| **Console** | Faders par canal, moniteur de sortie, instantanés | 32 faders vides sans libellé lisible ; mode Appareils = liste qui défile sans indice (doc 99) |
| **Installation** | Patch complet, barre d'univers | Tableau de boutons répétés sur chaque ligne (8 boutons × N appareils) ; fiche d'installation peu lisible (doc 99) |
| **Bibliothèque** | Import, arborescence | Écran vide tant qu'on n'a rien choisi ; formulaire d'édition très long |
| **Simulateur** | Plan réel, sortie ou aperçu | Plan non partagé avec la sélection : on sélectionne ailleurs, on regarde ici |
| **Sorties** | État, test, enregistrement | Correct |
| **Fenêtres** | — | Mélange de modales et non-modales sans règle ; « Enregistrer / Annuler » dans certaines, écriture immédiate ailleurs |

Défauts **transverses** (ce sont eux que la charte doit régler) :

- **Trois façons de modifier** coexistent : écriture immédiate (écran Scènes, champs), brouillon à enregistrer (programmeur),
  fenêtre à valider (couches, zones, versions). Rien ne dit laquelle s'applique.
- **Rien ne signale ce qui est en cours** : surcharge du programmeur active, valeur non enregistrée, mode aveugle.
- **Boutons désactivés muets** : ils ne disent pas pourquoi.
- **Sélection d'appareils éparpillée** : cases du programmeur, sélections de la Console, sélections rapides du Live.
- **Place** : écrans en pile verticale, défilement, pas de panneaux repliables ni réagençables.

## 4. Charte d'interaction (proposition)

### 4.1 Trois modes d'édition, un seul sélecteur

Un sélecteur toujours visible (en-tête de la fenêtre, à côté du Grand Master) : **ÉDITION · AVEUGLE · LIVE**.

| Mode | Un réglage fait sur un appareil… | Sortie DMX | Pastille du paramètre |
|---|---|---|---|
| **LIVE** (par défaut) | est une **surcharge temporaire**, par-dessus les scènes ; rien n'est enregistré ; « Libérer » la retire | modifiée | 🟡 jaune |
| **ÉDITION** | s'écrit **tout de suite dans l'étape sélectionnée** de la scène sélectionnée (annulable, Ctrl+Z) | modifiée | 🟢 verte |
| **AVEUGLE** | s'écrit dans la scène comme en Édition | **inchangée** (aperçu au simulateur) | 🟢 verte + icône 👁 |

Conséquences : le **programmeur-brouillon disparaît** (plus de « Charger / Remplacer / Fusionner »), l'étape sélectionnée
est toujours ce qu'on modifie en Édition, et le mode LIVE est la surcharge d'aujourd'hui, nommée et visible. Un bandeau de
couleur (vert / bleu / jaune) rappelle le mode en permanence. Passer de LIVE à ÉDITION ou AVEUGLE demande une action
explicite ; en soirée, on peut **verrouiller** en LIVE (§4.6).

### 4.2 Quand est-ce enregistré ?

Une seule règle : **tout ce qui se règle dans un panneau s'enregistre immédiatement**, et Ctrl+Z / Ctrl+Y défont et refont
(100 niveaux, déjà en place pour les scènes, étendu à tout le projet). Plus de bouton « Enregistrer » dans les panneaux.

Seules exceptions, en **fenêtre modale** (le reste de l'application est grisé, boutons **Valider / Annuler** en bas à droite) :
les créations en plusieurs champs (nouvel appareil, nouvelle scène à partir d'un modèle…), les actions destructrices
(supprimer, rétablir une version) et les assistants. Une fenêtre **non modale** (outil flottant) n'a jamais de bouton
« Enregistrer » et le dit dans son titre (ex. « Zones de la lyre 1 — les modifications s'appliquent tout de suite »).

### 4.3 États visibles

| Signal | Où | Sens |
|---|---|---|
| Pastille 🟢 / 🟡 / ◯ | en tête de chaque paramètre (fader, outil) | dans la scène / surcharge live / non utilisé |
| Point `•` après un nom d'onglet | onglets Intensité, Couleur, Position… | cette famille contient des valeurs |
| Liseré de couleur | scène, bouton | scène qui joue (plein), sélectionnée pour édition (contour) |
| Pastille orange | bandeau d'état | la sûreté agit (avec décompte) |
| Voyant rouge clignotant | bandeau d'état | enregistrement de trames |
| Pastille grise barrée | bandeau d'état | sortie absente |

### 4.4 Boutons et commandes

- Un bouton **désactivé** a toujours une **infobulle qui dit pourquoi** (« Sélectionnez une scène », « Aucune lyre dans la sélection »).
- **Clic droit** = menu contextuel **partout** (scène, appareil, étape, palette, colonne) avec les mêmes verbes dans le même ordre :
  *Renommer, Dupliquer, Couleur…, (spécifiques), Supprimer*.
- **Maintien** (flash, strobe, fumée, blackout MIDI) : toujours signalé par une icône ⏵⏸ « maintenir ».
- Une action destructrice est en **rouge** et demande confirmation ; elle est défaisable quand c'est possible.

### 4.5 Couleurs, typographie, dimensions

Jeton de couleur unique par sens (défini une fois dans `App.axaml`, jamais en dur dans un écran) :
accent (sélection) bleu, joue = couleur de la scène, surcharge live jaune, édition vert, aveugle bleu clair, sûreté orange,
danger / blackout / enregistrement rouge, texte secondaire gris. Deux tailles de texte dans les panneaux (normal, secondaire),
une pour les titres ; espacement sur une grille de 4 px ; largeurs de champs selon doc 03 §11.

**Tailles minimales des cibles** (ERG-035, demande de l'utilisateur : « éléments trop petits »), à vérifier en 1366 × 768 :
boutons de scène ≥ 44 px de haut (hors mode « resserré », choix de l'utilisateur), boutons ≥ 32 px, bande ✎ et boutons de
couche ≥ 32 px, molettes ≥ 70 px, glisseurs et faders : cible de saisie ≥ 32 px de large.

### 4.6 Verrou et Live

« Verrou soirée » : en LIVE, l'édition, l'installation et les suppressions sont bloquées (mot de passe facultatif). Le
Live garde tout ce qui sert à jouer. Idée reprise de Daslight, utile pour le mode automatique.

### 4.7 Affectations (MIDI, clavier, écran Touch)

Un seul mécanisme : bouton « Affecter » (MIDI, clavier) dans l'en-tête ; tout contrôle affectable se colore ; on clique sur
le contrôle, puis on appuie sur la touche / le pad / le fader. La liste des affectations s'édite dans un panneau. Réalise
MIDI-008 et LIVE-041, remplace l'édition à la main de `midi.json` pour l'utilisateur (le fichier reste la référence pour l'IA).

### 4.8 Déclencheurs : n'importe quelle entrée, une ou plusieurs actions

Constat : aujourd'hui une affectation relie **un** contrôle de l'APC à **une** action (`midi.json`), la grille suit les
colonnes du Live (8 lignes, puis pages avec Shift), le clavier est figé, l'entrée DMX n'existe pas.

Proposition (idée reprise de Daslight, où une même touche peut commander plusieurs éléments) : un **déclencheur** relie
**une entrée** à **une liste d'actions**.

| Élément | Choix |
|---|---|
| Entrée | touche du clavier (avec ou sans modificateur), note / fader MIDI de n'importe quel contrôleur, **canal DMX entrant** (plus tard : il faut une entrée DMX — le futur renifleur Leonardo pourra en servir), événement du mode automatique (P10) |
| Actions | lancer / arrêter / basculer une ou **plusieurs** scènes, flash, blackout, strobe, fumée, figer, master d'une couche, Grand Master, palette rapide sur une sélection, **appeler un « look »** (voir ci-dessous) |
| Comportement | appui (bascule), maintien (flash : actif tant qu'on appuie), valeur (fader) ; pour un fader : plage min / max |
| Retour | LED du contrôleur selon l'état (déjà en place pour l'APC) |

Un **look** (ou « préréglage de soirée ») est une liste nommée d'actions — par exemple « Temps mort » = arrêter les effets,
lancer « Ambre – couleur seule », baisser l'intensité à 40 % — appelable par un déclencheur, par le Live ou par le mode
automatique. C'est le même objet qui servira aux **commandes en surcouche** du mode auto (« la musique se calme → look
calme »).

La disposition par défaut de l'APC (grille = colonnes) reste proposée, mais comme **un ensemble de déclencheurs parmi
d'autres**, modifiable par la surcouche « Affecter » (§4.7). `midi.json` devient `declencheurs.json` (toutes les entrées).

### 4.9 Écran de jeu et fenêtre d'édition (chantier « Contrôle 2 », ERG-032 à ERG-039)

**Remplace le modèle à trois modes du §4.1** (décision de l'utilisateur, 2026-09-29, Q38 ; maquettes 5 à 8 validées ; chantier **validé** le 2026-09-30, v1.007 ; analyses de fin de chantier : [ergonomique](chantiers/analyse-ergonomique-fin-controle-2.md), [de code](chantiers/analyse-code-fin-controle-2.md)). Deux
lieux évidents au lieu d'un sélecteur de modes :

| Lieu | Ce qu'on y fait | Ce qu'il contient |
|---|---|---|
| **Écran de jeu** (Contrôle) | Jouer. Aucun mode à garder en tête. | Colonnes (grandes cibles), **Groupes dimmer**, Looks, Pilote automatique, Journal ; Stop / Tout stopper, Verrou soirée. Panneaux ancrables (une seule disposition, `jeu.json`). |
| **Fenêtre d'édition d'une scène** | Concevoir. Ouverte par la bande ✎, jamais toute seule ; non bloquante ; sur un second écran si on veut. | Plan, Réglages, Effets, Propriétés et étapes en disposition fixe ; brouillon ; Appliquer / Valider / Annuler ; case Aveugle. |
| Configuration | Installer. | Installation (dont « Gestion des dimmers »), Bibliothèque, Sorties. |

- **LIVE** n'est plus un mode : c'est l'état normal de l'écran de jeu. Les seules retouches en direct sont les **dimmers de groupe**
  (et les niveaux de couches), temporaires, jamais enregistrées ; un bandeau jaune les rappelle avec « Libérer tout » (Échap).
- **ÉDITION** = « la fenêtre d'édition est ouverte ». **AVEUGLE** = case à cocher de la fenêtre (aperçu au plan seulement).
- **Fader de couche** = **niveau** de la couche (multiplie ce qu'elle envoie) ; pas de réglage de couche propre à la scène (Q38 point 4).
- **Dimmers de groupe** : arbre de groupes (Installation › Gestion des dimmers), règle proportionnelle, seconde platine MIDI (doc 18b).
- Verrou soirée : la fenêtre d'édition ne s'ouvre pas ; jouer, arrêter et retoucher les dimmers restent permis.

### 4.10 Tempo, écran Audio et volet « Au rythme » (essai P7, lot ergonomique)

Décisions de l'utilisateur à l'essai de P7 (2026-10-01) ; analyse : [chantiers/analyse-ergonomique-p7.md](chantiers/analyse-ergonomique-p7.md) §9.

- **Un seul interrupteur « 🎧 Audio »** dans le bloc BPM : gris = tempo réglé à la main, bleu = l'horloge suit la musique écoutée. Il remplace
  la case « Écoute », le bouton « Fixer » et l'étiquette de source. L'écran Audio porte la même case (même réglage, pas deux).
- **Audio actif** : le champ BPM, TAP, − et + sont grisés (le tempo vient du son) ; **×2, ÷ 2 et « 1 ici » restent actifs** (ils corrigent
  l'analyse). La **confiance** (vert / orange / rouge) s'affiche à droite de l'interrupteur, à largeur fixe.
- **BPM saisissable** : taper une valeur puis **Entrée** (ou quitter le champ) fixe le tempo ; l'affichage ne l'écrase pas pendant la frappe.
- **TAP s'allume** un instant à chaque frappe, au bouton comme à la touche **T**.
- **Rappel du tempo en lecture seule** (BPM + quatre voyants, le 1 en orange) dans le **Simulateur** et l'écran **Audio** : même composant
  (`TempoGlance`), même présentation partout.
- **Écran Audio en une page** à 1920 × 1080, sans défilement : à gauche le direct (écoute, tempo, énergie, événements) ; à droite les
  **niveaux en colonnes verticales** et, **repliés**, Réglages et Calibration.
- **Volet « Au rythme »** : on ne montre que ce qui sert. La **fréquence** n'apparaît que si un événement fait avancer l'étape ; l'**horloge
  propre** si la scène est musicale ; la **vitesse selon l'énergie** quand la durée des étapes ou des effets en dépend. L'en-tête résume
  l'essentiel (« Au rythme : étapes sur le temps, ×2 · démarrage à la prochaine mesure »).
- **Fréquence** (remplace « Tous les N ») : ×4, ×2, ×1, ÷ 2, ÷ 4, ÷ 8. ×2 et ×4 passent deux ou quatre étapes par temps (ou par mesure) ; ÷
  n'en passe qu'une toutes les N fois ; ×2 et ×4 n'existent pas pour les impulsions des basses et des aigus.
- **Reporté** : titre du morceau (avec P9), mesure à trois temps (avec P8, GEN-025).

### 4.11 Shows et séquences (P8, Q44 solution C, Q45 ; maquettes 9 à 12)

- **Jouer** : une colonne **« ▶ Shows »** à gauche des couches, mêmes gestes que les scènes (clic = lancer / arrêter, bande ✎ = éditer,
  clic droit = Éditer, Renommer, Dupliquer, Supprimer) ; un seul show principal à la fois (le nouveau remplace l'ancien).
- **Superviser** : un **bandeau « Show en cours »** sous le bloc BPM, présent seulement quand un show ou une séquence joue : étape
  active, prochaines transitions (orange quand elle va partir), séquence en cours, **⏭ Forcer ▾**, **■ Arrêter**, **▾ Détail** (parcours,
  actions de l'étape, chaque transition avec son état et son bouton Forcer, show secondaire).
- **Éditer** : fenêtres non bloquantes, même charte que la fenêtre d'édition des scènes (brouillon, Appliquer / Annuler / Valider,
  Ctrl+Z, case Aveugle, croix qui demande avant de perdre un brouillon). Séquence : bibliothèque à gauche (glisser-déposer), frise au
  centre (grille mesure / temps / ½ temps, zoom, Suppr), propriétés à droite. Show : cartes à gauche (une par étape, ses transitions
  dessous), diagramme dessiné automatiquement à droite, panneau « Essai sans musique » en bas.

## 5. Composants communs (catalogue)

Tous dans `Luxia.UI.Controls`, dessinés en 2D Avalonia, chacun avec son test et sa capture dans une **galerie** (écran de
développement listant tous les composants dans tous leurs états, source des captures de référence).

| Composant | Usage | Remplace |
|---|---|---|
| **Fader** vertical (pastille d'état, valeur, clic droit = préréglages, double-clic = saisie) | Console, faders d'appareil, masters | Faders actuels hétérogènes |
| **Grille Pan / Tilt** (point = visée, glisser, fin à la molette, plusieurs appareils en relatif, rectangles de zones à 8 poignées) | Position, zones interdites, limites, effets de mouvement | Faders Pan/Tilt + fenêtre des zones |
| **Sélecteur de couleur** (carré teinte × saturation + curseur d'intensité, favoris, palettes du projet, blanc automatique) | Couleur des appareils, palettes, mixeur | Outil couleur du programmeur |
| **Molette** (dial) | Réglages live (vitesse, phase, taille), mixeur | Curseurs horizontaux |
| **Bouton de scène** (zone jouer / zone éditer, couleur, progression, état) | Colonnes du Live et de l'édition | Boutons du Live + liste de l'écran Scènes |
| **Colonne de couche** (en-tête, ⏸ ◀ ▶ +, master, arrêt) | Contrôle | Colonnes du Live |
| **Plan des appareils** (sélection rectangle / lasso / trait, ½ ⅓ ¼, index, sélections, zoom) | Partout où l'on choisit des appareils ; même dessin que le simulateur | Cases à cocher, listes de sélection |
| **Bande d'étapes** (glisser pour réordonner, multi-sélection, temps affichés graphiquement) | Scène en édition | Liste d'étapes + boutons |
| **Pastilles d'état** et **sélecteur de mode** | Bandeau | Pastilles actuelles |
| **Panneau de propriétés** (sections repliables, étiquettes alignées) | Scène, couche, appareil | Formulaires en grille |

## 6. Modules détachables et réagençables

**Proposition** : un système d'**ancrage** (bibliothèque libre *Dock* pour Avalonia, à valider par un prototype). Chaque
module devient un **panneau** que l'on peut coller à gauche / droite / bas, empiler en onglets, **détacher dans une fenêtre**
(deuxième écran) ou masquer ; la disposition est enregistrée dans les préférences, avec des dispositions prêtes :

| Disposition | Panneaux |
|---|---|
| **Contrôle** (jouer et corriger) | Colonnes des couches (centre) · Propriétés de la scène (droite) · Plan des appareils + Réglages des appareils (bas) · Journal (onglet bas) |
| **Installation** | Bibliothèque (gauche) · Patch (centre) · Plan / simulateur (droite) · Sorties (onglet) |
| **Spectacle** | Colonnes des couches en grand · mixeur · bandeau d'état ; tout le reste masqué ; verrou possible |
| **Deux écrans** | Contrôle sur l'écran principal, simulateur ou colonnes détachés sur le second |

Panneaux prévus : Colonnes (couches et scènes) · Propriétés (scène, couche) · Plan des appareils (sélection) · Réglages des
appareils (Intensité, Couleur, Position, Faisceau, Autres, Faders) · Palettes · Mixeur live · Journal · Simulateur ·
Console (canaux bruts) · Patch · Bibliothèque · Sorties · Lieux et zones.

Conséquence : les « écrans » Live et Scènes **fusionnent** en une disposition « Contrôle » (comme Daslight 5), le Live
restant une disposition dépouillée de la même chose.

## 7. Démarche proposée

1. **Validation de cette analyse** (décisions du §8).
2. **Prototype technique** (1 session) : ancrage *Dock* + deux composants graphiques (grille Pan/Tilt, sélecteur de couleur)
   + galerie de composants. But : vérifier Avalonia 12, les performances, l'enregistrement de la disposition.
3. **Maquettes** écran par écran (images), validées par l'utilisateur avant développement, en commençant par
   **Contrôle** (scènes + programmeur + zones), puis Live / Spectacle, puis Installation.
4. **Développement par lots**, dans une nouvelle discussion (fiches d'exigences, tests, captures de référence) :
   charte et composants → Contrôle → Live / Spectacle → Installation / Bibliothèque → Affectations.

## 8. Décisions à prendre avec l'utilisateur

| # | Question | Décision (utilisateur, 2026-09-27 : « d'accord pour tout ») |
|---|---|---|
| E1 | Modèle d'édition : trois modes ÉDITION / AVEUGLE / LIVE (§4.1), fin du programmeur-brouillon ? | Oui |
| E2 | Fusion Scènes + Live en une disposition « Contrôle » (bouton de scène à deux zones), le Live devenant la disposition « Spectacle » ? | Oui |
| E3 | Enregistrement immédiat + annuler partout ; modales réservées aux créations, destructions et assistants (§4.2) ? | Oui |
| E4 | Ancrage de panneaux détachables (*Dock*), dispositions enregistrées, deuxième écran ? | Oui, après prototype |
| E5 | Un plan des appareils unique pour sélectionner partout (§5) ? | Oui |
| E6 | Affectation MIDI / clavier par surcouche colorée (§4.7) ? | Oui |
| E7 | Verrou soirée (§4.6) ? | Oui, simple |
| E8 | Ordre : prototype, puis maquettes Contrôle, puis le reste (§7) ? | Oui |

### 8.1 Décisions plus fines (à trancher avant les maquettes)

| # | Question | Décision (utilisateur, 2026-09-27 : « d'accord pour tout ») |
|---|---|---|
| F1 | **Déclencheurs** (§4.8) : une entrée → plusieurs actions ; clavier, MIDI, DMX (plus tard) ; « looks » réutilisés par le mode auto | Oui |
| F2 | Surcharge LIVE quand une scène qui utilise le même canal est lancée : la surcharge **reste** (jusqu'à « Libérer ») ou **cède** à la scène ? (Daslight propose les deux) | Reste — c'est une intervention voulue |
| F3 | Annuler / rétablir : sur **tout le projet** (installation, scènes, palettes, couches) mais **jamais** sur ce qui a été joué (lancer une scène ne s'annule pas) | Oui |
| F4 | Valeurs affichées : **%** partout par défaut, **0-255** au choix (bascule dans le panneau Faders), degrés pour Pan / Tilt | Oui |
| F5 | Vocabulaire : on garde **couche** (Daslight dit « banque »), **sélection** (groupe d'appareils), **palette**, **lieu**, **look** ; chaque mot a une **infobulle d'explication** et une entrée du glossaire | Oui |
| F6 | **Aide intégrée** : un bouton « ? » par panneau qui explique la notion en 3 lignes (ce que c'est, à quoi ça sert, un exemple) ; pas de visite guidée | Oui |
| F7 | Zones de mouvement : garder les **zones interdites** et ajouter une **zone permise** (limites de la lyre, comme Daslight), dessinées sur la même grille | Oui, zone permise en option |
| F8 | Taille de l'interface : un réglage « échelle » (100 / 125 / 150 %) pour écran tactile ou lecture de loin | Oui |
| F9 | Écran distant (tablette, téléphone, comme Daslight Remote) | Plus tard, pas dans ce chantier |
| F10 | Place réservée dès maintenant au **panneau du pilote automatique** (titre, style, show choisi, raison, boutons d'intervention = looks) dans la disposition « Spectacle » | Oui, vide jusqu'à P10 |

## 9. Exigences du chantier

Famille **ERG**, phase « ERG » (chantier ergonomique, entre P5 et P6). Complétée au fil du chantier ; une fiche par exigence
(`docs/exigences/ERG-*.md`). Les exigences reportées de P5 (LIVE-006/007/011/041, MIDI-008/009, GEN-057/074, INST-070/071,
CONS-061) gardent leur identifiant et leur fiche.

| ID | Prio | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| ERG-001 | I | ERG | **Ancrage de panneaux** (bibliothèque *Dock*, Avalonia 12) : chaque panneau se colle à gauche / droite / haut / bas, s'empile en onglets, se **détache dans une fenêtre** (deuxième écran), se replie sur un bord ou se ferme ; un panneau fermé ou absent se réaffiche par le menu **Panneaux**, à sa place d'origine ; un panneau **détaché** y est marqué « (détaché : le remettre en place) » et y revient, comme un panneau fermé depuis sa fenêtre (son groupe, retiré par Dock s'il s'était vidé, est recréé contre son voisin livré) ; la fenêtre vide se ferme ; **double-clic** sur la barre de titre d'une fenêtre détachée = agrandir / restaurer. Deux dispositions prêtes : **Contrôle** et **Spectacle** (§6). | Prototype (§7.2) : manipulations faites par l'utilisateur ; test automatique du réaffichage. |
| ERG-002 | I | ERG | **Enregistrement de la disposition** : sans bouton, dès qu'elle change (vérification toutes les 2 s) et à la fermeture ; reprise au démarrage, fenêtres détachées et panneaux fermés compris ; une par disposition prête ; « Rétablir la disposition » revient à celle livrée ; un fichier illisible est mis de côté et la disposition livrée reprend, avec un message, sans jamais bloquer le démarrage. | Tests automatiques (écriture / relecture / fichier illisible) ; essai utilisateur (fermer, rouvrir). |
| ERG-003 | I | ERG | **Grille Pan / Tilt** (composant commun, §5) : un point par appareil, en degrés ; un clic amène la sélection sous le curseur (plusieurs appareils : leur centre, écarts conservés, sans écraser un appareil contre le bord) ; Maj + glisser = réglage fin ; molette = Tilt fin (Maj : Pan, Ctrl : ×10) ; flèches ; **zones interdites** (rouges) et **zone permise** (limites, extérieur assombri, F7) dessinées, déplacées et redimensionnées par **8 poignées**, Suppr pour retirer. | Tests des calculs ; galerie ; essai utilisateur. |
| ERG-004 | I | ERG | **Sélecteur de couleur** (composant commun, §5) : carré teinte × saturation, barre d'intensité, valeurs lisibles (°, %), **favoris** (clic = reprendre, clic droit = retirer, « + » = ajouter). | Tests des conversions et du découpage ; galerie ; essai utilisateur. |
| ERG-005 | M | ERG | **Galerie des composants** : chaque composant commun dans chacun de ses états, manipulable ; source des **captures de référence** (`LuXia-Prototype --captures <dossier>`, sans écran). | Captures produites et relues. |
| ERG-006 | I | ERG | **Mesures du prototype** : images par seconde, demandes par seconde des composants, mémoire, nombre de fenêtres, affichées dans un panneau ; bilan (fluidité d'un glisser, deux écrans) consigné au §10. | Relevé fait avec l'utilisateur. |
| ERG-008 | I | ERG | **8e couche par défaut « Libre »** (★, priorité 7, sans famille attendue, avant Flashs) : les 8 faders de couche de l'APC ont chacun une couche (C2). | Test des couches par défaut. |
| ERG-009 | I | ERG | **Identité visuelle** (dossier de l'utilisateur, `docs/identite`) : icône de l'exécutable et de la fenêtre, logo dans la fenêtre de démarrage, dans « À propos » et en tête de la navigation, logo du README. | Captures relues. |
| ERG-010 | I | ERG | **Mode LIVE** de l'écran Contrôle : un réglage fait sur les appareils sélectionnés est une surcharge temporaire, gardée quand une scène sur les mêmes canaux est lancée (F2), retirée par « Libérer la sélection » ou « Libérer tout » (Échap) ; ÉDITION et AVEUGLE refusés, avec la raison, tant qu'aucune scène n'est choisie ; retour en LIVE à l'ouverture d'un projet (C9). | Tests. |
| ERG-011 | I | ERG | **Mode ÉDITION** : le réglage s'écrit tout de suite dans l'étape choisie de la scène éditée ; l'étape est montrée sur la sortie (C7) ; un geste = une entrée d'annulation, écrite 0,5 s après le dernier mouvement ; « allumer en coloriant » (MOT-041) ; « Retirer de l'étape ». | Tests. |
| ERG-012 | I | ERG | **Mode AVEUGLE** : écrit comme ÉDITION, la sortie ne change pas ; l'étape va au moteur d'aperçu, que le plan montre (GEN-063). | Tests. |
| ERG-013 | I | ERG | **Zones du lieu dans l'écran Contrôle** (onglet Position) : dessinées, déplacées, ajustées, retirées sur la grille ; un clic prend la **plus petite** zone sous le curseur (seules les poignées de la zone choisie passent avant) ; **liste** des zones cliquable (choisir une zone recouverte), **nom** modifiable et **Supprimer** ; propres au lieu, valables pour toutes les scènes ; bandeau « ZONES » (C4) ; annulables. | Tests. |
| ERG-014 | I | ERG | **Plan des appareils = la sélection** (E5, SIM-010) : clic, Ctrl + clic, rectangle, sélections rapides (tous, par catégorie, enregistrées), ½ ⅓ ¼, inverser, aucun ; couleurs réellement émises (aperçu en AVEUGLE). | Tests. |
| ERG-015 | M | ERG | **Bande d'étapes** : cases proportionnelles aux durées (fondu dégradé, maintien plein), étape choisie entourée à la couleur du mode, étape jouée marquée ▶, choix au clic ou aux flèches. | Tests du découpage. |
| ERG-016 | I | ERG | **Propriétés de la scène éditée** : nom, couleur, couche, vitesse, enchaînement, fin, fondus, visible en Live, notes ; étapes (ajouter, dupliquer, déplacer, supprimer, nom, fondu, maintien) ; contenu de l'étape lisible ; tout enregistré à la saisie et annulable. | Tests. |
| ERG-017 | I | ERG | **Zone permise** (F7) : `"allowed": true` sur une zone de `lieux.json` ; le moteur ramène la cible dans la zone permise en évitant les zones interdites (l'extérieur devient des bandes interdites, calculées au chargement) ; plusieurs zones permises : leur intersection. | Tests moteur, compilation, fichier. |
| ERG-018 | I | ERG | **Colonnes de l'écran Contrôle** : toutes les couches par priorité (sauf masquées), toutes leurs scènes (masquées du Live estompées) ; bouton à deux zones (jouer / ✎ ; ✎ rouvre Propriétés si le panneau est fermé) ; ◀ ▶ (grisés si la scène qui joue n'a qu'une étape) ■ (net, ou fondu de sortie de la scène) et master par couche ; barre d'avancement pour une scène à plusieurs étapes ; « + scène » ; clic droit : Éditer, Renommer, Dupliquer, Couleur, Couche, Montrer / masquer dans le Live, Supprimer (annulable). | Tests. |
| ERG-019 | I | ERG | **Réglages des appareils** : onglets Intensité, Couleur (sélecteur, émetteurs, palettes, « + » = nouvelle palette), Position (grille, palettes, zones), Faisceau et autres (curseurs, plages nommées) ; onglet disponible selon la sélection ; pastilles 🟡 / 🟢 / ◯ avec leur légende ; avertissement « Intensité à 0 % : la couleur ne se verra pas » ; valeur demandée gardée à l'écran avant la réponse du moteur. | Tests. |
| ERG-020 | I | ERG | **Démonstration** : `tools/generer-demo-controle.py` fabrique `samples/Démo Contrôle` (couche Libre garnie, étapes nommées, zones permises des lyres) et le guide `docs/demos/ERG-controle.md` déroule l'essai. | Essai de l'utilisateur. |
| ERG-021 | I | ERG | **Verrou soirée** (E7, §4.6), bouton « 🔒 Verrou soirée » de l'écran Contrôle : revient en LIVE et refuse ÉDITION, AVEUGLE, toute modification de scène (propriétés, étapes, menu contextuel, nouvelle scène), des zones, et annuler / rétablir, en disant pourquoi ; jouer et retoucher en direct restent possibles. Sans mot de passe (« Oui, simple »). | Test. |
| ERG-022 | M | ERG | **Taille de l'interface** (F8) : menu Affichage → 100 / 125 / 150 %, gardée dans les préférences du poste (`uiScale`) ; les fenêtres détachées gardent leur taille. | Test des préférences ; capture à 125 %. |
| ERG-023 | I | ERG | **Looks** (F1, §4.8) : `looks.json` (lancer / arrêter des scènes, arrêter une couche, tout arrêter, masters) ; un look n'est pas une bascule (re-cliquer le rejoue) ; **pas de Grand Master** dans une capture (il reste à l'opérateur) ; la capture remet le master de chaque couche où elle relance une scène ; panneau Looks : jouer d'un clic, « Capturer ce qui joue », mettre à jour, renommer, couleur, supprimer ; verrou : jouer seulement ; `valider` signale une référence introuvable. Touches **F1 à F12** = looks 1 à 12 (écran Contrôle) ; déclencheurs MIDI et entrée DMX : à venir. | Tests. |
| ERG-024 | I | ERG | **Disposition Spectacle** (§6, F10) à côté de Contrôle, chacune enregistrée à part : colonnes en grand, panneau **Pilote automatique** (place réservée jusqu'à P10, avec les looks comme interventions), Looks, Journal. | Tests ; capture. |
| ERG-025 | M | ERG | **Scènes resserrées** : bouton « ☰ Resserré » sur la ligne du « ? » des Colonnes ; boutons de scène sur une ligne, environ moitié moins hauts, nom coupé par « … » ; l'infobulle d'une scène donne son nom complet (et son état) ; gardé dans les préférences du poste (`compactScenes`). | Test des préférences ; capture. |
| ERG-026 | I | ERG | **Stop et Tout stopper** en tête de l'écran Contrôle : « ■ Stop » arrête toutes les scènes sauf les couches protégées (Ambiance par défaut, réglable dans Couches…, colonne Protégée) ; « ■ Tout stopper » arrête tout ; permis sous le verrou soirée. | Test. |
| ERG-027 | M | ERG | **Marges** : la ligne du « ? » de chaque panneau porte sa barre d'outils (plus de ligne réservée au seul « ? ») ; menu et Grand Master / Blackout sur une seule ligne (le menu suit la taille de l'interface) ; Journal à interligne serré ; sélections rapides du plan sur trois lignes au plus ; titre du Pilote non répété. | Captures relues. |
| ERG-028 | M | P6 | **Molette** (composant commun, §5) : arc de la valeur à la couleur du mode, valeur et libellé ; toute la surface répond ; glisser vers le haut / le bas (200 pixels = toute la course), molette de la souris ± un pas (Maj : × 10), flèches ; **double-clic ou chiffre tapé = saisie au clavier** ; clic droit = saisie ou valeur par défaut (celle du modèle de l'effet) ; cadre de focus discret ; liée dans les deux sens (révisé à l'essai P6). | Tests ; captures. |
| ERG-029 | I | P6 | **Panneau Effets** de l'écran Contrôle (doc 16 §6), en onglet à côté de Réglages des appareils : à gauche la bibliothèque (modèle, « + Ajouter à la sélection », cellules) et les effets de l'étape ; à droite l'effet choisi : dessin animé (courbe, plan Pan / Tilt carré ou bande de couleurs, un point par appareil, le premier en blanc), forme, attribut, molettes (cycle, taille, centre, décalage, durée allumée), répartition, sens, centre de mouvement (palette), thème ou couleurs, relatif, cellules, « Reprendre la sélection du plan », « Enregistrer comme modèle ». Ordre des membres : de gauche à droite sur le plan (colonnes d'un mètre), puis ordre du patch. Écrit en ÉDITION / AVEUGLE seulement ; l'étape est jouée par le moteur (CMD-017) pour voir l'effet tout de suite. Ajouté de lui-même à une disposition enregistrée par une version antérieure. | Tests ; capture « Contrôle - Effets ». |
| ERG-030 | I | P6 | **En-têtes de couche toujours visibles** (demande de l'utilisateur, essai P6) : dans les Colonnes, le nom de la couche, ◀ ▶ ■ et le master restent en haut ; seules les scènes défilent verticalement ; **un ascenseur par colonne** (idée de l'utilisateur, essai 1.006.053) ; un défilement horizontal (colonnes trop nombreuses) déplace en-têtes et scènes ensemble, alignés. | Capture 1366 × 768 ; essai. |
| ERG-031 | M | P6 | **Éditeur de thèmes** (essai P6) : liste « Thème » avec « Aucun », boutons **+** (nouveau), **✎** (modifier) et **🗑** (retirer) à côté ; fenêtre « Thème de couleurs » : nom, crans (ajouter, retirer, monter, descendre, au moins deux), couleur au sélecteur commun ou en saisie libre « #RRGGBB » ; les thèmes livrés ne se modifient ni ne se retirent. Bibliothèque d'effets gérée à l'écart des boutons courants (« Bibliothèque ▾ » : retirer un modèle, rétablir les modèles livrés). « ⇠ Déplacer / Déplacer ⇢ » pour l'ordre des étapes ; « ▶ Lancer » repasse en LIVE depuis l'ÉDITION. | Tests. |
| ERG-007 | I | ERG | **Maquettes de la disposition Contrôle** (§7.3) : images rendues par Avalonia avec les vrais composants et des données fictives (modes LIVE / ÉDITION / AVEUGLE, scène en édition, zones), validées par l'utilisateur **avant** tout développement des écrans. | Validation de l'utilisateur. |
| ERG-032 | I | ERG2 | **Écran de jeu** (chantier « Contrôle 2 », remplace ERG-010/011/012 et le sélecteur de modes) : pas de mode ; peu de panneaux, grands (Colonnes, Groupes dimmer, Looks, Pilote, Journal) ancrables et détachables ; les retouches en direct (masters, dimmers, couleur rapide) sont temporaires (« Libérer ») ; verrou soirée gardé ; une seule disposition (Contrôle / Spectacle fusionnées, ERG-001/024 reformulées). | Maquette validée ; tests. |
| ERG-033 | I | ERG2 | **Fenêtre d'édition de scène** : ouverte à la demande (✎, jamais toute seule), non bloquante, déplaçable sur un second écran ; travaille sur une **copie** (brouillon) ; contient Plan, Réglages, Effets, Propriétés, Étapes en disposition fixe ; **Valider** enregistre (et rejoue la scène si elle joue), **Annuler** ou fermeture = retour à l'état d'origine sans confirmation ; un geste = une annulation (Ctrl+Z dans le brouillon) ; ouvre les sous-éditeurs (thème, effet). | Maquette validée ; tests. |
| ERG-034 | I | ERG2 | **Aperçu du brouillon** : hors aveugle, le brouillon est montré sur la sortie (CMD-017) ; **Appliquer** met à jour la scène sans fermer ; case **Aveugle** = aperçu au plan seulement, sortie inchangée (reformule GEN-063, SCN-035) ; l'état d'origine est restauré à l'annulation. | Tests. |
| ERG-035 | M | ERG2 | **Tailles minimales des cibles** (charte, §4.5) : boutons de scène ≥ 44 px de haut, boutons ≥ 32 px, molettes ≥ 70 px ; vérifiées en 1366 × 768 sur l'écran de jeu et la fenêtre d'édition. | Captures relues. |
| ERG-036 | I | ERG2 | **Groupes d'appareils (arbre)** : onglet « Gestion des dimmers » de l'écran Installation ; groupes imbriqués (Groupe 1 → 1.1, 1.2…), **un appareil dans un seul groupe** ; appareil non assigné = groupe racine implicite, nommable ; renommer, déplacer, supprimer (les appareils reviennent au parent) ; choix des groupes qui ont un dimmer ; enregistré dans le projet (`groupes.json`), distinct des sélections. | Tests ; maquette. |
| ERG-037 | I | ERG2 | **Dimmers de groupe** : panneau « Groupes dimmer » de l'écran de jeu (un fader par groupe à dimmer) ; le moteur multiplie l'intensité des appareils **après** la fusion des couches par chaque étage de l'arbre (règle proportionnelle : Maître × G1 × G1.1) ; appareil RVB sans canal d'intensité : les canaux émetteurs sont multipliés ; retouche non enregistrée (Q37). | Tests moteur. |
| ERG-038 | I | ERG2 | **Seconde platine MIDI** : deuxième contrôleur reconnu à part (par son port, MIDI-005) ; ses 8 premiers faders commandent les 8 premiers dimmers de groupe, dans l'ordre du panneau ; la première platine garde les couches ; affectation fixe (l'apprentissage « Affecter… », MIDI-008, reste reporté). | Tests ; essai matériel. |
| ERG-039 | I | ERG2 | **Fader de couche = niveau de couche** (précise COU, LIVE-040) : le master d'une couche multiplie sa contribution, comme aujourd'hui ; aucun réglage de couche paramétrable par scène (Q37, Q38 point 4). | Tests. |

## 10. Prototype technique (§7.2) : réalisation et bilan

- **Où** : `tools/Luxia.Tools.Prototype` (exécutable `LuXia-Prototype.exe`), application **séparée de LuXia** (décision de
  l'utilisateur, 2026-09-27) : ni projet, ni sortie DMX ; son propre verrou d'instance. Les **composants** sont, eux, dans
  `Luxia.UI.Controls` (`PanTiltGrid`, `ColorPicker`, `LightColor`…), prêts pour les écrans.
- **Dock 12.1.0.6** (licence MIT) compatible Avalonia 12.1.3. Tous les panneaux sont de simples `Tool` identifiés par leur
  `Id`, le contenu est donné par un gabarit (`PanelTemplate`) : la disposition enregistrée ne contient que des types de la
  bibliothèque, un panneau inconnu ne casse pas la relecture, et les vues, sans état, peuvent être recréées par Dock.
- **Écart constaté** : Dock n'enregistre pas le groupe d'origine d'un panneau fermé ; après relecture, « restaurer » le
  perdait (trouvé par un test). Réponse : le panneau revient dans le groupe qui l'accueille dans la disposition livrée
  (`PrototypeDockFactory.ShowPanel`).
- **Fichiers** : `%AppData%\LuXia\prototype\disposition-controle.json` et `disposition-spectacle.json`, enveloppe
  versionnée LuXia (`formatVersion` 1) autour du texte de Dock (≈ 40 Ko, verbeux mais lisible).
- **Écart corrigé au développement** (C11) : les menus propres à Dock (▾ d'un panneau : *Float*, *Close*…) étaient en
  anglais ; ils sont traduits par les clés de ressources de Dock (`DockStrings.fr.axaml`).
- **Mesures** (ERG-006) : à relever avec l'utilisateur sur son poste (panneau Mesures).
- **Maquettes** (ERG-007, §7.3) : `LuXia-Prototype --maquettes <dossier>` (1920 × 1080) et bouton **Maquettes ▾** du
  prototype ; images dans [maquettes/](maquettes/) : LIVE, ÉDITION, AVEUGLE, zones. Validation : Q35. Guide d'essai :
  [demos/ERG-prototype-et-maquettes.md](demos/ERG-prototype-et-maquettes.md).

## 11. Choix de Claude (délégation du 2026-09-27), à rediscuter à l'exploitation

L'utilisateur, devant l'ampleur des maquettes : « sans exploiter je n'arriverai pas à t'indiquer ce qui est bon ou ce qui
est mauvais […] prends les décisions qui te semblent les plus pertinentes, et présente-moi tes choix à la fin ». Chaque
choix ci-dessous est donc **provisoire** : il sera revu à l'usage. Q35 est close sur cette base.

| # | Choix | Pourquoi | Réversible par |
|---|---|---|---|
| C1 | Maquettes adoptées telles quelles (Q35, points 1 à 6) | Cohérentes avec la charte validée (E1-E8, F1-F10) | Nouvelle maquette |
| C2 | **8 couches par défaut** : ajout de **« Libre »** (★, priorité 7, sans famille attendue) avant Flashs (ERG-008) | L'APC a 8 faders de couche : le 8e ne servait à rien ; l'utilisateur pensait à une colonne pour « un seul équipement particulier ». Les couches restent en nombre libre (COU-001) | Fenêtre Couches… (renommer, supprimer) |
| C3 | Les couches vides restent affichées | Une colonne = un fader de l'APC : repère fixe | `live.json` (couches masquées) |
| C4 | En édition des zones, le bandeau passe en **ZONES** : « zones du lieu, valables pour toutes les scènes » | Les zones n'appartiennent pas à la scène : ne pas laisser croire qu'elles s'y écrivent | — |
| C5 | Contrôle arrive comme **nouvel écran, en tête** ; Live et Scènes restent en place jusqu'à la validation à l'exploitation, puis seront retirés | Rien de ce qui est validé ne disparaît avant que le remplaçant ait fait ses preuves | Retrait des anciens écrans |
| C6 | Le sélecteur **LIVE / ÉDITION / AVEUGLE** est dans l'en-tête de l'écran Contrôle, pas de la fenêtre (écart au §4.1) | Seul Contrôle règle des appareils selon ce mode ; les autres écrans n'en ont pas l'usage tant qu'ils existent | Déplacement dans l'en-tête de la fenêtre quand Live / Scènes disparaîtront |
| C7 | ÉDITION : l'étape choisie est **montrée sur la sortie** (ses valeurs par-dessus les scènes) ; chaque réglage s'y écrit ; un geste (glisser, molette) = **une** entrée d'annulation, écrite 0,5 s après le dernier mouvement | C'est « ce qu'on voit = ce qui est enregistré » ; écrire à chaque pixel de glisser saturerait le disque et l'historique | — |
| C8 | LIVE : les surcharges restent quand on change de mode (F2) ; « Libérer » agit sur la sélection, « Libérer tout » sur tout | Une intervention voulue ne s'efface pas toute seule | — |
| C9 | Démarrage et ouverture d'un projet : toujours en **LIVE** | Ne jamais modifier une scène sans l'avoir demandé | — |
| C10 | Disposition des panneaux enregistrée **sur le poste** (`%AppData%\LuXia\dispositions`), pas dans le projet (écart à LIVE-006) | Elle dépend de l'écran (taille, deuxième écran), pas du show | — |
| C11 | Menus de Dock traduits en français | Doc 03 §2 | — |
| C12 | ✎ **choisit** la scène, le mode (ÉDITION / AVEUGLE) dit **où vont** les réglages d'appareils : deux temps ; en LIVE, Propriétés l'explique | Passer seul en ÉDITION au clic sur ✎ changerait la sortie en plein spectacle (l'étape éditée est montrée). Question de l'utilisateur à l'essai, réponse acceptée | Option « ✎ passe en ÉDITION » |
| C13 | Les looks ne touchent pas au **Grand Master** | Il reste à l'opérateur, comme le fader de l'APC ; un look à 40 % assombrissait l'Ambiance et un look capturé à 100 % ne le remontait pas. Validé à l'essai | Action `grandMaster` toujours lue dans `looks.json` |
| C14 | Zones : la **plus petite** sous le curseur est prise ; liste pour les autres | L'utilisateur a préféré cette solution à la sienne | — |

## 12. Historique

| Date | Modification |
|---|---|
| 2026-10-01 | §4.10 : interrupteur Audio du bloc BPM, écran Audio en une page, volet « Au rythme » simplifié et fréquence ÷8 à ×4 (lot ergonomique de P7). |
| 2026-09-29 | §4.9 : écran de jeu et fenêtre d'édition (chantier « Contrôle 2 »), qui remplace le modèle à trois modes du §4.1 ; tailles minimales des cibles (§4.5). |
| 2026-09-28 | **Chantier validé** par l'utilisateur (1.005.237), fusionné dans `main`, étiquette `v1.005`. |
| 2026-09-28 | Essai de l'écran Contrôle par l'utilisateur, au matériel (guide §0 à §7, 1.005.192 → 1.005.226) : corrections au fil de l'eau (ERG-001, 013, 014, 016, 017, 018, 019, 023) ; ERG-025 scènes resserrées, ERG-026 Stop / Tout stopper, ERG-027 marges ; choix C12 à C14. |
| 2026-09-28 | Nuit de développement par délégation : écran Contrôle dans LuXia, zone permise, identité visuelle, démo ; ERG-009 à ERG-020. |
| 2026-09-27 | §11 choix de Claude par délégation (C1-C11), Q35 close ; ERG-008 (8e couche « Libre »). |
| 2026-09-27 | §9 exigences ERG-001 à ERG-007 ; §10 prototype technique (réalisation, écart Dock sur la restauration d'un panneau fermé). |
| 2026-09-27 | Analyse **validée** : E1-E8 et F1-F10 acceptés tels que proposés. |
| 2026-09-27 | Version 1.1 : finalité (mode automatique, IA), déclencheurs multi-actions et « looks » (§4.8), décisions fines F1-F10 (§8.1). |
| 2026-09-27 | Version 1 : lecture Daslight 4 / 5, inventaire des 8 écrans de LuXia v1.004, charte, composants, modules, décisions E1-E8. |
