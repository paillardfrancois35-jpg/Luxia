# Glossaire

> Document vivant : vocabulaire de référence du projet, aligné sur les termes du métier de l'éclairage scénique.
> Il fait foi pour le cahier des charges, l'interface et le code. À compléter au fil du projet.
> La colonne « Anglais » donne l'équivalent courant (documentation Art-Net, Open Fixture Library, GDTF, consoles).

## 1. DMX et transport

| Terme | Anglais | Définition |
|---|---|---|
| **DMX512** | DMX512 | Protocole série (RS-485, 250 kbit/s) qui transporte jusqu'à 512 valeurs d'un octet. Rafraîchissement max ≈ 44 trames/s pour 512 canaux. |
| **Canal** | Channel / Slot | Un octet (0-255) de la trame. Numéroté de 1 à 512. |
| **Adresse** | Address / Start address | Premier canal occupé par un appareil dans son univers (réglé sur l'appareil, ex. `d025`). |
| **Trame** | Frame / Packet | Ensemble des valeurs d'un univers émises en une fois (précédées d'un *break* et d'un *start code*). |
| **Start code** | Start code | Octet 0 de la trame DMX ; vaut 0 pour les données d'éclairage. |
| **Univers** | Universe | Une ligne DMX de 512 canaux, émise par une sortie. |
| **Sortie** | Output / Interface / Node | Matériel ou protocole qui émet un univers : Arduino USB, Art-Net, sACN, simulateur, sortie nulle, enregistreur. |
| **Art-Net / sACN** | Art-Net / sACN (E1.31) | Protocoles transportant des univers DMX sur réseau Ethernet/Wi-Fi. |
| **Chaîne DMX** | DMX chain / Daisy chain | Câblage d'appareils en série (IN → OUT). |
| **Terminaison** | Terminator | Résistance de 120 Ω en bout de chaîne. |
| **Splitter / Répartiteur** | Splitter | Duplique une ligne DMX en plusieurs branches isolées. |
| **DMX sans fil** | Wireless DMX | Émetteur/récepteur radio remplaçant un câble DMX. |
| **Décodeur DMX** | DMX decoder | Récepteur DMX qui pilote directement des sorties (bandes LED, relais…). |
| **Renifleur** | DMX sniffer | Récepteur qui affiche/transmet la trame réellement présente sur la ligne (outil de test). |
| **HTP** | Highest Takes Precedence | Règle de fusion : la valeur la plus haute gagne. |
| **LTP** | Latest Takes Precedence | Règle de fusion : la dernière valeur (ou la plus prioritaire) gagne. |

## 2. Appareils

| Terme | Anglais | Définition |
|---|---|---|
| **Bibliothèque** | Fixture library | Ensemble des modèles d'appareils connus. |
| **Modèle d'appareil** | Fixture type / Fixture definition | Description d'un type de projecteur : fabricant, nom, caractéristiques, modes. |
| **Mode** | Mode / DMX mode / Personality | Une configuration de canaux d'un modèle (nombre, ordre, fonctions). Ex. 9CH / 11CH. |
| **Attribut** | Attribute / Parameter | Grandeur pilotable et typée, occupant 1 à n canaux : Intensité, Couleur RGB, Pan, Tilt, Roue de couleur, Gobo, Strobe, Fumée… |
| **Canal fin** | Fine channel (16-bit) | Second octet d'un attribut 16 bits (ex. Pan fin) pour plus de précision. |
| **Plage** | Capability / Range | Intervalle de valeurs d'un canal avec une signification. Ex. Strobe 51-200 = clignotement lent → rapide. |
| **Intensité** | Intensity / Dimmer | Luminosité globale d'un appareil. |
| **Intensité virtuelle** | Virtual dimmer | Intensité calculée par le logiciel pour un appareil sans canal dimmer (les couleurs sont multipliées). |
| **Suit l'intensité** | Dimmer-linked channel | Propriété d'un canal : sa valeur est multipliée par l'intensité de l'appareil (le « lien au dimmer » de Daslight). |
| **Roue de couleur** | Color wheel | Disque de filtres colorés d'une lyre ; une valeur = un emplacement (ou un mélange / rotation). |
| **Gobo** | Gobo | Disque découpé projetant un motif. |
| **Strobe / Obturateur** | Strobe / Shutter | Clignotement ou obturation du faisceau. |
| **Pan / Tilt** | Pan / Tilt | Rotation horizontale / verticale d'une lyre. |
| **Lyre** | Moving head | Projecteur motorisé en Pan/Tilt. |
| **PAR** | PAR can | Projecteur fixe à faisceau large (ici à LED). |
| **Barre LED** | LED bar | Rampe de LED, souvent segmentée en zones pilotables. |
| **Effet multi-têtes** | Multi-head effect | Appareil à plusieurs têtes motorisées ou tournantes, souvent très riche en canaux. |
| **Programme interne** | Built-in program / Macro | Effet pré-enregistré dans l'appareil, déclenché par un canal. |
| **Appareil** | Fixture (instance) | Un exemplaire réel patché : modèle + mode + univers + adresse + nom. |

## 3. Installation et lieu

| Terme | Anglais | Définition |
|---|---|---|
| **Installation / Patch** | Patch / Rig | Ensemble des appareils patchés, des univers et de leurs sorties. |
| **Patcher** | To patch | Affecter un appareil à un univers et une adresse. |
| **Chevauchement** | Address conflict / Overlap | Deux appareils qui occupent un même canal (erreur, sauf duplication volontaire). |
| **Lieu** | Venue | Données propres à une salle : disposition sur le plan, palettes de position, appareils absents, zones interdites. |
| **Sélection / Groupe d'appareils** | Group (fixture group) | Ensemble ordonné d'appareils utilisé par les scènes et effets. |
| **Identifier** | Highlight / Locate | Faire ressortir un appareil (flash) pour le repérer physiquement. |

## 4. Programmation

| Terme | Anglais | Définition |
|---|---|---|
| **Programmeur** | Programmer | Espace de travail où l'on règle les attributs avant de les enregistrer. |
| **Palette** | Palette / Preset | Valeur d'attribut nommée et réutilisable (couleur, position, faisceau, intensité). Les scènes y font référence. |
| **Scène** | Scene / Cue / Chase | Suite d'une ou plusieurs étapes. Une scène à une étape en boucle = une « cue » ; à plusieurs étapes = un « chase ». |
| **Étape** | Step | État d'une scène + durée de maintien + durée de fondu. |
| **Fondu** | Fade | Transition progressive d'une valeur vers une autre. |
| **Fondu croisé** | Crossfade | Transition simultanée sortie de l'ancienne scène / entrée de la nouvelle. |
| **Effet (généré)** | Effect / FX | Modulation calculée (sinus, cercle, chenillard…) appliquée à une sélection avec décalage de phase. |
| **Phase / Décalage** | Phase / Spread | Décalage temporel d'un effet entre les appareils d'une sélection. |
| **Couche** | Layer / Playback group | Conteneur de scènes exclusives, avec priorité et master. *(anciennement « groupe de scènes »)* |
| **Séquence** | Sequence | Enchaînement temporel de scènes sur des pistes, exprimé en mesures ou en secondes. *(anciennement « méga-scène »)* |
| **Timeline** | Timeline | Éditeur graphique de séquences. |
| **Show** | Show | Graphe d'étapes et de transitions (de type Grafcet) qui pilote couches, scènes et séquences. |
| **Étape de show** | Show step | Étape du graphe de show : active des actions (lancer scènes, régler masters…). |
| **Transition** | Transition | Condition de passage d'une étape de show à la suivante (temps, mesures, événement musical, bouton…). |
| **Visible en Live** | Show in live | Indique si une scène apparaît sur l'écran Live (l'« œil » de Daslight). |

## 5. Restitution et jeu

| Terme | Anglais | Définition |
|---|---|---|
| **Projet** | Show file / Workspace | Dossier de fichiers regroupant installation, lieux, palettes, scènes, couches, séquences, shows et réglages d'une configuration de soirée. |
| **Tick** | Tick / Frame | Un cycle de calcul du moteur (40 par seconde par défaut). |
| **Commande** | Command | Intention envoyée au moteur ou à un module (lancer une scène, blackout…), avec son origine. Seule façon d'agir sur la restitution. |
| **Événement** | Event | Fait publié par un module (temps musical, morceau changé, sortie déconnectée…). |
| **Origine** | Source | Qui a émis une commande : Utilisateur, MIDI, Directeur, Show, Timeline. |
| **Surcharge** | Override / Park | Valeur imposée à la main depuis la console ou le programmeur, au-dessus des couches, jusqu'à libération. |
| **Moteur de rendu** | Playback engine | Calcule à chaque instant la trame à partir des scènes actives, des couches et des masters. |
| **Fusion** | Merge | Combinaison des contributions de plusieurs couches sur un même attribut (HTP / LTP / priorités). |
| **Priorité** | Priority | Rang d'une couche dans la fusion LTP. |
| **Master** | Master / Submaster | Atténuateur appliqué à une couche. |
| **Grand Master** | Grand master | Atténuateur global de toutes les intensités. |
| **Blackout** | Blackout | Extinction immédiate de toutes les intensités. |
| **Flash** | Flash / Bump | Action momentanée (active tant que le bouton est maintenu). |
| **Figer** | Freeze | Gèle la sortie dans son état actuel. |
| **Live** | Live / Show mode | Écran de jeu en soirée (par opposition à l'Atelier d'édition). |
| **Atelier** | Edit mode / Programming | Ensemble des écrans d'édition. |
| **Aveugle** | Blind | Édition sans envoi à la sortie. |

## 6. Musique et automatique

| Terme | Anglais | Définition |
|---|---|---|
| **Horloge (tempo)** | Tempo clock / Beat clock | Source de tempo : BPM + phase. Audio, tap tempo ou fixe. |
| **BPM** | Beats per minute | Nombre de temps par minute. |
| **Temps / Mesure** | Beat / Bar | Unité rythmique ; une mesure = généralement 4 temps. |
| **Tap tempo** | Tap tempo | Saisie du tempo en tapant en rythme. |
| **Impulsion** | Onset / Hit | Attaque détectée dans une bande de fréquences (kick, caisse claire, aigus). |
| **Énergie** | Energy | Niveau d'intensité musicale mesuré en continu. |
| **Break / Drop** | Break / Drop | Passage calme / retour brutal de l'énergie. |
| **Lecture en cours** | Now playing | Informations du morceau joué (titre, artiste…) fournies par Windows. |
| **Style** | Genre / Style | Famille musicale utilisée pour choisir les shows. |
| **Directeur** | Auto director | Module qui choisit et pilote les shows en mode automatique. |
| **Verrou** | Lock / Constraint | Contrainte imposée au Directeur (pas de strobe, couleur imposée…). |

## 7. Anciens termes du projet (correspondances)

| Terme employé au départ | Terme retenu |
|---|---|
| Galaxie | **Univers** |
| Univers (configuration matériel) | **Installation** (+ **Lieu** pour ce qui dépend de la salle) |
| Channel (RGB, Pan/Tilt…) | **Attribut** |
| Preset d'un channel | **Plage** (définition de l'appareil) / **Palette** (choix de l'utilisateur) |
| Groupe de scènes | **Couche** |
| Méga-scène | **Séquence** |
| Scène de pilotage / scène d'aiguillage | **Étape de show** |
