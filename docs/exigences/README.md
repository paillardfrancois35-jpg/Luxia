# Fiches d'exigences (suivi à la manière d'un Redmine)

> **Une fiche par exigence développée** (ou commencée, ou reportée après examen). La fiche **fait foi** pour le statut
> de l'exigence et garde la trace de **tout** ce qui la concerne : questions, réponses, décisions, écarts, développements,
> tests, validations. But : ne jamais trancher une question deux fois sans savoir pourquoi elle avait été tranchée
> la première fois.

## 1. Règles

| # | Règle |
|---|---|
| F1 | Une fiche `docs/exigences/<ID>.md` est créée dès qu'on commence à travailler sur une exigence (ou qu'on décide de la reporter). |
| F2 | L'historique est **ajout seul** : on n'efface ni ne réécrit une entrée ; une erreur se corrige par une nouvelle entrée qui cite la précédente. |
| F3 | Toute question (`docs/01`), décision (registre doc 02 §19), idée retenue (`docs/99`) ou écart au cahier des charges qui touche une exigence est **aussi** consignée dans sa fiche, avec sa référence (Q.., D..). |
| F4 | **Revenir sur une décision** : nouvelle entrée de type « Décision » qui cite l'entrée ou la décision annulée (date, D.., Q..), explique **ce qui a changé** (fait nouveau, essai, retour terrain) et ce que cela implique. L'ancienne entrée reste visible. |
| F5 | Chaque commit qui touche une exigence ajoute une entrée « Développement » (hash + résumé) ; le corps du commit garde la ligne `Exigences : …`. |
| F6 | Le **statut** et la **remarque** en tête de fiche sont tenus à jour ; `python tools/matrice-exigences.py P0 P1 P2` régénère la matrice (doc 31) et l'index ci-dessous à partir des fiches. |
| F7 | La validation de l'utilisateur est une entrée « Validation » (par : Utilisateur) ; seule elle fait passer le statut à « Validé ». |

## 2. Statuts

| Statut | Sens |
|---|---|
| À faire | Fiche ouverte, rien de développé |
| En cours | Développement commencé |
| Partiel | Une partie est faite, le reste est expliqué dans la remarque (souvent dépendant d'une autre phase) |
| Réalisé | Fait, testé automatiquement ou vérifié par revue |
| Réalisé, à valider sur matériel | Fait, mais la preuve demande le matériel (Arduino, appareils) |
| Validé | Accepté par l'utilisateur (entrée « Validation ») |
| Reporté (Pn) | Volontairement repoussé à la phase indiquée, avec la raison |
| Non réalisé | Écarté pour l'instant (souvent priorité S), avec la raison |
| Abandonné | Retiré du cahier des charges (l'exigence garde son numéro, doc 02 §2.1) |

## 3. Types d'entrée d'historique

| Type | Par | Contenu |
|---|---|---|
| Création | Conception | Rédaction ou modification de l'exigence dans le cahier des charges |
| Question | Claude / Utilisateur | Question posée (avec son numéro Q..) |
| Réponse | Utilisateur | Réponse ou arbitrage |
| Décision | Claude / Utilisateur | Choix de réalisation ou d'interprétation, avec le **pourquoi** (et le D.. s'il est au registre) |
| Écart | Claude | Différence avec le cahier des charges, et où elle a été reportée |
| Développement | Claude | Commit (hash, résumé) |
| Test | Claude / Utilisateur | Mesure, essai, résultat |
| Validation | Utilisateur | Acceptation (ou refus motivé) |
| Note | Claude | Précision utile qui n'entre dans aucune autre catégorie |

## 4. Modèle de fiche

```markdown
# XXX-000 – Titre court

| Champ | Valeur |
|---|---|
| **Statut** | En cours |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc NN – section](../NN-document.md) |
| **Remarque** | — |
| **Liens** | Q.., D.., autres exigences |

## Description

> Énoncé de l'exigence (copié du cahier des charges).

**Critère d'acceptation** : …

## Réalisation

- `chemin/du/fichier.cs`

## Tests

- `ClasseDeTest.Methode`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| AAAA-MM-JJ | Conception | Création | Exigence rédigée au cahier des charges (doc NN). |
```

## 5. Index

> Généré par `python tools/matrice-exigences.py` à partir des fiches.

<!-- INDEX:DEBUT -->
| Fiche | Titre | Phase | Pri. | Statut |
|---|---|---|---|---|
| [BIB-001](BIB-001.md) | Modèle de données complet d'un appareil | P2 | I | Réalisé |
| [BIB-002](BIB-002.md) | Au moins un mode ; définitions de canaux partagées | P2 | I | Réalisé |
| [BIB-003](BIB-003.md) | Attribut 16 bits = un seul attribut sur deux canaux | P2 | I | Réalisé |
| [BIB-004](BIB-004.md) | Validation d'un modèle | P2 | I | Réalisé |
| [BIB-005](BIB-005.md) | Nombre de canaux et réglage sur l'appareil par mode | P2 | I | Réalisé |
| [BIB-006](BIB-006.md) | Intensité virtuelle et « Suit l'intensité » | P2 | I | Réalisé |
| [BIB-007](BIB-007.md) | Étiquettes de sûreté déduites et modifiables | P2 | I | Réalisé |
| [BIB-008](BIB-008.md) | Couleur des emplacements de roue | P2 | M | Réalisé |
| [BIB-009](BIB-009.md) | Version du modèle incrémentée | P2 | M | Réalisé |
| [BIB-010](BIB-010.md) | Dériver un modèle d'un autre | P2 | S | Réalisé |
| [BIB-020](BIB-020.md) | Liste par fabricant, recherche et filtres | P2 | I | Réalisé |
| [BIB-021](BIB-021.md) | Modes en onglets, canaux réordonnables | P2 | I | Réalisé |
| [BIB-022](BIB-022.md) | Tableau des plages et barre 0-255 | P2 | I | Réalisé |
| [BIB-023](BIB-023.md) | Saisie rapide de plages | P2 | I | Réalisé |
| [BIB-024](BIB-024.md) | Annuler / rétablir dans l'éditeur | P2 | I | Réalisé |
| [BIB-025](BIB-025.md) | Éditeur de roues | P2 | M | Réalisé |
| [BIB-026](BIB-026.md) | Aperçu de la fiche de réglage | P2 | M | Réalisé |
| [BIB-027](BIB-027.md) | Notice et photo liées au modèle | P2 | S | Partiel |
| [BIB-060](BIB-060.md) | Test en direct d'un modèle | P2 | I | Réalisé, à valider sur matériel |
| [BIB-061](BIB-061.md) | Clic sur une plage ou une borne | P2 | I | Réalisé, à valider sur matériel |
| [BIB-062](BIB-062.md) | Mode découverte | P2 | M | Réalisé |
| [BIB-063](BIB-063.md) | Sortie du test et nettoyage à la fermeture | P2 | M | Réalisé |
| [BIB-080](BIB-080.md) | Import Open Fixture Library | P2 | I | Réalisé |
| [BIB-081](BIB-081.md) | Import QLC+ | P2 | M | Réalisé |
| [BIB-082](BIB-082.md) | Rapport d'import | P2 | I | Réalisé |
| [BIB-083](BIB-083.md) | Import par lots sans figer l'interface | P2 | M | Réalisé |
| [BIB-084](BIB-084.md) | Export OFL | P2 | S | Non réalisé |
| [BIB-093](BIB-093.md) | Groupe fabricant non déplié après une recherche | P2 | S | À faire |
| [BIB-094](BIB-094.md) | LPC008S, plage « Fondu » du sélecteur de fonction à revoir | P2 | S | À faire |
| [BIB-095](BIB-095.md) | Tomshine Mini lyre gobo, définition refaite d'après la vraie notice | P2 | M | Réalisé à valider sur matériel |
| [BIB-096](BIB-096.md) | Test en direct : saisie d'une valeur au clavier | P2 | S | À faire |
| [BIB-097](BIB-097.md) | Barre de défilement horizontale trop fine | P2 | S | À faire |
| [CMD-020](CMD-020.md) | Commande SurchargerCanal | P1 | — | Réalisé |
| [CMD-022](CMD-022.md) | Commande LibérerSurcharges | P1 | — | Réalisé |
| [CMD-024](CMD-024.md) | Commande TesterSortie | P0 | — | Réalisé |
| [CONS-001](CONS-001.md) | Faders par pages | P1 | I | Réalisé |
| [CONS-002](CONS-002.md) | Modes de saisie des faders | P1 | I | Réalisé |
| [CONS-003](CONS-003.md) | Prise et libération d'un fader | P1 | I | Réalisé |
| [CONS-004](CONS-004.md) | Commandes de libération et de page | P1 | I | Réalisé |
| [CONS-005](CONS-005.md) | Le fader affiche la valeur émise | P1 | I | Réalisé |
| [CONS-006](CONS-006.md) | Sélection multiple de faders | P1 | I | Réalisé |
| [CONS-007](CONS-007.md) | Appareil, attribut et plage sur le fader | P1 | M | Reporté (P3) |
| [CONS-008](CONS-008.md) | Surcharges soumises au blackout et à la sûreté | P1 | M | Reporté (P4-P5) |
| [CONS-009](CONS-009.md) | Choix de l'univers | P1 | M | Réalisé |
| [CONS-010](CONS-010.md) | Instantanés de console | P1 | S | Réalisé |
| [CONS-040](CONS-040.md) | Moniteur de sortie 512 cases | P1 | I | Réalisé |
| [CONS-041](CONS-041.md) | Infos au survol du moniteur | P1 | I | Réalisé |
| [CONS-043](CONS-043.md) | Délimitation des appareils et surcharges dans le moniteur | P1 | M | Partiel |
| [CONS-044](CONS-044.md) | Moniteur dans une fenêtre séparée | P1 | S | Non réalisé |
| [CONS-060](CONS-060.md) | Composant « faders d'un appareil » réutilisable | P2 | I | Réalisé |
| [CONS-091](CONS-091.md) | Écart conservé au-delà des bornes en déplacement relatif multiple | P4 | I | À faire |
| [CONS-092](CONS-092.md) | Survol immédiat du moniteur (numéro de canal + cadre) | P3 | S | À faire |
| [GEN-001](GEN-001.md) | Moteur indépendant de l'interface et du matériel | P0 | I | Réalisé |
| [GEN-002](GEN-002.md) | Une seule porte d'entrée : les commandes | P0 | I | Réalisé |
| [GEN-003](GEN-003.md) | Atelier et Live indépendants | P0 | I | Réalisé |
| [GEN-010](GEN-010.md) | Commandes horodatées, avec origine, au tick suivant | P4 | I | Partiel |
| [GEN-011](GEN-011.md) | Ordre d'arrivée des commandes | P4 | I | Réalisé |
| [GEN-013](GEN-013.md) | Publication non bloquante des événements | P4 | I | Partiel |
| [GEN-020](GEN-020.md) | Valeurs internes normalisées 0-1 | P2 | I | Réalisé |
| [GEN-021](GEN-021.md) | Affichage dans l'unité la plus parlante | P2 | I | Réalisé |
| [GEN-030](GEN-030.md) | Tick à 40 Hz (25-44 Hz) | P0 | I | Réalisé, à valider sur matériel |
| [GEN-031](GEN-031.md) | Gigue du tick < 5 ms | P0 | I | Réalisé, à valider sur matériel |
| [GEN-032](GEN-032.md) | Calculs sur le temps écoulé réel | P4 | I | Réalisé |
| [GEN-033](GEN-033.md) | Horloges injectables | P4 | I | Réalisé |
| [GEN-050](GEN-050.md) | Fichiers JSON lisibles | P0 | I | Réalisé |
| [GEN-051](GEN-051.md) | Version de format et migrations | P0 | I | Réalisé |
| [GEN-052](GEN-052.md) | Identifiants stables | P2 | I | Réalisé |
| [GEN-056](GEN-056.md) | Fichier illisible sans plantage | P0 | I | Réalisé |
| [GEN-058](GEN-058.md) | Chemins relatifs (projet déplaçable) | P2 | S | Non réalisé |
| [GEN-060](GEN-060.md) | Blackout au démarrage | P0 | I | Réalisé |
| [GEN-061](GEN-061.md) | Fondu au noir à la fermeture | P5 | I | Partiel |
| [GEN-080](GEN-080.md) | Perte du PC : noir en 2 s | P0 | I | Réalisé, à valider sur matériel |
| [GEN-081](GEN-081.md) | Arrêt anormal de l'application : noir en 2 s | P0 | I | Réalisé, à valider sur matériel |
| [GEN-090](GEN-090.md) | Latence action → trame < 50 ms | P1 | I | Réalisé |
| [GEN-091](GEN-091.md) | Sortie déconnectée non bloquante, reconnexion < 3 s | P0 | I | Réalisé, à valider sur matériel |
| [GEN-093](GEN-093.md) | Panne d'un module secondaire isolée | P0 | I | Réalisé |
| [GEN-096](GEN-096.md) | Pas de mise en veille du PC pendant l'émission | P0 | I | Réalisé, à valider sur matériel |
| [GEN-100](GEN-100.md) | Interface en français | P1 | I | Réalisé |
| [GEN-101](GEN-101.md) | Thème sombre | P1 | I | Réalisé |
| [GEN-102](GEN-102.md) | Annuler / rétablir (50 niveaux minimum) | P2 | I | Réalisé |
| [GEN-103](GEN-103.md) | Confirmation des actions destructrices | P1 | I | Réalisé |
| [GEN-104](GEN-104.md) | Indicateur d'état permanent | P1 | I | Partiel |
| [GEN-105](GEN-105.md) | Recherche dans les longues listes | P2 | M | Réalisé |
| [GEN-107](GEN-107.md) | Glisser-déposer | P1 | M | Partiel |
| [GEN-108](GEN-108.md) | Taille de police réglable | P1 | S | Non réalisé |
| [GEN-109](GEN-109.md) | Opérations longues sans figer l'interface | P1 | I | Réalisé |
| [GEN-110](GEN-110.md) | Journal technique | P0 | I | Réalisé |
| [GEN-120](GEN-120.md) | Fonctionnement hors-ligne | P0 | I | Réalisé |
| [GEN-130](GEN-130.md) | Format des fichiers documenté | P4 | I | Partiel |
| [SORT-001](SORT-001.md) | Univers vers plusieurs pilotes | P0 | I | Réalisé |
| [SORT-002](SORT-002.md) | Pilotes indépendants | P0 | I | Réalisé |
| [SORT-003](SORT-003.md) | Seule la trame la plus récente | P0 | I | Réalisé |
| [SORT-004](SORT-004.md) | État publié par chaque pilote | P0 | I | Réalisé |
| [SORT-005](SORT-005.md) | Moteur actif sans aucune sortie | P0 | I | Réalisé |
| [SORT-006](SORT-006.md) | Configuration des sorties dans les préférences du poste | P0 | I | Réalisé |
| [SORT-007](SORT-007.md) | Écran « Sorties » et test de sortie | P0 | M | Réalisé |
| [SORT-008](SORT-008.md) | Canaux maintenus pendant le test de sortie | P3 | M | À faire |
| [SORT-010](SORT-010.md) | Détection automatique de l'Arduino | P0 | I | Réalisé, à valider sur matériel |
| [SORT-011](SORT-011.md) | Dernier port essayé en premier | P0 | I | Réalisé |
| [SORT-012](SORT-012.md) | Jamais 1200 bauds, DTR actif | P0 | I | Réalisé |
| [SORT-013](SORT-013.md) | Reconnexion automatique | P0 | I | Réalisé, à valider sur matériel |
| [SORT-014](SORT-014.md) | Port imposé manuellement | P0 | M | Réalisé |
| [SORT-015](SORT-015.md) | Version du firmware | P0 | M | Réalisé |
| [SORT-020](SORT-020.md) | Trame complète à chaque tick | P0 | I | Réalisé |
| [SORT-021](SORT-021.md) | Nombre de canaux émis réglable | P0 | M | Réalisé |
| [SORT-022](SORT-022.md) | Erreur d'écriture sans effet sur le moteur | P0 | I | Réalisé |
| [SORT-023](SORT-023.md) | Mesures du pilote (durée, trames/s) | P0 | M | Réalisé |
| [SORT-040](SORT-040.md) | Rafraîchissement DMX continu par le firmware | P0 | I | Réalisé, à valider sur matériel |
| [SORT-041](SORT-041.md) | Canaux à 0 au démarrage de la carte | P0 | I | Réalisé, à valider sur matériel |
| [SORT-042](SORT-042.md) | Chien de garde du firmware (2 s) | P0 | I | Réalisé, à valider sur matériel |
| [SORT-043](SORT-043.md) | Messages du protocole pris en charge | P0 | I | Réalisé, à valider sur matériel |
| [SORT-044](SORT-044.md) | Firmware sans allocation dynamique | P0 | I | Réalisé |
| [SORT-045](SORT-045.md) | Message appliqué seulement s'il est complet | P0 | I | Réalisé, à valider sur matériel |
| [SORT-046](SORT-046.md) | LED d'état de la carte | P0 | M | Réalisé, à valider sur matériel |
| [SORT-047](SORT-047.md) | Version unique du firmware | P0 | M | Réalisé |
| [SORT-048](SORT-048.md) | Firmware versionné et documenté | P0 | M | Réalisé |
| [SORT-049](SORT-049.md) | Trame DMX ajustée au nombre de canaux reçus | P0 | S | Réalisé, à valider sur matériel |
| [SORT-060](SORT-060.md) | Enregistreur de trames | P0 | I | Réalisé |
| [SORT-061](SORT-061.md) | Enregistreur activable à chaud | P0 | I | Réalisé |
<!-- INDEX:FIN -->
