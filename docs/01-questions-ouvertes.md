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
| Effet multi-têtes | 1 | WZYBUTA 150 W (moteur tournant + 4 têtes pivotantes) | 16 ch | — |
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
| Q22 | Git | Dépôt distant (GitHub privé ? autre ?) ou dépôt local uniquement ? | ✅ | **Dépôt local uniquement.** |
| Q23 | Test de sortie | Un chenillard **canal par canal** n'allume rien sur un appareil à gradateur maître (PAR en 7 canaux, lyres, UV…) : le maître est à 0 quand la couleur passe, et inversement. Ajouter au test une liste de **canaux maintenus** à la valeur de test pendant tout le chenillard (ex. `1, 8, 15, 22` = maîtres des 4 PAR) ? *(proposition : oui, en P1, noté au carnet d'idées ; en attendant, le guide P0 conseille le mode 3 canaux pour le test)* | ⏳ | |
| Q24 | Bibliothèque – barre LED | La notice `LCB803` du dépôt ne contient que la couverture et une page blanche (2 pages sur 32) : **le tableau DMX de la barre BeamZ LCB803 manque**. Pouvez-vous fournir la notice complète (ou une photo du tableau des modes) ? *En attendant, la barre est décrite par un générique « RGB 3 canaux » signalé comme provisoire.* | ⏳ | |
| Q25 | Bibliothèque – effet WZYBUTA | Le tableau 16 canaux de l'effet multi-têtes est ambigu (traduction) : canal 6 « Master switch », 7 « flash, **255 = reset** », 8 « programmes 6-255 », 11-14 « rouge/vert/bleu/blanc 1-9 », 16 « totalise ». La définition livrée est marquée « à vérifier en direct » : pourrez-vous la vérifier avec le mode **découverte** de l'éditeur (P2) ? | ⏳ | |
