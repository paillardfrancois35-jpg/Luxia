# Journal du show de référence

> Ce qui a été ajouté à chaque phase, ce qui a été validé, les retours de l'utilisateur (doc 41 §1, REF-1 à REF-6).

## P0 – Fondations (2026-09-24)

### Ajouté

| Élément | Fichier | Description |
|---|---|---|
| Fiche du projet | `projet.json` | Identifiant stable, nom, description (format 1) |
| Enregistrement du chenillard | `Enregistrements/P0-chenillard-1-180.dmxrec` | Canaux 1 à 180, **180 (fumée) exclu**, valeur 50 % (128), 250 ms par canal, une passe (45,4 s, 1817 trames à 40 Hz) |

La configuration de sortie n'est **pas** dans le show : elle est dans les préférences du poste (SORT-006, Q15).

### Comment rejouer / vérifier

```bash
luxia-headless relire "samples/Show de référence/Enregistrements/P0-chenillard-1-180.dmxrec" --canaux 1-180
luxia-headless lancer --test 1-180 --exclus 180 --valeur 50 --pas 1000 --une-fois
```

La seconde commande rejoue le chenillard **sur le matériel** (1 s par canal, ≈ 3 min) : chaque appareil branché au plan
d'adresses du doc 41 §2 réagit quand son canal passe (avec la limite du mode 7 canaux expliquée dans le guide P0, exemple 3).

### Non-régression

Test automatique `ReferenceShowP0Tests` : l'enregistrement est relu (canaux 1 → 179 dans l'ordre, 180 jamais allumé,
une seule valeur à 128) et le moteur actuel, rejoué en temps virtuel, reproduit la même séquence.

### Validation par l'utilisateur

| Élément | Statut | Retour |
|---|---|---|
| Chaque appareil réagit au chenillard | ⏳ en attente | |

## P1 – Console (2026-09-24)

### Ajouté

| Élément | Fichier | Description |
|---|---|---|
| 6 instantanés de console (catégorie « Phase P1 ») | `console.json` | PAR 1 en blanc ; PAR 1 à 4 en blanc ; PAR 1 à 4 en rouge à 50 % ; Lyre 1 au centre ; Lyres 1 et 2 au centre ; UV plein |

Valeurs établies à partir des notices : LPC008S 7 canaux (fiche `betopper-lpc008s.md`), lyre Tomshine 11 canaux
(Pan, Pan fin, Tilt, Tilt fin, couleur, gobo, obturateur, gradateur, vitesse, contrôle, mode), BeamZ BUV463 7 canaux
(maître, 4 rangées UV, strobe / vitesse, programmes). Détail dans le guide `docs/demos/P1-console.md`.

### Non-régression

Test `ReferenceShowP1Tests` : le projet s'ouvre sans message ; chaque instantané, rappelé par le moteur, produit
exactement ses canaux ; aucun ne touche la fumée (180) ni les canaux de contrôle des lyres (120, 135).

### Validation par l'utilisateur

| Élément | Statut | Retour |
|---|---|---|
| Canaux conformes au plan d'adresses (chaque instantané allume ce qui est annoncé) | ⏳ en attente | |

## P2 – Bibliothèque (2026-09-25)

### Ajouté

| Élément | Emplacement | Description |
|---|---|---|
| Définitions des appareils du parc | `samples/Bibliothèque/` | LPC008S, LPC010, LPC120, mini lyre Tomshine (refaite le 25/09, BIB-095), BUV463, effet WZYBUTA (à vérifier) |

Les définitions sont dans une **bibliothèque d'exemple**, pas encore dans le projet : la copie des modèles utilisés dans le
projet (GEN-053) arrive avec le patch (P3). Pour les utiliser : Bibliothèque → Importer un dossier → `samples/Bibliothèque`.
Barre LCB803 : notice incomplète (Q24), générique « RGB » en attendant.

### Non-régression

`ParkLibraryTests` : les 6 définitions se chargent sans message et sans erreur de validation ; déductions vérifiées
(intensité virtuelle du LPC008S en 3 canaux, Pan/Tilt 16 bits de la lyre en 11 canaux, 4 cellules UV).

### Validation par l'utilisateur

| Élément | Statut | Retour |
|---|---|---|
| Plages vérifiées en direct sur chaque appareil (T-BIB-05) | ⏳ en attente | |
| Définition de l'effet WZYBUTA (Q25) | 🟡 20CH / 64CH saisis d'après ScanLibrary ; reste le sens de variation du canal 2 et le réglage du menu | 2026-09-25 |

## P3 – Installation + Simulateur (2026-09-26)

### Ajouté

| Élément | Fichier | Description |
|---|---|---|
| Copie des modèles utilisés (GEN-053) | `Bibliothèque/<fabricant>/<modèle>.json` | 6 définitions du parc, copiées telles quelles depuis `samples/Bibliothèque/` |
| Installation : univers, patch des 14 appareils | `installation.json` | Adresses conformes au plan (doc 41 §2) ; Gros PAR patchés en **Betopper LPC120, mode 8 canaux** (choix provisoire, Q27) — **corrigé le 2026-09-27** : gros PAR = générique **WT05** 7 canaux ; UV en **8 canaux** (8e canal « lissage »), UV 2 déplacé en **169** |
| Lieu « Générique » | `lieux.json` | 12 × 8 m, deux totems (2 PAR + 1 gros PAR + 1 lyre chacun), barres au sol devant, effet au centre, UV en façade, fumée au fond ; disposition à corriger selon l'installation réelle (doc 41 §3, à valider) |

### Non-régression

`ReferenceShowP3Tests` : l'installation se charge sans message, 14 appareils aux adresses du plan, aucun chevauchement,
chaque modèle utilisé est présent dans la copie du projet, chaque appareil est placé et présent dans le lieu « Générique ».

### Validation par l'utilisateur

| Élément | Statut | Retour |
|---|---|---|
| Modèle réel des gros PAR (LPC010 ou LPC120) et son mode (Q27) | ✅ 2026-09-27 | Ni l'un ni l'autre : **WT05** sans marque, 7 canaux comme le LPC008S |
| Disposition du lieu « Générique » conforme à l'installation réelle (doc 41 §13) | ⏳ en attente | |
| Adresses réglées sur les appareils, identification OK (doc 41 §11) | ⏳ en attente | |

## P4 – Moteur + Scènes (2026-09-26)

### Ajouté

| Élément | Fichier | Description |
|---|---|---|
| Couches par défaut (doc 17 §1.3, D28) | *(implicites)* | Intensité, Couleurs, Mouvements, Faisceau, Effets, Ambiance, Flashs ; `couches.json` est écrit au premier enregistrement depuis l'application |
| Palettes | `palettes.json` | 13 couleurs et 4 intensités par défaut (PAL-009) ; positions des lyres « Piste centre », « Plafond », « Croisé », « Repos » (**valeurs à calibrer sur place**, par lieu en P5) ; faisceau « Gobo étoile » |
| Sélections | `installation.json` | « PAR gauche → droite » (PAR 1 → 4), « Lyres » |
| 10 scènes, catégorie « Phase P4 » | `scènes.json` | Blanc chaud sur les 4 PAR ; Bleu sur tout le parc ; Chenillard 4 couleurs ; Lyres sur 3 positions ; Fondu lent (4 s) ; Instantané ; Roue de couleur qui bascule franchement ; Vague gauche → droite ; UV plein ; **Piège : couleur sans intensité** (DEMO-4) |

Chaque scène a une note (champ `notes`) qui dit ce qu'elle montre. Détail et marche à suivre : guide `docs/demos/P4-moteur-scenes.md`.

### Comment rejouer / vérifier sans matériel

```bash
luxia-headless valider "samples/Show de référence"
luxia-headless jouer "samples/Show de référence" --scene "Chenillard 4 couleurs" --duree 4 --pas 0.5
```

### Non-régression

`ReferenceShowP4Tests` : le projet se valide sans aucun problème ; chaque scène « Phase P4 » est jouée 6 s en temps
virtuel et ses trames sont comparées octet par octet à la référence `tests/assets/golden/P4-scenes.txt` (T-MOT-07) ;
contrôles ciblés : blanc chaud (gradateur + couleur), piège (PAR noirs), roue de couleur (jamais de valeur
intermédiaire), vague (décalage de 0,5 s entre PAR).

### Validation par l'utilisateur

| Élément | Statut | Retour |
|---|---|---|
| Rendu au simulateur puis sur le matériel (doc 41 §11) | ✅ validé le 2026-09-26 : guide P4 déroulé pas à pas, exemples 1 à 12 conformes (blanc chaud réglé à 100 / 42 / 0 %) | 4 PAR, lyre 1, barre 1 branchés ; les deux paires de PAR ne rendent pas les mêmes couleurs (doc 99, correction par appareil) |
| Positions des lyres à calibrer | 🟡 Lyre 1 calibrée au salon (2026-09-26) : Piste centre pan 46,6 % / tilt 65,7 %, Plafond pan 19,6 % / tilt 11,3 % (la proposition de départ visait le mur opposé), Croisé pan 39,1 % ; Lyre 2 non branchée, valeurs proposées (à calibrer quand elle sera raccordée) | Essai P4, exemple 4 |
| Plage « sans strobe » des PAR et barres (Q28) | ⏳ en attente | |

## P5 – Couches, Palettes, Live, MIDI (2026-09-27)

### Ajouté

| Élément | Fichier | Description |
|---|---|---|
| Plage « Pas de strobe » (Q28, BIB-101) | `Bibliothèque/Betopper/LPC008S.json`, `LPC120.json`, `Bibliothèque/BeamZ/LCB803.json` | LPC008S et LPC120 : 0-4 = pas de strobe ; LCB803 : 0 = pas de strobe |
| 15 scènes, catégorie « Phase P5 » | `scènes.json` | Intensité : **Plein feu** (MOT-042), Intensité 50 % ; Couleurs : Rouge / Bleu / Ambre – **couleur seule** ; Mouvements : Lyres : piste centre / plafond, **Piège : lyre 1 vers le public** ; Effets : **Strobe PAR (plafonné à 10 s)** ; Ambiance : **Fumée longue (plafonnée à 10 s)**, Fumée courte (3 s) ; Flashs : **Flash blanc**, **Strobe flash** (PAR et barres), **All Strobes** (tous les appareils à strobe, ajoutée à la demande de l'utilisateur pendant l'essai), **Blackout partiel (sauf UV)** |
| Zone interdite d'exemple | `lieux.json` | Lieu Générique : « Public (exemple) » pour chaque lyre, Pan 30-70 %, Tilt 85-100 % (à adapter à la salle) ; propriété parasite `active` retirée |
| Réglages du Live | `live.json` | Boutons FLASH = « Flash blanc », STROBE = « Strobe flash », rafale de fumée 3 s |
| Réglages de sûreté | `sûreté.json` | Valeurs par défaut écrites en clair : strobe 10 s puis 10 s de pause, fumée 10 s puis 30 s de repos |
| Couches | *(implicites)* | Modèle par défaut (doc 17 §1.3) : Ambiance protégée de « Tout arrêter », Flashs de type Flash, familles d'attributs par couche |

Affectation de l'APC mini : celle par défaut (doc 18b §3), pas de `midi.json`.

### Comment rejouer / vérifier sans matériel

```bash
luxia-headless valider "samples/Show de référence"
luxia-headless jouer "samples/Show de référence" --scene "Strobe PAR (plafonné à 10 s)" --duree 22 --pas 1
```

Un scénario peut combiner les couches et les flashs (verbes `flash`, `figer`, `fumee`, `canal`, doc 50 §13).

### Non-régression

`ReferenceShowP5Tests` : chaque scène « Phase P5 » rejouée 6 s et comparée à `tests/assets/golden/P5-scenes.txt` ;
contrôles ciblés : Intensité × Couleurs (couleur seule = PAR noirs, avec Plein feu = rouge), zone interdite (Tilt ramené
de 95 à 85 %), strobe coupé à 10 s puis repris après 10 s, fumée coupée à 10 s, blackout partiel qui garde les UV.

### Validation par l'utilisateur

| Élément | Statut | Retour |
|---|---|---|
| Guide P5, au simulateur puis sur le matériel | 🟡 en cours (2026-09-27) : exemples 1 et 2 | UV 1 raccordé à l'adresse 161 pendant l'essai |

## Essai P5 (2026-09-27)

Guide P5 validé au matériel. Corrections du parc : **BUV463 en 8 canaux** (8e canal « lissage du gradateur » non documenté, cause de l'« allumage lent »), UV 2 en **169** ; gros PAR = générique **WT05** 7 canaux. `sûreté.json` : `restFactor` 3 (repos de fumée proportionnel). Trames de référence P4 / P5 régénérées.

## Phase P6 – Effets (2026-09-28)

| Élément | Fichier | Contenu |
|---|---|---|
| Effet multi-têtes | `installation.json` | WZYBUTA passé en **64 canaux à l'adresse 181** (après la fumée) : 12 têtes RVBW pilotables une à une ; aucun autre appareil ne bouge (Q25) |
| Scènes « Phase P6 » | `scènes.json` | 10 scènes à effets : vague et miroir sur les PAR, arc-en-ciel et chenillard des 8 sections des barres, têtes décalées de l'effet multi-têtes, alternance Latino, scintillement UV, cercle et huit des lyres, **piège** « grand cercle de la lyre 1 » (ramené par la zone interdite) |
| Thèmes | `palettes.json` | Latino, Froid, Chaud, Disco, Club, Tricolore (PAL-010) |
| Bibliothèque d'effets | *(implicite)* | Pas de `effets.json` : les 19 modèles livrés (EFF-007) |

### Comment rejouer / vérifier sans matériel

```bash
luxia-headless valider "samples/Show de référence"
luxia-headless jouer "samples/Show de référence" --scene "Vague sur les PAR (gauche → droite)" --duree 2 --pas 0.5
```

### Non-régression

`ReferenceShowP6Tests` : chaque scène « Phase P6 » rejouée 6 s et comparée à `tests/assets/golden/P6-scenes.txt` ;
contrôles ciblés : vague à un quart de cycle d'écart, chenillard d'une seule section à la fois, 4 couleurs sur les 12 têtes,
cercle de 60° de Pan, grand cercle jamais dans la zone interdite. Trames P4 / P5 régénérées (scènes visant tout le parc :
l'effet multi-têtes a d'autres canaux).


## Phase P7 – Audio et tempo (2026-09-30)

| Élément | Fichier | Contenu |
|---|---|---|
| Scènes « Phase P7 » | `scènes.json` | 8 scènes au rythme (la dernière, *Lyres allumées (sans les PAR)*, remplace *Plein feu* qui masquait le chenillard des PAR) : *Un PAR par temps (chenillard au tempo)*, *Couleur à chaque mesure*, *Flash sur le kick* (impulsions basses, au temps sans musique), *Cercle calé sur la mesure* (effet d'une mesure), *Mouvement lent à 30 BPM (horloge propre)*, *Départ à la mesure (blanc chaud)* (quantifié), *Calibration de latence* (flash sur chaque deuxième temps, pour l'écran Audio) |
| Options de scène | `scènes.json` | `advance`, `advanceEvery`, `quantize`, `ownBpm`, `energySpeed` (doc 50 §12) |

### Comment rejouer / vérifier sans matériel

```bash
luxia-headless valider "samples/Show de référence"
luxia-headless jouer "samples/Show de référence" --scene "Un PAR par temps (chenillard au tempo)" --duree 3 --pas 0.25
luxia-headless scenario "samples/Show de référence" mon-scenario.txt   # verbes tempo 90, tap, ajuster-tempo x2|/2|un-ici
luxia-headless audio tests/assets/audio --rapport rapport.md           # rapport chiffré de l'analyse du son
```

### Non-régression

`ReferenceShowP7Tests` : chaque scène « Phase P7 » rejouée 6 s à 120 BPM fixes et comparée à `tests/assets/golden/P7-scenes.txt` ;
un PAR par temps, tempo du scénario, départ quantifié à la mesure, horloge propre indépendante du tempo principal.

## Phase P8 – Show & séquences (2026-10-02)

| Élément | Fichier | Contenu |
|---|---|---|
| Séquences « Phase P8 » | `séquences.json` | *Montée 16 mesures* (bleu 8 mesures, chenillard 4, blanc chaud 4 ; lyres en 3 positions puis cercle à la mesure ; vague puis strobe sur les PAR ; **rampe du niveau de la couche Couleurs** de 30 à 100 % sur 8 mesures ; fumée sur la dernière mesure), *Groove 8 mesures* (en boucle : une couleur par mesure, vague, lyres au centre puis en huit), *Break calme 8 mesures* (50 %, bleu, plafond, UV), *Explosion drop 4 mesures* (flash d'un temps, strobe 2 mesures puis vague, chenillard, cercle, fumée) |
| Shows « Phase P8 » | `shows.json` | *Couplet / Refrain / Drop* (exemple du doc 20 §3.4 : intro → couplet à l'énergie Groove → refrain au drop, montée, retour au break ou après 16 mesures, final au 3e refrain, qui reste), *Tirage au sort (variantes)* (variante A 60 % / B 40 % toutes les 8 mesures, jamais deux fois la même), *Ambiance UV et fumée (secondaire)* (en parallèle du show principal : UV, une chance sur deux d'une rafale de fumée à chaque phrase de 8 mesures), **piège** *Boucle sans condition* (refusé au lancement, erreur de `valider`) |

### Comment rejouer / vérifier sans matériel

```bash
luxia-headless valider "samples/Show de référence"
luxia-headless jouer "samples/Show de référence" --sequence "Montée 16 mesures" --duree 34 --pas 1
luxia-headless jouer "samples/Show de référence" --show "Tirage au sort (variantes)" --duree 120 --pas 4
luxia-headless scenario "samples/Show de référence" mon-scenario.txt   # verbes show, sequence, forcer, simuler drop|break|montee, energie 40, style "Électro"
```

### Non-régression

`ReferenceShowP8Tests` : chaque séquence « Phase P8 » rejouée sur toute sa longueur et chaque show (sauf le piège) 50 s au
métronome avec la même suite d'événements simulés (énergie, drop, break, montée), comparés à `tests/assets/golden/P8-shows.txt` ;
rampe de la couche Couleurs ; piège refusé.
