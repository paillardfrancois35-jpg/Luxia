# Documentation du projet LuXia

Application C# (Avalonia, Windows) de pilotage d'éclairage, alternative légère à Daslight 4, avec mode automatique musical.

## Lire dans cet ordre

| Document | Contenu |
|---|---|
| [00 – Analyse et découpage](00-analyse-et-decoupage.md) | Analyse du besoin, forces / faiblesses, idées retenues, architecture proposée |
| [01 – Questions ouvertes](01-questions-ouvertes.md) | Suivi des questions et réponses |
| [Glossaire](glossaire.md) | Vocabulaire de référence (document vivant) |
| [02 – Principes et architecture fonctionnelle](02-principes-et-architecture-fonctionnelle.md) | Règles transverses, modules, commandes / événements, chaîne de rendu, données, sûreté, **registre des décisions** |

## Cahier des charges par module

| Doc | Module | Préfixe | Phase |
|---|---|---|---|
| [10](10-sortie-dmx-et-firmware.md) | Sortie DMX et firmware | SORT | P0 |
| [11](11-console.md) | Console | CONS | P1 / P3 |
| [12](12-bibliotheque-appareils.md) | Bibliothèque d'appareils | BIB | P2 |
| [13](13-installation-et-lieux.md) | Installation, sélections, lieux | INST | P3 / P5 |
| [14](14-simulateur.md) | Simulateur | SIM | P3 |
| [15](15-moteur-de-rendu.md) | Moteur de rendu | MOT | P4-P7 |
| [16](16-scenes-et-effets.md) | Scènes, programmeur, effets | SCN / EFF | P4 / P6 |
| [17](17-couches-et-palettes.md) | Couches et palettes | COU / PAL | P4 / P5 |
| [18](18-live.md) | Écran Live | LIVE | P5-P10 |
| [18b](18b-controleurs-midi.md) | Contrôleurs MIDI (APC mini) | MIDI | P5 |
| [19](19-audio-et-tempo.md) | Audio et tempo | AUD | P7 |
| [20](20-show-et-sequences.md) | Show (Grafcet) et séquences | SHOW | P8 |
| [21](21-lecture-en-cours-et-style.md) | Lecture en cours et style | MUS | P9 |
| [22](22-directeur-automatique.md) | Directeur automatique | AUTO | P10 |
| [23](23-timeline-par-morceau.md) | Timeline par morceau | TL | P11 |

## Vérification et pilotage

| Doc | Contenu |
|---|---|
| [30 – Plan de tests](30-plan-de-tests.md) | Stratégie, niveaux, jeux de données, scénarios, check-lists de jalons |
| [40 – Feuille de route](40-feuille-de-route.md) | Phases, preuves de concept, travaux de contenu, conduite du projet, démonstrations |
| [41 – Show de référence](41-show-de-reference.md) | Le show complet construit au fil des phases avec le parc réel (plan d'adresses, palettes, scènes, shows) + samples par mécanique |
| [exigences/](exigences/README.md) | **Fiches d'exigences** : une par exigence, statut et historique (questions, décisions, écarts, commits, validations) |
| [32 – Passation](32-passation.md) | **Point d'entrée pour reprendre dans une nouvelle discussion** : état, carte du code, commandes, démarrage d'une phase |
| [31 – Matrice exigences ↔ tests](31-matrice-exigences.md) | Générée par `tools/matrice-exigences.py` |
| [50 – Format des données](50-format-des-donnees.md) | Fichiers JSON et binaires de l'application (tenu au fil du développement) |
| [demos/](demos/) | Guides de démonstration par phase (`P0-fondations.md` à `P5-couches-palettes-live.md`) |
| [99 – Carnet d'idées](99-idees.md) | Idées en attente |

## Autres

- `Equipements/` : notices et photos du matériel ; `Equipements/DasLight/ecoute-ligne-dmx-dvc4.md` : écouter la ligne DMX avec le DVC4.
- `Daslight4/` et `Daslight 5/` : documentations et captures de Daslight, sources de principes pour le chantier ergonomique (pas de copie).
- [03 – Règles de développement](03-regles-de-developpement.md) : conventions de code, structure, tests, Git.
