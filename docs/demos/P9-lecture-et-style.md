# Guide de découverte P9 – Lecture en cours et style

> Version à essayer : **1.011.NNN** (barre de titre ; Claude annonce le numéro exact). Branche `p9/lecture-style`. Guide **mis à jour après le second essai** (1.011.075, précisions de consignes) et le premier (1.011.051) : la fenêtre « Base musicale » est refaite en « liste + fiche » ([doc 60 §4.11](../60-ergonomie.md)).
> Procédure d'essai : [33](../33-procedure-essais.md). Résultats à noter dans [essais/P9-resultats.md](../essais/P9-resultats.md).
> Cahier des charges : [21 – Lecture en cours et style](../21-lecture-en-cours-et-style.md) ; formats : [50 §12i et §12j](../50-format-des-donnees.md) ;
> décisions : Q48 à Q52, D40 ([02 §19](../02-principes-et-architecture-fonctionnelle.md)). Première partie déjà essayée : [sonde PoC-3](P9-poc3-sonde.md).

**Vocabulaire** : la **lecture en cours** est ce que Deezer ou YouTube Music annoncent à Windows (titre, artiste, position) ; le **style**
est la **famille** que LuXia reconnaît pour ce morceau (14 familles : Électro / Dance, Rock, Latino, Slow / Ballade…, plus **Inconnu**),
avec une **confiance** (vert ≥ 80 %, orange ≥ 50 %, rouge en dessous, gris : inconnu). Le **show** choisit son ambiance selon ce style.
La **base musicale** (dans le projet) apprend : chaque **correction** est mémorisée. Un **artiste** a **un seul style** ; un artiste joué mais absent de la base y est ajouté avec le style « **Inconnu** », à classer plus tard. Les **titres** ne sont pas livrés avec l'application (seulement ≈ 460 artistes) : ils apparaissent quand vous en corrigez un. Rien ne se fait en ligne en soirée.

## 0. Préparation

1. Fermer LuXia ; Claude régénère le show de travail (après votre accord) et l'ouvre : vérifier **Aide → À propos**, « Dossier du projet ».
2. Brancher les PAR si possible (sinon : écran **Simulateur**). Écran **Contrôle**. Ouvrir **Deezer** (application) et **YouTube Music**
   (Chrome), un titre prêt dans chacun. Pas de VLC (il ne s'annonce pas à Windows, doc 99).
3. Sous le bloc BPM : le bloc **« Morceau en cours »** (titre, artiste, application, **pastille de style** encadrée avec ses deux boutons **Corriger ▾** et
   **Imposer ▾** ; à droite **Base…** et **Saisir…**). Le **Journal** (en bas) écrit « ♫ morceau » et « ♪ style ».
4. Entre deux exemples : **■ Tout stopper**.

## 1. Catalogue des exemples (catégorie « Phase P9 » du show de référence)

| Exemple | Type | Démontre | Sans musique ? | Ce qu'on doit voir |
|---|---|---|---|---|
| *Style du morceau (P9)* | show | Le show **suit le style** : une ambiance par famille ; **retour à « Neutre » au morceau suivant** (vrai changement de titre) | oui, avec le style simulé (ex. 9) | Neutre (bleu, lyres au plafond) → **Rock** (rouge, plein feu) / **Électro** (chenillard, cercle des lyres, arc-en-ciel des barres) / **Latino** (ambre, alternance pairs / impairs) / **Slow** (blanc chaud 50 %) / **Inconnu** (une couleur par mesure, 50 %) |
| *Piège : style qui n'existe pas (P9)* | show | **Avertissement** à la validation : « Musette » n'est le nom d'aucune famille (c'est une étiquette de « Bal / Traditionnel ») | oui | Le show se lance mais reste à l'attente ; menu **Projet → Problèmes du projet…** (ou `luxia-headless valider`) : « écrivez plutôt « Bal » » |
| `docs/demos/P9-exemple-echange.json` | fichier | **Import** d'un échange JSON : 1 style, 5 artistes, 3 alias (dont un alias refusé : « Queen » appartient déjà à Queen) | — | Bilan : 4 artistes ajoutés, 1 mis à jour, 2 alias, 1 style, 1 ligne refusée |

## 2. Lecture en cours et style

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 1 | Deezer | Jouer un titre connu (Queen, Daft Punk, ABBA…) dans **Deezer** | Au bout de ≈ 1 s : titre et artiste dans le bloc (« ▶ »), application « Deezer », **position** qui avance, puce de style (ex. **Rock 80 %** en vert) ; Journal : « ♫ … », « ♪ style : Rock (80 %, artiste) » |
| 2 | YouTube Music | Jouer un titre dans **YouTube Music** (Chrome), de préférence un clip « Artiste - Titre (Official Video) » ou une chaîne « XYZ - Topic » | **Artiste et titre nettoyés** (sans « Official Video », sans le nom de la chaîne) : le bloc montre l'artiste réel ; même famille que dans Deezer |
| 3 | Pause, reprise, titre suivant | Pause (le bloc passe à « ⏸ », **le style reste**), reprise, puis passer au titre suivant | **Un seul** changement de morceau par titre ; pas de clignotement ; la position repart de zéro |
| 4 | Titre inconnu | Jouer un titre d'un artiste peu connu | Puce **grise « Inconnu »** (pas de confiance) ; info-bulle : « le show suit l'énergie seule » |
| 5 | **Corriger** | Sur le titre inconnu : bouton **Corriger ▾** (à droite de la pastille de style) → **Pour cet artiste ▸** → une famille | Le style change **tout de suite**, confiance **100 %**, vert ; Journal « style corrigé » ; rejouer un autre titre du même artiste : même style (confiance habituelle) |
| 6 | Corriger un seul titre | Sur un titre connu : **Corriger ▾ → Pour ce titre seulement ▸ → Slow** ; passer à un autre titre de l'artiste. Puis sur un titre d'un **artiste encore « Inconnu »** : même menu | Le titre corrigé est « Slow », l'autre garde le style de l'artiste. Pour un artiste inconnu, LuXia demande « **Appliquer ce style à l'artiste ?** » : **Oui** = l'artiste entier, **Non** = ce titre seulement |
| 7 | **Imposer** | **Prérequis** : un morceau joue (Deezer) et un show est lancé : colonne **Shows** → clic sur *Style du morceau (P9)*. 1. Cliquer **Imposer ▾** (à droite de la pastille). 2. Choisir **Latino**. 3. Attendre la fin de la mesure (≈ 2 s). 4. Passer au titre suivant | La puce dit « **imposé** » et le show passe sur l'étape **Latino** (ambre) **sans repasser par Neutre**, quelle que soit l'ambiance de départ ; **au titre suivant l'imposition s'arrête** et le show suit le style de l'écoute ; « ↺ Revenir à la détection automatique » la lève aussi |
| 8 | **Saisir…** | À utiliser **seulement quand aucun lecteur ne s'annonce à Windows** (platine, VLC, DJ extérieur) : en soirée vous lancez Deezer ou YouTube Music et LuXia lit tout seul. Pour l'essai : 1. fermer Deezer et Chrome ; 2. **Saisir…** ; 3. remplir **Artiste** (champ du haut) « ABBA » puis **Titre** « Dancing Queen » ; 4. **Identifier ce morceau** | « Aucun morceau » avant ; après : style **Disco / Funk / Soul** (application « saisie manuelle ») ; rien ne plante |
| 8b | Plus de lecteur | **Lancer d'abord un show** (ex. *Style du morceau (P9)*), jouer un titre puis **fermer Deezer** (et Chrome) | Le bloc revient à « Aucun morceau » (jamais un vieux titre) ; **le show continue** (style absent). Si vous relancez ensuite Chrome et YouTube Music, le morceau doit être détecté à nouveau (le journal technique garde la trace des sessions vues par Windows) |

## 3. Le show qui suit le style

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 9 | Essai sans musique | **Prérequis** : colonne **Shows** → ✎ sur *Style du morceau (P9)* ; le panneau **Essai sans musique** est déjà affiché sous le diagramme. 1. **▶ Jouer le show**. 2. Dans la liste **Style**, choisir **Rock** : le style s'applique tout de suite. 3. Choisir **Latino**, puis **Électro / Dance**. 4. **Morceau suivant**. 5. Choisir **Sans son** | Étape active : Neutre → **Rock** → **Latino** → **Électro** (sans repasser par Neutre) ; **Morceau suivant** : Neutre puis retour sur le style choisi ; **Sans son** : retour à l'étape **Neutre** ; la liste propose **Sans son**, **Inconnu**, puis les familles ; « Inconnu » : étape **Inconnu** |
| 10 | Avec la vraie musique | Lancer *Style du morceau (P9)* ; jouer un titre Rock, puis un titre Électro, puis un titre Latino (Deezer) — essayer aussi un titre à **deux artistes** (« Luis Fonsi et Daddy Yankee », « Post Malone et Swae Lee ») | Après chaque **vrai changement de titre** le show repasse par **Neutre** puis prend l'ambiance du **nouveau style** ; les artistes séparés par « et », « & », « , », « x », « and », « y » sont reconnus un à un ; bandeau « Show en cours » et Journal « ◆ étape … » à l'appui |
| 11 | Piège | Cliquer *Piège : style qui n'existe pas (P9)* ; ouvrir le menu **Projet → Problèmes du projet…** (ou `luxia-headless valider`) | Le show reste à son étape d'attente ; avertissement orange : « le style « Musette » ne correspond à aucune famille … écrivez plutôt « Bal » » |
| 12 | Repli sans lecteur | **Prérequis** : show *Couplet / Refrain / Drop* lancé, **🎧 Audio** allumé (son du PC). **Partie 1, sans lecteur** : fermer Deezer et Chrome, jouer un fichier ou une vidéo hors lecteur suivi ; ce show dépend du **son** : l'**énergie** fait passer **Intro → Couplet**, un vrai **silence** (couper le son 2 s) le fait passer à **Final**, la **reprise du son** le ramène à **Intro**. **Partie 2, avec un lecteur** (Deezer) : mettre en **pause**, puis relancer | Partie 1 : Couplet à l'énergie, Final au silence, Intro à la reprise (« au morceau suivant » par l'écoute, le repli). Partie 2 : avec un lecteur en lecture seul le changement de titre compte (Q49) ; **la pause (plus de son) reste une fin de morceau** : étape finale, et la reprise ramène à l'**Intro** — c'est voulu (vos lecteurs restent ouverts en soirée) |

## 4. La base musicale

Bouton **Base…** du bloc « Morceau en cours » (fenêtre non bloquante). Onglet **Base** : la **liste** des artistes à gauche (ses boutons **Ajouter, Dupliquer,
Supprimer, Chercher les doublons** sont toujours au même endroit), la **fiche** de l'artiste choisi à droite. Rien n'est enregistré avant **Enregistrer**.

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 13 | Parcourir | Onglet **Base** : taper « queen » dans la recherche, cliquer l'artiste | La liste se réduit (artistes de départ : ≈ 460) ; la fiche montre le **nom**, le **style** (liste déroulante), les **alias** et les **titres** (la liste des titres est vide : les titres ne sont pas livrés, seuls ceux que vous corrigez y entrent) |
| 14 | Modifier la fiche | Changer le **style** dans la liste déroulante ; **+** dans « Alias » et taper un alias ; **+** dans « Titres » et taper un titre, sa version, un style propre | Dès la première frappe : « ● modifications non enregistrées », **Enregistrer** et **Annuler** s'activent ; **rien n'est écrit** avant Enregistrer ; **Annuler** remet la fiche comme elle était ; **Enregistrer** met la liste et le titre en cours à jour |
| 14b | Fiche modifiée : liste verrouillée | Modifier la fiche (ex. le style), puis essayer de cliquer **un autre artiste**, de taper dans la recherche, de cocher le filtre, de cliquer **Ajouter** ou un autre onglet ; puis fermer la fenêtre **Fermer** | Dès la première modification, **toute la liste est grisée** (« ● non enregistré : liste verrouillée ») ainsi que les onglets **À classer** et **Propositions** : seuls **Enregistrer** et **Annuler** sortent de là. À la **fermeture** de la fenêtre : « Enregistrer les modifications de … ? » **Oui** enregistre, **Non** abandonne, **Annuler** reste |
| 14c | Ajouter, dupliquer, supprimer | **Ajouter** (nom, style, Enregistrer) ; sur un artiste **Dupliquer** (copie : style et titres, **sans les alias**, nom « … (copie) » à changer) ; **Supprimer** | Les boutons ne bougent pas quand on clique un artiste ; Supprimer demande **Oui / Non** ; le nouvel artiste est sélectionné après Enregistrer |
| 15 | Alias | Dans la fiche, **+** deux fois de suite ; taper un alias ; taper l'alias d'un autre artiste ; Enregistrer ; saisir ensuite ce titre avec **Saisir…** | Le 2ᵉ « + » **ne crée pas une 2ᵉ ligne vide** : le curseur retourne dans la ligne vide ; un alias déjà pris par un autre artiste est **refusé** à l'enregistrement (message qui dit par qui) et la fiche reste ouverte ; reconnu sous l'alias |
| 16 | Doublons | Ajouter « Beatles Doublon » et « Doublon Beatles » ; bouton **Chercher les doublons** de la liste | S'il n'y a aucun doublon : un message, aucun panneau. Sinon, le panneau **Doublons probables** apparaît sous la fiche : choisir la paire, **Garder la première fiche** : les deux noms désignent une seule fiche (l'autre devient alias) |
| 17 | **Importer** un échange JSON | **Importer un JSON…** → `docs\demos\P9-exemple-echange.json` | Message : « 4 artiste(s) ajouté(s), 1 mis à jour, 2 alias, 1 style(s) ; 1 ligne(s) refusée(s) » (l'alias « Queen » est refusé : il appartient à Queen) |
| 18 | **À classer** | Onglet **À classer** : les artistes « **Inconnu** » (dont ceux joués aux exemples 4 et 8 et l'artiste inconnu de l'import) ; cliquer la **pastille** d'un style. Dans l'onglet Base, cocher « **Seulement les artistes « Inconnu »** » | Chaque pastille **classe l'artiste** et passe au suivant ; **aucun raccourci clavier** (les touches 1 à 9, 0, Q, W, E, R ne font plus rien) ; « Rien à classer » à la fin ; le filtre de la liste donne les mêmes artistes |
| 19 | Exporter | **Exporter en JSON…** ; ouvrir le fichier | Trois tables : `styles` (code, libellé, genres, ordre), `artists` (code, nom, style), `aliases` (alias, artiste) ; accents corrects ; pas de poids, pas de source |
| 20 | Journal de soirée | Ouvrir `Documents\LuXia\Journaux\soiree-AAAAMMJJ.csv` | **Une ligne par événement** (`morceau`, puis `correction`, `imposé` ou `style` si le style change pendant le titre) ; colonnes heure, événement, titre et artiste **nettoyés** (l'artiste du titre, pas le nom de la chaîne YouTube), application, style, confiance, méthode, imposé, show, puis titre brut et artiste brut du lecteur |

## 5. Enrichissement en ligne (à la maison, jamais en soirée)

L'outil **`luxia-enrich`** propose une famille pour les artistes « **Inconnu** » de la base (onglet « À classer ») à partir de MusicBrainz (gratuit, sans clé). **Il ne
change rien dans la base** : il écrit des **propositions** que vous validez.

```powershell
& "D:\Develop\Claude\CSharp\DMX\tools\Luxia.Tools.MusicEnrich\bin\Debug\net10.0\luxia-enrich.exe" "D:\Develop\Claude\CSharp\DMX\samples\Show de travail" --max 20
```

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 21 | Proposer | **Prérequis** : avoir au moins un artiste « Inconnu » (exemples 4, 8 ou 17) et Internet. Lancer la commande ci-dessus (1 requête par seconde) | Une ligne par artiste : « Artiste : famille (confiance, MusicBrainz) », « Artiste : aucune proposition » (MusicBrainz a répondu sans étiquette reconnue) ou « Artiste : requête sans réponse… : à relancer » (réseau ou limite de débit) ; un message « ⚠ N requête(s) sans réponse » les récapitule ; le message final est « N proposition(s) enregistrée(s) dans …\propositions.json. À valider dans LuXia : bouton « Base… » de l'écran de jeu, onglet « Propositions »… » |
| 22 | Valider | **Base… → onglet Propositions** : **Accepter**, **accepter avec un autre style**, **Rejeter** ; **Accepter les propositions sûres (≥ 70 %)** | L'artiste accepté reçoit son style (il quitte « À classer ») ; un rejet n'est plus reproposé ; les moins de 70 % sont en orange |
| 23 | Hors ligne | Relancer la commande avec `--hors-ligne` | Aucune requête : le cache (`%AppData%\LuXia\cache-enrichissement`, 90 jours) répond |

## 6. Interface

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 24 | Listes déroulantes | Ouvrir plusieurs listes (unité d'une durée, liste des conditions d'un show, périphérique audio, style d'une fiche) | La liste dépliée a **exactement la largeur du champ**, alignée à gauche, **même position** quelle que soit la longueur des textes ; un texte trop long est coupé par « … » et lisible en **info-bulle** |
| 25 | Titre long | Un titre très long (clip YouTube) | Le bloc « Morceau en cours » **ne change pas de taille** : le titre est coupé par « … », info-bulle au survol |

## 7. Retour

Pour chaque exemple : **correct / à revoir / idée**. Les points bloquants (a / b / c) interrompent l'essai : pause et message pour la
discussion de développement (doc 33).
