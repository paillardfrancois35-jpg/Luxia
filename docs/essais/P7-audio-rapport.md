# Rapport chiffré – analyse du jeu de test audio (P7)

> Produit par `luxia-headless audio tests/assets/audio --rapport rapport.md` (doc 19 §7, T-AUD-02). **Troisième édition**,
> version 1.009.099 du 2026-10-02 (fin de P7, après les correctifs de l'essai : un kick doit s'accompagner d'un claquement) ; la
> deuxième (1.009.077) et la première (1.009.059) donnaient les mêmes tempos mais d'autres impulsions et énergies. Le jeu de test (41 morceaux, `tests/assets/audio`, non versionné) a été déposé par
> l'utilisateur : pour chaque famille, un titre « effet waouh » et un titre « défi technique ». Les BPM de référence sont dans
> `annotations.csv` (songbpm, tunebat, ou de mémoire : à confirmer) ; `0` ou `?` = pas de référence. Diagnostic plus fin
> (impulsions sur les temps, passages sans basses, niveaux d'énergie) : `luxia-headless audio-diag`.

## Lecture

- **Tempo** : sur les 25 morceaux qui ont une référence, **21 (84 %) sont justes à ± 2 %** (comme avant). Écarts : *Blinding Lights*
  à 85,5 (référence 171 ou 86 en demi-temps : les deux sont justes), *Perfect* à 95 pour 63 (6/8), *Someone Like You* à 133,5 pour 67
  (octave, confiance basse : l'horloge garde son tempo), *Tití Me Preguntó* à 103 pour 98 (référence de mémoire).
- **Calage** (colonne « Convergence », dernier instant hors de ± 2 %) : **4 s** pour onze morceaux réguliers (*Animals*, *Glue*, *The Rhythm
  Of The Night*, *What Is Love*, *Don't Start Now*, *Another One Bites The Dust*, *Hips Don't Lie*…), 8 à 10 s pour *Le Freak*, *Without Me*,
  *HUMBLE* ; plus long quand l'introduction n'a pas de rythme (*24K Magic* 26 s, *Sandstorm* 19 s) ou que le tempo bouge (*September*, *Boogie
  Wonderland*). *Summer* : 12 s d'après `audio-diag` (24 s ici, la colonne exige de rester dans ± 2 % jusqu'à la fin).
  Avant correction : 15 à 30 s sur ces morceaux.
- **Impulsions des basses** (`audio-diag`, troisième édition : seuls les coups de batterie comptent, une note de basse seule ne compte plus) : de 0,5 à 3,0 par seconde selon le morceau, **1.5 en moyenne** (2,0 à la deuxième édition, 2,3 à 2,6 **partout** à la première) ; **part sur les temps 55 % en moyenne** (48 % avant), par exemple *Sandstorm* 81 % (33 % avant), *bad guy* 78 % (52 %), *Summer* 77 % (66 %) ; dans les passages sans basses
  0,0 à 0,3 par seconde (1,9 à 2,6 avant) ; part sur les temps de 60 à 75 % pour les kicks réguliers (*Boogie Wonderland* 75 %, *Gasolina* 72 %, *Another One Bites The Dust* 71 %, *Summer* 66 %),
  27 à 40 % quand la grille de temps est calée sur un autre instrument que le kick (*Sandstorm*, *HUMBLE*, *24K Magic*) : piste d'amélioration (caler la grille sur les kicks).
- **Énergie** : « Explosif » 0 à 40 % du temps (80 % avant), changements de niveau 1 à 7 par minute (4 à 12 avant).
- **Break et drop** : *Animals* : break à 17,7 s, drop à 21,0 s (la pause réelle) ; *Glue* 119 s → 133 s ; *Summer* break à 163 s, drop à 184 s ;
  le rap (*HUMBLE*) et *bad guy* en donnent beaucoup (arrêts brefs). Pas d'annotation des drops : critère AUD-062 (≥ 80 % à ± 1 temps) non mesuré.
- **Confiance** : élevée sur les titres réguliers, basse sur les défis (rubato, mesures irrégulières) ; sous 0,3 l'horloge garde son tempo.
- **Premier temps de la mesure** : connu de 37 à 88 % du temps ; non annoté (AUD-024 reste partiel).
- **Durée d'analyse** : environ 15 s par morceau ; l'analyse seule consomme moins de 5 % d'un cœur en temps réel (AUD-007), 2 % mesurés dans l'application.

## Tableau

| Morceau | BPM mesuré | BPM attendu | Écart | Convergence | Confiance | 1er temps connu | Kicks/s | Aigus/s | Énergie | Breaks | Drops | Montées |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Animals (Radio Edit) | 128.0 | 128 | ✓ ±2 % | 4 s | 0.96 | 40 % | 1.3 | 2.4 | 0.56 | 5 | 4 | 4 |
| Glue | 130.0 | 130 | ✓ ±2 % | 4 s | 1.00 | 88 % | 1.4 | 4.0 | 0.51 | 4 | 2 | 4 |
| Love Tonight Vs Aria Vs Give Me Everything (David Guetta Mashup) (Lee Barzola Remake) | 126.0 | 0 | — | 62 s | 0.93 | 62 % | 1.2 | 3.3 | 0.61 | 2 | 1 | 0 |
| Summer | 128.0 | 128 | ✓ ±2 % | 24 s | 0.99 | 62 % | 1.6 | 2.0 | 0.58 | 2 | 1 | 2 |
| Titanium (feat. Sia) | 125.9 | 0 | — | 4 s | 0.75 | 35 % | 1.1 | 2.4 | 0.49 | 1 | 0 | 4 |
| Children | 137.0 | 137 | ✓ ±2 % | 114 s | 1.00 | 40 % | 1.7 | 3.5 | 0.61 | 1 | 0 | 0 |
| Sandstorm (Radio Edit) | 136.0 | 136 | ✓ ±2 % | 19 s | 0.99 | 46 % | 1.6 | 2.8 | 0.61 | 5 | 4 | 0 |
| The Rhythm Of The Night | 127.8 | 128 | ✓ ±2 % | 4 s | 0.98 | 64 % | 2.2 | 5.6 | 0.55 | 2 | 1 | 4 |
| What Is Love (7 Mix) | 123.8 | 123 | ✓ ±2 % | 4 s | 0.98 | 83 % | 2.4 | 4.5 | 0.55 | 2 | 1 | 0 |
| 24K Magic | 107.0 | 107 | ✓ ±2 % | 26 s | 1.00 | 56 % | 1.9 | 3.7 | 0.59 | 0 | 0 | 1 |
| bad guy | 135.0 | 135 | ✓ ±2 % | non | 0.97 | 68 % | 1.4 | 2.8 | 0.51 | 6 | 1 | 5 |
| Blinding Lights | 85.5 | 171 | octave ÷2 | non | 0.94 | 37 % | 1.0 | 3.1 | 0.58 | 1 | 0 | 0 |
| Don't Start Now | 124.0 | 124 | ✓ ±2 % | 4 s | 0.96 | 54 % | 1.9 | 3.2 | 0.59 | 0 | 0 | 3 |
| Boogie Wonderland | 132.5 | 132 | ✓ ±2 % | 31 s | 1.00 | 40 % | 2.2 | 3.4 | 0.51 | 1 | 0 | 0 |
| What Is Hip | 102.9 | 103 | ✓ ±2 % | non | 0.77 | 73 % | 2.8 | 3.6 | 0.51 | 0 | 0 | 0 |
| Le Freak | 118.9 | 118 | ✓ ±2 % | 8 s | 1.00 | 55 % | 1.8 | 2.9 | 0.51 | 1 | 0 | 0 |
| September | 127.1 | 126 | ✓ ±2 % | 46 s | 0.98 | 53 % | 2.3 | 3.0 | 0.60 | 1 | 0 | 0 |
| Another One Bites The Dust | 110.0 | 110 | ✓ ±2 % | 4 s | 0.97 | 71 % | 2.0 | 2.4 | 0.49 | 1 | 0 | 0 |
| Back In Black | 94.8 | 94 | ✓ ±2 % | 248 s | 0.73 | 50 % | 1.5 | 2.1 | 0.55 | 1 | 0 | 1 |
| Schism | 109.5 | 0 | — | non | 0.80 | 51 % | 1.9 | 2.4 | 0.65 | 1 | 1 | 1 |
| Sonne | 149.9 | 150 | ✓ ±2 % | non | 0.65 | 75 % | 1.6 | 2.0 | 0.55 | 1 | 0 | 0 |
| Gasolina | 96.0 | 96 | ✓ ±2 % | 147 s | 0.99 | 56 % | 1.4 | 4.0 | 0.52 | 0 | 0 | 3 |
| SAOKO | 100.1 | 0 | — | 106 s | 0.46 | 62 % | 1.5 | 3.0 | 0.61 | 1 | 1 | 2 |
| Hips Don't Lie | 100.0 | 100 | ✓ ±2 % | 4 s | 0.96 | 40 % | 1.7 | 3.7 | 0.53 | 1 | 0 | 0 |
| Tití Me Preguntó | 103.0 | 98 | +5.1 % | non | 0.76 | 74 % | 1.2 | 3.2 | 0.55 | 4 | 3 | 2 |
| SICKO MODE | 103.3 | 0 | — | non | 0.90 | 56 % | 1.1 | 3.7 | 0.60 | 3 | 3 | 3 |
| Wesley's Theory | 113.9 | 0 | — | 219 s | 0.77 | 54 % | 1.2 | 2.3 | 0.61 | 3 | 1 | 0 |
| HUMBLE | 149.8 | 150 | ✓ ±2 % | 10 s | 1.00 | 61 % | 1.3 | 4.0 | 0.54 | 7 | 7 | 4 |
| Without Me | 112.3 | 112 | ✓ ±2 % | 8 s | 1.00 | 38 % | 1.0 | 3.7 | 0.52 | 1 | 0 | 0 |
| Alexandrie Alexandra | 126.0 | 0 | — | 4 s | 0.97 | 34 % | 2.0 | 3.4 | 0.52 | 1 | 0 | 0 |
| Les lacs du Connemara | 113.0 | 0 | — | non | 0.59 | 69 % | 0.9 | 0.5 | 0.63 | 2 | 1 | 3 |
| Les sunlights des tropiques | 126.5 | 0 | — | 5 s | 1.00 | 37 % | 1.4 | 3.4 | 0.58 | 1 | 0 | 0 |
| Les Champs-Elysées | 111.5 | 0 | — | 4 s | 0.99 | 52 % | 1.7 | 2.3 | 0.56 | 1 | 0 | 0 |
| My Immortal | 120.2 | 0 | — | 241 s | 0.44 | 71 % | 0.8 | 1.4 | 0.59 | 8 | 7 | 2 |
| The Great Gig in the Sky | 118.3 | 0 | — | non | 0.32 | 67 % | 0.7 | 1.1 | 0.52 | 4 | 3 | 1 |
| Perfect | 95.0 | 63 | ×1,5 | 134 s | 0.84 | 71 % | 0.9 | 1.9 | 0.62 | 1 | 0 | 1 |
| Someone Like You | 133.5 | 67 | octave ×2 | 277 s | 0.27 | 81 % | 0.6 | 1.6 | 0.57 | 5 | 4 | 3 |
| Bohemian Rhapsody | 141.1 | 0 | — | non | 0.52 | 69 % | 1.1 | 1.6 | 0.58 | 5 | 4 | 4 |
| Waltz No. 2 (Original Motion Picture Soundtrack) | 94.6 | 0 | — | 174 s | 0.58 | 63 % | 0.7 | 1.4 | 0.58 | 0 | 0 | 1 |
| J'ai demandé à la lune | 110.0 | 0 | — | 4 s | 1.00 | 55 % | 2.7 | 2.9 | 0.69 | 2 | 1 | 1 |
| La valse d'Amélie | 95.9 | 0 | — | non | 0.95 | 42 % | 1.6 | 2.4 | 0.59 | 1 | 0 | 0 |

## Références de tempo et leurs sources

> Copie de `tests/assets/audio/annotations.csv` (non versionné) : sans elle, les références seraient introuvables dans le dépôt (analyse de la documentation D17). « De mémoire » = à confirmer.

| Morceau | BPM de référence | Source ou remarque |
|---|---|---|
| Animals (Radio Edit) | 128 | de mémoire |
| Glue | 130 | songbpm / tunebat |
| Summer | 128 | de mémoire |
| Titanium (feat. Sia) | 126 | de mémoire |
| Children | 137 | songbpm / tunebat (version originale) |
| Sandstorm (Radio Edit) | 136 | tunebat |
| The Rhythm Of The Night | 128 | de mémoire |
| What Is Love (7 Mix) | 123 | de mémoire |
| 24K Magic | 107 | de mémoire |
| bad guy | 135 | songbpm (changement de tempo sur l'outro) |
| Blinding Lights | 171 | songbpm : 171, ou 86 en demi-temps (les deux sont justes) |
| Don't Start Now | 124 | de mémoire |
| Boogie Wonderland | 132 | songbpm |
| What Is Hip | 103 | songbpm (tempo variable, 101 à 110 selon les sources) |
| Le Freak | 118 | de mémoire |
| September | 126 | de mémoire |
| Another One Bites The Dust | 110 | de mémoire |
| Back In Black | 94 | de mémoire |
| Sonne | 150 | songbpm : 75, ou 150 en double (les deux sont justes) |
| Gasolina | 96 | songbpm |
| Hips Don't Lie | 100 | de mémoire |
| Tití Me Preguntó | 98 | de mémoire |
| HUMBLE | 150 | de mémoire, ressenti à 75 |
| Without Me | 112 | de mémoire |
| Perfect | 63 | ballade à 6/8 : 63 hors plage 70-180 |
| Someone Like You | 67 | ballade : 67 hors plage 70-180 |
