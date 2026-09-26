# 13 – Installation, sélections et lieux

> Cahier des charges – module **Installation / Lieux** (préfixe `INST`). Phase principale : **P3**.
> Références : [02](02-principes-et-architecture-fonctionnelle.md), [10](10-sortie-dmx-et-firmware.md), [12](12-bibliotheque-appareils.md), [14](14-simulateur.md).

---

## 1. Rôle et découpage

| Notion | Contient | Change… |
|---|---|---|
| **Installation** (patch) | Univers, appareils patchés (modèle, mode, univers, adresse, nom), sélections | Rarement : quand on achète / modifie du matériel |
| **Lieu** | Plan de la salle, position et orientation de chaque appareil, appareils absents, zones interdites, palettes de position | À chaque salle |

L'installation décrit **votre kit** ; le lieu décrit **comment il est posé ce soir**. Les scènes ne dépendent que de l'installation
(et des palettes) : on change de salle sans retoucher aux scènes.

Un projet contient **une** installation et **un ou plusieurs** lieux, dont un **lieu actif**.

## 2. Univers

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| INST-001 | I | P3 | L'installation comporte **un ou plusieurs univers** (numérotés, nommables). Le parc actuel n'en utilise qu'un. | Créer 2 univers. |
| INST-002 | I | P3 | Le lien univers → pilotes de sortie relève des préférences du poste (SORT-006) ; l'installation ne contient que les univers. | — |
| INST-003 | I | P3 | Vue **barre d'univers** : 512 cases, colorées par appareil, avec nom au survol ; canaux libres visibles. | Revue. |

## 3. Patch des appareils

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| INST-010 | I | P3 | Ajouter un appareil : choix du modèle (depuis la bibliothèque), du mode, de l'univers, de l'adresse, d'un nom. | Patcher un PAR. |
| INST-011 | I | P3 | Ajout **multiple** : N appareils identiques, adresses consécutives (ou avec écart réglable), noms numérotés automatiquement (« PAR 1 » … « PAR 4 »). | 4 × PAR 7CH depuis 1 → 1, 8, 15, 22. |
| INST-012 | I | P3 | Proposition automatique de la **première adresse libre** suffisante pour le mode choisi. | — |
| INST-013 | I | P3 | **Détection des chevauchements** en temps réel (erreur visible dans la liste et la barre d'univers). | Chevauchement signalé immédiatement. |
| INST-014 | M | P3 | **Doublon volontaire** : deux appareils peuvent partager la même adresse s'ils sont déclarés « jumeaux » (même modèle et mode) ; ils reçoivent alors les mêmes valeurs (utile si un appareil est chaîné en esclave). | Deux PAR jumeaux → pas d'erreur, pilotés ensemble. |
| INST-015 | I | P3 | Modifier l'adresse ou l'univers d'un appareil (saisie ou glisser dans la barre d'univers) sans impact sur les scènes. | Déplacer un PAR → scènes intactes, nouvelle adresse émise. |
| INST-016 | I | P3 | **Changer le mode** d'un appareil patché : rapport des attributs perdus / gagnés, et des scènes impactées ; confirmation requise. | Passer un PAR de 7CH à 3CH → rapport « Strobe, Programme perdus ; 3 scènes impactées ». |
| INST-017 | I | P3 | Chaque appareil a un nom, une couleur d'affichage, et un **numéro court** (1, 2, 3…) utilisé dans les listes et au simulateur. | — |
| INST-018 | I | P3 | **Fiche d'installation** affichable et imprimable : pour chaque appareil, nom, modèle, **réglage à faire sur l'appareil** (ex. « A008 » / « 9CH, d017 »), univers, câblage suggéré dans l'ordre de la chaîne. | Fiche lisible en PDF / impression. |
| INST-019 | I | P3 | Bouton **Identifier** par appareil (CMD-023) et **Identifier tout à la suite** (chenillard d'identification, un appareil à la fois, avancée manuelle ou automatique). | Chenillard d'identification sur le parc. |
| INST-020 | M | P3 | Supprimer un appareil : rapport des scènes, palettes et sélections impactées, confirmation. | — |
| INST-021 | M | P3 | Options par appareil : **inverser Pan**, **inverser Tilt**, **échanger Pan/Tilt**, décalage de Pan (°), bornes Pan/Tilt personnelles. | Lyre montée à l'envers → mouvements cohérents avec l'autre lyre. |

## 4. Sélections (groupes d'appareils)

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| INST-030 | I | P3 | Une **sélection** est une liste **ordonnée** d'appareils (ou de cellules d'appareils), nommée et colorée. L'ordre sert aux chenillards et aux décalages de phase. | Sélection « PAR gauche → droite ». |
| INST-031 | I | P3 | Sélections **automatiques** : tous les appareils ; par modèle ; par catégorie (tous les PAR, toutes les lyres…). Elles se mettent à jour seules. | Ajouter un PAR → présent dans « Tous les PAR ». |
| INST-032 | I | P3 | Sélections **manuelles**, créées par sélection au plan ou dans la liste ; réordonnables par glisser. | — |
| INST-033 | M | P3 | Opérations de sélection : inverser l'ordre, pair / impair, moitié gauche / droite, **ordre selon la position au plan** (gauche→droite, avant→arrière, du centre vers l'extérieur). | Sélection triée par position. |
| INST-034 | M | P3 | Une sélection peut contenir des **cellules** (ex. les 8 segments de deux barres = 16 cellules ordonnées). | Chenillard sur 16 segments. |

## 5. Lieux

### 5.1 Contenu d'un lieu

| Élément | Description |
|---|---|
| Plan | Dimensions de la salle (m), éventuellement image de fond (photo, plan), repères : piste de danse, scène, public, boule à facettes… |
| Position des appareils | x, y (au sol), hauteur, orientation (angle), montage (posé / suspendu) |
| Présence | Appareils **absents ce soir** (non emportés / en panne) |
| Zones interdites | Par lyre : zones Pan/Tilt à ne jamais viser (public, yeux) |
| Palettes de position | Valeurs Pan/Tilt de chaque lyre pour chaque palette de position (doc 17) |

### 5.2 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| INST-050 | I | P3 | Créer, **dupliquer**, renommer, supprimer un lieu ; choisir le **lieu actif**. Un lieu « Générique » existe par défaut. | Dupliquer « Salle X » en « Salle Y ». |
| INST-051 | I | P3 | Éditeur de plan : placer les appareils par glisser-déposer, les orienter, régler leur hauteur et leur montage ; aligner / répartir. | Placer tout le parc en < 5 min. |
| INST-052 | I | P3 | Marquer un appareil **absent** : il n'est plus émis (ses canaux restent à 0), n'apparaît plus au simulateur ni dans les sélections actives, et les scènes l'ignorent sans erreur. | Lyre absente → aucune erreur, show inchangé pour les autres. |
| INST-053 | I | P5 | **Zones interdites** par lyre, définies en visant à la main (pad Pan/Tilt) les limites à ne pas franchir ; appliquées par le moteur (GEN-085). | Scène visant le public → bornée. |
| INST-054 | I | P5 | Les palettes de **position** sont stockées **par lieu** (doc 17) ; dupliquer un lieu copie ses palettes. | — |
| INST-055 | S | P11 | **Visée géométrique** : à partir de la position, hauteur et orientation d'une lyre, calculer le Pan/Tilt pour viser un point du plan (clic au sol). Nécessite un calage (2 points visés à la main). | Clic au centre de la piste → les deux lyres y convergent (± 30 cm). |

## 6. Assistant d'installation sur site

Parcours guidé, accessible depuis l'accueil et le Live, pour être prêt en **moins de 15 minutes** côté logiciel.

| Étape | Contenu | Exigence |
|---|---|---|
| 1 | Choisir le lieu (existant, ou dupliquer un lieu proche) | INST-070 |
| 2 | Cocher les appareils **présents** ce soir | INST-070 |
| 3 | Vérifier la sortie (Arduino détecté, voyant vert) | INST-070 |
| 4 | Afficher la **fiche d'installation** (réglages d'adresse à faire) | INST-018 |
| 5 | **Test appareil par appareil** : chaque appareil s'allume en blanc puis en couleurs, bouge (lyres), l'utilisateur valide d'un clic ou signale un problème | INST-071 |
| 6 | **Calibrer les palettes de position** des lyres : pour chaque palette de position du projet, viser à la main et enregistrer | INST-072 |
| 7 | Définir les **zones interdites** | INST-053 |
| 8 | Placer rapidement les appareils sur le plan (facultatif) | INST-051 |
| 9 | Passer en Live | — |

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| INST-070 | M | P5 | L'assistant enchaîne les étapes ci-dessus, chacune pouvant être passée. | Parcours complet sur le parc réel. |
| INST-071 | M | P5 | Test appareil par appareil avec résultat (OK / problème + note) consigné dans le journal. | Journal contient le compte rendu. |
| INST-072 | M | P5 | Calibration des positions : pour chaque palette de position, pad Pan/Tilt par lyre, **les deux lyres pouvant être visées ensemble** (miroir optionnel). | Calibrer 4 positions en < 3 min. |

## 7. Tests

| Test | Type | Contenu |
|---|---|---|
| T-INST-01 | Unitaire | Adresse libre, chevauchements, jumeaux, ajout multiple. |
| T-INST-02 | Unitaire | Changement de mode : rapport d'impacts exact. |
| T-INST-03 | Unitaire | Sélections automatiques et tri par position. |
| T-INST-04 | Intégration | Appareil absent → canaux à 0, scènes jouées sans erreur. |
| T-INST-05 | Intégration | Inversion Pan/Tilt : même palette → mouvements symétriques attendus. |
| T-INST-06 | Manuel | Assistant d'installation chronométré sur le parc réel (< 15 min). |

## 8. Notes de réalisation (P3)

> Écarts et précisions constatés au développement (doc 40 §6). Détail complet dans les fiches d'exigences.

| Sujet | Réalisation |
|---|---|
| Projet `Dmx.Patch` | Installation, sélections et lieux dans un nouveau projet domaine (Core, Persistence, Fixtures), doc 00 §7.2. |
| Sélections automatiques | Jamais enregistrées : recalculées à la volée à partir du patch courant (D24), toujours cohérentes. |
| GEN-053 | Copie des modèles dans `<projet>/Bibliothèque/`, même format et même code que la bibliothèque partagée (`Dmx.Fixtures.FixtureLibrary`) ; les génériques de l'application n'y sont jamais copiés. |
| INST-016 / GEN-053 | Rapport d'impact (canaux perdus / gagnés) commun au changement de mode et à la mise à jour depuis la bibliothèque ; le volet « scènes impactées » attend P4. |
| INST-021 | Modèle et décodeur (options de montage) prêts et utilisés par le simulateur ; pas encore d'éditeur dans l'écran (INST-021). |
| INST-034 | Sélection de cellules individuelles (segments de barre) reportée après P6 (effets par cellule) ; les barres se pilotent en entier dès P3. |
| INST-051 | Positionnement par champs numériques (X, Y en mètres), pas de glisser-déposer sur un plan visuel. |
| Identification (CMD-023) | Réalisée par des surcharges de canaux minutées côté interface (comme la découverte BIB-062), pas par une commande moteur dédiée : voir la fiche CMD-023 pour la discussion complète. |
| Écran | `Dmx.UI.Modules.Installation` : onglets Univers et patch, Sélections, Lieux, Fiche d'installation. |
