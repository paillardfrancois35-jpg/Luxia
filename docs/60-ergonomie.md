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
| ERG-001 | I | ERG | **Ancrage de panneaux** (bibliothèque *Dock*, Avalonia 12) : chaque panneau se colle à gauche / droite / haut / bas, s'empile en onglets, se **détache dans une fenêtre** (deuxième écran), se replie sur un bord ou se ferme ; un panneau fermé ou absent se réaffiche par le menu **Panneaux**, à sa place d'origine. Deux dispositions prêtes : **Contrôle** et **Spectacle** (§6). | Prototype (§7.2) : manipulations faites par l'utilisateur ; test automatique du réaffichage. |
| ERG-002 | I | ERG | **Enregistrement de la disposition** : sans bouton, dès qu'elle change (vérification toutes les 2 s) et à la fermeture ; reprise au démarrage, fenêtres détachées et panneaux fermés compris ; une par disposition prête ; « Rétablir la disposition » revient à celle livrée ; un fichier illisible est mis de côté et la disposition livrée reprend, avec un message, sans jamais bloquer le démarrage. | Tests automatiques (écriture / relecture / fichier illisible) ; essai utilisateur (fermer, rouvrir). |
| ERG-003 | I | ERG | **Grille Pan / Tilt** (composant commun, §5) : un point par appareil, en degrés ; un clic amène la sélection sous le curseur (plusieurs appareils : leur centre, écarts conservés, sans écraser un appareil contre le bord) ; Maj + glisser = réglage fin ; molette = Tilt fin (Maj : Pan, Ctrl : ×10) ; flèches ; **zones interdites** (rouges) et **zone permise** (limites, extérieur assombri, F7) dessinées, déplacées et redimensionnées par **8 poignées**, Suppr pour retirer. | Tests des calculs ; galerie ; essai utilisateur. |
| ERG-004 | I | ERG | **Sélecteur de couleur** (composant commun, §5) : carré teinte × saturation, barre d'intensité, valeurs lisibles (°, %), **favoris** (clic = reprendre, clic droit = retirer, « + » = ajouter). | Tests des conversions et du découpage ; galerie ; essai utilisateur. |
| ERG-005 | M | ERG | **Galerie des composants** : chaque composant commun dans chacun de ses états, manipulable ; source des **captures de référence** (`LuXia-Prototype --captures <dossier>`, sans écran). | Captures produites et relues. |
| ERG-006 | I | ERG | **Mesures du prototype** : images par seconde, demandes par seconde des composants, mémoire, nombre de fenêtres, affichées dans un panneau ; bilan (fluidité d'un glisser, deux écrans) consigné au §10. | Relevé fait avec l'utilisateur. |
| ERG-007 | I | ERG | **Maquettes de la disposition Contrôle** (§7.3) : images rendues par Avalonia avec les vrais composants et des données fictives (modes LIVE / ÉDITION / AVEUGLE, scène en édition, zones), validées par l'utilisateur **avant** tout développement des écrans. | Validation de l'utilisateur. |

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
- **Écart à corriger au développement** : les menus propres à Dock (▾ d'un panneau : *Float*, *Close*…) sont en
  anglais, alors que l'interface est en français (doc 03 §2) ; à franciser en surchargeant les gabarits de Dock.
- **Mesures** (ERG-006) : à relever avec l'utilisateur sur son poste (panneau Mesures).
- **Maquettes** (ERG-007, §7.3) : `LuXia-Prototype --maquettes <dossier>` (1920 × 1080) et bouton **Maquettes ▾** du
  prototype ; images dans [maquettes/](maquettes/) : LIVE, ÉDITION, AVEUGLE, zones. Validation : Q35. Guide d'essai :
  [demos/ERG-prototype-et-maquettes.md](demos/ERG-prototype-et-maquettes.md).

## 11. Historique

| Date | Modification |
|---|---|
| 2026-09-27 | §9 exigences ERG-001 à ERG-007 ; §10 prototype technique (réalisation, écart Dock sur la restauration d'un panneau fermé). |
| 2026-09-27 | Analyse **validée** : E1-E8 et F1-F10 acceptés tels que proposés. |
| 2026-09-27 | Version 1.1 : finalité (mode automatique, IA), déclencheurs multi-actions et « looks » (§4.8), décisions fines F1-F10 (§8.1). |
| 2026-09-27 | Version 1 : lecture Daslight 4 / 5, inventaire des 8 écrans de LuXia v1.004, charte, composants, modules, décisions E1-E8. |
