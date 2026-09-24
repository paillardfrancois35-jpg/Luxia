# 10 – Sortie DMX et firmware

> Cahier des charges – module **Sortie** (préfixe `SORT`). Phase principale : **P0**.
> Références : [02 §8, §13, §14](02-principes-et-architecture-fonctionnelle.md), POC `Arduino/dmx_poc/dmx_poc.ino`.

---

## 1. Rôle

Le module Sortie reçoit du moteur, à chaque tick, une trame de 512 octets par univers et l'achemine vers un ou plusieurs
**pilotes de sortie**. Il supervise les liaisons, se reconnecte seul, et publie leur état.

```
 Moteur ──trame U1──▶ ┌────────────────────┐ ──▶ Pilote Arduino (USB série) ──▶ Leonardo ──▶ ligne DMX ──▶ appareils
                      │ Routeur de sorties │ ──▶ Pilote Simulateur (en mémoire)
                      │ (univers → pilotes)│ ──▶ Pilote Art-Net (UDP, réseau local)          [S]
                      └────────────────────┘ ──▶ Pilote Enregistreur (fichier)
                                             ──▶ Pilote Nul
```

## 2. Pilotes de sortie

| Pilote | Usage | Priorité | Phase |
|---|---|---|---|
| **Arduino** | Émission réelle via le Leonardo + shield DMX | I | P0 |
| **Nul** | Aucun matériel ; permet de tout faire tourner (développement, tests) | I | P0 |
| **Enregistreur** | Écrit les trames horodatées dans un fichier (tests, analyse, rejeu) | I | P0 |
| **Simulateur** | Alimente le visualiseur (doc 14) | I | P3 |
| **Art-Net** | Émet sur le réseau local (visualiseurs tiers, futurs nœuds réseau) | S | P3 |
| **Lecteur** | Rejoue un fichier de l'Enregistreur comme source (tests du simulateur) | S | P3 |

## 3. Exigences – routage et supervision (côté PC)

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| SORT-001 | I | P0 | Chaque univers est associé à **zéro, un ou plusieurs** pilotes. Une même trame peut partir simultanément vers l'Arduino et le simulateur. | Univers 1 → Arduino + Simulateur : les deux reçoivent des trames identiques. |
| SORT-002 | I | P0 | Chaque pilote tourne indépendamment : un pilote lent ou en erreur ne retarde ni le moteur ni les autres pilotes. | Test : pilote volontairement bloqué → autres pilotes à 40 trames/s. |
| SORT-003 | I | P0 | Si un pilote n'a pas fini d'émettre la trame précédente, seule **la plus récente** est conservée (pas de file d'attente qui grossit). | Test : pilote ralenti → latence bornée, pas de croissance mémoire. |
| SORT-004 | I | P0 | Chaque pilote publie son état : `Déconnecté`, `Connexion…`, `Connecté`, `Erreur` (+ message), trames/s effectives, nombre d'erreurs. | Affichage dans l'indicateur permanent (GEN-104). |
| SORT-005 | I | P0 | Le moteur continue de fonctionner quand aucun pilote n'est connecté. | Démarrage sans Arduino : moteur actif, simulateur fonctionnel. |
| SORT-006 | I | P0 | La configuration des sorties (univers → pilotes, paramètres) est enregistrée dans les **préférences du poste**, pas dans le projet (un projet doit s'ouvrir sur un autre PC avec un autre port). | Ouvrir le projet sur un autre PC : pas d'erreur, sortie à configurer ou détectée. |
| SORT-007 | M | P0 | Écran « Sorties » : liste des pilotes, état, port, trames/s, bouton reconnecter, bouton test (chenillard canal par canal via la commande `TesterSortie`, CMD-024) avec **plage réglable** (par défaut 1-16), **liste de canaux exclus** réglable (par défaut : 180, fumée) et **valeur de test** réglable (par défaut 50 %), afin de ne déclencher ni la fumée ni les canaux Reset/contrôle (Q16). | Revue. |

## 4. Exigences – pilote Arduino (côté PC)

### 4.1 Détection et connexion

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| SORT-010 | I | P0 | **Détection automatique** : énumération des ports série ; pour chaque port candidat (priorité aux identifiants USB de l'Arduino Leonardo), envoi d'une requête d'identification ; le port qui répond correctement est retenu. | Brancher l'Arduino sur n'importe quel port USB → connecté sans réglage. |
| SORT-011 | I | P0 | Le dernier port utilisé est mémorisé et essayé en premier. | Démarrage plus rapide (< 1 s) quand le port n'a pas changé. |
| SORT-012 | I | P0 | Le port n'est **jamais ouvert à 1200 bauds** (cette vitesse déclenche le mode programmation du Leonardo). Le signal DTR est activé à l'ouverture. | Revue ; aucune remise à zéro intempestive observée. |
| SORT-013 | I | P0 | **Reconnexion automatique** : en cas de perte (débranchement, erreur d'écriture), tentative toutes les 1 s, sans action de l'utilisateur ; reprise en < 3 s après rebranchement (GEN-091). | Test débranchement / rebranchement. |
| SORT-014 | M | P0 | Un port choisi manuellement peut forcer la connexion (identification facultative, pour les firmwares anciens). | Connexion avec le firmware POC actuel. |
| SORT-015 | M | P0 | L'identification récupère la **version du firmware** et l'affiche ; une version trop ancienne produit un avertissement (pas un blocage). | Affichage de la version. |

### 4.2 Émission

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| SORT-020 | I | P0 | À chaque tick, le pilote envoie la **trame complète** de l'univers (et non les seules différences) : cela sert aussi de signal de vie au firmware. | Analyse du flux série : 40 messages/s. |
| SORT-021 | M | P0 | Le nombre de canaux émis est réglable (par défaut : 512 ; option « jusqu'au dernier canal patché ») pour accélérer la ligne DMX si besoin. | Réglage à 120 → messages de 120 canaux. |
| SORT-022 | I | P0 | Une erreur d'écriture ne lève jamais d'erreur vers le moteur : elle bascule le pilote en `Erreur` puis en reconnexion. | Test d'arrachement du câble pendant l'émission. |
| SORT-023 | M | P0 | Le pilote mesure et publie la durée d'écriture et les trames réellement émises par seconde. | Affichage. |

## 5. Protocole PC ↔ Arduino

### 5.1 Choix

Le protocole cible est le **sous-ensemble utile du protocole Enttec DMX USB Pro**, standard de fait. Intérêts :
l'interface devient compatible avec d'autres logiciels (QLC+, etc.), pratique pour diagnostiquer ou comparer ;
le format est documenté et éprouvé. Le POC actuel en est déjà très proche.

### 5.2 Format général d'un message

```
 0x7E | Label | Lg poids faible | Lg poids fort | Données (Lg octets) | 0xE7
```

### 5.3 Messages pris en charge

| Label | Sens | Nom | Données | Priorité |
|---|---|---|---|---|
| **6** | PC → Arduino | *Output Only Send DMX* | start code (0x00) + 1 à 512 valeurs | I |
| **10** | PC → Arduino | *Get Widget Serial Number* | aucune | I |
| **10** | Arduino → PC | Réponse numéro de série | 4 octets | I |
| **3** | PC → Arduino | *Get Widget Parameters* | 2 octets (taille de config utilisateur = 0) | M |
| **3** | Arduino → PC | Réponse paramètres | version firmware (2 octets), break, MAB, débit | M |
| **0x4D** (77) | PC → Arduino | *Identification étendue* (propre au projet) | aucune | I |
| **0x4D** (77) | Arduino → PC | Réponse identification | texte ASCII « DMX-LEONARDO;fw=x.y;ch=512 » | I |
| **0x11** (17) | PC → Arduino | Ancien format POC (sans start code) | 1 à 512 valeurs | S (transition) |

> Le label 77 est choisi en dehors des labels documentés par Enttec ; un logiciel tiers l'ignorera.
> L'ancien label 0x11 est accepté pendant la transition, puis pourra être retiré.

### 5.4 Règles de réception (firmware)

- Resynchronisation sur `0x7E` ; message rejeté si la longueur annoncée dépasse 513 ou si l'octet final n'est pas `0xE7`.
- Label 6 : si le start code n'est pas 0, le message est ignoré (réservé aux protocoles non éclairage).
- Un message rejeté n'altère pas la dernière trame valide.

## 6. Exigences – firmware

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| SORT-040 | I | P0 | La ligne DMX est rafraîchie **en continu par le firmware** (bibliothèque DMXSerial en mode contrôleur), indépendamment du rythme d'arrivée des messages du PC. | Oscilloscope / renifleur : trames DMX continues même si le PC n'envoie qu'1 message/s. |
| SORT-041 | I | P0 | Au démarrage, tous les canaux sont à 0. | Mise sous tension : aucun appareil ne s'allume, fumée inactive. |
| SORT-042 | I | P0 | **Chien de garde** : sans message DMX valide pendant **2 s**, tous les canaux passent à 0 (GEN-080). | Débrancher l'USB → noir en ≤ 2 s. |
| SORT-043 | I | P0 | Prise en charge des messages du §5.3 (labels 6, 10, 77 ; 3 en M ; 0x11 en S). | Tests de protocole (cf. §8). |
| SORT-044 | I | P0 | Aucune allocation dynamique de mémoire ; tampon de réception unique de 513 octets. | Revue du code. |
| SORT-045 | I | P0 | Les valeurs d'un message ne sont appliquées qu'après réception complète et valide du message (pas de trame à moitié mise à jour). | Test : message tronqué → ancienne trame conservée. |
| SORT-046 | M | P0 | LED interne : clignote à chaque message valide ; allumée fixe si chien de garde déclenché ; éteinte si jamais connecté. | Observation. |
| SORT-047 | M | P0 | La version du firmware est une constante unique, renvoyée par les messages 3 et 77. | Revue. |
| SORT-048 | M | P0 | Le firmware est **versionné dans le dépôt** (`firmware/arduino-dmx`) avec la liste des bibliothèques requises et la configuration des cavaliers du shield. | Revue. |
| SORT-049 | S | P0 | Le nombre de canaux émis sur la ligne est ajusté à la longueur du dernier message reçu (fréquence DMX plus élevée pour les petits parcs). | Mesure : 120 canaux → > 100 trames DMX/s. |

**Configuration matérielle de référence** (issue du POC) : Leonardo, DMXSerial sur `Serial1`, broche de direction RS-485 = 2,
shield en mode émission, terminaison 120 Ω en bout de chaîne.

## 7. Pilotes Simulateur, Enregistreur, Art-Net

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| SORT-060 | I | P0 | **Enregistreur** : écrit chaque trame avec son horodatage (temps écoulé depuis le début, en ms) dans un fichier compact ; en-tête avec version, univers, fréquence. | Enregistrer 10 min, relire : trames et horodatages identiques. |
| SORT-061 | I | P0 | L'Enregistreur peut être activé/désactivé à chaud, et utilisé en même temps que l'Arduino. | Test. |
| SORT-062 | I | P3 | **Simulateur** : transmet la trame en mémoire au visualiseur, sans copie bloquante. | Simulateur à jour à 40 Hz. |
| SORT-063 | S | P3 | **Lecteur** : rejoue un fichier de l'Enregistreur à vitesse réelle (ou accélérée) vers le simulateur. | Rejouer une soirée enregistrée. |
| SORT-064 | S | P3 | **Art-Net** : émet des paquets ArtDmx (UDP 6454) en diffusion ou vers une adresse donnée ; numéro d'univers Art-Net réglable. | Réception dans un visualiseur Art-Net tiers. |

## 8. Tests

| Test | Type | Contenu |
|---|---|---|
| T-SORT-01 | Unitaire | Encodage / décodage de chaque message (labels 6, 10, 3, 77, 0x11) ; cas limites (longueur 0, 513, 514, octet final erroné). |
| T-SORT-02 | Unitaire | Routeur : un univers vers N pilotes ; pilote lent ; conservation de la seule trame la plus récente. |
| T-SORT-03 | Unitaire | Enregistreur / Lecteur : aller-retour à l'octet et à la milliseconde près. |
| T-SORT-04 | Matériel | Détection automatique sur 3 ports USB différents. |
| T-SORT-05 | Matériel | Débranchement / rebranchement USB pendant l'émission (10 fois) : reconnexion < 3 s, aucun plantage. |
| T-SORT-06 | Matériel | Chien de garde : arrêt brutal de l'application → noir en ≤ 2 s. |
| T-SORT-07 | Matériel | Endurance : 512 canaux variant en continu pendant 1 h ; mesure de la gigue et des trames/s. |
| T-SORT-08 | Matériel (S) | **Banc renifleur** : un second Arduino + shield en réception DMX renvoie la trame lue au PC ; comparaison automatique trame émise / trame lue. |
| T-SORT-09 | Matériel | Compatibilité : piloter l'interface depuis un logiciel tiers compatible Enttec (ex. QLC+). |

## 9. Notes de réalisation (P0)

> Écarts et précisions constatés au développement (doc 40 §6).

| Sujet | Réalisation |
|---|---|
| SORT-010 – ports sondés | Par défaut, seuls le **dernier port utilisé** et les ports dont l'identifiant USB est une carte Arduino (VID 2341 / 2A03) sont sondés ; le chargeur de démarrage (PID 0036) est ignoré. Option « sonder aussi les ports non Arduino » (désactivée par défaut) : certains ports virtuels (Bluetooth) bloquent longtemps à l'ouverture et envoyer des octets à un appareil inconnu n'est pas anodin. |
| SORT-010 – identification | Label 77 d'abord ; à défaut label 10 (une vraie interface Enttec est alors acceptée comme « Compatible Enttec »). Délai de réponse : 400 ms. |
| SORT-004 – états | « Connexion… » n'est affiché qu'à la première tentative ; les tentatives suivantes (toutes les secondes) gardent l'état Déconnecté / Erreur, pour ne pas inonder le journal. Le compteur d'erreurs augmente à chaque **entrée** dans l'état Erreur. |
| SORT-007 – test | Réalisé par la commande `TesterSortie` (CMD-024, D19) ; le test **remplace** la restitution de l'univers testé (D21). Forme supplémentaire « rampe » (tous les canaux varient) pour l'endurance T-SORT-07. |
| SORT-007 – limite | Un chenillard canal par canal n'allume pas un appareil à gradateur maître (PAR 7 canaux…) : voir Q23. |
| SORT-049 | Le firmware émet au moins **24 canaux** par trame DMX, même si le message en contient moins. |
| Label 3 | Réponse : version mineure, version majeure, break (9 × 10,67 µs), MAB (1 × 10,67 µs), débit (40). |
| EVT-002 `TrameÉmise` | Non publié sur le bus en P0 : l'interface lit la dernière trame du moteur à son rythme (état observable, doc 02 §6.4). À publier si le simulateur (P3) en a besoin. |
| Arrêt propre | À la fermeture, une trame de blackout est envoyée à toutes les sorties avant l'arrêt des pilotes : les appareils s'éteignent tout de suite (le fondu de GEN-061 viendra en P5). |
| Outil | `dmx-headless` : `ports`, `lancer`, `endurance`, `gigue`, `relire`, `projet`. |

## 10. Points ouverts

- Utilisation du **DMX sans fil** en soirée : latence et pertes à mesurer (aucune exigence logicielle particulière, la ligne étant rafraîchie en continu).
- Rôle éventuel du **décodeur DMX512 générique** (bandes LED ?) : à préciser si utilisé ; il se patche comme un appareil générique.
