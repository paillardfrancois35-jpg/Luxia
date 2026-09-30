# 15 – Moteur de rendu

> Cahier des charges – module **Moteur** (préfixe `MOT`). Phase principale : **P4** (socle), complété en **P5** (couches, flashs, sûreté), **P6** (effets), **P7** (temps musical).
> Références : [02 §6, §8, §9, §13](02-principes-et-architecture-fonctionnelle.md), [12](12-bibliotheque-appareils.md), [16](16-scenes-et-effets.md), [17](17-couches-et-palettes.md).
> C'est le **cœur** du système : toute la restitution passe par lui. Il est spécifié avant les éditeurs.

---

## 1. Rôle

À chaque tick, calculer pour chaque appareil la valeur de chaque attribut, à partir de :
les commandes reçues, les scènes actives dans les couches, les effets, les signaux musicaux, les surcharges, les masters,
les limites de sûreté ; puis produire une trame par univers.

## 2. Notions d'exécution

| Notion | Description |
|---|---|
| **Lecture de scène** | Instance en cours d'une scène dans une couche : étape courante, temps écoulé dans l'étape, sens (aller/retour), compteur de boucles, vitesse, horloge suivie, **poids** (0→1, progression du fondu d'entrée ou de sortie), état. |
| **États d'une lecture** | `Entrée` (fondu d'entrée) → `En cours` → `Sortie` (fondu de sortie) → `Terminée`. Une lecture « maintenue sur la dernière étape » reste `En cours`. |
| **Contribution** | Pour un couple (appareil ou cellule, attribut) : une valeur cible + un poids. |
| **Valeur sous-jacente** | Résultat des couches de priorité inférieure (ou valeur par défaut) pour un attribut ; c'est depuis / vers elle que se font les fondus d'entrée / sortie. |
| **Attribut continu** | Valeur interpolable (intensité, couleurs, Pan/Tilt, vitesses, focus…). |
| **Attribut discret** | Valeur non interpolable : plages de type emplacement de roue, gobo, programme, macro, sélecteur de fonction. |

## 3. Boucle de rendu

À chaque tick (GEN-030), dans cet ordre :

1. Lire le **temps écoulé réel** depuis le tick précédent (GEN-032) et l'état de l'horloge musicale.
2. Appliquer les **commandes** reçues depuis le tick précédent, dans leur ordre d'arrivée (GEN-010, GEN-011).
3. Faire **avancer** chaque lecture de scène : fondus, changements d'étape, boucles, fins.
4. Calculer les **contributions** de chaque lecture (valeurs d'étape interpolées + effets).
5. **Fusionner** couche par couche (§5), puis appliquer les étapes 3 à 11 de la chaîne de rendu (02 §9).
6. Produire les **trames** et les transmettre au module Sortie ; publier l'**état** et les **événements**.

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MOT-001 | I | P4 | La boucle suit strictement l'ordre ci-dessus. | Tests de la suite « chaîne de rendu ». |
| MOT-002 | I | P4 | Le temps de calcul d'un tick reste inférieur à **5 ms** pour 100 appareils, 20 couches et 40 lectures actives. | Mesure sur PC portable standard. |
| MOT-003 | I | P4 | Le moteur tourne dans son **propre fil d'exécution** à priorité élevée ; aucune opération bloquante (disque, interface) n'y est effectuée. | Revue ; gigue conforme (GEN-031). |
| MOT-004 | I | P4 | **Déterminisme** : tout tirage aléatoire utilise un générateur à graine ; la graine de la session est journalisée pour permettre de rejouer une session à l'identique. | Deux exécutions avec la même graine et les mêmes entrées → trames identiques. |

## 4. Avancement des scènes

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MOT-010 | I | P4 | Chaque étape a un **fondu d'entrée** puis un **maintien** ; la durée d'une étape = fondu + maintien. Le passage à l'étape suivante a lieu à la fin du maintien. | Étape (fondu 1 s, maintien 2 s) → étape suivante à t = 3 s. |
| MOT-011 | I | P4 | Pendant le fondu d'une étape, chaque attribut **continu** est interpolé depuis la valeur qu'il avait au début du fondu vers sa cible, selon la **courbe** de l'étape (linéaire, en S, instantanée). | Fondu linéaire 0 → 100 % en 2 s à 40 Hz : 80 pas réguliers. |
| MOT-012 | I | P4 | Un attribut **discret** n'est jamais interpolé : il bascule au **début** du fondu (réglage par étape : début / milieu / fin). | Roue de couleur : changement franc. |
| MOT-013 | I | P4 | Modes de boucle : **une fois**, **N fois**, **infini**, **aller-retour**, **aléatoire** (étape suivante tirée au hasard, sans répéter l'étape courante). | Tests par mode. |
| MOT-014 | I | P4 | Fin de scène : **s'arrêter** (fondu de sortie), **rester sur la dernière étape**, **enchaîner** sur une scène donnée (dans la même couche). | Tests par mode. |
| MOT-015 | I | P4 | **Vitesse** de lecture : multiplicateur (0,1 à 10) appliqué aux durées de la scène, modifiable en direct (CMD-016). | Vitesse ×2 → étapes deux fois plus courtes. |
| MOT-016 | I | P7 | Durées **musicales** : une durée exprimée en temps/mesures est convertie avec le BPM **courant** ; un changement de BPM en cours d'étape s'applique au temps restant. | 2 temps à 120 BPM = 1 s ; BPM passé à 60 à mi-étape → 1 s restante. |
| MOT-017 | I | P7 | **Avance à l'événement** : une scène peut avancer d'étape sur chaque temps (ou tous les N temps / mesures) de l'horloge, ou sur chaque **impulsion** (basses / aigus) (doc 19) au lieu de la durée. | Avance sur impulsion basses : une étape par kick. |
| MOT-018 | M | P7 | **Quantification du lancement** : une scène peut attendre le prochain temps, la prochaine mesure ou la prochaine phrase (4 ou 8 mesures) pour démarrer. | Lancement en milieu de mesure → démarre au 1er temps suivant. |
| MOT-019 | M | P4 | Commandes `ÉtapeSuivante` / `ÉtapePrécédente` sur une lecture (pas à pas manuel). | — |
| MOT-020 | M | P7 | Une scène peut suivre l'horloge **principale** ou une **horloge fixe propre** (BPM fixe défini dans la scène). | Mouvement lent à 30 BPM fixe pendant que la musique est à 128. |

## 5. Fusion

### 5.1 Au sein d'une couche

Une couche **exclusive** ne laisse qu'une scène `Entrée`/`En cours` à la fois ; la scène remplacée passe en `Sortie`. Pendant la
transition (fondu croisé), pour chaque attribut :

| Attribut présent dans… | Comportement |
|---|---|
| l'ancienne et la nouvelle scène | interpolation directe ancienne valeur → nouvelle valeur (pas de passage par la valeur sous-jacente) |
| la nouvelle seulement | fondu depuis la valeur sous-jacente vers la nouvelle valeur |
| l'ancienne seulement | fondu depuis l'ancienne valeur vers la valeur sous-jacente |

Une couche **non exclusive** fusionne ses lectures entre elles avec les mêmes règles qu'entre couches de même priorité (§5.2).

### 5.2 Entre couches

| Attribut | Règle par défaut | Détail |
|---|---|---|
| **Intensité** | **HTP** | Résultat = maximum des contributions (valeur × poids × master de couche) de toutes les couches. |
| **Autres attributs** | **LTP par priorité** | Couches parcourues par priorité croissante : résultat = interpolation (résultat précédent → valeur de la couche) selon le poids de la contribution. À priorité égale, l'ordre d'activation départage (la plus récente l'emporte). |

**Mode d'intensité par couche** (option, doc 17) :

| Mode | Effet sur l'intensité |
|---|---|
| **HTP** (défaut) | Participe au maximum. |
| **Prioritaire** | Remplace (LTP) le résultat des couches de priorité inférieure. Ex. couche « Intensité maîtresse ». |
| **Additif** | Ajoute sa contribution au résultat (plafonné à 100 %). Ex. « bump » sur les kicks. |
| **Multiplicatif** | Multiplie le résultat. Ex. vague sinusoïdale qui module l'intensité existante. |

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MOT-030 | I | P4 | Fondu croisé dans une couche exclusive conforme au §5.1. | Tests des 3 cas. |
| MOT-031 | I | P4 | Fusion entre couches conforme au §5.2, avec les 4 modes d'intensité. | Test par mode. |
| MOT-032 | I | P4 | Un attribut non touché par aucune scène vaut la **valeur par défaut** du canal (BIB). | — |
| MOT-033 | I | P4 | Le **master de couche** s'applique à l'intensité de sa contribution ; option « master sur tous les attributs » (le poids LTP est alors multiplié par le master). | Master 50 % : intensité × 0,5 ; couleurs inchangées (sauf option). |
| MOT-034 | M | P4 | Pour chaque (appareil, attribut), le moteur sait fournir la **source** de la valeur finale (GEN-043). | Info « Couche Couleurs / Scène Bleu profond ». |

### 5.3 Intensité et couleur : convention d'allumage

Convention retenue (façon consoles professionnelles) : **l'intensité est un attribut distinct de la couleur**. Une scène qui ne
définit qu'une couleur ne suffit pas à allumer un appareil dont l'intensité vaut 0. Pour garder la simplicité de Daslight :

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MOT-040 | I | P4 | Pour chaque canal marqué **« Suit l'intensité »** (BIB-006), la valeur émise = valeur du canal × intensité logique finale de l'appareil (après masters, Grand Master et blackout). | PAR RGB 3CH : blanc (R = G = B = 100 %), intensité 80 % → 204 / 204 / 204 ; Grand Master 50 % → 102 / 102 / 102. |
| MOT-041 | I | P4 | Option du programmeur (active par défaut) : **« allumer en coloriant »** — donner une couleur à un appareil dont l'intensité est à 0 dans le programmeur ajoute l'intensité 100 % à l'enregistrement. | Scène « couleurs » créée depuis zéro → appareils allumés. |
| MOT-042 | I | P5 | Le modèle de couches par défaut (doc 17) comprend une couche **Intensité** avec une scène « Plein feu » (100 %) permettant de séparer couleurs et intensité. | — |

## 6. Couleurs

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MOT-050 | I | P4 | Conversion couleur logique → **RGB** : directe. | — |
| MOT-051 | I | P4 | Conversion couleur logique → **RGBW** : extraction de la composante blanche (réglage par modèle : « blanc = min(R,G,B) », ou « blanc désactivé », ou « blanc prioritaire »). | Blanc logique → W = 100 %, RGB = 0 (mode par défaut). |
| MOT-052 | I | P4 | Conversion couleur logique → **roue de couleur** : emplacement dont la couleur est la plus proche (distance perceptuelle), demi-couleurs exclues par défaut. | Rouge → emplacement rouge. |
| MOT-053 | M | P4 | Les **UV** et **ambre** sont pilotés comme des émetteurs indépendants ; une couleur logique peut en préciser l'usage (ex. palette « UV ») ; sinon ils restent à leur valeur par défaut. | — |
| MOT-054 | M | P4 | Interpolation des couleurs dans un espace qui évite les teintes « sales » au milieu des fondus (réglage par étape : RGB direct / teinte). | Fondu rouge → vert via jaune (mode teinte). |

## 7. Effets (exécution)

Le paramétrage des effets est défini au doc 16. Le moteur les exécute ainsi :

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MOT-060 | I | P6 | Un effet calcule, pour chaque membre de sa sélection, une valeur = f(forme, phase du membre, vitesse, taille, centre), à chaque tick. | Sinus sur intensité de 4 PAR, décalage 90° → valeurs attendues à t donné. |
| MOT-061 | I | P6 | Effet **relatif** : s'ajoute à la valeur de base (issue de l'étape ou sous-jacente) ; effet **absolu** : remplace la valeur. | Cercle relatif autour d'une palette de position. |
| MOT-062 | I | P7 | Vitesse d'effet en Hz **ou** en temps musicaux (un cycle = N temps), avec **phase calée sur l'horloge musicale** (le début de cycle tombe sur un temps). | Cercle en 1 mesure : le départ coïncide avec le 1er temps. |
| MOT-063 | M | P6 | Un effet entre et sort avec le poids de sa scène (fondu d'entrée/sortie de la taille de l'effet). | Pas de saut à l'arrivée d'un cercle. |

## 8. Commandes spéciales

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MOT-070 | I | P4 | **Blackout** (CMD-001) : intensités à 0 au tick suivant, autres attributs inchangés (GEN-041) ; la désactivation restaure instantanément. | Test. |
| MOT-071 | I | P4 | **Grand Master** (CMD-002) : multiplie toutes les intensités. | Test. |
| MOT-072 | I | P5 | **Flash** (CMD-014) : la scène est appliquée instantanément au-dessus de toutes les couches tant que la commande est maintenue ; au relâchement, retour instantané (ou fondu court réglable). | Test appui/relâche. |
| MOT-073 | I | P5 | **Figer** (CMD-003) : la trame issue des étapes 1 à 7 est gelée ; blackout et sûreté restent actifs ; les lectures continuent en arrière-plan (reprise à l'état courant lors du dégel) — option : lectures suspendues. | Test. |
| MOT-074 | I | P1 | **Surcharges** (CMD-020 à 022) conformes à la chaîne de rendu (étapes 5 et 11). | Tests CONS. |
| MOT-075 | I | P3 | **Identifier** (CMD-023) : l'appareil prend sa valeur d'identification (ou intensité 100 % blanc + clignotement 2 Hz) au-dessus de tout, hors sûreté. | Test. |

## 9. Sûreté

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MOT-080 | I | P5 | **Limiteur de strobe** : pour chaque appareil, le temps passé dans une plage étiquetée `strobe` (ou attribut Strobe actif) est compté ; au-delà de la durée maximale (GEN-083), le canal est forcé à sa valeur de repos pendant la pause réglée. Interdiction globale possible. | Strobe 30 s demandé → 10 s + pause. |
| MOT-081 | I | P5 | **Limiteur de fumée** : durée continue maximale et repos minimal (GEN-084), y compris pour les surcharges et les flashs. | Test. |
| MOT-082 | I | P5 | **Zones interdites** : si la cible Pan/Tilt d'une lyre tombe dans une zone interdite du lieu actif, elle est ramenée au point autorisé le plus proche ; option : intensité de la lyre à 0 tant que sa position estimée traverse une zone. | Test. |
| MOT-083 | I | P5 | Toute intervention d'un limiteur publie `LimiteSécuritéAtteinte` (EVT-012), une seule fois par épisode. | Journal. |

## 10. Conversion et émission

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MOT-090 | I | P4 | Conversion de chaque attribut en octets selon le patch : adresse, 8/16 bits (grossier + fin), inversion, options de montage (INST-021). | Pan 0,5 en 16 bits → 0x80, 0x00. |
| MOT-091 | I | P4 | Les appareils **absents** (lieu actif) ne sont pas émis (canaux à 0). | Test. |
| MOT-092 | I | P4 | Les appareils **jumeaux** reçoivent les mêmes valeurs. | Test. |
| MOT-093 | I | P4 | Une trame par univers est remise au module Sortie à chaque tick, même si rien n'a changé. | — |

## 11. État, événements, reprise

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MOT-100 | I | P4 | Publication de l'état observable (02 §6.4) à chaque tick, sans bloquer (GEN-013). | — |
| MOT-101 | I | P4 | Publication des événements `ScèneDémarrée`, `ScèneArrêtée`, `ÉtapeChangée`, `CommandeRefusée`. | — |
| MOT-102 | M | P5 | **Instantané de reprise** : toutes les 5 s, l'état des lectures (scènes, couches, masters, modes) est sauvegardé pour permettre la reprise après plantage (GEN-095). | Tuer / relancer → mêmes scènes actives. |
| MOT-103 | M | P4 | Mode **sans interface** : le moteur peut être piloté par un fichier de scénario (commandes horodatées) et produire un enregistrement de trames (outil de test). | Scénario → fichier de trames comparé à une référence. |

## 12. Tests

| Test | Type | Contenu |
|---|---|---|
| T-MOT-01 | Unitaire (temps virtuel) | Étapes, fondus, courbes, boucles, fins, vitesse. |
| T-MOT-02 | Unitaire | Fusion : 3 cas de fondu croisé, 4 modes d'intensité, LTP par priorité et à égalité. |
| T-MOT-03 | Unitaire | Conversions couleur RGB / RGBW / roue ; intensité virtuelle ; 16 bits ; inversions. |
| T-MOT-04 | Unitaire | Blackout, Grand Master, flash, figer, surcharges, identifier. |
| T-MOT-05 | Unitaire | Limiteurs strobe, fumée, zones interdites. |
| T-MOT-06 | Unitaire | Durées musicales, changement de BPM en cours, avance à l'événement, quantification. |
| T-MOT-07 | Non-régression | **Trames de référence** (« golden files ») : 20 scénarios de projet rejoués en temps virtuel, comparés octet par octet. |
| T-MOT-08 | Performance | Budget de 5 ms par tick, gigue < 5 ms sur 1 h. |
| T-MOT-09 | Déterminisme | Même graine + mêmes entrées → mêmes trames. |

## 13. Notes de réalisation (P4)

| Sujet | Réalisation |
|---|---|
| Architecture (D26) | Le moteur calcule sur un modèle compilé (`ShowModel`) : un **paramètre** par attribut d'appareil patché (une définition de canal), les couches, les scènes aux valeurs déjà résolues. La compilation (`Luxia.Scenes`) est faite hors du fil du moteur ; un nouveau modèle est chargé entre deux ticks et les lectures en cours continuent avec leur nouvelle version (PAL-005). |
| Transitions (§4, §5.1) | Une seule mécanique : chaque paramètre d'une lecture a une contribution (valeur, poids) qui va de son état courant vers celui de l'étape visée. Absent de l'étape = poids vers 0 (retour au sous-jacent) ; sans poids avant = valeur prise d'emblée, poids qui monte (fondu depuis le sous-jacent). Le **fondu croisé** d'une couche exclusive est une **reprise** : la nouvelle lecture prend, attribut par attribut, valeur et poids de l'ancienne (MOT-030). |
| Instant de lancement | Une scène lancée pendant un tick part de cet instant : elle n'avance pas du temps écoulé depuis le tick précédent (idem pas à pas et fondu de sortie demandés par commande). |
| Fusion (§5.2) | Lectures triées par priorité de couche puis ordre d'activation. Intensité HTP : la première contribution remplace la valeur par défaut (pas de max avec elle). Discret : la valeur l'emporte si son poids effectif ≥ 0,5 ; revient au sous-jacent dès le début du fondu de sortie. |
| Intensité virtuelle (D27) | Paramètre sans canal, **0 par défaut** comme un vrai gradateur ; les canaux qui suivent l'intensité prennent le gradateur de leur cellule, sinon le maître, sinon l'intensité virtuelle (MOT-040). |
| Blackout et surcharges brutes (GEN-042) | Pendant un blackout, la surcharge brute d'un canal d'intensité ou qui suit l'intensité est ignorée ; les autres canaux gardent leur surcharge. Sûreté : P5. |
| Temps (GEN-023) | Durées en secondes, temps ou mesures ; tempo de l'**horloge musicale** (`MusicalClock`, P7 lot 1 : 120 BPM fixes par défaut ; sources Fixe et Tap, ×2, ÷2, recalage de la mesure, latence globale ; commandes CMD-040 à 042). Un changement de tempo en cours d'étape recalcule la part musicale de la durée (MOT-016). Tolérance de 1 µs sur les comparaisons de temps (somme de pas de 25 ms inexacte en virgule flottante). |
| Solo (SCN-034) | Option de `LancerScène` : tant qu'une lecture « solo » joue, les autres sont masquées (pas arrêtées). |
| Aperçu (GEN-063) | Second moteur, cadencé avec le premier quand l'édition est en aveugle ; ses trames ne vont qu'au simulateur. |
| État observable (MOT-100) | Copié à la fin de chaque tick sous verrou ; `Snapshot` construit une copie pour l'interface. Journal des 500 dernières commandes (réception, application, origine, refus ; glissés regroupés). |
| MOT-002 | Mesuré par test : 100 appareils, 20 couches, 40 lectures, bien sous 5 ms et sans allocation en régime établi. |
| MOT-054 (M) | Non réalisé : interpolation RVB directe ; à reprendre avec les effets (P6). |
| MOT-103 | Scénario texte (`luxia-headless scenario`, doc 50 §13) ; `luxia-headless jouer` pour une scène. |

## 14. Notes de réalisation (P5)

| Sujet | Réalisation |
|---|---|
| Étape 4 – flashs (MOT-072) | Un flash est une lecture à part, fusionnée **après** toutes les couches, intensité en mode prioritaire, master de sa couche ; elle ne remplace pas la scène de sa couche : au relâchement, on retrouve exactement ce qui jouait. |
| Étape 8 – figer (MOT-073) | **Écart d'ordre assumé** avec doc 02 §9 : les valeurs sont gelées après surcharges et Grand Master mais **avant** le blackout, pour que le blackout reste actif pendant le gel (exigé par MOT-073). Lectures poursuivies (défaut) ou suspendues. |
| Étape 9 – sûreté (MOT-080 à 083, D29) | Zones interdites sur les paramètres Pan/Tilt (bord autorisé le plus proche ; une zone qui touche une butée s'étend au-delà) ; strobe et fumée sur les **octets finaux**, après surcharges brutes et test de sortie (GEN-042 : jamais contournables). Strobe compté par appareil, coupure de moins de 1 s tolérée, pause 10 s par défaut ; fumée : repos de 30 s après une émission coupée par la limite, 3 × la durée émise après une émission plus courte (plafonné à 30 s, `restFactor`, essai P5). Événement une fois par épisode ; limites actives dans l'instantané (`ActiveLimits`). Option « intensité à 0 pendant la traversée d'une zone » non réalisée. |
| Bascule (LIVE-003) | `LaunchSceneCommand.StopIfPlaying` : si la scène joue, elle est arrêtée au lieu d'être relancée ; tranché au traitement de la commande (l'écran et l'APC ne décident plus d'après un état en retard). |
| Commandes P5 | `FlashScène` (CMD-014), `Figer` (CMD-003), `Fumée` (CMD-030, maintien ou rafale, refusée sans machine), `ArrêterCouche` avec `Everything` (COU-007). Scène de repos (COU-009) relancée dès qu'une couche est vide. |
| Étape 6 bis – dimmers de groupe (ERG-037, CMD-031) | Après le Grand Master et avant le figer / blackout : l'intensité de chaque paramètre est multipliée par le **niveau effectif** de son groupe (niveau du groupe × niveau de tous ses parents, calculé une fois par tick, un parent précède toujours ses enfants dans le modèle). Même résultat que multiplier avant la fusion (la multiplication par appareil est commutative avec le HTP). Un appareil RVB sans gradateur suit par son intensité virtuelle (MOT-040). Niveaux non enregistrés, gardés à la recompilation du projet. |
| Reprise (MOT-102) | Instantané toutes les 5 s par l'hôte (`reprise.json`), pas par le moteur : le moteur n'écrit jamais sur le disque (MOT-003). |

## 15. Notes de réalisation (P6)

| Sujet | Réalisation |
|---|---|
| Effets (MOT-060) | `EngineStep.Effects` : un `EngineEffect` = forme, durée d'un cycle, rapport cyclique, sens, relatif / absolu, et un `EffectChannel` par paramètre d'un membre (retard de phase, centre, taille, table). `EffectShapes` : formes pures, sans allocation (formes de couleur = tables compilées). La lecture fait tourner chaque effet (phase en cycles, vitesse de la scène comprise) et calcule par paramètre une contribution absolue (valeur, poids) et un écart relatif. |
| Fusion (MOT-061) | Avant la fusion d'une lecture : un effet absolu remplace la valeur de l'étape selon son poids ; un effet relatif s'ajoute à la valeur de l'étape, sinon à la **valeur sous-jacente** (couches inférieures) ; deux relatifs s'additionnent (EFF-009). |
| Entrée / sortie (MOT-063) | Le poids d'un effet suit le fondu de l'étape (entrée, changement d'étape) puis le fondu de sortie de la scène. Un même effet (même identifiant) dans deux étapes successives continue sans repartir ; un effet qui arrive part du début de son cycle. |
| Aléatoire (MOT-004) | SplitMix64 sur (graine de session ⊕ graine de l'effet + rang du membre, numéro de cycle) : reproductible, sans état. |
| Fondu par la teinte (MOT-054) | `EngineStep.HueFade` : les triplets R / V / B d'une cellule (`ShowModel.ColorGroups`) suivent la roue des teintes par le plus court chemin pendant le fondu. |
| Étape montrée (CMD-017) | Lecture à part, figée sur une étape, fusionnée après toutes les couches (intensité prioritaire, master 1), absente de la liste des lectures : l'écran Contrôle s'en sert pour montrer l'étape éditée avec ses effets (EFF-006). |
| Sûreté | Inchangée et toujours après : un effet qui vise une zone interdite est ramené au bord (exemple « Piège : grand cercle de la lyre 1 »). |
| MOT-002 | Le test de charge porte des effets sur une scène sur deux : toujours 0 octet alloué par tick. |

## 16. Notes de réalisation (P7)

| Sujet | Réalisation |
|---|---|
| Horloge (GEN-023, GEN-034) | `MusicalClock` (`Luxia.Engine/Timing`) : tempo, source (Fixe, Tap, Audio), position en temps, compteur de mesures à 4 temps, temps franchis à chaque tick, décalage de latence, somme des sauts de phase. Avancée à chaque tick du temps réellement écoulé, avant les commandes. Commandes CMD-040 (Tap), 041 (source, BPM fixe), 042 (×2, ÷2, ± BPM, « 1 ici »). `RenderEngine.Bpm` est le tempo de l'horloge (l'écrire = source Fixe). |
| Durées musicales (MOT-016) | Le changement de tempo en cours d'étape recalcule la part musicale de la durée en gardant la proportion écoulée (`Playback.RescaleForTempo`) ; les durées en secondes ne bougent pas ; les retards et fondus propres à une valeur ne sont pas recalculés. |
| Avance à l'événement (MOT-017) | Option de scène `advance` (`duration`, `beat`, `bar`, `bassPulse`, `treblePulse`) et `advanceEvery` (1 à 64) : l'étape dure jusqu'au N-ième événement ; le fondu de l'étape reste en temps. Les impulsions viennent de `IAudioFeed` quand le son est entendu, sinon l'horloge (SCN-052). |
| Quantification (MOT-018) | `quantize` (`beat`, `bar`, `phrase4`, `phrase8`) : le lancement attend la position d'horloge voulue (file du moteur, publiée dans `EngineSnapshot.PendingLaunches`) ; sur un instant exact, départ immédiat ; un second appui (bascule) ou l'arrêt de la scène, de sa couche ou de tout annule l'attente. `LaunchSceneCommand.Immediate` démarre sans attendre. Les phrases se comptent depuis l'origine de l'horloge (« 1 ici » la recale). |
| Horloge propre (MOT-020) | `ownBpm` : la lecture a sa propre `MusicalClock` à tempo fixe (durées, événements et effets en temps musicaux la suivent) ; sa vitesse suit celle de la scène. |
| Effets calés (MOT-062) | Un effet dont la période est en temps ou en mesures commence son cycle à la position de l'horloge (`ClockCycle`) et suit ses recalages (`ShiftTotal`) ; vitesse en Hz et en secondes inchangées. |
| Vitesse selon l'énergie (SCN-051) | `energySpeed` : facteur de 0,6 (calme) à 1,4 (explosif) sur la vitesse de la scène quand le son est entendu. |
| Sans allocation | `TempoInfo` est une structure ; les lectures audio sont des structures ; aucune allocation par tick ajoutée (test de performance inchangé). |
