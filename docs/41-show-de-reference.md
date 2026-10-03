# 41 – Show de référence (fichier de travail)

> Document de **contenu** : décrit le projet de référence construit **au fil des phases** avec le parc réel.
> Il remplace la notion de « projet de démonstration » du doc 40 §7 : c'est **un vrai show complet**, utilisable en soirée à la fin du projet.
> Références : [40 §7](40-feuille-de-route.md), [12 annexe A](12-bibliotheque-appareils.md), [17](17-couches-et-palettes.md), [20](20-show-et-sequences.md), [21 §3.4](21-lecture-en-cours-et-style.md), [22](22-directeur-automatique.md).

---

## 1. Principe

| Élément | Emplacement | Rôle |
|---|---|---|
| **Show de référence** | `samples/Show de référence/` | **Un seul projet**, enrichi à chaque phase : installation réelle, lieux, palettes, scènes, effets, couches, séquences, shows par style, réglages du Directeur. C'est à la fois la démonstration de chaque livraison **et** le show de soirée final. |
| **Samples par mécanique** | `samples/Mécaniques/<nom>/` | Petits projets **isolés**, produits **à la demande**, pour revoir un mécanisme précis sans le bruit du show complet (ex. « fusion des couches », « lien au dimmer »). |
| **Journal du show** | `samples/Show de référence/JOURNAL.md` | Ce qui a été ajouté à chaque phase, ce qui a été validé, les retours de l'utilisateur. |

### Règles

| ID | Règle |
|---|---|
| REF-1 | Chaque phase **ajoute** au show de référence (jamais de projet parallèle jetable) ; le contenu ajouté est listé dans le journal du show. |
| REF-2 | Tout est construit sur **le parc réel** (annexe A du doc 12) et le **plan d'adresses** du §2 ; un appareil pas encore défini est remplacé par un générique équivalent, puis substitué dès que sa définition existe. |
| REF-3 | Les objets ajoutés par une phase sont d'abord dans une catégorie « **Phase Pn** » ; une fois validés par l'utilisateur, ils passent dans leur catégorie définitive (Couleurs, Mouvements…). |
| REF-4 | Le contenu validé est **figé comme référence** : ses trames sont rejouées à chaque livraison (non-régression, doc 30 §8). |
| REF-5 | L'utilisateur peut modifier librement le show ; l'IA de développement **ne réécrit jamais** un objet modifié par l'utilisateur sans accord (elle propose une variante). |
| REF-6 | Un sample par mécanique est créé sur demande (« fais-moi un sample sur X ») ; il réutilise la bibliothèque et l'installation du show de référence, mais ne contient que le strict nécessaire au mécanisme. |

---

## 2. Installation (plan d'adresses proposé)

Adresses « rondes » pour faciliter le réglage sur les appareils, avec des **réserves** pour les modes plus riches à venir.

| Appareils | Qté | Modèle / mode | Adresses | Canaux | Réserve jusqu'à |
|---|---|---|---|---|---|
| PAR 1 à 4 | 4 | Betopper LPC008S – **7CH** (`A001`) | 1, 8, 15, 22 | 7 | 30 |
| Gros PAR 1 et 2 | 2 | Générique **WT05** (PAR 160 W, sans marque) – **7CH**, mêmes commandes que le LPC008S *(essai P5, 2026-09-27 ; ce n'étaient pas des LPC120)* | 31, 41 | 7 | 50 |
| Barres 1 et 2 | 2 | BeamZ LCB803 – **24CH** (4 sections, menu `ChNd 24Ch`) *(proposé : 48CH ne tient pas dans la réserve)* | 51, 81 | 24 | 110 |
| Lyres 1 et 2 | 2 | Tomshine – **11CH** (Pan/Tilt 16 bits) | 111, 126 | 11 | 140 |
| Effet multi-têtes | 1 | WZYBUTA 150 W – **64CH** (mode 2 : 12 cellules RVBW pilotables une à une, EFF-008) *(P6, 2026-09-28, Q25 : placé **après la fumée** pour ne réadresser aucun autre appareil ; 141-160 restent libres)* | **181** | 64 | 244 |
| UV 1 et 2 | 2 | BeamZ BUV463 – **8CH** : les 7 canaux de la notice + un 8e **« lissage du gradateur »** non documenté (essai P5, 2026-09-27 ; à 255, ≈ 15 s de lissage) | 161, **169** | 8 | 177 |
| Fumée | 1 | Générique « Machine à fumée » – 1CH | 180 | 1 | 180 |

> À valider par l'utilisateur. Modes et adresses définitifs dès que les tableaux DMX manquants sont connus.

## 3. Lieux

| Lieu | Usage | Disposition proposée |
|---|---|---|
| **Générique** | Référence de repli pour les palettes | Deux **totems** (gauche / droite) : sur chacun 2 PAR, 1 gros PAR, 1 lyre en haut ; barres au sol devant les totems ; effet multi-têtes au centre ; UV en façade ; fumée derrière, au centre |
| **Salon** | Tests à la maison (parc partiel sur le bureau) | 1 PAR + 1 lyre présents, le reste marqué absent |
| **Salle type** | Répétition au simulateur | Salle de 12 × 8 m, piste de 6 × 5 m, public sur les côtés, boule à facettes au centre |

> Disposition à corriger selon l'installation réelle de l'utilisateur.

## 4. Sélections

| Sélection | Contenu (ordre) |
|---|---|
| Tous | Tout le parc |
| PAR | PAR 1 → 4 (gauche → droite) |
| PAR gauche / PAR droite | PAR 1-2 / PAR 3-4 |
| Gros PAR | Gros PAR 1 → 2 |
| Couleurs – tout | PAR + gros PAR + barres |
| Barres (segments) | Toutes les cellules des 2 barres, de l'extérieur vers le centre |
| Lyres | Lyre 1 → 2 |
| Effet | Effet multi-têtes (4 têtes) |
| UV | UV 1 → 2 |
| Côté gauche / Côté droit | Tous les appareils d'un totem |

## 5. Palettes

| Type | Palettes |
|---|---|
| **Couleurs** | Blanc, Blanc chaud, Rouge, Orange, Ambre, Jaune, Vert, Cyan, Bleu, Lavande, Magenta, Rose, UV |
| **Thèmes par style** (PAL-010) | Électro : bleu / cyan / magenta · House : ambre / rose / violet · Hip-hop : violet / bleu / rouge · Pop : multicolore vif · Rock : blanc / rouge / ambre · 80's : magenta / cyan / jaune · Disco-Funk : or / orange / rose · Latino : jaune / orange / rouge · Variété : chaud doux · Reggae : vert / jaune / rouge · Rétro : rouge / blanc / bleu · Bal : ambre / blanc chaud / rose · Festif : multicolore · Slow : bleu profond / lavande / rose pâle |
| **Positions** (par lieu) | Piste centre, Piste gauche, Piste droite, Boule, Plafond, Repos, Croisé (lyres qui se croisent), Écartées |
| **Faisceau** | Gobos de la lyre (à nommer selon la roue), Prisme (si présent) |
| **Intensité** | Plein, 70 %, 50 %, Veilleuse (15 %) |

## 6. Couches

Modèle par défaut du doc 17 §1.3 : **Intensité, Couleurs, Mouvements, Faisceau, Effets, Ambiance, Flashs**.

## 7. Catalogue de scènes (cible)

| Couche | Scènes | Phase |
|---|---|---|
| Intensité | Plein, 70 %, 50 %, Veilleuse, Vague lente, Vague rapide, Respiration, Pulse au temps, Bump sur kick, Alternance gauche/droite | P4 → P7 |
| Couleurs | Une scène par couleur principale (8), Thème du style (piloté par le Directeur), Arc-en-ciel lent, Arc-en-ciel rapide, Chenillard 4 couleurs, Alternance 2 couleurs au temps, Dégradé barres, Miroir centre | P4 → P7 |
| Mouvements | Piste centre, Boule, Balayage lent, Balayage rapide, Cercle lent, Cercle au temps, Huit, Croisés, Aléatoire lent, Têtes de l'effet décalées | P4 → P7 |
| Faisceau | Gobo 1 à N, Rotation lente, Prisme | P4 |
| Effets | Strobe lent, Strobe rapide, Strobe au temps, Programme interne effet multi-têtes, Programmes internes PAR | P4 → P7 |
| Ambiance | UV plein, UV pulsé, Fumée courte, Fumée longue | P4 / P5 |
| Flashs | Flash blanc, Flash couleur du thème, Blackout partiel (tout sauf UV), Strobe flash | P5 |

## 8. Séquences (P8)

| Séquence | Longueur | Contenu |
|---|---|---|
| Groove 8 | 8 mesures | Couleurs au temps, balayage lent, vague d'intensité |
| Montée 16 | 16 mesures | Accélération progressive des effets, intensité croissante, strobe final |
| Break calme | 8 mesures | Couleurs lentes, lyres au plafond, UV |
| Explosion drop | 4 mesures | Flash, strobe au temps, cercles rapides, fumée courte |
| Slow 16 | 16 mesures | Bleu profond / lavande, balayage très lent |

## 9. Shows (P8 / P10)

| Show | Rôle | Styles | Énergie |
|---|---|---|---|
| Ouverture | Ouverture | Tous | Toutes |
| Transition noir court / Transition flash | Transition | Tous | Toutes |
| Attente | Attente (silence, pub) | — | — |
| Slow | Slow | Slow, et énergie basse | Calme |
| Générique Calme / Groove / Énergique | Principal (show par défaut, thème du style) | Tous, Inconnu | Chacun sa plage |
| Électro A / B | Principal | Électro, House | Groove → Explosif |
| Rock A / B | Principal | Rock, Rétro | Groove → Explosif |
| Latino A / B | Principal | Latino, Reggae | Groove → Énergique |
| Années 80 / Disco A / B | Principal | 80's, Disco-Funk, Pop | Groove → Énergique |
| Bal / Festif | Principal | Bal, Festif, Variété | Toutes |

> Liste à ajuster selon les styles réellement joués (journal de soirée).

## 10. Directeur (P10)

Réglages de départ : anti-répétition N = 3 ; délai style 3 s ; transition « noir court » ; rotation toutes les 32 mesures ;
budgets strobe 30 s / 5 min, fumée 3 rafales / 10 min ; retour au Directeur au changement de morceau.

## 11. Construction par phase

| Phase | Ajouts au show de référence | Validation par l'utilisateur |
|---|---|---|
| P0 | Squelette du projet (fichier projet versionné) ; enregistrement d'un chenillard de test canaux 1-180, **canal 180 (fumée) exclu**, valeur de test 50 % (pour ne pas déclencher la fumée ni les canaux Reset des lyres et de l'effet WZYBUTA) ; `JOURNAL.md`. La configuration de sortie n'est **pas** dans le show : elle est dans les préférences du poste (SORT-006, Q15). | Chaque appareil réagit |
| P1 | Instantanés de console par appareil | Canaux conformes au plan d'adresses |
| P2 | Définitions de tous les appareils (bibliothèque d'exemple `samples/Bibliothèque/` ; copie dans le projet à partir de P3, GEN-053) | Plages vérifiées en direct |
| P3 | Installation (§2), sélections (§4), lieux (§3), fiche d'installation | Adresses réglées sur les appareils, identification OK |
| P4 | Palettes (§5, hors thèmes), scènes statiques et chenillards simples (§7) | Rendu simulateur puis matériel |
| P5 | Couches (§6), flashs, ambiance, positions calibrées, disposition Live, affectation APC mini | **Jalon 1** : soirée manuelle avec ce show |
| P6 | Scènes à effets (arcs-en-ciel, vagues, cercles, huit, segments, têtes) — **fait le 2026-09-28** : 10 scènes « Phase P6 » (couches Effets et Mouvements), dont un piège (grand cercle ramené par la zone interdite) ; 6 thèmes dans `palettes.json` ; trames de référence `P6-scenes.txt` | Rendu |
| P7 | Scènes « Phase P7 » — **fait le 2026-09-30** : 8 scènes (*Un PAR par temps*, *Couleur à chaque mesure*, *Flash sur le kick*, *Cercle calé sur la mesure*, *Mouvement lent à 30 BPM*, *Départ à la mesure*, *Calibration de latence*, *Lyres allumées (sans les PAR)*), trames de référence `P7-scenes.txt` (tempo fixe 120 BPM) ; la vitesse selon l'énergie se règle scène par scène | Synchro sur morceaux au choix ([guide P7](demos/P7-audio-tempo.md)) |
| P8 | Séquences (§8), shows génériques et par style (§9) — **fait le 2026-10-02** : 6 scènes (barres dans la couche Libre ; intensité, couleur et vague séparées pour *Groove* ; blanc des PAR pour le strobe de *Bloc refrain*), 6 séquences « Phase P8 » (*Montée 16 mesures*, *Groove 8 mesures*, *Break calme 8 mesures*, *Explosion drop 4 mesures*, *Pulsation couleurs (double temps)*, *Barres et multi-têtes 8 mesures*) et 7 shows (*Couplet / Refrain / Drop*, *Tirage au sort (variantes)*, *Ambiance UV et fumée (secondaire)*, *Visite guidée (sans musique)*, *Branches parallèles et macro-étape* et son sous-show *Bloc refrain (macro-étape)*, piège *Boucle sans condition*) ; catalogue commenté au [guide P8](demos/P8-show-et-sequences.md) §1 ; trames de référence `P8-shows.txt` (120 BPM, événements simulés). Les shows **par style** (§9) attendent les styles de P9 et le Directeur de P10 | Répétition au simulateur ([guide P8](demos/P8-show-et-sequences.md)) |
| P9 | **Fait le 2026-10-03** : base musicale d'amorçage livrée avec l'application (environ 460 artistes par famille, Luxia.Music/Seed/artistes-initiaux.txt, Q48) ; 2 shows « Phase P9 » : *Style du morceau (P9)* (Neutre, puis une ambiance par famille Rock / Électro / Latino / Slow / Inconnu, retour à Neutre au morceau suivant) et le piège *Piège : style qui n'existe pas (P9)* (avertissement de `valider`) ; trames de référence P9-shows.txt ; fichier d'exemple d'échange JSON (styles, artistes, alias) docs/demos/P9-exemple-echange.json. Les shows **par style** complets (§9) restent à construire avec les styles réellement joués (journal de soirée) et le Directeur de P10 | Styles détectés sur Deezer et YouTube Music ([guide P9](demos/P9-lecture-et-style.md)) |
| P10 | Réglages du Directeur (§10), métadonnées des shows | **Jalon 2** : soirée automatique |
| P11 | Timeline d'exemple (ouverture de bal) | Calage |

## 12. Samples par mécanique (à la demande)

Exemples de samples utiles (créés seulement si demandés) :

| Sample | Mécanisme montré |
|---|---|
| Lien au dimmer | PAR 3CH : couleur fixe, intensité / Grand Master / blackout agissant sur R, G, B |
| Fusion des couches | HTP sur l'intensité, LTP par priorité sur la couleur, 4 modes d'intensité |
| Fondu croisé | Les 3 cas du doc 15 §5.1 dans une couche exclusive |
| Roue de couleur | Palette par intention → emplacement le plus proche, bascule franche |
| Zones interdites | Lyre bornée, événement de sûreté |
| Phase d'effet | Linéaire / miroir / groupes sur 4 PAR et sur les segments de barres |
| Quantification | Lancement au temps, à la mesure, à la phrase |
| Grafcet | Divergence OU aléatoire, divergence ET, macro-étape, variables |
| Directeur | Simulation accélérée sur un scénario de soirée, rapport |

## 13. Points à valider par l'utilisateur

- Plan d'adresses (§2) et modes retenus.
- Disposition du lieu « Générique » (§3) : comment le matériel est-il réellement installé en soirée ?
- Thèmes de couleurs par style (§5) et liste des shows (§9).
