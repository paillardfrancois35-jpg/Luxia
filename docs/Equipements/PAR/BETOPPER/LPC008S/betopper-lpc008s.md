# PAR — BETOPPER LPC008S

Source : notice PDF `BETOPPER LPC008S/LPC007-LPC008_LPC08-H_..._User_Manual.pdf`
(couvre LPC007 / LPC008 / LPC08-H) + photos.

- **Type** : PAR LED **RGB** (3-en-1), ~54 LED. Modèle A = 54×3 W (180 W), modèle B = 54×1,5 W (90 W)
- **Alim** : AC 100–240 V
- **Modes** : Auto / Son / **DMX** / Maître-esclave
- **Connecteurs** : DMX IN (XLR 3 pts) · DMX OUT (XLR 3 pts) — vérifier le genre
- **Affichage** : 4 digits + MENU / UP / DOWN / ENTER
- **Effets** : changement de couleur, strobe

## Choisir le mode DMX = choisir le menu d'adresse

| Menu | Plage | Effet |
|---|---|---|
| `d001` | 001–512 | règle l'adresse **et** met le PAR en **mode 3 canaux** |
| `A001` | 001–512 | règle l'adresse **et** met le PAR en **mode 7 canaux** |

## Mode 3 canaux  (utilisé par le POC — menu `d001`)

| Canal | Fonction |
|------:|----------|
| 1 | Rouge (0→255) |
| 2 | Vert (0→255) |
| 3 | Bleu (0→255) |

## Mode 7 canaux  (menu `A001`)

| Canal | Fonction | Détail |
|------:|----------|--------|
| 1 | Gradation générale | maître R+G+B (0 = éteint) |
| 2 | Rouge | 0→255 |
| 3 | Vert | 0→255 |
| 4 | Bleu | 0→255 |
| 5 | Strobe général | R+G+B, lent → rapide |
| 6 | Sélecteur de fonction | 0–50 gradation DMX · 51–100 sortie couleur (8) · 101–150 fondu · 151–200 transition · 201–250 pulsation · 251–255 audio (avec CH7) |
| 7 | Vitesse | vitesse des fonctions du CH6 (0–250 lent→rapide ; = choix de couleur si CH6 en « sortie couleur ») |

## Menu d'exploitation (digital display)

`d0xx` adresse 3c · `A0xx` adresse 7c · `r/G/b 000-255` couleur fixe · `FH` strobe ·
`CL 1-8` sortie couleur · `CC` fondu auto · `DE` transition auto · `CP` pulsation auto ·
`SU 1-9` mode son.

> Menu « fonctions spéciales » (appui long MENU) : `TH / Tl / P / Fu / Ft` = réglages
> ventilateur/thermique, ne pas toucher. `Pj` = version firmware. `Nod` = indicateur de mode.

## Pour le patch (phases suivantes)

- Retenir le **mode 7 c** en exploitation réelle (canal de gradation maître utile pour les fondus).
- 7 canaux consécutifs par PAR → 2 PAR aux adresses 1 et 8.
- ⚠️ En 7 c, canal 1 (Dim) à 0 ⇒ PAR éteint quelles que soient les couleurs.
