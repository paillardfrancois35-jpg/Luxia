# Projet LuXia – Analyse du besoin, découpage et feuille de route

> Document 00 – préalable au cahier des charges.
> Objet : analyser le souhait de projet, identifier forces / faiblesses, trier les idées,
> proposer une architecture de modules, un vocabulaire commun, un ordre de réalisation et une stratégie de tests.
> Les règles de développement (style de code, conventions, outillage) sont hors sujet ici.

---

## Sommaire

1. [Synthèse en une page](#1-synthèse-en-une-page)
2. [Reformulation du besoin](#2-reformulation-du-besoin)
3. [Forces du projet](#3-forces-du-projet)
4. [Faiblesses et risques](#4-faiblesses-et-risques)
5. [Idées à garder, à transformer, à oublier](#5-idées-à-garder-à-transformer-à-oublier)
6. [Vocabulaire commun (glossaire proposé)](#6-vocabulaire-commun-glossaire-proposé)
7. [Une solution ou plusieurs ? Architecture proposée](#7-une-solution-ou-plusieurs--architecture-proposée)
8. [Analyse module par module](#8-analyse-module-par-module)
9. [Modules manquants à ajouter](#9-modules-manquants-à-ajouter)
10. [Le moteur de rendu : le cœur du système](#10-le-moteur-de-rendu--le-cœur-du-système)
11. [Le mode automatique : la finalité](#11-le-mode-automatique--la-finalité)
12. [Expérience utilisateur : parcours types](#12-expérience-utilisateur--parcours-types)
13. [Ordre de réalisation : phases et jalons](#13-ordre-de-réalisation--phases-et-jalons)
14. [Stratégie de tests](#14-stratégie-de-tests)
15. [Questions ouvertes à trancher avant le cahier des charges](#15-questions-ouvertes-à-trancher-avant-le-cahier-des-charges)
16. [Plan du futur cahier des charges](#16-plan-du-futur-cahier-des-charges)

---

## 1. Synthèse en une page

**Le besoin** : un logiciel d'éclairage personnel, plus léger que Daslight 4, taillé pour un petit parc
(PAR RGB/RGBW, barre RGB, UV, machine à fumée, 2 lyres à roue de couleur), capable à terme de
**tourner seul pendant une soirée dansante** en s'adaptant à la musique (style, tempo, énergie).

**Verdict global** : projet réaliste et bien motivé, **à condition** de :

1. **Construire tôt un simulateur visuel** (vous n'avez pas le matériel monté pour tester : c'est LE frein n°1 du projet).
2. **Mettre le moteur de rendu (fusion des couches, priorités, fondus) au centre dès le début** – ce n'est pas un détail « à revoir plus tard », c'est le cœur.
3. **Stocker les scènes en langage « appareil » (Rouge, Dimmer, Pan) et non en adresses DMX brutes**.
4. **Remplacer l'astuce « scène qui saute vers N scènes » par un vrai séquenceur de type Grafcet** – votre cursus d'automaticien est ici un atout direct.
5. **Ne pas miser sur une énorme bibliothèque titre→style** : raisonner par **artiste**, s'appuyer sur des **API musicales + cache local**, et surtout **combiner le style (esthétique) avec l'analyse audio (énergie)**.
6. **Livrer vite une version « soirée manuelle » utilisable**, avant d'attaquer l'automatique. Un projet de cette taille qui ne sert à rien pendant 18 mois s'arrête.

**Architecture** : **une seule solution C#**, découpée en **plusieurs projets** (bibliothèques de classes) + une application hôte.
Pas de solutions séparées, pas de chargement dynamique de plugins : des modules compilés ensemble, qui communiquent
par des interfaces et un bus d'événements, avec un moteur totalement indépendant de l'interface.

**Ordre recommandé** (≠ numérotation de vos modules) :
Sortie DMX → Console de faders → Bibliothèque d'appareils → Installation/Patch + Simulateur → Moteur + Scènes →
Couches + Palettes + écran Live (**jalon « soirée manuelle »**) → Effets générés → Audio/BPM → Séquenceur de show →
Lecture en cours + Style → Directeur automatique (**jalon « soirée automatique »**) → Timeline.

---

## 2. Reformulation du besoin

### 2.1 Contexte d'usage

| Élément | Constat |
|---|---|
| Utilisateur | Une seule personne, technicien (électricité / informatique / électronique), connaît Daslight 4 |
| Lieu | Variable (soirées, salles différentes) → le matériel est **installé à chaque fois** à des positions différentes |
| Parc | ~8 à 12 appareils, probablement < 150 canaux DMX au total → **un seul univers suffit largement** aujourd'hui |
| Interface DMX | Arduino Leonardo + shield DMX, piloté en USB série – **déjà validé** |
| Musique | Jouée sur le PC (lecture en cours récupérée via l'API Windows – **déjà validé**) |
| Objectif final | À l'ouverture de la piste de danse : **bascule en mode automatique**, le show se pilote seul |
| Contrainte forte | Monter tout le matériel pour tester est très pénible |

### 2.2 Ce que l'utilisateur veut réellement (au-delà des modules listés)

- **Préparer chez soi, sans matériel**, et arriver en soirée avec un show prêt.
- **Installer vite** sur place (adresser, vérifier, caler les lyres sur la piste).
- **Garder la main à tout moment** (blackout, flash, fumée, forcer une ambiance) même en automatique.
- **Un show non répétitif** qui colle au style et à l'intensité de la musique.
- **Une navigation plus simple** que Daslight (le nombre de scènes y devenait « imbuvable »).

---

## 3. Forces du projet

1. **Besoin réel et vécu.** Vous connaissez les limites d'un outil existant : vous savez ce qui manque (plusieurs sources BPM, enchaînements de show, navigation). C'est la meilleure base pour un cahier des charges.
2. **Sortie matérielle déjà validée.** Le risque technique le plus « bas niveau » (parler DMX) est levé.
3. **Lecture en cours déjà validée** (API Windows `GlobalSystemMediaTransportControls`).
4. **Parc modeste.** Aucune contrainte de performance sérieuse : 512 octets à ~40 Hz, c'est trivial pour un PC. On peut privilégier la clarté à l'optimisation.
5. **Bonne intuition sur les couches par attribut** (un groupe pour les couleurs, un pour le strobe, un pour les mouvements…). C'est exactement le principe des consoles professionnelles (couches / « playbacks » + priorités). Cela réduit drastiquement le nombre de scènes : 6 couleurs × 4 mouvements × 3 strobes = 13 scènes au lieu de 72.
6. **Culture automatisme (Grafcet).** Le séquencement de show est un problème d'automate à états : vous avez la grille de lecture idéale.
7. **Pensée modulaire.** Vous avez déjà identifié que les modules doivent communiquer et pouvoir s'imbriquer.
8. **Objectif final clair et mesurable** : « je lance le mode auto et ça tient la soirée ».

---

## 4. Faiblesses et risques

| # | Faiblesse / risque | Gravité | Réponse proposée |
|---|---|---|---|
| F1 | **Périmètre énorme** (11 modules dont 3 de R&D : BPM, style, auto). Risque d'effet tunnel et d'abandon. | 🔴 | Jalons utilisables tôt (« soirée manuelle » avant tout module audio). |
| F2 | **Tests matériels difficiles** mais aucun module de simulation prévu. | 🔴 | Ajouter un **simulateur 2D** (et une sortie Art-Net) dès la phase d'installation. |
| F3 | **Priorisation des canaux reportée** (« on reverra ça »). Or c'est le cœur du moteur : si on se trompe, tout est à refaire. | 🔴 | Définir les règles de fusion dès la conception du moteur (§10). |
| F4 | **Vocabulaire non standard** : votre « univers » = une installation, votre « galaxie » = ce que tout le monde appelle un univers. Votre « channel » = un attribut multi-canaux. | 🟠 | Adopter le glossaire §6 avant d'écrire le cahier des charges (sinon confusion avec la doc Art-Net, OFL, GDTF…). |
| F5 | Risque de **scènes stockées en valeurs DMX brutes** (comme un enregistrement de console). Changer une adresse ou un mode casse tout. | 🔴 | Scènes exprimées en **attributs d'appareils** ; conversion en DMX uniquement au rendu. |
| F6 | **Bibliothèque géante titre → style** : coûteuse à constituer, à maintenir, et fragile face aux titres imparfaits. | 🟠 | Raisonner par artiste, API + cache, IA locale en enrichissement hors-ligne seulement (§8.11). |
| F7 | **Timeline calée sur un morceau précis** placée en « bouquet final » : énorme effort par morceau, peu rentable en soirée dansante (on ne sait pas quels morceaux passeront). | 🟠 | La transformer en **séquences exprimées en mesures** (réutilisables sur tout morceau au même tempo) ; la version « par morceau » devient optionnelle. |
| F8 | **« Fréquence élevée »** : le DMX512 plafonne physiquement à ~44 trames/s pour 512 canaux. Envoyer plus vite depuis le PC ne sert à rien. | 🟢 | Moteur cadencé à 40–44 Hz ; l'Arduino rafraîchit seul la ligne. |
| F9 | **Palettes (module 8) séparées** de la définition d'appareil alors qu'elles en découlent directement. | 🟢 | Plages de valeurs (définition) et palettes (choix utilisateur) conçues ensemble. |
| F10 | **Positions des lyres dépendantes du lieu** : une scène « lyres vers la piste » n'a pas les mêmes Pan/Tilt d'une salle à l'autre. Non mentionné. | 🟠 | Palettes de position **par lieu** + calibration rapide à l'installation. |
| F11 | **Sécurité / robustesse en soirée** non évoquées : plantage appli, câble USB arraché, strobe prolongé (épilepsie), fumée en continu, lyres dans les yeux. | 🟠 | Exigences de sûreté dès le cahier des charges (§11.5). |
| F12 | Nom « groupe » utilisé pour deux choses (groupe de scènes exclusives / groupe de projecteurs). | 🟢 | Deux mots différents (§6). |

---

## 5. Idées à garder, à transformer, à oublier

### 5.1 À garder telles quelles ✅

- Bibliothèque d'appareils avec **modes** (un même appareil en 5 ou 9 canaux).
- **Plages de valeurs nommées** sur un canal (Strobe : 0-10 fermé, 11-50 ouvert…).
- Console de faders simple pour tester et **vérifier une définition d'appareil en direct**.
- Scène = suite d'étapes ; une « cue » = scène à une étape en boucle. **Modèle unifié, excellent.**
- Scène avec durée et option « boucler à l'infini ».
- Groupes de scènes **exclusifs** par défaut, joués en parallèle entre groupes.
- **Masquer une scène côté Live** (l'« œil » de Daslight) : distinguer scènes « de travail » et scènes « de jeu ».
- Plusieurs interfaces de sortie possibles (plusieurs Arduino) – **dans le modèle**, pas forcément implémenté tout de suite.
- Écoute de la lecture en cours.
- Plusieurs « sortes » de réactivité musicale (au lieu d'un seul BPM).

### 5.2 À transformer 🔄

| Idée d'origine | Transformation proposée | Pourquoi |
|---|---|---|
| « Univers » = config matériel, « Galaxie » = interface 512 canaux | **Installation** (ou Patch) contient des **Univers** ; chaque univers est relié à une **Sortie** (Arduino, Art-Net…) | Vocabulaire standard, évite les contresens. |
| « Channel » (RGB, Pan/Tilt…) | **Attribut** (ou Fonction) : un concept logique qui peut occuper 1..n canaux | Un canal DMX = 1 octet. RGB = 1 attribut, 3 canaux. |
| « Presets » d'un channel | **Plages** (définition physique, fixée par le fabricant) **+ Palettes** (valeurs choisies par vous : « Rouge chaud », « Piste centre ») | Ce sont deux choses différentes : l'une décrit l'appareil, l'autre votre intention artistique. |
| Scène qui saute vers N scènes pour faire un show | **Séquenceur de show type Grafcet** : étapes (qui activent des scènes), transitions (conditions : temps, mesures, changement de morceau, drop, bouton…), divergences ET/OU, choix aléatoire pondéré | Sépare le **contenu** (scènes) de la **logique** (pilotage) : c'est ce mélange qui faisait exploser le nombre de scènes. |
| Timeline calée sur une musique (module 6) | D'abord **séquences en mesures** (8/16/32 temps, rejouables sur n'importe quel morceau), puis en option **timeline par morceau** | Rentabilité en soirée ; la timeline par morceau reste pour les moments forts (ouverture de bal, première danse…). |
| Bibliothèque énorme titre → style | **Artiste → style** + API (Last.fm, MusicBrainz, Deezer, Discogs) + cache + correction manuelle mémorisée + **énergie audio** | Beaucoup moins de données, bien plus robuste. |
| Plusieurs « écoutes BPM » | **Une** analyse audio produisant **3 signaux** : horloge tempo, impulsions par bande, énergie (§8.10) | Répond au besoin sans multiplier les analyses. |
| Palettes par projecteur (module 8, tardif) | Palettes générées **automatiquement** depuis la définition + palettes utilisateur **par intention** (« Rouge » fonctionne sur un PAR RGB *et* sur une lyre à roue) | Une scène « tout en rouge » ne doit pas être réécrite pour chaque type d'appareil. |

### 5.3 À oublier (ou reporter très loin) ❌

- **Recoder ScanLibrary à l'identique.** Faire un éditeur à nous, mais surtout **importer** depuis les bibliothèques ouvertes (Open Fixture Library, QLC+) : des milliers d'appareils déjà décrits.
- **Tenter de lire les fichiers ScanLibrary (.ssl2)** : format propriétaire, gain faible pour une dizaine d'appareils.
- **IA locale dans la boucle temps réel** (identification de style à chaque morceau) : consommation mémoire/GPU, latence, hallucinations. À réserver à un **outil d'enrichissement hors-ligne** de la base.
- **Chaînes de sauts arbitraires entre scènes** comme mécanisme principal de show (remplacé par le séquenceur). On peut garder un simple « à la fin, enchaîner sur X » pour les cas triviaux.
- **Multi-univers / multi-Arduino dès le départ** : prévu dans le modèle de données, implémenté quand le besoin sera réel.
- **Chargement dynamique de plugins (DLL découvertes à l'exécution)** : complexité inutile pour un seul utilisateur.
- **Visualiseur 3D photoréaliste** : un plan 2D vu de dessus suffit largement (3D éventuellement bien plus tard).

---

## 6. Vocabulaire commun (glossaire proposé)

> ✅ Validé : la référence à jour est désormais [glossaire.md](glossaire.md) (document vivant). Le tableau ci-dessous est l'état initial.

| Terme | Définition | Exemple |
|---|---|---|
| **Canal** | Un octet (0-255) dans une trame DMX. Adresse 1 à 512. | Canal 17 = 200 |
| **Univers** | Une trame de 512 canaux, envoyée par **une** sortie. *(votre « galaxie »)* | Univers 1 → Arduino COM5 |
| **Sortie** (Interface) | Le matériel ou protocole qui émet un univers. | Arduino USB, Art-Net, Simulateur, Nulle |
| **Modèle d'appareil** | Description d'un type de projecteur (fabricant, nom, modes). Stocké dans la **Bibliothèque**. | « Lyre XYZ 60W » |
| **Mode** | Une configuration de canaux d'un modèle (nombre de canaux, ordre). | Mode 5 canaux / mode 9 canaux |
| **Attribut** (Fonction) | Une grandeur pilotable, typée, occupant 1..n canaux. | Intensité, Couleur RGB, Pan 16 bits, Roue de couleur, Strobe, Gobo, Fumée |
| **Plage** (Capacité) | Un intervalle de valeurs d'un canal avec une signification. | Strobe 51-200 = « clignotement lent→rapide » |
| **Appareil** (Instance) | Un exemplaire réel, patché : modèle + mode + univers + adresse + nom. | « PAR 3 – jardin », U1 @ 25 |
| **Installation** (Patch) | L'ensemble des appareils patchés et des univers. *(votre « univers »)* | « Mon kit complet » |
| **Lieu** | Données propres à une salle : disposition sur le plan, palettes de position, appareils absents. | « Salle des fêtes de X » |
| **Sélection** (groupe d'appareils) | Un ensemble ordonné d'appareils, utilisé par les scènes et effets. | « Tous les PAR », « PAR côté cour » |
| **Palette** | Une valeur d'attribut nommée et réutilisable. | Couleur « Ambre », Position « Piste centre » |
| **Scène** | Suite d'étapes (≥ 1) qui affecte des valeurs à des attributs d'appareils, avec temps et fondus. | « Arc-en-ciel lent » |
| **Étape** (d'une scène) | Un état + durée de maintien + durée de fondu. | Étape 2 : PAR en bleu, 2 s, fondu 0,5 s |
| **Effet** (générateur) | Une modulation calculée (sinus, carré, cercle, chenillard…) appliquée à une sélection avec décalage de phase. | Cercle sur Pan/Tilt des lyres, 1 tour / 2 mesures |
| **Couche** *(votre « groupe de scènes »)* | Conteneur de scènes mutuellement exclusives, avec une priorité et un master. | Couche « Couleurs », Couche « Strobe » |
| **Séquence** | Enchaînement temporel de scènes exprimé en **mesures/temps** (ou en secondes). *(votre « méga-scène »)* | « Montée 16 mesures » |
| **Show** | Un graphe (type Grafcet) d'étapes et de transitions qui pilote couches/scènes/séquences. | « Show Rock énergique » |
| **Horloge** | Source de tempo (BPM + phase). | Horloge audio, tap tempo, fixe |
| **Style** | Catégorie musicale utilisée pour choisir les shows. | Rock, Électro, Latino, Slow |
| **Énergie** | Niveau d'intensité musicale mesuré en continu. | Calme / Groove / Énergique / Explosif |
| **Directeur** | Le module qui, en mode auto, choisit et pilote les shows. | — |
| **Grand Master** | Atténuateur global de toutes les intensités. | — |

Autres noms possibles pour la « méga-scène » : *Séquence*, *Chorégraphie*, *Tableau*, *Routine*. Je propose **Séquence** (court, clair) ; « Timeline » désignant l'éditeur.

---

## 7. Une solution ou plusieurs ? Architecture proposée

### 7.1 Recommandation : **une solution, plusieurs projets**

| Option | Avantages | Inconvénients | Verdict |
|---|---|---|---|
| Plusieurs solutions séparées (une par module) | Isolation | Partage du modèle pénible (paquets NuGet internes, versions), refactorings transverses douloureux | ❌ |
| Une solution, un seul projet | Simple au début | Tout couplé, tests difficiles, modules non réutilisables | ❌ |
| **Une solution, N projets (bibliothèques) + 1 application hôte** | Frontières claires, tests par module, refactoring global facile, un seul exécutable à déployer | Demande de la discipline sur les dépendances | ✅ |
| Plusieurs processus (moteur séparé de l'UI) | Si l'UI plante, les lumières continuent | IPC, déploiement plus complexe | ⏳ Plus tard, **si** le besoin apparaît. On conçoit le moteur pour que ce soit possible. |

Vos « modules injectables les uns dans les autres » se traduisent par : **chaque module = un projet**,
qui expose des **interfaces** et s'enregistre dans l'**injection de dépendances** de l'hôte. Un module d'édition
(ex. Console) peut être intégré dans un autre écran (ex. éditeur d'appareil) car c'est un composant d'interface
réutilisable qui parle au moteur via une interface commune.

### 7.2 Découpage en projets (proposition)

```
LuXia.sln
│
├── src/
│   ├── Dmx.Core               Modèle de domaine pur : Canal, Univers, Trame, Attribut, types de base. Aucune dépendance.
│   ├── Dmx.Fixtures           Bibliothèque d'appareils : modèles, modes, attributs, plages. Import OFL / QLC+.
│   ├── Dmx.Patch              Installation, univers, adresses, sélections, lieux, détection de chevauchements.
│   ├── Dmx.Engine             Moteur de rendu temps réel : horloge, scènes, étapes, fondus, effets, couches, fusion, masters.
│   ├── Dmx.Output             Abstraction des sorties + pilotes : Arduino série, Art-Net, sACN, Nulle, Enregistreur.
│   ├── Dmx.Audio              Capture audio (boucle système / entrée), analyse : tempo, impulsions, énergie.
│   ├── Dmx.Media              Lecture en cours (API Windows).
│   ├── Dmx.Music              Identification de style : normalisation titres, rapprochement flou, API, cache.
│   ├── Dmx.Show               Séquenceur de show (Grafcet), séquences en mesures, Directeur automatique.
│   ├── Dmx.Persistence        Lecture/écriture des fichiers (bibliothèque, installation, lieux, show), migrations de format.
│   ├── Dmx.Messaging          Bus d'événements / commandes entre modules (contrats uniquement).
│   │
│   ├── Dmx.UI.Controls        Contrôles réutilisables : fader, roue de couleur, pad Pan/Tilt, timeline, grafcet…
│   ├── Dmx.UI.Modules.*       Un projet par écran/module : Console, Bibliothèque, Patch, Scènes, Couches, Live, Show, Visualiseur…
│   └── Dmx.App                Application hôte (shell, navigation, DI, configuration).
│
├── tools/
│   ├── Dmx.Tools.FixtureImport    Outil ligne de commande d'import de bibliothèques.
│   ├── Dmx.Tools.MusicEnrich      Enrichissement hors-ligne de la base artistes/styles (API, IA locale éventuelle).
│   └── Dmx.Tools.Headless         Exécution d'un show sans UI (tests d'endurance, futur mode « service »).
│
├── firmware/
│   └── arduino-dmx                Firmware Leonardo (versionné avec le reste, protocole documenté).
│
└── tests/
    ├── Dmx.Core.Tests, Dmx.Fixtures.Tests, Dmx.Engine.Tests, …   Tests unitaires
    ├── Dmx.Integration.Tests                                       Scénarios bout-en-bout avec sortie simulée
    └── assets/                                                     Fichiers audio de référence, bibliothèques de test, listes de titres « sales »
```

**Règle de dépendance** (à formaliser dans les règles de dev) : `Core` ne dépend de rien ; `Engine` ne connaît
ni l'UI, ni l'audio, ni les sorties concrètes (il reçoit des signaux et produit des trames) ; l'UI ne calcule rien, elle
envoie des commandes et observe l'état.

### 7.3 Communication entre modules

```
             ┌──────────────┐   signaux tempo/énergie   ┌───────────────┐
 Audio ────▶ │  Dmx.Audio   │ ─────────────────────────▶│               │
             └──────────────┘                            │               │
             ┌──────────────┐   morceau en cours          │  Dmx.Show     │  commandes (lancer scène,
 Windows ──▶ │  Dmx.Media   │ ──────────┐                │  (Directeur,  │  changer master, …)
             └──────────────┘           ▼                │  Grafcet)     │──────────┐
                                ┌──────────────┐ style   │               │          │
                                │  Dmx.Music   │────────▶│               │          ▼
                                └──────────────┘         └───────────────┘   ┌──────────────┐  trames   ┌────────────┐
                                                                             │  Dmx.Engine  │──────────▶│ Dmx.Output │──▶ Arduino / Art-Net / Simulateur
 Utilisateur ──▶ UI (Live, Console, éditeurs) ── commandes ─────────────────▶│  (40-44 Hz)  │           └────────────┘
                     ▲                                                       └──────┬───────┘
                     └──────────────── état (scènes actives, valeurs, trames) ──────┘
```

- **Commandes** (vers le moteur) : `LancerScène`, `ArrêterCouche`, `RéglerMaster`, `Blackout`, `ForcerCanal`…
  → une seule porte d'entrée : cela permettra plus tard une télécommande (tablette, MIDI) sans rien réécrire.
- **Événements** (depuis les modules) : `Temps`, `Mesure`, `ImpulsionBasses`, `ÉnergieChangée`, `MorceauChangé`, `StyleDétecté`, `SortieDéconnectée`…
- **État observable** : scènes actives, trame courante par univers, valeurs par appareil (pour l'UI et le simulateur).

### 7.4 Choix de la technologie d'interface (à trancher)

| Option | Pour | Contre |
|---|---|---|
| **WPF** (.NET 8/10) | Très mature, énormément de ressources, parfait pour contrôles personnalisés (faders, timeline) ; Windows seulement mais l'API « lecture en cours » l'est aussi | Techno « ancienne » (mais stable et maintenue) |
| **Avalonia** | Moderne, proche de WPF, multiplateforme, bon rendu custom | Un peu moins de ressources ; multiplateforme inutile ici |
| WinUI 3 | Look Windows 11 natif | Écosystème moins stable, déploiement plus lourd |
| Web local (Blazor / ASP.NET + navigateur) | Télécommande tablette/téléphone « gratuite » | Temps réel UI moins direct, complexité serveur |

> ✅ **Décision** : **Avalonia**, cible **Windows uniquement**. Une commande depuis un téléphone Android sera un projet séparé, branché sur la porte d'entrée « commandes ».

**Recommandation initiale** : WPF ou Avalonia pour l'application principale (préférence légère WPF pour la maturité,
Avalonia si vous voulez garder la porte ouverte vers Linux / Raspberry). Une **télécommande web** légère pourra
s'ajouter plus tard grâce à la porte d'entrée « commandes » unique.

---

## 8. Analyse module par module

Chaque module est décrit selon : **rôle**, **analyse**, **recommandations**, **UX**, **tests clés**. Le détail
exhaustif (exigences numérotées) ira dans le cahier des charges.

### 8.1 Module 1 – Bibliothèque d'appareils

**Existe-t-il des solutions ouvertes ?** Oui, et c'est une excellente nouvelle :

| Source | Format | Intérêt |
|---|---|---|
| **Open Fixture Library** (open-fixture-library.org) | JSON, schéma public, open source | Modèle très proche de ce que vous décrivez (modes, canaux, *capabilities* = vos plages). Plusieurs milliers d'appareils. Idéal comme **inspiration du modèle** et **source d'import**. |
| **QLC+** (fichiers `.qxf`) | XML ouvert | Bibliothèque très fournie, y compris beaucoup d'appareils « génériques » chinois. |
| **GDTF** (gdtf-share.com) | Standard ouvert (XML dans une archive) | Standard professionnel (grandMA3, Vectorworks) ; riche mais complexe. Import éventuel plus tard. |
| ScanLibrary (Daslight) | Propriétaire | À ne pas cibler. |

**Recommandations**

- Modèle de données **propre à l'appli** (JSON lisible), **fortement inspiré d'OFL**, avec importeurs OFL et QLC+.
- Attributs **typés** avec un catalogue fermé mais extensible : Intensité, Rouge/Vert/Bleu/Blanc/Ambre/UV, Couleur (roue), Pan, Tilt (8 ou 16 bits : canal fin), Vitesse Pan/Tilt, Strobe/Shutter, Gobo, Rotation gobo, Prisme, Focus, Macro/Programme interne, Vitesse, Fumée/Débit, Ventilateur, Reset/Contrôle, Générique.
- Chaque canal : valeur par défaut, valeur « repos » (appareil éteint proprement), valeur de « surlignage » (pour l'identification), inversion possible, 8/16 bits.
- **Plages** : min, max, libellé, type (fixe / progressif lent→rapide), et pour une roue de couleur : **couleur associée** (code couleur) → permet au simulateur de l'afficher *et* aux palettes « par intention » de trouver « rouge » sur la roue.
- Informations physiques utiles au simulateur et au directeur : angle d'ouverture, amplitude Pan (540°) / Tilt (270°), temps de réaction des moteurs, **appareil sans canal dimmer** (RGB pur → on calcule un dimmer virtuel).
- **Intensité virtuelle** : pour un PAR RGB sans canal dimmer, le moteur multiplie R, G, B par l'intensité logique. Indispensable pour que Grand Master, fondus et couches « luminosité » fonctionnent sur tous les appareils.
- **Test en direct depuis l'éditeur** : cliquer sur une plage → l'appareil (réel ou simulé) reçoit la valeur médiane ; un curseur permet de balayer la plage. C'est l'intégration du module 2 dans le module 1 que vous pressentiez.
- Versionnement du modèle d'appareil : modifier un modèle déjà utilisé dans une installation doit être signalé (ex. ajout d'un canal → décalage d'adresses).

**UX** : liste des modèles à gauche (recherche, filtre fabricant/type) ; au centre les modes en onglets ; tableau
des canaux (ordre, attribut, défaut) ; panneau des plages du canal sélectionné avec barre 0-255 colorée par plage ;
bouton « Tester » qui ouvre un mini-panneau de faders sur un appareil patché temporaire.

**Tests clés** : import d'un fichier OFL/QLC+ connu → comparaison avec un résultat attendu ; détection de plages
qui se chevauchent / trous ; aller-retour sauvegarde/chargement sans perte ; définition de **vos** appareils réels validée
avec la console.

### 8.2 Module 2 – Console DMX (faders)

**Rôle** : outil de test brut. 512 faders, sans notion d'appareil (mode « canaux ») – puis mode « appareils » (faders regroupés et nommés par appareil patché).

**Recommandations**
- Pages de 16/32/48 faders, saisie numérique directe, pas fin (molette = ±1, Maj+molette = ±10).
- Affichage 0-255 **et** % **et** libellé de la plage courante si le canal est connu (« Strobe : clignotement 42 % »).
- **Priorité absolue « manuelle »** : les faders de la console passent au-dessus de tout (y compris le mode auto) quand ils sont « pris », et se relâchent explicitement (bouton « Libérer »). Très utile en dépannage live.
- « Figer » la trame, « Tout à zéro », capture de la trame courante → **création d'une scène** (pont vers le module 4).
- Moniteur de sortie : vue grille 512 cases (valeur en couleur), ce qui est **réellement** envoyé après fusion.

**Tests clés** : fader → octet correct dans la trame émise ; latence fader→sortie mesurée ; 512 canaux à 255 en continu pendant 1 h.

### 8.3 Module 3 – Installation (Patch), univers, sorties

**Recommandations**
- Ajouter un appareil = choisir modèle + mode + univers + adresse (proposition automatique de la **première adresse libre**) + nom + quantité (« 4 × PAR RGBW à partir de 1 » → adresses 1, 8, 15, 22).
- **Détection des chevauchements** en temps réel, visualisation en « barre d'univers » 1..512 colorée par appareil.
- **Changement de mode** d'un appareil déjà patché : conserver ce qui peut l'être dans les scènes (les scènes étant en attributs, un changement de mode ne casse que les attributs qui disparaissent → liste d'avertissements).
- **Identifier** : bouton qui fait clignoter l'appareil (réel) + le surligne dans le simulateur. Indispensable à l'installation.
- **Sélections** (groupes d'appareils) ordonnées – l'ordre sert aux chenillards et décalages de phase.
- **Sorties** : association univers → sortie, avec détection automatique de l'Arduino (identifiant USB / réponse à un message d'identification), reconnexion automatique, indicateur d'état.
- **Lieu** (séparé de l'installation) : position des appareils sur le plan 2D, orientation, appareils non emportés ce soir (désactivés), palettes de position propres à la salle.
- Adresse DMX à régler **sur l'appareil** : afficher clairement « Régler l'appareil sur **d025** » (et le mode, ex. « **9CH** ») – aide précieuse sur place.

**À propos des « galaxies » / multi-Arduino** : le modèle supporte N univers, chacun lié à une sortie ;
plusieurs Arduino = plusieurs sorties série. **Attention** : deux Arduino ne sont pas synchronisés entre eux (léger décalage
possible, sans conséquence visible à 40 Hz).

**Tests clés** : patcher tout votre parc ; chevauchement détecté ; changement de mode avec avertissements corrects ; débrancher/rebrancher l'Arduino en cours de lecture.

### 8.4 Module 4 – Scènes (cues & chases unifiés)

**Le modèle unifié est le bon.** Précisions proposées :

- **Une scène ne contient que les attributs qu'elle touche** (pas les 512 canaux). C'est ce qui rend possible la superposition par couches.
- Valeurs exprimées en **attributs d'appareils** (et idéalement via **palettes** : « PAR 1-4 → palette Rouge » plutôt que R=255 G=0 B=0 ; modifier la palette met à jour toutes les scènes).
- Chaque étape : valeurs, **durée de maintien**, **durée de fondu** (entrée), courbe de fondu (linéaire, S, instantané), et possibilité de fondus **par attribut** (couleurs en fondu, position instantanée…).
- Temps exprimés au choix en **secondes** ou en **temps musicaux** (1 temps, 1/2, 2 mesures…) → la scène suit le BPM.
- Paramètres de scène : boucle (infinie / N fois / aller-retour / aléatoire), vitesse (multiplicateur), **fin de scène** (s'arrêter, rester sur la dernière étape, enchaîner sur une scène), fondu de sortie.
- Déclenchement des étapes : **au temps** (durée) ou **à l'événement** (chaque temps, chaque impulsion de basses…) = le « sound to light » de Daslight, généralisé.
- **Effets générés** intégrables dans une étape (voir §9.2) : c'est ce qui évite de programmer 32 étapes à la main pour un arc-en-ciel.
- Drapeau **« visible en Live »** (l'œil) + catégorie/couleur/icône pour organiser l'affichage.
- Scène « enregistrée depuis la console » ou « depuis l'état courant » (programmation par l'exemple).

**UX d'édition** (le « Programmeur ») : à gauche, le plan 2D / la liste des appareils (sélection au clic, sélection
de sélections) ; au centre, les outils d'attributs de la sélection (roue de couleur, pad Pan/Tilt, faders d'intensité,
boutons de palettes, plages) ; en bas, la liste des étapes de la scène (vignettes colorées, durées) ; aperçu instantané
dans le simulateur et/ou sur le matériel (« aveugle » possible : éditer sans envoyer à la sortie).

**Tests clés** : moteur avec **horloge virtuelle** (on avance le temps artificiellement) → valeurs attendues à chaque instant ; fondu 0→255 en 2 s à 40 Hz = 80 pas réguliers ; boucle, aller-retour, fin de scène ; changement de BPM en cours de scène.

### 8.5 Module 5 – Couches (groupes de scènes exclusives)

**Très bonne idée, à consolider par des règles de fusion explicites** (détaillées §10). Proposition :

- Une couche = liste de scènes, **une seule active** (exclusivité par défaut), option « non exclusive » possible.
- Chaque couche a : **priorité** (ordre d'empilement), **master** (0-100 %, n'agit que sur l'intensité ou sur toutes les valeurs selon réglage), **temps de transition** entre scènes (fondu croisé), mode de fusion (voir §10).
- Couches types livrées par défaut (modèle de départ) : *Intensité*, *Couleurs*, *Mouvements*, *Faisceau/Gobos*, *Strobe/Effets*, *Atmosphère (UV, fumée)*, *Flashs* (priorité maximale, momentanés).
- Couche **« Flash / Momentané »** : scène active tant que le bouton est maintenu (flash blanc, strobe, blackout).

**Tests clés** : lancer une scène dans une couche coupe la précédente (avec fondu croisé) ; deux couches qui touchent le même attribut → le résultat suit la règle de priorité ; master de couche à 50 % → intensité divisée par deux sans altérer les couleurs.

### 8.6 Module 6 – Timeline (« méga-scène » → Séquence)

**Analyse** : c'est un besoin réel, mais il faut en distinguer deux usages :

| Usage | Unité de temps | Réutilisable ? | Priorité |
|---|---|---|---|
| **Séquence rythmique** : « 16 mesures : 8 mesures couleurs lentes, 4 mesures montée, 4 mesures strobe » | Mesures / temps | Oui, sur tout morceau (suit l'horloge) | **Moyenne** – très utile au mode auto |
| **Timeline par morceau** : caler des effets à la seconde près sur un titre précis | Secondes, calée sur la position de lecture | Non (un morceau) | **Basse** – moments spéciaux |

**Point technique important** : la position de lecture fournie par l'API Windows est **peu précise et rafraîchie irrégulièrement**
selon le lecteur (parfois toutes les quelques secondes). Pour la timeline par morceau il faudra **interpoler** la position et
probablement proposer un **recalage manuel** (bouton « top départ ») ou un recalage sur le premier temps détecté par l'analyse audio.
Précision réaliste attendue : quelques dizaines à quelques centaines de ms – à confirmer par prototype.

**UX** : pistes = couches (une piste par couche, respect de l'exclusivité), blocs de scènes glissés depuis la bibliothèque,
grille magnétique en mesures, zoom, lecture en boucle d'une portion, affichage de la forme d'onde si un fichier audio est associé.

### 8.7 Module 7 – Show (séquenceur type Grafcet)

**Analyse de votre astuce Daslight** : une scène « aiguillage » ultra-courte qui déclenche N scènes, dont la scène d'aiguillage suivante.
C'est **exactement un Grafcet** : l'aiguillage est une *étape*, les N scènes sont ses *actions*, la durée est la *réceptivité* de la transition.
Le problème n'est pas l'idée, c'est l'**outil** : Daslight n'a pas de notion d'étape/transition, donc tout est codé en scènes.

**Proposition : un éditeur de Show graphique**

- **Étape** : active un ensemble d'actions : lancer scène X dans couche Y, lancer une séquence, régler un master, changer d'horloge, déclencher la fumée…
  (actions *continues* tant que l'étape est active, ou *impulsionnelles* à l'activation).
- **Transition** : condition de franchissement (réceptivité) :
  - temporelle (après N secondes / N mesures / N temps),
  - musicale (changement de morceau, *drop* détecté, *break* détecté, énergie > seuil, style = X),
  - manuelle (bouton Live, touche clavier, pad MIDI),
  - logique (ET / OU / NON de conditions).
- **Divergence en OU** avec **choix aléatoire pondéré** → casse la répétitivité (« après le couplet : 60 % variante A, 40 % variante B »).
- **Divergence en ET** (étapes parallèles) : utile par exemple pour faire évoluer les couleurs et les mouvements à des rythmes différents.
- **Macro-étapes / sous-shows** : réutiliser un bout de graphe (ex. « Bloc refrain »).
- **Visualisation en Live** de l'étape active (comme un Grafcet en mode supervision).

**Scènes visibles / masquées** : on garde le principe de l'œil. Le Live affiche les couches et les scènes visibles ;
un Show peut utiliser des scènes masquées (scènes « techniques »).

**Tests clés** : exécution d'un graphe avec horloge virtuelle et événements simulés → séquence d'étapes attendue ; choix aléatoire : distribution statistique conforme aux poids sur 10 000 tirages ; pas de blocage (graphe sans transition sortante détecté à l'édition).

### 8.8 Module 8 – Palettes

- **Palettes automatiques** issues de la bibliothèque : chaque plage nommée devient un bouton (« Strobe lent », « Gobo 3 », « Couleur roue : Rouge »).
- **Palettes utilisateur** :
  - *Couleur* : une couleur « intention » (ex. Ambre) → traduite pour chaque type d'appareil : mélange RGB/RGBW (calcul du blanc), ou **emplacement de roue le plus proche** pour les lyres.
  - *Position* : Pan/Tilt par appareil, **par lieu** (« Piste centre », « Boule à facettes », « Public – interdit », « Repos plafond »).
  - *Faisceau* : gobo + prisme + focus.
  - *Intensité* : niveaux nommés.
- Les scènes **référencent** les palettes → modifier une palette met à jour tout le show.
- Écran Live : grille de palettes cliquables appliquées à la sélection courante (mode « programmeur live »).

### 8.9 Module 9 – Lecture en cours (déjà validé)

- API Windows *Global System Media Transport Controls* : titre, artiste, album, parfois genre (rarement renseigné), état lecture/pause, position (imprécise), vignette.
- **Limites** : dépend du lecteur (navigateurs, Spotify, lecteur Windows : oui ; certains logiciels DJ : non ou partiellement). Titres « sales » depuis YouTube (« Artiste - Titre (Official Video) [4K] »).
- Événements à produire : `MorceauChangé`, `LectureDémarrée/Arrêtée`, `PositionEstimée`.
- Prévoir une **saisie manuelle** / sources alternatives plus tard (ex. fichier « now playing » écrit par un logiciel DJ).

### 8.10 Module 10 – Gestion des BPM : quels « types » ?

Vous demandez 2 ou 3 types. Je propose de raisonner **non pas en plusieurs BPM, mais en trois natures de signaux musicaux**,
produites par **une seule** analyse audio, et auxquels une scène peut s'abonner :

| # | Signal | Nature | Pour quoi faire | Source |
|---|---|---|---|---|
| **A** | **Horloge tempo** (BPM + phase) | Régulier, **prédictif** : continue même pendant un break | Chenillards, changements de couleur au temps, mouvements synchronisés, séquences en mesures | Audio auto **ou** Tap tempo **ou** BPM fixe |
| **B** | **Impulsions par bande** : *Basses* (kick) et *Aigus* (charleston/claps) | Irrégulier, **réactif** : ne réagit qu'aux vraies frappes | Flashs sur les kicks, strobe sur les claps, « bump » d'intensité | Audio (filtrage par bande + détection d'attaques) |
| **C** | **Énergie** (enveloppe continue + niveaux + détection *break* / *drop*) | Continu, lent | Intensité générale, choix de scènes calmes/énergiques, transitions du Show, mode auto | Audio (RMS / volume perçu, lissé) |

Paramètres par scène (ou par étape) : signal suivi (A/B/C), **diviseur/multiplicateur** pour A (×2, ×1, ½, ¼, 1 mesure, 2 mesures),
**seuil / sensibilité** pour B, **plage d'action** pour C.

**Horloges multiples ?** Une horloge **principale** (audio/tap/fixe au choix, avec bascule automatique vers le tap ou le « dernier BPM connu »
si l'audio ne détecte plus rien) + la possibilité pour une scène d'utiliser un **BPM fixe propre** (mouvements lents d'ambiance
indépendants de la musique). Cela couvre ce que Daslight ne permettait pas, sans complexité inutile.

**Points techniques** :
- Capture de ce que joue le PC via **boucle WASAPI** (librairie NAudio) ; alternative : entrée micro / ligne si la musique vient d'une table DJ.
- La détection de tempo fiable est un **vrai sujet de R&D** (erreurs d'octave : 64 vs 128 BPM, musique sans percussions). Prévoir : verrouillage de plage (ex. 80-160), **×2 / ÷2** manuels, tap tempo pour recaler, indicateur de confiance.
- **Compensation de latence** : l'analyse a toujours un retard (≈ 50-150 ms) ; l'horloge étant prédictive, on peut **anticiper** les temps. Réglage d'un décalage global (calibré visuellement : le flash doit tomber pile sur le kick).
- Bibliothèques possibles : implémentation maison (flux spectral + autocorrélation, suffisant pour la danse), ou bibliothèques existantes (aubio, BTrack… attention aux licences GPL si diffusion).

### 8.11 Module 11 – Identification du style musical

**Analyse de l'idée « énorme bibliothèque de titres »** : faisable mais peu rentable. Les limites :
volume à saisir (des milliers de titres), titres imparfaits, maintenance, nouveaux titres jamais connus.

> ✅ **Décision** : **pas d'Internet pour le logiciel en soirée**. Toute l'identification se fait hors-ligne sur une base locale.
> Les API (point 4) et l'IA locale (point 6) ne servent qu'**à la maison**, pour enrichir cette base pendant la préparation.

**Proposition en entonnoir (du moins cher au plus cher)** :

1. **Normalisation du titre** : retirer « (Official Video) », « [HD] », « - Remastered 2011 », « feat. X », « Radio Edit », mettre en minuscules, retirer accents et ponctuation, séparer artiste/titre si la source les mélange.
2. **Base locale artiste → style(s)** (et accessoirement titre → style pour les exceptions). **Un artiste a en général un style dominant** : 500 artistes couvrent l'essentiel d'une playlist de soirée, là où il faudrait 10 000 titres.
3. **Rapprochement flou** (distance de Levenshtein / Jaro-Winkler / similarité par mots) pour tolérer les fautes et variantes.
4. **API en ligne si réseau disponible** : Last.fm (tags par titre/artiste, gratuit avec clé), MusicBrainz (genres communautaires, gratuit, 1 requête/s), Deezer (genre d'album, sans clé), Discogs (styles très précis pour l'électro). Résultat **mis en cache** localement → la base se construit toute seule à l'usage et fonctionne ensuite hors-ligne. *(L'API Spotify a été fortement restreinte fin 2024 ; ne pas compter dessus.)*
5. **Correction manuelle mémorisée** : en Live, un clic « Ce morceau est : Latino » corrige et apprend (artiste ou titre).
6. **IA locale : en outil hors-ligne**, pas en temps réel. Ex. un petit modèle via Ollama pour pré-classer une liste d'artistes une fois pour toutes (avec relecture). Coût : plusieurs Go de RAM/VRAM, résultats parfois inventés → à valider. Intéressant pour **pré-remplir** la base, pas pour décider en direct.
7. **Plus tard, éventuellement** : classification par l'audio (modèles ONNX de type « genre ») – fonctionne même sans titre, mais c'est un chantier en soi.

**Idée clé à retenir** : pour la lumière, le style dit **« quelle esthétique »** (palette de couleurs, types de mouvements, présence de strobe),
et l'**énergie audio** dit **« quelle intensité, maintenant »**. Les deux combinés sont bien plus pertinents que le style seul :
un même morceau rock a des couplets calmes et des refrains explosifs.

**Taxonomie de styles** : limiter à une **douzaine de familles** utiles pour la lumière (ex. : Électro/Dance, House, Hip-hop/RnB,
Rock, Pop, Latino (salsa, reggaeton, bachata…), Années 80/Disco/Funk, Variété française, Slow/Ballade, Reggae, Rock'n'roll/Rétro, Musiques de bal/traditionnelles…)
et un **mappage** des tags bruts (des centaines) vers ces familles. À définir ensemble.

**Tests clés** : jeu de 200+ titres « sales » réels (copiés de vraies playlists YouTube/Spotify) avec style attendu → taux de bonne réponse mesuré à chaque évolution ; fonctionnement **sans réseau**.

---

## 9. Modules manquants à ajouter

### 9.1 Simulateur / Visualiseur 2D 🔴 (priorité haute)

Votre difficulté n°1 (monter le matériel) rend ce module **indispensable**, et tôt.

- Plan 2D vu de dessus (et/ou de face) du lieu, appareils placés par glisser-déposer.
- PAR / barre : disque ou segments de la couleur résultante, luminosité = intensité ; strobe animé.
- Lyres : faisceau orienté selon Pan/Tilt (projection sur le sol du plan), couleur de roue, gobo symbolisé.
- UV : halo violet ; fumée : nuage qui apparaît/disparaît (avec inertie réaliste).
- **Fonctionne comme une sortie** : il consomme exactement les trames DMX envoyées (il décode via le patch), donc il teste toute la chaîne.
- Mode « **répétition** » : jouer une playlist réelle, mode auto actif, sans aucun matériel branché.
- Complément : sortie **Art-Net** pour utiliser un visualiseur tiers si besoin, ou piloter des appareils réseau plus tard.

### 9.2 Générateur d'effets 🟠

Formes (sinus, triangle, carré, dent de scie, aléatoire, cercle, huit, balayage), appliquées à un attribut
d'une **sélection** avec : vitesse (en temps musicaux ou Hz), amplitude, centre, **décalage de phase** entre appareils
(répartition, en miroir, par groupes), forme d'onde de couleur (arc-en-ciel, alternance de 2 palettes).
C'est ce qui produit un show riche avec **peu** de scènes.

### 9.3 Écran Live (mode spectacle) 🔴

Séparation nette **Atelier (édition)** / **Live (jeu)**, comme Daslight, mais pensée « une main, dans le noir » :
- Thème sombre, gros boutons, compatible écran tactile.
- Couches en colonnes, scènes visibles en boutons (état actif, progression).
- Zone permanente : **Grand Master**, **Blackout**, **Flash**, **Strobe**, **Fumée (bouton maintenu + rafale)**, **Tap tempo**, affichage BPM/énergie/morceau/style, **AUTO ON/OFF**, **Figer**.
- Mode auto : affichage de ce que décide le Directeur et possibilité de **forcer / verrouiller** (ex. « verrouiller couleur = bleu », « interdire strobe »).

### 9.4 Contrôleurs physiques (MIDI) 🟢 (optionnel, plus tard)

Un contrôleur MIDI d'occasion (type pad + faders) rend le Live bien plus agréable qu'une souris. Grâce à la porte d'entrée « commandes », c'est un module d'entrée parmi d'autres (clavier, MIDI, télécommande web).

### 9.5 Assistant d'installation sur site 🟠

Parcours guidé à l'arrivée (voir §12.2) : choix du lieu, détection de la sortie, vérification appareil par appareil, calibration des positions des lyres, test général.

### 9.6 Sûreté et supervision 🟠

Journal (log) consultable, surveillance de la sortie (reconnexion), sauvegarde automatique, règles de sécurité (§11.5), mode dégradé.

---

## 10. Le moteur de rendu : le cœur du système

C'est le module le plus critique ; il mérite d'être spécifié **avant** les éditeurs de scènes.

### 10.1 Boucle de rendu

À chaque tick (≈ 40-44 Hz, cadence fixée par une horloge haute précision, **pas** par un timer d'interface) :

1. Lire l'horloge temps réel + l'horloge musicale (temps, phase).
2. Pour chaque couche active (par ordre de priorité) : calculer les valeurs des scènes actives (étape courante, fondus, effets, signaux audio).
3. **Fusionner** les contributions par attribut d'appareil (règles §10.2).
4. Appliquer masters : master de couche, **intensité virtuelle**, Grand Master, Blackout.
5. Appliquer les surcharges manuelles (console « prise »), puis les **limites de sécurité** (bornes Pan/Tilt, durée strobe, fumée).
6. Convertir attributs → canaux DMX (via le patch : adresses, 16 bits, inversions).
7. Publier la trame vers les sorties et l'état vers l'UI/simulateur (l'UI se rafraîchit à son propre rythme, sans bloquer le moteur).

### 10.2 Règles de fusion (proposition à valider)

| Type d'attribut | Règle par défaut | Explication |
|---|---|---|
| **Intensité** | **HTP** (le plus haut gagne) entre couches de même priorité, puis masters multiplicatifs | Comportement intuitif : deux scènes qui allument un PAR → il est allumé au max des deux. |
| **Couleur, Position, Gobo, Strobe, etc.** | **LTP par priorité** : la couche de plus haute priorité qui touche l'attribut gagne ; à priorité égale, la dernière activée gagne | Une couleur ne s'additionne pas. |
| Flash / Momentané | Priorité maximale, restitue l'état précédent au relâchement | — |
| Console « prise » | Au-dessus de tout (sauf sécurité) | Dépannage. |

Option par couche : « **Additive** » (ajoute à l'intensité, ex. bump sur kick) ou « **Multiplicative** » (module l'intensité existante, ex. vague sinus).

Ce jeu de règles couvre votre cas « une couche par paramètre » : les couleurs viennent de la couche Couleurs,
les mouvements de la couche Mouvements, sans conflit ; et s'il y a conflit, il est **déterministe et explicable**.

### 10.3 Exigences de qualité du moteur

- **Déterministe et testable** : l'horloge est injectée → tests avec temps virtuel, reproductibles à l'octet près.
- **Indépendant de l'UI** : peut tourner sans fenêtre (outil *Headless*).
- **Stable dans le temps** : pas de dérive, gigue < quelques ms, pas d'allocation mémoire excessive par tick (soirées de 6 h).
- **Tolérant aux pannes de sortie** : une sortie débranchée ne bloque pas le moteur ; reconnexion transparente.

### 10.4 Sortie Arduino : recommandations

- **Le DMX plafonne à ~44 trames/s** pour 512 canaux (250 kbit/s, 11 bits/canal). On peut monter plus haut en n'émettant que les N premiers canaux utiles (ex. 150 canaux ≈ 130 trames/s), sans grand intérêt visuel.
- **L'Arduino doit rafraîchir la ligne DMX de lui-même** en continu avec la dernière trame reçue : le PC n'envoie que des mises à jour. Une saccade côté PC ne produit alors aucun scintillement.
- Protocole PC→Arduino **documenté et robuste** : en-tête, longueur, somme de contrôle, message d'identification (pour l'auto-détection du port), message de vie (« heartbeat »).
- **Comportement en cas de perte du PC** à définir (tenir la dernière trame ? fondu vers un état de repos après N secondes ?) → sécurité de la fumée notamment.
- Option intéressante : rendre le firmware compatible avec le protocole **Enttec DMX USB Pro** (standard de fait) → votre Arduino fonctionnerait aussi avec QLC+ ou d'autres logiciels, pratique pour comparer/diagnostiquer.
- Outil de diagnostic : un **second Arduino en réception DMX** (renifleur) branché sur la ligne permettrait de vérifier la trame réellement émise sans aucun projecteur. Peu coûteux, très utile pour les tests automatisés matériels.

---

## 11. Le mode automatique : la finalité

### 11.1 Principe

Le **Directeur** est un module qui observe les signaux et **pilote des Shows** (il ne manipule pas directement les canaux) :

```
Entrées                                   Directeur                              Sorties
─────────                                 ─────────                              ───────
Style (Music)            ─┐        ┌─ Choisit un Show adapté au style ─┐
Énergie + break/drop     ─┤        │  Module l'intensité / variantes   │   Commandes au moteur
Horloge (BPM, mesures)   ─┼──────▶ │  Gère les transitions entre       │──▶ (lancer Show/scènes,
Changement de morceau    ─┤        │  morceaux                         │    masters, fumée…)
Contraintes utilisateur  ─┘        └─ Évite les répétitions ───────────┘
(verrous, interdits)
```

### 11.2 Ce que l'utilisateur prépare pour l'auto

- Des **Shows par style**, idéalement **plusieurs par style** (vous l'avez identifié : sinon rébarbatif), chacun réagissant à l'énergie.
- Pour chaque Show : style(s) visés, plage d'énergie, poids (probabilité), durée max avant rotation.
- Un **Show « par défaut »** si le style est inconnu (piloté par l'énergie seule).
- Des **transitions** de changement de morceau (ex. fondu au noir court, ou flash).

### 11.3 Règles du Directeur (exemples, paramétrables)

- Au changement de morceau : identifier le style → choisir un Show parmi les candidats (pondéré, en évitant les N derniers joués).
- Tous les 16/32 mesures : varier (changer la palette de couleurs, la forme des mouvements) pour éviter la monotonie.
- Sur *drop* : rafale strobe / flash + mouvements rapides (si autorisés) ; sur *break* : ambiance calme, mouvements lents, éventuellement **fumée** (dans les limites).
- Énergie basse durable (slow) : bascule vers un Show « slow » même si le style dit autre chose.
- Silence prolongé / lecture en pause : ambiance d'attente (lent, couleurs douces).

### 11.4 Garder la main

- Tout reste contrôlable : un bouton Live manuel **prend la priorité** sur le Directeur (sur la couche concernée) et le rend ensuite.
- **Verrous** : « couleur imposée », « pas de strobe », « pas de mouvement », « pas de fumée », « intensité max 60 % » (discours, repas, etc.).
- Journal lisible : « 22:41 – Morceau : X – Style : Rock (Last.fm, confiance 0,8) – Show : Rock B – Énergie : haute ».

### 11.5 Sécurité (exigences à inscrire au cahier des charges)

- **Strobe** : durée continue maximale et fréquence maximale paramétrables (risque photosensible) ; désactivable globalement.
- **Fumée** : durée max d'émission, temps de repos minimal entre deux émissions, arrêt forcé si perte de liaison PC.
- **Lyres** : zones interdites (public, yeux) définies par lieu → bornage Pan/Tilt appliqué par le moteur.
- **Blackout** accessible en un geste en permanence ; **reprise** propre après un plantage (sauvegarde de l'état).

---

## 12. Expérience utilisateur : parcours types

### 12.1 Chez soi : préparation (sans matériel)

1. Bibliothèque : importer ou saisir les modèles de ses appareils, **vérifier chaque plage** (avec le matériel une fois, ou au moins le manuel).
2. Installation : patcher son kit (une fois pour toutes).
3. Lieu « Générique » : disposition type sur le plan 2D.
4. Palettes : couleurs, positions génériques, faisceaux.
5. Scènes par couche + effets générés ; aperçu dans le **simulateur**.
6. Séquences en mesures, Shows par style.
7. **Répétition** : lancer une vraie playlist, mode auto, simulateur plein écran ; corriger ce qui déplaît.

### 12.2 Sur place : installation (objectif < 15 min côté logiciel)

1. Ouvrir l'appli → **Assistant d'installation** → choisir/créer le **Lieu** (dupliquer un lieu existant).
2. Cocher les appareils **emportés** ce soir.
3. Brancher l'Arduino → détecté automatiquement (voyant vert).
4. Afficher la fiche « adresses à régler » (appareil → d025 / 9CH) ; régler les appareils.
5. **Test appareil par appareil** (chenillard d'identification) → valider chacun d'un clic.
6. **Calibrer les lyres** : pour 3-4 positions de référence (piste centre, piste gauche/droite, boule, repos), viser à la main (pad Pan/Tilt), enregistrer. Définir la zone interdite (public).
7. Placer rapidement les appareils sur le plan (optionnel, pour le simulateur).
8. Passer en **Live**.

### 12.3 En soirée

- Accueil / repas : Live manuel, ambiance douce, verrou « intensité max 50 % ».
- Ouverture de bal : éventuellement une **timeline par morceau** (moment fort préparé).
- Piste de danse : **AUTO ON**. L'utilisateur surveille, force ponctuellement (flash, fumée, style), corrige un style mal détecté (mémorisé pour la prochaine fois).
- Slow : le Directeur bascule seul (énergie basse), ou bouton « Slow ».
- Fin : fondu général, blackout.

---

## 13. Ordre de réalisation : phases et jalons

> Principe : chaque phase se termine par **quelque chose d'utilisable et testé**. On n'avance pas tant que le socle ne tient pas.

| Phase | Contenu | Livrable / critère de sortie | Dépend de |
|---|---|---|---|
| **P0 – Fondations** | Solution, `Core`, abstraction Sortie, pilote Arduino (protocole robuste + firmware versionné), sortie Nulle, sortie Enregistreur (fichier), boucle d'émission cadencée | Trame de 512 octets émise à 40 Hz stable pendant 1 h ; débranchement/rebranchement USB géré | — |
| **P1 – Console DMX** *(module 2)* | 512 faders en pages, moniteur de sortie, figer/zéro | Piloter chaque canal de chaque appareil réel à la main | P0 |
| **P2 – Bibliothèque d'appareils** *(module 1)* | Modèle, éditeur, plages, import OFL (+ QLC+), test en direct | **Vos** appareils décrits et vérifiés plage par plage | P1 |
| **P3 – Installation + Simulateur** *(module 3 + nouveau)* | Patch, univers/sorties, chevauchements, identification, sélections, lieux, **simulateur 2D**, sortie Art-Net | Tout le kit patché ; la console en mode « appareils » ; le simulateur reflète la sortie | P2 |
| **P4 – Moteur + Scènes** *(module 4 + §10)* | Moteur de rendu (horloge, étapes, fondus, fusion, masters, intensité virtuelle), programmeur de scènes, palettes de base | Scènes multi-étapes jouées sur simulateur et matériel ; suite de tests moteur au vert | P3 |
| **P5 – Couches + Palettes + Live v1** *(modules 5 et 8 + nouveau)* | Couches exclusives, priorités, flashs, palettes utilisateur et par lieu, écran Live, sécurité de base, sauvegarde auto | 🎉 **JALON 1 – « Soirée manuelle »** : faire une vraie soirée en manuel avec l'appli | P4 |
| **P6 – Effets générés** | Générateur d'effets avec phases sur sélections | Arc-en-ciel, cercles, vagues sans programmation étape par étape | P5 |
| **P7 – Audio et tempo** *(module 10)* | Capture, horloge tempo (auto/tap/fixe), impulsions basses/aigus, énergie, break/drop, compensation de latence ; scènes et effets en temps musicaux | Chenillard calé sur la musique ; tests sur fichiers de référence | P5 (P6 conseillé) |
| **P8 – Show séquenceur** *(module 7)* + **Séquences en mesures** *(module 6, partie 1)* | Éditeur graphique Grafcet, exécution, supervision Live ; séquences en mesures | Un show complet déroulé sur une playlist, en simulateur | P7 |
| **P9 – Lecture en cours + Style** *(modules 9 et 11)* | Intégration GSMTC, normalisation, base artistes, rapprochement flou, API + cache, correction manuelle | Taux de bonne détection mesuré sur le jeu de test | P0 (indépendant, peut démarrer en parallèle dès P5) |
| **P10 – Directeur automatique** | Choix des shows, rotation, réactions énergie, verrous, journal, sécurité avancée | 🎉 **JALON 2 – « Soirée automatique »** : 3 h de playlist en mode auto sans intervention (répétition simulateur puis réel) | P8, P9 |
| **P11 – Timeline par morceau** *(module 6, partie 2)* | Pistes, blocs, calage sur position de lecture, recalage | Un morceau « spécial » calé correctement | P8 |
| **P12 – Extensions** | MIDI, télécommande web, multi-univers réel, IA d'enrichissement, classification audio, 3D… | Au fil des envies | — |

**Parallélisation possible** : le module *Lecture en cours + Style* (P9) est indépendant du reste ; il peut avancer en tâche de fond dès P5
(notamment la constitution du jeu de test et de la base artistes, qui demande du temps calendaire).

**Prototypes (« preuves de concept ») à faire tôt**, car ils conditionnent la faisabilité :
- **PoC-1** (pendant P0) : protocole Arduino robuste + rafraîchissement autonome + mesure de gigue.
- **PoC-2** (avant P7, peut se faire dès P1) : détection de tempo sur 20 morceaux variés, mesure de la précision et de la latence.
- **PoC-3** (avant P9) : précision de la position de lecture GSMTC selon les lecteurs utilisés (Spotify, navigateur…).
- **PoC-4** (avant P9) : qualité des tags Last.fm / MusicBrainz / Deezer sur 100 titres de vos playlists.

---

## 14. Stratégie de tests

### 14.1 Pyramide de tests

| Niveau | Quoi | Outils / méthode |
|---|---|---|
| **Unitaires** | Modèle, conversions attribut→DMX, 16 bits, plages, fusion, fondus, effets, normalisation des titres, rapprochement flou | Tests automatisés, rapides, lancés à chaque modification |
| **Moteur en temps virtuel** | Scènes, couches, shows exécutés avec une horloge simulée ; comparaison des trames produites à des **trames de référence** (« golden files ») | Tests déterministes ; toute régression est visible octet par octet |
| **Intégration** | Chaîne complète : fichier de show → moteur → sortie Enregistreur → comparaison | Sans matériel |
| **Audio** | Fichiers audio de référence (BPM connus, morceaux avec break/drop annotés, silence, musique sans percussions) → tempo, phases, événements attendus avec tolérance | Jeu de fichiers dans `tests/assets` |
| **Style** | Liste de titres « sales » annotés → taux de réussite, suivi dans le temps (non-régression) | Rapport chiffré |
| **Matériel (HIL)** | Arduino réel + (idéalement) Arduino renifleur ; débranchements, 1 h à 512 canaux, trame vérifiée | Banc de test sur le bureau, sans projecteurs |
| **Visuels / manuels** | Simulateur : vérification humaine des scènes, effets, synchro musicale | Check-lists par phase |
| **Endurance** | 6 h de mode auto en simulateur (playlist réelle) : mémoire, CPU, gigue, absence de blocage, pas de répétition excessive | Outil *Headless* + journal |
| **Terrain** | Répétition sur matériel réel monté, puis vraie soirée (jalons 1 et 2) | Retour d'expérience → nouvelles exigences |

### 14.2 Tests matériels minimaux sans monter le kit

Pour éviter de tout monter : **un seul PAR et une lyre** sur le bureau suffisent pour valider 90 % des définitions
(plages, 16 bits, roue de couleur, inversion). Le reste se valide au simulateur. Le kit complet n'est monté que pour les jalons.

### 14.3 Critères d'acceptation globaux (proposition)

- Latence action utilisateur → sortie DMX < 50 ms.
- Gigue de la cadence d'émission < 5 ms.
- Aucune fuite mémoire notable sur 6 h.
- Reprise automatique après débranchement USB < 3 s.
- Démarrage de l'appli jusqu'à « prêt en Live » < 10 s.
- Détection de tempo : erreur < 2 BPM sur 90 % des morceaux dansants du jeu de test (hors erreurs d'octave corrigeables).
- Style : > 80 % de bonnes familles sur le jeu de test (objectif initial, à ajuster).

---

## 15. Questions ouvertes à trancher avant le cahier des charges

1. **Glossaire** (§6) : validez-vous *Installation / Univers / Sortie / Attribut / Plage / Palette / Couche / Séquence / Show* ?
2. **Techno UI** : WPF ou Avalonia ? Écran tactile prévu en soirée ?
3. **Comment la musique est-elle jouée en soirée ?** Spotify / YouTube dans un navigateur / logiciel DJ (lequel ?) / fichiers locaux ? → conditionne la lecture en cours, la position de lecture et la source audio (boucle système vs entrée ligne).
4. **Internet disponible** en soirée ? (conditionne les API de style ; le cache couvre le cas hors-ligne).
5. **Liste exacte de votre matériel** (marques/modèles, modes utilisés) → pour vérifier leur présence dans OFL/QLC+.
6. **Firmware Arduino actuel** : quel protocole utilisez-vous ? Le Leonardo rafraîchit-il seul la ligne ? Ouvert à une compatibilité Enttec ?
7. **Contrôleur physique** (MIDI) envisagé ? Si oui, lequel ?
8. **Simulateur** : un plan 2D vu de dessus vous suffit-il ?
9. **Taxonomie de styles** : quelles familles de musiques passent dans vos soirées ?
10. **Comportement si le PC plante** : l'Arduino garde la dernière trame, ou fondu vers un état de repos ? Et la fumée ?
11. **Fusion** (§10.2) : les règles HTP intensité / LTP par priorité pour le reste vous conviennent-elles ?
12. **Format de sauvegarde** : fichiers JSON lisibles (versionnables avec Git, éditables à la main) – d'accord ?
13. **Une seule langue d'interface** (français) ou prévoir l'anglais ?

---

## 16. Plan du futur cahier des charges

Proposition de structure des documents suivants (un document par module, numérotés, exigences identifiées pour la traçabilité tests ↔ exigences) :

```
docs/
├── 00-analyse-et-decoupage.md          (ce document)
├── 01-glossaire-et-principes.md        Vocabulaire définitif, principes transverses (unités, temps, priorités, sécurité)
├── 02-architecture-fonctionnelle.md    Modules, flux, commandes/événements, fichiers de données
├── 10-sortie-dmx-et-firmware.md        Exigences SORT-xxx, protocole PC↔Arduino
├── 11-console.md                       CONS-xxx
├── 12-bibliotheque-appareils.md        BIB-xxx (modèle de données complet, import)
├── 13-installation-lieux.md            INST-xxx
├── 14-simulateur.md                    SIM-xxx
├── 15-moteur-de-rendu.md               MOT-xxx (fusion, fondus, masters, sécurité)
├── 16-scenes-et-effets.md              SCN-xxx, EFF-xxx
├── 17-couches-et-palettes.md           COU-xxx, PAL-xxx
├── 18-live.md                          LIVE-xxx
├── 19-audio-et-tempo.md                AUD-xxx
├── 20-show-sequenceur.md               SHOW-xxx
├── 21-lecture-en-cours-et-style.md     MUS-xxx
├── 22-directeur-automatique.md         AUTO-xxx
├── 23-timeline.md                      TL-xxx
├── 30-plan-de-tests.md                 Cas de tests reliés aux exigences
└── 40-feuille-de-route.md              Phases, jalons, suivi
```

Chaque exigence : identifiant, énoncé, justification, priorité (Indispensable / Important / Souhaitable), critère d'acceptation, phase.
