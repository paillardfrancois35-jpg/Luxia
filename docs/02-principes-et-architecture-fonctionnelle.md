# 02 – Principes transverses et architecture fonctionnelle

> Cahier des charges – document transverse. Il s'applique à **tous** les modules.
> Prérequis : [00-analyse-et-decoupage.md](00-analyse-et-decoupage.md), [01-questions-ouvertes.md](01-questions-ouvertes.md), [glossaire.md](glossaire.md).
> Les documents de modules (10 à 23) détaillent chaque module ; ils ne doivent pas contredire celui-ci. En cas de conflit, ce document fait foi jusqu'à sa révision.

---

## Sommaire

1. [Objet et périmètre](#1-objet-et-périmètre)
2. [Conventions de rédaction des exigences](#2-conventions-de-rédaction-des-exigences)
3. [Contexte et contraintes](#3-contexte-et-contraintes)
4. [Principes directeurs](#4-principes-directeurs)
5. [Architecture fonctionnelle : les modules](#5-architecture-fonctionnelle--les-modules)
6. [Échanges entre modules : commandes, événements, états](#6-échanges-entre-modules--commandes-événements-états)
7. [Représentation des grandeurs (unités)](#7-représentation-des-grandeurs-unités)
8. [Temps et horloges](#8-temps-et-horloges)
9. [Chaîne de rendu et ordre des priorités](#9-chaîne-de-rendu-et-ordre-des-priorités)
10. [Données : organisation, fichiers, références](#10-données--organisation-fichiers-références)
11. [Modes de fonctionnement de l'application](#11-modes-de-fonctionnement-de-lapplication)
12. [Entrées de contrôle](#12-entrées-de-contrôle)
13. [Sûreté](#13-sûreté)
14. [Robustesse et performances](#14-robustesse-et-performances)
15. [Ergonomie transverse](#15-ergonomie-transverse)
16. [Journalisation](#16-journalisation)
17. [Fonctionnement hors-ligne](#17-fonctionnement-hors-ligne)
17b. [Conception assistée par IA (préparation)](#17b-conception-assistée-par-ia-préparation)
18. [Hors périmètre](#18-hors-périmètre)
19. [Registre des décisions](#19-registre-des-décisions)

---

## 1. Objet et périmètre

Ce document définit :

- les **règles communes** à tous les modules (unités, temps, priorités, sûreté, données, ergonomie) ;
- l'**architecture fonctionnelle** : quels modules existent, ce dont chacun est responsable, et ce qu'ils s'échangent ;
- les **exigences transverses** (préfixe `GEN`).

Il ne traite pas de l'implémentation (choix de classes, conventions de code) : ce sera l'objet des règles de développement.

---

## 2. Conventions de rédaction des exigences

### 2.1 Identifiants

Chaque exigence porte un identifiant `PRÉFIXE-NNN` unique et **jamais réutilisé** (une exigence supprimée garde son numéro, marqué « supprimée »).

| Préfixe | Document |
|---|---|
| `GEN` | 02 – Principes transverses (ce document) |
| `SORT` | 10 – Sortie DMX et firmware |
| `CONS` | 11 – Console |
| `BIB` | 12 – Bibliothèque d'appareils |
| `INST` | 13 – Installation et lieux |
| `SIM` | 14 – Simulateur |
| `MOT` | 15 – Moteur de rendu |
| `SCN` / `EFF` | 16 – Scènes / Effets |
| `COU` / `PAL` | 17 – Couches / Palettes |
| `LIVE` | 18 – Écran Live |
| `MIDI` | 18b – Contrôleurs MIDI (APC mini) |
| `AUD` | 19 – Audio et tempo |
| `SHOW` | 20 – Show (séquenceur) et séquences |
| `MUS` | 21 – Lecture en cours et style |
| `AUTO` | 22 – Directeur automatique |
| `TL` | 23 – Timeline par morceau |
| `CMD` / `EVT` | Catalogue des commandes / événements (ce document, §6) |

### 2.2 Priorités

| Priorité | Sens |
|---|---|
| **I** – Indispensable | Sans elle, le jalon associé n'est pas atteint. |
| **M** – Important | Attendue pour le jalon, mais un contournement temporaire est acceptable. |
| **S** – Souhaitable | Amélioration, peut glisser à une phase ultérieure. |

### 2.3 Forme d'une exigence

> **ID** · Priorité · Phase
> Énoncé (« Le système doit… »).
> *Justification* (si non évidente).
> ✔ *Critère d'acceptation* (vérifiable par un test).

Dans les tableaux, ces champs sont en colonnes.

---

## 3. Contexte et contraintes

| Contrainte | Valeur |
|---|---|
| Système | Windows 10/11, 64 bits, **uniquement** |
| Interface | **Avalonia**, **français** uniquement, **sans tactile** (souris, clavier, contrôleur MIDI) |
| Langage | C# / .NET (version LTS en vigueur au démarrage du développement) |
| Organisation du code | **Une solution**, plusieurs projets (cf. doc 00 §7) |
| Sortie DMX | Arduino Leonardo + shield Conceptinetics CTC-DRA-10-R2, USB série. D'autres sorties possibles (Art-Net, simulateur). |
| Musique | Jouée **sur le même PC** par des applications de streaming variées (Deezer, Spotify, YouTube Music…), pas de logiciel DJ |
| Réseau | **Aucun accès Internet requis en soirée** |
| Utilisateur | Une seule personne, technicien ; l'appli n'a pas de gestion de comptes |
| Parc actuel | 4 PAR RGB, 2 PAR RGBW, 2 lyres identiques, 2 UV, 2 barres LED, 1 effet multi-têtes, 1 machine à fumée (1 canal), soit ~100 canaux, **1 univers** |
| Contrôleurs | AKAI APC mini MK1 et MK2 |

---

## 4. Principes directeurs

Ces principes guident les arbitrages lorsqu'une exigence de module est ambiguë.

| ID | Principe | Conséquence |
|---|---|---|
| **P1** | **La sûreté d'abord.** | Blackout toujours accessible ; limites (strobe, fumée, zones interdites) appliquées en dernier, par le moteur, quelle que soit l'origine de la commande. |
| **P2** | **Le moteur ne dépend de rien d'autre que ses entrées.** | Aucun module d'interface, d'audio ou de sortie n'est requis pour que le moteur calcule une trame. Il peut tourner sans fenêtre. |
| **P3** | **Une seule porte d'entrée pour agir.** | Toute action sur la restitution (clic, touche, pad MIDI, Directeur, futur téléphone) passe par une **commande** du catalogue (§6). |
| **P4** | **On décrit des intentions, pas des octets.** | Scènes, palettes et effets manipulent des **attributs d'appareils**. La conversion en canaux DMX n'a lieu qu'en fin de chaîne. |
| **P5** | **Le simulateur est une sortie comme une autre.** | Il reçoit les mêmes trames que l'Arduino : ce qui est vu au simulateur est ce qui sera émis. |
| **P6** | **Déterminisme.** | Pour les mêmes entrées (commandes, signaux, temps), le moteur produit exactement les mêmes trames. Le temps est une entrée. |
| **P7** | **Hors-ligne par défaut.** | Aucune fonction utilisée en soirée ne dépend d'Internet. |
| **P8** | **Des données lisibles et durables.** | Fichiers JSON lisibles, versionnés par un numéro de format, migrés automatiquement. |
| **P9** | **Le Live ne pose jamais de question bloquante.** | Aucune boîte de dialogue modale en mode Live ; les erreurs s'affichent sans interrompre. |
| **P10** | **Livrable à chaque phase.** | Chaque phase produit une application utilisable (cf. doc 00 §13). |
| **P11** | **Chaque livraison se montre.** | Chaque phase enrichit le **show de référence** (doc 41), construit avec **les appareils réels du parc**, et livre un guide qui dit quoi regarder (doc 40 §7). |

---

## 5. Architecture fonctionnelle : les modules

### 5.1 Vue d'ensemble

```
 ┌──────────────────────────────────────────── PRÉPARATION (Atelier) ───────────────────────────────────────────┐
 │  Bibliothèque ──▶ Installation / Lieux ──▶ Palettes ──▶ Scènes & Effets ──▶ Couches ──▶ Séquences ──▶ Shows │
 │                                                                                             Réglages Directeur│
 └──────────────────────────────────────────────────────┬────────────────────────────────────────────────────────┘
                                                        │ (données du projet)
 ┌──────────────── ENTRÉES ────────────────┐            ▼                           ┌────────── SORTIES ──────────┐
 │ Souris / clavier (UI)                   │   ┌──────────────────┐   trames      │ Arduino (USB série)          │
 │ Contrôleurs MIDI (APC mini)             │──▶│  MOTEUR DE RENDU │──────────────▶│ Simulateur 2D / 3D           │
 │ Directeur automatique ◀── Style ◀── Lecture en cours           │               │ Art-Net (S)                  │
 │        ▲             ◀── Audio (tempo, impulsions, énergie)    │               │ Enregistreur (tests)         │
 │        └─ Séquenceur de Show        │   └────────┬─────────┘               │ Nulle                        │
 └─────────────────────────────────────────┘            │ état                   └──────────────────────────────┘
                                                        ▼
                                             Écran Live, Console, Moniteur, Journal
```

### 5.2 Fiche des modules

| Module | Responsabilité | Entrées | Sorties | Données possédées | Doc | Phase |
|---|---|---|---|---|---|---|
| **Sortie** | Émettre chaque univers vers son matériel/protocole ; superviser la liaison | Trames du moteur | Octets vers Arduino / réseau / simulateur ; `EVT` d'état de sortie | Configuration des sorties | 10 | P0 |
| **Console** | Réglage manuel canal par canal ou appareil par appareil ; moniteur de sortie | Actions utilisateur | Commandes de surcharge (`CMD`) | — | 11 | P1 |
| **Bibliothèque** | Décrire les modèles d'appareils (modes, attributs, plages) ; importer OFL/QLC+ | Fichiers, saisie | Modèles | Bibliothèque | 12 | P2 |
| **Installation / Lieux** | Patcher les appareils, gérer univers, sélections, lieux, calibrations | Bibliothèque, saisie | Patch, sélections, lieux | Installation, lieux | 13 | P3 |
| **Simulateur** | Représenter visuellement le résultat des trames | Trames + patch + lieu | Affichage | Disposition du plan (dans le lieu) | 14 | P3 |
| **Moteur de rendu** | Calculer à chaque tick les valeurs de tous les attributs puis la trame | Commandes, signaux musicaux, temps, projet | Trames, état observable, `EVT` | État d'exécution (non persistant, sauf reprise) | 15 | P4 |
| **Scènes & Effets** | Éditer scènes, étapes, effets générés | Programmeur, palettes | Scènes | Scènes, effets | 16 | P4 / P6 |
| **Couches & Palettes** | Organiser les scènes en couches ; définir les palettes | Saisie | Couches, palettes | Couches, palettes | 17 | P5 |
| **Live** | Jouer en soirée | Utilisateur, état moteur | Commandes | Disposition de l'écran Live | 18 | P5 |
| **MIDI** | Traduire les contrôleurs en commandes ; retour LED | Messages MIDI, état moteur | Commandes, messages MIDI | Affectations MIDI | 18b | P5 |
| **Audio** | Capturer le son du PC, produire tempo, impulsions, énergie | Flux audio WASAPI | Signaux musicaux (`EVT`) | Réglages d'analyse | 19 | P7 |
| **Show / Séquences** | Exécuter les graphes de show et les séquences en mesures | Commandes, signaux, horloge | Commandes vers le moteur | Shows, séquences | 20 | P8 |
| **Lecture en cours & Style** | Connaître le morceau joué et en déduire un style | API Windows, base musicale | `EVT` morceau / style | Base musicale, journal des titres | 21 | P9 |
| **Directeur** | Choisir et piloter les shows en automatique | Style, signaux, verrous | Commandes | Réglages du Directeur | 22 | P10 |
| **Timeline** | Caler des séquences sur un morceau précis | Position de lecture, horloge | Commandes | Timelines | 23 | P11 |
| **Persistance** | Lire, écrire, migrer, sauvegarder | — | — | Tous les fichiers | 02 §10 | P0→ |
| **Journal** | Tracer ce qui se passe (technique et soirée) | `EVT` | Fichiers journaux, affichage | Journaux | 02 §16 | P0→ |

### 5.3 Règles de dépendance fonctionnelle

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-001 | I | P0 | Le moteur de rendu doit pouvoir calculer des trames sans qu'aucun module d'interface, d'audio ou de sortie matérielle ne soit démarré. | Un test exécute un projet et produit des trames sans interface ni matériel. |
| GEN-002 | I | P0 | Aucun module ne doit modifier l'état de restitution autrement que par une commande du catalogue (§6). | Revue : aucune écriture directe dans l'état du moteur hors traitement de commande. |
| GEN-003 | I | P0 | Les modules d'édition (Atelier) ne doivent pas dépendre du module Live, et réciproquement. | Le Live fonctionne avec un projet chargé sans ouvrir aucun éditeur. |
| GEN-004 | M | P3 | Un composant d'édition (ex. faders d'appareil) doit pouvoir être intégré dans un autre écran (ex. éditeur de bibliothèque pour le test en direct). | Le test en direct de la bibliothèque réutilise le composant de la console. |

---

## 6. Échanges entre modules : commandes, événements, états

### 6.1 Principes

- Une **commande** exprime une **intention** envoyée au moteur (ou à un module) : elle peut être refusée (ex. sécurité) ; le refus est signalé.
- Un **événement** exprime un **fait passé** publié par un module ; plusieurs modules peuvent s'y abonner.
- Un **état observable** est un instantané consultable (valeurs courantes, scènes actives…), rafraîchi au rythme du moteur ; l'interface l'affiche à son propre rythme.
- Toute commande porte son **origine** : `Utilisateur`, `MIDI`, `Directeur`, `Show`, `Timeline`, `Distant` (futur). L'origine sert à la journalisation et aux règles de priorité entre manuel et automatique (doc 22).

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-010 | I | P4 | Toute commande doit être horodatée, porter son origine et être traitée au plus tard au tick suivant sa réception. | Test : commande reçue entre deux ticks → effet visible dans la trame du tick suivant. |
| GEN-011 | I | P4 | Les commandes reçues pendant un même tick doivent être appliquées dans leur ordre d'arrivée. | Test : `LancerScène A` puis `LancerScène B` dans la même couche → B active. |
| GEN-012 | M | P4 | Une commande refusée doit produire un événement `CommandeRefusée` avec le motif. | Test : flash strobe au-delà de la limite de sécurité → refus journalisé. |
| GEN-013 | I | P4 | La publication des événements et de l'état ne doit jamais bloquer ni retarder le tick du moteur. | Un abonné volontairement lent n'altère pas la cadence (gigue < 5 ms). |

### 6.2 Catalogue des commandes (version initiale)

> Le catalogue s'enrichira dans les documents de modules ; chaque ajout est référencé ici.

| ID | Commande | Paramètres principaux | Module cible | Phase |
|---|---|---|---|---|
| CMD-001 | `Blackout` | actif / inactif | Moteur | P4 |
| CMD-002 | `RéglerGrandMaster` | niveau 0-1 | Moteur | P4 |
| CMD-003 | `Figer` | actif / inactif | Moteur | P5 |
| CMD-010 | `LancerScène` | scène, couche (optionnel si unique), temps de fondu (optionnel), solo, `StopIfPlaying` (bascule : arrête si la scène joue, tranché par le moteur — P5, LIVE-003), `Immediate` (interne : relance sans quantification, utilisée par le moteur à l'échéance d'un départ quantifié, MOT-018) | Moteur | P4 |
| CMD-011 | `ArrêterScène` | scène, temps de fondu (optionnel) | Moteur | P4 |
| CMD-012 | `ArrêterCouche` | couche, temps de fondu | Moteur | P5 |
| CMD-013 | `RéglerMasterCouche` | couche, niveau 0-1 | Moteur | P5 |
| CMD-014 | `FlashScène` | scène, appui / relâche | Moteur | P5 |
| CMD-015 | `ÉtapeSuivante` / `ÉtapePrécédente` | scène | Moteur | P4 |
| CMD-016 | `RéglerVitesseScène` | scène, multiplicateur | Moteur | P5 |
| CMD-017 | `MontrerÉtape` | scène, étape (ou aucune) : l'étape éditée est jouée, effets compris, au-dessus des couches et figée sur cette étape (aperçu des effets pendant le réglage, EFF-006) | Moteur | P6 |
| CMD-020 | `SurchargerCanal` | univers, canal, valeur | Moteur (console) | P1 |
| CMD-021 | `SurchargerAttribut` | appareil(s), attribut, valeur | Moteur (console / programmeur) | P4 |
| CMD-022 | `LibérerSurcharges` | tout / canal / appareil | Moteur | P1 |
| CMD-023 | `IdentifierAppareil` | appareil, actif / inactif | Moteur | P3 |
| CMD-024 | `TesterSortie` | actif / inactif, univers, plage de canaux, canaux exclus, valeur de test, durée par canal, une passe / en boucle, forme (chenillard / rampe) | Moteur (écran Sorties) | P0 |
| CMD-030 | `Fumée` | appui / relâche, ou rafale (durée) | Moteur | P5 |
| CMD-031 | `RéglerDimmerGroupe` | groupe, niveau 0-1 | Moteur | ERG2 |
| CMD-040 | `TapTempo` | — | Audio / Horloge | P7 |
| CMD-041 | `ChoisirSourceTempo` | audio / tap / fixe (+ BPM) | Horloge | P7 |
| CMD-042 | `AjusterTempo` | ×2, ÷2, ± valeur en BPM, « 1 ici » (le temps en cours devient le premier de la mesure), retour au tempo entendu (essai P8 : oublie la correction ×2 / ÷ 2, sinon gardée d'un morceau à l'autre) | Horloge | P7 |
| CMD-043 | `RégleLatenceTempo` | décalage en secondes (± 0,5) appliqué aux événements musicaux ; l'écoute le mémorise par périphérique | Horloge | P7 |
| CMD-050 | `LancerShow` / `ArrêterShow` | show (bascule possible) ; arrêt d'un show, ou de tous les shows et séquences (en gardant les shows secondaires pour « ■ Stop », qui épargne aussi les couches protégées) | Show | P8 |
| CMD-051 | `ForcerTransition` | show, transition | Show | P8 |
| CMD-052 | `LancerSéquence` / `ArrêterSéquence` | séquence | Show | P8 |
| CMD-053 | `SimulerMusique` | événement (drop, break, montée, silence, reprise, morceau changé), énergie simulée ou rendue à l'écoute, style simulé (mode simulation, SHOW-027 ; D38) | Moteur, Show | P8 |
| CMD-060 | `ModeAuto` | actif / inactif | Directeur | P10 |
| CMD-061 | `PoserVerrou` / `LeverVerrou` | type de verrou, valeur | Directeur | P10 |
| CMD-062 | `ForcerStyle` | style (ou « détection ») | Style / Directeur | P9 |

### 6.3 Catalogue des événements (version initiale)

| ID | Événement | Émetteur | Principaux abonnés | Phase |
|---|---|---|---|---|
| EVT-001 | `SortieConnectée` / `SortieDéconnectée` | Sortie | Live, Journal | P0 |
| EVT-002 | `TrameÉmise` (échantillonné) | Sortie | Moniteur, Simulateur | P0 |
| EVT-010 | `SceneDémarrée` / `ScèneArrêtée` / `ÉtapeChangée` | Moteur | Live, MIDI (LED), Show, Journal | P4 |
| EVT-011 | `CommandeRefusée` | Moteur | Live, Journal | P4 |
| EVT-012 | `LimiteSécuritéAtteinte` | Moteur | Live, Journal | P5 |
| EVT-020 | `Temps` (numéro de temps, numéro de mesure, BPM) | Horloge | Moteur, Show, Directeur | P7 |
| EVT-021 | `Impulsion` (bande : basses / aigus, force) | Audio | Moteur, Directeur | P7 |
| EVT-022 | `ÉnergieChangée` (niveau, tendance) | Audio | Directeur, Show, Live | P7 |
| EVT-023 | `Break` / `Drop` | Audio | Show, Directeur | P7 |
| EVT-024 | `TempoChangé` (BPM, confiance, source) | Horloge | Live, Directeur | P7 |
| EVT-030 | `ÉtapeShowActivée` | Show | Live, Journal | P8 |
| EVT-040 | `MorceauChangé` (titre, artiste, album) | Lecture en cours | Style, Directeur, Journal | P9 |
| EVT-041 | `LectureDémarrée` / `LectureEnPause` | Lecture en cours | Directeur | P9 |
| EVT-042 | `StyleDétecté` (style, confiance, méthode) | Style | Directeur, Live, Journal | P9 |
| EVT-050 | `DécisionDirecteur` (show choisi, motif) | Directeur | Live, Journal | P10 |

### 6.4 États observables (version initiale)

| État | Contenu | Rafraîchissement |
|---|---|---|
| Trame par univers | 512 octets | chaque tick |
| Valeurs par appareil | attributs logiques (avant conversion DMX) | chaque tick |
| Restitution | couches, scène active par couche, étape, progression, masters, blackout, figé | chaque tick |
| Surcharges | canaux / attributs surchargés par la console | à chaque changement |
| Musique | BPM, phase, énergie, morceau, style, source tempo | à chaque changement / 10 Hz |
| Sorties | état de connexion, trames/s, erreurs | 1 Hz |
| Automatique | mode auto, show et étape en cours, verrous, dernière décision | à chaque changement |

---

## 7. Représentation des grandeurs (unités)

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-020 | I | P2 | En interne, toute valeur d'attribut est une **valeur logique normalisée** entre 0 et 1, avec une résolution au moins équivalente à 16 bits. La conversion en 8 ou 16 bits DMX se fait uniquement à la sortie du moteur. | Test : 0,5 sur un attribut 8 bits → 128 ; sur 16 bits → 32768 (0x80/0x00). |
| GEN-021 | I | P2 | L'interface affiche les valeurs dans l'unité la plus parlante : **%** pour les intensités, **0-255** pour les canaux bruts, **degrés** pour Pan/Tilt (si l'amplitude est connue), **nom de plage** pour les canaux à plages. | Revue des écrans. |
| GEN-022 | I | P4 | Les couleurs sont manipulées comme des couleurs logiques (rouge, vert, bleu, + blanc/ambre/UV si utiles) et converties selon les émetteurs de chaque appareil (RGB, RGBW, roue de couleur). | Test : couleur « blanc chaud » → valeurs RGB sur PAR RGB, RGBW avec blanc sur PAR RGBW, emplacement le plus proche sur la roue d'une lyre. |
| GEN-023 | I | P4 | Les durées sont exprimées **soit en secondes** (résolution 1 ms), **soit en temps musicaux** (temps, fraction de temps, mesures). Le choix se fait par durée (étape, fondu, effet). | Test : étape de « 2 temps » à 120 BPM dure 1,000 s ; à 90 BPM 1,333 s. |
| GEN-024 | I | P7 | La mesure par défaut est à **4 temps**. | — |
| GEN-025 | S | P8 | Les mesures à **3 temps** (valse, musiques de bal) sont prises en charge par séquence et par show. | Test : séquence 3/4 → une mesure = 3 temps. |
| GEN-026 | I | P7 | Le tempo est exprimé en **BPM** (décimal, précision 0,1), borné par le moteur à **20-400 BPM** ; l'écoute explore une plage réglable à l'écran Audio (par défaut 70-180). | — |

---

## 8. Temps et horloges

### 8.1 Horloges

| Horloge | Rôle | Source |
|---|---|---|
| **Horloge temps réel** | Cadence le moteur ; mesure les durées en secondes | Horloge monotone haute précision du système (jamais l'heure murale) |
| **Horloge musicale** | Donne temps, mesures, phase et BPM | Audio (auto), tap tempo, ou BPM fixe (cf. doc 19) |
| **Heure murale** | Uniquement pour les journaux et l'affichage | Système |

### 8.2 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-030 | I | P0 | Le moteur est cadencé par un **tick** régulier, par défaut **40 Hz**, réglable de 25 à 44 Hz. | Mesure de 15 min (≈ 36 000 ticks) : fréquence moyenne 40 ± 0,5 Hz. *(1 h avant D23.)* |
| GEN-031 | I | P0 | La gigue du tick doit rester inférieure à 5 ms (99e centile) sur un PC standard, interface ouverte. | Mesure de 15 min, rapport de gigue (`luxia-headless gigue`). *(1 h avant D23.)* |
| GEN-032 | I | P4 | Les calculs du moteur utilisent le **temps écoulé réel** (et non le nombre de ticks) : un tick en retard ne ralentit pas les fondus. | Test : ticks irréguliers simulés → fondu de 2 s terminé à 2 s ± 1 tick. |
| GEN-033 | I | P4 | Toutes les horloges sont **injectables** pour les tests (temps virtuel). | Tests moteur exécutés en temps virtuel, 1 h simulée en quelques secondes. |
| GEN-034 | I | P7 | L'horloge musicale continue de battre au dernier tempo connu si le signal audio disparaît (break, silence) et se recale quand il revient. | Test sur fichier avec break de 8 s : pas d'arrêt des temps. |
| GEN-035 | M | P7 | Un **décalage de latence global** (± 250 ms) permet d'avancer ou de retarder tous les événements musicaux pour compenser la latence audio et celle des appareils. | Réglage visible ; test : décalage de −100 ms → événements émis 100 ms plus tôt que la détection brute. |

---

## 9. Chaîne de rendu et ordre des priorités

Cette chaîne est **la** référence pour résoudre tout conflit de valeurs. Le doc 15 la détaille.

```
 1. Valeurs par défaut des appareils (définies dans la bibliothèque)
 2. Couches, de la priorité la plus basse à la plus haute
      - intensité : HTP entre couches (par défaut)
      - autres attributs : LTP (la plus prioritaire ; à égalité, la dernière activée)
      - option par couche pour l'intensité : prioritaire (LTP) / additive / multiplicative (cf. doc 15 §5.2)
 3. Master de chaque couche (appliqué à sa contribution)
 4. Flashs (priorité au-dessus de toutes les couches, tant que maintenus)
 5. Surcharges du programmeur / de la console en mode « appareils » (attributs)
 6. Intensité virtuelle (appareils sans canal dimmer) puis Grand Master
 7. Blackout
 8. Figer (si actif, remplace le résultat 1-7 par le dernier résultat figé)
 9. Limites de sûreté (zones interdites Pan/Tilt, strobe, fumée)
10. Conversion attributs → canaux DMX (patch, 8/16 bits, inversions)
11. Surcharges de la console en mode « canaux » (octets bruts) – hors blackout et sûreté
12. Émission vers les sorties
```

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-040 | I | P4 | Le moteur applique la chaîne ci-dessus dans cet ordre, à chaque tick, pour chaque appareil. | Suite de tests « chaîne de rendu », un cas par étape. |
| GEN-041 | I | P4 | Le blackout et le Grand Master n'agissent que sur les attributs d'**intensité** (et l'intensité virtuelle), jamais sur la couleur ou la position. | Test : blackout puis relâche → couleurs et positions inchangées, sans mouvement parasite des lyres. |
| GEN-042 | I | P4 | Les surcharges brutes de la console (étape 11) restent soumises au blackout et aux limites de sûreté ; la **fumée** et le **strobe** ne sont jamais contournables. | Test : fumée surchargée à 255 + limite de durée → coupure à la limite. |
| GEN-043 | M | P4 | L'état de la chaîne doit être **explicable** : pour un appareil et un attribut donnés, l'interface peut indiquer quelle couche/scène/surcharge fournit la valeur. | Info-bulle ou panneau « d'où vient cette valeur ». |

---

## 10. Données : organisation, fichiers, références

### 10.1 Découpage des données

| Ensemble | Contenu | Portée | Emplacement par défaut |
|---|---|---|---|
| **Bibliothèque** | Modèles d'appareils (un fichier par modèle) | Partagée par tous les projets | `Documents\LuXia\Bibliothèque\` |
| **Projet** | Installation, lieux, palettes, scènes, effets, couches, séquences, shows, écran Live, affectations MIDI, réglages du Directeur | Un spectacle / une configuration de soirée | `Documents\LuXia\Projets\<nom>\` |
| **Base musicale** | Artistes → styles, titres → styles, alias, corrections, taxonomie | Partagée | `Documents\LuXia\Musique\` |
| **Journaux** | Journal technique, journal de soirée (titres joués, styles, décisions) | Par session | `Documents\LuXia\Journaux\` |
| **Préférences** | Dernier projet, sorties, réglages d'affichage, audio | Poste | `%AppData%\LuXia\` |

Un **Projet** est un **dossier** de fichiers JSON (plutôt qu'un fichier unique) afin que les différences soient lisibles avec Git et qu'un fichier corrompu n'emporte pas tout.

### 10.2 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-050 | I | P0 | Tous les fichiers de données sont en **JSON UTF-8 indenté**, lisibles et modifiables à la main. | Ouverture dans un éditeur de texte ; diff Git lisible. |
| GEN-051 | I | P0 | Chaque fichier porte un **numéro de version de format**. L'application migre automatiquement les anciennes versions et conserve une copie de l'original. | Test : fichier v1 chargé par une appli v2 → migré, copie `.v1.bak` présente. |
| GEN-052 | I | P2 | Les objets se référencent par un **identifiant stable** (généré à la création), jamais par leur nom ni par une position. Renommer un objet ne casse aucune référence. | Test : renommer une palette → scènes intactes. |
| GEN-053 | I | P3 | Le projet contient une **copie des modèles d'appareils** qu'il utilise. Une modification de la bibliothèque n'est répercutée dans le projet que sur action explicite, avec la liste des impacts. | Test : modifier un modèle dans la bibliothèque → projet inchangé ; « Mettre à jour » → rapport d'impacts. |
| GEN-054 | I | P5 | **Sauvegarde automatique** du projet ouvert (par défaut toutes les 2 min et à chaque passage Atelier → Live), en plus de la sauvegarde manuelle. | Tuer le processus → au redémarrage, proposition de restaurer la dernière sauvegarde automatique. |
| GEN-055 | M | P5 | Conservation des **N dernières versions** du projet (par défaut 10) pour revenir en arrière. | Liste des versions consultable et restaurable. |
| GEN-056 | I | P0 | Un fichier illisible ou incohérent ne doit jamais faire planter l'application : il est signalé, mis de côté, et le reste du projet se charge. | Test : fichier de scène corrompu → projet chargé sans cette scène, message clair. |
| GEN-057 | M | P5 | Export / import d'un projet complet sous forme d'archive unique (pour sauvegarde ou transfert). | Aller-retour export → import identique. |
| GEN-058 | S | P2 | Les chemins sont relatifs au dossier du projet ou de la bibliothèque (projet déplaçable). | Déplacer le dossier → projet toujours ouvrable. |

---

## 11. Modes de fonctionnement de l'application

### 11.1 Atelier et Live

| Mode | Usage | Caractéristiques |
|---|---|---|
| **Atelier** | Préparer : bibliothèque, installation, scènes, shows… | Tous les éditeurs ; sortie active ou **aveugle** ; annuler/rétablir |
| **Live** | Jouer en soirée | Écran unique optimisé ; aucune fenêtre modale ; édition interdite (sauf corrections rapides autorisées explicitement : palettes de position, verrous) |

### 11.2 États de l'application

```
 Démarrage ──▶ Projet chargé (sortie en blackout) ──▶ Atelier ◀──▶ Live manuel ◀──▶ Live auto
                                                        │                               │
                                                        └──────── Arrêt (fondu au noir) ◀┘
```

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-060 | I | P0 | Au démarrage, la sortie émet un **blackout** tant que l'utilisateur n'a rien lancé. | Démarrage appareils branchés → aucune lumière, fumée à 0. |
| GEN-061 | I | P5 | À la fermeture de l'application, un **fondu au noir** (1 s par défaut) est effectué avant l'arrêt de l'émission. | Observation au simulateur / sur matériel. |
| GEN-062 | I | P5 | Le passage Atelier ↔ Live ne doit **jamais** interrompre la restitution en cours. | Passer en Live pendant qu'une scène tourne → aucune coupure. |
| GEN-063 | M | P4 | Mode **aveugle** en Atelier : l'édition ne modifie pas la sortie ; l'aperçu reste visible au simulateur. | Éditer une scène en aveugle → trame inchangée, simulateur « aperçu » à jour. |
| GEN-064 | I | P5 | Démarrage jusqu'à « prêt en Live » en moins de 10 s (projet de taille courante, PC standard). | Mesure. |
| GEN-065 | M | P5 | **Fenêtre de démarrage** : dès le lancement, une petite fenêtre indique l'étape en cours (préférences et projet, moteur DMX et sorties, écrans) avec un pourcentage, puis disparaît à l'ouverture de la fenêtre principale. | Demande utilisateur (essai P5). |

---

## 12. Entrées de contrôle

| Entrée | Rôle | Phase |
|---|---|---|
| Souris | Toute l'application | P1 |
| Clavier | Raccourcis globaux et Live ; saisie | P1 |
| **APC mini MK1** | Pads (scènes), faders (masters), boutons ; LED 3 couleurs | P5 |
| **APC mini MK2** | Idem, LED RGB | P5 |
| Télécommande Android | Projet séparé ultérieur, via le catalogue de commandes | Hors périmètre |

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-070 | I | P5 | Toute entrée (clavier, MIDI) se traduit en commandes du catalogue ; aucune entrée n'a d'effet direct sur la sortie. | Revue ; journal indique l'origine de chaque commande. |
| GEN-071 | I | P5 | Raccourcis clavier **globaux** en Live, actifs quel que soit le focus : Blackout, Flash général, Fumée (maintien), Tap tempo, Auto on/off. Les touches exactes sont définies dans le doc 18. | Test manuel ; aucun raccourci ne saisit de texte par erreur. |
| GEN-072 | M | P5 | Les deux modèles d'APC mini sont reconnus automatiquement et peuvent être branchés simultanément. | Brancher MK1 + MK2 → deux contrôleurs actifs, profils corrects. |
| GEN-073 | M | P5 | Débrancher / rebrancher un contrôleur MIDI en cours de soirée est géré sans redémarrage. | Test manuel. |
| GEN-074 | S | P5 | Les affectations MIDI sont modifiables par « apprentissage » (bouger une commande de l'appareil pour l'affecter). | Test manuel. |

---

## 13. Sûreté

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-080 | I | P0 | **Perte du PC** : si l'interface DMX ne reçoit plus de trame valide pendant **2 s**, elle émet un blackout complet (tous canaux à 0, fumée comprise). | Débrancher l'USB → noir en ≤ 2 s (déjà présent dans le POC). |
| GEN-081 | I | P0 | Si l'application se ferme anormalement, le comportement GEN-080 s'applique. | Tuer le processus → noir en ≤ 2 s. |
| GEN-082 | I | P4 | Le **blackout** est accessible en permanence par une action unique (bouton toujours visible, raccourci global, pad MIDI) et prend effet au tick suivant. | Test : latence < 50 ms. |
| GEN-083 | I | P5 | **Strobe** : durée continue maximale et fréquence maximale réglables globalement (par défaut 10 s continues, puis pause forcée) ; le strobe peut être **interdit globalement**. | Test : strobe demandé 30 s → coupé à 10 s, événement journalisé. |
| GEN-084 | I | P5 | **Fumée** (1 canal) : durée d'émission continue maximale (par défaut 10 s) et temps de repos minimal entre émissions (par défaut 30 s) réglables, applicables **aussi à la commande manuelle**. *Précision du 2026-09-27 (essai P5, choix délégué par l'utilisateur) : 30 s de repos après une émission **coupée** par la limite ; après une émission plus courte, repos = 3 × la durée émise (plafonné à 30 s, facteur réglable `restFactor`).* Un bouton « maintien » n'émet que tant qu'il est maintenu. | Test : maintien 20 s → coupure à 10 s. |
| GEN-085 | I | P5 | **Zones interdites Pan/Tilt** par lieu et par lyre : le moteur borne ou contourne ces zones quelle que soit la commande. | Test : scène visant le public → faisceau borné à la limite, événement `LimiteSécuritéAtteinte`. |
| GEN-086 | M | P5 | Un **signal visuel permanent** en Live indique toute limite de sûreté active ou tout verrou. | Revue écran. |
| GEN-087 | S | P10 | Le Directeur respecte un « budget » d'effets agressifs (strobe, flashs) par période, réglable. | Test d'endurance : aucun dépassement. |

> Les valeurs par défaut ci-dessus sont des propositions à ajuster lors des essais.

---

## 14. Robustesse et performances

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-090 | I | P1 | Latence entre une action utilisateur (clic, touche, fader, pad MIDI) et la trame émise < **50 ms**. | Mesure instrumentée. |
| GEN-091 | I | P0 | Une **sortie déconnectée** ne bloque ni le moteur ni l'interface ; reconnexion automatique en < **3 s** après rebranchement, sans action utilisateur. | Test débranchement / rebranchement en cours de lecture. |
| GEN-092 | I | P10 | **Endurance** : 6 h de fonctionnement continu en mode auto sans dégradation (mémoire stable, CPU stable, gigue conforme). | Test d'endurance en simulateur (outil sans interface) + une soirée réelle. |
| GEN-093 | I | P0 | Une erreur dans un module secondaire (audio, style, MIDI, simulateur) ne doit ni arrêter le moteur ni la sortie : le module est désactivé et signalé. | Test : exception forcée dans l'analyse audio → lumière continue, alerte en Live. |
| GEN-094 | M | P5 | Utilisation CPU moyenne < 15 % en Live (hors simulateur 3D) sur un PC portable standard. | Mesure. |
| GEN-096 | I | P0 | Tant que l'application émet, le PC **ne se met pas en veille** : demande temporaire au système, propre au processus, sans modifier les réglages du poste (l'écran peut s'éteindre). | PC réglé sur une veille courte : aucune veille pendant l'émission ; veille de nouveau possible après fermeture. |
| GEN-095 | M | P5 | **Reprise après plantage** : au redémarrage, proposition de revenir au dernier état de restitution connu (projet, scènes actives, mode). | Tuer le processus en Live, relancer → reprise proposée. |

---

## 15. Ergonomie transverse

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-100 | I | P1 | Interface entièrement en **français**. | Revue. |
| GEN-101 | I | P1 | **Thème sombre** par défaut (usage dans le noir), contrastes suffisants ; aucune zone blanche étendue en Live. | Revue. |
| GEN-102 | I | P2 | **Annuler / rétablir** dans tous les éditeurs de l'Atelier (au moins 50 niveaux). | Test par éditeur. |
| GEN-103 | I | P1 | Toute action destructrice (suppression, écrasement) demande confirmation en Atelier ; en Live, aucune action destructrice n'est disponible. | Revue. |
| GEN-104 | I | P1 | Indicateur permanent : état de la sortie (connectée / déconnectée / simulée), trames/s, blackout, mode auto. | Revue. |
| GEN-105 | M | P2 | Recherche / filtre dans toute liste de plus de 20 éléments (modèles, scènes, palettes…). | Revue. |
| GEN-106 | M | P4 | Les objets (scènes, couches, palettes, shows) ont un **nom**, une **couleur** et optionnellement une **icône**, repris partout (Live, MIDI MK2, timeline). | Revue. |
| GEN-107 | M | P1 | Glisser-déposer disponible pour les opérations naturelles (patch, placement au plan, scènes dans couches/timeline). | Revue. |
| GEN-108 | S | P1 | Taille de police de l'interface réglable. | Revue. |
| GEN-109 | I | P1 | Aucune opération longue (import, analyse, sauvegarde) ne fige l'interface ; une progression est affichée. | Import de 1 000 modèles : interface réactive. |

---

## 16. Journalisation

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-110 | I | P0 | **Journal technique** : démarrage, erreurs, connexions/déconnexions, avertissements ; fichiers tournants (taille et nombre limités). | Fichiers présents, rotation vérifiée. |
| GEN-111 | I | P9 | **Journal de soirée** : pour chaque morceau, heure, titre, artiste, style détecté (et méthode, confiance), corrections manuelles, shows joués. Réutilisable pour enrichir la base musicale à la maison. | Après une répétition, le journal liste tous les morceaux. |
| GEN-112 | M | P4 | Journal des commandes (avec origine) consultable en Live sur les dernières minutes. | Revue. |
| GEN-113 | S | P4 | Enregistrement optionnel des trames émises (fichier) pour rejouer / analyser une session. | Rejouer un enregistrement dans le simulateur. |

---

## 16b. Diagnostic et robustesse

> Ajouté le 2026-09-26 (écart constaté en testant le renommage LuXia avec l'utilisateur) : deux besoins concrets, absents du cahier des charges initial.
> GEN-117 et GEN-118 ajoutés le 2026-09-26 pendant l'essai P4 avec l'utilisateur (exception sous le champ Vitesse de l'écran Scènes).

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-114 | M | P3 | Menu **Aide → À propos** : boîte de dialogue en lecture seule, copiable, avec de quoi diagnostiquer un problème à distance (exécutable, répertoire de travail, PID, .NET/OS, dossiers de données, fichier de préférences, dernier projet en mémoire vs projet réellement ouvert, sortie configurée). | Le texte copié suffit à comprendre un problème signalé par l'utilisateur sans accès à son poste. |
| GEN-115 | I | P3 | **Une seule instance** de l'application à la fois (verrou au démarrage) : deux processus se disputeraient le port série de l'Arduino et pourraient perdre le « dernier projet » des préférences. | Lancer une deuxième instance affiche un message et se ferme, sans toucher aux préférences ni au port série ; la première continue normalement. |
| GEN-116 | S | P3 | L'exécutable et la fenêtre ont une **icône** propre à l'application (pas l'icône générique .NET). | Icône visible dans l'Explorateur, la barre des tâches et le titre de la fenêtre. |
| GEN-117 | I | P4 | **Toute exception** est écrite dans le journal technique, d'où qu'elle vienne : interface, autres fils, tâches de fond, et valeur refusée par un champ de saisie (qu'Avalonia intercepte sans la journaliser) ; une erreur d'interface est aussi signalée dans la barre d'état. | Une exception levée en saisissant une valeur apparaît dans le journal technique avec sa pile d'appels. |
| GEN-118 | I | P4 | Un **enregistrement de fichier** refusé un court instant par le poste (antivirus, outil de sécurité) est retenté automatiquement ; un échec persistant est signalé clairement, sans perte des données déjà enregistrées. | Fichier verrouillé 60 ms pendant un enregistrement → enregistrement réussi. |
| GEN-119 | M | P4 | En développement, la **version affichée** (barre de titre, À propos) porte un **numéro de compilation** qui augmente à chaque compilation : « v1.003.017 » = 17ᵉ compilation du passage qui sera validé en 1.003 (doc 03 §7) ; une fois validée, « v1.003 ». | Deux compilations successives affichent deux numéros différents ; l'utilisateur voit s'il teste la dernière. |

---

## 17. Fonctionnement hors-ligne

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-120 | I | P0 | Toutes les fonctions de l'application utilisées en soirée fonctionnent **sans accès Internet**. | Test complet réseau désactivé. |
| GEN-121 | I | P9 | Les fonctions qui utilisent Internet (enrichissement de la base musicale, import de bibliothèques en ligne) sont **séparées**, lancées explicitement, et jamais en arrière-plan en Live. | Aucun trafic Internet observé en Live. |
| GEN-122 | M | P3 | Les sorties réseau locales (Art-Net) restent autorisées : elles n'utilisent que le réseau local. | — |

---

## 17b. Conception assistée par IA (préparation)

Une IA de développement (ex. Claude Code) doit pouvoir **concevoir du contenu** (scènes, effets, séquences, shows, réglages du Directeur)
en lisant et écrivant les fichiers du projet, puisqu'elle connaît le matériel (bibliothèque, installation) et l'application (cette documentation).
Cela se fait **à la maison**, jamais en soirée (P7).

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| GEN-130 | I | P4 | Le **format des fichiers** du projet est documenté au fil du développement (`docs/50-format-des-donnees.md`) : chaque type d'objet, ses champs, ses valeurs possibles, avec un exemple ; accompagné d'un **schéma JSON** vérifiable. | Une IA produit une scène valide à partir de la seule documentation. |
| GEN-131 | I | P4 | Outil en ligne de commande (`Luxia.Tools.Headless`) : **valider** un projet (références, schéma, règles des modules) et produire un rapport lisible, sans ouvrir l'application. | Projet avec une palette inexistante → erreur explicite (fichier, objet, champ). |
| GEN-132 | M | P4 | Le même outil **joue** une scène, une séquence ou un show en temps virtuel (BPM et événements musicaux simulés) et produit un **résumé lisible** (qui s'allume, quelles couleurs, quels mouvements, à quel moment) + un enregistrement de trames rejouable au simulateur. | L'IA peut vérifier elle-même le déroulé d'un show qu'elle a écrit. |
| GEN-133 | I | P4 | Le contenu généré est rangé dans une **catégorie dédiée** (« Proposé par IA ») et n'écrase jamais un objet existant sans accord ; l'application le recharge sans redémarrer (ou sur demande). | Import d'un lot de scènes générées → visibles, rien d'écrasé. |
| GEN-134 | M | P8 | Un **guide de conception** (`docs/51-guide-conception-shows.md`) décrit, pour une IA comme pour l'utilisateur, les bonnes pratiques : organisation en couches, usage des palettes, réactivité musicale, variété, sûreté, métadonnées pour le Directeur. | — |

## 18. Hors périmètre

- Commande depuis un téléphone Android (projet séparé ultérieur, via le catalogue de commandes).
- Multiplateforme (Linux, macOS).
- Écran tactile.
- Utilisation multi-utilisateur, comptes, droits.
- Intégration de logiciels DJ.
- Pilotage de matériel autre que l'éclairage (vidéo, lasers ILDA, pyrotechnie).
- RDM (configuration à distance des appareils via DMX).

---

## 19. Registre des décisions

> Les décisions antérieures au renommage transverse (2026-09-26, avant P4) citent les anciens noms `Dmx.*` et `dmx-headless` :
> ce sont aujourd'hui `Luxia.*` et `luxia-headless`.

| # | Date | Décision | Origine |
|---|---|---|---|
| D1 | 2026-09-24 | Une solution C#, plusieurs projets ; modules compilés ensemble (pas de plugins dynamiques). | Doc 00 §7 |
| D2 | 2026-09-24 | Glossaire métier standard, tenu dans `glossaire.md`. | Q1 |
| D3 | 2026-09-24 | Avalonia, Windows uniquement, sans tactile, français uniquement. | Q2, Q13 |
| D4 | 2026-09-24 | Musique jouée sur le PC de l'application → lecture en cours Windows + capture audio en boucle système. | Q3 |
| D5 | 2026-09-24 | Aucune dépendance Internet en soirée ; enrichissement musical à la maison. | Q4 |
| D6 | 2026-09-24 | Firmware : évolution du POC vers le protocole Enttec DMX USB Pro (à tenter). | Q6 |
| D7 | 2026-09-24 | Prise en charge des APC mini MK1 et MK2. | Q7 |
| D8 | 2026-09-24 | Simulateur 2D, puis prévisualisation 3D simple. | Q8 |
| D9 | 2026-09-24 | Taxonomie de styles définie par le projet + style « Inconnu » piloté par l'énergie + journal de soirée. | Q9 |
| D10 | 2026-09-24 | Perte du PC → blackout complet (fumée comprise) en ≤ 2 s. | Q10 |
| D11 | 2026-09-24 | Fusion : HTP intensité, LTP par priorité de couche pour le reste. | Q11 |
| D12 | 2026-09-24 | Données en JSON lisible ; projet = dossier de fichiers. | Q12, §10 |
| D13 | 2026-09-24 | Valeurs internes normalisées 0-1 ; conversion DMX en fin de chaîne. | §7 |
| D14 | 2026-09-24 | Tick moteur 40 Hz par défaut (25-44 Hz). | §8 |
| D15 | 2026-09-24 | Intensité séparée de la couleur (convention pro) + option « allumer en coloriant » dans le programmeur pour garder la simplicité Daslight. | Doc 15 §5.3 |
| D16 | 2026-09-24 | Intensité HTP entre toutes les couches par défaut ; modes par couche : prioritaire, additif, multiplicatif. | Doc 15 §5.2 |
| D17 | 2026-09-24 | Chaque phase livre un projet de démonstration sur le parc réel + guide de découverte ; les exemples servent aussi de tests de non-régression. | Demande utilisateur, doc 40 §7 |
| D18 | 2026-09-24 | Conception de contenu (scènes, séquences, shows) assistée par une IA **à la maison**, par écriture directe des fichiers JSON du projet ; jamais en soirée. Exigences GEN-130 à 134. | Demande utilisateur |
| D19 | 2026-09-24 | Le test de sortie (chenillard) passe par la commande `TesterSortie` (CMD-024), traitée par le moteur ; il sera soumis aux limiteurs de sûreté dès leur existence (P5). Canaux exclus et valeur de test réglables (fumée exclue par défaut). | Q16, Q17 |
| D20 | 2026-09-24 | Projet d'assemblage **`Dmx.Hosting`** (non prévu au doc 00 §7) : journal technique et assemblage des modules, partagé par l'application et `Dmx.Tools.Headless`. Pas de conteneur d'injection de dépendances en P0 (assemblage explicite, plus lisible) ; à reconsidérer quand les écrans se multiplieront. | Développement P0 |
| D21 | 2026-09-24 | Tant que le test de sortie est actif, il **remplace** la restitution de l'univers testé (un seul canal allumé) : c'est un outil de diagnostic, pas une couche. | Développement P0 |
| D22 | 2026-09-25 | Les définitions du parc sont livrées comme **bibliothèque d'exemple** (`samples/Bibliothèque/`), importable ; la copie dans le projet (GEN-053) viendra avec le patch (P3). Projets d'interface : `Dmx.UI.Controls` + un projet par écran (`Dmx.UI.Modules.Console`, `.Library`, `.Outputs`), conformément au doc 00 §7. | Développement P2 |
| D23 | 2026-09-25 | Mesure de cadence et de gigue (GEN-030 / 031) ramenée d'**1 h à 15 min** (≈ 36 000 ticks, suffisant pour un 99e centile ; la stabilité longue relève de GEN-092). L'application **empêche la mise en veille** du PC pendant l'émission (nouvelle exigence GEN-096) : la veille du poste ne peut pas être allongée et une veille en soirée couperait la lumière. | Q26 |
| D24 | 2026-09-26 | Projet `Dmx.Patch` (doc 00 §7.2) pour l'installation, les sélections et les lieux. Les sélections **automatiques** (INST-031 : Tous, par modèle, par catégorie) ne sont **jamais enregistrées** : elles sont recalculées à la volée depuis le patch courant (toujours à jour, aucun risque d'incohérence). GEN-053 (copie des modèles dans le projet) réutilise le format et le code de `FixtureLibrary`, pointés sur `<projet>/Bibliothèque/` : les modèles génériques de l'application n'y sont jamais copiés (déjà disponibles partout, intégrés à l'application). | Développement P3 |
| D25 | 2026-09-26 | Solution au format **`.sln` classique**, pas `.slnx` (choisi sans discussion en P0) : l'outil de développement de l'utilisateur ne sait pas ouvrir/compiler `.slnx`, ce qui l'a bloqué en plein test. `Dmx.sln` régénéré à l'identique (mêmes 24 projets, mêmes dossiers `src/`, `tests/`, `tools/`) ; `Dmx.slnx` supprimé. Le format de solution ne change plus sans repasser par ici. | Demande utilisateur |
| D26 | 2026-09-26 | Le moteur calcule sur un **modèle compilé** (`ShowModel`) : un **paramètre** par attribut d'appareil patché (une définition de canal du modèle), plus les couches et les scènes aux valeurs déjà résolues. La compilation (sélections développées, palettes traduites, couleurs converties selon les émetteurs) est faite hors du fil du moteur par le nouveau projet **`Luxia.Scenes`** (scènes, palettes, couches, fichiers `scènes.json`, `palettes.json`, `couches.json`). Le moteur reste limité à Core et Messaging (P2 du §4) ; un nouveau modèle est chargé entre deux ticks et les scènes en cours continuent avec leur nouvelle version (PAL-005). | Développement P4 |
| D27 | 2026-09-26 | Résolution d'une valeur de scène : une valeur sur l'**appareil entier** s'applique à tous les canaux de cet attribut, toutes cellules confondues ; une valeur sur une **cellule** l'emporte sur celle de l'appareil, qui l'emporte sur celle d'une **sélection** (à précision égale, la dernière de l'étape). L'intensité de l'appareil entier va au gradateur maître, sinon aux gradateurs de cellule, sinon à l'**intensité virtuelle** (BIB-006), qui vaut **0 par défaut** comme un vrai gradateur (convention D15) : une scène de couleurs seules n'allume pas un PAR 3 canaux, c'est le rôle de « allumer en coloriant » (MOT-041). Les **jumeaux** partagent les paramètres du premier (MOT-092). Un canal est **discret** (jamais interpolé, MOT-012) si son attribut est à emplacements (roue, macro, gobo, prisme, programme, sélecteur, reset…) ou s'il a au moins deux plages dont une non progressive. | Développement P4 |
| D28 | 2026-09-26 | Les **couches** existent dès P4 dans les données (modèle par défaut du doc 17 §1.3, identifiants fixes, `couches.json`) pour que la fusion des couches (MOT-030, MOT-031) serve dès maintenant ; leur **éditeur** reste en P5 (COU-001). De même, le jeu de palettes par défaut (PAL-009) a des identifiants fixes : un projet sans `palettes.json` retrouve toujours les mêmes. | Développement P4 |
| D29 | 2026-09-26 | Les **réglages de sûreté** (strobe : durée, pause, interdiction, vitesse maximale ; fumée : émission, repos) sont enregistrés **dans le projet** (`sûreté.json`), pas dans les préférences du poste : ils décrivent le parc et le show, voyagent avec lui et restent réglables par une IA de conception (contrôlés par `valider`). Les **zones interdites** des lyres sont propres à chaque **lieu** (`lieux.json`). Les limiteurs de strobe et de fumée agissent sur les **octets finaux** (après surcharges brutes et test de sortie) pour n'être jamais contournables (GEN-042) ; les zones, sur les paramètres Pan/Tilt. | P5 (GEN-083 à 085, Q29, Q30) |
| D30 | 2026-09-27 | **Contrôleurs MIDI** dans un projet `Luxia.Midi` sans dépendance NuGet (API Windows `winmm` appelée directement, Q31) ; profils des modèles en **fichiers de données** ; le module ne connaît pas le projet : l'hôte lui fournit la **même disposition que l'écran Live** (règle commune `LiveRules`), et les affectations modifiées viennent de `midi.json`. Numéros MIDI repris des protocoles publiés par AKAI (absents des notices du dépôt), à confirmer sur l'appareil avec `luxia-headless midi`. | P5 (MIDI-001 à 007, Q31) |
| D31 | 2026-09-27 | **Sauvegarde automatique = versions.** LuXia enregistre chaque modification au moment où elle est faite (depuis P1) : un arrêt brutal ne perd rien du projet. GEN-054 est donc réalisée par des **versions** du projet (`<projet>\Versions\…`, toutes les 2 min si changé et au passage en Live, 10 gardées, GEN-055), et la proposition au redémarrage porte sur la **reprise du jeu** (scènes, masters, modes : `%AppData%\LuXia
| D32 | 2026-09-28 | **Effets rangés dans l'étape** (`scènes.json`), compilés comme les valeurs (D26) : cibles développées, phases, degrés et couleurs traduits par appareil (couleurs → tables par canal) ; le moteur ne connaît que des formes et des tables. **Bibliothèque d'effets** `effets.json` : appliquer un modèle en fait une **copie** (Q36). Effet multi-têtes WZYBUTA en **64 canaux à l'adresse 181** (Q25). | P6 |
| D33 | 2026-09-28 | **Aperçu des effets pendant l'édition** : les surcharges d'attributs de l'ÉDITION (valeurs fixes, C7) ne peuvent pas animer un effet ; l'étape éditée qui a des effets est donc **jouée par le moteur** (commande CMD-017 `MontrerÉtape`, lecture à part au-dessus des couches, figée sur l'étape, absente des lectures affichées), sur la sortie en ÉDITION et sur l'aperçu en AVEUGLE ; les attributs animés ne reçoivent plus de surcharge. | P6 (EFF-006) |
eprise.json`, GEN-095, MOT-102). | P5 (GEN-054, 055, 095) |
| D34 | 2026-09-30 | **Capture audio par NAudio** (MIT, boucle WASAPI), isolée derrière `IAudioSource` (remplaçable, testable par fichiers) ; analyse (tempo, impulsions, énergie) **maison**, sans bibliothèque de détection de tempo, dans un fil dédié (AUD-006). Écart assumé par rapport au « sans dépendance » de D30 : WASAPI en COM direct serait lourd et fragile (Q40). | P7 |
| D35 | 2026-09-30 | **Horloge musicale dans le moteur, écoute hors du moteur.** `MusicalClock` (tempo, position, mesures, latence) appartient au moteur et reste pilotée par commandes (CMD-040 à 042) ; l'écoute (`Luxia.Audio`) n'est connue du moteur que par `IAudioFeed`, lu à chaque tick : sans écoute, l'horloge tourne au dernier tempo (GEN-034) et les scènes à impulsion avancent au temps (SCN-052). Les temps et le tempo sont publiés dans l'instantané (`EngineSnapshot.Tempo`), pas sur le bus. | P7 |
| D36 | 2026-09-30 | **Seuils de break et de montée de P7** (écarts au doc 19) : un break dure au moins **une mesure** (1,5 à 4 s) et non deux, constaté sur *Animals*, *Summer*, *Don't Start Now* où les pauses avant le drop sont brèves ; les niveaux d'énergie sont relatifs à l'énergie des deux dernières minutes. Jeu de test : 41 morceaux au lieu de 30, BPM de référence pris de sources publiques ou de mémoire (`annotations.csv`). | P7 |
| D37 | 2026-10-02 | **Module Show = projet `Luxia.Show`** (modèle, fichiers `séquences.json` et `shows.json`, validation, exécution). L'exécution tourne **dans le tick du moteur** : le moteur définit l'interface `ISequencer` et l'appelle à chaque tick après ses commandes ; `Luxia.Show` l'implémente. Les séquences et les transitions tombent ainsi au temps près sur l'horloge, et un franchissement active et désactive ses étapes dans le même tick (R2). Le séquenceur agit par les commandes du catalogue (origine `Show`), appliquées tout de suite et journalisées (les rampes de niveau ne journalisent que leur début et leur fin). Écart au §6 (« module qui envoie des commandes au moteur ») : seul moyen d'avoir la précision voulue sans dupliquer l'horloge. Le moteur reste limité à Core et Messaging. | P8 (SHOW-003, SHOW-023) |
| D38 | 2026-10-02 | **Événements musicaux pour le séquenceur** : drop, break, montée, silence, reprise et niveau d'énergie passent au moteur par `IAudioFeed` (comme les impulsions, D35). **Morceau changé** (avant P9) : reprise après un silence, ou saut du tempo entendu de plus de 15 % hors rapport d'octave. EVT-020 (`Temps`) et EVT-021 (`Impulsion`) **ne sont pas publiés sur le bus** (40 messages par seconde) : le séquenceur lit l'horloge et les impulsions dans le moteur ; EVT-024 (`TempoChangé`) est publié quand le tempo bouge d'au moins 1 BPM ou change de source. **Mode simulation** : commande `SimulerMusique` (CMD-053), qui provoque un événement, impose une énergie (ou la rend à l'écoute) et un style (seule source de style jusqu'à P9). | P8 (EVT-020, 021, 024 ; SHOW-027) |
| D39 | 2026-10-02 | **Séquences et shows, règles de P8** : positions et durées des blocs en **mesures décimales** depuis 0 (0,25 = un temps ; mesure à 4 temps, GEN-025 reportée, Q46) ; une piste = une couche (seules ses scènes s'y posent) ou une piste d'**actions** (niveaux, fumée, flash, noir) ; un bloc arrête sa scène à sa fin, sauf relais immédiat sur la même piste ou fin « laisser jouer ». Étapes de show à **identifiant court** lisible (« 0 », « 2a ») ; **une évolution par tick** ; une transition quantifiée est **armée** quand sa condition devient vraie et franchie à la frontière musicale suivante si elle est encore validée ; tirage pondéré entre transitions de même amont, option « éviter la même branche ». **Fin d'un show** (R6 précisée) : par défaut il **tient** ses dernières étapes (leurs scènes continuent, « terminé » affiché) jusqu'à ce qu'on l'arrête ; « s'arrêter » ou « reprendre au début » au choix ; « Tout arrêter » arrête aussi shows et séquences. Interface : colonne « Shows » et bandeau « Show en cours » sur l'écran de jeu, fenêtre d'édition en brouillon (Q44 solution C, Q45). | P8 (SHOW-001 à 031) |
