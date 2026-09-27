# 12 – Bibliothèque d'appareils

> Cahier des charges – module **Bibliothèque** (préfixe `BIB`). Phase principale : **P2**.
> Références : [02 §7 (unités), §10 (données)](02-principes-et-architecture-fonctionnelle.md), [11 (console)](11-console.md), [glossaire](glossaire.md).
> Inspirations : **Open Fixture Library** (OFL, format JSON ouvert), **QLC+** (format `.qxf`), GDTF.

---

## 1. Rôle

Décrire **ce qu'est** chaque modèle d'appareil et **comment il se pilote** en DMX, de façon suffisamment riche pour que :

- le moteur convertisse des intentions (« rouge », « 50 % », « piste centre ») en octets ;
- la console et le programmeur proposent les bons outils (couleur, pad Pan/Tilt, boutons de plages) ;
- le simulateur représente l'appareil ;
- les palettes automatiques soient générées ;
- les règles de sûreté sachent quels canaux sont un strobe ou une fumée.

## 2. Modèle de données

### 2.1 Vue d'ensemble

```
Modèle d'appareil
 ├─ Identité : id, fabricant, nom, référence, catégorie, version, auteur, source (saisie / OFL / QLC+), notes, photo
 ├─ Physique : type de source (LED RGB, RGBW, UV…), angle de faisceau, amplitude Pan (°), amplitude Tilt (°),
 │             vitesse max Pan/Tilt, temps de chauffe (fumée), nombre de têtes / cellules
 ├─ Roues : roues de couleur, roues de gobos (emplacements : nom, couleur(s), image)
 ├─ Définitions de canaux (réutilisables entre modes)
 │    └─ Canal : nom, attribut, cellule, 8/16 bits, défaut, repos, identification, plages[]
 │                                                               └─ Plage : min, max, type, libellé, paramètres
 └─ Modes[] : nom, nom court (ex. « 7CH »), liste ordonnée des canaux (références), réglage à faire sur l'appareil (ex. « A001 »)
```

### 2.2 Catégories d'appareils

`PAR`, `Barre LED`, `Lyre (spot/wash/beam)`, `Effet` (multi-têtes, derby…), `Stroboscope`, `UV`, `Machine à fumée / brouillard`,
`Laser`, `Gradateur / Décodeur`, `Autre`. La catégorie sert au tri, aux icônes, au simulateur et aux sélections automatiques.

### 2.3 Catalogue des attributs

Chaque canal est lié à **un** attribut du catalogue (fermé mais extensible par version de l'application).

| Famille | Attributs | Remarques |
|---|---|---|
| **Intensité** | `Intensité` (dimmer maître), `Intensité cellule` | Soumis au Grand Master / blackout |
| **Couleur** | `Rouge`, `Vert`, `Bleu`, `Blanc`, `Blanc chaud`, `Ambre`, `UV`, `Cyan`, `Magenta`, `Jaune`, `Lime` | Émetteurs de mélange |
| **Couleur** | `Roue de couleur`, `Macro couleur` (couleurs préenregistrées), `Température de couleur` | |
| **Position** | `Pan`, `Tilt`, `Pan continu`, `Tilt continu`, `Vitesse Pan/Tilt` | 16 bits possibles |
| **Faisceau** | `Strobe / Obturateur`, `Gobo`, `Rotation gobo`, `Prisme`, `Rotation prisme`, `Focus`, `Zoom`, `Iris`, `Frost` | `Strobe` est soumis aux limites de sûreté |
| **Mouvement d'effet** | `Rotation` (moteur d'effet), `Vitesse de rotation` | Ex. moteurs Y1-Y4 de l'effet multi-têtes |
| **Programmes** | `Programme interne`, `Vitesse programme`, `Sensibilité son`, `Mode (sélecteur de fonction)` | Pilotés via plages |
| **Atmosphère** | `Fumée` (débit / déclenchement), `Ventilateur` | `Fumée` est soumis aux limites de sûreté |
| **Contrôle** | `Reset`, `Maintenance`, `Lampe on/off` | Jamais animés par les effets |
| **Divers** | `Générique`, `Sans fonction` | |

### 2.4 Canal

| Champ | Description | Obligatoire |
|---|---|---|
| Nom | Libellé du constructeur (ex. « CH6 Sélecteur de fonction ») | oui |
| Attribut | Voir catalogue | oui |
| Cellule | N° de tête / segment (0 = appareil entier) | non |
| Résolution | 8 bits, ou 16 bits (avec référence du canal **fin**) | oui |
| Valeur par défaut | Valeur quand rien ne pilote l'attribut (étape 1 de la chaîne de rendu) | oui |
| Valeur de repos | Valeur « appareil éteint proprement » (utilisée par le blackout pour les canaux non-intensité si nécessaire, ex. shutter fermé) | non |
| Valeur d'identification | Valeur utilisée par la commande « Identifier » | non |
| Inversion | Sens inversé (0 = max) | non |
| **Suit l'intensité** | Le canal est multiplié par l'intensité logique de l'appareil (lien « au dimmer », comme dans Daslight). Déduit automatiquement (§2.7), modifiable canal par canal | déduit |
| Plages | Liste des plages (vide = canal continu 0-255 sans signification particulière) | non |
| Étiquettes de sûreté | `strobe`, `fumée`, `mouvement` (déduites de l'attribut, modifiables) | déduit |

### 2.5 Plage (capacité)

| Champ | Description |
|---|---|
| Min, Max | Bornes DMX (0-255) incluses |
| Libellé | Texte affiché (ex. « Strobe lent → rapide ») |
| Type | `Fixe` (une seule signification), `Progressif` (un paramètre varie de début à fin), `Emplacement de roue`, `Rotation`, `Programme`, `Sans fonction`, `Arrêt / fermé`, `Ouvert` |
| Paramètre | Pour `Progressif` : nature (vitesse, fréquence Hz, angle, %…) + valeur de début + valeur de fin (ex. 1 Hz → 20 Hz) |
| Couleur(s) | Pour `Emplacement de roue` couleur ou `Macro couleur` : code(s) couleur (1 ou 2 si demi-couleur) |
| Image | Pour un gobo : image du motif (facultatif) |
| Effet de strobe | Pour `Strobe / Obturateur` : `Fermé`, `Ouvert`, `Strobe`, `Pulsation`, `Aléatoire`… |
| Palette automatique | Oui / non (génère un bouton de palette, cf. doc 17) |

Exemple — canal Strobe d'un PAR :

| Min | Max | Type | Libellé | Effet de strobe | Paramètre |
|---:|---:|---|---|---|---|
| 0 | 10 | Arrêt / fermé | Éteint | Fermé | — |
| 11 | 50 | Ouvert | Allumé fixe | Ouvert | — |
| 51 | 200 | Progressif | Strobe lent → rapide | Strobe | 1 Hz → 20 Hz |
| 201 | 255 | Fixe | Strobe aléatoire | Aléatoire | — |

### 2.6 Cellules (têtes, segments)

Certains appareils ont plusieurs parties pilotables séparément : **barre LED** à segments (ex. 8 × RGB), **effet multi-têtes**
(ex. 4 têtes motorisées). Un mode peut déclarer **N cellules** ; chaque canal est rattaché à une cellule (ou à l'appareil entier).
Les cellules permettent aux effets de se répartir *dans* un appareil (chenillard le long d'une barre, têtes décalées).

### 2.7 Intensité virtuelle

Si un mode n'a **pas** de canal `Intensité` mais possède des émetteurs de couleur (ex. PAR RGB en mode 3 canaux), le modèle est
marqué **« intensité virtuelle »** (déduit automatiquement) : le moteur multiplie les émetteurs par l'intensité logique.

C'est l'équivalent du **lien des canaux au dimmer** de Daslight, mais réglé **canal par canal** (propriété « Suit l'intensité », §2.4) :

| Cas | Canaux qui suivent l'intensité (par défaut) |
|---|---|
| Mode **sans** canal Intensité (PAR RGB 3CH) | Tous les émetteurs de couleur (R, G, B, W, ambre, UV…) |
| Mode **avec** canal Intensité (PAR 7CH) | Aucun : c'est le canal Intensité réel qui porte la luminosité (sinon on atténuerait deux fois) |
| Canaux non lumineux (Pan, Tilt, gobo, strobe, programme, fumée…) | Jamais |

Exemple (PAR RGB 3CH, blanc) : couleur R = G = B = 100 %, intensité logique 80 % → émis R = G = B = 204 (80 % de 255).
Tout ce qui agit sur l'intensité passe par ce lien : scènes de la couche Intensité, master de couche, **Grand Master**, **blackout**, fondus.
La couleur et la luminosité restent ainsi réglables séparément, même sur un appareil qui n'a pas de dimmer.

> Limite physique : à très faible intensité, un canal 8 bits multiplié n'a plus que quelques pas (ex. 5 % de 255 ≈ 13 niveaux) ;
> les fondus lents vers le noir peuvent paraître « en escalier ». C'est inhérent à l'appareil, pas au logiciel.

## 3. Exigences – modèle et validation

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| BIB-001 | I | P2 | Le modèle de données permet de décrire l'ensemble des éléments du §2 (identité, physique, roues, canaux, plages, modes, cellules). | Saisie complète des appareils du parc (annexe A). |
| BIB-002 | I | P2 | Un modèle possède **au moins un mode** ; un mode référence des définitions de canaux (une même définition peut servir à plusieurs modes). | Modèle 3 canaux / 7 canaux partageant R, G, B. |
| BIB-003 | I | P2 | Un attribut 16 bits est décrit comme **un seul attribut** occupant deux canaux (grossier + fin), les deux étant positionnés indépendamment dans chaque mode. | Pan 16 bits en CH1 + CH2 → pad Pan unique. |
| BIB-004 | I | P2 | **Validation** à l'enregistrement : plages qui se chevauchent (erreur) ; trous entre plages (avertissement) ; canal fin orphelin (erreur) ; mode sans canal (erreur) ; deux canaux d'un mode sur la même position (erreur). | Jeu de modèles invalides → erreurs attendues. |
| BIB-005 | I | P2 | Le mode affiche son **nombre de canaux** et le **réglage à faire sur l'appareil** (ex. « A001 = mode 7 canaux »). | Revue. |
| BIB-006 | I | P2 | L'intensité virtuelle est déduite automatiquement (§2.7) ; la propriété **« Suit l'intensité »** est pré-remplie selon le tableau du §2.7 et **modifiable canal par canal** dans l'éditeur (case à cocher). | PAR RGB 3 canaux → R, G, B cochés ; PAR 7 canaux → aucun coché ; décocher B → le bleu ne suit plus le dimmer. |
| BIB-007 | I | P2 | Les étiquettes de sûreté (`strobe`, `fumée`) sont déduites de l'attribut et modifiables (ex. un canal « Programme » dont une plage est un strobe). | Plage de strobe dans un canal programme → étiquetée. |
| BIB-008 | M | P2 | Les plages de type **emplacement de roue** portent une couleur ; le simulateur et les palettes couleur « par intention » s'en servent. | Roue de lyre saisie → couleurs visibles. |
| BIB-009 | M | P2 | Un modèle porte une **version** incrémentée à chaque modification enregistrée. | — |
| BIB-010 | S | P2 | Un modèle peut être **dérivé** d'un autre (copier puis modifier), avec mention de l'origine. | Dupliquer LPC008S → LPC010. |
| BIB-101 | I | P5 | Canal Strobe du **LPC008S**, du **LPC120** et de la **LCB803** : plage « Pas de strobe » distincte de la plage de strobe (Q28), pour que le simulateur, `jouer` et le limiteur de strobe (MOT-080) ne prennent pas un appareil qui éclaire fixe pour un strobe. | LPC008S : 0-4 néant, 5-255 strobe ; LCB803 : 0 néant, 1-255 strobe ; LPC120 comme le LPC008S (à confirmer). |

## 4. Exigences – éditeur

```
┌ Bibliothèque ───────────────────────────────────────────────────────────────────────────────────┐
│ [Rechercher…      ] [Fabricant ▾] [Catégorie ▾]  │  Betopper LPC008S  – PAR – v3                 │
│ ▸ BeamZ                                          │  Modes : [3CH d001] [7CH A001] [+]            │
│    BUV463 (UV)                                   │ ┌ Canaux du mode 7CH ──────────┐┌ Plages du canal 6 ─────────────┐│
│    LCB803 (Barre)                                │ │ 1 Intensité                   ││ 0-50    Gradation DMX          ││
│ ▾ Betopper                                       │ │ 2 Rouge   3 Vert   4 Bleu     ││ 51-100  Couleurs fixes (8)     ││
│    LPC008S (PAR) ◀                               │ │ 5 Strobe                      ││ 101-150 Fondu                  ││
│    LPC010  (PAR)                                 │ │ 6 Sélecteur de fonction ◀     ││ …                               ││
│ ▸ Génériques                                     │ │ 7 Vitesse                     ││ ▓▓▓▓░░░░▒▒▒▒████ (barre 0-255) ││
│                                                  │ └───────────────────────────────┘└─────────────────────────────────┘│
│ [+ Nouveau] [Importer…]                          │  [Tester en direct]  [Valider]  [Enregistrer]                      │
└──────────────────────────────────────────────────┴──────────────────────────────────────────────────────────────────────┘
```

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| BIB-020 | I | P2 | Liste des modèles groupés par fabricant, avec recherche et filtres (fabricant, catégorie). | Revue. |
| BIB-021 | I | P2 | Édition des modes en onglets ; ajout, suppression, réordonnancement des canaux d'un mode par glisser-déposer. | Revue. |
| BIB-022 | I | P2 | Édition des plages d'un canal dans un tableau + **barre 0-255** colorée par plage, redimensionnable à la souris. | Revue. |
| BIB-023 | I | P2 | Saisie rapide de plages : « découper en N plages égales », « remplir le trou suivant ». | Créer 8 plages de couleur en une opération. |
| BIB-024 | I | P2 | Annuler / rétablir (GEN-102). | — |
| BIB-025 | M | P2 | Éditeur de roues : emplacements avec nom, couleur (sélecteur), image de gobo. | — |
| BIB-026 | M | P2 | Aperçu de la fiche « réglage sur l'appareil » (mode + adresse) telle qu'elle apparaîtra à l'installation. | — |
| BIB-027 | S | P2 | Joindre la notice PDF et une photo au modèle (fichiers liés). | Ouvrir la notice depuis l'éditeur. |

## 5. Exigences – test en direct

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| BIB-060 | I | P2 | Bouton **Tester en direct** : patch temporaire du modèle (mode courant) à une adresse choisie, puis affichage des faders de l'appareil (composant de la console, CONS-060). | Tester un PAR réel à l'adresse 1. |
| BIB-061 | I | P2 | Clic sur une plage → émission de sa valeur médiane ; curseur de balayage dans la plage ; clic sur une bordure → émission de la valeur exacte de la borne. | Vérification des plages Strobe sur matériel. |
| BIB-062 | M | P2 | Mode « **découverte** » : balayer lentement un canal de 0 à 255 en affichant la valeur, avec bouton « Nouvelle plage ici » pour créer une borne à la valeur courante. | Construire les plages d'un canal inconnu en observant l'appareil. |
| BIB-063 | M | P2 | La sortie du test est la sortie active (Arduino et/ou simulateur) ; à la fermeture du test, le patch temporaire et ses surcharges sont supprimés. | Aucune trace après fermeture. |

## 6. Exigences – import / export

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| BIB-080 | I | P2 | **Import Open Fixture Library** (fichier JSON d'un appareil ou dossier) : modes, canaux, capacités → plages, roues, fins (16 bits), cellules (matrices) ; les types de capacités OFL sont convertis vers le catalogue d'attributs. | Import de 5 appareils OFL de référence → modèles valides, comparés à un résultat attendu. |
| BIB-081 | M | P2 | **Import QLC+** (`.qxf`) : canaux (groupes → attributs), capacités → plages, modes, têtes → cellules. | Import de 5 fichiers QLC+ de référence. |
| BIB-082 | I | P2 | Rapport d'import : éléments non convertis ou approximés, listés clairement ; l'import ne bloque jamais sur un élément inconnu (converti en `Générique`). | Rapport lisible. |
| BIB-083 | M | P2 | Import de **lots** (dossier entier) sans figer l'interface (GEN-109). | Import de 1 000 fichiers. |
| BIB-084 | S | P2 | Export au format OFL (partage, contribution). | Fichier validé par l'outil de validation OFL. |
| BIB-085 | S | P12 | Import GDTF. | — |

Les imports se font à partir de **fichiers locaux** (téléchargés à la maison) : aucune dépendance Internet (GEN-120).

## 7. Modèles génériques livrés

| Modèle générique | Modes |
|---|---|
| Gradateur | 1 canal (Intensité) |
| RGB | 3 canaux (R, G, B), 4 canaux (Intensité + RGB) |
| RGBW | 4 canaux, 5 canaux (Intensité + RGBW) |
| Machine à fumée | 1 canal (Fumée : 0 = arrêt, 1-255 = émission) |
| Stroboscope simple | 2 canaux (Intensité, Vitesse) |
| Canal générique | 1 canal |

## 8. Stockage

- Un fichier JSON **par modèle** dans le dossier Bibliothèque (GEN-050), nommé `fabricant/modele.json`.
- Le projet embarque une **copie** des modèles utilisés (GEN-053) ; l'écran Bibliothèque signale les modèles du projet
  qui ont une version plus récente en bibliothèque, avec « Mettre à jour » et rapport d'impacts (canaux ajoutés/supprimés,
  adresses décalées, attributs disparus utilisés par des scènes).

## 9. Tests

| Test | Type | Contenu |
|---|---|---|
| T-BIB-01 | Unitaire | Validation : chaque règle de BIB-004 sur un jeu de modèles invalides. |
| T-BIB-02 | Unitaire | Import OFL / QLC+ : fichiers de référence → modèles attendus (comparaison JSON). |
| T-BIB-03 | Unitaire | Aller-retour enregistrement / chargement sans perte. |
| T-BIB-04 | Unitaire | Intensité virtuelle et étiquettes de sûreté déduites correctement. |
| T-BIB-05 | Manuel | Saisie et vérification **en direct** de chaque appareil du parc, plage par plage (check-list annexe A). |

---

## 10. Notes de réalisation (P2)

| Sujet | Réalisation |
|---|---|
| Modèle | Projet `Dmx.Fixtures`. Attribut = `AttributeKind` (liste du §2.3). Une définition 16 bits a une seule clé ; ses octets grossier et fin sont deux positions `coarse` / `fine` d'un mode. Plage = `Capability`. Format de fichier : doc 50 §6. |
| BIB-003 | Passer un canal en 16 bits ajoute l'octet fin juste après l'octet grossier dans chaque mode (déplaçable) ; revenir en 8 bits le retire. |
| BIB-004 | Erreurs : identité vide, clé ou nom de mode en double, plages invalides ou qui se chevauchent, mode sans canal, plus de 512 canaux, même canal à deux positions, canal inconnu, octet fin d'un canal 8 bits, canal fin orphelin, roue inconnue. Avertissements : trous (y compris début / fin de 0-255), 16 bits sans octet fin dans un mode. L'enregistrement est refusé s'il reste une erreur. |
| BIB-006 | Émetteur d'une cellule : suit l'intensité si ni l'appareil ni sa cellule n'ont de gradateur. L'éditeur affiche la valeur déduite pour le mode choisi. |
| BIB-007 | Plage d'effet Strobe, Pulsation ou Aléatoire → étiquette « strobe », même sur un canal Programme. Étiquettes imposables (case « automatique » décochée). |
| BIB-009 | Version incrémentée à chaque enregistrement d'un modèle déjà présent. |
| BIB-010 | « Dupliquer » : nouvel identifiant, « (copie) », `derivedFrom`, remarque « Dérivé de … ». C'est aussi la façon de modifier un générique (lecture seule). |
| BIB-021 | Glisser-déposer dans la liste des positions (appui sur une ligne, relâche sur une autre) + boutons ▲ ▼. |
| BIB-022 | Barre 0-255 : frontières déplaçables à la souris entre plages adjacentes. |
| BIB-024 | Annuler / rétablir : 100 niveaux (état complet du modèle, immuable). |
| BIB-060 | Le test en direct prend les canaux à leur **valeur par défaut**, en surcharges brutes (CMD-020) : l'étape « attributs » du moteur arrive en P4. |
| BIB-061, 062 | Clic plage = médiane ; clic frontière = borne exacte ; découverte : balayage (1-100 valeurs/s), pause, ±1, « Nouvelle plage ici » coupe la plage courante à cette valeur. |
| BIB-080, 081 | Fichiers de référence **rédigés** au format OFL / QLC+ (`tests/Dmx.Fixtures.Tests/assets`) : aucun téléchargement n'a été fait sans l'accord de l'utilisateur. À compléter par de vrais fichiers téléchargés. |
| BIB-082, 083 | Import par lots hors du fil de l'interface avec progression ; un modèle déjà présent (même fabricant + modèle, ou même identifiant) n'est **pas** écrasé (« = » au rapport). |
| Unités (GEN-021) | Faders d'appareil : nom de plage, degrés (Pan/Tilt si l'amplitude est connue), % (intensités, émetteurs), sinon 0-255. |
| Non réalisés | BIB-027 (S, ouvrir notice / photo : seul le chemin est saisi), BIB-084 (S, export OFL). |
| Parc (annexe A) | 6 définitions dans `samples/Bibliothèque/` (script d'amorçage `generer.py`) ; tableaux LPC010 / LPC120 lus en rendant les PDF en images. LCB803 : saisie le 2026-09-25 d'après les pages fournies (Q24) ; programme « rapide → lent » sans plage d'arrêt documentée, à vérifier ; strobe : 0 = néant, 1-255 = strobe croissant (Q28, BIB-101). WZYBUTA : à vérifier (Q25). Lyre Tomshine : **refaite** le 2026-09-25, confirmée par Open Fixture Library et vérification en direct de l'utilisateur (BIB-095) ; Tilt 0-230° (pas 180°) ; obturateur ouvert par défaut (0) ; canal Reset en dernière position (121 en 11CH). |

## Annexe A – Appareils du parc (état de la documentation)

| Rôle | Qté | Modèle | Modes connus | Documentation | État de la définition |
|---|---|---|---|---|---|
| PAR | 4 | Betopper LPC008S (RGB) | 3CH (`d001`), 7CH (`A001`) | PDF + fiche `betopper-lpc008s.md` | Tableau complet connu – à saisir |
| Gros PAR | 2 | Betopper LPC010 ou LPC120 (RGBW) | 4CH (`d001`), 8CH (`A001`) | PDF image (lus par rendu en P2) | **Saisi** (`samples/Bibliothèque/Betopper/`) |
| Gros PAR (réels) | 2 | Générique **WT05** (PAR 160 W, RGB, sans marque) | 7CH, mêmes commandes que le LPC008S | Aucune notice (utilisateur, essai P5) | **Saisi** (`samples/Bibliothèque/Générique/WT05.json`) ; remplace le LPC120 dans le patch |
| Lyre | 2 | Tomshine (lyre à gobos) | 9CH, 11CH (Pan/Tilt 16 bits en 11CH) | Photo de la notice (2026-09-25) | **Saisi** ; roue de couleur, gobo et canal Son à vérifier en direct (BIB-095) |
| UV | 2 | BeamZ BUV463 (UV strobe) | **8CH** réel (7CH de la notice + 8e canal « lissage » non documenté, essai P5) | PDF texte | Saisi ; 8e canal ajouté le 2026-09-27 |
| Barre LED | 2 | BeamZ LCB803 (80 × 3-en-1) | 3, 6, 12, 24, 48CH (menu `ChNd`) ; 2 / 4 / 8 sections | Pages fournies le 2026-09-25 (Q24) | **Saisi** (`samples/Bibliothèque/BeamZ/LCB803.json`) |
| Effet multi-têtes | 1 | WZYBUTA Moving Head 150 W (plateau + 4 barrettes de 3 projecteurs RGBW) | 20CH, 64CH (la notice décrit un 16CH qui ne correspond pas) | Captures ScanLibrary de l'utilisateur (2026-09-25) | **Saisi** (12 cellules en 64CH ; canaux 17-19 / 61-63 = laser optionnel absent) |
| Fumée | 1 | — | 1CH | — | Modèle générique « Machine à fumée » |

Chaque définition sera **vérifiée sur le matériel** avec le test en direct (BIB-060) avant la phase P3.
