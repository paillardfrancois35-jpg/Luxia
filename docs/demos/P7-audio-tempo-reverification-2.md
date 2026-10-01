# Guide de re-vérification P7 – seconde série

> Version à essayer : **1.009.090** (la version annoncée par la discussion de développement). Ne reprend que ce qui a été corrigé
> après la re-vérification de 1.009.077 et ce qui a changé depuis ; le lot ergonomique a son propre guide
> ([P7-ergonomie-reverification.md](P7-ergonomie-reverification.md)). Résultats : lignes à ajouter (ajout seul) à la fin de
> [essais/P7-resultats.md](../essais/P7-resultats.md), même grille ✅ / ❌ / 💡.
> Préparation : le **Show de travail** doit avoir été régénéré par la discussion dev (la scène *Flash sur le kick* a changé) ; scènes « Phase P7 » dans les colonnes.
> **Piège** : *Plein feu* fige les scènes d'intensité des PAR ; utiliser *Lyres allumées (sans les PAR)*.

## Ce qui a changé

| Sujet | Avant | Maintenant |
|---|---|---|
| Changement de sortie de Windows | « Le son joué par le PC » ne suivait pas casque ↔ haut-parleurs : détection figée, aucun avis | L'écoute vérifie chaque seconde la sortie par défaut de Windows et se reconnecte seule, avec avis (écran Audio, Journal) |
| Drop | Jamais vu dans la liste d'événements | La liste se rafraîchit toujours ; deux listes séparées ; bouton **Effacer les listes** |
| Voyants de temps après ×2 | Suivaient le tempo d'origine de la musique | Suivent le tempo affiché |
| Kicks | Notes de basse (guitare, basse) comptées comme kicks | Seuls les coups de batterie avec leur claquement comptent |
| Flash sur le kick | Restait allumé tant qu'aucun kick ne venait | Flash bref de 0,15 s, puis extinction toute seule |
| Vitesse selon l'énergie | 0,6× à 1,4× | 0,5× à 1,8× (1× à mi-énergie) : les accélérations se voient |
| Journal | Texte non sélectionnable | Sélection du texte, clic droit « Copier tout le journal » |
| Glossaire | Break / drop en une ligne | Break, drop, kick, beat, montée expliqués séparément |

## Exemples

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 15 | Changement de sortie de Windows | Écoute en cours (interrupteur **Audio** bleu, périphérique « Le son joué par le PC ») ; changer la sortie de Windows : casque Bluetooth → haut-parleurs du PC, puis retour | En **1 à 2 s** : avis « Changement de périphérique audio : reconnexion… » (écran Audio, 12 s) puis « Écoute reprise sur … », ligne 🎧 au Journal de l'écran de jeu ; **niveaux et BPM reviennent seuls**, sans toucher à la liste ni à Actualiser |
| 21 | Break et drop | Écran Audio, **Effacer les listes**, puis *Animals* depuis le début | **BREAK** (rouge) vers 0:17, puis **DROP** vers 0:21 dans la liste « Break, drop, montée, silence » ; la liste « Niveaux d'énergie » est à part |
| 21 b | Listes d'événements | Laisser jouer deux ou trois morceaux sans effacer | Les listes continuent de se mettre à jour (plus de gel après 30 événements) ; le texte se sélectionne |
| 28 b | Voyants après ×2 | Écran de jeu, interrupteur **Audio** bleu sur un morceau régulier ; cliquer **×2** ; regarder les voyants 1-2-3-4 | Les voyants battent **au BPM affiché** (deux fois plus vite qu'avant le clic), toujours calés sur les coups ; **÷ 2** : un voyant sur deux |
| 19 | Kicks et notes de basse | Scène *Lyres allumées (sans les PAR)*, puis *Flash sur le kick* pendant *Sandstorm*, puis un morceau avec guitare ou basse seule (*Another One Bites The Dust*, *Back In Black*) | Un **flash bref** à chaque coup de grosse caisse, **qui s'éteint tout seul** ; plus de flash sur les notes de basse ; si la lumière paraît en retard sur le son, régler la **Latence** (écran Audio, volet Réglages) vers + 50 à + 100 ms et noter la valeur |
| 19 b | Sensibilité | Écran Audio → Réglages : 40 % puis 80 % | Moins puis plus de flashs ; à 80 % les kicks plus doux comptent aussi |
| 23 | Vitesse selon l'énergie | *Un PAR par temps* en « À la durée de l'étape » avec « La vitesse suit l'énergie » ; jouer *Animals* (très contrasté) | Chenillard **nettement plus lent** dans le break, **nettement plus rapide** au drop |
| J | Journal | Écran de jeu : sélectionner du texte d'une ligne, Ctrl+C ; clic droit sur le Journal | Le texte se copie ; « Copier tout le journal » met tout le journal dans le presse-papiers |
| G | Glossaire | Lire [glossaire.md](../glossaire.md) §8 | Break, drop, montée, kick, beat expliqués ; dire si une définition est à reprendre |

## Retour

Pour chaque ligne : **correct / à revoir / idée**, dans [essais/P7-resultats.md](../essais/P7-resultats.md) (ajout seul).
