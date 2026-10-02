# Résultats d'essai – P8 Show & séquences

> Fichier de résultats de la phase P8 (procédure : [doc 33](../33-procedure-essais.md)). Guide :
> [demos/P8-show-et-sequences.md](../demos/P8-show-et-sequences.md). Écrit par la discussion **test** en ajout seul ; lu par la
> discussion **dev**, qui corrige et tient les fiches d'exigences.

| Date | Version | Exemple | Résultat | Observation de l'utilisateur | Demande / anomalie |
|---|---|---|---|---|---|
| 2026-10-02 | v1.010.079 | 0 Préparation | ✅ | « tout ok » | — |
| 2026-10-02 | v1.010.079 | 1 Montée 16 mesures | ✅ | « tout ok » | — |
| 2026-10-02 | v1.010.079 | 2a Groove : une couleur par mesure | ❌ | « On reste sur du bleu pour les PAR » | Hypothèse (lecture de `scènes.json` / `séquences.json`, rien modifié) : la scène « Vague sur les PAR » (couche 7c1a0001-…-05) impose la palette « Bleu » (9a1e0001-…-09) aux PAR et masque la scène « Couleur à chaque mesure » (couche …-02). À vérifier par la discussion dev : ordre de priorité des couches, ou scène de vague à débarrasser de sa couleur. |
| 2026-10-02 | v1.010.079 | 2b Groove : vague sur les PAR | ❌ | « Pas de vague » | À examiner avec 2a : scène « Vague sur les PAR » (sinus 2 s, 10-100 %, déphasage 360°, taille 0,9, centre 0,55, sélection « PAR gauche → droite »). Demander à la discussion dev si l'intensité de la vague est bien appliquée par-dessus « Plein feu » (couche Intensité). Pistes à préciser avec l'utilisateur : matériel ou Simulateur ? |
| 2026-10-02 | v1.010.079 | 2a/2b précision | 💡 | « J'ai le matériel branché, c'est le matériel que je regarde » | 2a et 2b constatés sur le matériel réel (pas au Simulateur). |
| 2026-10-02 | v1.010.079 | 2c Groove : lyres au centre puis en huit | ✅ | « Ok » | — |
| 2026-10-02 | v1.010.079 | 2d Groove : reprise sans coupure | ✅ | « ok » | — |
| 2026-10-02 | v1.010.079 | 2e Groove : second clic = arrêt | ✅ | « ok » | — |
| 2026-10-02 | v1.010.079 | 4 Barres et multi-têtes (a à d) | ✅ | « a ok, b ok, c ok, d ok » | Remarque doc : le guide dit « flash d'un temps », la séquence joue le flash sur 0,25 temps (début 7,75, longueur 0,25), tout le parc. |
| 2026-10-02 | v1.010.079 | 4b Pulsation double temps (a à c) | ✅ | « a ok, b ok, c ok » | — |
| 2026-10-02 | v1.010.079 | 5 Visite guidée (a à c) | ✅ | « a ok, b ok, c ok » | — |
| 2026-10-02 | v1.010.079 | 6 Bandeau déplié (a à d) | ✅ | « a ok, b ok, c ok, d ok » | — |
| 2026-10-02 | v1.010.079 | 7 Forcer (a, b) | ✅ | « a ok, b ok » | — |
| 2026-10-02 | v1.010.079 | 8 Branches parallèles et macro-étape (a à e) | ✅ | « a ok, b ok, c ok, d ok, e ok. Juste, j'ai pas vraiment vu le strobe. J'ai vu un flash en réel, très court » | ❌ Strobe des PAR peu ou pas vu dans l'étape « Éclat » du Bloc refrain (matériel). Lecture seule : l'étape joue « Strobe PAR (plafonné à 10 s) » (couche …-05, canal `shutter` 0,8 sur la catégorie « par », 2 mesures = 4 s) + flash blanc 0,5 s ; `sûreté.json` n'est pas en cause (strobe max 10 s continu). Même couche …-05 que la « Vague sur les PAR » absente en 2b : à vérifier côté dev (mapping du canal strobe des PAR en mode 7 canaux, ou scène de couche Effets écrasée). |
| 2026-10-02 | v1.010.079 | 9 Tirage au sort (en cours) | 💡 | « à un moment donné j'ai les PAR qui sont tous de la même couleur et ils changent tous de couleur ensemble sur chaque mesure » | C'est l'étape « Base » (couleur par mesure, sans la vague) : elle fonctionne donc ici, ce qui renforce l'hypothèse 2a (la vague de la couche …-05 impose le bleu dans *Groove*). Doc : le guide dit « toutes les 16 s une variante » ; en réalité Base 16 s + variante 16 s (une variante toutes les ≈ 32 s) ; à préciser dans le guide. |
| 2026-10-02 | v1.010.085 | 9 Tirage au sort (a à e) | ✅ | « Tout est ok sur ce test » ; précise avoir cru à 2 variantes alors qu'il y a 3 états : Base, A, B | 💡 Guide : dire clairement que le show alterne **Base → A ou B → Base…** (3 étapes, pas 2 variantes seules). Essai fait avec le correctif de l'exemple 2 (v1.010.085 selon le retour dev). |
| 2026-10-02 | v1.010.085 | 2 Groove, re-test après correctif (a à d) | ✅ | « a ok, b ok, c ok, d ok » (a couleur par mesure, b vague d'intensité, c lyres, d reprise sans noir) | Corrige 2a et 2b. |
| 2026-10-02 | v1.010.085 | 1 Montée 16 mesures, re-test après correctif (a, b) | ✅ | « a ok, b ok » | Chenillard des PAR visible mesures 9 à 12. |
| 2026-10-02 | v1.010.085 | 4 Barres et multi-têtes, re-test (a à d) | ✅ | « a ok, b ok, c ok, d ok » ; confirme : pas de strobe, un simple flash blanc | — |
| 2026-10-02 | v1.010.085 | 4b Pulsation double temps, re-test (a à c) | ✅ | « a ok, b ok, c ok » | — |
| 2026-10-02 | v1.010.085 | 5, 6, 7 Visite guidée, bandeau, Forcer | ⏸ | « on saute 5, 6 et 7 » | Non refaits en .085 (✅ en .079, non touchés par le correctif). |
| 2026-10-02 | v1.010.085 | 8 Branches parallèles et macro-étape, re-test (a à f) | ✅ | « a ok, b ok, c ok, d ok, e ok, f ok. Juste, strobe très court, sûrement normal » | 💡 À confirmer par le dev : l'étape « Éclat » doit durer 2 mesures (4 s à 120 BPM) ; l'utilisateur perçoit un strobe « très court ». Non bloquant. |
| 2026-10-02 | v1.010.085 | 8 précision : durée du strobe | ❌ | « je confirme test précédent, strobe à peine 1s » | Attendu : étape « Éclat » du *Bloc refrain* = 2 mesures (4 s) de strobe des PAR (+ flash 0,5 s). Constaté sur le matériel : ≈ 1 s. À examiner par le dev : durée réelle de l'étape « Éclat » dans la macro-étape (le parent quitte-t-il la macro avant la fin ?), strobe coupé par le flash, limiteur de strobe. |
| 2026-10-02 | v1.010.085 | 10 Show secondaire (a à d) | ✅ | « tout ok » ; ne teste pas la fumée (équipement non branché jusqu'à la finalisation de l'appli) | Fumée non testée au matériel : ⏸. |
| 2026-10-02 | v1.010.085 | 11 Piège boucle sans condition (a, b) | ✅ | « a ok, b ok » | — |
| 2026-10-02 | v1.010.085 | 12 Couplet / Refrain / Drop sans musique (a à f) | ✅ | « a ok, b ok, c ok, d ok, e ok, f ok. Par contre : WOUAH!!! Elle est très très complexe cette interface! » | 💡 Ergonomie : l'éditeur de show (cartes, diagramme, essai) paraît très complexe à la première découverte ; à traiter dans l'analyse ergonomique de fin de phase (simplifier, guider, masquer le détail). |
| 2026-10-02 | v1.010.079 | 3a Tempo ×2 | ✅ | « ok » | — |
| 2026-10-02 | v1.010.079 | 3b Tempo ÷2 | ✅ | « ok » | — |
| 2026-10-02 | v1.010.079 | 3c Tempo TAP | ✅ | « ok » | — |
