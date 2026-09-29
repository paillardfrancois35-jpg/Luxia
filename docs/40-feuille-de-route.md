# 40 – Feuille de route

> Découpage en phases, contenus, livrables, critères de fin. Chaque phase se termine par une application **utilisable et testée**.
> Références : [00 §13](00-analyse-et-decoupage.md), [30 (tests, jalons)](30-plan-de-tests.md).

---

## 1. Vue d'ensemble

```
P0 Fondations ─▶ P1 Console ─▶ P2 Bibliothèque ─▶ P3 Installation + Simulateur ─▶ P4 Moteur + Scènes ─▶ P5 Couches, Palettes, Live, MIDI
                                                                                                             │
                                                                                          ★ JALON 1 « Soirée manuelle »
                                                                                                             │
     ┌───────────────────────────────────────────────────────────────────────────────────────────────────────┘
     ▼
P6 Effets ─▶ P7 Audio & tempo ─▶ P8 Show & séquences ─▶ P10 Directeur ─▶ ★ JALON 2 « Soirée automatique » ─▶ P11 Timeline ─▶ P12 Extensions
                                        ▲
             P9 Lecture en cours & Style (en parallèle dès P5) ┘

PoC-1 (P0) protocole Arduino · PoC-2 (dès P1) tempo audio · PoC-3 (dès P5) position de lecture · PoC-4 (dès P5) sources de styles
```

## 2. Phases

| Phase | Contenu | Documents | Livrable | Critère de fin |
|---|---|---|---|---|
| **P0 – Fondations** | Solution et projets ; modèle de base ; boucle du moteur (vide) cadencée ; routeur de sorties ; pilotes Arduino, Nul, Enregistreur ; firmware Enttec ; préférences ; journal technique ; persistance JSON (versions, migrations) | 02, 10 | Appli qui émet une trame à 40 Hz, se reconnecte, s'arrête proprement | Exigences I de P0 validées ; T-SORT-04 à 07 |
| **P1 – Console** | Console mode canaux ; surcharges ; moniteur de sortie ; thème sombre, shell de l'appli | 11 | Piloter à la main chaque canal du parc | SC-01 |
| **P2 – Bibliothèque** | Modèle d'appareil complet ; éditeur ; validation ; test en direct ; import OFL (+ QLC+) ; génériques | 12 | Tout le parc décrit et vérifié | Annexe A du doc 12 complète |
| **P3 – Installation + Simulateur** | Canaux maintenus du test de sortie (SORT-008) ; univers, patch, sélections, lieux, plan, identification, fiche d'installation ; simulateur 2D ; console mode appareils | 13, 14, 11 | Parc patché, visible au simulateur | SC-02 |
| **P4 – Moteur + Scènes** | Moteur complet (fondus, fusion, couleurs, conversions, surcharges, blackout, GM) ; programmeur ; scènes ; palettes de base | 15, 16, 17 | Créer et jouer des scènes au simulateur et sur le matériel | SC-03 ; golden files en place |
| **P5 – Couches, Palettes, Live, MIDI** | Couches, flashs, figer, sûreté (strobe, fumée, zones) ; palettes complètes (lieu) ; Live ; APC mini ; sauvegarde auto, reprise ; assistant d'installation | 17, 18, 18b, 13 | ★ **Jalon 1** | Check-list jalon 1 |
| **P6 – Effets** | Générateur d'effets, bibliothèque d'effets, cellules | 16, 15 | Arcs-en-ciel, cercles, vagues | T-EFF |
| **Chantier « Groupes et dimmers »** (entre P6 et P7, décision de l'utilisateur du 2026-09-29, Q37) | Arbre de groupes d'appareils (un appareil dans un seul groupe), onglet « Gestion des dimmers » (Installation), panneau « Groupes dimmer » (Contrôle), règle **proportionnelle** (chaque étage multiplie), « réglage de couche » à la place du master d'intensité, faders d'une seconde platine MIDI | 15, 17, 18b, 60 | Doser l'intensité par groupe d'appareils, à l'écran et au MIDI | Conception validée par l'utilisateur, puis essai |
| **P7 – Audio & tempo** | Capture, horloge (audio / tap / fixe), impulsions, énergie, breaks/drops ; durées musicales ; réactivité des scènes ; calibration | 19, 15, 16 | Lumière calée sur la musique | Rapport du jeu audio ; SC-07 |
| **P8 – Show & séquences** | Séquences en mesures ; éditeur et exécution des shows ; supervision ; mode simulation | 20 | Shows complets au simulateur | SC-08 |
| **P9 – Lecture & Style** | Lecture en cours ; normalisation ; base musicale ; identification ; corrections ; journal de soirée ; outil d'enrichissement | 21 | Style affiché et corrigeable en Live | Rapport du jeu de titres ; SC-09 |
| **P10 – Directeur** | Sélection, rotation, réactions, verrous, budgets, manuel/auto, répétition, simulation accélérée | 22 | ★ **Jalon 2** | Check-list jalon 2 |
| **P11 – Timeline** | Timeline par morceau, synchronisation | 23 | Moments forts préparés | T-TL |
| **P12 – Extensions** | 3D simple, Art-Net, GDTF, IA locale d'enrichissement, classification audio, visée géométrique, télécommande Android (projet séparé) | divers | Au fil des envies | — |

## 3. Preuves de concept

| PoC | Quand | Question | Décision attendue |
|---|---|---|---|
| **PoC-1** | P0 | Protocole Enttec + rafraîchissement autonome + chien de garde sur Leonardo : gigue, débit, robustesse | Protocole définitif du firmware |
| **PoC-2** | Dès P1 (parallèle) | Détection de tempo / phase / énergie sur 30 morceaux | Algorithme maison ou bibliothèque |
| **PoC-3** | Dès P5 (parallèle) | Lecture en cours selon Spotify, Deezer, YouTube (Music) dans les navigateurs ; précision de position ; pubs | Règles de normalisation ; faisabilité de la timeline |
| **PoC-4** | Dès P5 (parallèle) | Qualité des étiquettes de style des sources en ligne / IA locale sur 100 titres réels | Sources retenues pour l'enrichissement |

## 4. Travaux de contenu (hors développement)

À mener en parallèle par l'utilisateur, car ils prennent du temps calendaire :

| Travail | Dès | Pour |
|---|---|---|
| Fournir les tableaux DMX des 6 PAR (LPC010 / LPC120 / LC003-H) | Maintenant | P2 |
| Collecter les titres de playlists de soirées (pour le jeu de test et la base) | P5 | P9 |
| Sélectionner et annoter les 30 morceaux de référence audio | P1 | PoC-2, P7 |
| Créer les scènes et couches de son style | P5 | Jalon 1 |
| Créer les shows par style (seul ou avec une IA de conception, GEN-130 à 134) | P8 | Jalon 2 |

## 5. Utilisation des modèles d'IA (recommandation)

| Travaux | Modèle conseillé |
|---|---|
| Conception, révisions du cahier des charges, architecture | Opus |
| P4 (moteur), P7 (audio), P8 (séquenceur), P10 (Directeur) | Opus |
| P0-P3, P5, P6, P9, P11 : écrans, éditeurs, persistance, pilotes, tests | Sonnet |
| Tâches mécaniques (saisie de définitions à partir de tableaux, mise en forme) | Haiku / Sonnet |

## 6. Règles de conduite du projet

- Une phase n'est **livrée** qu'avec ses **démonstrations** (§7) : code + exemples + guide.
- On **ne commence pas** une phase tant que les exigences I de la précédente ne sont pas validées (sauf travaux parallèles indiqués).
- Toute nouvelle idée va dans un **carnet d'idées** (`docs/99-idees.md`) ; elle n'entre dans une phase qu'après mise à jour du document de module concerné.
- Tout écart constaté au développement entraîne une **mise à jour du cahier des charges** (le document reste la référence).
- Les décisions structurantes sont ajoutées au registre des décisions (doc 02 §19).

## 7. Démonstrations livrées à chaque phase

### 7.1 Principe

Chaque phase livre, en plus du code, de quoi **comprendre et juger** ce qui a été développé, sans rien avoir à construire soi-même :

| Élément | Contenu |
|---|---|
| **Show de référence** | Le projet unique `samples/Show de référence/` (doc 41), enrichi à chaque phase, construit avec **les appareils réels du parc** (définitions de la bibliothèque, patch réel, adresses réelles). Tant qu'un appareil n'est pas défini (ex. gros PAR), un modèle générique équivalent le remplace, signalé comme tel. |
| **Exemples** | Des objets prêts à jouer propres à la phase (scènes, effets, shows…), nommés clairement et rangés dans une catégorie « Phase Pn » du show de référence (doc 41, REF-3) ; samples isolés par mécanique sur demande (doc 41 §12). |
| **Guide de découverte** | `docs/demos/Pn-<nom>.md` : pour chaque exemple, **comment le lancer**, **ce qu'on doit observer** (au simulateur et sur le matériel), **ce que ça illustre**, et **comment le modifier** pour essayer soi-même. |
| **Fonctionnement sans matériel** | Chaque exemple est jouable au simulateur ; le guide précise ce qui ne se voit bien que sur le matériel. |
| **Retour** | Le guide se termine par une courte grille « correct / à revoir / idée » que l'utilisateur remplit ; les retours alimentent le carnet d'idées ou des corrections. |

### 7.2 Règles

| ID | Règle |
|---|---|
| DEMO-1 | Une phase n'est pas livrée sans son guide et ses exemples. |
| DEMO-2 | Les exemples utilisent les **vrais appareils** du parc (annexe A du doc 12), pas des appareils fictifs. |
| DEMO-3 | Les exemples des phases précédentes restent jouables : ils sont **rejoués automatiquement** et comparés à des trames de référence (non-régression, doc 30). |
| DEMO-4 | Au moins **un exemple par fonctionnalité visible** de la phase, et au moins **un exemple « piège »** montrant une limite ou une règle de sûreté (strobe plafonné, zone interdite…). |
| DEMO-5 | Les exemples ne dépendent ni d'Internet ni d'un fichier personnel (sauf audio : le guide indique de lancer n'importe quel morceau). |

### 7.3 Contenu prévu par phase (indicatif)

| Phase | Exemples |
|---|---|
| P0 | Chenillard de test canal par canal sur 1-16 (plage, canaux exclus et valeur réglables ; fumée exclue) ; débranchement / rebranchement commenté ; enregistrement de trames relu |
| P1 | Instantanés de console : « PAR 1 en blanc », « lyre au centre », « UV plein » ; démonstration prise / libération d'un fader |
| P2 | Les définitions de tous vos appareils ; parcours du test en direct sur un PAR (plages du canal Strobe) et sur une lyre (roue de couleur) |
| P3 | Installation complète de votre parc ; lieu « Salon » (pour tester chez vous) ; fiche d'installation ; chenillard d'identification |
| P4 | Scènes : blanc chaud sur les 4 PAR (lien au dimmer visible), couleur fixe sur tout le parc, chenillard 4 couleurs, lyres sur 3 positions, fondu lent vs instantané, roue de couleur qui bascule franchement |
| P5 | Couches Intensité × Couleurs × Mouvements combinées ; flash blanc ; figer ; strobe plafonné à 10 s ; fumée plafonnée ; zone interdite des lyres ; configuration APC mini |
| P6 | Arc-en-ciel sur les barres (cellules), vague d'intensité sur les PAR gauche → droite, cercle et huit sur les lyres, effet miroir, têtes de l'effet multi-têtes décalées |
| P7 | Chenillard au temps, flash sur kick, cercle calé sur la mesure, vitesse suivant l'énergie ; outil de calibration de latence |
| P8 | Séquence « Montée 16 mesures » ; show « Couplet / Refrain / Drop » ; show avec tirage aléatoire ; mode simulation sans musique |
| P9 | Base musicale d'exemple (quelques artistes par famille) ; démonstration de titres YouTube « sales » normalisés ; correction de style mémorisée |
| P10 | Jeu de shows par style ; simulation accélérée de 6 h avec rapport ; répétition sur une playlist au choix |
| P11 | Une timeline de démonstration au métronome, puis sur un morceau au choix |
