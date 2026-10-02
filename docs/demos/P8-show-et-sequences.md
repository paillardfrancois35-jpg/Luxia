# Guide de découverte P8 – Show & séquences

> Version à essayer : **1.010.NNN** (voir la barre de titre ; Claude annonce le numéro exact). Branche `p8/show-sequences`.
> Procédure d'essai : [33](../33-procedure-essais.md). Résultats à noter dans [essais/P8-resultats.md](../essais/P8-resultats.md).
> Cahier des charges : [20 – Show et séquences](../20-show-et-sequences.md) ; format des fichiers : [50 §12g et §12h](../50-format-des-donnees.md) ;
> guide de conception (pour l'utilisateur comme pour une IA) : [51](../51-guide-conception-shows.md) ; maquettes 9 à 12 : [docs/maquettes](../maquettes).

**Vocabulaire** (voir le [glossaire](../glossaire.md)) : une **séquence** pose des scènes et des actions sur des **pistes**, en
**mesures** ; un **show** enchaîne des **étapes** par des **transitions** dont la **condition** (« réceptivité ») est musicale ou
temporelle, à la frontière musicale choisie (temps, mesure, phrase).

## 0. Préparation

1. Fermer LuXia ; Claude régénère le show de travail (après accord) et l'ouvre : vérifier **Aide → À propos**, « Dossier du projet ».
2. Brancher les PAR et les lyres si possible (sinon : Simulateur). Écran **Contrôle**.
3. Dans les colonnes, **à gauche**, la nouvelle colonne **« ▶ Shows »** : en haut les shows, en bas les séquences (catégorie « Phase P8 »).
   Sous le bloc BPM, le **bandeau « Show en cours »** n'apparaît que quand un show ou une séquence joue.

## 1. Séquences (sans musique)

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 1 | Montée 16 mesures | Tempo 120 (case BPM). Cliquer *Montée 16 mesures* dans la colonne Shows | Elle part **à la mesure suivante** (bouton « attend la mesure ») ; le bouton montre « mesure N / 16 » et une barre d'avancement ; bandeau : « séquence « Montée 16 mesures » N / 16 ». Les PAR en bleu **montent de 30 à 100 %** sur 8 mesures (16 s), puis chenillard et cercle des lyres à la **mesure 9**, vague puis strobe à la **mesure 13**, fumée sur la dernière mesure ; tout s'arrête à la fin (32 s) |
| 2 | Groove 8 mesures (boucle) | Lancer *Groove 8 mesures* ; la relancer d'un clic pour l'arrêter | Une couleur par mesure, lyres au centre puis en huit ; elle **reprend au début sans coupure** ; second clic = arrêt (ses scènes s'arrêtent) |
| 3 | Tempo | Pendant *Groove 8 mesures*, changer le tempo (×2, ÷2, TAP) | La séquence suit tout de suite le nouveau tempo |

## 2. Shows (avec ou sans musique)

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 4 | Couplet / Refrain / Drop | Lancer *Couplet / Refrain / Drop* ; jouer un morceau électro sur le PC avec **🎧 Audio** (ou passer à l'exemple 7 pour l'essai sans musique) | Bandeau : **Intro**, puis **Couplet** quand l'énergie atteint « Groove » (à la mesure suivante) ; au **drop**, **Refrain** (flash blanc, arc-en-ciel) ; à la **montée**, *Montée 16 mesures* ; au **break**, retour au couplet ; au **3e refrain**, **Final**, qui reste. Journal : « ◆ show … : étape 3 « Refrain » (au drop) » |
| 5 | Bandeau déplié | Pendant un show, **▾ Détail** | Parcours (0 → 1 → 3…), étape active et ce qu'elle joue, chaque transition possible : condition, frontière (« à la prochaine mesure »), état (« attend la condition », « vraie dans 4 mesures », « part dans 2 temps »), bouton **⏭ Forcer** |
| 6 | Forcer | **⏭ Forcer ▾** (ou le bouton d'une transition du détail) | L'étape change **à la frontière** de la transition (prochaine mesure), sans attendre sa condition ; Journal : « (forcée) » |
| 7 | Essai sans musique | ✎ sur *Couplet / Refrain / Drop* ; cocher **👁 Aveugle** et **métronome** ; **▶ Jouer le show** ; boutons **Groove**, puis **Drop**, **Break**, **Montée**… | La sortie sur scène **ne change pas** (aperçu seulement : Simulateur) ; la carte et le **diagramme** de l'étape active se colorent ; le texte « Étape active / Ensuite » suit ; Annuler ferme sans rien changer |
| 8 | Tirage au sort | Lancer *Tirage au sort (variantes)* et le laisser 1 à 2 minutes (tempo 120 : 8 mesures = 16 s) | Base, puis **variante A ou B** toutes les 8 mesures, **jamais deux fois la même de suite** ; retour à la base après 8 mesures |
| 9 | Show secondaire | Lancer *Couplet / Refrain / Drop*, puis *Ambiance UV et fumée (secondaire)* | Les deux jouent ensemble (bandeau : « en parallèle : … ») ; lancer un autre show principal remplace le premier, pas le secondaire. Fumée : une chance sur deux toutes les 8 mesures (limiteur toujours actif) |
| 10 | Piège | Cliquer *Piège : boucle sans condition* | **Rien ne s'allume** ; Journal : « ✕ commande refusée : … boucle sans condition (a → b → a) » ; `valider` le signale aussi |
| 11 | Tout stopper | Pendant un show, **■ Tout stopper** | Le show s'arrête avec ses scènes ; le bandeau disparaît |

## 3. Créer soi-même

| # | Exemple | À faire | À observer |
|---|---|---|---|
| 12 | Nouvelle séquence | Colonne Shows → **+ séquence**, nommer ; **glisser** des scènes de la liste de gauche sur la frise ; **déplacer** un bloc, tirer son **bord droit** ; changer la **Grille** (mesure / temps / ½ temps) ; **▶ Jouer** ; **Valider** | Une scène lâchée sur la mauvaise piste va sur la piste de sa couche ; les blocs s'aimantent ; Ctrl+Z annule un geste ; Suppr retire le bloc choisi ; après Valider, le bouton apparaît dans la colonne |
| 13 | Bloc d'action | Glisser *Niveau de couche (rampe)* sur la piste **Actions**, régler couche, 0 → 100 % | La rampe se dessine dans le bloc ; à l'essai, le niveau de la couche monte (curseur « Niveau » de la colonne) |
| 14 | Nouveau show | **+ show**, nommer ; **+ étape** ; dans une carte, **+ action** (« jouer une scène ») ; **+ transition** : condition « au drop », vers « 1 », « à la prochaine mesure » ; essai sans musique (exemple 7) ; **Valider** | Le diagramme se redessine à chaque changement ; une étape sans transition qui y mène est signalée « inatteignable » ; un problème bloquant (boucle sans condition, étape initiale absente) s'affiche en rouge sous le diagramme |
| 15 | Durées en temps | ✎ sur une scène, étape : **Maintien** `2` **temps** ; **Fondu** `1` **mesure** | La bande d'étapes écrit « 1 mesure + 2 temps » ; la durée suit le tempo (E2) |

## 4. Sans interface (facultatif)

```powershell
luxia-headless jouer "samples/Show de travail" --sequence "Montée 16 mesures" --duree 34 --pas 2
luxia-headless jouer "samples/Show de travail" --show "Tirage au sort (variantes)" --duree 120 --pas 4
```

Le résumé liste ce qui s'allume et chaque étape de show activée avec son motif.

## 5. Ce qui ne se voit bien qu'au matériel

Strobe et fumée de *Montée 16 mesures* et *Explosion drop* (limiteurs de sûreté actifs) ; drops et breaks réels (exemple 4).

## 6. Retour

| Point | Correct / à revoir / idée |
|---|---|
| Colonne Shows (place, gestes) | |
| Bandeau « Show en cours » (lisibilité, Forcer) | |
| Éditeur de séquence (glisser-déposer, grille) | |
| Éditeur de show (cartes, diagramme, essai sans musique) | |
| Les shows d'exemple (enchaînements musicaux) | |
