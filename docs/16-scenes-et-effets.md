# 16 – Scènes, programmeur et effets

> Cahier des charges – modules **Scènes** (préfixe `SCN`) et **Effets** (préfixe `EFF`). Phases : **P4** (scènes), **P6** (effets), **P7** (réactivité musicale).
> Références : [15 (exécution)](15-moteur-de-rendu.md), [17 (couches, palettes)](17-couches-et-palettes.md), [13 (sélections)](13-installation-et-lieux.md).

---

## 1. Principes

- Une **scène** est une suite d'**étapes** (≥ 1). Une scène à une étape bouclée à l'infini est un état fixe (« cue ») ; à plusieurs étapes, c'est un « chase ». **Un seul concept.**
- Une scène ne contient **que les attributs qu'elle touche** : c'est ce qui permet de superposer les couches.
- Les valeurs sont exprimées sur des **appareils ou des sélections** et des **attributs**, idéalement via des **palettes** (P4 du doc 02).
- Les **effets** générés remplacent les longues listes d'étapes (arc-en-ciel, cercles, vagues).

## 2. Modèle de données

```
Scène
 ├─ id, nom, couleur, icône, catégorie, notes
 ├─ Visible en Live (l'« œil »), couche d'appartenance (doc 17)
 ├─ Paramètres de lecture
 │    ├─ boucle : une fois | N fois | infini | aller-retour | aléatoire
 │    ├─ fin : s'arrêter | rester sur la dernière étape | enchaîner sur <scène>
 │    ├─ fondu d'entrée par défaut, fondu de sortie
 │    ├─ vitesse (multiplicateur)
 │    ├─ horloge : principale | fixe (<BPM>)
 │    ├─ avance des étapes : durée | chaque N temps/mesures | chaque impulsion (basses/aigus)
 │    └─ quantification du lancement : aucune | temps | mesure | phrase (4 / 8 mesures)
 └─ Étapes[]
      ├─ nom (facultatif), durée de maintien, fondu d'entrée, courbe
      │   (durées en secondes ou en temps musicaux ; limite : seul l'ancien écran Scènes offre le choix s / temps / mesures, la fenêtre d'édition ne saisit que des secondes — E2, en tête de P8)
      ├─ Valeurs[] : cible (appareil | cellule | sélection) × attribut → valeur | palette | plage
      │               (option par valeur : fondu propre, retard)
      └─ Effets[] (§6)
```

## 3. Exigences – scènes

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| SCN-001 | I | P4 | Créer, dupliquer, renommer, supprimer une scène ; lui donner couleur, icône, catégorie. | — |
| SCN-002 | I | P4 | Une scène comporte **1 à N étapes** ; ajouter, insérer, dupliquer, supprimer, réordonner (glisser) des étapes. | — |
| SCN-003 | I | P4 | Chaque étape définit une durée de **maintien** et une durée de **fondu d'entrée**, en secondes ou en temps musicaux (GEN-023), et une courbe. | — |
| SCN-004 | I | P4 | Modification **groupée** des durées de plusieurs étapes (toutes à 1 temps, fondus à 0…). | — |
| SCN-005 | I | P4 | Paramètres de lecture du §2 (boucle, fin, fondus, vitesse). | Tests moteur (MOT-013/014/015). |
| SCN-006 | I | P7 | Paramètres musicaux du §2 (horloge, avance à l'événement, quantification). | Tests moteur (MOT-016 à 020). |
| SCN-007 | I | P4 | Les valeurs peuvent cibler un **appareil**, une **cellule** ou une **sélection** ; une valeur sur une sélection s'applique à tous ses membres présents (y compris ceux ajoutés plus tard à une sélection automatique). | Ajouter un PAR à « Tous les PAR » → la scène le pilote. |
| SCN-008 | I | P4 | Une valeur peut être : une valeur directe, une **référence de palette** (mise à jour automatique si la palette change), ou une **plage** (valeur médiane ou position dans la plage). | Modifier la palette « Ambre » → toutes les scènes suivent. |
| SCN-009 | I | P4 | Drapeau **Visible en Live** (l'œil) : une scène masquée n'apparaît pas sur l'écran Live mais reste utilisable par les shows et séquences. | — |
| SCN-010 | M | P4 | **Retard par membre** (« fan » temporel) : pour une valeur sur une sélection, le fondu de chaque membre peut être décalé (répartition linéaire sur une durée), dans l'ordre de la sélection. | Vague de couleur gauche → droite. |
| SCN-011 | M | P4 | Fondu **par attribut** : dans une étape, un attribut peut avoir son propre fondu (ex. couleurs en 2 s, position instantanée). | — |
| SCN-012 | M | P4 | Ranger les scènes par **catégorie** et filtrer la liste (catégorie, couche, visibles). | — |
| SCN-013 | M | P4 | Rapport des **utilisations** d'une scène (couches, séquences, shows) avant suppression. | — |
| SCN-014 | S | P6 | **Assistants de création** : « chenillard de couleurs sur une sélection », « alternance de 2 palettes », « balayage de positions » → génèrent les étapes. | Chenillard 4 PAR × 4 couleurs généré en 1 opération. |

## 4. Programmeur (éditeur de scène)

```
┌ Scène : « Arc-en-ciel lent »  [Couche : Couleurs ▾] [👁 Visible] [Aveugle ☐]  [▶ Tester] [■]              ┐
├──────────────── Sélection ────────────┬────────────── Outils d'attributs ──────────────────────────────────┤
│ Plan (mini simulateur) + liste         │ Intensité : [████████░░] 80 %        Palettes intensité : [100][50]│
│ ☑ PAR 1  ☑ PAR 2  ☐ Lyre 1 …           │ Couleur   : (roue chromatique)  R G B W   Palettes : [Rouge][Ambre]…│
│ Sélections : [Tous les PAR] [Lyres] …  │ Position  : (pad XY)  Pan 45°  Tilt 30°   Palettes : [Piste][Boule]  │
│                                        │ Faisceau  : Gobo [1][2][3]…  Strobe [Fermé][Ouvert][Lent→rapide]      │
│                                        │ Effets    : [+ Effet]                                                  │
├─────────────────────────────── Étapes ──────────────────────────────────────────────────────────────────────┤
│ [1 ■■■■ 2 t / 1 t] [2 ■■■■ 2 t / 1 t] [3 ■■■■ …] [+]     Boucle : infini   Avance : durée   Horloge : principale │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| SCN-030 | I | P4 | Sélection d'appareils par clic / lasso au plan, dans la liste, ou par sélection enregistrée ; sélection de cellules. | — |
| SCN-031 | I | P4 | Outils d'attributs adaptés à la sélection (seuls les attributs communs ou présents sont affichés) : fader, roue chromatique, pad Pan/Tilt, boutons de plages, boutons de palettes. | Sélection PAR + lyre → couleur et intensité ; Pan/Tilt pour la lyre seule. |
| SCN-032 | I | P4 | Seuls les attributs **modifiés** dans le programmeur sont enregistrés dans l'étape (marqués visuellement). Un attribut peut être « retiré » de l'étape. | Scène « couleurs » ne contient pas d'intensité si on n'y a pas touché (sauf MOT-041). |
| SCN-033 | I | P4 | Enregistrer dans l'étape courante : **remplacer** ou **fusionner** ; enregistrer comme **nouvelle étape**. | — |
| SCN-034 | I | P4 | **Tester** : jouer la scène seule (solo) ou dans son contexte (les autres couches restant actives). | — |
| SCN-035 | I | P4 | **Aveugle** : éditer sans modifier la sortie ; aperçu au simulateur (GEN-063). | — |
| SCN-036 | I | P4 | Option **« allumer en coloriant »** (MOT-041), active par défaut. | — |
| SCN-037 | M | P4 | **Enregistrer depuis la sortie** : capturer l'état actuel (tout ou une sélection d'appareils / d'attributs) dans une étape. | Capture de la console (CONS-025). |
| SCN-038 | M | P4 | Copier / coller des valeurs entre appareils, étapes et scènes ; « appliquer à la sélection en miroir » (lyres symétriques). | — |
| SCN-039 | I | P4 | Annuler / rétablir (GEN-102). | — |

## 5. Réactivité musicale des scènes

Une scène (ou une étape, ou un effet) peut s'abonner aux trois signaux du doc 19 :

| Signal | Usages dans une scène | Paramètres |
|---|---|---|
| **A – Horloge tempo** | Durées en temps/mesures ; avance d'étape tous les N temps ; vitesse d'effet calée sur le tempo | Fréquence des étapes : ×4, ×2, ×1, ÷ 2, ÷ 4, ÷ 8 ; période d'un effet : N temps ou N mesures (MOT-062) |
| **B – Impulsions** (basses / aigus) | Avance d'étape à chaque impulsion ; effet « bump » d'intensité | Bande, seuil de force, temps mort minimal entre deux déclenchements |
| **C – Énergie** | Moduler la vitesse, la taille d'un effet ou l'intensité selon l'énergie | Plage d'énergie → plage de valeur, lissage |

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| SCN-050 | I | P7 | Les paramètres ci-dessus sont réglables par scène (avance, horloge) et par effet (vitesse, bump, modulation). | — |
| SCN-051 | M | P7 | **Modulation par l'énergie** : la vitesse, la taille d'un effet ou une intensité peuvent suivre l'énergie (doc 19). | Énergie haute → cercle plus rapide. |
| SCN-052 | M | P7 | En l'absence de signal audio (silence, capture arrêtée), une scène à avance « impulsion » avance au temps de l'horloge (repli). | Pas de scène figée pendant un break. |

## 5b. Notes de réalisation de la réactivité musicale (P7)

Réalisé : **avance d'étape** au temps, à la mesure ou aux impulsions (SCN-050, MOT-017), **quantification du lancement**, **horloge propre** et **vitesse selon l'énergie** (SCN-051, sur la vitesse de la scène seulement), repli au temps sans signal audio (SCN-052). Réglages dans la fenêtre d'édition, volet « Au rythme » ; format : [doc 50](50-format-des-donnees.md). **Non fait** : réglages propres à un effet (bump d'intensité sur impulsion, modulation de la taille ou de l'intensité par l'énergie). Détails : [15 §16](15-moteur-de-rendu.md), [19 §10](19-audio-et-tempo.md).

## 6. Effets générés

> Principe de fonctionnement d'ensemble (types, plusieurs effets par étape, combinaison, continuité) : [doc 16b](16b-effets-fonctionnement.md).

### 6.1 Paramètres d'un effet

| Paramètre | Valeurs |
|---|---|
| Cible | Sélection (appareils ou cellules), attribut(s) : intensité, couleur, Pan/Tilt, ou tout attribut continu |
| **Forme** | Intensité : sinus, triangle, carré (on/off), dent de scie montante/descendante, impulsion, aléatoire (scintillement) ; Position : **cercle**, **huit**, balayage horizontal, balayage vertical, aléatoire lent ; Couleur : **arc-en-ciel**, alternance palette A / palette B, dégradé A→B |
| Vitesse | Hz, ou N temps / N mesures par cycle (MOT-062) |
| Taille | Amplitude (%, degrés) |
| Centre | Valeur de base, palette, ou valeur sous-jacente (mode relatif) |
| **Phase** | Décalage entre membres : 0-360° réparti ; modes : linéaire, miroir (depuis le centre), par groupes de N, aléatoire (fixé) |
| Sens | Avant / arrière / aller-retour |
| Rapport cyclique | Pour les formes carré / impulsion |
| Mode | Relatif / absolu (MOT-061) |
| Réactivité | Bump sur impulsion ; modulation par énergie (§5) |

### 6.2 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| EFF-001 | I | P6 | Ajouter un ou plusieurs effets à une étape, avec les paramètres du §6.1. | — |
| EFF-002 | I | P6 | Formes d'intensité : sinus, triangle, carré, dent de scie, impulsion, aléatoire. | Tests de forme (valeurs à t donné). |
| EFF-003 | I | P6 | Formes de position : cercle, huit, balayages, aléatoire lent ; appliquées autour d'un centre (palette de position). | Cercle autour de « Piste centre ». |
| EFF-004 | I | P6 | Formes de couleur : arc-en-ciel, alternance, dégradé entre palettes. | — |
| EFF-005 | I | P6 | Phase répartie selon l'**ordre de la sélection**, modes linéaire / miroir / groupes / aléatoire. | Chenillard gauche → droite ; miroir du centre vers les bords. |
| EFF-006 | I | P6 | Aperçu en direct au simulateur pendant le réglage des paramètres. | — |
| EFF-007 | M | P6 | **Bibliothèque d'effets prédéfinis** (modèles réutilisables : « Vague douce », « Cercle rapide », « Chenillard on/off »…), applicables à n'importe quelle sélection. | Appliquer « Vague douce » aux barres. |
| EFF-008 | M | P6 | Effets sur **cellules** (segments de barre, têtes de l'effet multi-têtes). | Chenillard sur les 16 segments des 2 barres. |
| EFF-009 | S | P6 | Combinaison de deux effets sur un même attribut (ex. cercle + balayage) par addition. | — |
| EFF-011 | I | P6 | **Effet d'intensité visible** (essai P6) : ajouter un effet d'intensité à des appareils sans couleur dans l'étape leur donne le **blanc** (palette « Blanc »), modifiable ensuite ; inverse de MOT-041 (un PAR RVB aux émetteurs à 0 reste noir quelle que soit son intensité). | Nouvelle scène + chenillard sur les 4 PAR : ils s'allument. |
| EFF-012 | I | P6 | **Type d'effet lisible** : le panneau affiche le type (Intensité, Mouvement, Couleur), ne propose que les formes de ce type et marque « (modifié) » un effet dont la forme diffère du modèle. Principe d'ensemble : [doc 16b](16b-effets-fonctionnement.md). | Modèle « Arc-en-ciel en vague » sur 4 PAR : seules 3 formes de couleur. |

### 6.3 Précisions (P6)

| Sujet | Précision |
|---|---|
| Cible | **Plusieurs** cibles possibles (appareils, cellules, sélections, sélections automatiques), mises bout à bout dans l'ordre : leurs membres forment ceux de l'effet (un membre en double ne compte qu'une fois). Option **par cellule** : chaque segment d'une barre, chaque tête d'un effet multi-têtes devient un membre (EFF-008). |
| Taille, centre | Intensité (et tout attribut continu) : fraction 0-1 (affichée en %). Position : **degrés**, convertis selon la course Pan / Tilt du modèle (540° / 270° si elle n'est pas renseignée). |
| Décalage (phase) | En degrés, entre le premier et le dernier membre : 0 = tous ensemble, 360 = un cycle réparti. **Par groupes de N** = un membre sur N ensemble (N = 2 : pairs / impairs). **Miroir** = du centre de la sélection vers les bords. **Aléatoire** = ordre tiré une fois pour toutes (même effet, même ordre). |
| Centre d'un mouvement | Palette de position (lieu actif, PAL-004) : l'effet est alors absolu autour d'elle ; sinon relatif (autour de la position de l'étape ou de la position sous-jacente) ou absolu autour du milieu de la course. |
| Couleurs | Arc-en-ciel (tour des teintes) ; alternance (en escalier) ; dégradé (progressif, revient à la première couleur) ; couleurs données une à une (couleur ou palette) ou par un **thème** (PAL-010). Toujours absolu. Un effet de couleur n'allume pas un appareil éteint : l'écran ajoute l'intensité à 100 % aux cibles qui n'en ont pas (MOT-041). |
| Vitesse | Durée d'un cycle, en secondes (affichée aussi en Hz) ou en temps musicaux (120 BPM fixe jusqu'à P7, MOT-062). La vitesse de la scène (MOT-015) s'applique aussi à ses effets. |
| Aléatoire | Reproductible : même graine de session, mêmes trames (MOT-004, principe P6 du doc 02). |

## 7. Tests

| Test | Type | Contenu |
|---|---|---|
| T-SCN-01 | Unitaire | Modèle : cibles appareil / cellule / sélection ; références de palette résolues. |
| T-SCN-02 | Unitaire | Programmeur : seuls les attributs modifiés sont enregistrés ; remplacer / fusionner. |
| T-SCN-03 | Unitaire | Retards par membre, fondus par attribut. |
| T-EFF-01 | Unitaire | Chaque forme : valeurs attendues à des instants donnés, pour chaque mode de phase. |
| T-EFF-02 | Unitaire | Effets calés sur tempo : départ de cycle sur le temps. |
| T-SCN-04 | Manuel | Créer au simulateur 10 scènes types (couleurs, mouvements, strobe, UV) + 5 effets, vérification visuelle puis sur matériel. |

## 8. Notes de réalisation (P4)

| Sujet | Réalisation |
|---|---|
| Écran | Nouvel écran **Scènes** : liste filtrée (texte, catégorie, couche, visibles en Live) avec lecture ▶ ■ ; éditeur (identité, lecture, étapes) ; programmeur ; palettes. Chaque modification est enregistrée tout de suite ; annuler / rétablir sur l'ensemble des scènes (100 niveaux). |
| Valeurs (D27) | Une valeur vise des attributs, jamais des canaux ; une valeur sur une cellule l'emporte sur l'appareil, qui l'emporte sur une sélection ; une sélection automatique (« Tous les PAR »…) est recalculée à chaque compilation (SCN-007). |
| Programmeur | Hors aveugle, ses réglages sont des **surcharges d'attributs** (étape 5) : ils passent **au-dessus** des scènes testées (un message le rappelle). En aveugle, ils vont au moteur d'aperçu. Un réglage choisi par un bouton de sélection vise la sélection ; coché à la main, il vise chaque appareil. |
| Enregistrer (SCN-033) | Le programmeur est un brouillon : « Charger l'étape », « Remplacer », « Fusionner », « Nouvelle étape ». Choisir une étape la charge si le programmeur n'a rien de nouveau. |
| « Allumer en coloriant » (MOT-041) | À l'enregistrement : une cible qui reçoit une couleur sans aucune valeur d'intensité reçoit l'intensité 100 % ; une intensité réglée à 0 est respectée. |
| SCN-010 / SCN-011 | Fondu propre et retard réparti réglables dans le programmeur (pour les prochains réglages ou appliqués aux réglages faits). |
| SCN-002 | Réordonner par boutons ◀ ▶ ; pas de glisser-déposer. |
| SCN-030 (partiel) | Liste d'appareils et raccourcis de sélection ; le clic / lasso au plan (SIM-010) et la sélection de cellules (INST-034) restent à faire. |
| SCN-031 (partiel) | Faders R/V/B (+ blanc, ambre, UV selon la sélection) et Pan/Tilt, boutons de plages et de palettes ; pas encore de roue chromatique ni de pad XY. |
| SCN-013 | Enchaînements ; couches, séquences et shows s'y ajouteront. |
| SCN-037, CONS-025 | « Capturer la sortie » : ce qui est émis pour les appareils choisis (tous si aucun), hors valeurs par défaut, entre dans le programmeur. |
| IA de conception (GEN-133) | Projet → Importer des scènes… (ajout seul, catégorie « Proposé par IA ») ; Projet → Relire les scènes et palettes. |

## 9. Notes de réalisation (P6)

| Sujet | Réalisation |
|---|---|
| Données | `SceneStep.Effects` (`SceneEffect`) et `SceneStep.HueFade` dans `scènes.json` (champs facultatifs, format 1 inchangé) ; bibliothèque `effets.json` (EFF-007) ; thèmes = palettes `theme` (PAL-010). Doc 50 §10, §11, §12d-ter. |
| Compilation | `EffectCompiler` : membres ordonnés, retard de phase par membre, degrés → course du modèle, couleurs traduites par appareil en **tables** (une par canal : RVB, RVBW, roue), problèmes signalés (`valider`) et effet écarté s'il ne pilote rien. |
| Écran | Panneau **Effets** de l'écran Contrôle (ERG-029) : bibliothèque, effets de l'étape, dessin animé, molettes (ERG-028). On écrit en ÉDITION / AVEUGLE ; l'étape est jouée par le moteur (CMD-017) pour voir l'effet tout de suite (EFF-006). L'ancien écran Scènes ne montre pas les effets (il les conserve). |
| Ordre des membres | Créé depuis le plan : de gauche à droite (colonnes d'un mètre), puis ordre du patch (appareils empilés sur un pied). |
| Assistants (SCN-014) | Panneau Propriétés, « Assistant : générer les étapes » : chenillard de couleurs, alternance de 2 couleurs, balayage de positions ; remplace les étapes, Ctrl+Z revient. |
| Réactivité musicale | Non traitée (P7) : un effet en temps suit le tempo fixe. |
