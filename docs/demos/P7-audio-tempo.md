# Guide de découverte P7 – Audio et tempo

> Version à essayer : **1.009.NNN** (voir la barre de titre ; Claude annonce le numéro exact). Branche `p7/audio-tempo`.
> Procédure d'essai : [33](../33-procedure-essais.md). Résultats à noter dans [essais/P7-resultats.md](../essais/P7-resultats.md).
> Cahier des charges : [19 – Audio et tempo](../19-audio-et-tempo.md), [15](../15-moteur-de-rendu.md), [16 §5](../16-scenes-et-effets.md).

## 0. Préparation

1. Fermer LuXia, puis régénérer le show de travail : `cp -r "samples/Show de référence" "samples/Show de travail"`, renommer
   `"name"` dans son `projet.json` en « Show de travail » et ouvrir **ce** dossier (contrôler dans Aide → À propos).
2. Brancher les PAR (et les lyres si on veut voir les cercles). Écran **Contrôle** : le **bloc BPM** est sous la barre
   d'arrêt, avec `♪ 120 BPM`, `− +`, `TAP`, `×2 ÷2`, `1 ici`, les quatre points du compteur de temps, `Fixer`, `🎧 Audio`, `Écoute`.
3. Les scènes de l'essai sont dans les colonnes (catégorie « Phase P7 ») : *Un PAR par temps*, *Couleur à chaque mesure*,
   *Flash sur le kick*, *Cercle calé sur la mesure*, *Mouvement lent à 30 BPM*, *Départ à la mesure*, *Calibration de latence*, *Lyres allumées (sans les PAR)*.
   **Piège à connaître** : *Plein feu* allume tous les appareils à 100 % et, l'intensité la plus haute l'emportant, il **fige** les scènes qui jouent sur l'intensité des PAR (*Un PAR par temps*, *Flash sur le kick*). Pour voir les lyres avec ces scènes, lancer *Lyres allumées (sans les PAR)* (colonne Intensité) à la place de *Plein feu*.

## 1. Horloge sans musique (lot 1)

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 1 | Tap tempo | Taper **T** (ou le bouton TAP) 5 fois en rythme, environ 2 frappes par seconde | Le BPM passe à ≈ 120 ; la source affiche **Tap** ; les points du compteur défilent 1-2-3-4 sur vos frappes |
| 2 | Corriger | Cliquer **×2**, **÷2**, **+**, **−** ; saisir `90` dans la case puis **Fixer** | Le BPM suit ; la source repasse à **Fixe** ; les lignes « ♪ … » apparaissent dans le Journal |
| 3 | « 1 ici » | Cliquer **1 ici** au moment précis où vous entendez/voyez « le 1 » | Le point orange (temps 1) s'allume tout de suite |
| 4 | Stabilité | Laisser tourner 1 minute | Aucun bouton ne bouge ; le texte du BPM ne fait pas sauter la mise en page |

## 2. Scènes au rythme (lot 2)

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 5 | Un PAR par temps | Lancer *Un PAR par temps (chenillard au tempo)* ; changer le tempo (TAP, ×2, Fixer 60) | Un PAR s'allume par temps, en tournant ; le chenillard suit immédiatement le tempo |
| 6 | Couleur à chaque mesure | Lancer *Couleur à chaque mesure*, puis **1 ici** | Les PAR changent de couleur au « 1 » de chaque mesure ; « 1 ici » déplace le changement |
| 7 | Départ à la mesure | Appuyer n'importe quand sur *Départ à la mesure (blanc chaud)* | Le bouton affiche **⏳ dans N t** ; la scène démarre au « 1 » suivant ; un **second appui** annule l'attente |
| 8 | Cercle calé sur la mesure | Lancer *Lyres allumées (sans les PAR)*, puis *Cercle calé sur la mesure* ; taper un nouveau tempo | Un tour de cercle par mesure ; le cercle repart sur le « 1 » |
| 9 | Horloge propre | Lancer *Mouvement lent à 30 BPM*, puis changer le tempo de l'horloge | Le mouvement lent **ne change pas** (horloge propre de la scène) |
| 10 | Fenêtre d'édition | Ouvrir une scène (✎), volet **Au rythme** | « Étape suivante », « Tous les », « Démarrage », « Horloge propre », « La vitesse suit l'énergie » |

## 3. Écoute de la musique (lot 3)

Musique jouée **sur ce PC** (Deezer, YouTube Music ou VLC), volume confortable.

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 11 | Démarrer | Lancer un morceau régulier (par ex. *Summer*, *24K Magic*). Cliquer **🎧 Audio** dans le bloc BPM | L'état dit « Écoute : <votre périphérique> » ; en 5 à 10 s, le BPM se cale ; la source affiche **Audio** et « confiance xx % » |
| 12 | Écran Audio | Ouvrir l'écran **Audio** | Niveau, basses / médiums / aigus qui bougent ; BPM, confiance, compteur de temps |
| 13 | Suivre la musique | Lancer *Un PAR par temps* pendant le morceau | Les PAR avancent **sur le temps** de la musique (à quelques dizaines de ms près) |
| 14 | Volume | Baisser puis monter le volume du PC | Le tempo et la confiance ne changent pas |
| 15 | Changement de périphérique | Passer du casque aux enceintes (ou l'inverse) pendant l'écoute | Message « Changement de périphérique… », puis l'écoute reprend toute seule en quelques secondes |
| 16 | Changer de morceau | Baisser le son, lancer **tout de suite** un autre morceau d'un autre tempo | LuXia retrouve le nouveau tempo en moins de 8-10 s ; l'horloge garde l'ancien pendant ce temps |
| 17 | Pause | Mettre le morceau en pause 5 s, puis le relancer | « silence » dans les événements ; l'horloge continue au dernier tempo, se recale à la reprise |
| 18 | Micro USB | Écran Audio → Périphérique → *Entrée : <micro USB>* ; jouer un morceau près du micro | Le tempo se trouve, en général moins précis ; noter la confiance |

## 4. Impulsions, énergie et événements (lot 4)

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 19 | Flash sur le kick | Pendant un morceau à kick marqué, lancer *Flash sur le kick* | Les PAR flashent sur les kicks ; « Impulsion basses » clignote dans l'écran Audio |
| 20 | Sensibilité | Écran Audio → Sensibilité des impulsions : 20 % puis 80 % | Moins puis plus d'impulsions |
| 21 | Break et drop | Lancer *Animals* (Martin Garrix) ou *Summer* | Événements **BREAK** puis **DROP** dans la liste au bon moment ; « BREAK » rouge pendant la pause |
| 22 | Énergie | Regarder la jauge et le niveau (Calme, Groove, Énergique, Explosif) sur un morceau qui monte | Le niveau change sans osciller ; la tendance (↗ ↘) suit |
| 23 | Vitesse selon l'énergie | Dans la fenêtre d'édition d'une scène à étapes de 2 s (par ex. *Un PAR par temps*), volet **Au rythme** : **Étape suivante = « À la durée de l'étape »** (avec « À chaque temps », le rythme est celui du BPM : le facteur ne joue pas), cocher « La vitesse suit l'énergie de la musique » ; la jouer pendant un morceau | La scène accélère sur les passages énergiques, ralentit sur les calmes |
| 24 | Calibration | Écran Audio → **Lancer le flash sur chaque temps**, puis régler la **Latence (ms)** | Le flash des PAR tombe sur le kick ; noter la valeur retenue ; **Arrêter** |

## 5. Pièges (DEMO-4)

| # | Piège | À faire | Attendu |
|---|---|---|---|
| 25 | Ballade | Jouer *Perfect* ou *Someone Like You* | Confiance **faible** : l'horloge garde son tempo ; le BPM affiché peut valoir 1,5× ou 2× le tempo ressenti : **×2 / ÷2** |
| 26 | Tempo qui change | Jouer *Les Lacs du Connemara* jusqu'à la fin | Le tempo suit l'accélération par petits pas |
| 27 | Mesures irrégulières | Jouer *Schism* (Tool) ou *The Great Gig in the Sky* | Confiance faible ou instable ; aucune scène ne se fige (repli au dernier tempo) |
| 28 | Écoute coupée | Décocher **Écoute** pendant un morceau | L'horloge garde son dernier tempo ; les scènes « sur le kick » avancent au temps |

## 6. Mesures à relever

- Temps de convergence sur 3 morceaux (chronomètre), écart moyen perçu entre flash et kick, charge CPU de LuXia
  (barre d'état) avec l'écoute active, démarrage avec l'écoute mémorisée.

## Retour

Pour chaque exemple : **correct / à revoir / idée**, dans [essais/P7-resultats.md](../essais/P7-resultats.md).
