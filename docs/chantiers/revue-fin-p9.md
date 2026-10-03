# Revue de fin de phase P9 – Lecture en cours et style

> Revue globale (documentation, code, commentaires, fiches), faite le 2026-10-03 avant l'essai, comme la [revue de P7](revue-fin-p7.md).
> Version de développement **1.011.049**. Aucun correctif ne reste ouvert dans cette liste.

## 1. Chiffres

| Contrôle | Résultat |
|---|---|
| Compilation de la solution | 0 avertissement, 0 erreur |
| Tests automatiques | **1 357**, tous verts (Music 318, UI 304, Engine 175, Fixtures 77, Integration 70, Scenes 70, Patch 60, Show 58, Architecture 51, Audio 42, Midi 39, Output 34, Persistence 19, Media 18, Core 13, Prototype 9) ; P8 : 970 au début de P9 (+387) |
| `dotnet format --verify-no-changes` | propre (3 fichiers de tests reformatés) |
| Analyseurs de code mort (IDE0051, 0052, 0059, 0060) | aucun signalement |
| `python tools/audit-documentation.py` | aucun nouveau problème ; restent les faux positifs connus (GEN-030 et GEN-031, dates hors ordre ; 2 liens d'exemple : `../NN-document.md` du README des fiches et le lien du README à `JOURNAL.md` avec une adresse encodée) |
| Fiches d'exigences | 505 ; toutes les exigences de P9 ont leur fiche (MUS-001 à 007, 020 à 029, 040 à 042, EVT-040 à 042, CMD-062, CMD-063, LIVE-022, GEN-111, GEN-121), sauf MUS-004 « Abandonné » (Q43) |
| Caractères de contrôle dans les fichiers modifiés | aucun (après réparation de 6 endroits de la documentation écrits depuis PowerShell, voir doc 03 §11) |

## 2. Défauts trouvés et corrigés à la relecture

| # | Constat | Correction |
|---|---|---|
| R1 | Le service de style remplaçait la base **sous son verrou** et levait alors des événements (écran, moteur) : un abonné lent aurait pu bloquer les lectures de l'état | Remplacement hors du verrou (`MusicStyleService.Session`) |
| R2 | « Morceau changé » était porté par un drapeau partagé : un style **imposé** arrivant juste avant le vrai changement de titre pouvait le consommer (cue envoyé avec l'ancien style) | **Numéro de série** du morceau dans `StyleState` : un nouveau morceau seulement quand le numéro change ; test `Serial_ChangesWithTheTrack_NotWithAForcedOrCorrectedStyle` |
| R3 | Une session en pause jamais vue en lecture devenait « le morceau en cours » quand la session suivie se fermait (Deezer ferme la sienne après une pause) | Seules les sessions qui ont joué depuis le démarrage peuvent remplacer la session suivie (corrigé pendant le PoC-3, 2 tests) |
| R4 | L'écran « À classer » ne montrait pas la supposition de LuXia pour les artistes proches d'un artiste connu | Ligne « Supposition de LuXia : Pop (59 %), à confirmer » |
| R5 | L'écran Contrôle et le guide annonçaient 7 shows : 9 avec P9 | Test mis à jour ; trames et guide |
| R6 | Chemins des exécutables (`net10.0` → `net10.0-windows10.0.19041.0`) dans les docs 03, 33 et le guide P0 | Mis à jour |
| R7 | Six passages de documentation avaient perdu des caractères (accent grave suivi de `a`, `v` ou `t` dans PowerShell) | Réparés ; piège noté au doc 03 §11 |

## 3. Limites connues (à garder en tête à l'essai)

- **Sources de lecture** : Deezer (application) et YouTube Music (Chrome) seulement. VLC, VirtualDJ et Mixxx ne s'annoncent pas à Windows ([doc 99](../99-idees.md), mémoire du projet).
- **Position** : donnée par Deezer ; périmée de plusieurs secondes sur YouTube Music pendant une pause (relevé du navigateur) : la position estimée dérive alors.
- **Taux d'identification** : environ 96 % sur 163 titres annotés par moi-même (base de départ de 461 artistes) : c'est un **plancher optimiste** pour mes propres goûts musicaux ; la vraie mesure (T-MUS-03 sur vos playlists) reste à faire quand vous fournirez un export (Q52).
- **Enrichissement en ligne** : testé **sans réseau** (réponses simulées). Jamais essayé sur le vrai MusicBrainz : le format des réponses est celui de la documentation publique ; à essayer à la maison (exemples 21 à 23 du guide).
- **Import de playlists** : formats d'export Deezer et Spotify supposés d'après les noms de colonnes usuels ; non essayé avec un vrai fichier.
- **MUS-023** (taxonomie modifiable) : par le fichier seulement, sans écran ni mise à jour automatique des références.
- **Titre sans artiste** (nom de fichier sans « Artiste - Titre ») : impossible à classer dans « À classer » (message).
- **Panneau « Pilote automatique »** : garde ses champs « Titre en cours : — » et « Style : — » (P10) alors que le bloc « Morceau en cours » les montre : doublon temporaire à trancher avec le Directeur.
- **Deezer** : l'application nomme sa session `Deezer.62021768415AF_q7m17pa7q8kj0` ; le nom est raccourci en « Deezer » (`MediaApps.FriendlyName`).

## 4. Documentation mise à jour

Doc 02 (CMD-062, CMD-063, D40), 03 (§11), 21 (§6 notes de réalisation), 20 (§6 révision de D38), 40, 41, 50 (§12i, §12j), 51 (§4, règle 8), 99 (idées VLC / VirtualDJ / Mixxx), 01 (Q43 révisée, Q48 à Q52), 32 (passation) ; schémas `normalisation`, `taxonomie`, `artistes`, `titres`, `corrections`, `aclasser`, `propositions` ; `samples/Show de référence/JOURNAL.md` ; guide d'essai [P9](../demos/P9-lecture-et-style.md), [PoC-3](../demos/P9-poc3-sonde.md) et fichiers de résultats [P9](../essais/P9-resultats.md), [PoC-3](../essais/P9-poc3.md).
