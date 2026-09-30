# Rapport chiffré – analyse du jeu de test audio (P7)

> Produit par `luxia-headless audio tests/assets/audio --rapport rapport.md` (doc 19 §7, T-AUD-02) sur la version 1.009 du
> 2026-09-30. Le jeu de test (41 morceaux, `tests/assets/audio`, non versionné) a été déposé par l'utilisateur : pour chaque
> famille, un titre « effet waouh » et un titre « défi technique ». Les BPM de référence sont dans `annotations.csv` (songbpm,
> tunebat, ou de mémoire : à confirmer) ; `0` ou `?` = pas de référence, le tempo est seulement relevé.

## Lecture

- **Tempo** : sur les 25 morceaux qui ont une référence, **21 (84 %) sont justes à ± 2 %**. Les écarts : *Blinding Lights*
  trouvé à 85,5 (la référence donne 171, ou 86 en demi-temps : les deux sont justes), *Perfect* à 95 pour 63 (6/8 : rapport de 1,5),
  *Someone Like You* à 133,6 pour 67 (octave, confiance 0,29 : l'horloge garde son tempo), *Tití Me Preguntó* à 103 pour 98
  (référence de mémoire, à vérifier). Critère AUD-020 : « ≥ 90 % des morceaux dansants à ± 2 % (hors erreurs d'octave) » :
  **atteint** hors ballades.
- **Convergence** : la colonne mesure le dernier instant où le tempo sort de ± 2 % de sa valeur finale ; « non » = il bouge
  encore en fin de morceau (changement de section, tempo qui varie). Les morceaux réguliers convergent en 5 à 10 s ; les
  morceaux longs ou qui changent de section (*Animals*, *Sandstorm*, *September*…) oscillent d'un pas ou deux.
- **Confiance** : élevée (≥ 0,95) sur les titres réguliers, basse sur *SAOKO* (0,49), *My Immortal* (0,47), *The Great Gig in
  the Sky* (0,34), *Someone Like You* (0,29), *Bohemian Rhapsody* (0,53), *Waltz No. 2* (0,59), *Les lacs du Connemara* (0,60) :
  ce sont les morceaux annoncés comme des défis (rubato, changements de tempo, mesures irrégulières) ; l'horloge, sous 0,3,
  garde son dernier tempo.
- **Premier temps de la mesure** : connu de 27 % à 86 % du temps selon le morceau (environ la moitié en moyenne) ; non
  annoté, donc non chiffré contre une vérité (AUD-024 reste partiel).
- **Impulsions** : les basses tombent à 2 à 3 par seconde sur tous les morceaux (les notes graves comptent comme des attaques,
  le temps mort est de 0,25 s) ; les aigus distinguent les morceaux percutants (3 à 5 par seconde) des ballades (moins de 2).
- **Événements** : *Animals* donne deux Break / Drop aux creux nets du morceau (≈ 19 s → 21 s, 94 s → 96 s) ; les morceaux
  de rap et de pop à arrêts brefs (*HUMBLE*, *bad guy*) en donnent beaucoup. Pas d'annotation des drops : critère AUD-062
  (≥ 80 % à ± 1 temps) non mesuré.
- **Énergie** : moyenne de 0,6 à 0,8 sur tous les morceaux, par construction (elle est normalisée sur l'historique récent) ;
  ce qui compte est sa variation au sein d'un morceau (écran Audio).
- **Durée d'analyse** : environ 15 s par morceau (décodage MP3 compris) ; l'analyse seule consomme moins de 5 % d'un cœur
  en temps réel (AUD-007).

## Tableau

| Morceau | BPM mesuré | BPM attendu | Écart | Convergence | Confiance | 1er temps connu | Kicks/s | Aigus/s | Énergie | Breaks | Drops | Montées |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Animals (Radio Edit) | 128.0 | 128 | ✓ ±2 % | 96 s | 0.95 | 47 % | 2.4 | 3.6 | 0.80 | 3 | 2 | 2 |
| Glue | 130.0 | 130 | ✓ ±2 % | 5 s | 1.00 | 84 % | 2.1 | 3.1 | 0.69 | 4 | 3 | 3 |
| Love Tonight Vs Aria Vs Give Me Everything (David Guetta Mashup) (Lee Barzola Remake) | 126.0 | 0 | — | 60 s | 0.95 | 60 % | 2.2 | 3.3 | 0.73 | 2 | 1 | 1 |
| Summer | 128.0 | 128 | ✓ ±2 % | 23 s | 0.98 | 59 % | 2.2 | 3.0 | 0.77 | 2 | 1 | 1 |
| Titanium (feat. Sia) | 126.0 | 0 | — | non | 0.74 | 37 % | 2.3 | 2.7 | 0.65 | 2 | 1 | 3 |
| Children | 137.0 | 137 | ✓ ±2 % | non | 0.99 | 39 % | 2.4 | 2.9 | 0.63 | 1 | 0 | 1 |
| Sandstorm (Radio Edit) | 136.0 | 136 | ✓ ±2 % | 97 s | 0.99 | 47 % | 2.5 | 3.8 | 0.76 | 2 | 1 | 1 |
| The Rhythm Of The Night | 127.8 | 128 | ✓ ±2 % | 5 s | 0.98 | 62 % | 2.5 | 5.2 | 0.76 | 1 | 0 | 4 |
| What Is Love (7 Mix) | 123.8 | 123 | ✓ ±2 % | 5 s | 0.97 | 81 % | 2.5 | 4.6 | 0.78 | 2 | 1 | 1 |
| 24K Magic | 107.0 | 107 | ✓ ±2 % | 25 s | 1.00 | 45 % | 2.5 | 3.2 | 0.75 | 1 | 0 | 2 |
| bad guy | 135.0 | 135 | ✓ ±2 % | non | 0.98 | 67 % | 2.1 | 2.3 | 0.74 | 4 | 4 | 6 |
| Blinding Lights | 85.5 | 171 | octave ÷2 | non | 0.93 | 34 % | 2.4 | 2.4 | 0.70 | 1 | 0 | 1 |
| Don't Start Now | 124.0 | 124 | ✓ ±2 % | 5 s | 0.97 | 50 % | 2.4 | 3.0 | 0.72 | 1 | 1 | 1 |
| Boogie Wonderland | 132.5 | 132 | ✓ ±2 % | 28 s | 1.00 | 47 % | 2.5 | 3.2 | 0.64 | 1 | 0 | 0 |
| What Is Hip | 103.4 | 103 | ✓ ±2 % | non | 0.82 | 75 % | 2.9 | 3.7 | 0.70 | 0 | 0 | 0 |
| Le Freak | 118.9 | 118 | ✓ ±2 % | 5 s | 1.00 | 50 % | 2.5 | 2.3 | 0.61 | 1 | 0 | 2 |
| September | 127.2 | 126 | ✓ ±2 % | 204 s | 0.98 | 46 % | 2.5 | 2.7 | 0.70 | 1 | 0 | 0 |
| Another One Bites The Dust | 110.0 | 110 | ✓ ±2 % | 5 s | 0.96 | 72 % | 2.3 | 2.3 | 0.65 | 1 | 0 | 4 |
| Back In Black | 94.8 | 94 | ✓ ±2 % | non | 0.73 | 64 % | 2.4 | 2.1 | 0.67 | 1 | 0 | 2 |
| Schism | 109.2 | 0 | — | non | 0.81 | 45 % | 2.5 | 2.4 | 0.68 | 3 | 3 | 3 |
| Sonne | 149.8 | 150 | ✓ ±2 % | 272 s | 0.69 | 78 % | 2.5 | 2.7 | 0.67 | 1 | 0 | 1 |
| Gasolina | 96.0 | 96 | ✓ ±2 % | 144 s | 1.00 | 54 % | 2.4 | 3.8 | 0.69 | 0 | 0 | 2 |
| SAOKO | 100.1 | 0 | — | non | 0.49 | 74 % | 2.4 | 3.3 | 0.76 | 0 | 0 | 0 |
| Hips Don't Lie | 100.0 | 100 | ✓ ±2 % | 12 s | 0.95 | 38 % | 2.5 | 3.8 | 0.66 | 1 | 0 | 0 |
| Tití Me Preguntó | 103.0 | 98 | +5.1 % | non | 0.75 | 75 % | 2.2 | 2.5 | 0.72 | 3 | 2 | 4 |
| SICKO MODE | 103.3 | 0 | — | non | 0.93 | 66 % | 2.1 | 3.6 | 0.72 | 1 | 1 | 4 |
| Wesley's Theory | 114.0 | 0 | — | 217 s | 0.77 | 54 % | 2.4 | 2.5 | 0.75 | 3 | 2 | 2 |
| HUMBLE | 149.8 | 150 | ✓ ±2 % | 9 s | 1.00 | 61 % | 2.3 | 3.3 | 0.79 | 7 | 7 | 5 |
| Without Me | 112.3 | 112 | ✓ ±2 % | 6 s | 1.00 | 38 % | 2.5 | 3.6 | 0.74 | 1 | 0 | 1 |
| Alexandrie Alexandra | 126.0 | 0 | — | 5 s | 0.97 | 36 % | 2.5 | 3.4 | 0.67 | 1 | 0 | 0 |
| Les lacs du Connemara | 113.0 | 0 | — | non | 0.60 | 63 % | 2.3 | 1.1 | 0.66 | 2 | 1 | 2 |
| Les sunlights des tropiques | 126.5 | 0 | — | 6 s | 1.00 | 27 % | 2.3 | 3.1 | 0.61 | 1 | 0 | 0 |
| Les Champs-Elysées | 111.5 | 0 | — | 5 s | 0.99 | 46 % | 2.4 | 2.7 | 0.74 | 1 | 0 | 1 |
| My Immortal | 120.3 | 0 | — | 271 s | 0.47 | 67 % | 2.2 | 0.9 | 0.62 | 4 | 3 | 2 |
| The Great Gig in the Sky | 118.2 | 0 | — | non | 0.34 | 73 % | 2.1 | 0.6 | 0.61 | 2 | 1 | 2 |
| Perfect | 95.0 | 63 | ×1,5 | non | 0.85 | 73 % | 2.1 | 1.8 | 0.69 | 2 | 1 | 0 |
| Someone Like You | 133.6 | 67 | octave ×2 | non | 0.29 | 78 % | 2.2 | 0.9 | 0.62 | 4 | 3 | 4 |
| Bohemian Rhapsody | 141.0 | 0 | — | non | 0.53 | 67 % | 2.2 | 1.3 | 0.65 | 6 | 5 | 5 |
| Waltz No. 2 (Original Motion Picture Soundtrack) | 94.8 | 0 | — | 189 s | 0.59 | 56 % | 2.2 | 0.0 | 0.53 | 2 | 1 | 0 |
| J'ai demandé à la lune | 110.0 | 0 | — | 5 s | 1.00 | 54 % | 2.9 | 2.5 | 0.71 | 0 | 0 | 0 |
| La valse d'Amélie | 96.0 | 0 | — | non | 0.97 | 46 % | 2.7 | 1.5 | 0.65 | 1 | 0 | 0 |

