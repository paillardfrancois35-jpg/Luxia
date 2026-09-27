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
| [BIB-093](BIB-093.md) | Groupe fabricant non déplié après une recherche | P2 | S | Réalisé |
| [BIB-094](BIB-094.md) | LPC008S, plage « Fondu » du sélecteur de fonction à revoir | P2 | S | Partiel |
| [BIB-095](BIB-095.md) | Tomshine Mini lyre gobo, définition refaite d'après la vraie notice | P2 | M | Validé |
| [BIB-096](BIB-096.md) | Test en direct : saisie d'une valeur au clavier | P2 | S | Réalisé |
| [BIB-097](BIB-097.md) | Barre de défilement horizontale trop fine | P2 | S | Réalisé |
| [BIB-098](BIB-098.md) | Import : proposer d'écraser un modèle déjà présent | P3 | S | Réalisé |
| [BIB-099](BIB-099.md) | Mode découverte : ergonomie de « Nouvelle plage ici » | P2 | M | Partiel |
| [BIB-100](BIB-100.md) | Signaler visiblement une erreur de validation et amener sur l'onglet concerné | P2 | M | Réalisé |
| [BIB-101](BIB-101.md) | Plage « Pas de strobe » du LPC008S, du LPC120 et de la LCB803 | P5 | I | Réalisé |
| [CMD-001](CMD-001.md) | Commande Blackout | P4 | — | Réalisé |
| [CMD-002](CMD-002.md) | Commande RéglerGrandMaster | P4 | — | Réalisé |
| [CMD-003](CMD-003.md) | Commande Figer | P5 | I | Réalisé |
| [CMD-010](CMD-010.md) | Commande LancerScène | P4 | — | Réalisé |
| [CMD-011](CMD-011.md) | Commande ArrêterScène | P4 | — | Validé |
| [CMD-012](CMD-012.md) | Commande ArrêterCouche | P5 | — | Réalisé |
| [CMD-013](CMD-013.md) | Commande RéglerMasterCouche | P5 | — | Réalisé |
| [CMD-014](CMD-014.md) | Commande FlashScène | P5 | I | Réalisé |
| [CMD-015](CMD-015.md) | Commandes ÉtapeSuivante / ÉtapePrécédente | P4 | — | Réalisé |
| [CMD-016](CMD-016.md) | Commande RéglerVitesseScène | P5 | — | Réalisé |
| [CMD-020](CMD-020.md) | Commande SurchargerCanal | P1 | — | Réalisé |
| [CMD-021](CMD-021.md) | Commande SurchargerAttribut | P4 | — | Validé |
| [CMD-022](CMD-022.md) | Commande LibérerSurcharges | P1 | — | Réalisé |
| [CMD-023](CMD-023.md) | Commande IdentifierAppareil | P3 | — | Réalisé |
| [CMD-024](CMD-024.md) | Commande TesterSortie | P0 | — | Réalisé |
| [CMD-030](CMD-030.md) | Commande Fumée | P5 | I | Réalisé, à valider sur matériel |
| [CONS-001](CONS-001.md) | Faders par pages | P1 | I | Réalisé |
| [CONS-002](CONS-002.md) | Modes de saisie des faders | P1 | I | Réalisé |
| [CONS-003](CONS-003.md) | Prise et libération d'un fader | P1 | I | Réalisé |
| [CONS-004](CONS-004.md) | Commandes de libération et de page | P1 | I | Réalisé |
| [CONS-005](CONS-005.md) | Le fader affiche la valeur émise | P1 | I | Réalisé |
| [CONS-006](CONS-006.md) | Sélection multiple de faders | P1 | I | Réalisé |
| [CONS-007](CONS-007.md) | Appareil, attribut et plage sur le fader | P1 | M | Réalisé |
| [CONS-008](CONS-008.md) | Surcharges soumises au blackout et à la sûreté | P1 | M | Partiel |
| [CONS-009](CONS-009.md) | Choix de l'univers | P1 | M | Réalisé |
| [CONS-010](CONS-010.md) | Instantanés de console | P1 | S | Réalisé |
| [CONS-020](CONS-020.md) | Faders regroupés par appareil patché | P3 | I | Réalisé |
| [CONS-021](CONS-021.md) | Outil adapté par type d'attribut | P3 | I | Partiel |
| [CONS-022](CONS-022.md) | Surcharge d'attribut soumise à la chaîne de rendu | P3 | I | Validé |
| [CONS-023](CONS-023.md) | Clic sur une plage et balayage | P3 | M | Réalisé |
| [CONS-024](CONS-024.md) | Bouton Identifier par appareil | P3 | M | Réalisé |
| [CONS-025](CONS-025.md) | Capturer les surcharges dans une scène | P4 | M | Réalisé |
| [CONS-040](CONS-040.md) | Moniteur de sortie 512 cases | P1 | I | Réalisé |
| [CONS-041](CONS-041.md) | Infos au survol du moniteur | P1 | I | Réalisé |
| [CONS-042](CONS-042.md) | Origine de la valeur au survol du moniteur | P4 | M | Validé |
| [CONS-043](CONS-043.md) | Délimitation des appareils et surcharges dans le moniteur | P1 | M | Réalisé |
| [CONS-044](CONS-044.md) | Moniteur dans une fenêtre séparée | P1 | S | Non réalisé |
| [CONS-060](CONS-060.md) | Composant « faders d'un appareil » réutilisable | P2 | I | Réalisé |
| [CONS-061](CONS-061.md) | Une page de console peut être affectée aux faders d'un APC mini | P5 | S | Reporté (chantier ergonomie) |
| [CONS-091](CONS-091.md) | Écart conservé au-delà des bornes en déplacement relatif multiple | P4 | I | Validé |
| [CONS-092](CONS-092.md) | Survol immédiat du moniteur (numéro de canal + cadre) | P3 | S | Réalisé |
| [COU-001](COU-001.md) | Créer, renommer, réordonner | P5 | I | Réalisé |
| [COU-002](COU-002.md) | Une scène appartient à une couche | P5 | I | Réalisé |
| [COU-003](COU-003.md) | Exclusivité | P5 | I | Validé |
| [COU-004](COU-004.md) | Couche non exclusive | P5 | I | Réalisé |
| [COU-005](COU-005.md) | Couche de type Flash | P5 | I | Validé |
| [COU-006](COU-006.md) | Modèle de couches par défaut pour un nouveau projet | P5 | I | Réalisé |
| [COU-007](COU-007.md) | Arrêter la couche | P5 | M | Réalisé |
| [COU-008](COU-008.md) | Avertissement | P5 | M | Réalisé |
| [COU-009](COU-009.md) | Scène de repos par couche | P5 | S | Réalisé |
| [GEN-001](GEN-001.md) | Moteur indépendant de l'interface et du matériel | P0 | I | Réalisé |
| [GEN-002](GEN-002.md) | Une seule porte d'entrée : les commandes | P0 | I | Réalisé |
| [GEN-003](GEN-003.md) | Atelier et Live indépendants | P0 | I | Réalisé |
| [GEN-004](GEN-004.md) | Composant d'édition réutilisable dans un autre écran | P3 | M | Réalisé |
| [GEN-010](GEN-010.md) | Commandes horodatées, avec origine, au tick suivant | P4 | I | Réalisé |
| [GEN-011](GEN-011.md) | Ordre d'arrivée des commandes | P4 | I | Réalisé |
| [GEN-012](GEN-012.md) | Commande refusée : événement avec le motif | P4 | M | Réalisé |
| [GEN-013](GEN-013.md) | Publication non bloquante des événements | P4 | I | Réalisé |
| [GEN-020](GEN-020.md) | Valeurs internes normalisées 0-1 | P2 | I | Réalisé |
| [GEN-021](GEN-021.md) | Affichage dans l'unité la plus parlante | P2 | I | Réalisé |
| [GEN-022](GEN-022.md) | Couleurs logiques converties selon les émetteurs | P4 | I | Réalisé |
| [GEN-023](GEN-023.md) | Durées en secondes ou en temps musicaux | P4 | I | Partiel |
| [GEN-030](GEN-030.md) | Tick à 40 Hz (25-44 Hz) | P0 | I | Réalisé, à valider sur matériel |
| [GEN-031](GEN-031.md) | Gigue du tick < 5 ms | P0 | I | Réalisé, à valider sur matériel |
| [GEN-032](GEN-032.md) | Calculs sur le temps écoulé réel | P4 | I | Réalisé |
| [GEN-033](GEN-033.md) | Horloges injectables | P4 | I | Réalisé |
| [GEN-040](GEN-040.md) | Chaîne de rendu appliquée dans l'ordre, à chaque tick | P4 | I | Réalisé |
| [GEN-041](GEN-041.md) | Blackout et Grand Master sur les seules intensités | P4 | I | Validé |
| [GEN-042](GEN-042.md) | Surcharges brutes soumises au blackout et à la sûreté | P4 | I | Réalisé |
| [GEN-043](GEN-043.md) | Chaîne de rendu explicable | P4 | M | Validé |
| [GEN-050](GEN-050.md) | Fichiers JSON lisibles | P0 | I | Réalisé |
| [GEN-051](GEN-051.md) | Version de format et migrations | P0 | I | Réalisé |
| [GEN-052](GEN-052.md) | Identifiants stables | P2 | I | Réalisé |
| [GEN-053](GEN-053.md) | Copie des modèles d'appareils dans le projet | P3 | I | Réalisé |
| [GEN-054](GEN-054.md) | Sauvegarde automatique du projet ouvert | P5 | I | Réalisé |
| [GEN-055](GEN-055.md) | Conservation des N dernières versions du projet | P5 | M | Réalisé |
| [GEN-056](GEN-056.md) | Fichier illisible sans plantage | P0 | I | Réalisé |
| [GEN-057](GEN-057.md) | Export / import d'un projet complet sous forme d'archive unique | P5 | M | Reporté (chantier ergonomie) |
| [GEN-058](GEN-058.md) | Chemins relatifs (projet déplaçable) | P2 | S | Non réalisé |
| [GEN-060](GEN-060.md) | Blackout au démarrage | P0 | I | Réalisé |
| [GEN-061](GEN-061.md) | Fondu au noir à la fermeture | P5 | I | Réalisé, à valider sur matériel |
| [GEN-062](GEN-062.md) | Le passage Atelier ↔ Live ne doit jamais interrompre la restitution en cours | P5 | I | Réalisé |
| [GEN-063](GEN-063.md) | Mode aveugle en Atelier | P4 | M | Validé |
| [GEN-064](GEN-064.md) | Démarrage jusqu'à « prêt en Live » en moins de 10 s | P5 | I | Réalisé, à valider sur matériel |
| [GEN-070](GEN-070.md) | Toute entrée | P5 | I | Réalisé |
| [GEN-071](GEN-071.md) | Raccourcis clavier globaux en Live, actifs quel que soit le focus | P5 | I | Partiel |
| [GEN-072](GEN-072.md) | Les deux modèles d'APC mini sont reconnus automatiquement et peuvent être branchés simulta | P5 | M | Réalisé, à valider sur matériel |
| [GEN-073](GEN-073.md) | Débrancher / rebrancher un contrôleur MIDI en cours de soirée est géré sans redémarrage | P5 | M | Réalisé, à valider sur matériel |
| [GEN-074](GEN-074.md) | Les affectations MIDI sont modifiables par « apprentissage » | P5 | S | Reporté (chantier ergonomie) |
| [GEN-080](GEN-080.md) | Perte du PC : noir en 2 s | P0 | I | Réalisé, à valider sur matériel |
| [GEN-081](GEN-081.md) | Arrêt anormal de l'application : noir en 2 s | P0 | I | Réalisé, à valider sur matériel |
| [GEN-082](GEN-082.md) | Blackout accessible en permanence | P4 | I | Validé |
| [GEN-083](GEN-083.md) | Strobe | P5 | I | Réalisé |
| [GEN-084](GEN-084.md) | Fumée | P5 | I | Réalisé, à valider sur matériel |
| [GEN-085](GEN-085.md) | Zones interdites Pan/Tilt par lieu et par lyre | P5 | I | Réalisé |
| [GEN-086](GEN-086.md) | Un signal visuel permanent en Live indique toute limite de sûreté active ou tout verrou | P5 | M | Réalisé |
| [GEN-090](GEN-090.md) | Latence action → trame < 50 ms | P1 | I | Réalisé |
| [GEN-091](GEN-091.md) | Sortie déconnectée non bloquante, reconnexion < 3 s | P0 | I | Réalisé, à valider sur matériel |
| [GEN-093](GEN-093.md) | Panne d'un module secondaire isolée | P0 | I | Réalisé |
| [GEN-094](GEN-094.md) | Utilisation CPU moyenne < 15 % en Live | P5 | M | Réalisé, à valider sur matériel |
| [GEN-095](GEN-095.md) | Reprise après plantage | P5 | M | Réalisé |
| [GEN-096](GEN-096.md) | Pas de mise en veille du PC pendant l'émission | P0 | I | Réalisé, à valider sur matériel |
| [GEN-100](GEN-100.md) | Interface en français | P1 | I | Réalisé |
| [GEN-101](GEN-101.md) | Thème sombre | P1 | I | Réalisé |
| [GEN-102](GEN-102.md) | Annuler / rétablir (50 niveaux minimum) | P2 | I | Réalisé |
| [GEN-103](GEN-103.md) | Confirmation des actions destructrices | P1 | I | Réalisé |
| [GEN-104](GEN-104.md) | Indicateur d'état permanent | P1 | I | Partiel |
| [GEN-105](GEN-105.md) | Recherche dans les longues listes | P2 | M | Réalisé |
| [GEN-106](GEN-106.md) | Nom, couleur et icône des objets | P4 | M | Réalisé |
| [GEN-107](GEN-107.md) | Glisser-déposer | P1 | M | Partiel |
| [GEN-108](GEN-108.md) | Taille de police réglable | P1 | S | Non réalisé |
| [GEN-109](GEN-109.md) | Opérations longues sans figer l'interface | P1 | I | Réalisé |
| [GEN-110](GEN-110.md) | Journal technique | P0 | I | Réalisé |
| [GEN-112](GEN-112.md) | Journal des commandes consultable | P4 | M | Réalisé |
| [GEN-113](GEN-113.md) | Enregistrement des trames d'une session | P4 | S | Partiel |
| [GEN-114](GEN-114.md) | Menu « À propos » avec diagnostic copiable | P3 | M | Réalisé |
| [GEN-115](GEN-115.md) | Une seule instance de l'application à la fois | P3 | I | Réalisé |
| [GEN-116](GEN-116.md) | Icône de l'application | P3 | S | Réalisé |
| [GEN-117](GEN-117.md) | Toute exception journalisée | P4 | I | Réalisé |
| [GEN-118](GEN-118.md) | Enregistrement robuste aux refus passagers | P4 | I | Réalisé |
| [GEN-119](GEN-119.md) | Numéro de compilation affiché en développement | P4 | M | Réalisé |
| [GEN-120](GEN-120.md) | Fonctionnement hors-ligne | P0 | I | Réalisé |
| [GEN-122](GEN-122.md) | Sorties réseau locales autorisées (Art-Net) | P3 | M | Réalisé |
| [GEN-130](GEN-130.md) | Format des fichiers documenté | P4 | I | Réalisé |
| [GEN-131](GEN-131.md) | Outil de validation d'un projet | P4 | I | Validé |
| [GEN-132](GEN-132.md) | Outil qui joue une scène et la résume | P4 | M | Validé |
| [GEN-133](GEN-133.md) | Contenu généré rangé à part, jamais écrasant | P4 | I | Réalisé |
| [INST-001](INST-001.md) | Un ou plusieurs univers, numérotés et nommables | P3 | I | Réalisé |
| [INST-002](INST-002.md) | Lien univers → pilotes dans les préférences | P3 | I | Réalisé |
| [INST-003](INST-003.md) | Vue barre d'univers | P3 | I | Réalisé |
| [INST-010](INST-010.md) | Ajouter un appareil au patch | P3 | I | Réalisé |
| [INST-011](INST-011.md) | Ajout multiple d'appareils identiques | P3 | I | Réalisé |
| [INST-012](INST-012.md) | Première adresse libre proposée | P3 | I | Réalisé |
| [INST-013](INST-013.md) | Détection des chevauchements en temps réel | P3 | I | Réalisé |
| [INST-014](INST-014.md) | Doublon volontaire (jumeaux) | P3 | M | Réalisé |
| [INST-015](INST-015.md) | Déplacer un appareil | P3 | I | Réalisé |
| [INST-016](INST-016.md) | Changer le mode d'un appareil patché | P3 | I | Validé |
| [INST-017](INST-017.md) | Nom, couleur et numéro court | P3 | I | Réalisé |
| [INST-018](INST-018.md) | Fiche d'installation | P3 | I | Réalisé |
| [INST-019](INST-019.md) | Identifier un appareil / chenillard d'identification | P3 | I | Réalisé |
| [INST-020](INST-020.md) | Supprimer un appareil | P3 | M | Réalisé |
| [INST-021](INST-021.md) | Options de montage par appareil | P3 | M | Partiel |
| [INST-030](INST-030.md) | Sélection manuelle ordonnée | P3 | I | Réalisé |
| [INST-031](INST-031.md) | Sélections automatiques | P3 | I | Réalisé |
| [INST-032](INST-032.md) | Sélections manuelles créées et réordonnées | P3 | I | Partiel |
| [INST-033](INST-033.md) | Opérations d'ordre sur une sélection | P3 | M | Réalisé |
| [INST-034](INST-034.md) | Sélection de cellules | P3 | M | Non réalisé |
| [INST-050](INST-050.md) | Créer, dupliquer, activer un lieu | P3 | I | Réalisé |
| [INST-051](INST-051.md) | Éditeur de plan | P3 | I | Partiel |
| [INST-052](INST-052.md) | Appareil absent | P3 | I | Réalisé |
| [INST-053](INST-053.md) | Zones interdites par lyre, définies en visant à la main | P5 | I | Réalisé |
| [INST-054](INST-054.md) | Les palettes de position sont stockées par lieu | P5 | I | Réalisé |
| [INST-070](INST-070.md) | L'assistant enchaîne les étapes ci-dessus, chacune pouvant être passée | P5 | M | Reporté (chantier ergonomie) |
| [INST-071](INST-071.md) | Test appareil par appareil avec résultat | P5 | M | Reporté (chantier ergonomie) |
| [INST-072](INST-072.md) | Calibration des positions | P5 | M | Partiel |
| [LIVE-001](LIVE-001.md) | Bandeau d'état permanent | P5 | I | Partiel |
| [LIVE-002](LIVE-002.md) | Colonnes de couches | P5 | I | Réalisé |
| [LIVE-003](LIVE-003.md) | Un clic sur une scène la lance | P5 | I | Validé |
| [LIVE-004](LIVE-004.md) | Actions permanentes toujours visibles | P5 | I | Validé |
| [LIVE-005](LIVE-005.md) | Palettes rapides | P5 | I | Réalisé |
| [LIVE-006](LIVE-006.md) | Disposition personnalisable | P5 | M | Reporté (chantier ergonomie) |
| [LIVE-007](LIVE-007.md) | Mini-simulateur optionnel dans l'écran Live | P5 | M | Reporté (chantier ergonomie) |
| [LIVE-008](LIVE-008.md) | Indication visible de toute limite de sûreté active et de tout verrou | P5 | I | Réalisé |
| [LIVE-009](LIVE-009.md) | Journal défilant des derniers événements | P5 | M | Réalisé |
| [LIVE-010](LIVE-010.md) | Alerte non bloquante et visible si la sortie est déconnectée ou si un module est en erreur | P5 | I | Réalisé, à valider sur matériel |
| [LIVE-011](LIVE-011.md) | Accès à l'assistant d'installation | P5 | M | Reporté (chantier ergonomie) |
| [LIVE-040](LIVE-040.md) | Raccourcis du tableau ci-dessus | P5 | I | Partiel |
| [LIVE-041](LIVE-041.md) | Raccourcis personnalisables | P5 | S | Reporté (chantier ergonomie) |
| [LIVE-060](LIVE-060.md) | L'écran Live se rafraîchit à ≥ 20 images/s sans affecter le moteur | P5 | I | Réalisé |
| [LIVE-061](LIVE-061.md) | Latence clic → sortie < 50 ms | P5 | I | Réalisé, à valider sur matériel |
| [MIDI-001](MIDI-001.md) | Détection automatique des APC mini MK1 et MK2 | P5 | I | Réalisé, à valider sur matériel |
| [MIDI-002](MIDI-002.md) | Affectation par défaut du §3 | P5 | I | Réalisé |
| [MIDI-003](MIDI-003.md) | Retour lumineux du §4, mis à jour à chaque changement d'état | P5 | I | Réalisé, à valider sur matériel |
| [MIDI-004](MIDI-004.md) | Reprise douce des faders | P5 | I | Réalisé |
| [MIDI-005](MIDI-005.md) | Les deux contrôleurs peuvent être branchés simultanément, avec des affectations différente | P5 | I | Réalisé |
| [MIDI-006](MIDI-006.md) | Débranchement / rebranchement à chaud | P5 | I | Réalisé, à valider sur matériel |
| [MIDI-007](MIDI-007.md) | Affectations modifiables et enregistrées dans le projet | P5 | M | Réalisé |
| [MIDI-008](MIDI-008.md) | Apprentissage | P5 | S | Reporté (chantier ergonomie) |
| [MIDI-009](MIDI-009.md) | Disposition alternative « palettes » | P5 | S | Reporté (chantier ergonomie) |
| [MIDI-010](MIDI-010.md) | Sur MK2, la couleur des pads reprend la couleur des scènes | P5 | M | Réalisé, à valider sur matériel |
| [MIDI-011](MIDI-011.md) | Blackout du contrôleur tant que maintenu (comme Daslight) | P5 | I | Réalisé, à valider sur matériel |
| [MOT-001](MOT-001.md) | Ordre de la boucle de rendu | P4 | I | Réalisé |
| [MOT-002](MOT-002.md) | Budget de 5 ms par tick | P4 | I | Réalisé |
| [MOT-003](MOT-003.md) | Fil d'exécution dédié, sans opération bloquante | P4 | I | Réalisé |
| [MOT-004](MOT-004.md) | Déterminisme : aléatoire à graine journalisée | P4 | I | Réalisé |
| [MOT-010](MOT-010.md) | Étape = fondu d'entrée + maintien | P4 | I | Validé |
| [MOT-011](MOT-011.md) | Interpolation des attributs continus selon la courbe | P4 | I | Validé |
| [MOT-012](MOT-012.md) | Attributs discrets : bascule franche | P4 | I | Validé |
| [MOT-013](MOT-013.md) | Modes de boucle | P4 | I | Validé |
| [MOT-014](MOT-014.md) | Fin de scène : arrêt, maintien, enchaînement | P4 | I | Validé |
| [MOT-015](MOT-015.md) | Vitesse de lecture | P4 | I | Validé |
| [MOT-019](MOT-019.md) | Pas à pas : étape suivante / précédente | P4 | M | Réalisé |
| [MOT-030](MOT-030.md) | Fondu croisé dans une couche exclusive | P4 | I | Validé |
| [MOT-031](MOT-031.md) | Fusion entre couches et modes d'intensité | P4 | I | Réalisé |
| [MOT-032](MOT-032.md) | Attribut non touché = valeur par défaut | P4 | I | Validé |
| [MOT-033](MOT-033.md) | Master de couche | P4 | I | Réalisé |
| [MOT-034](MOT-034.md) | Source de chaque valeur finale | P4 | M | Réalisé |
| [MOT-040](MOT-040.md) | « Suit l'intensité » en fin de chaîne | P4 | I | Validé |
| [MOT-041](MOT-041.md) | « Allumer en coloriant » | P4 | I | Validé |
| [MOT-042](MOT-042.md) | Le modèle de couches par défaut | P5 | I | Validé |
| [MOT-050](MOT-050.md) | Couleur logique vers RVB | P4 | I | Réalisé |
| [MOT-051](MOT-051.md) | Couleur logique vers RVBW (extraction du blanc) | P4 | I | Réalisé |
| [MOT-052](MOT-052.md) | Couleur logique vers roue de couleur | P4 | I | Validé |
| [MOT-053](MOT-053.md) | UV et ambre en émetteurs indépendants | P4 | M | Réalisé |
| [MOT-054](MOT-054.md) | Interpolation des couleurs sans teintes « sales » | P4 | M | Non réalisé |
| [MOT-070](MOT-070.md) | Blackout | P4 | I | Validé |
| [MOT-071](MOT-071.md) | Grand Master | P4 | I | Validé |
| [MOT-072](MOT-072.md) | Flash | P5 | I | Validé |
| [MOT-073](MOT-073.md) | Figer | P5 | I | Réalisé |
| [MOT-074](MOT-074.md) | Surcharges conformes à la chaîne de rendu | P1 | I | Réalisé |
| [MOT-075](MOT-075.md) | Identifier un appareil au-dessus de tout | P3 | I | Réalisé |
| [MOT-080](MOT-080.md) | Limiteur de strobe | P5 | I | Réalisé |
| [MOT-081](MOT-081.md) | Limiteur de fumée | P5 | I | Réalisé, à valider sur matériel |
| [MOT-082](MOT-082.md) | Zones interdites | P5 | I | Partiel |
| [MOT-083](MOT-083.md) | Toute intervention d'un limiteur publie LimiteSécuritéAtteinte | P5 | I | Réalisé |
| [MOT-090](MOT-090.md) | Conversion des attributs en octets selon le patch | P4 | I | Réalisé |
| [MOT-091](MOT-091.md) | Appareils absents émis à 0 | P4 | I | Réalisé |
| [MOT-092](MOT-092.md) | Jumeaux : mêmes valeurs | P4 | I | Réalisé |
| [MOT-093](MOT-093.md) | Une trame par univers à chaque tick | P4 | I | Réalisé |
| [MOT-100](MOT-100.md) | Publication de l'état observable | P4 | I | Réalisé |
| [MOT-101](MOT-101.md) | Événements de scène et de refus | P4 | I | Réalisé |
| [MOT-102](MOT-102.md) | Instantané de reprise | P5 | M | Réalisé |
| [MOT-103](MOT-103.md) | Mode sans interface piloté par scénario | P4 | M | Réalisé |
| [PAL-001](PAL-001.md) | Créer une palette depuis le programmeur | P4 | I | Réalisé |
| [PAL-002](PAL-002.md) | Palettes couleur par intention | P4 | I | Validé |
| [PAL-003](PAL-003.md) | Palettes automatiques | P4 | I | Réalisé |
| [PAL-004](PAL-004.md) | Palettes de position par lieu | P5 | I | Réalisé |
| [PAL-005](PAL-005.md) | Les scènes suivent les palettes | P4 | I | Validé |
| [PAL-006](PAL-006.md) | Suppression d'une palette utilisée | P4 | I | Réalisé |
| [PAL-007](PAL-007.md) | Grilles de palettes | P4 | M | Partiel |
| [PAL-008](PAL-008.md) | Palettes de position manquantes dans un lieu | P5 | M | Réalisé |
| [PAL-009](PAL-009.md) | Jeu de palettes couleur par défaut | P4 | M | Réalisé |
| [PAL-010](PAL-010.md) | Palettes de combinaisons de couleurs | P5 | S | Reporté (P6) |
| [SCN-001](SCN-001.md) | Créer, dupliquer, renommer, supprimer une scène | P4 | I | Validé |
| [SCN-002](SCN-002.md) | Étapes : ajouter, insérer, dupliquer, supprimer, réordonner | P4 | I | Validé |
| [SCN-003](SCN-003.md) | Durées d'une étape et courbe | P4 | I | Validé |
| [SCN-004](SCN-004.md) | Modification groupée des durées | P4 | I | Réalisé |
| [SCN-005](SCN-005.md) | Paramètres de lecture d'une scène | P4 | I | Réalisé |
| [SCN-007](SCN-007.md) | Cibles : appareil, cellule, sélection | P4 | I | Validé |
| [SCN-008](SCN-008.md) | Valeur directe, palette ou plage | P4 | I | Réalisé |
| [SCN-009](SCN-009.md) | Drapeau « Visible en Live » | P4 | I | Réalisé |
| [SCN-010](SCN-010.md) | Retard par membre (« fan ») | P4 | M | Validé |
| [SCN-011](SCN-011.md) | Fondu propre à un attribut | P4 | M | Réalisé |
| [SCN-012](SCN-012.md) | Catégories et filtre des scènes | P4 | M | Réalisé |
| [SCN-013](SCN-013.md) | Rapport des utilisations d'une scène | P4 | M | Réalisé |
| [SCN-030](SCN-030.md) | Sélection d'appareils dans le programmeur | P4 | I | Partiel |
| [SCN-031](SCN-031.md) | Outils d'attributs adaptés à la sélection | P4 | I | Partiel |
| [SCN-032](SCN-032.md) | Seuls les attributs modifiés sont enregistrés | P4 | I | Validé |
| [SCN-033](SCN-033.md) | Enregistrer : remplacer, fusionner, nouvelle étape | P4 | I | Validé |
| [SCN-034](SCN-034.md) | Tester : seule ou dans son contexte | P4 | I | Validé |
| [SCN-035](SCN-035.md) | Aveugle | P4 | I | Validé |
| [SCN-036](SCN-036.md) | Option « allumer en coloriant » | P4 | I | Validé |
| [SCN-037](SCN-037.md) | Enregistrer depuis la sortie | P4 | M | Réalisé |
| [SCN-038](SCN-038.md) | Copier / coller, miroir | P4 | M | Réalisé |
| [SCN-039](SCN-039.md) | Annuler / rétablir dans l'éditeur de scènes | P4 | I | Validé |
| [SIM-001](SIM-001.md) | Affichage du plan du lieu actif | P3 | I | Réalisé |
| [SIM-002](SIM-002.md) | Rendu 30 images/s sans ralentir le moteur | P3 | I | Réalisé |
| [SIM-003](SIM-003.md) | Décodage des trames via le patch | P3 | I | Réalisé |
| [SIM-004](SIM-004.md) | Faisceau des lyres | P3 | I | Réalisé |
| [SIM-005](SIM-005.md) | Survol / clic sur un appareil | P3 | I | Réalisé |
| [SIM-006](SIM-006.md) | Choix de la source affiché en permanence | P3 | I | Partiel |
| [SIM-007](SIM-007.md) | Fenêtre détachable et plein écran | P3 | M | Non réalisé |
| [SIM-008](SIM-008.md) | Zones interdites et repères du lieu | P3 | M | Non réalisé |
| [SIM-009](SIM-009.md) | Appareils identifiés et en erreur mis en évidence | P3 | M | Réalisé |
| [SIM-010](SIM-010.md) | Sélection au clic / au lasso | P3 | M | Non réalisé |
| [SIM-012](SIM-012.md) | Protection photosensible (strobe) | P3 | I | Réalisé |
| [SIM-013](SIM-013.md) | Vue de face | P3 | S | Non réalisé |
| [SORT-001](SORT-001.md) | Univers vers plusieurs pilotes | P0 | I | Réalisé |
| [SORT-002](SORT-002.md) | Pilotes indépendants | P0 | I | Réalisé |
| [SORT-003](SORT-003.md) | Seule la trame la plus récente | P0 | I | Réalisé |
| [SORT-004](SORT-004.md) | État publié par chaque pilote | P0 | I | Réalisé |
| [SORT-005](SORT-005.md) | Moteur actif sans aucune sortie | P0 | I | Réalisé |
| [SORT-006](SORT-006.md) | Configuration des sorties dans les préférences du poste | P0 | I | Réalisé |
| [SORT-007](SORT-007.md) | Écran « Sorties » et test de sortie | P0 | M | Réalisé |
| [SORT-008](SORT-008.md) | Canaux maintenus pendant le test de sortie | P3 | M | Réalisé |
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
| [SORT-062](SORT-062.md) | Pilote Simulateur | P3 | I | Réalisé |
| [SORT-063](SORT-063.md) | Lecteur d'enregistrements | P3 | S | Non réalisé |
| [SORT-064](SORT-064.md) | Pilote Art-Net | P3 | S | Non réalisé |
| [SORT-065](SORT-065.md) | Enregistrement des trames visible, chemin copiable | P5 | M | Réalisé |
| [SORT-066](SORT-066.md) | Journal de l'enregistrement : actions, commandes et canaux entrelacés | P5 | M | Réalisé |
<!-- INDEX:FIN -->
