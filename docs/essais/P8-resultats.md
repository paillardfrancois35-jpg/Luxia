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
| 2026-10-02 | v1.010.079 | 3a Tempo ×2 | ✅ | « ok » | — |
| 2026-10-02 | v1.010.079 | 3b Tempo ÷2 | ✅ | « ok » | — |
| 2026-10-02 | v1.010.079 | 3c Tempo TAP | ✅ | « ok » | — |
