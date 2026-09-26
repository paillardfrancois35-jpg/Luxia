# Questions ouvertes – suivi

> Questions à trancher avant la rédaction du cahier des charges.
> Statut : ⏳ en attente · 🟡 partiellement répondu · ✅ répondu.

| # | Sujet | Question | Statut | Réponse / décision |
|---|---|---|---|---|
| Q1 | Glossaire | Vocabulaire de référence ? | ✅ | Glossaire métier standard, tenu dans [glossaire.md](glossaire.md) (document vivant). |
| Q2 | Interface | Techno ? Tactile ? | ✅ | **Avalonia**, **Windows uniquement**, **pas de tactile** (souris / clavier / contrôleur MIDI). Commande Android = projet séparé ultérieur. |
| Q3 | Musique | Source de la musique ? | ✅ | Streaming varié (Deezer, YouTube Music, Spotify…) lancé par des amateurs, **sur le PC qui fait tourner l'appli**. → Lecture en cours via l'API Windows + capture audio en boucle système (WASAPI). Pas de logiciel DJ. |
| Q4 | Réseau | Internet en soirée ? | ✅ | **Non** pour le logiciel : **100 % hors-ligne** en soirée. Internet / IA uniquement à la maison pour enrichir la base. |
| Q5 | Matériel | Inventaire et modèles | ✅ | Inventaire validé (tableau ci-dessous). Lyres identiques. Fumée = **1 canal DMX**, commandée surtout à la main. *Mis de côté (non bloquant) : tableaux DMX exacts des 6 PAR, fournis plus tard par l'utilisateur.* |
| Q6 | Firmware | Protocole PC↔Arduino | ✅ | POC existant `Arduino/dmx_poc/dmx_poc.ino`. **Évolution vers le protocole Enttec DMX USB Pro : à tenter.** |
| Q7 | Contrôleur | APC mini MK1 / MK2 ? | ✅ | Les deux sont possédés → **prise en charge des deux** (profils distincts : MK1 LED 3 couleurs, MK2 LED RGB). Intégration proposée avec l'écran Live (phase P5). |
| Q8 | Simulateur | 2D suffisant ? | ✅ | 2D d'abord ; **prévisualisation 3D simple** envisageable ensuite. |
| Q9 | Styles | Familles de musiques ? | ✅ | « De tout », imprévisible → **taxonomie définie par nous** (une douzaine de familles, évolutive), **famille « Inconnu »** pilotée uniquement par l'énergie audio, **journal des titres joués** en soirée pour les classer ensuite à la maison. |
| Q10 | Sûreté | PC planté ? | ✅ | **On coupe** (blackout, fumée comprise). Déjà en place dans le POC (délai 2 s). |
| Q11 | Moteur | Règles de fusion | ✅ | HTP pour l'intensité, LTP par priorité de couche pour le reste (logique Daslight). |
| Q12 | Données | Format de sauvegarde | ✅ | Fichiers **JSON lisibles**, versionnables avec Git. |
| Q13 | Langue | Langue de l'interface | ✅ | **Français** uniquement. |

## Inventaire matériel (Q5)

| Rôle | Qté | Modèle | Canaux connus | Reste à vérifier |
|---|---|---|---|---|
| PAR | 4 | Betopper LPC008S (RGB) | 3 / 7 ch | — |
| Gros PAR | 2 | Betopper LPC010 ou LPC120 (RGBW) | 4 / 8 ch | ✅ Tableaux lus en P2 (rendu des PDF en images) |
| Lyres | 2 (identiques) | Tomshine (lyre gobo) | 9 / 11 ch | — |
| UV | 2 | BeamZ BUV463 | 7 ch | — |
| Barres | 2 | BeamZ LCB803 | ? | Tableau DMX (PDF image) |
| Effet multi-têtes | 1 | WZYBUTA 150 W (plateau tournant + 4 barrettes de 3 projecteurs RGBW) | 20 ch / 64 ch | — |
| Fumée | 1 | — | 1 ch | — |
| Contrôleurs | 2 | AKAI APC mini MK1 et MK2 | — | — |
| Interface DMX | 1 | Arduino Leonardo + shield Conceptinetics CTC-DRA-10-R2 | — | — |

## Questions de développement – P0 (ouvertes et tranchées le 2026-09-24)

| # | Sujet | Question | Statut | Réponse / décision |
|---|---|---|---|---|
| Q14 | .NET | La « LTS en vigueur » est **.NET 10** (support jusqu'en nov. 2028). Seul le SDK .NET 8 est installé sur le poste : installer le SDK .NET 10 ? *(proposition : oui, .NET 10)* | ✅ | **.NET 10** (LTS). SDK installé par l'utilisateur (`winget install Microsoft.DotNet.SDK.10`). |
| Q15 | Divergence doc 41 / SORT-006 | Doc 41 §11 met la « configuration de sortie » dans le show de référence, alors que SORT-006 la range dans les **préférences du poste**. *Proposition : SORT-006 fait foi ; l'apport P0 au show = squelette du projet versionné + enregistrement du chenillard + JOURNAL.md ; doc 41 corrigé.* | ✅ | SORT-006 fait foi : la configuration de sortie reste dans les préférences du poste. Doc 41 §11 corrigé. |
| Q16 | Chenillard de test | SORT-007 et doc 40 §7.3 : canaux 1-16 ; doc 41 §11 : canaux 1-180. *Proposition : plage réglable, 1-16 par défaut à l'écran Sorties, 1-180 pour l'enregistrement de démonstration.* | ✅ | Plage réglable (1-16 par défaut à l'écran Sorties, 1-180 pour l'enregistrement de démonstration) + **liste de canaux exclus** réglable (par défaut : 180, fumée) + **valeur de test** réglable (par défaut 50 %), pour ne pas déclencher fumée ni canaux Reset/contrôle. Reporté dans SORT-007 et doc 41. |
| Q17 | Source du chenillard | En P0 le moteur est « vide ». Pour respecter GEN-002 / P3, *proposition : nouvelle commande `TesterSortie` (plage, pas, durée) traitée par la boucle du moteur ; ajoutée au catalogue doc 02 §6.2.* Alternative : générateur de test propre au module Sortie. | ✅ | Commande **`TesterSortie`** (CMD-024) traitée par la boucle du moteur, ajoutée au catalogue doc 02 §6.2 ; soumise aux limiteurs de sûreté dès qu'ils existent (P5). |
| Q18 | Interface en P0 | *Proposition : fenêtre Avalonia minimale = écran « Sorties » (SORT-007 : état, port, version firmware, trames/s, reconnecter, test, enregistreur on/off) ; shell, thème et navigation restent en P1.* Alternative : P0 sans interface (outil Headless seul). | ✅ | Fenêtre Avalonia minimale limitée à l'écran « Sorties » en P0 ; shell et thème en P1. |
| Q19 | Firmware | Installer **arduino-cli** pour que je compile (et éventuellement téléverse) le firmware, ou compilation par vous dans l'IDE Arduino ? *(proposition : arduino-cli)* | ✅ | **arduino-cli** : compilation libre ; **chaque téléversement sur la carte est demandé à l'utilisateur au préalable**. |
| Q20 | POC firmware | *Proposition : le POC est versionné tel quel au premier commit puis remplacé par `firmware/arduino-dmx` (historique conservé dans Git) ; le dossier `Arduino/` disparaît.* | ✅ | Accepté : POC versionné au premier commit puis remplacé par `firmware/arduino-dmx`. |
| Q21 | Règles de développement | Valider les propositions (langue du code, style C#, structure, dépendances, bibliothèques, tests, Git, commentaires) avant rédaction de `docs/03-regles-de-developpement.md`. | ✅ | Toutes les propositions acceptées → [03](03-regles-de-developpement.md). |
| Q22 | Git | Dépôt distant (GitHub privé ? autre ?) ou dépôt local uniquement ? | ✅ | Décidé le 2026-09-25 : **dépôt local uniquement**. Revu le 2026-09-26 : distant **GitHub** câblé (`origin` = [github.com/paillardfrancois35-jpg/Luxia](https://github.com/paillardfrancois35-jpg/Luxia)) — voir [03 §7](03-regles-de-developpement.md). |
| Q23 | Test de sortie | Un chenillard **canal par canal** n'allume rien sur un appareil à gradateur maître (PAR en 7 canaux, lyres, UV…) : le maître est à 0 quand la couleur passe, et inversement. Ajouter au test une liste de **canaux maintenus** à la valeur de test pendant tout le chenillard (ex. `1, 8, 15, 22` = maîtres des 4 PAR) ? *(proposition : oui, en P1, noté au carnet d'idées ; en attendant, le guide P0 conseille le mode 3 canaux pour le test)* | ✅ | Utilisateur : **oui, en P3** → exigence SORT-008 (canaux maintenus pendant le test de sortie). |
| Q24 | Bibliothèque – barre LED | La notice `LCB803` du dépôt ne contient que la couverture et une page blanche (2 pages sur 32) : **le tableau DMX de la barre BeamZ LCB803 manque**. Pouvez-vous fournir la notice complète (ou une photo du tableau des modes) ? *En attendant, la barre est décrite par un générique « RGB 3 canaux » signalé comme provisoire.* | ✅ | Notice fournie (pages 1, 5, 6, 27-29) le 2026-09-25 : 5 modes (3, 6, 12, 24, 48 canaux), sections de 6 canaux → définition `samples/Bibliothèque/BeamZ/LCB803.json`. |
| Q25 | Bibliothèque – effet WZYBUTA | Le tableau 16 canaux de l'effet multi-têtes est ambigu (traduction) : canal 6 « Master switch », 7 « flash, **255 = reset** », 8 « programmes 6-255 », 11-14 « rouge/vert/bleu/blanc 1-9 », 16 « totalise ». La définition livrée est marquée « à vérifier en direct » : pourrez-vous la vérifier avec le mode **découverte** de l'éditeur (P2) ? | 🟡 | Utilisateur (25/09, soir) : l'appareil a en réalité un mode 1 = **20 canaux** et un mode 2 = **64 canaux** (définition ScanLibrary de Daslight, captures déposées) ; le tableau 16 canaux de la notice ne s'applique pas. Définition refaite ; plan du show : 20CH à l'adresse 141. Canaux 17-19 / 61-63 = laser optionnel absent (inutilisés). **Reste ouvert** : réglage du menu de l'appareil qui choisit 20 ou 64 canaux (question précisée le 25/09) ; variation de vitesse du canal 2 dans chaque sens (vérification par l'utilisateur). |
| Q26 | Veille du PC | La mise en veille du poste ne peut pas être allongée (réglage imposé). Or un PC qui s'endort en soirée coupe la lumière (chien de garde → noir). *Proposition : l'application demande elle-même à Windows de rester éveillée tant qu'elle émet (`SetThreadExecutionState`, demande temporaire propre au processus, sans toucher aux réglages), aussi pour `dmx-headless gigue`. Et mesure de gigue ramenée à 15 min (≈ 36 000 ticks, suffisant pour le 99e centile) ; l'endurance longue reste le rôle de GEN-092 (6 h, P10).* | ✅ | Utilisateur : mesure ramenée à **15 min** ; **blocage de la veille** par l'application → GEN-096, D23. |
| Q27 | Installation – gros PAR | Le patch du show de référence (doc 41 §2) demande un modèle pour les 2 « gros PAR » RGBW, entre **Betopper LPC010** et **Betopper LPC120** (les deux définitions existent dans la bibliothèque, doc 12). *En attendant, le show de référence patche provisoirement en LPC120, mode 8 canaux, adresses 31 et 41 (dans la réserve du plan, ≤ 10 canaux).* Lequel possédez-vous réellement ? Un mode différent des « 8 canaux » convient-il mieux ? | ⏳ | — |
| Q28 | Bibliothèque – canal Strobe à 0 | Les définitions du **LPC008S** (canal 5, 7 canaux) et de la barre **LCB803** (canal Strobe de chaque section) décrivent une seule plage « Strobe » sur **0-255**. Le simulateur (SIM-012) et le résumé de `luxia-headless jouer` considèrent donc qu'un PAR avec son canal Strobe à 0 **clignote**, alors qu'en vrai (test P3) il éclairait fixe. *Proposition : ajouter une plage « Pas de strobe » 0-10 (valeurs à confirmer sur l'appareil, par exemple avec le mode découverte de la Bibliothèque), comme sur l'UV BUV463.* Quelle est la vraie plage « sans strobe » de ces deux appareils ? | ⏳ | — |

## Questions de développement – P5 (ouvertes le 2026-09-26)

| # | Sujet | Question | Statut | Réponse / décision |
|---|---|---|---|---|
| Q29 | Sûreté – fumée | La machine à fumée (générique 1 canal, adresse 180) est-elle **raccordée et utilisable pour les essais** P5 (limiteur de fumée, GEN-084 / MOT-081) ? Les valeurs par défaut du cahier des charges conviennent-elles : **10 s** d'émission continue au plus, **30 s** de repos minimal, s'appliquant aussi à la commande manuelle ? | ⏳ | — |
| Q30 | Sûreté – zones interdites | *Proposition (INST-053, MOT-082)* : une zone interdite = un **rectangle Pan/Tilt** par lyre et par lieu, défini en visant ses deux coins à la main (faders Pan/Tilt du programmeur) ; le moteur ramène la cible au bord le plus proche. Dans votre salon, quelle zone faut-il interdire en priorité (vers les yeux du public, un mur, un miroir) ? Un rectangle suffit-il, ou faut-il plusieurs zones par lyre ? | ⏳ | — |
| Q31 | MIDI – matériel et bibliothèque | Quel(s) APC mini pouvez-vous brancher pour les essais P5 (MK1, MK2, les deux) ? *Proposition* : accès MIDI par l'API Windows `winmm` appelée directement (aucune dépendance NuGet), plutôt qu'une bibliothèque (DryWetMIDI, NAudio). | ⏳ | — |
| Q32 | Périmètre et ordre de P5 | P5 est la plus grosse phase (≈ 60 exigences). *Proposition d'ordre* : 1) sûreté (strobe, fumée, zones) ; 2) couches complètes (éditeur, Flash, figer, arrêter) ; 3) palettes par lieu ; 4) écran Live compact ; 5) APC mini ; 6) sauvegarde auto, reprise, fondu au noir ; 7) assistant d'installation. *Et, vu la remarque d'ergonomie (doc 99)* : reporter les exigences « M/S » qui ajoutent des écrans ou panneaux (LIVE-006 disposition personnalisable, LIVE-007 mini-simulateur, INST-070/071 assistant d'installation, GEN-057 archive, MIDI-008 apprentissage, LIVE-041 raccourcis personnalisables), et privilégier ce qui aide l'IA à construire les shows (couches, disposition Live et affectations MIDI décrites en JSON documenté, vérifiées par `valider`). D'accord ? | ⏳ | — |
