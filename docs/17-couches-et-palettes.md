# 17 – Couches et palettes

> Cahier des charges – modules **Couches** (préfixe `COU`) et **Palettes** (préfixe `PAL`). Phase principale : **P5** (palettes de base dès **P4**).
> Références : [15 §5 (fusion)](15-moteur-de-rendu.md), [16](16-scenes-et-effets.md), [13 (lieux)](13-installation-et-lieux.md).

---

## 1. Couches

### 1.1 Principe

Une **couche** regroupe des scènes **mutuellement exclusives** : lancer une scène coupe (par fondu croisé) celle qui jouait dans
la même couche. Les couches jouent **en parallèle** et se combinent selon leur **priorité** (doc 15 §5).
L'idée est d'organiser les scènes **par famille d'attributs** : une couche pour les couleurs, une pour les mouvements, une pour le strobe…
On combine ainsi librement « couleur × mouvement × strobe » avec peu de scènes.

### 1.2 Propriétés d'une couche

| Propriété | Valeurs | Défaut |
|---|---|---|
| Nom, couleur, icône | — | — |
| **Priorité** | Entier ; ordre d'empilement pour la fusion LTP | Ordre de création |
| **Exclusive** | Oui / non | Oui |
| **Master** | 0-100 % ; agit sur l'intensité, option « sur tous les attributs » (MOT-033) | 100 % |
| **Mode d'intensité** | HTP / prioritaire / additif / multiplicatif (doc 15 §5.2) | HTP |
| **Fondu croisé** | Durée par défaut des transitions entre scènes de la couche (s ou temps musicaux) | 0,5 s |
| **Type** | Normale / **Flash** (scènes actives tant que maintenues, priorité maximale) | Normale |
| Scènes | Liste ordonnée (l'ordre = ordre d'affichage en Live et sur l'APC mini) | — |
| Scène de repos | Scène jouée quand aucune autre ne l'est (facultatif) | aucune |

### 1.3 Modèle de couches par défaut (nouveau projet)

| Priorité | Couche | Mode d'intensité | Contenu type |
|---:|---|---|---|
| 1 | **Intensité** | HTP | Plein feu, 50 %, vagues, bump |
| 2 | **Couleurs** | HTP | Couleurs fixes, arcs-en-ciel, alternances |
| 3 | **Mouvements** | HTP | Positions, cercles, balayages (lyres, effet multi-têtes) |
| 4 | **Faisceau** | HTP | Gobos, prismes |
| 5 | **Effets** | HTP | Strobe, programmes internes |
| 6 | **Ambiance** | HTP | UV, fumée |
| 7 | **Libre** | HTP | Ce qui n'entre dans aucune autre couche : un appareil piloté à part, un essai (sans famille attendue ; ERG-008, 2026-09-27 : donne un rôle au 8e fader de l'APC) |
| 99 | **Flashs** | Prioritaire | Flash blanc, strobe flash, blackout partiel |

### 1.4 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| COU-001 | I | P5 | Créer, renommer, réordonner (= priorités), supprimer des couches ; propriétés du §1.2. | — |
| COU-002 | I | P5 | Une scène appartient à **une** couche ; on peut la déplacer ou la dupliquer vers une autre couche. | — |
| COU-003 | I | P5 | Exclusivité : lancer une scène coupe la précédente de la même couche avec le fondu croisé de la couche (ou celui passé dans la commande). | Test moteur MOT-030. |
| COU-004 | I | P5 | Couche non exclusive : plusieurs scènes simultanées, fusionnées entre elles. | — |
| COU-005 | I | P5 | Couche de type **Flash** : ses scènes sont actives tant que la commande est maintenue (souris, touche, pad). | — |
| COU-006 | I | P5 | Nouveau projet → modèle de couches par défaut du §1.3 (modifiable). | — |
| COU-007 | M | P5 | **Arrêter la couche** (CMD-012) avec fondu ; **arrêter tout** (toutes les couches sauf Ambiance, réglable). | — |
| COU-008 | M | P5 | Avertissement (non bloquant) si une scène touche des attributs « hors famille » de sa couche (ex. une scène de la couche Couleurs qui touche le Pan) : aide à garder une organisation propre. | — |
| COU-009 | S | P5 | Scène de repos par couche. | — |

## 2. Palettes

### 2.1 Types de palettes

| Type | Contenu | Portée |
|---|---|---|
| **Couleur** | Une couleur « intention » (logique) + émetteurs spéciaux éventuels (UV, ambre) ; valeurs spécifiques par appareil facultatives | Projet |
| **Position** | Pan/Tilt **par lyre** (et par tête d'effet) | **Lieu** (INST-054) |
| **Faisceau** | Gobo, rotation, prisme, focus, zoom, par modèle | Projet |
| **Intensité** | Niveau nommé (100 %, 50 %, veilleuse…) | Projet |
| **Automatique** | Générée depuis les plages de la bibliothèque (« Strobe lent », « Gobo 3 », « Macro : fondu ») | Modèle d'appareil |

### 2.2 Palettes couleur « par intention »

Une palette couleur est définie **une fois** et fonctionne sur tous les appareils capables de la reproduire :

| Appareil | Traduction de la palette « Ambre » |
|---|---|
| PAR RGB | R = 100 %, G = 50 %, B = 0 |
| PAR RGBW | idem + extraction du blanc (MOT-051) |
| Lyre à roue | Emplacement le plus proche (MOT-052) |
| UV | Ignorée (aucun émetteur compatible) |

L'utilisateur peut **affiner** la valeur pour un modèle précis (ex. l'ambre des PAR RGBW rend mieux avec un peu de blanc) :
la valeur spécifique prime sur la traduction automatique.

### 2.3 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| PAL-001 | I | P4 | Créer une palette depuis le programmeur (« Enregistrer comme palette ») pour les types couleur, position, faisceau, intensité. | — |
| PAL-002 | I | P4 | Palettes couleur par intention (§2.2), avec valeurs spécifiques par modèle facultatives. | Palette « Rouge » → PAR, lyres, barres corrects. |
| PAL-003 | I | P4 | Palettes **automatiques** issues des plages marquées « palette automatique » (BIB) : disponibles sans action de l'utilisateur. | Boutons « Strobe lent / rapide » du PAR. |
| PAL-004 | I | P5 | Palettes de **position par lieu** : changer de lieu actif change les valeurs Pan/Tilt de toutes les scènes qui utilisent ces palettes. | Même scène, deux lieux → positions différentes. |
| PAL-005 | I | P4 | Les scènes **référencent** les palettes (SCN-008) : modifier une palette met à jour immédiatement toutes les scènes (et la sortie si elles jouent). | Modifier « Piste centre » pendant une scène → lyres se déplacent. |
| PAL-006 | I | P4 | Suppression d'une palette utilisée : rapport des utilisations ; option « figer les valeurs dans les scènes » avant suppression. | — |
| PAL-007 | M | P4 | Palettes organisées en grilles par type, avec couleur et nom, réordonnables ; mêmes grilles en Live. | — |
| PAL-008 | M | P5 | Palettes de position **manquantes** dans un lieu (jamais calibrées) signalées ; valeur de repli = valeur du lieu « Générique ». | Nouveau lieu → alerte + repli. |
| PAL-009 | M | P4 | Jeu de palettes couleur livré par défaut : blanc, blanc chaud, rouge, orange, ambre, jaune, vert, cyan, bleu, lavande, magenta, rose, UV. | — |
| PAL-010 | S | P5 | Palettes de **combinaisons de couleurs** (thèmes) : « Latino » = {jaune, orange, rouge}, « Froid » = {bleu, cyan, blanc} — utilisables par les effets d'alternance et par le Directeur (doc 22). | Effet alternance sur un thème. |

## 3. Tests

| Test | Type | Contenu |
|---|---|---|
| T-COU-01 | Unitaire | Exclusivité, non-exclusivité, couche Flash, arrêt de couche, priorités. |
| T-PAL-01 | Unitaire | Traduction d'une palette couleur sur RGB, RGBW, roue ; valeur spécifique prioritaire. |
| T-PAL-02 | Unitaire | Palette de position par lieu ; repli sur « Générique ». |
| T-PAL-03 | Unitaire | Modification de palette → scènes mises à jour ; suppression avec figeage. |

## 4. Notes de réalisation (P4)

| Sujet | Réalisation |
|---|---|
| Couches (D28, écart) | Le modèle par défaut du §1.3 existe dès P4 dans les données (identifiants fixes, `couches.json`) pour que la fusion serve ; **COU-006 est donc avancé en partie** (données seulement) ; l'éditeur (COU-001) reste en P5. Le type « Flash » (COU-005) n'est pas encore traité : la couche Flashs est une couche normale en mode d'intensité prioritaire. |
| Palettes par défaut (PAL-009) | 13 couleurs + 4 intensités (doc 41 §5), identifiants fixes. |
| Traduction (PAL-002, MOT-050 à 053) | RVB direct ; RVBW selon `whiteMode` du modèle (défaut : blanc = min(R,V,B), retiré) ; roue : emplacement le plus proche en CIE Lab, demi-couleurs exclues, « ouvert » = blanc ; UV et ambre seulement si la couleur les précise. Valeurs par modèle ou par appareil prioritaires. |
| Palettes de position | En P4 par appareil, pour tout le projet ; par lieu avec repli sur « Générique » en P5 (PAL-004, PAL-008). |
| PAL-003 | Boutons générés depuis les plages marquées « palette automatique » ; aucune plage du parc n'est encore marquée. |
| PAL-006 | Suppression d'une palette utilisée : liste des étapes concernées, puis figeage des valeurs (couleur logique ou niveau quand c'est possible, sinon valeurs par appareil) avant suppression. |
| PAL-007 (partiel) | Grilles par type, déplacement ◀ ▶ (clic droit) ; les grilles du Live viennent en P5. |

## 5. Notes de réalisation (P5)

| Sujet | Réalisation |
|---|---|
| Éditeur de couches (COU-001) | Fenêtre « Couches… » depuis l'écran Scènes (pas de nouveau panneau, Q32) ; l'ordre de la liste donne les priorités ; une couche qui contient des scènes ne se supprime pas. |
| Propriétés ajoutées | `kind` (normale / Flash), `keepOnStopAll` (Ambiance protégée par défaut), `restSceneId`, `families` (familles attendues, avertissement COU-008 dans `valider`). Familles par défaut : Couleurs et Ambiance acceptent aussi l'intensité, Effets accepte faisceau, programmes, mouvement, intensité, couleur ; Flashs : tout. |
| Couche Flash (COU-005) | Ses scènes jouent en flash (appui / relâche) en Live et sur l'APC mini ; les boutons FLASH / STROBE jouent une scène de cette couche (`live.json`). |
| Positions par lieu (PAL-004, PAL-008, INST-054) | `venueId` sur les valeurs de palette ; sans lieu = « Générique » et valeur de repli, signalée pour les appareils non calibrés du lieu actif. La mise à jour depuis le programmeur ne touche que les appareils sélectionnés dans le lieu actif (avant P5 : toute la palette). |
| Thèmes (PAL-010) | Reportés en P6 avec les effets d'alternance qui les utilisent. |
| Plein feu (MOT-042) | Contenu d'un nouveau projet : scène « Plein feu » dans la couche Intensité (identifiant fixe). |
