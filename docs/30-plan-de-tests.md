# 30 – Plan de tests

> Stratégie globale de vérification. Les tests détaillés sont listés à la fin de chaque document de module (`T-XXX-nn`).
> Références : [00 §14](00-analyse-et-decoupage.md), [02 §2 (critères d'acceptation)](02-principes-et-architecture-fonctionnelle.md).

---

## 1. Principes

1. **Chaque exigence Indispensable (I) a au moins un test** : automatisé de préférence, sinon une ligne de check-list manuelle.
2. **Le moteur se teste en temps virtuel** : on fait avancer l'horloge artificiellement ; une heure de show se vérifie en quelques secondes, de façon reproductible.
3. **Pas de matériel pour 90 % des tests** : sorties Nulle / Enregistreur / Simulateur. Le matériel sert aux tests de sortie, aux vérifications de définitions et aux jalons.
4. **Les jeux de données de test sont versionnés** (sauf fichiers audio personnels, conservés localement).
5. **Non-régression par trames de référence** : des scénarios de projet produisent des enregistrements de trames comparés octet par octet à une référence validée.

## 2. Niveaux de tests

| Niveau | Objet | Moyens | Fréquence |
|---|---|---|---|
| **Unitaires** | Règles de chaque module (conversion, fusion, fondus, validation, normalisation…) | Tests automatisés | À chaque modification |
| **Moteur en temps virtuel** | Scènes, couches, effets, shows | Horloge injectée, scénarios de commandes horodatées | À chaque modification |
| **Non-régression (golden files)** | Trames produites par 20+ scénarios | Outil sans interface + comparaison | À chaque modification |
| **Intégration** | Chaîne complète projet → moteur → sortie | Pilote Enregistreur | À chaque modification |
| **Jeux de données** | Audio (tempo, impulsions, énergie), titres (normalisation, styles) | Rapports chiffrés comparés à la version précédente | À chaque évolution des algorithmes |
| **Matériel (banc)** | Firmware, pilote Arduino, définitions d'appareils | Arduino + (option) renifleur + 1 PAR + 1 lyre | À chaque évolution du firmware / des définitions |
| **Visuel / manuel** | Écrans, simulateur, ergonomie Live | Check-lists | Fin de phase |
| **Endurance** | Stabilité sur 6 h | Outil sans interface + simulateur | Avant chaque jalon |
| **Terrain** | Vraie soirée | Journal et rapport de soirée | Jalons 1 et 2 |

## 3. Environnements

| Environnement | Composition |
|---|---|
| **Développement** | PC, sortie Nulle / Enregistreur, simulateur |
| **Banc de bureau** | + Arduino Leonardo + shield ; + 1 PAR RGB + 1 lyre ; (option) second Arduino en **renifleur** (T-SORT-08) ; APC mini MK1 et MK2 |
| **Répétition** | PC + playlist réelle + simulateur plein écran, mode auto |
| **Parc complet** | Tout le matériel monté (uniquement pour les jalons) |

## 4. Jeux de données de test

| Jeu | Contenu | Emplacement |
|---|---|---|
| Modèles d'appareils | Définitions valides / invalides ; fichiers OFL et QLC+ de référence | `tests/assets/fixtures` |
| Projets de test | Petits projets couvrant chaque fonction (scènes, couches, effets, shows) | `tests/assets/projects` |
| Trames de référence | Enregistrements validés des scénarios | `tests/assets/golden` |
| Audio | 30 morceaux annotés + pièges + breaks/drops + enchaînements (doc 19 §7) | `tests/assets/audio` (local, non versionné) |
| Titres | 200 titres bruts réels + 300 titres annotés en style | `tests/assets/music` |
| Scénarios de soirée | Flux d'événements simulés (morceaux, styles, énergies) pour le Directeur | `tests/assets/scenarios` |

## 5. Scénarios de bout en bout

| ID | Scénario | Vérifie | Phase |
|---|---|---|---|
| SC-01 | Démarrer sans Arduino, brancher l'Arduino, piloter un canal, débrancher, rebrancher | Détection, reconnexion, sûreté (noir en ≤ 2 s) | P0-P1 |
| SC-02 | Créer le modèle d'un PAR, le vérifier en direct, le patcher 4 fois, identifier chacun | Bibliothèque, patch, identification | P2-P3 |
| SC-03 | Changer de mode un appareil utilisé par 3 scènes | Rapport d'impacts, scènes conservées | P3-P4 |
| SC-04 | Couleurs × mouvements × strobe sur 3 couches ; fondus croisés ; blackout ; Grand Master | Fusion, masters, blackout sans mouvement parasite | P4-P5 |
| SC-05 | Changer de lieu actif | Palettes de position, appareils absents | P5 |
| SC-06 | Soirée manuelle de 30 min au simulateur avec APC mini | Live, MIDI, raccourcis | P5 |
| SC-07 | Chenillard au temps + cercle sur 1 mesure sur un morceau de référence | Horloge, effets calés | P7 |
| SC-08 | Show « Couplet / Refrain / Drop » sur un morceau électro | Séquenceur, événements audio, quantification | P8 |
| SC-09 | Playlist de 20 titres variés (Spotify + YouTube) | Lecture en cours, styles, corrections | P9 |
| SC-10 | 3 h de répétition en mode auto au simulateur | Directeur, variété, budgets, endurance | P10 |
| SC-11 | Arrêt brutal du PC en plein show | Noir en ≤ 2 s ; reprise proposée au redémarrage | P5 |

## 6. Check-lists de jalons

### Jalon 1 – « Soirée manuelle » (fin de P5)

- [ ] Tous les appareils du parc définis et vérifiés en direct (annexe A du doc 12).
- [ ] Installation complète patchée ; fiche d'installation imprimée.
- [ ] Lieu « Générique » + lieu de la prochaine soirée ; positions calibrées ; zones interdites.
- [ ] Au moins 6 couches, 40 scènes utiles (couleurs, mouvements, intensité, effets, ambiance, flashs).
- [ ] APC mini configuré ; raccourcis maîtrisés.
- [ ] SC-01 à SC-06 et SC-11 passés.
- [ ] Endurance 6 h (moteur + sortie + Live) sans anomalie.
- [ ] Soirée réelle : journal et retour d'expérience rédigés → nouvelles exigences éventuelles.

### Jalon 2 – « Soirée automatique » (fin de P10)

- [ ] Jeu audio : objectifs AUD-020, AUD-040, AUD-062 atteints (rapport).
- [ ] Jeu de titres : objectif T-MUS-03 atteint (rapport) ; base musicale préremplie.
- [ ] Au moins 2 shows principaux par famille fréquente + show par défaut + Transition / Attente / Slow.
- [ ] Tableau de couverture du Directeur sans trou majeur.
- [ ] SC-07 à SC-10 passés ; simulation accélérée 6 h conforme (T-AUTO-04).
- [ ] Répétition 3 h au simulateur jugée satisfaisante.
- [ ] Soirée réelle en mode auto ; rapport de soirée analysé.

## 7. Traçabilité

Une **matrice exigences ↔ tests** est tenue à jour au fil du développement (fichier généré ou tableau), avec pour chaque exigence :
identifiant, priorité, phase, test(s) associé(s), statut (non commencé / en cours / validé). Une phase n'est close que si toutes
ses exigences I sont validées.

### 7.1 Correspondance des tests audio et tempo (P7)

| Test prévu | Réalisé par |
|---|---|
| T-AUD-01 (analyse sur signaux synthétiques) | `AnalyzerTests`, `PulseAndEnergyTests` (`tests/Luxia.Audio.Tests`) |
| T-AUD-02 (jeu de test annoté) | `luxia-headless audio` ; rapport chiffré [essais/P7-audio-rapport.md](essais/P7-audio-rapport.md) (troisième édition) |
| T-AUD-03 (horloge musicale) | `MusicalClockTests` (`tests/Luxia.Engine.Tests`) |
| T-AUD-04 (écoute, scènes au temps) | `AudioListenerTests`, `MusicalReactivityTests`, `ReferenceShowP7Tests` (trames de référence `P7-scenes.txt`) |
| T-AUD-05 (essai à la main) | Guides [P7](demos/P7-audio-tempo.md) et re-vérifications ; résultats [essais/P7-resultats.md](essais/P7-resultats.md) |
| T-AUD-06 (6 h de capture continue) | **Non fait** : 1 h sur le matériel avant validation, 6 h avec P10 (GEN-092) ; `AudioListenerHardwareTests` (catégorie « Materiel ») pour l'essai court |

## 8. Démonstrations et non-régression

Les exemples livrés à chaque phase (doc 40 §7) font partie des tests : chaque exemple est rejoué automatiquement en temps virtuel
et ses trames sont comparées à une référence validée par l'utilisateur lors de la livraison de la phase. Un exemple cassé bloque la livraison.
