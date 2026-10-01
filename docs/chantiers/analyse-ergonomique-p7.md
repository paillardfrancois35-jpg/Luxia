# Pré-analyse ergonomique – P7 « Audio et tempo »

> Doc 32 §5.6, rédigée le 2026-10-01 **avant** l'essai en discussion test, à partir : du développement, des captures hors
> matériel de `tools/Luxia.Tools.Captures` (écran Audio, écran de jeu, fenêtre d'édition à 1366 × 768), de l'essai informel
> de l'utilisateur pendant le développement et de la charte [doc 60](../60-ergonomie.md). Elle sera **reprise et complétée
> à la fin de la phase** avec les résultats d'essai ([essais/P7-resultats.md](../essais/P7-resultats.md)). Statuts :
> ✅ fait · ⏳ proposé · ❓ décision de l'utilisateur · 🔎 à observer à l'essai. Documents sœurs :
> [analyse de code](analyse-code-p7.md), [analyse de la documentation](analyse-docs-p7.md).

## 1. Ce que l'essai informel a déjà montré

| Constat de l'utilisateur ou observation | Nature | Suite |
|---|---|---|
| « Bloc BPM ok », « Audio franchement très correct, impressionnant » | Le fonctionnel convainc | — |
| **Les boutons du bloc BPM bougeaient** : le texte « confiance xx % » changeait de largeur | Mise en page instable (principe de l'essai « Contrôle 2 » : *toute mise en page reste stable quand une information change*) | ✅ largeurs fixes |
| « Il manque le rappel des voyants de rythme dans l'onglet Audio » | Un même état doit se voir pareil partout où il est utile | ✅ voyants 1-2-3-4 ajoutés |
| **« La lyre tourne mais sans lumière »** | Incompréhension du modèle de couches : une scène de mouvement n'allume rien (il faut la couche Intensité) | ✅ scène « Lyres allumées », consigne au guide ; cause profonde en E1 |
| **« Un PAR par temps : chaque étape allume un PAR mais rien en direct »** | Même cause : *Plein feu* (HTP) masquait l'intensité des PAR | ✅ idem ; E1 |

**Enseignement** : les trois difficultés vraiment rencontrées ne venaient pas du tempo ni de l'analyse mais de la **lisibilité
du modèle de couches** (pourquoi ça ne s'allume pas, pourquoi ça ne bouge pas). C'est le premier sujet d'ergonomie de la phase.

## 2. Principes de la charte appliqués à P7

1. **Un état affiché est l'état réel** (source du tempo, écoute en cours, attente d'un lancement).
2. **Mise en page stable** : aucune largeur ne dépend d'une valeur (corrigé pour le bloc BPM ; à vérifier pour l'écran Audio : largeurs fixes posées).
3. **Une action, un endroit** ; un mot pour une notion (voir E4).
4. **1366 × 768 sans défilement** pour ce qu'on regarde en jouant.
5. **Un refus ou un effet invisible s'explique** (journal, bulle, badge), jamais en silence.

## 3. Constats et propositions

| # | Gravité | Constat | Proposition | Coût | Suite |
|---|---|---|---|---|---|
| **E1** | **Haute** | **L'effet d'une scène d'intensité est invisible dès que *Plein feu* joue** : la couche Intensité est HTP (la plus haute valeur l'emporte), *Plein feu* tient tous les appareils à 100 % ; rien n'explique pourquoi un chenillard ou un flash ne se voit plus | (a) **Ligne de journal** à l'appui sur *Plein feu* quand une scène joue sur l'intensité : « Plein feu tient tous les appareils à 100 % : les scènes qui jouent sur l'intensité ne se voient plus » ; (b) **info-bulle** sur les scènes de la colonne Intensité ; (c) à terme, **badge « masquée »** sur une scène dont toute la contribution d'intensité est dominée (l'instantané sait déjà dire d'où vient chaque valeur, GEN-043) ; (d) renommer en **« Plein feu (tout à 100 %) »** | (a) (b) (d) petit ; (c) moyen | ❓ |
| **E2** | **Haute** | **Plus aucun moyen de saisir une durée en temps ou en mesures** dans la fenêtre d'édition (fondu, maintien, fondus de scène : secondes seulement) ; seul l'ancien écran Scènes a le choix s / temps / mesures. Le retrait de cet écran rendrait MOT-016 sans interface | Porter le **choix d'unité** dans les Propriétés et les étapes (liste « s / temps / mesures » à côté de chaque durée) **avant** de retirer l'écran Scènes | moyen | ❓ à faire dans cette phase ou juste après |
| **E3** | Moyenne | **L'écran Audio fait 1 150 px de haut** : à 768 px, Énergie, Réglages et Calibration sont sous le pli, deux défilements pour voir ce qu'on règle. Ce qu'on **regarde** en jouant (entend, tempo, énergie, événements) et ce qu'on **règle une fois** (réglages, calibration) sont mélangés | **Deux colonnes** : à gauche le direct (Écoute, Entend, Tempo, Énergie, Événements), à droite les réglages et la calibration, repliables. Gabarit : § 5. Objectif : tout le direct sans défilement à 768 px | moyen | ⏳ maquette puis développement |
| **E4** | Moyenne | **Quatre mots pour deux notions** : « Écouter » (case de l'écran Audio), « Écoute » (case du bloc BPM), « 🎧 Audio » (bouton du bloc), « Suivre le tempo de la musique » (bouton de l'écran). *Écouter* = recevoir le son ; *Audio* = prendre le tempo de ce son | Un vocabulaire unique : **« Écouter »** (case) et **« Tempo : Fixe · Tap · Audio »** (choix). Retirer « Suivre le tempo de la musique » | petit | ⏳ |
| **E5** | Moyenne | **La source du tempo s'affiche en texte gris et se change par trois boutons de natures différentes** (TAP, Fixer, 🎧 Audio) : l'état n'est pas visible comme un état (principe 1) | **Sélecteur à trois positions** (Fixe · Tap · Audio), la position active en surbrillance (comme le verrou soirée en bleu) | petit | ⏳ |
| **E6** | Moyenne | **La confiance est un pourcentage sans signification** (« confiance 62 % ») ; le seuil de 30 % (l'horloge garde son tempo) est invisible | Afficher une **pastille de couleur** : vert à partir de 70 %, orange de 30 à 70 %, rouge en dessous (« tempo conservé ») ; garder le pourcentage en info-bulle ; la pastille a une largeur fixe | petit | ⏳ |
| **E7** | Moyenne | **Rien n'indique qu'une scène dépend de l'horloge** : avance au temps, départ quantifié, horloge propre, période en temps, vitesse selon l'énergie. L'horloge démarre à **120 BPM fixes** sans que rien le dise : une scène « au temps » tourne donc au rythme fictif de 120 | **Badge ♪** sur les boutons de scène qui dépendent de l'horloge (info-bulle : ce qu'elles suivent) ; badge **⏱** pour un départ quantifié, *avant* l'appui ; bloc BPM : libellé **« 120 BPM fixe »** tant que la source est Fixe | petit à moyen | ⏳ |
| **E8** | Moyenne | **Le départ quantifié** : à l'appui il ne se passe rien pendant jusqu'à huit mesures ; seul le texte « ⏳ dans N t » de l'état le dit ; rien n'indique que le second appui annule | Info-bulle « Démarre au début de la prochaine mesure ; cliquez encore pour annuler » ; liseré du bouton pendant l'attente ; compte à rebours en **temps de la mesure** (« dans 3 »), pas en « t » | petit | ⏳ |
| **E9** | Basse | **Volet « Au rythme »** : fermé par défaut, parmi quatre volets ; « Tous les » ne dit pas quoi (temps, mesures, impulsions ?) ; il reste affiché quand l'avance est « à la durée de l'étape » | En-tête qui **résume les réglages non standard** (« Au rythme · à chaque mesure · départ à la prochaine mesure ») ; « Tous les N **temps / mesures / impulsions** » dynamique ; masquer « Tous les » pour « à la durée de l'étape » | petit | ⏳ |
| **E10** | Basse | **Trois représentations du même temps** dans le panneau Tempo de l'écran Audio : le numéro, quatre points, une barre de phase | Garder le numéro (accessibilité : la couleur seule ne suffit pas) et les points ; **retirer la barre de phase** ; ajouter le numéro dans le bloc BPM (qui n'a que les points) | petit | ⏳ |
| **E11** | Basse | **États vides muets** : avant l'écoute, l'écran Audio n'est que des barres grises ; la liste « Derniers événements » est vide sans un mot | « Cochez *Écouter*, puis lancez un morceau (Deezer, YouTube Music, VLC) » ; « Aucun événement pour l'instant » | petit | ⏳ |
| **E12** | Basse | **Calibration** : le bouton « Lancer le flash » ne change pas d'état (la propriété `Calibrating` existe mais n'est liée à rien) ; la convention de signe (« positif : la lumière avance ») est à relire deux fois ; aucune mesure automatique | Bouton **à bascule** (Lancer ↔ Arrêter, rouge pendant le flash) ; afficher « la lumière est en avance / en retard de 40 ms » selon le signe ; idée : **calibration par tap** (taper sur le kick entendu, LuXia mesure l'écart moyen) | petit ; l'idée : moyen | ⏳ / ❓ |
| **E13** | Basse | **Bloc BPM à 15 commandes sur une ligne** à 1366 px : lisible mais dense ; « Fixer » + case de saisie occupent beaucoup pour un usage rare | Regrouper par séparateurs (valeur et ± · tap et corrections · voyants · source) ; remplacer « case + Fixer » par un **BPM éditable sur place** (clic, saisie, Entrée) | moyen | ⏳ |
| **E14** | Basse | **Clavier et MIDI** : seule la touche **T** existe ; « 1 ici » et ×2 / ÷2 n'ont pas de raccourci ; aucun pad de l'APC ne tape le tempo | Raccourcis **1** (« 1 ici »), **[** / **]** (÷2 / ×2) affichés dans les info-bulles ; **un pad de tap** dans les profils des APC (affectation fixe, l'apprentissage MIDI-008 reste reporté) | petit | ❓ |
| **E15** | Basse | **Les événements musicaux (break, drop, niveau) ne laissent aucune trace dans le Journal** de l'écran de jeu, où l'on voit déjà « ♪ tempo … » | Les y écrire (break, drop, montée seulement ; pas les changements de niveau, trop bavards) | petit | ⏳ |
| **E16** | **Moyenne** | **Une démonstration demande neuf gestes et la connaissance des couches** (Plein feu ou Lyres allumées, scène de couleur, mouvement, UV, écoute, source) | Deux **looks** livrés dans le show de référence, **« Démo musicale 1 : kick et drop »** et **« Démo musicale 2 : chenillard au tempo »**, déclenchés par F1 / F2 ; une **action de look « source du tempo »** (Audio) pour que l'appui règle aussi l'horloge | moyen | ❓ |
| **E17** | Basse | **Aperçu d'édition** : l'horloge de l'aperçu n'est pas calée sur le direct (code C7) : un effet « une mesure » ou un départ quantifié ne tombe pas sur la musique en ÉDITION | Le dire dans l'aide « ? » de l'aperçu ; ou recopier la position | petit | ⏳ |
| **E18** | Basse | **Microphone** : si Windows refuse l'accès, l'écran affiche « Écoute impossible : … » sans dire où l'autoriser | Message : « Autorisez l'accès au micro : Paramètres Windows → Confidentialité → Microphone → Applications de bureau » | petit | ⏳ |
| **E19** | Info | **Place de l'écran Audio** dans la navigation (après Simulateur, avant Sorties) | Convient ; à réexaminer au retrait de Live et Scènes | — | — |

## 4. Parcours mesurés (à rejouer avec l'utilisateur)

| Parcours | Gestes aujourd'hui | Objectif | Remarque |
|---|---|---|---|
| Démo « kick et drop » (*Animals*) | 1 regénération du show, 1 ouverture, 1 clic 🎧 Audio, 4 scènes, 1 lecture du morceau = **7 gestes** et la connaissance des couches | 1 touche (F1) après *Écouter* | E16 |
| Corriger un tempo à la moitié | 1 clic (÷2) | — | ✅ |
| Caler le « 1 » | 1 clic (« 1 ici ») ; **tenait mal en source Audio** | 1 clic qui tient | ✅ corrigé (code C1) |
| Changer de morceau | 0 geste si la musique est propre (retrouve le tempo en < 8-10 s) ; 1 (« 1 ici ») si le « 1 » est faux | — | 🔎 exemples 16 et 17 |
| Voir pourquoi une scène « ne fait rien » | Aucun moyen direct (il faut savoir lire les couches) | journal et badge | E1 |
| Régler la latence | 6 gestes (écran Audio, lancer, régler, regarder, arrêter) | 4 | E12 |

## 5. Gabarit proposé pour l'écran Audio (E3, E4, E5, E10, E11)

```text
┌ Audio ────────────────────────────────────────────────────────────────────────┐
│ Écouter ☑   Périphérique [Le son joué par le PC ▾]  ⟳        état : Écoute : Casque…│
├───────────────────────────────────────────────┬────────────────────────────────┤
│ Tempo : (Fixe) (Tap) [Audio]   128,0 BPM  ●vert │ Réglages (repliable)            │
│ temps 3   ● ● ◉ ●        [×2] [÷2]            │  Sensibilité des impulsions  50 │
├───────────────────────────────────────────────┤  Lissage de l'énergie      2,0 s │
│ Niveau  ▓▓▓▓▓▓▓▓░░   Basses ▓▓▓▓▓░░░░░         │  Tempo min / max / préféré       │
│ Médiums ▓▓▓▓░░░░░░   Aigus  ▓▓░░░░░░░░         │  Latence (ms)            ± 0    │
│ Impulsions : basses ◉  aigus ○                 ├────────────────────────────────┤
├───────────────────────────────────────────────┤ Calibration de la latence        │
│ Énergie ▓▓▓▓▓▓▓░░░  Énergique ↗  BREAK         │  [▶ Flash sur chaque temps]      │
│ 2:14 DROP · 1:55 BREAK · 1:30 montée …        │  la lumière est en avance de 40 ms│
└───────────────────────────────────────────────┴────────────────────────────────┘
```

Hauteur visée : environ 600 px, donc tout le direct sans défilement à 768 px. **À valider par maquette avant tout
développement** (règle du chantier « Contrôle 2 »), sur le même principe que les maquettes 5 à 8.

## 6. Décisions demandées à l'utilisateur (après l'essai)

1. **E1** : valider (a), (b), (d) ; (c) le badge « masquée » est-il voulu ?
2. **E2** : porter le choix d'unité dans les Propriétés **dans cette phase** (recommandé) ou après ?
3. **E3 à E5** : refonte de l'écran Audio en deux colonnes et sélecteur de source : maquette d'abord ?
4. **E14** : pad de tap sur l'APC, et lequel ?
5. **E16** : les deux looks de démonstration, avec une action de look « source du tempo » ?
6. **Premier temps automatique ou manuel** (code C2) : voulez-vous que « 1 ici » reste la règle par défaut ?

## 7. À observer pendant l'essai (pour trancher avec des faits)

- Temps mis pour comprendre qu'*une scène d'intensité est masquée* (E1), et si le guide suffit (exemples 5, 6, 13).
- Nombre de défilements sur l'écran Audio à 1366 × 768 (E3).
- Confusion entre « Écouter », « Écoute » et « 🎧 Audio » (E4).
- Si la pastille de confiance aurait évité un doute (E6), notamment sur les ballades (exemple 25).
- Si l'utilisateur cherche spontanément à **saisir une durée en temps** (E2) ou à **caler le 1** autrement.
- Si le flash de calibration se règle en moins de trois réglages (E12).

## 8. À rejouer à la fin de la phase

1. Passer chaque ligne E1 à E19 à ✅, ❌ (refusée, avec la raison) ou ⏳ (rangée dans doc 40 / 99).
2. Ajouter ce que l'essai a montré qui n'est pas dans ce tableau ; refaire les captures 1366 × 768 (`luxia-captures`) de l'écran Audio, du bloc BPM et du volet « Au rythme ».
3. Reporter les règles retenues au doc 60 (§4.10 « Horloge musicale et écoute », voir l'analyse de la documentation D12).

## 9. Après l'essai du 2026-10-01 (v1.009.065) : constats et file de décisions

> Ajout de la discussion dev, à partir de [essais/P7-resultats.md](../essais/P7-resultats.md) (bilan) et des idées de l'utilisateur.
> Les anomalies fonctionnelles (impulsions, énergie, drop, bloc BPM, micro, avis, boutons, calage) sont corrigées en **1.009.077** ;
> restent ici les idées d'ergonomie, à décider **une par une**.

### 9.1 Ce que l'essai a confirmé ou infirmé

| # | Constat de l'essai | Effet sur l'analyse |
|---|---|---|
| E1 | Plein feu masquant une scène : non rencontré à l'essai (le guide l'annonçait) | Reste valable ; badge « masquée » toujours proposé, non urgent |
| E3 | « Un défilement à 1920 × 1080 », « gros vide à droite » ; la liste des événements est introuvable (« c'est où ? ») | Confirmé : refonte de l'écran Audio (idée 2) |
| E4, E5 | **Spécification de l'utilisateur** : un seul bouton **Audio** à bascule (gris / bleu), plus de libellé Tap / Fixe / Audio, plus de case « Écoute », champ BPM et boutons manuels grisés quand Audio est actif, confiance à droite du bouton | Remplace E4 et E5 (idée 1) |
| E6 | La confiance se lit dans l'écran Audio ; le bloc BPM la montrait même hors écoute (défaut corrigé) | La pastille de couleur reste utile (idée 1) |
| E9 | « Revoir la matrice de gestion ou les règles de ce panneau, entièrement » : la case « vitesse selon l'énergie » ne joue pas avec « À chaque temps » | Confirmé (idée 7) |
| E12 | Calibration à 0 ms jugée conforme ; mesure 2 : flash et kick indiscernables | Satisfaisant ; le bouton à bascule reste un petit confort |
| E14 | La touche T doit allumer TAP ; **Entrée** = Fixer (idées 4 et 5) | Faits simples, sans décision |
| E16 | Démonstrations : plusieurs gestes ; l'utilisateur a bien suivi *Animals* et *Sandstorm* | Reste une idée (looks de démonstration) |

### 9.2 Idées de l'utilisateur et décisions à prendre

| N° | Idée | Ma recommandation | Décision |
|---|---|---|---|
| 1 | **Bloc BPM** : bouton Audio à bascule ; Audio actif = BPM, TAP, + et − grisés ; confiance à droite ; plus de libellé de source ni de case Écoute | Oui ; **garder actifs ×2, ÷ 2 et « 1 ici » en Audio** (ils corrigent l'analyse : ballades à 6/8, premier temps) ; le champ BPM affiche le tempo suivi, grisé | ✅ **Décidé le 2026-10-01** : bouton Audio à bascule (gris / bleu) ; Audio actif : champ BPM, TAP, + et − grisés ; **×2, ÷ 2 et « 1 ici » restent visibles et actifs** (corrections de l'analyse : octave, premier temps) ; confiance à droite du bouton ; plus de libellé Tap / Fixe / Audio ni de case Écoute |
| 2 | **Écran Audio** sur une page à 1920 × 1080, niveaux **verticaux** dans un panneau à droite | Panneau de droite repliable | ✅ **Décidé le 2026-10-01** : une page ; à gauche le direct (écoute et périphérique, tempo et confiance, énergie, événements) ; à droite les niveaux verticaux (niveau, basses, médiums, aigus, impulsions) puis « Réglages » et « Calibration » **repliables** ; maquette à valider avant le développement |
| 3 | **Titre du morceau en cours** (écran Audio et bloc BPM) | Fonction « Lecture en cours » de P9 | ✅ **Décidé le 2026-10-01** : **attendre P9** (MUS-001 à 003 ; lecture de la session média de Windows, choix de la session, titres de vidéos, base musicale ; passer plusieurs projets sur une cible Windows 10 ne se justifie pas pour un affichage seul) |
| 4 | **Touche T** allume le bouton TAP | Oui, sans décision | à faire |
| 5 | **Entrée** dans la case BPM = Fixer | Oui, sans décision (devient inutile si la case disparaît : voir 1) | à faire |
| 6 | **Rappel du BPM et des voyants** dans le **Simulateur** (lecture seule) | Oui, même composant que le bloc de l'écran Audio | à faire |
| 7 | **Volet « Au rythme »** : revoir toutes les règles | Réglages inopérants masqués + phrase de résumé | ✅ **Décidé le 2026-10-01** : seuls les réglages qui agissent sont affichés selon « Étape suivante » ; une phrase d'en-tête résume la scène (« Change d'étape à chaque mesure, démarre au début de la prochaine mesure ») ; « La vitesse suit l'énergie » n'apparaît pas quand les étapes suivent les temps |
| 8 | **Facteur de rythme dans les deux sens** (÷ 8 … × 4) sans changer le BPM affiché | À définir : par scène (le volet « Au rythme ») ou global (bloc BPM) ? | ❓ |
| 9 | **Mesure à 3 temps** (valses) | Aujourd'hui 4 temps fixes ; prévu en P8 (GEN-025) pour la séquence ou le morceau ; détecter 3/4 automatiquement est un autre travail | ❓ |
| 10 | Autres idées de l'analyse : durées en temps / mesures dans les Propriétés (E2), Plein feu (E1), looks de démonstration (E16), pad de tap sur l'APC (E14) | Reprises après les neuf ci-dessus | ⏳ |

### 9.3 Faits simples à réaliser sans attendre une décision

Touche T allumant TAP, Entrée = Fixer (jusqu'à la refonte du bloc), rappel du tempo dans le Simulateur : à faire dans la prochaine
version, avec les décisions 1 à 3.

