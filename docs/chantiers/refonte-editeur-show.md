# Cahier des charges – Refonte de l'éditeur de show

> Préparé le 2026-10-02, en fin de P8, à la demande de l'utilisateur après l'essai ([essais/P8-resultats.md](../essais/P8-resultats.md),
> exemples 12 et 16). Destiné à une **discussion dédiée** (ergonomie), à ouvrir après la validation de P8. Documents liés :
> [analyse ergonomique de P8](analyse-ergonomique-p8.md), cahier des charges des shows ([doc 20](../20-show-et-sequences.md) §3),
> guide de conception ([doc 51](../51-guide-conception-shows.md)), charte ([doc 60](../60-ergonomie.md) §4.11), maquette d'origine
> [maquette 12](../maquettes/maquette-12-edition-show.png).

## 1. Ce que dit l'utilisateur

- Ex. 12 : « WOUAH !!! Elle est très très complexe cette interface ! »
- Ex. 16c : « Quand je dis que c'est un bordel cet écran… J'ai ouvert la bonne [liste] quelque part ? Tu vois « Aussitôt » toi ? »
- Ex. 16c : « Bien compliqué ce test… »
- Conclusion : « Il faudra dédier une discussion à une refonte ergonomique en profondeur de cette interface. Au final, c'est
  principalement une IA qui devrait générer ces shows, mais tout de même, j'aimerais qu'humainement ce soit compréhensible. »

**Lecture** : l'utilisateur n'est pas du métier ; il doit **comprendre** un show (le lire, le vérifier, le retoucher), pas
forcément le construire de zéro. Le premier besoin est donc la **lisibilité** ; la création complète passe au second plan, car
elle sera surtout faite par une IA (P10, Directeur) ou par duplication d'un exemple.

## 2. L'écran actuel (v1.010.091)

Captures hors matériel : [éditeur de show](captures-p8/editeur-show.png), [éditeur de séquence](captures-p8/editeur-sequence.png)
(pour comparaison : jugé clair), [bandeau déplié](captures-p8/bandeau-detail.png) (supervision, jugée claire).

| Zone | Contenu | Problème observé |
|---|---|---|
| Gauche : **cartes d'étapes** | Pour chaque étape : identifiant, nom, « initiale », macro-étape, liste « Pendant l'étape », ajout d'action (2 listes + bouton), puis **toutes ses transitions** (condition, paramètres, « vers », quantification, poids, « depuis », ↑ ↓ ✕), « tirage au sort », « éviter la même » | Tout est déplié en même temps ; une carte fait une demi-hauteur d'écran ; il faut défiler pour voir la 2e étape ; les transitions sont rangées **dans l'étape de départ**, alors que le diagramme les montre **entre** deux étapes |
| Droite : **diagramme** | Dessiné automatiquement ; clic sur une étape = montrer sa carte | Lisible, mais en **lecture seule** : on ne peut rien y faire, d'où des allers-retours avec les cartes |
| Bas : **essai sans musique** | Jouer, métronome, Drop / Break / Montée / Silence / Morceau suivant, Énergie, Style, « Étape active / Ensuite » | Utile ; à garder |
| Bas : **problèmes** | ⛔ erreurs, ⚠ avertissements | Corrigé pendant l'essai : couleurs distinctes, confirmation à Valider |
| Repli : « Pour le pilote automatique (P10) et variables » | Rôle, styles, énergie, poids, durée max, secondaire, variables | Sans intérêt pour l'utilisateur aujourd'hui (servira au Directeur) |

## 3. Vocabulaire actuel (à revoir)

| Notion | Mot affiché aujourd'hui | Remarque |
|---|---|---|
| Étape | « étape », identifiant libre (« 0 », « 2a ») | L'identifiant technique se voit partout (« vers 1 ») ; un nom suffirait |
| Étape de départ | « initiale » (case) | « Départ » plus parlant |
| Sous-show | « Macro-étape » | Terme de Grafcet ([glossaire](../glossaire.md)) ; « joue un autre show » plus parlant |
| Transition | « transition », réceptivité « si… » | Le mot « réceptivité » n'est plus affiché, bien |
| Plusieurs étapes à la fois | « vers » avec plusieurs identifiants (« 2a, 2b »), « depuis » | Invisible tant qu'on ne le sait pas |
| Tirage | « tirage au sort », « éviter la même », « poids » | Poids sans unité |
| Quantification (6) | « Tout de suite », « Au prochain temps », « À la prochaine mesure », « À la prochaine phrase (4 / 8 / 16 mesures) » | Liste distincte de la condition, mais « aussitôt » (condition) et « Tout de suite » (quantification) se ressemblent |

**Conditions (19 choix, dans cet ordre)** : après une durée · au drop · au break · à la montée · au morceau suivant · au silence ·
à la reprise du son · niveau d'énergie · énergie qui passe au-dessus de… · énergie qui passe sous… · fin d'une séquence ·
séquence jouée N fois · fin d'une scène · variable · au hasard · style · tempo entre… · à la main seulement · aussitôt.
(Conditions composées ET / OU / NON : possibles dans le fichier, pas encore dans l'éditeur, doc 99.)

**Actions (13 choix)** : jouer une scène (pendant l'étape) · jouer une séquence (pendant l'étape) · lancer une scène et la laisser ·
lancer une séquence et la laisser · arrêter une scène · arrêter une séquence · arrêter une couche · niveau d'une couche ·
vitesse d'une scène · flash d'une scène · rafale de fumée · noir court · compter (variable + 1).

**Fin du show (3)** : tenir la dernière étape · s'arrêter · reprendre au début.

## 4. Objectifs

1. **Lire un show en 10 secondes** : ce qu'il joue, ce qui le fait avancer, où il va. Une phrase par transition, en français
   (« Au drop, à la prochaine mesure → Refrain »).
2. **Une seule représentation de la structure** : le diagramme **est** l'éditeur (cliquer une flèche = la modifier), ou les cartes
   **sont** le diagramme ; pas les deux en parallèle.
3. **Montrer peu, déplier à la demande** : l'étape choisie seule en détail ; les autres en une ligne.
4. **Des listes courtes et rangées** : conditions classées (Musique · Temps · Séquences et scènes · Avancé), le plus courant en tête.
5. **Les cas avancés existent mais ne se voient pas** tant qu'on ne les demande pas (branches parallèles, macro-étapes, tirage,
   variables, conditions composées).
6. **Partir d'un modèle** : « nouveau show » propose les exemples (couplet / refrain, visite guidée, tirage…) plutôt qu'une page vide.
7. **Rester compatible** avec le format `shows.json` (doc 50) et avec les shows générés par une IA.

## 5. Pistes à comparer dans la discussion (sans préjuger)

- **A. Diagramme éditable** : étapes en boîtes, flèches cliquables, panneau latéral pour l'élément choisi (étape ou transition).
- **B. Liste lisible** : une ligne par étape (« Refrain : joue … ; au break → Couplet ; après 16 mesures → Couplet ») ; clic = édition
  dans un panneau ; diagramme en vignette.
- **C. Assistant** : questions guidées (« Que se passe-t-il au drop ? ») pour les cas simples, l'éditeur complet en mode avancé.
- Dans tous les cas : la **phrase en français** de chaque transition, le **classement des conditions**, et la fusion
  « aussitôt » / « Tout de suite » en un vocabulaire unique.

## 6. Méthode proposée

1. Trois maquettes textuelles (A, B, C), l'utilisateur tranche (comme Q44).
2. Maquettes dessinées de la solution retenue (outil `Luxia.Tools.Prototype`, comme les maquettes 9 à 12).
3. Développement, puis essai en discussion test sur les mêmes exemples (12, 16) : **critère de réussite** : l'utilisateur crée le show
   de l'exemple 16 et lit *Couplet / Refrain / Drop* sans aide.

## 7. Message pour ouvrir la discussion

> Nous reprenons le projet LuXia (dépôt `D:\Develop\Claude\CSharp\DMX`). Lis `docs/32-passation.md`, puis
> `docs/chantiers/refonte-editeur-show.md` et ses liens. Objectif : refonte ergonomique de l'éditeur de show. Commence par me
> proposer trois solutions textuelles (pas de maquettes), puis je tranche. Réponds toujours en français ; mon terminal est PowerShell.
