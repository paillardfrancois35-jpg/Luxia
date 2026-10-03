# P9 – Preuve de concept PoC-3 : la sonde de la lecture en cours

> Guide du premier essai de P9 (lot 1), à dérouler avec Claude dans la discussion de développement (≈ 10 minutes, un lecteur à
> la fois). But : voir **ce que Windows sait** de ce que vous écoutez (titre, artiste, position, état) avec Deezer, YouTube Music
> et VLC, et **ce que LuXia en retient** (session suivie, changement de morceau, position estimée). Ce que vous voyez décide des
> règles de normalisation (lot 2) : rien n'est à corriger ici, on observe.
> Références : [doc 21 §2](../21-lecture-en-cours-et-style.md), questions [Q43](../01-questions-ouvertes.md) et Q52, exigences
> [MUS-001](../exigences/MUS-001.md), [MUS-002](../exigences/MUS-002.md), [MUS-003](../exigences/MUS-003.md),
> [MUS-006](../exigences/MUS-006.md). Résultats : [essais/P9-poc3.md](../essais/P9-poc3.md).

## 0. Lancer la sonde

La sonde ne pilote **aucun matériel** (pas de DMX) : elle ne fait que lire les sessions média de Windows. Dans PowerShell :

```powershell
& "D:\Develop\Claude\CSharp\DMX\tools\Luxia.Tools.Headless\bin\Debug\net10.0-windows10.0.19041.0\luxia-headless.exe" media --duree 300 --rapport "$env:TEMP\sonde-p9.txt"
```

Elle tourne 5 minutes (`--duree` en secondes) et écrit un rapport texte à la fin ; **Ctrl+C** l'arrête plus tôt (dans ce cas, copiez
simplement ce qui s'affiche). Chaque ligne commence par l'heure. Sans lecture, elle écrit « (aucune session média) » : c'est normal.

Comment lire une ligne de session :

| Partie | Sens |
|---|---|
| `★` en tête | la session que LuXia **suit** (MUS-001) |
| `Deezer` / `chrome` / `vlc` | l'application source, telle que Windows la nomme |
| `Playing` / `Paused` / `Stopped` | état de lecture |
| `« titre » — artiste · album « … »` | ce que le lecteur déclare, **brut** |
| `position 1:23 (relevée il y a 2,1 s)` | position donnée par le lecteur et âge de ce relevé ; `—` = le lecteur n'en donne pas |
| `durée`, `vitesse` | durée du morceau (0:00 si inconnue), vitesse de lecture |
| `► MORCEAU : …` | LuXia publie un changement de morceau, **1 s** après qu'il est stable (MUS-002) |
| `suivi : … position estimée` | la position que LuXia calcule entre deux relevés (MUS-003) |

## 1. Deezer (application ou navigateur)

1. Lancez la sonde (§0), puis jouez un titre dans Deezer.
2. **Observez** : la session apparaît-elle ? Titre et artiste sont-ils exacts ? La position avance-t-elle (la ligne revient toutes les
   5 s) ? Notez l'« âge du relevé ».
3. Mettez en **pause**, puis reprenez : l'état passe-t-il à `Paused` puis `Playing` ? Une ligne « ► LECTURE » apparaît-elle ?
4. Passez au **titre suivant** : un seul « ► MORCEAU » ? Au bout de combien de temps environ ?
5. **Avancez** dans le morceau (clic sur la barre) : la position suit-elle ?

## 2. YouTube Music (navigateur)

1. Jouez un titre dans YouTube Music (Chrome ou Edge, comme d'habitude).
2. Mêmes observations qu'au §1. En plus : **qu'est-ce qui est écrit dans « artiste »** ? (nom de l'artiste, « Artiste - Topic »,
   nom de la chaîne ?) Le titre contient-il des mentions comme « (Official Video) » ?
3. Jouez ensuite une **vidéo YouTube ordinaire** (clip d'un titre que vous passez en soirée) et notez titre et « artiste ».

## 3. VLC

1. Jouez un **fichier** dans VLC. La session apparaît-elle ? Titre et artiste sont-ils ceux des étiquettes du fichier, ou le nom du
   fichier ? La position est-elle donnée ?
2. Si **rien n'apparaît** : notez-le (c'est une information importante : Q43 supposait que VLC s'annonce à Windows).
3. Si l'option existe dans VLC (Outils → Préférences → Interface), notez si une case « intégration Windows / contrôles
   multimédias » est cochée.

## 4. Plusieurs lecteurs à la fois (MUS-001)

1. Jouez un titre dans un lecteur, **puis** lancez la lecture dans un deuxième. Le `★` passe-t-il au deuxième ?
2. Mettez le deuxième en pause : le `★` reste-t-il sur lui (dernier actif) ?
3. Fermez le deuxième lecteur : le `★` revient-il au premier ?

## 5. Retour

Copiez dans le chat le rapport (`$env:TEMP\sonde-p9.txt`) ou les lignes marquantes, et dites-moi pour chaque lecteur :
**correct / à revoir / idée**. Je note tout dans [essais/P9-poc3.md](../essais/P9-poc3.md) et j'adapte les lots 2 à 7 à ce qu'on a vu.
