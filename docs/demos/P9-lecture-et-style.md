# Guide de découverte P9 – Lecture en cours et style

> Version à essayer : **1.011.NNN** (barre de titre ; Claude annonce le numéro exact). Branche `p9/lecture-style`.
> Procédure d'essai : [33](../33-procedure-essais.md). Résultats à noter dans [essais/P9-resultats.md](../essais/P9-resultats.md).
> Cahier des charges : [21 – Lecture en cours et style](../21-lecture-en-cours-et-style.md) ; formats : [50 §12i et §12j](../50-format-des-donnees.md) ;
> décisions : Q48 à Q52, D40 ([02 §19](../02-principes-et-architecture-fonctionnelle.md)). Première partie déjà essayée : [sonde PoC-3](P9-poc3-sonde.md).

**Vocabulaire** : la **lecture en cours** est ce que Deezer ou YouTube Music annoncent à Windows (titre, artiste, position) ; le **style**
est la **famille** que LuXia reconnaît pour ce morceau (14 familles : Électro / Dance, Rock, Latino, Slow / Ballade…, plus **Inconnu**),
avec une **confiance** (vert ≥ 80 %, orange ≥ 50 %, rouge en dessous, gris : inconnu). Le **show** choisit son ambiance selon ce style.
La **base musicale** (dans le projet) apprend : chaque **correction** est mémorisée. Rien ne se fait en ligne en soirée.

## 0. Préparation

1. Fermer LuXia ; Claude régénère le show de travail (après votre accord) et l'ouvre : vérifier **Aide → À propos**, « Dossier du projet ».
2. Brancher les PAR si possible (sinon : écran **Simulateur**). Écran **Contrôle**. Ouvrir **Deezer** (application) et **YouTube Music**
   (Chrome), un titre prêt dans chacun. Pas de VLC (il ne s'annonce pas à Windows, doc 99).
3. Sous le bloc BPM : le nouveau bloc **« Morceau en cours »** (titre, artiste, application, style, boutons **Corriger ▾**, **Imposer ▾**,
   **Base…**, **Saisir…**). Le **Journal** (en bas) écrit « ♫ morceau » et « ♪ style ».
4. Entre deux exemples : **■ Tout stopper**.

## 1. Catalogue des exemples (catégorie « Phase P9 » du show de référence)

| Exemple | Type | Démontre | Sans musique ? | Ce qu'on doit voir |
|---|---|---|---|---|
| *Style du morceau (P9)* | show | Le show **suit le style** : une ambiance par famille ; **retour à « Neutre » au morceau suivant** (vrai changement de titre) | oui, avec le style simulé (ex. 9) | Neutre (bleu, lyres au plafond) → **Rock** (rouge, plein feu) / **Électro** (chenillard, cercle des lyres, arc-en-ciel des barres) / **Latino** (ambre, alternance pairs / impairs) / **Slow** (blanc chaud 50 %) / **Inconnu** (une couleur par mesure, 50 %) |
| *Piège : style qui n'existe pas (P9)* | show | **Avertissement** à la validation : « Musette » n'est le nom d'aucune famille (c'est une étiquette de « Bal / Traditionnel ») | oui | Le show se lance mais reste à l'attente ; menu **Projet → Problèmes du projet…** (ou `luxia-headless valider`) : « écrivez plutôt « Bal » » |
| `docs/demos/P9-exemple-playlist.csv` | fichier | **Import** d'une playlist (8 lignes) | — | Bilan : 8 lignes, 3 titres classés, 2 déjà connus, 3 « à classer » |

## 2. Lecture en cours et style

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 1 | Deezer | Jouer un titre connu (Queen, Daft Punk, ABBA…) dans **Deezer** | Au bout de ≈ 1 s : titre et artiste dans le bloc (« ▶ »), application « Deezer », **position** qui avance, puce de style (ex. **Rock 80 %** en vert) ; Journal : « ♫ … », « ♪ style : Rock (80 %, artiste) » |
| 2 | YouTube Music | Jouer un titre dans **YouTube Music** (Chrome), de préférence un clip « Artiste - Titre (Official Video) » ou une chaîne « XYZ - Topic » | **Artiste et titre nettoyés** (sans « Official Video », sans le nom de la chaîne) : le bloc montre l'artiste réel ; même famille que dans Deezer |
| 3 | Pause, reprise, titre suivant | Pause (le bloc passe à « ⏸ », **le style reste**), reprise, puis passer au titre suivant | **Un seul** changement de morceau par titre ; pas de clignotement ; la position repart de zéro |
| 4 | Titre inconnu | Jouer un titre d'un artiste peu connu | Puce **grise « Inconnu »** (pas de confiance) ; info-bulle : « le show suit l'énergie seule » |
| 5 | **Corriger** | Sur le titre inconnu : **Corriger ▾ → Pour tous les titres de cet artiste ▸ →** une famille | Le style change **tout de suite**, confiance **100 %**, vert ; Journal « style corrigé » ; rejouer un autre titre du même artiste : même style |
| 6 | Corriger un seul titre | Sur un titre connu : **Corriger ▾ → Pour ce titre seulement ▸ → Slow** ; passer à un autre titre de l'artiste | Le titre corrigé est « Slow », l'autre garde le style de l'artiste |
| 7 | **Imposer** | **Imposer ▾ → Latino** pendant un titre Rock ; puis titre suivant | La puce dit « **imposé** » ; le show suit « Latino » ; **au titre suivant l'imposition s'arrête** ; « ↺ Revenir à la détection automatique » la lève aussi |
| 8 | **Saisir…** | Fermer Deezer et Chrome ; **Saisir…** « Dancing Queen » / « ABBA » | « Aucun morceau » avant ; après : style **Disco / Funk / Soul** (application « saisie manuelle ») ; rien ne plante |
| 8b | Plus de lecteur | Jouer un titre puis **fermer Deezer** | Le bloc revient à « Aucun morceau » (jamais un vieux titre) ; les shows continuent (style absent) |

## 3. Le show qui suit le style

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 9 | Essai sans musique | Ouvrir *Style du morceau (P9)* (✎ dans la colonne Shows) → **Essai sans musique** : champ « Style » (taper « Rock »), bouton **Morceau suivant** | Étape active : Neutre → **Rock** ; « Morceau suivant » : retour **Neutre** ; taper « Électro » : **Électro** ; « Inconnu » : étape **Inconnu** |
| 10 | Avec la vraie musique | Lancer *Style du morceau (P9)* ; jouer un titre Rock, puis un titre Électro, puis un titre Latino (Deezer) | Après chaque **vrai changement de titre** le show repasse par **Neutre** puis prend l'ambiance du **nouveau style** ; bandeau « Show en cours » et Journal « ◆ étape … » à l'appui |
| 11 | Piège | Cliquer *Piège : style qui n'existe pas (P9)* ; ouvrir le menu **Projet → Problèmes du projet…** (ou `luxia-headless valider`) | Le show reste à son étape d'attente ; avertissement orange : « le style « Musette » ne correspond à aucune famille … écrivez plutôt « Bal » » |
| 12 | Repli sans lecteur | Éteindre les lecteurs ; **🎧 Audio** allumé (micro ou son du PC) ; enchaîner deux morceaux au silence près | « Au morceau suivant » (écoute) fonctionne encore **sans** lecteur ; avec un lecteur en lecture, seul le changement de titre compte (Q49) |

## 4. La base musicale

Bouton **Base…** du bloc « Morceau en cours » (fenêtre non bloquante).

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 13 | Parcourir | Onglet **Base** : taper « queen » dans la recherche | La liste se réduit (les artistes de départ : ≈ 460) ; clic sur un artiste : ses boutons de style, ses titres connus |
| 14 | Changer, ajouter, retirer | Changer le style d'un artiste (clic sur une famille) ; **Ajouter un artiste** (nom + famille) ; **Retirer** (confirmation) | Pris en compte tout de suite ; le titre en cours se met à jour |
| 15 | Alias | Sur un artiste : « Autre orthographe (alias) » → **Ajouter l'alias** ; saisir ensuite ce titre avec **Saisir…** | Reconnu sous l'alias ; un alias déjà pris par un autre artiste est refusé (message) |
| 16 | Doublons | Ajouter « Beatles Doublon » et « Doublon Beatles » ; **Chercher les doublons** ; choisir la paire ; **Garder la première fiche** | Les deux noms désignent une seule fiche (l'autre devient alias) |
| 17 | **Importer** la playlist d'exemple | **Importer un CSV…** → `docs\demos\P9-exemple-playlist.csv` | Message : « 8 ligne(s), 3 titre(s) … classés, 2 déjà connus, 3 à classer » ; l'onglet **À classer** s'ouvre |
| 18 | **À classer** | Onglet **À classer** : 3 artistes ; **touche 1 à 9, 0, Q, W, E, R** (une famille) ; **Entrée** pour passer | Chaque touche **classe l'artiste** et passe au suivant ; la liste se vide ; « Rien à classer » à la fin ; les morceaux inconnus joués aux exemples 4 et 8 y figurent aussi (journal de soirée) |
| 19 | Exporter | **Exporter en CSV…** ; ouvrir dans Excel | Colonnes artiste, style, poids, alias, source ; accents corrects |
| 20 | Journal de soirée | Ouvrir `Documents\LuXia\Journaux\soiree-AAAAMMJJ.csv` | Une ligne par morceau : heure, titre, artiste, application, style, confiance, méthode, imposé, show |

## 5. Enrichissement en ligne (à la maison, jamais en soirée)

L'outil **`luxia-enrich`** propose une famille pour les artistes de « À classer » à partir de MusicBrainz (gratuit, sans clé). **Il ne
change rien dans la base** : il écrit des **propositions** que vous validez.

```powershell
& "D:\Develop\Claude\CSharp\DMX\tools\Luxia.Tools.MusicEnrich\bin\Debug\net10.0\luxia-enrich.exe" "D:\Develop\Claude\CSharp\DMX\samples\Show de travail" --max 20
```

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 21 | Proposer | Lancer la commande ci-dessus (Internet nécessaire ; 1 requête par seconde) | Une ligne par artiste (« Artiste : famille (confiance, MusicBrainz) » ou « aucune proposition ») ; « N proposition(s) enregistrée(s) » |
| 22 | Valider | **Base… → onglet Propositions** : **Accepter**, **accepter avec une autre famille**, **Rejeter** ; **Accepter les propositions sûres (≥ 70 %)** | L'artiste accepté entre dans la base (origine « enrichissement ») ; un rejet n'est plus reproposé ; les moins de 70 % sont en orange |
| 23 | Hors ligne | Relancer la commande avec `--hors-ligne` | Aucune requête : le cache (`%AppData%\LuXia\cache-enrichissement`, 90 jours) répond |

## 6. Interface

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 24 | Listes déroulantes (début de P9) | Ouvrir plusieurs listes (unité d'une durée, liste des conditions d'un show, périphérique audio) | La liste dépliée a **toujours la même largeur** (300 px), alignée à gauche du champ, **même position** quelle que soit la longueur des textes ; un texte trop long est coupé par « … » et lisible en **info-bulle** |
| 25 | Titre long | Un titre très long (clip YouTube) | Le bloc « Morceau en cours » **ne change pas de taille** : le titre est coupé par « … », info-bulle au survol |

## 7. Retour

Pour chaque exemple : **correct / à revoir / idée**. Les points bloquants (a / b / c) interrompent l'essai : pause et message pour la
discussion de développement (doc 33).
