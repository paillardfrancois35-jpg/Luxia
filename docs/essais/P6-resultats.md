# Résultats d'essai – P6 Effets

> Fichier de résultats de la phase P6 (procédure : [doc 33](../33-procedure-essais.md)). Guide :
> [demos/P6-effets.md](../demos/P6-effets.md). Écrit par la discussion **test** en ajout seul ; lu par la discussion
> **dev**, qui corrige et tient les fiches d'exigences.

| Date | Version | Exemple | Résultat | Observation de l'utilisateur | Demande / anomalie |
|---|---|---|---|---|---|
| 2026-09-28 | 1.006.040 | Préparer | ✅ | Show de travail régénéré ; un autre projet était ouvert au départ, rouvert : seul l'avertissement « Lyres sur 3 positions » | — |
| 2026-09-28 | 1.006.040 | 1 Vague | ✅ | De gauche à droite, fluide | « Garder un minimum » → corrigé (10 à 100 %) |
| 2026-09-28 | 1.006.040 | 2 Miroir | ✅ | « Le miroir fonctionne » | — |
| 2026-09-28 | 1.006.040 | 3 Barres | ✅ | Arc-en-ciel et chenillard des segments fonctionnent | — |
| 2026-09-28 | 1.006.040 | 4 Multi-têtes | ⏸ | WZYBUTA pas encore installé | À faire : menu 64 canaux, adresse 181 ; canal 182 (Q25) |
| 2026-09-28 | 1.006.040 | 5 Lyres | ✅ | « Tout est ok » ; le huit ressemble à un V dans le garage (peu de recul) | Expliqué : déformation due à la projection proche, pas de changement |
| 2026-09-28 | 1.006.040 | 6 Piège | ✅ | La lyre longe le bord, journal ok | — |
| 2026-09-28 | 1.006.040 | — | 💡 | Colonnes : les commandes de couche disparaissent en faisant défiler | Réalisé : ERG-030 (en-têtes fixes), v1.006.049 |
| 2026-09-29 | 1.006.049 | 7 Créer un effet | ❌ | Étapes 1 à 3 faites : l'effet tourne au panneau, mais les PAR réels ne s'allument pas | Cause : effet d'intensité sans couleur dans l'étape (PAR RVB à 0) ; correctif en cours côté dev (blanc ajouté automatiquement) |
| 2026-09-29 | 1.006.053 | 7 Créer un effet | — | — | Correctif EFF-011 livré en 1.006.053 (blanc ajouté aux cibles sans couleur) : reprendre l'exemple 7 (retirer puis réajouter l'effet de « Mon effet ») |
