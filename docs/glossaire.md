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
| **Renifleur** | DMX sniffer | Récepteur qui affiche/transmet la trame réellement présente sur la ligne (outil de test). En attendant le renifleur Leonardo, le DVC4 Daslight sait écouter la ligne. |
| **Journal de l'enregistrement** | Recording log | Fichier `.journal.txt` écrit à côté d'un enregistrement de trames : actions de l'utilisateur, commandes du moteur et canaux qui changent, sur la même horloge (SORT-066). |
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
| **Look** | Look / Preset (soirée) | Liste nommée d'actions (lancer ou arrêter des scènes, arrêter des couches, régler des masters) appelée d'un geste : « Temps mort », « Retour de piste ». Sert aussi d'intervention au pilote automatique (doc 60 §4.8). Ce n'est pas une bascule : re-cliquer le rejoue ; il ne touche pas au Grand Master. |
| **Zone permise** | Allowed zone / Limits | Rectangle Pan / Tilt dont une lyre ne sort jamais dans un lieu (ses limites) ; le contraire d'une zone interdite (F7). |
| **Verrou soirée** | Show lock | Verrou de l'écran Contrôle : on ne fait plus que jouer et retoucher en direct ; l'édition est bloquée (doc 60 §4.6). |
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
| **Forme** (d'un effet) | Shape / Waveform | Ce que parcourt un effet pendant un cycle : sinus, carré, cercle, huit, arc-en-ciel… |
| **Cycle** (d'un effet) | Period / Cycle | Un tour complet de la forme ; sa durée fixe la vitesse (Hz = 1 / durée). |
| **Décalage** (phase) | Phase spread / Offset | Retard d'un membre à l'autre dans le cycle : 0 = tous ensemble, 360° = un cycle réparti. |
| **Membre** (d'un effet) | Member | Appareil ou cellule qui reçoit l'effet, dans l'ordre de la cible. |
| **Thème** (de couleurs) | Theme / Color set | Palette qui réunit plusieurs couleurs (« Latino », « Froid ») : alternances, dégradés, pilote automatique. |
| **Bibliothèque d'effets** | Effect library / FX presets | Modèles d'effets du projet (`effets.json`), copiés dans une étape quand on les applique. |
| **Molette** | Dial / Encoder | Réglage rotatif de l'interface (vitesse, taille, décalage), tourné en glissant ou à la molette de la souris. |
| **Phase / Décalage** | Phase / Spread | Décalage temporel d'un effet entre les appareils d'une sélection. |
| **Couche** | Layer / Playback group | Conteneur de scènes exclusives, avec priorité et master. *(anciennement « groupe de scènes »)* |
| **Groupe (de dimmer)** | Fixture group / Dimmer group | Ensemble d'appareils rangé dans un **arbre** (un appareil dans un seul groupe ; les autres sont dans le groupe implicite « Non assigné »). Un groupe peut avoir un **dimmer** : un niveau réglé en direct, multiplié le long de l'arbre. Distinct d'une *sélection* (ordre, cellules). |
| **Dimmer de groupe** | Group dimmer | Niveau (0-100 %) d'un groupe, appliqué **après** les couches : l'intensité de ses appareils est multipliée par le niveau de chaque groupe, de la racine au sien (règle proportionnelle). Retouche en direct, jamais enregistrée. |
| **Niveau de couche** | Layer level | Fader d'une couche : multiplie ce qu'elle envoie (anciennement « master » de couche). |
| **Brouillon** | Draft | Copie d'une scène en cours d'édition dans la fenêtre d'édition : rien n'est écrit avant **Appliquer** ou **Valider** ; **Annuler** revient à l'état d'origine. |
| **Fenêtre d'édition** | Scene editor | Fenêtre non bloquante (Plan, Réglages, Effets, Propriétés et étapes) ouverte par la bande ✎ d'une scène. |
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
| **Aperçu** | Preview | Moteur secondaire qui calcule ce que donnerait l'édition en aveugle ; montré au simulateur seulement. |
| **Paramètre** | Parameter | Unité de calcul du moteur : un attribut d'un appareil patché (une définition de canal de son modèle), ou son intensité virtuelle (D26). |
| **Lecture (de scène)** | Playback | Instance en cours d'une scène dans une couche : étape, temps, poids, état (doc 15 §2). |
| **Scénario** | Script | Fichier de commandes horodatées joué sans interface (`luxia-headless scenario`, MOT-103). |
| **Reprise douce** | Soft takeover / Pickup | Un fader physique (APC mini) ne prend la main sur un niveau qu'en croisant la valeur affichée, pour éviter un saut de lumière (MIDI-004). |
| **Lissage (du gradateur)** | Dimmer smoothing | Canal de certains appareils qui adoucit allumages et extinctions ; sur le BUV463, 8e canal non documenté (≈ 15 s à 255). |

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

## 8. Audio et tempo (P7)

| Terme | Sens |
|---|---|
| **BPM** | Battements par minute : le tempo de la musique. |
| **Tap tempo** | Taper le rythme (bouton TAP, touche T) : quatre frappes régulières donnent le tempo. |
| **Horloge musicale** | Compteur de temps et de mesures du moteur, à un tempo donné ; source **Fixe**, **Tap** ou **Audio**. |
| **Quantification** | Faire attendre à une scène le temps, la mesure ou la phrase suivante pour démarrer. |
| **Phrase** | Groupe de 4 ou 8 mesures. |
| **Impulsion** | Attaque détectée dans le son : basses (kick) ou aigus (caisse claire, charleston). |
| **Énergie** | Intensité perçue de la musique (volume, basses, densité des attaques) : Calme, Groove, Énergique, Explosif. |
| **Break / Drop / Montée** | Pause brève de la musique ; retour brutal après une pause ; énergie qui grimpe avant un drop. |
| **Latence** | Décalage entre le son entendu et la lumière, réglable de ± 250 ms (calibration). |
