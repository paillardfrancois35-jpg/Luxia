# Guide d'essai du lot ergonomique P7

> Version à essayer : **1.009.078** ou suivante (annoncée par la discussion de développement). Ne couvre que le lot ergonomique décidé après
> l'essai du 2026-10-01 ; les corrections du guide [P7-audio-tempo-reverification.md](P7-audio-tempo-reverification.md) se vérifient à part.
> Résultats : lignes à ajouter (ajout seul) à [essais/P7-resultats.md](../essais/P7-resultats.md), même grille ✅ / ❌ / 💡.
> Captures de référence : [Audio en écoute](../essais/ergo-p7/Audio%20-%20en%20écoute.png), [écran de jeu](../essais/ergo-p7/Contrôle%20-%20écran%20de%20jeu.png).

## Bloc BPM de l'écran de jeu

| # | Exemple | À faire | À observer |
|---|---|---|---|
| E1 | Interrupteur Audio | Cliquer **🎧 Audio** (gris au départ), jouer un morceau, recliquer | Bleu = suit la musique ; gris = tempo fixe, qui garde sa dernière valeur. Plus de case « Écoute », de « Fixer » ni d'étiquette de source |
| E2 | Audio actif | Interrupteur bleu | Le champ BPM, **TAP**, **−**, **+** sont grisés ; **×2**, **÷ 2** et **1 ici** restent actifs ; la confiance (vert / orange / rouge) apparaît à droite du bouton sans décaler le reste |
| E3 | Saisie du BPM | Interrupteur gris : taper `96` dans le champ, **Entrée** ; puis taper `110` et cliquer ailleurs | Le tempo passe à 96 puis 110 ; l'affichage ne saute pas pendant la frappe ; une saisie illisible (`abc`) rend le tempo courant |
| E4 | TAP allumé | Taper **T** au clavier puis cliquer TAP | Le bouton s'allume en vert un instant à chaque frappe, clavier comme souris |
| E5 | Redémarrage | Laisser l'interrupteur bleu, fermer et relancer LuXia | L'écoute et la source Audio reviennent toutes seules |

## Écran Audio et Simulateur

| # | Exemple | À faire | À observer |
|---|---|---|---|
| E6 | Une page | Écran **Audio** en plein écran 1920 × 1080 | Tout tient **sans défilement** : direct à gauche, **niveaux verticaux** à droite ; Réglages et Calibration repliés (un clic les déplie) |
| E7 | Même réglage | Cocher / décocher « Écouter et suivre le tempo » | L'interrupteur du bloc BPM suit (et inversement) |
| E8 | Rappel du tempo | Écran **Simulateur** pendant un morceau | BPM et quatre voyants (le 1 en orange) battent à côté de « Source » |

## Volet « Au rythme » (fenêtre d'édition d'une scène)

| # | Exemple | À faire | À observer |
|---|---|---|---|
| E9 | Masquer l'inutile | Éditer *Un PAR par temps* ; ouvrir **Au rythme** ; passer « Étape suivante » de *À chaque temps* à *À la durée de l'étape* | **Fréquence** disparaît avec la durée et réapparaît avec un événement ; l'en-tête résume les réglages (« Au rythme : étapes sur le temps, ×1 ») |
| E10 | Fréquence ×2 | *À chaque temps* + **×2**, puis **×4** ; jouer à 120 BPM | Deux, puis quatre changements d'étape par temps ; **÷ 4** : un changement tous les quatre temps |
| E11 | Impulsions | *Sur les basses (kick)* | La liste ne propose que ×1, ÷ 2, ÷ 4, ÷ 8 (pas de ×2 ni ×4) |
| E12 | Anciens réglages | Ouvrir une scène qui avait « Tous les 2 » | Elle affiche **÷ 2** (un sur deux), rien n'a changé à la lecture |

## Retour

Pour chaque ligne : **correct / à revoir / idée**, dans [essais/P7-resultats.md](../essais/P7-resultats.md) (ajout seul).
