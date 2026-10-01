# 19 – Audio et tempo

> Cahier des charges – module **Audio** (préfixe `AUD`). Phase principale : **P7**. Preuve de concept **PoC-2** à réaliser tôt (dès P1).
> Références : [02 §7, §8](02-principes-et-architecture-fonctionnelle.md), [15 (MOT-016 à 020, MOT-062)](15-moteur-de-rendu.md), [16 §5](16-scenes-et-effets.md).

---

## 1. Rôle

Écouter la musique jouée par le PC et produire **trois signaux** auxquels les scènes, effets, shows et le Directeur s'abonnent :

| Signal | Nature | Événements / états |
|---|---|---|
| **A – Horloge tempo** | Régulier, prédictif : continue pendant les breaks | `Temps` (n° de temps, n° de mesure, phase), `TempoChangé` |
| **B – Impulsions par bande** | Irrégulier, réactif : seulement sur les vraies frappes | `Impulsion` (basses / aigus, force) |
| **C – Énergie** | Continu, lissé ; niveaux ; ruptures | `ÉnergieChangée` (niveau, tendance), `Break`, `Drop`, `Silence` |

Une **seule** chaîne d'analyse produit les trois.

## 2. Capture

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| AUD-001 | I | P7 | Capture du son **joué par le PC** (boucle système WASAPI) sur le périphérique de sortie par défaut. | Musique d'un navigateur analysée. |
| AUD-002 | I | P7 | Suivi du **changement de périphérique par défaut** (casque ↔ enceintes, Bluetooth) sans redémarrage. | Changer de sortie audio en cours → analyse continue en < 3 s. |
| AUD-003 | M | P7 | Choix manuel d'un périphérique de sortie à écouter, ou d'une **entrée** (micro / ligne) pour les cas où la musique ne vient pas du PC. | — |
| AUD-004 | I | P7 | L'analyse est **indépendante du volume** : normalisation adaptative du niveau (le réglage du volume du PC ne change pas les résultats). | Même morceau à 30 % et 100 % de volume → mêmes BPM et niveaux d'énergie. |
| AUD-005 | I | P7 | Détection du **silence** (lecture en pause, fin de playlist) → événement `Silence` ; reprise → événement de reprise. | — |
| AUD-006 | I | P7 | L'analyse audio fonctionne dans son propre fil d'exécution ; une erreur de capture ne perturbe ni le moteur ni l'interface (GEN-093) ; reconnexion automatique. | Débrancher un casque USB → alerte, reprise. |
| AUD-007 | M | P7 | Charge CPU de l'analyse < 5 % d'un cœur. | Mesure. |

## 3. Signal A – Horloge tempo

### 3.1 Sources

| Source | Description |
|---|---|
| **Audio** (défaut) | Tempo et phase estimés en continu à partir du son |
| **Tap** | Tempo donné par l'utilisateur (≥ 4 frappes) ; la phase est calée sur la dernière frappe |
| **Fixe** | BPM saisi ; phase libre (recalable) |

### 3.2 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| AUD-020 | I | P7 | Estimation du tempo dans une plage réglable (défaut 70-180 BPM), précision ± 1 BPM sur musique rythmée, convergence en < 8 s après le début d'un morceau. | Jeu de référence (§7) : ≥ 90 % des morceaux dansants à ± 2 BPM (hors erreurs d'octave). |
| AUD-021 | I | P7 | Suivi de la **phase** (position des temps) avec une horloge **prédictive** (boucle à verrouillage de phase) : les temps sont émis à l'instant **prévu**, pas à leur détection. | Écart moyen temps émis / temps réel < 30 ms (après compensation). |
| AUD-022 | I | P7 | **Indice de confiance** (0-1) publié avec le tempo. En dessous d'un seuil, l'horloge garde le dernier tempo fiable (GEN-034). | Passage calme sans percussions → tempo conservé. |
| AUD-023 | I | P7 | Correction d'**octave** : préférence pour la plage 90-150 BPM en cas d'ambiguïté (réglable) ; commandes ×2 et ÷2 (CMD-042). | 64 BPM détecté sur un morceau à 128 → ×2 corrige et reste mémorisé pour le morceau. |
| AUD-024 | I | P7 | Détection du **premier temps de la mesure** (temps fort) si possible ; sinon, recalage manuel (« le 1 est maintenant »). | Compteur 1-2-3-4 aligné sur la mesure dans ≥ 70 % des morceaux du jeu de test. |
| AUD-025 | I | P7 | **Tap tempo** : moyenne des intervalles des dernières frappes (4 à 8), réinitialisation après 2 s sans frappe ; passage automatique en source Tap (réglable : ou « tap corrige l'audio »). | Taper à 120 → 120 ± 1. |
| AUD-026 | I | P7 | **Changement de morceau** (EVT-040 ou rupture détectée) : réinitialisation rapide de l'estimation ; pendant la convergence, maintien du tempo précédent. | Enchaînement 90 → 128 BPM : nouveau tempo en < 8 s. |
| AUD-027 | I | P7 | **Décalage de latence** global réglable ± 250 ms (GEN-035) avec un outil de calibration : un appareil flashe sur chaque temps, l'utilisateur ajuste jusqu'à ce que le flash tombe sur le kick. | Calibration effectuée en < 1 min. |
| AUD-028 | M | P7 | Le BPM corrigé par l'utilisateur (×2, ÷2, tap) pour un morceau est **mémorisé** dans la base musicale (doc 21) et réutilisé à la prochaine lecture. | — |
| AUD-029 | M | P7 | Mesures à 4 temps par défaut ; 3 temps si le morceau / la séquence le demande (GEN-025). | — |

## 4. Signal B – Impulsions par bande

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| AUD-040 | I | P7 | Détection des attaques dans deux bandes : **basses** (grosse caisse, ~40-150 Hz) et **aigus** (caisse claire, claps, charleston, ~2-10 kHz). | Jeu de test annoté : ≥ 85 % des kicks détectés, < 10 % de fausses détections. |
| AUD-041 | I | P7 | Chaque impulsion porte une **force** (0-1) relative au contexte récent. | — |
| AUD-042 | I | P7 | Seuil de sensibilité et **temps mort** minimal entre deux impulsions réglables globalement (et par scène, SCN-050). | — |
| AUD-043 | M | P7 | Latence de détection < 60 ms (non prédictif par nature) ; compensée partiellement par le décalage global. | Mesure sur fichiers de test. |
| AUD-044 | S | P7 | Bande **médiums** (voix) en troisième canal d'impulsions. | — |

## 5. Signal C – Énergie

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| AUD-060 | I | P7 | Mesure continue de l'**énergie** perçue (volume perçu + densité rythmique), normalisée 0-1, lissée (constante de temps réglable, défaut ~2 s). | Courbe d'énergie cohérente avec l'écoute sur le jeu de test. |
| AUD-061 | I | P7 | **Niveaux** discrets avec hystérésis : **Calme**, **Groove**, **Énergique**, **Explosif**. Les seuils s'**adaptent** à l'historique récent (la soirée entière n'est pas « Explosif » juste parce que le volume est fort). | Pas d'oscillation rapide entre deux niveaux. |
| AUD-062 | I | P7 | Détection de **Break** (chute durable de l'énergie et/ou disparition des basses sur ≥ 2 mesures) et de **Drop** (retour brutal de l'énergie et des basses après un break ou une montée). | Jeu de test annoté : ≥ 80 % des drops détectés à ± 1 temps, < 1 faux drop par morceau. |
| AUD-063 | M | P7 | Détection de **montée** (« build-up » : énergie et densité croissantes sur plusieurs mesures) → événement `Montée`. | — |
| AUD-064 | M | P7 | **Tendance** (monte / stable / descend) publiée avec l'énergie. | — |

## 6. Affichage et réglages

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| AUD-080 | I | P7 | Écran **Audio** : niveau d'entrée, spectre simplifié (3 bandes), impulsions visibles, BPM + confiance + phase, énergie + niveau, derniers événements (break, drop). | Revue. |
| AUD-081 | I | P7 | Réglages : source de tempo, plage de BPM, préférence d'octave, sensibilités, temps morts, lissage de l'énergie, seuils, décalage de latence. Enregistrés dans les préférences du poste. | — |
| AUD-082 | M | P7 | **Enregistrement** de l'analyse (tempo, impulsions, énergie horodatés) avec le journal de soirée, pour améliorer les réglages a posteriori. | — |

## 7. Jeu de test audio

Constitué dans `tests/assets/audio` (fichiers personnels, non diffusés) :

| Catégorie | Contenu | Annotation |
|---|---|---|
| Tempos de référence | 30 morceaux dansants de styles variés (électro, rock, latino, pop, disco, variété, hip-hop) | BPM exact, position du 1er temps |
| Pièges | Musique sans percussions, ballade, valse (3 temps), morceaux à tempo variable, intro longue | Tempo attendu ou « non fiable » |
| Breaks / drops | 10 morceaux électro / pop avec breaks et drops | Instants des breaks, montées, drops |
| Enchaînements | Fichiers simulant des changements de morceau (fondu, coupure franche, silence) | Instants de changement |
| Volume | Même extrait à plusieurs volumes | — |

Les tests automatisés lisent ces fichiers (sans passer par la carte son) et comparent les événements produits aux annotations
avec des tolérances ; un **rapport chiffré** est produit à chaque évolution de l'algorithme.

## 8. Preuve de concept (PoC-2)

À réaliser dès la phase P1, en parallèle, pour valider la faisabilité avant P7 :
1. Capture boucle système + estimation de tempo sur les 30 morceaux de référence.
2. Mesure : précision BPM, erreurs d'octave, temps de convergence, latence.
3. Décision : algorithme maison suffisant, ou recours à une bibliothèque existante (attention aux licences).

## 9. Tests

| Test | Type | Contenu |
|---|---|---|
| T-AUD-01 | Unitaire | Chaque étage d'analyse sur signaux synthétiques (clic à 120 BPM, sinus, bruit, silence). |
| T-AUD-02 | Jeu de test | Tempo, phase, impulsions, énergie, breaks/drops vs annotations (§7) ; rapport chiffré. |
| T-AUD-03 | Unitaire | Tap tempo, ×2 / ÷2, horloge fixe, maintien du tempo en silence. |
| T-AUD-04 | Intégration | Scène avançant au temps, effet calé sur la mesure, lecture d'un fichier audio de référence. |
| T-AUD-05 | Manuel | Calibration de latence sur matériel réel ; écoute de 10 morceaux avec synchro visuelle. |
| T-AUD-06 | Endurance | 6 h de capture continue : pas de dérive mémoire, pas de décrochage. |

## 10. Notes de réalisation (P7)

| Sujet | Réalisation |
|---|---|
| Architecture (D34, D35) | Projet `Luxia.Audio` (NAudio.Wasapi pour la capture, analyse maison sans bibliothèque de détection de tempo). `AudioAnalyzer` ne dépend ni de la carte son ni d'un fil : il reçoit du son mono et publie un `AnalysisState` (tempo, confiance, phase, temps dans la mesure, niveau, trois bandes, énergie, niveau d'énergie, tendance, break, impulsions) ; `AudioListener` branche une `IAudioSource` (WASAPI), analyse dans le fil de capture et fournit au moteur un `IAudioFeed` lu à chaque tick. Le moteur ne connaît que `IAudioFeed` (défini dans `Luxia.Engine`). |
| Chaîne d'analyse | FFT de 1 024 échantillons toutes les ~5,8 ms (172 trames/s) ; normalisation adaptative du volume (AUD-004) ; flux spectral large bande, basses (40-150 Hz) et aigus (2-10 kHz) ; silence sous 0,0005 de niveau efficace. Charge mesurée inférieure à 5 % d'un cœur (AUD-007). |
| Tempo (AUD-020 à 026) | Autocorrélation de l'enveloppe des attaques sur 8 s (estimation toutes les 0,25 s, première après **3 s**) ; score d'un tempo = somme pondérée de l'autocorrélation à ses 1 à 4 multiples, pas de 0,25 BPM, avec une préférence d'octave (centre 118 BPM, largeur 0,75 octave) ; confiance = écart du meilleur score à la moyenne. Plage 70-180 par défaut. **Stabilisation (essai P7)** : les 16 dernières estimations (4 s) votent par grappes de ± 3 % pondérées par leur confiance ; la grappe la plus lourde donne le tempo, sa part des voix donne la solidité ; un tempo déjà tenu ne cède qu'à six estimations concordantes et 70 % des voix (une reprise après pause ne le fait plus chuter) ; le tempo tenu survit à la remise à zéro du silence. |
| Temps et phase (AUD-021, 024) | Grille de temps lissée : le dernier temps est cherché sur l'enveloppe basses + moitié large bande (le charleston ne doit pas l'emporter sur le kick) et la grille est recalée de 35 % à chaque estimation ; le premier temps de la mesure est déduit de l'énergie des basses sur les quatre positions (connu environ la moitié du temps sur les morceaux réels : le recalage manuel « 1 ici » reste nécessaire). |
| Horloge du moteur | `MusicalClock` suit l'écoute quand la source est Audio : tempo lissé (15 % par tick), bascule d'un coup si l'écart dépasse 6 % (nouveau morceau), phase ramenée de 12 % par tick vers la phase entendue (saut direct au premier verrouillage), premier temps corrigé après une seconde de désaccord **sauf si l'utilisateur l'a posé à la main (« 1 ici »)**. Confiance sous 0,3, silence ou écoute coupée : l'horloge garde son dernier tempo (GEN-034) ; couper l'écoute rend la source à **Fixe** en gardant le tempo. **Correction d'octave (×2, ÷2)** : facteur appliqué au tempo entendu, tenu jusqu'au prochain morceau (tempo qui saute de plus de 15 %, hors rapports d'octave) ou 1,5 s de silence ; si l'analyse change elle-même d'octave, le facteur s'ajuste. |
| Impulsions (AUD-040 à 043) | Détecteur sur l'**amplitude de la bande** (basses 40-150 Hz, aigus 4-10 kHz, après normalisation du volume) : un pic est une impulsion s'il dépasse de **2,4 fois** (de 4 à 1,3 selon la sensibilité) le **creux** des 0,4 s précédentes (c'est une attaque, pas une note tenue) **et** s'il atteint **27 %** (de 55 % à 8 %) du pic de la bande sur 30 s (la bande est présente : aucun déclenchement sans basses), avec un temps mort (basses 0,25 s, aigus 0,10 s) ; force relative au pic de 5 s. Sensibilité par défaut 60 %. Mesure (`luxia-headless audio-diag`) : passages sans basses 0,0 à 0,3 impulsion par seconde ; part sur les temps jusqu'à 85 à 95 % pour les kicks réguliers. |
| Énergie (AUD-060 à 064) | Énergie = 0,55 volume (sur ~1 s) + 0,25 basses + 0,20 densité des attaques, chacun remplacé par son **rang** dans l'histoire du morceau (histogrammes à décroissance, demi-vie 90 s ; neutre pendant les 6 premières secondes) ; un morceau dont le volume varie peu est ramené vers le milieu ; lissée (2 s). Niveaux : seuils 0,30 / 0,52 / 0,72, hystérésis 0,04, durée minimale 2 s (montée) et 3 s (descente) ; tendance sur 4 s (± 0,12). **Break** : volume rapide (0,5 s) sous 30 % de la référence de 6 s (figée pendant la pause) pendant une demi-mesure à une mesure, ou basses < 8 % et volume < 60 % pendant 2,5 fois plus longtemps. **Drop** : remontée d'au moins 8 dB au-dessus du creux et quart du niveau d'avant avec retour des basses, ou retour des basses ; au moins 0,75 s de pause. **Montée** : énergie + 0,25 et densité + 20 % sur 6 s, après 20 s d'écoute. |
| Événements | `AudioEvent` (Silence, Resumed, Break, Drop, BuildUp, EnergyChanged) levés par l'analyse ; republiés sur le bus en `MusicEvent` (EVT-022, EVT-023). Temps, BPM et source sont dans `EngineSnapshot.Tempo`, pas sur le bus. |
| Périphériques (AUD-002, 003) | `WasapiSource` : sortie par défaut (suit les changements par `IMMNotificationClient` et se rebranche), sortie choisie (boucle) ou entrée (micro, ligne), cette dernière **capturée sur événement par blocs de 10 ms** (60 ms en mode par défaut) avec repli. Écran Audio : liste (chargée au premier affichage, hors fil d'interface) et choix, mémorisés dans `preferences.json` (`audio`). `AudioListener` ne libère **jamais** une source en tenant son verrou (risque d'interblocage), garde un **avis** de 12 s pour les changements de périphérique, reprises et erreurs (écran Audio et Journal), et espace ses nouveaux essais (2, 5, 10 s). |
| Latence (GEN-035, AUD-027) | `SetTempoLatencyCommand` (**± 500 ms**) décale la position vue par les scènes ; réglée dans l'écran Audio avec la scène « Calibration de latence » (PAR en flash sur chaque deuxième temps) et **mémorisée par périphérique** (`audio.latencyByDevice` ; le son du PC garde `audio.latencySeconds`) : un micro Bluetooth ajoute 300 à 500 ms, la boucle de sortie 0 ms (essai P7). |
| Jeu de test (§7) | 41 morceaux dans `tests/assets/audio` (non versionnés) ; `luxia-headless audio <dossier> [--rapport f.md] [--evenements] [--trace]` produit le rapport chiffré ; `audio-ecoute <fichier>` joue un fichier et l'écoute par la boucle WASAPI. BPM de référence dans `annotations.csv` (certains de mémoire, à confirmer). Rapport : [essais/P7-audio-rapport.md](essais/P7-audio-rapport.md). |
| Limites connues | Ballades à 6/8 (tempo trouvé à 1,5× ou 2× du ressenti) ; mesures irrégulières, rubato et tempo variable : confiance faible (l'horloge garde son tempo) ; premier temps de la mesure fiable à environ 50 % ; AUD-028 (mémoire par morceau) en P9 ; AUD-044 (médiums) et AUD-082 (enregistrement) non faits ; SCN-050 / 051 limités à la scène (pas de bump ni de modulation de taille par effet). |
