# Guide de re-vérification P7 – exemples corrigés

> Version à essayer : **1.009.077** (ou la dernière annoncée). Ne reprend que ce qui a été corrigé après l'essai du
> 2026-10-01 ; le reste du [guide P7](P7-audio-tempo.md) est validé. Résultats : lignes à ajouter (ajout seul) à la fin de
> [essais/P7-resultats.md](../essais/P7-resultats.md), exemple par exemple (même grille ✅ / ❌ / 💡).
> Même préparation que le guide P7 §0 : Show de travail régénéré par la discussion dev, scènes « Phase P7 » dans les colonnes.
> **Piège** : *Plein feu* fige les scènes d'intensité des PAR ; utiliser *Lyres allumées (sans les PAR)*.

## Ce qui a changé (à avoir en tête)

| Sujet | Avant | Maintenant |
|---|---|---|
| Impulsions des basses | 2 à 3 par seconde sur tous les morceaux, même sans basses ; sensibilité sans effet | Seulement sur de vraies attaques de basses ; rien dans les passages sans basses ; la sensibilité se voit (20 % = peu, 90 % = beaucoup) ; défaut 60 % |
| Énergie | « Explosif » presque tout le temps, instable | Niveau relatif au morceau : un passage calme est « Calme », un refrain « Énergique / Explosif », un passage stable reste stable (changement de niveau : 2 s pour monter, 3 s pour descendre) |
| Break / Drop | Break tardif, drop jamais vu | Break vu environ 1 s plus tôt ; drop au retour de la musique |
| Bloc BPM | « Audio » et confiance affichés après coupure de l'écoute ; ×2 / ÷2 annulés en Audio | Couper l'écoute remet la source à Fixe (tempo gardé) ; ×2 / ÷2 tiennent jusqu'au prochain morceau |
| Micro | Retard 300 à 500 ms, latence limitée à ± 250 ms | Capture 10 ms au lieu de 60 ms ; latence ± 500 ms **mémorisée par périphérique** |
| Changement de périphérique | Message invisible | Avis de 12 s dans l'écran Audio et une ligne au Journal |
| Boutons − + ÷ | − et + décentrés, ÷ lu comme + | Centrés ; « ÷ 2 » plus gros |
| Calage du tempo | 15 à 30 s | Dès 3 à 4 s sur les morceaux réguliers (intro sans rythme : plus long) |

## Exemples

### Bloc BPM

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 4 | Boutons du bloc BPM | Regarder les boutons **−**, **+**, **×2**, **÷ 2** | Les symboles sont centrés ; « ÷ 2 » ne se lit plus comme « + 2 » |
| 28 | Écoute coupée | Écran de jeu : cliquer **🎧 Audio** (BPM suit la musique), puis décocher **Écoute** | La source repasse à **Fixe**, la confiance disparaît, le BPM garde sa dernière valeur |
| 28 b | ×2 / ÷2 en Audio | Source Audio sur un morceau à tempo régulier ; cliquer **×2** (ou **÷ 2**) et attendre 10 s | Le BPM **reste** doublé (ou divisé) tant que le morceau joue ; il se recale tout seul quand on change de morceau |

### Écran Audio

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 12 | Symbole ÷ | Écran **Audio**, panneau Tempo | « ÷ 2 » lisible ; voyants 1-2-3-4 présents |
| 15 | Changement de périphérique | Écoute en cours : passer du casque aux enceintes (ou l'inverse) | Un **avis** s'affiche une dizaine de secondes dans l'écran Audio (« Changement de périphérique audio : reconnexion… » puis « Écoute reprise sur … ») et une ligne 🎧 apparaît au Journal de l'écran de jeu |
| 18 | Micro USB | Périphérique = *Entrée : …* ; jouer un morceau près du micro ; régler la **Latence (ms)** jusqu'à ce que les points de tempo tombent sur le son (essayer + 200 à + 400 ms) ; changer de périphérique puis revenir | La latence est **propre au micro** : retrouvée au retour sur le micro, **à 0** pour le son du PC ; le retard résiduel se règle ; noter la valeur retenue |

### Impulsions, énergie, événements

Morceaux conseillés : *Sandstorm* (kicks nets, intro sans basses), *Animals*, *Glue*, *Summer*.

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 19 | Flash sur le kick | Lancer *Lyres allumées (sans les PAR)* puis *Flash sur le kick* pendant *Sandstorm* | Les flashs suivent **les vrais kicks** (un sur deux, la scène alterne allumé / éteint) ; **aucun** dans les passages sans basses ; « Impulsion basses » de l'écran Audio ne clignote que sur les kicks |
| 20 | Sensibilité | Écran Audio, réglage « Sensibilité des impulsions » : 20 % puis 60 % puis 90 % (attendre 3 s après chaque changement) | Moins, normal, puis beaucoup plus d'impulsions : la différence se **voit** sur « Impulsion basses » et sur les PAR |
| 21 | Break et drop | Écran Audio ouvert pendant *Animals* (pause vers 0:17, retour vers 0:21) | **BREAK** (rouge) peu après le début de la pause, puis **DROP** dans la liste « Derniers événements » au retour de la musique ; note : la liste est dans le panneau Énergie, sous la jauge (défiler si besoin) |
| 22 | Énergie | Écran Audio pendant *Glue* (passages calmes et passages forts) | Niveau **Calme / Groove** sur les passages calmes, **Énergique / Explosif** sur les forts ; un passage stable **reste stable** ; pas de changement plus rapide que toutes les 2 à 3 s |
| 23 | Vitesse selon l'énergie | Fenêtre d'édition d'*Un PAR par temps* : étapes de **2 s**, volet **Au rythme** → *Étape suivante = À la durée de l'étape*, cocher « La vitesse suit l'énergie de la musique » ; jouer pendant *Glue* | Le chenillard est **plus lent** sur les passages calmes et **plus rapide** sur les forts (facteur de 0,6× à 1,4×) |

### Mesure 1 et pause

| # | Exemple | À faire | À observer |
|---|---|---|---|
| M1 | Convergence | Chronométrer le temps avant que le BPM affiché soit juste et stable : *Summer*, *Hips Don't Lie*, *24K Magic* | Attendu : *Summer* et *Hips Don't Lie* en moins de 10 s ; *24K Magic* plus long (intro parlée, sans rythme) ; noter les temps |
| 17 | Pause et reprise | Pendant un morceau régulier : pause de 5 s, puis reprise | Le BPM **ne chute plus** à la reprise (il reste au tempo d'avant puis se confirme) |

## Retour

Pour chaque ligne : **correct / à revoir / idée**, dans [essais/P7-resultats.md](../essais/P7-resultats.md) (ajout seul).
