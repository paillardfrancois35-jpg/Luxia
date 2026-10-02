# Guide de découverte P8 – Show & séquences

> Version à essayer : **1.010.NNN** (voir la barre de titre ; Claude annonce le numéro exact). Branche `p8/show-sequences`.
> Procédure d'essai : [33](../33-procedure-essais.md). Résultats à noter dans [essais/P8-resultats.md](../essais/P8-resultats.md).
> Cahier des charges : [20 – Show et séquences](../20-show-et-sequences.md) ; format des fichiers : [50 §12g et §12h](../50-format-des-donnees.md) ;
> guide de conception : [51](../51-guide-conception-shows.md) ; maquettes 9 à 12 : [docs/maquettes](../maquettes).

**Vocabulaire** (voir le [glossaire](../glossaire.md)) : une **séquence** pose des scènes et des actions sur des **pistes**, en
**mesures** ; un **show** enchaîne des **étapes** par des **transitions** dont la **condition** (« réceptivité ») est musicale ou
temporelle, à la frontière musicale choisie (temps, mesure, phrase). À 120 BPM : **1 mesure = 2 s**, 8 mesures = 16 s.

## 0. Préparation

1. Fermer LuXia ; Claude régénère le show de travail (après accord) et l'ouvre : vérifier **Aide → À propos**, « Dossier du projet ».
2. Brancher les PAR et les lyres si possible (sinon : écran **Simulateur**). Écran **Contrôle**, tempo **120** (case BPM, Entrée),
   **🎧 Audio** éteint sauf mention contraire.
3. Dans les colonnes, **à gauche**, la colonne **« ▶ Shows »** : en haut les shows, en bas les séquences. Sous le bloc BPM, le
   **bandeau « Show en cours »** n'apparaît que quand un show ou une séquence joue ; le **Journal** (en bas) trace chaque étape
   (« ◆ show … : étape … (motif) »).
4. Entre deux exemples : **■ Tout stopper**.

## 1. Catalogue des exemples (catégorie « Phase P8 » du show de référence)

| Exemple | Type | Démontre | Durée à 120 BPM | Sans musique ? | Ce qu'on doit voir |
|---|---|---|---|---|---|
| *Montée 16 mesures* | séquence | Pistes par couche, relais au même instant, **rampe** de niveau, fumée, départ à la mesure | 32 s, une fois | oui | Bleu qui monte de 30 à 100 % (16 s) ; mesure 9 : chenillard + cercle des lyres ; mesure 13 : strobe + blanc chaud ; fumée sur la dernière mesure |
| *Groove 8 mesures* | séquence | **Boucle** sans coupure | 16 s, en boucle | oui | Une couleur par mesure, vague sur les PAR, lyres au centre puis en huit |
| *Break calme 8 mesures* | séquence | Ambiance douce (UV) | 16 s, une fois | oui | 50 %, bleu, lyres au plafond, UV |
| *Explosion drop 4 mesures* | séquence | **Flash** d'un temps, strobe puis vague | 8 s, une fois | oui | Flash blanc, strobe 4 s, chenillard, cercle, fumée |
| *Pulsation couleurs (double temps)* | séquence | **Vitesse « double »** (SHOW-008) | 4 s par tour, en boucle | oui | Rouge, ambre, bleu, ambre : une couleur toutes les secondes (2 temps) au lieu de toutes les 2 s |
| *Visite guidée (sans musique)* | show | Un show **qui avance seul** : durées en mesures, **fin d'une séquence** | ≈ 60 s par tour | **oui, idéal pour commencer** | Ouverture (8 s) → Groove (16 s) → Break (jusqu'à sa fin) → Explosion (jusqu'à sa fin) → Final (8 s) → Ouverture… |
| *Branches parallèles et macro-étape* | show | **Divergence et convergence en ET**, **macro-étape** (sous-show) | ≈ 42 s par tour | **oui** | Intro (4 s) ; **deux étapes actives à la fois** (couleurs, mouvements), les mouvements changent à 12 s, les couleurs à 20 s ; puis le sous-show *Bloc refrain* (arc-en-ciel 8 s, éclat 4 s) ; final 8 s ; reprise |
| *Bloc refrain (macro-étape)* | show | Le **sous-show** utilisé par l'exemple précédent | 12 s puis tient | oui | Arc-en-ciel et cercle, puis strobe et flash ; reste sur « Fin du bloc », où plus rien ne s'allume : c'est voulu, cette fin « libère » la macro-étape |
| *Couplet / Refrain / Drop* | show | **Événements musicaux** (énergie, drop, break, montée), **quantification**, **compteur** de refrains | selon la musique | avec la simulation (exemple 12) | Intro → Couplet quand l'énergie atteint « Groove » → Refrain au drop (flash) → … → Final au 3e refrain |
| *Tirage au sort (variantes)* | show | **Tirage pondéré**, « jamais deux fois la même » | 16 s par branche | oui | Base, puis variante A ou B toutes les 8 mesures, jamais deux fois la même de suite |
| *Ambiance UV et fumée (secondaire)* | show | **Show secondaire** en parallèle | en continu | oui | UV ; une chance sur deux d'une rafale de fumée toutes les 8 mesures |
| *Piège : boucle sans condition* | show | **Refus** d'un show fautif (SHOW-024) | — | — | Rien ne s'allume ; refus au Journal |

## 2. Séquences (sans musique)

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 1 | Montée 16 mesures | Cliquer *Montée 16 mesures* dans la colonne Shows | Elle part **à la mesure suivante** (le bouton dit « attend la mesure ») ; bouton : « mesure N / 16 » et barre d'avancement ; bandeau : « séquence « Montée 16 mesures » N / 16 » ; déroulé du catalogue ; tout s'éteint à la fin |
| 2 | Groove 8 mesures | Lancer *Groove 8 mesures* ; attendre 20 s ; recliquer | Elle **reprend au début sans coupure** ; second clic = arrêt (ses scènes s'arrêtent) |
| 3 | Tempo | Relancer *Groove* ; changer le tempo (×2, ÷2, TAP) | La séquence suit tout de suite le nouveau tempo |
| 4 | Double temps | Lancer *Pulsation couleurs (double temps)* ; puis ✎ dessus, **Vitesse** = « normale », **▶ Jouer** dans la fenêtre | Une couleur par seconde ; en « normale », une toutes les 2 s. Fermer la fenêtre par **Annuler** |

## 3. Shows sans musique

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 5 | Visite guidée | Lancer *Visite guidée (sans musique)* et regarder une minute | Le bandeau suit les étapes (« depuis N mesures », « ensuite : après 4 mesures → Groove ») ; Journal : une ligne par étape, motif « après 4 mesures », « fin de « Break calme 8 mesures » » ; le tour recommence après le final |
| 6 | Bandeau déplié | Pendant la visite, **▾ Détail** | Parcours (0 → 1 → 2…), étape active et ce qu'elle joue, la transition possible avec son état (« vraie dans N mesure(s) », puis « part dans N temps ») et son bouton **⏭ Forcer** |
| 7 | Forcer | **⏭ Forcer ▾** → la transition proposée | L'étape suivante arrive **à la mesure suivante**, sans attendre sa condition ; Journal : « (forcée) » |
| 8 | Branches en parallèle | Lancer *Branches parallèles et macro-étape* | Après l'intro, le bandeau montre **deux étapes actives** (« Couleurs : une par mesure, Mouvements : cercle ») ; à 12 s seuls les mouvements changent, à 20 s les couleurs ; puis « Refrain (macro) » : le sous-show *Bloc refrain* joue (le détail montre ses étapes) ; puis Final ; reprise |
| 9 | Tirage au sort | Lancer *Tirage au sort (variantes)*, laisser 2 minutes | Variante A ou B toutes les 16 s, **jamais deux fois la même de suite** (A, B, A… ou B, A, B…) |
| 10 | Show secondaire | Lancer *Visite guidée*, puis *Ambiance UV et fumée (secondaire)* ; puis **■ Stop** ; puis **■ Tout stopper** | Les deux jouent ; bandeau : « en parallèle : « Ambiance… » » ; **■ Stop** arrête la visite mais **garde** l'ambiance (comme les couches protégées) ; **■ Tout stopper** arrête tout |
| 11 | Piège | Cliquer *Piège : boucle sans condition* | **Rien ne s'allume** ; Journal : « ✕ commande refusée : … boucle sans condition (a → b → a) » |

## 4. Shows réagissant à la musique

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 12 | Couplet / Refrain / Drop, sans musique | ✎ sur *Couplet / Refrain / Drop* ; cocher **👁 Aveugle** et **métronome** ; **▶ Jouer le show** ; boutons **Groove**, puis **Drop**, **Break**, **Montée**, **Drop**… | La sortie sur scène **ne change pas** (Simulateur seulement) ; la carte et le **diagramme** de l'étape active se colorent ; « Étape active / Ensuite » suit ; après un Drop, le refrain arrive **à la mesure suivante** ; au 3e refrain : Final. **Annuler** ferme sans rien changer |
| 13 | Couplet / Refrain / Drop, avec musique | Lancer un morceau électro sur le PC, **🎧 Audio** allumé ; lancer le show | Couplet quand l'énergie atteint « Groove », refrain aux drops réels, retour aux breaks (selon la détection de P7) |

## 5. Créer soi-même

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 14 | Nouvelle séquence | **+ séquence**, nommer ; **glisser** des scènes de la liste de gauche sur la frise ; **déplacer** un bloc, tirer son **bord droit** ; changer la **Grille** ; **▶ Jouer** ; **Valider** | Une scène lâchée sur la mauvaise piste va sur celle de sa couche ; aimantation ; Ctrl+Z annule un geste ; Suppr retire le bloc choisi ; après Valider, le bouton apparaît dans la colonne |
| 15 | Bloc d'action | Glisser *Niveau de couche (rampe)* sur la piste **Actions**, couche Couleurs, 0 → 100 % | La rampe se dessine ; à l'essai, le curseur « Niveau » de la colonne Couleurs monte |
| 16 | Nouveau show | **+ show** ; **+ étape** ; **+ action** (« jouer une scène ») ; **+ transition** (« après une durée », 4 mesures, vers « 1 ») ; essai (exemple 12) ; **Valider** | Le diagramme se redessine ; une étape non reliée est signalée « inatteignable » ; un problème bloquant s'affiche en rouge sous le diagramme |
| 17 | Durées en temps | ✎ sur une scène, étape : **Maintien** `2` **temps**, **Fondu** `1` **mesure** | La bande d'étapes écrit « 1 mesure + 2 temps » (E2) |

## 6. Sans interface (facultatif)

```powershell
luxia-headless jouer "samples/Show de travail" --show "Branches parallèles et macro-étape" --duree 45 --pas 30
luxia-headless jouer "samples/Show de travail" --sequence "Montée 16 mesures" --duree 34 --pas 2
```

Le résumé liste ce qui s'allume et chaque étape activée avec son motif.

## 7. Ce qui ne se voit bien qu'au matériel

Strobe et fumée (*Montée 16 mesures*, *Explosion drop*, *Bloc refrain*, *Ambiance*) : limiteurs de sûreté actifs ; drops et breaks réels (exemple 13).

## 8. Retour

| Point | Correct / à revoir / idée |
|---|---|
| Colonne Shows (place, gestes) | |
| Bandeau « Show en cours » (lisibilité, Forcer) | |
| Éditeur de séquence (glisser-déposer, grille) | |
| Éditeur de show (cartes, diagramme, essai sans musique) | |
| Les exemples (lesquels parlent, lesquels non) | |
