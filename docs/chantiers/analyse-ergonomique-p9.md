# Analyse ergonomique – fin de P9 « Lecture en cours et style »

> Doc 32 §5.6. Rédigée le 2026-10-03 par la discussion de développement, **sans toucher au code**, d'après les captures `luxia-captures` en **1920 × 1080**
> ([captures-p9](captures-p9/)) et les résultats des deux essais P9 ([P9-resultats](../essais/P9-resultats.md)). Statuts : ⏳ proposé · ❓ à trancher avec l'utilisateur · ✅ fait.
> Cadrage de l'utilisateur (2026-10-03) : **résolution cible 1920 × 1080** (on ne vérifie pas les tailles plus petites ; les barres de défilement suffisent) ; **le chantier ergonomique
> « Shows et séquences » s'ouvre après la validation de la v1.011** ; le sujet de l'onglet **Live** est à traiter ici.

## 1. Cadre et méthode

**Principe retenu** (demande de l'utilisateur) : tout n'a pas à être accessible à l'écran en même temps. Quand l'affichage devient lourd, on change de **modèle de présentation**
(niveaux de détail, volets à la demande, modèles de départ) au lieu de tasser plus d'éléments.

**Grille appliquée à chaque écran** :

1. **Tâches** : qui fait quoi, quand (à la maison, avant la soirée, **en soirée**), à quelle fréquence. Un écran de soirée se lit d'un coup d'œil ; un écran de préparation peut être dense s'il est guidé.
2. **Charge** : éléments visibles, notions à connaître, clics pour les tâches types (relevés sur les captures 1920 × 1080).
3. **Six critères** : (a) lisible en 2 secondes, (b) vocabulaire du métier, (c) détail **révélé à la demande**, (d) erreur évitée ou réversible, (e) cohérence avec la charte ([doc 60](../60-ergonomie.md) §4, dont §4.11 « liste + fiche »), (f) sûr en Live.
4. **Gravité** (haute / moyenne / basse) et **coût** (petit / moyen / grand) par constat.

## 2. Vue d'ensemble : où va l'attention de l'utilisateur

| Écran | Moment d'usage | Verdict | Détail |
|---|---|---|---|
| **Contrôle** (écran de jeu) | Soirée + préparation | **Bon, devenu l'écran principal** | §3 |
| **Live** | Soirée | **Doublon de Contrôle** : à trancher | §4 |
| **Scènes** | Préparation | Utile mais **doublon partiel** de la fenêtre d'édition | §4 |
| **Éditeur de show** | Préparation (et relecture) | **Trop lourd : refonte** | §5 |
| **Éditeur de séquence** | Préparation | **Clair**, quelques ajustements | §6 |
| **Base musicale** (nouvelle, P9) | Préparation à la maison, tri après soirée | **Conforme à la charte**, quelques ajustements | §7 |
| Installation, Bibliothèque, Console, Audio, Sorties, Simulateur | Mise en place, essais | Écrans de préparation, denses mais lisibles ; audit « liste + fiche » | §8 |

## 3. Écran Contrôle (écran de jeu)

Constat sur [la capture](captures-p9/1920-controle.png) : en 1920 × 1080 l'écran porte, du haut vers le bas, la ligne Stop / Verrou / Couches / Panneaux, la ligne BPM,
la ligne « Morceau en cours », le bandeau du show (quand un show joue), une ligne de messages, puis les panneaux (Colonnes, Groupes dimmer, Looks, Pilote automatique, Journal).

| # | Gravité | Constat | Proposition | Coût | Suite |
|---|---|---|---|---|---|
| C1 | Moyenne | **Quatre lignes d'en-tête** avant les panneaux (≈ 190 px) : la zone des colonnes de scènes perd de la hauteur utile alors que c'est la zone de jeu | Fusionner la ligne de messages dans la barre d'état du bas ; regrouper BPM et « Morceau en cours » sur **une seule ligne** à 1920 px (il y a la place) | moyen | ⏳ |
| C2 | Moyenne | Le panneau **« Pilote automatique »** (« Titre en cours : — », « Style : — », « Pourquoi : — ») répète le bloc « Morceau en cours » et reste vide : bruit permanent | Le garder replié ou le retirer jusqu'au Directeur (P10) ; ou le brancher sur le même état | petit | ⏳ (repris de E1) |
| C3 | Basse | Pastille de style + Corriger + Imposer : **clair** depuis l'essai (cadre commun) ; Corriger demande 3 clics (menu à deux niveaux) | Rien à changer avant d'avoir joué une vraie soirée ; si c'est lent, mettre les styles les plus corrigés en tête | — | ❓ à juger en soirée |
| C4 | Moyenne | Les noms des scènes sont **coupés par « … »** (« Blanc chaud s… », « Cercle des lyr… ») dans les colonnes resserrées | À 1920 px, laisser 2 lignes ou élargir les colonnes ; ne resserrer que sur demande | petit | ⏳ |
| C5 | Basse | Le bandeau du show (« Refrain · depuis 1 mesure · ensuite : … ») est lisible ; les pastilles de transitions suivantes sont utiles | À garder tel quel | — | ✅ |
| C6 | Basse | Le **journal** (bas droite) est peu visible : trois lignes, police petite | Hauteur ajustable ; filtre « erreurs » | petit | ⏳ |

## 4. L'onglet Live (et l'écran Scènes) : le sujet à trancher

**Historique.** Q47 (2026-10-02) : retirer Live et Scènes au dernier lot de P8 ; l'inventaire du lot 7 a montré des fonctions validées encore absentes de Contrôle ; l'utilisateur a choisi de
**les garder pour la v1.010** (doc 99 ligne « Retirer les écrans Live et Scènes »). La liste des fonctions à porter existe : **pour Live** les actions permanentes Flash, Strobe, Fumée (maintien
et rafale), Figer (LIVE-004) et leurs touches (F, S, Z, G, 1 à 9, flèches) ; **pour Scènes** la modification groupée des durées (SCN-004), l'enregistrement depuis la sortie (SCN-037), le copier / coller
et le miroir (SCN-038), la calibration des positions (INST-072), les grilles de palettes (PAL-007) et le filtre par catégorie (SCN-012).

**Constat** sur [Live](captures-p9/1920-live.png) : huit colonnes de scènes (comme dans « Colonnes » de Contrôle), les gros boutons FLASH / STROBE / FUMÉE / FIGER / Tout arrêter à droite, une barre de
palettes en bas. C'est **la même chose que Contrôle**, avec en plus les gros boutons permanents ; en revanche Contrôle ajoute le bloc morceau / BPM, les shows, la supervision, les groupes dimmer, les looks et la fenêtre
d'édition. Deux écrans de jeu en parallèle posent un problème : *où est la vérité* (deux zones de colonnes à synchroniser, deux jeux de raccourcis), et *quel écran ouvrir en soirée*.

| Option | Principe | Pour | Contre |
|---|---|---|---|
| **A. Absorber Live dans Contrôle (recommandée)** | Un panneau ancrable **« Actions »** dans Contrôle (Flash, Strobe, Fumée, Figer, Tout arrêter, avec leurs touches) ; les raccourcis clavier deviennent globaux à Contrôle ; Live est retiré après portage | Un seul écran de jeu ; plus de duplication ; cohérent avec la disposition libre des panneaux | Travail de portage (actions permanentes + raccourcis) ; tests de Live à faire migrer |
| B. Garder Live comme **mode plein écran** de Contrôle | Même écran, bascule « Live » qui masque l'en-tête et agrandit les colonnes et les gros boutons | Pas de perte de lisibilité à distance | Deux présentations à maintenir |
| C. Ne rien changer | Live et Contrôle côte à côte | Aucun coût | La duplication continue ; le jour où l'un évolue, l'autre ment |

**Scènes** : la création d'une scène passe déjà par la **fenêtre d'édition** de Contrôle (brouillon, aveugle, Appliquer / Valider). L'écran Scènes garde un « programmeur » (sélections d'appareils à gauche, réglages,
palettes à droite) qui n'a pas d'équivalent complet ailleurs : [capture](captures-p9/1920-scenes.png). Proposition ⏳ : porter les six fonctions listées dans la fenêtre d'édition (Propriétés, Palettes), puis retirer Scènes ; **à décider après** le chantier Shows et séquences pour ne pas ouvrir deux fronts.

❓ **À décider par l'utilisateur** : option A, B ou C pour Live ; ordre de retrait (Live d'abord, puis Scènes). Ma recommandation : **A, en lot 1 du chantier ergonomique**, car c'est le plus petit lot et il retire un doublon.

## 5. Éditeur de show : refonte (le gros sujet)

Constat sur [la capture](captures-p9/1920-edition-show.png) et sur le cahier des charges existant [refonte-editeur-show.md](refonte-editeur-show.md) (préparé en fin de P8, toujours valable).

**Charge relevée** : une étape *dépliée* affiche à elle seule ≈ 15 contrôles (identifiant, nom, « initiale », supprimer, macro-étape, 4 lignes d'actions avec ✕, 2 listes + « + action », en-tête des transitions avec 2 cases)
puis, **par transition**, ≈ 12 contrôles (type de condition, 2 paramètres, « vers », quantification, poids, « depuis », trois boutons ↑ ↓ ✕, résumé) : environ **30 contrôles pour une étape et une transition**, avant de défiler jusqu'à l'étape suivante.
À droite le diagramme est **en lecture seule**, en bas l'essai sans musique ajoute ≈ 20 contrôles. Le show de démonstration P9 a 6 étapes et 30 transitions : la page défile sur plusieurs écrans.

| # | Gravité | Constat | Proposition | Coût | Suite |
|---|---|---|---|---|---|
| S1 | **Haute** | Tout est déplié en même temps : l'utilisateur voit des **champs avant de voir le sens** du show | **Niveau 1 = lecture** : une carte compacte par étape, et sous chaque transition **une phrase** (« Quand le style devient Rock, aller à Rock à la fin de la mesure »). Aucun champ visible à ce niveau | grand | ❓ → chantier |
| S2 | **Haute** | Deux représentations simultanées (cartes + diagramme), dont une inactive | **Une seule à la fois** : bascule *Liste lisible* ↔ *Diagramme*, le diagramme devenant **cliquable** (clic sur une flèche = éditer sa transition) | grand | ❓ |
| S3 | **Haute** | L'édition se fait **dans la page** : on perd le fil en défilant | **Niveau 2 = volet ou fenêtre modale à la demande** : cliquer une étape ou une transition ouvre un panneau d'édition (fiche de l'élément, charte §4.11 : Enregistrer / Annuler, liste verrouillée) ; le reste de la page est grisé | grand | ❓ |
| S4 | Moyenne | **Les 30 transitions d'ambiance à ambiance** (P9) illustrent le problème : un show « par style » demande n × (n−1) transitions à la main | **Modèles et assistants** : « Un show par style » (une étape par famille, transitions générées), « Enchaînement de séquences », « Couplet / Refrain / Drop » ; créer à partir d'un modèle plutôt que d'une page vide | grand | ❓ (touche aussi P10) |
| S5 | Moyenne | Options pour le futur pilote automatique (rôle, poids, durée max, variables) : « sans intérêt pour l'utilisateur aujourd'hui » | **Niveau 3 = replié** par défaut, hors du chemin ; visible quand P10 existera | petit | ⏳ |
| S6 | Moyenne | Vocabulaire technique : identifiants (« 0 », « 2a »), « macro-étape », « initiale », « quantification », « poids » | Mots du métier : *Départ*, *joue un autre show*, *à la mesure suivante* ; l'identifiant devient invisible (le nom suffit) | moyen | ⏳ (liste dans refonte-editeur-show §3) |
| S7 | Moyenne | Les problèmes (⛔ / ⚠) sont en bas, loin de l'élément concerné | Pastille ⛔ / ⚠ **sur l'étape ou la transition fautive** + liste cliquable | moyen | ⏳ |
| S8 | Basse | Essai sans musique : utile mais bruyant (≈ 20 contrôles) ; la liste de style est maintenant claire | Panneau **repliable**, fermé par défaut ; ouvert par « Essayer » | petit | ⏳ |
| S9 | Basse | Bandeau de supervision en jeu (étape, depuis, ensuite) | **Bon** (essais P8 et P9) : à garder tel quel | — | ✅ |

**Découpage proposé du chantier « Shows et séquences »** (après v1.011) : *lot 1* lecture (S1, S2, S6) sans changer le modèle de données ; *lot 2* édition à la demande (S3, S7) ; *lot 3* modèles et assistants (S4) ; *lot 4* finitions (S5, S8). Chaque lot est essayable seul.

## 6. Éditeur de séquence

[Capture](captures-p9/1920-edition-sequence.png) : jugé **clair** aux essais (modèle « pistes et blocs » compris). Palette des scènes à gauche, frise au centre, fiche de la séquence et du bloc à droite : c'est déjà le schéma « liste + fiche » avec glisser-déposer.

| # | Gravité | Constat | Proposition | Coût | Suite |
|---|---|---|---|---|---|
| Q1 | Moyenne | La frise ne montre que ≈ 14 mesures sur 1920 px pour une séquence de 16 : il faut défiler pour tout voir | Zoom « tout voir » (ajuster à la fenêtre) ; en 1920 la place existe | petit | ⏳ |
| Q2 | Basse | Deux zones d'aide en texte gris (bas de la palette, bas de la frise) | Aide **à la demande** (bouton « ? ») plutôt que permanente | petit | ⏳ |
| Q3 | Basse | Pas de vue d'ensemble des séquences du projet (la liste est dans la colonne Shows de Contrôle) | À traiter avec le lot 1 du chantier (même présentation que les shows) | moyen | ❓ |

## 7. Fenêtre « Base musicale » (P9)

Conforme à la charte [§4.11](../60-ergonomie.md) : liste à gauche (boutons fixes, filtre « Inconnu », doublons à la demande), fiche à droite (style en liste déroulante, alias et titres saisis dans leurs listes), enregistrement explicite, **liste verrouillée pendant la modification**, listes de styles avec « Inconnu » en premier ([captures](captures-p9/base-musicale-fiche.png)).

| # | Gravité | Constat | Proposition | Coût | Suite |
|---|---|---|---|---|---|
| B1 | Moyenne | Un artiste « Inconnu » joué en soirée n'est signalé que par une puce grise | Compteur « n à classer » sur le bouton **Base…** (nombre d'artistes « Inconnu ») | petit | ⏳ (repris de E6) |
| B2 | Basse | Pas d'entrée de menu pour rouvrir la Base hors de l'écran de jeu | « Projet → Base musicale… » | petit | ⏳ (E8) |
| B3 | Basse | Pas de création / renommage de style à l'écran (ex. « Jeux vidéos », Q62) | Reporté (« plus tard » : décision de l'utilisateur) | moyen | ⏳ Q62 |
| B4 | Basse | Noms de styles longs dans la puce (« Festif / Tubes de soirée ») | Nom court en info-bulle pour le nom entier | petit | ❓ (E5) |

## 8. Audit « liste + fiche » des autres écrans (Q59)

Lecture sur captures 1920 × 1080 ; **à confirmer par lecture du code** avant d'ouvrir le chantier. Critères de la charte §4.11 : liste à gauche et boutons fixes (1), fiche à droite (2), enregistrement explicite (3), verrouillage + question à la fermeture (4), listes de niveau N+1 (5), combo plutôt que rangée de boutons (6/7).

| Écran | Forme actuelle | Écarts à la charte | Gravité | Suite |
|---|---|---|---|---|
| **Bibliothèque** | Liste (recherche, filtres) à gauche, éditeur à droite, Nouveau / Importer en bas | Boutons *en bas* de la liste (et non en tête) ; question Oui / Non / Annuler déjà conforme (ERG-041) ; verrouillage de la liste non fait | Basse | ⏳ verrouiller la liste |
| **Installation** (onglet Univers et patch) | **Tableau** d'appareils avec 6 boutons par ligne (Renommer, Déplacer, Changer de mode, Mettre à jour, Identifier, Supprimer) | Pas de fiche : édition directe en ligne + 6 boutons répétés ×N ; formulaire « Ajouter un appareil » séparé | **Moyenne** | ⏳ passer à liste + fiche ; boutons d'action dans la fiche |
| **Installation** (Sélections, Dimmers, Lieux, Fiche) | Mêmes onglets, listes et formulaires | À examiner au code | Moyenne | ❓ |
| **Scènes** | Liste des scènes à gauche (Nouvelle / Dupliquer / Supprimer en bas), programmeur au centre, palettes à droite | Boutons en bas ; pas de fiche : le programmeur *est* l'édition ; enregistrement immédiat | Moyenne | ⏳ avec le retrait de Scènes (§4) |
| **Console** (instantanés) | Liste d'instantanés + actions | À examiner au code | Basse | ❓ |
| **Colonne Shows** / Looks / Palettes (Contrôle) | Listes de boutons à lancer, édition par ✎ | Hors charte (ce sont des commandes de jeu, pas des fiches) | — | ✅ |

**Règle de lecture** : la charte vise les écrans de **codification** (Base, Bibliothèque, Installation, Palettes, Groupes) ; les écrans de **jeu** (Contrôle, Live, colonnes) restent des surfaces de commande.

## 9. Ce que cette analyse conclut

1. **Contrôle est l'écran principal** et doit le rester : un seul écran de jeu (§4, option A recommandée) ; alléger ses en-têtes (C1, C2).
2. **Shows et séquences = un chantier ergonomique à part entière** (§5, §6), en quatre lots, après la validation de la v1.011 (décision de l'utilisateur). Le principe directeur est « **lecture d'abord, détail à la demande, modèles de départ** ».
3. **Installation** est le principal écart à la charte « liste + fiche » après la Base musicale (§8) : à inscrire au chantier, après Shows et séquences.
4. **Rien n'est à corriger avant de valider la v1.011** : les constats de cette analyse sont des propositions pour le chantier suivant ; les écarts de la Base musicale (B1 à B4) sont mineurs.

## 10. Décisions à prendre avec l'utilisateur

| # | Décision | Recommandation |
|---|---|---|
| D1 | Live : option A (absorber dans Contrôle), B (mode plein écran) ou C (statu quo) | **A**, en lot 1 du chantier |
| D2 | Ordre du chantier : Live, puis Shows et séquences (lots 1 à 4), puis Installation et Scènes | Oui |
| D3 | Éditeur de show : valider le principe « lecture d'abord, détail dans un volet, modèles de départ » avant de dessiner (maquettes) | Oui : maquettes avant code |
| D4 | Retirer le panneau « Pilote automatique » de Contrôle jusqu'à P10 (C2) | Oui (replié ou retiré) |
| D5 | Compteur « n à classer » sur « Base… » (B1) | Oui, petit lot avant la validation ou dans le chantier |

## 11. Historique

| Date | Entrée |
|---|---|
| 2026-10-03 | Version complète après les deux essais P9 : cadrage 1920 × 1080, Live et Scènes (§4), refonte des shows et séquences (§5, §6), audit liste + fiche (§8). Remplace la proposition du matin (captures 1680 × 1050, avant l'essai) : ses constats sur l'ancienne fenêtre « Base musicale » (touches 1 à 9, 0, Q, W, E, R) sont **caducs**, la fenêtre ayant été refaite. |
