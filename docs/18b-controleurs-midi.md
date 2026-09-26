# 18b – Contrôleurs MIDI (AKAI APC mini MK1 et MK2)

> Cahier des charges – module **MIDI** (préfixe `MIDI`). Phase principale : **P5**.
> Références : [02 §12](02-principes-et-architecture-fonctionnelle.md), [18](18-live.md). Notices : `docs/Equipements/AKAI/`.

---

## 1. Rôle

Traduire les contrôleurs physiques en **commandes** (GEN-070) et renvoyer un **retour lumineux** (LED des pads) reflétant l'état
du moteur. Rend le Live beaucoup plus confortable qu'à la souris.

## 2. Matériel

| Élément | APC mini MK1 | APC mini MK2 |
|---|---|---|
| Grille 8 × 8 pads | LED 3 couleurs (vert, rouge, jaune) + clignotement | LED **RGB** (couleur + luminosité / clignotement / pulsation) |
| Faders | 9 (8 + 1 master) | 9 (8 + 1 master) |
| Boutons ronds bas | 8 (LED rouge) | 8 (LED rouge) |
| Boutons ronds droite | 8 (LED verte) | 8 (LED verte) |
| Shift | 1 | 1 |

> Les numéros de notes / contrôleurs et les valeurs de couleur des LED sont différents entre MK1 et MK2 ; ils seront relevés
> dans les notices présentes (`docs/Equipements/AKAI`) et stockés dans un **profil** par modèle (fichier de données, pas de code).

## 3. Affectation par défaut

```
          Col 1      Col 2      Col 3       Col 4     Col 5    Col 6     Col 7    Col 8        Boutons droite
 Ligne 1  Intensité  Couleurs   Mouvements  Faisceau  Effets   Ambiance  Flashs   (libre)      [Blackout]
 Ligne 2  scène 1    scène 1    scène 1     …                                                   [Flash]
 …        …                                                                                     [Strobe]
 Ligne 8  scène 8    …                                                                          [Fumée]
                                                                                                [Tap]
 Boutons bas : [Stop couche 1] … [Stop couche 8]                                                [Figer]
 Faders 1-8 : masters des couches 1-8          Fader 9 : Grand Master                          [Auto]
 Shift + boutons bas : page de scènes suivante / précédente (au-delà de 8 scènes par couche)    [Show : transition]
```

- **Colonnes** = couches (dans l'ordre du Live), **lignes** = scènes visibles de chaque couche (ligne 1 = première scène).
- Au-delà de 8 couches ou 8 scènes : pages (Shift + boutons).

## 4. Retour lumineux

| État du pad | MK2 (RGB) | MK1 (3 couleurs) |
|---|---|---|
| Emplacement vide | éteint | éteint |
| Scène disponible | **couleur de la scène**, faible luminosité | jaune |
| Scène active | couleur de la scène, pleine luminosité | vert |
| Scène active, en fondu d'entrée | pulsation | vert clignotant |
| Scène en attente de quantification | clignotement | jaune clignotant |
| Couche en manuel (auto actif) | bouton stop de la couche allumé | idem |

## 5. Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| MIDI-001 | I | P5 | Détection automatique des APC mini MK1 et MK2 (nom du périphérique MIDI) et chargement du profil correspondant (GEN-072). | Brancher chaque modèle → reconnu. |
| MIDI-002 | I | P5 | Affectation par défaut du §3 ; tous les messages traduits en commandes avec l'origine `MIDI`. | Journal : origine MIDI. |
| MIDI-003 | I | P5 | Retour lumineux du §4, mis à jour à chaque changement d'état (latence < 100 ms), y compris pour les actions venant de la souris ou du Directeur. | Lancer une scène à la souris → pad allumé. |
| MIDI-004 | I | P5 | **Reprise douce des faders** : un fader physique ne prend le contrôle d'un master que lorsqu'il **croise** la valeur courante (pas de saut brutal). | Master à 100 % à l'écran, fader physique à 0 → aucun effet jusqu'au croisement. |
| MIDI-005 | I | P5 | Les deux contrôleurs peuvent être branchés **simultanément**, avec des affectations différentes (ex. MK2 = couches, MK1 = palettes rapides et actions). | Test avec les deux. |
| MIDI-006 | I | P5 | Débranchement / rebranchement à chaud (GEN-073) ; le retour lumineux est restauré au rebranchement. | Test. |
| MIDI-007 | M | P5 | Affectations modifiables et enregistrées dans le projet : chaque pad / bouton / fader → une commande avec paramètres. | — |
| MIDI-008 | S | P5 | **Apprentissage** : cliquer une cible à l'écran puis toucher le contrôle physique (GEN-074). | — |
| MIDI-009 | S | P5 | Disposition alternative « palettes » : grille = palettes couleur / position pour une sélection. | — |
| MIDI-010 | M | P5 | Sur MK2, la couleur des pads reprend la **couleur des scènes** (GEN-106), approchée dans la palette de couleurs du contrôleur. | — |

## 6. Tests

| Test | Type | Contenu |
|---|---|---|
| T-MIDI-01 | Unitaire | Traduction message → commande pour chaque profil. |
| T-MIDI-02 | Unitaire | Reprise douce des faders. |
| T-MIDI-03 | Unitaire | Calcul des LED selon l'état (les deux profils). |
| T-MIDI-04 | Manuel | Les deux contrôleurs branchés, 30 min de Live, débranchements à chaud. |

## 7. Notes de réalisation (P5)

| Sujet | Réalisation |
|---|---|
| Module (D30) | Projet `Luxia.Midi` : API Windows `winmm` appelée directement (aucune dépendance, Q31), profils en fichiers de données (`src/Luxia.Midi/Profiles/*.json`). Le module ne connaît pas le projet : l'hôte lui fournit la disposition du Live (même règle `LiveRules`). |
| **Écart** : numéros MIDI | Les notices du dépôt ne contiennent pas la table MIDI (PDF du MK2 : 2 pages). Numéros repris des protocoles publiés par AKAI : grille 0-63 (0 en bas à gauche) ; MK1 bas 64-71, droite 82-89, Shift 98, LED 1 vert / 3 rouge / 5 jaune (+1 = clignotant) ; MK2 bas 100-107, droite 112-119, Shift 122, LED par canal (luminosité, pulsation) et palette de couleurs ; faders CC 48-56. **À confirmer sur l'appareil** avec `luxia-headless midi`. |
| Affectation | Conforme au §3 ; précisions : Shift + bas 1 / 2 = page de scènes précédente / suivante, Shift + bas 3 / 4 = couches ; boutons de droite 5 (Tap) et 7 (Auto) sans effet jusqu'à P7 / P10 ; le 8e fait « Tout arrêter » en attendant la transition de show (P8). |
| Retour lumineux | Recalculé toutes les 40 ms d'après l'état du moteur, seuls les changements envoyés ; boutons stop allumés quand la couche joue ; LED éteintes à la fermeture. MK2 : couleur de la scène approchée dans une palette de couleurs franches. |
| Affectations (MIDI-007) | `midi.json` (doc 50 §12d), par modèle possible (MIDI-005) ; apprentissage (MIDI-008) et disposition « palettes » (MIDI-009) reportés (Q32). |
