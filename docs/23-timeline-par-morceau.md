# 23 – Timeline par morceau

> Cahier des charges – module **Timeline** (préfixe `TL`). Phase : **P11** (priorité basse).
> Dépend du résultat de la **PoC-3** (précision de la position de lecture).
> Références : [20 (séquences)](20-show-et-sequences.md), [21 (lecture en cours)](21-lecture-en-cours-et-style.md), [19](19-audio-et-tempo.md).

---

## 1. Rôle

Caler un spectacle lumineux **à la seconde près sur un morceau précis**, pour les moments forts préparés à l'avance :
ouverture de bal, première danse, entrée des mariés, gâteau, morceau fétiche. Contrairement aux séquences en mesures (doc 20),
une timeline n'est **pas réutilisable** : elle appartient à un titre.

## 2. Principes

- Une timeline est **associée à un titre** de la base musicale (doc 21) ; elle se déclenche automatiquement quand ce titre est
  reconnu (si l'option est active), ou manuellement.
- Pendant qu'une timeline joue, elle **a la main** sur les couches qu'elle utilise ; le Directeur suspend ses décisions sur ces couches.
- Le calage repose sur la **position de lecture** fournie par Windows, **interpolée**, et corrigée par l'analyse audio et par
  un recalage manuel.

## 3. Synchronisation

| Mécanisme | Rôle |
|---|---|
| Position du lecteur | Base de temps (souvent imprécise, rafraîchie irrégulièrement selon l'application) |
| Interpolation | Entre deux mises à jour, la position avance au rythme de l'horloge temps réel |
| Grille de temps | Si le tempo du morceau est connu, la timeline peut être exprimée en mesures et **se caler sur les temps détectés** (plus précis que la position) |
| Recalage manuel | Bouton « **Top** » : l'utilisateur appuie sur un repère audible (début du morceau, premier temps du refrain) |
| Empreinte | Option : enregistrer, lors d'une première écoute, l'enveloppe d'énergie du morceau (capture audio) ; aux lectures suivantes, recaler par corrélation avec l'énergie mesurée |

## 4. Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| TL-001 | I | P11 | Éditeur : pistes (une par couche), blocs de scènes et d'actions positionnés en secondes **ou** en mesures, marqueurs nommés (couplet, refrain, drop…), zoom, lecture en boucle d'une portion. | — |
| TL-002 | I | P11 | Association à un titre de la base ; déclenchement automatique à la reconnaissance du titre (option) ou manuel. | Lancer le titre → timeline démarrée. |
| TL-003 | I | P11 | Suivi de la position interpolée ; écart maximal toléré réglable ; au-delà, recalage progressif (pas de saut visible). | Écart constaté < 200 ms sur les lecteurs du PoC-3 (objectif, à confirmer). |
| TL-004 | I | P11 | Recalage manuel « Top » sur un marqueur choisi. | — |
| TL-005 | I | P11 | Pause / reprise / saut dans le morceau suivis (la timeline se met en pause, reprend, se repositionne). | — |
| TL-006 | I | P11 | Priorité sur le Directeur pour les couches utilisées ; retour au Directeur à la fin du morceau. | — |
| TL-007 | M | P11 | Affichage de l'**enveloppe d'énergie** enregistrée (empreinte) dans l'éditeur, pour placer les blocs visuellement. | — |
| TL-008 | M | P11 | Mode « mesures » : si le BPM est connu, la grille est en mesures et les blocs se calent sur les temps détectés. | — |
| TL-009 | S | P11 | Recalage automatique par corrélation avec l'empreinte d'énergie. | Écart < 100 ms après recalage. |
| TL-010 | S | P11 | Préparation « au métronome » sans le morceau (BPM + durée saisis). | — |

## 5. Tests

| Test | Type | Contenu |
|---|---|---|
| T-TL-01 | Unitaire | Interpolation de position, recalage progressif, pause / saut. |
| T-TL-02 | Intégration | Timeline sur fichier audio de référence : instants des commandes vs marqueurs. |
| T-TL-03 | Manuel | 3 morceaux réels sur 2 lecteurs différents : écart mesuré (vidéo + son). |
