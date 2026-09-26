# Démonstration P0 – Fondations

> Guide de découverte de la phase P0 (doc 40 §7, doc 41 §11). Durée : 30 à 45 minutes.
> Matériel : PC, Arduino Leonardo + shield DMX, **1 PAR Betopper LPC008S**, câble DMX, terminaison si disponible.
> Branche Git : `p0/fondations`.

## Ce que livre P0

| Élément | Où |
|---|---|
| Application (écran « Sorties ») | `src/Luxia.App` → `LuXia.exe` |
| Outil sans interface | `tools/Dmx.Tools.Headless` → `dmx-headless` |
| Firmware 1.0 (protocole Enttec) | `firmware/arduino-dmx` |
| Show de référence : fiche projet + enregistrement du chenillard 1-180 | `samples/Show de référence/` |
| Journal technique | `Documents\LuXia\Journaux\technique-AAAAMMJJ.log` |
| Préférences du poste (sorties, test) | `%AppData%\LuXia\preferences.json` |

> Sur ce PC, « Documents » est redirigé vers OneDrive : `C:\Users\<vous>\OneDrive - …\Documents\LuXia\`.

## Préparer

```bash
dotnet build Dmx.sln
```

L'application se lance avec `src/Luxia.App/bin/Debug/net10.0/LuXia.exe` (ou `dotnet run --project src/Luxia.App`).
L'outil : `dotnet tools/Dmx.Tools.Headless/bin/Debug/net10.0/dmx-headless.dll` (appelé `dmx-headless` ci-dessous).

**Téléverser le firmware** (une seule fois, remplace le POC) — *je ne l'ai pas fait : à faire par vous ou avec votre accord* :

```bash
arduino-cli upload --fqbn arduino:avr:leonardo -p COMx firmware/arduino-dmx
```

(`dmx-headless ports` indique le port de l'Arduino ; arduino-cli est dans `%LOCALAPPDATA%\Programs\arduino-cli`.)

**Le PAR pour la démonstration** : adresse **1**, de préférence en **mode 3 canaux** (`d001`) — voir l'exemple 3 pour la raison.

---

## Exemple 1 – Démarrer sans matériel

**Lancer** : Arduino **débranché**, démarrer `LuXia.exe`.

**Observer** :
- l'écran « Sorties » s'ouvre ; ligne « Arduino (univers 1) » : **Déconnecté – aucune carte Arduino détectée** ;
- en haut : « Moteur : 40,0 Hz – gigue p99 … ms » (la gigue doit rester sous 5 ms) ;
- aucune erreur, l'application reste réactive.

**Illustre** : GEN-001 / SORT-005 (le moteur tourne sans matériel), GEN-030 (40 Hz), GEN-091 (une sortie absente ne bloque rien).

## Exemple 2 – Brancher, débrancher, rebrancher (détection et reconnexion)

**Lancer** : application ouverte, brancher l'Arduino (firmware 1.0).

**Observer** :
- en moins de 3 s : **Connecté – COMx – DMX-LEONARDO 1.0**, ~40 trames/s ; la LED de la carte **clignote** ;
- débrancher l'USB : passage en **Erreur** puis **Déconnecté** ; compteur d'erreurs + 1 ; la LED s'éteint (plus d'alimentation) ;
- rebrancher (sur **un autre port USB** si possible) : reconnexion **sans rien toucher**, en moins de 3 s ;
- le port retenu est mémorisé (`preferences.json`, `lastPort`) et essayé en premier au démarrage suivant.

**Illustre** : SORT-010, 011, 013, 015, 022 ; GEN-091. Correspond aux tests T-SORT-04 (3 ports différents) et T-SORT-05 (10 fois).

**Avec l'ancien firmware POC** : choisir le port dans la liste, cocher « Ancien firmware POC », Appliquer (SORT-014).

## Exemple 3 – Chenillard de test canal par canal (et son piège)

**Lancer** : « Test de sortie » : canaux `1-16`, exclus `180`, valeur `50 %`, durée `1000` ms → **Démarrer le test**.

**Observer** — PAR en **mode 3 canaux** (`d001`, adresse 1) :
- canal 1 → le PAR s'allume **rouge** à mi-intensité 1 s, puis canal 2 **vert**, canal 3 **bleu**, puis plus rien jusqu'au canal 16, et on recommence ;
- l'écran indique « En cours : canal N ».

**Piège (règle montrée)** — PAR en **mode 7 canaux** (`A001`) : **presque rien ne s'allume**. Le canal 1 (gradation maître)
seul n'allume rien car les couleurs sont à 0, et les canaux 2-4 (couleurs) seuls n'allument rien car le maître est à 0.
Le canal 6 à 50 % (valeur 128) place le PAR dans son programme interne « fondu » : il peut s'animer seul pendant 1 s.
C'est normal : un chenillard **canal par canal** vérifie la **ligne et les adresses**, pas le rendu d'un appareil à gradateur maître.
→ Q23 : des canaux « maintenus » pendant le test seront ajoutés en P3 (SORT-008). En attendant, testez les couleurs du mode 7 canaux avec la Console.

**Sûreté** : le canal 180 (machine à fumée du show de référence) est **exclu par défaut** et la valeur est modérée (50 %),
pour ne déclencher ni la fumée, ni les canaux Reset de l'effet multi-têtes (WZYBUTA).

**Illustre** : SORT-007, CMD-024 `TesterSortie` (D19), GEN-002 (le test passe par une commande du moteur).

## Exemple 4 – Arrêt brutal : chien de garde (noir en ≤ 2 s)

**Lancer** : chenillard en cours sur le PAR (mode 3 canaux, canal 1 rouge allumé : mettez « Durée par canal » à 20000 ms pour avoir le temps).

**Observer** :
- **tuer** l'application (Gestionnaire des tâches → Fin de tâche sur « LuXia ») : le PAR s'**éteint en 2 s au plus** ; la LED de la carte devient **fixe** ;
- relancer l'application : reconnexion, blackout (rien ne s'allume tant que vous n'avez rien lancé — GEN-060) ;
- **fermer normalement** l'application pendant le chenillard : le PAR s'éteint **immédiatement** (trame de blackout envoyée avant fermeture), sans attendre les 2 s.

**Illustre** : GEN-080, GEN-081, SORT-042 (firmware), GEN-060. Correspond à T-SORT-06.

## Exemple 5 – Enregistrer et relire des trames

**Lancer** : « Enregistrer les trames » pendant quelques secondes de chenillard, puis arrêter. Le fichier est dans `Documents\LuXia\Enregistrements\`.

**Relire** :

```bash
dmx-headless relire "samples/Show de référence/Enregistrements/P0-chenillard-1-180.dmxrec" --canaux 170-180
```

**Observer** : 1817 trames sur 45,4 s (40 trames/s), canaux 170 à 179 allumés à 250 ms d'intervalle, valeur 128,
**canal 180 jamais allumé**. Faites la même chose sur votre propre enregistrement.

**Illustre** : SORT-060, SORT-061 (activation à chaud, en même temps que l'Arduino), T-SORT-03.

## Exemple 6 – Mesures (PoC-1)

| Mesure | Commande | Attendu |
|---|---|---|
| Cadence et gigue, 15 min (D23) | `dmx-headless gigue` (900 s par défaut ; la veille du PC est bloquée pendant la mesure, GEN-096) | 40 ± 0,5 Hz ; gigue p99 < 5 ms (GEN-030, GEN-031) — *mesuré sur 20 s : 40,04 Hz, p99 0,8 ms* |
| Endurance matériel, 1 h, 512 canaux | `dmx-headless endurance --duree 3600` (Arduino branché, **appareils débranchés ou PAR seul**) | aucune erreur, ~40 trames/s constantes (T-SORT-07) |
| Ligne DMX plus rapide | écran Sorties, « Canaux émis » = 30 | le firmware émet des trames courtes (SORT-049) ; le PAR réagit de la même façon |
| Compatibilité Enttec | QLC+ → sortie « DMX USB » | **à tester** (T-SORT-09) : QLC+ cherche les Enttec via le pilote FTDI ; le Leonardo (USB natif) risque de ne pas être listé |

## Pour aller plus loin (modifier soi-même)

- Changer la plage et les exclusions du test (ex. `1-7` exclus `1`) et observer.
- Changer la fréquence du moteur (25 à 44 Hz) dans « Enregistreur et cadence ».
- Ouvrir `%AppData%\LuXia\preferences.json` dans un éditeur : il est lisible et modifiable à la main (GEN-050).
  Mettez une erreur de syntaxe volontaire : au démarrage suivant, le fichier est mis de côté (`.illisible-…`), un message orange l'indique, les valeurs par défaut sont utilisées (GEN-056).

## Grille de retour

| Exemple | Correct | À revoir | Idée / remarque |
|---|---|---|---|
| 1 – Démarrer sans matériel | ☐ | ☐ | |
| 2 – Détection / reconnexion | ☐ | ☐ | |
| 3 – Chenillard | ☐ | ☐ | |
| 4 – Chien de garde | ☐ | ☐ | |
| 5 – Enregistrer / relire | ☐ | ☐ | |
| 6 – Mesures PoC-1 | ☐ | ☐ | |
