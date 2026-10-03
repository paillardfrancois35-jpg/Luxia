# 21 – Lecture en cours et identification du style

> Cahier des charges – module **Musique** (préfixe `MUS`). Phase principale : **P9** (peut démarrer en parallèle dès P5).
> Preuves de concept : **PoC-3** (lecture en cours selon les lecteurs), **PoC-4** (qualité des sources de styles).
> Références : [02 §16, §17](02-principes-et-architecture-fonctionnelle.md), [19](19-audio-et-tempo.md), [22](22-directeur-automatique.md).

---

## 1. Rôle et contraintes

1. Savoir **quel morceau** est joué sur le PC (titre, artiste, album, état de lecture, position).
2. En déduire un **style** (famille musicale) avec une **confiance**, pour que le Directeur choisisse une esthétique adaptée.

Contraintes (décisions D4, D5, D9) :
- la musique vient d'applications de streaming variées (Deezer, Spotify, YouTube Music, navigateurs…) **sur le même PC** ;
- **aucun accès Internet en soirée** : l'identification est **100 % locale** ;
- les styles joués sont imprévisibles → taxonomie maison, famille **Inconnu** gérée par l'énergie seule, apprentissage au fil des soirées.

## 2. Lecture en cours

### 2.1 Source

API Windows de contrôle des médias (sessions de lecture du système) : chaque application compatible expose une session
(titre, artiste, album, miniature, état, position).

### 2.2 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MUS-001 | I | P9 | Lecture des sessions média du système ; choix de la session **en cours de lecture** ; si plusieurs jouent, la plus récemment démarrée ; si aucune ne joue, la dernière active. | Spotify + navigateur ouverts → la bonne session suivie. |
| MUS-002 | I | P9 | Événements `MorceauChangé` (après stabilisation ~1 s pour éviter les doublons), `LectureDémarrée`, `LectureEnPause`. | Changement de titre → un seul événement. |
| MUS-003 | I | P9 | Extraction titre, artiste, album, miniature, durée et **position estimée** (interpolée entre deux mises à jour du lecteur). | Affichage en Live. |
| MUS-004 | I | P9 | Détection des **publicités** (versions gratuites) : heuristiques (artiste vide ou égal au nom de l'application, titres types « Advertisement », « Publicité », durée ≤ 60 s…) → style spécial **Pub**. | Pub Spotify gratuite → style Pub. |
| MUS-005 | I | P9 | Gestion des titres **YouTube** : l'« artiste » est souvent le nom de la chaîne (« XYZ – Topic », « XYZVEVO »), le vrai artiste est dans le titre (« Artiste - Titre (Official Video) ») → séparation artiste / titre (§3.1). | Jeu de test YouTube. |
| MUS-006 | M | P9 | Si aucune application ne fournit d'informations, le système fonctionne quand même : style **Inconnu**, pilotage par l'énergie. | — |
| MUS-007 | M | P9 | Saisie manuelle possible du morceau / du style depuis le Live. | — |

### 2.3 Preuve de concept (PoC-3)

Vérifier avec **Spotify (application), Deezer (application), YouTube Music et YouTube (navigateurs Chrome/Edge/Firefox)** :
présence du titre / artiste, forme des chaînes, précision et fréquence de mise à jour de la position, comportement des pubs.
Le résultat alimente les règles de normalisation (§3.1) et la faisabilité de la timeline par morceau (doc 23).

## 3. Identification du style (hors-ligne)

### 3.1 Normalisation

Appliquée au titre et à l'artiste avant toute recherche (règles **configurables**, fichier de données) :

| Étape | Exemple |
|---|---|
| Séparer artiste / titre si l'artiste est une chaîne ou si le titre contient « Artiste - Titre » | « Queen - Don't Stop Me Now (Official Video) » → Queen / Don't Stop Me Now |
| Supprimer les mentions parasites | (Official Video), [HD], (Lyrics), (Audio), Remastered 2011, Radio Edit, Extended Mix*, Live at… |
| Extraire les invités | « feat. X », « ft. X », « & X », « x X » → artistes secondaires |
| Nettoyer l'artiste | « XYZ - Topic », « XYZVEVO », « Official » |
| Uniformiser | minuscules, accents retirés, ponctuation retirée, « & » → « et », espaces multiples |

\* Les mentions de version (Extended, Remix) sont conservées à part : un remix peut changer de style.

### 3.2 Base musicale locale

| Élément | Contenu |
|---|---|
| **Taxonomie** | Liste des **familles** de styles (§3.4) + table de correspondance **étiquettes brutes → familles** (« eurodance », « french house » → Électro/Dance…) |
| **Artistes** | Nom normalisé, **alias** (orthographes, noms courts), styles pondérés (ex. Rock 0,8 / Pop 0,2), source (manuel, enrichissement, correction) |
| **Titres** | Artiste, titre normalisé, alias, style **spécifique** (s'il diffère de l'artiste : le slow d'un groupe de rock), BPM mémorisé (AUD-028), énergie typique, notes |
| **Corrections** | Historique des corrections faites en Live (titre / artiste, ancien → nouveau style, date) |

### 3.3 Chaîne d'identification

```
 Titre brut ─▶ Normalisation ─▶ 1. Titre exact connu ?          ──oui──▶ style du titre        (confiance 0,95)
                              ─▶ 2. Titre approché (flou) ?      ──oui──▶ style du titre        (0,7-0,9 selon score)
                              ─▶ 3. Artiste exact / alias ?      ──oui──▶ style dominant        (0,8)
                              ─▶ 4. Artiste approché (flou) ?    ──oui──▶ style dominant        (0,5-0,75)
                              ─▶ 5. Artistes invités connus ?    ──oui──▶ style de l'invité     (0,5)
                              ─▶ 6. Genre fourni par le lecteur ? ─oui──▶ via taxonomie        (0,4)
                              ─▶ 7. Sinon                         ──────▶ Inconnu              (0)
 + correction de l'utilisateur ─▶ prioritaire, confiance 1, mémorisée
```

Le **rapprochement flou** utilise une similarité tolérante (fautes, mots inversés, mots manquants) avec seuil réglable.

### 3.4 Taxonomie initiale (proposition, modifiable)

| # | Famille | Exemples | Esthétique suggérée (doc 22) |
|---|---|---|---|
| 1 | Électro / Dance | EDM, eurodance, techno grand public | Couleurs saturées, strobe sur drops, mouvements rapides |
| 2 | House / Disco moderne | House, nu-disco, deep house | Couleurs chaudes, mouvements fluides, groove |
| 3 | Hip-hop / R'n'B | Rap, R'n'B, trap | Violets / bleus, bumps sur basses |
| 4 | Pop | Pop actuelle | Couleurs variées, dynamique moyenne |
| 5 | Rock | Rock, pop-rock, hard rock | Blanc / rouge / ambre, flashs sur caisse claire |
| 6 | Années 80 | Synthpop, new wave, tubes 80 | Néons (magenta, cyan), arcs-en-ciel |
| 7 | Disco / Funk / Soul | 70's, funk | Boule, multicolore chaud, balayages |
| 8 | Latino | Salsa, reggaeton, bachata, kizomba, zouk | Jaune / orange / rouge, mouvements ondulants |
| 9 | Variété française | Chanson, variété | Doux à festif selon énergie |
| 10 | Reggae / Dancehall | Reggae, ragga | Vert / jaune / rouge, lent |
| 11 | Rock'n'roll / Rétro | 50-60's, twist | Couleurs vives franches, chenillards |
| 12 | Bal / Traditionnel | Musette, valse, madison, danses en ligne, tubes de mariage | Chaleureux, lisible |
| 13 | Festif / Tubes de soirée | Chansons à danser collectives | Multicolore, énergique |
| 14 | Slow / Ballade | Slows, ballades | Couleurs douces, mouvements lents, pas de strobe |
| — | **Inconnu** | Non identifié | Show neutre piloté par l'énergie |
| — | **Pub** | Publicités | Ambiance d'attente |

### 3.5 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MUS-020 | I | P9 | Normalisation du §3.1, règles stockées en données modifiables. | Jeu de 200 titres bruts → formes normalisées attendues. |
| MUS-021 | I | P9 | Chaîne d'identification du §3.3 avec **confiance** et **méthode** publiées dans `StyleDétecté` (EVT-042). | — |
| MUS-022 | I | P9 | Temps d'identification < 200 ms pour une base de 50 000 titres et 10 000 artistes. | Mesure. |
| MUS-023 | I | P9 | Taxonomie modifiable (ajouter, renommer, fusionner des familles) avec mise à jour des références. | — |
| MUS-024 | I | P9 | **Correction en Live** (LIVE-022) : appliquer à « ce titre » ou « cet artiste » ; prise en compte immédiate ; mémorisation. | Corriger puis rejouer le titre → nouveau style. |
| MUS-025 | I | P9 | **Journal de soirée** (GEN-111) : chaque morceau avec style, confiance, méthode, correction éventuelle. | — |
| MUS-026 | I | P9 | Style forcé (CMD-062) prioritaire sur la détection jusqu'à annulation ou fin du morceau (réglable). | — |
| MUS-027 | M | P9 | Écran **Base musicale** : recherche, édition des artistes / titres / alias / styles, fusion de doublons, import / export CSV. | — |
| MUS-028 | M | P9 | Écran **« À classer »** : titres et artistes rencontrés (journaux de soirée) non identifiés ou à faible confiance, classables rapidement (raccourcis clavier, classement par artiste en un geste). | Classer 100 titres en < 10 min. |
| MUS-029 | S | P9 | Import de **playlists** exportées en CSV (outils d'export des plateformes) pour pré-remplir la base avant une soirée. | — |

## 4. Outil d'enrichissement (à la maison)

Outil **séparé** (`Luxia.Tools.MusicEnrich`), lancé explicitement, jamais en Live (GEN-121).

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MUS-040 | M | P9 | Pour une liste d'artistes / titres (« À classer », playlists importées), interroger des sources en ligne d'étiquettes musicales (ex. Last.fm, MusicBrainz, Deezer, Discogs) et **proposer** une famille via la taxonomie. | Proposition pour 100 artistes. |
| MUS-041 | M | P9 | Les propositions sont **validées par l'utilisateur** (accepter / modifier / rejeter, en lot) avant d'entrer dans la base. | Aucune écriture sans validation. |
| MUS-042 | M | P9 | Respect des limites d'usage des services (débit, clé personnelle) ; cache des réponses. | — |
| MUS-043 | S | P12 | Option **IA locale** (petit modèle de langage exécuté sur le PC) pour proposer une famille quand les sources en ligne ne donnent rien ; mêmes règles de validation. | — |
| MUS-044 | S | P12 | Classification par **analyse audio** (modèle local) pour les morceaux inconnus, à partir d'extraits enregistrés en soirée. | — |

## 5. Preuve de concept (PoC-4)

Sur 100 titres réels de vos soirées : taux de familles correctes proposées par chaque source en ligne et par l'IA locale ;
temps de traitement. Détermine les sources retenues pour l'outil d'enrichissement.

## 6. Notes de réalisation (P9)

| Sujet | Réalisation |
|---|---|
| Lecture en cours (lot 1) | Projet `Luxia.Media` : `NowPlayingTracker` (choix de la session : la dernière démarrée parmi celles qui jouent, sinon la suivie, sinon la dernière active ; changement de morceau publié après **1 s** de stabilité ; titre vide ignoré ; position interpolée selon la vitesse de lecture) sur une source abstraite `IMediaSessionSource`. Adaptateur `Luxia.Media.Windows` (API de contrôle des médias de Windows, relecture groupée après chaque événement du système + toutes les 5 s). Événements `TrackChanged` (EVT-040) et `PlaybackChanged` (EVT-041) sur le bus. Sonde `luxia-headless media` (PoC-3, [guide](demos/P9-poc3-sonde.md)). |
| Normalisation (lot 2) | Projet `Luxia.Music` : `TrackNormalizer` produit une **liste d'hypothèses** (artiste, artistes pris un à un, invités, titre, versions) de la plus à la moins probable ; un navigateur (YouTube, YouTube Music) a un champ « artiste » qui est un nom de chaîne, donc « Artiste - Titre » et « Titre \| Artiste » sont essayés d'abord ; règles en données (`normalisation.json`, doc 50 §12i) ; jeu de 218 titres écrit à la main (T-MUS-01). Observé au PoC-3 : l'artiste est dans le titre, après « \| », et la chaîne a fait le remix. |
| Base et identification (lot 3) | `MusicBase` (taxonomie, artistes, titres, corrections, index de trigrammes), `StyleIdentifier` (chaîne du §3.3), `FuzzyMatch`, `StyleSession` (style imposé, corrections), base de départ d'environ 350 artistes ; fichiers dans le projet (doc 50 §12j). Service `MusicStyleService` (Hosting) : abonné à la lecture en cours, informe le moteur (CMD-063 : style, changement de morceau réel, lecture suivie), publie EVT-042, enregistre les corrections après 1,5 s. **D40 révise D38** : le titre réel est la source de « au morceau suivant » tant qu'un morceau est suivi ; l'écoute reste le repli. Mesures : 96 % d'identification sur 163 titres annotés, 0 faux positif, pire cas 2,9 ms sur 50 000 titres. |
| Plateforme | `Luxia.Media.Windows` cible `net10.0-windows10.0.19041.0` (projection de l'API Windows) ; l'application, `luxia-headless` et `luxia-captures` aussi. Les autres projets restent en `net10.0`. |
| Écarts | MUS-004 (publicités) **abandonnée** (Q43). La miniature (MUS-003) n'est pas lue. |

## 7. Tests

| Test | Type | Contenu |
|---|---|---|
| T-MUS-01 | Unitaire | Normalisation : 200 titres bruts réels (Spotify, Deezer, YouTube) → attendus. |
| T-MUS-02 | Unitaire | Rapprochement flou : fautes, inversions, mots manquants, faux positifs à éviter. |
| T-MUS-03 | Jeu de test | **Taux d'identification** sur 300 titres annotés (objectif initial : > 80 % de familles correctes quand la base contient l'artiste ; mesure du taux « Inconnu »). Rapport chiffré à chaque évolution. |
| T-MUS-04 | Intégration | Lecture en cours : lecteurs du PoC-3 ; pubs ; changement de session. |
| T-MUS-05 | Performance | Base de 50 000 titres : < 200 ms. |
| T-MUS-06 | Hors-ligne | Toute la chaîne avec réseau désactivé. |
