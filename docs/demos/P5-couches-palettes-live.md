# Démonstration P5 – Couches, Palettes, Live, MIDI

> Guide de découverte de la phase P5 (doc 40 §7, doc 41 §11) — ★ **Jalon 1 « Soirée manuelle »**. Durée : 1 h 30 à 2 h.
> Matériel : PC seul (tout est jouable au simulateur) ; Arduino, appareils et **APC mini** branchés pour le matériel réel.
> Branche Git : `p5/couches-palettes-live`. Version à vérifier dans la barre de titre : **v1.004.NNN**.

## Ce que livre P5

| Élément | Où |
|---|---|
| Écran **Live** : une colonne par couche, actions FLASH / STROBE / FUMÉE à maintenir, figer, palettes rapides, journal, raccourcis | `LuXia.exe` → **Live** (premier écran) |
| **Couches** : éditeur (ordre = priorités), type **Flash**, couche protégée, scène de repos, avertissement « hors famille » | Scènes → **Couches…** |
| **Sûreté** : strobe plafonné (10 s puis pause), fumée plafonnée (10 s puis 30 s de repos), **zones interdites** des lyres | Moteur ; zones : Scènes → Palettes → **Zones interdites…** |
| **Flash** (au-dessus de tout, tant que maintenu) et **Figer** | Live, APC mini, clavier |
| **Positions par lieu** : palettes « Positions – *lieu* », repli sur « Générique » signalé | Scènes → Palettes |
| **APC mini MK1 / MK2** : pads = scènes, faders = masters, retour lumineux, branchement à chaud | Brancher l'APC (USB) |
| **Fiabilité** : versions du projet, reprise après arrêt brutal, fondu au noir à la fermeture, processeur mesuré | Menu Projet → Versions ; barre d'état « CPU » |
| Plage « Pas de strobe » du LPC008S, LPC120, LCB803 (Q28) | Bibliothèque |
| Fichiers pour construire un show sans l'interface : `live.json`, `midi.json`, `sûreté.json`, zones, positions par lieu | Doc 50 ; `luxia-headless valider` |
| 14 scènes « Phase P5 » sur le parc réel | `samples/Show de référence` |

## Préparer : régénérer le show de travail

Le show de référence a changé : **régénérez la copie de travail** (doc 32 §4), jamais l'original.

```bash
rm -rf "samples/Show de travail"
```

```bash
cp -r "samples/Show de référence" "samples/Show de travail"
```

Renommez `"name"` en « Show de travail » dans `samples/Show de travail/projet.json`, lancez LuXia, **Projet → Ouvrir…** →
`samples/Show de travail`. Menu **Projet → Problèmes du projet…** : un seul avertissement attendu (voir exemple 3).

---

## Exemple 1 – Couches : Intensité × Couleurs × Mouvements (MOT-042, COU-003)

1. Écran **Live**. Colonne **Couleurs** : cliquez **Rouge – couleur seule**. **Observez** : rien ne s'allume (sauf les appareils sans gradateur) : c'est une couleur **sans** intensité.
2. Colonne **Intensité** : **Plein feu** → tout le parc s'allume en rouge.
3. Colonne **Couleurs** : **Bleu – couleur seule** → le rouge cède la place au bleu en fondu croisé (la couche est exclusive) ; l'intensité ne bouge pas.
4. Colonne **Mouvements** : **Lyres : piste centre** puis **Lyres : plafond** → les lyres bougent, couleur et intensité gardées.
5. **Intensité 50 %** : tout baisse de moitié, couleur et position gardées. Le master d'une colonne (curseur en bas) agit sur les **intensités** de sa couche : sans effet sur une couche qui n'en contient pas (couleurs seules, positions) ; essayez-le sur la colonne **Intensité**.
6. **Ce que ça illustre** : peu de scènes, beaucoup de combinaisons. Clic sur une scène qui joue = l'arrêter.

## Exemple 2 – Flash et blackout partiel (MOT-072, COU-005, LIVE-004)

1. Laissez Plein feu + une couleur. **Maintenez** le bouton **FLASH** (ou la touche **F**) : tout en blanc ; relâchez : retour instantané à la couleur.
2. Colonne **Flashs** : maintenez **Blackout partiel (sauf UV)** : tout au noir sauf les UV ; relâchez.
3. **STROBE** (touche **S**) : strobe blanc des PAR et barres tant que maintenu.

## Exemple 3 – Éditeur de couches et avertissement « hors famille » (COU-001, COU-008, COU-009)

1. Écran **Scènes** → **Couches…** (sous la liste des scènes). L'ordre de la liste = les priorités (la couche du bas l'emporte).
2. Regardez **Flashs** (type « Flash (maintien) ») et **Ambiance** (« Protégée » : « Tout arrêter » l'épargne).
3. Choisissez une **Scène de repos** pour Couleurs (par ex. Ambre – couleur seule), Enregistrer. En Live, arrêtez la couleur : l'ambre revient tout seul. Remettez « Aucune » ensuite.
4. **Projet → Problèmes du projet…** : « Lyres sur 3 positions » (P4) touche l'intensité et la couleur alors qu'elle est dans Mouvements : avertissement **non bloquant** qui aide à garder une couche par famille.

## Exemple 4 – Strobe plafonné à 10 s (MOT-080, DEMO-4)

1. En Live : **Plein feu**, une couleur, puis colonne **Effets** : **Strobe PAR (plafonné à 10 s)**.
2. **Observez** : les PAR strobent 10 s, s'arrêtent (pastille **Sûreté** orange, ligne ⚠ au journal), puis reprennent 10 s plus tard.
3. Un PAR simplement allumé (canal Strobe à 0-4) n'est **jamais** compté comme un strobe (Q28).

## Exemple 5 – Fumée plafonnée (MOT-081, DEMO-4) — sans machine pour l'instant (Q29)

1. Colonne **Ambiance** : **Fumée longue (plafonnée à 10 s)**. La machine n'étant pas branchée, regardez le **canal 180** (écran Console, moniteur de sortie) ou le journal : 255 pendant 10 s, puis 0 et 30 s de repos.
2. Bouton **FUMÉE** maintenu (touche **Z**) pendant ce repos : rien ne sort. **Rafale** : 3 s.

## Exemple 6 – Zone interdite des lyres (MOT-082, INST-053, DEMO-4)

1. En Live : Plein feu, puis **Piège : lyre 1 vers le public** (la scène vise Tilt 95 %).
2. **Observez** : la lyre s'arrête à **Tilt 85 %** (bord de la zone d'exemple « Public », Pan 30-70 % / Tilt 85-100 %) ; pastille **Sûreté** orange.
3. **Adapter à votre salle** : écran **Scènes** → sélectionnez **Lyre 1** au programmeur → panneau Palettes, **Zones interdites…** : visez un coin de la zone à protéger (faders Pan / Tilt), **Coin 1**, visez le coin opposé, **Coin 2**, Enregistrer. Plusieurs zones par lyre possibles.

## Exemple 7 – Figer (MOT-073)

1. Lancez un chenillard (Couleurs → **Chenillard 4 couleurs**), puis **FIGER** (touche **G**) : la lumière se fige ; pastille « ❄ FIGÉ ».
2. Lancez une autre couleur : rien ne change à la sortie. **Blackout** (B) : le noir fonctionne quand même.
3. Re-FIGER : on retrouve l'état **courant** des lectures (le chenillard a continué en arrière-plan).

## Exemple 8 – Palettes rapides (LIVE-005)

1. En bas du Live : choisissez **Toutes les lyres**, puis la palette de position **Plafond** : les lyres sont forcées au plafond, par-dessus leurs scènes.
2. **Libérer (Échap)** : elles reprennent ce que jouent les couches.

## Exemple 9 – Clavier (LIVE-040)

Sans souris : **←/→** pour encadrer une colonne, **1-9** pour lancer ses scènes, **Page ↑/↓** pour le Grand Master, **B** blackout,
**F / S / Z** à maintenir, **G** figer, **Échap** libérer. Rappel des touches sous les actions.

## Exemple 10 – APC mini (MIDI-001 à 006)

1. Branchez l'APC mini **MK2** (USB) pendant que LuXia tourne : pastille « 🎛 APC mini MK2 » en haut du Live (2 s max).
2. **Grille** : colonnes = couches, lignes = scènes (ligne 1 en haut). Les pads prennent la **couleur des scènes** (faible = disponible, fort = active, pulsation = fondu d'entrée). Lancez / arrêtez depuis les pads, puis à la souris : les pads suivent.
3. **Boutons du bas** : arrêter la couche (allumé quand elle joue). **Shift + bas 1 / 2** : pages de scènes ; **3 / 4** : pages de couches.
4. **Boutons de droite** (haut → bas) : Blackout (**tant que maintenu**, MIDI-011 : relâché, le noir s'annule même s'il avait été mis à l'écran), Flash, Strobe, Fumée, (Tap, P7), Figer, (Auto, P10), Tout arrêter.
5. **Faders 1-8** = masters des colonnes, **9** = Grand Master : un fader ne prend la main qu'en **croisant** la valeur affichée (pas de saut).
6. Débranchez puis rebranchez : l'APC est reconnu à nouveau et ses LED reviennent.
7. **Si un pad ou une LED ne répond pas comme prévu** : les numéros MIDI viennent des protocoles publiés par AKAI (les notices du dépôt ne les ont pas). LuXia fermé, lancez :

```bash
dotnet run --project D:/Develop/Claude/CSharp/DMX/tools/Luxia.Tools.Headless -- midi --duree 60 --leds
```

   puis appuyez sur les pads, boutons et faders : chaque message s'affiche (numéro, valeur). Donnez-moi ce que vous voyez.

## Exemple 11 – Positions par lieu (PAL-004, PAL-008, INST-054)

1. Écran **Installation** : dupliquez le lieu **Générique** (« Générique (copie) »), renommez-le « Garage », activez-le.
2. Écran **Scènes** : le panneau affiche « Positions – Garage ». Sélectionnez la lyre 1, visez, clic droit sur **Piste centre** → *Mettre à jour depuis le programmeur* : seule la lyre 1, seulement au Garage.
3. Réactivez **Générique** : la lyre revient à l'ancienne position. Un lieu où une position n'a jamais été calibrée utilise celle du Générique, et **Problèmes du projet** le signale.

## Exemple 12 – Fiabilité (GEN-054, 055, 061, 095)

1. **Fondu au noir** : fermez LuXia pendant qu'une scène joue : la lumière descend en 1 s.
2. **Reprise** : relancez, lancez deux scènes, puis **tuez** LuXia (Gestionnaire des tâches, « Fin de tâche »). Au redémarrage : « LuXia ne s'est pas fermé normalement… Reprendre ? » → Oui : mêmes scènes, mêmes masters.
3. **Versions** : Projet → **Versions du projet…** : une version toutes les 2 min si le projet a changé, et à chaque passage en Live ; tapez un numéro pour y revenir.
4. Barre d'état : **CPU x %** (à relever pendant une demi-heure de Live, objectif < 15 %) ; Aide → À propos : temps de démarrage.

## Exemple 13 – Sans interface (GEN-131, MOT-103)

```bash
dotnet run --project D:/Develop/Claude/CSharp/DMX/tools/Luxia.Tools.Headless -- jouer "D:/Develop/Claude/CSharp/DMX/samples/Show de travail" --scene "Strobe PAR (plafonné à 10 s)" --duree 22 --pas 1
```

Le résumé montre la coupure à 10 s (« ⚠ sûreté ») et la reprise. Les scénarios acceptent `flash`, `figer`, `fumee`, `canal`
(doc 50 §13).

---

## Ce qui n'est pas encore là

| Manque | Quand |
|---|---|
| Tempo, tap, BPM, énergie ; morceau et style ; shows ; mode auto et verrous | P7, P9, P8, P10 |
| Disposition du Live à l'écran, mini-simulateur, assistant d'installation, apprentissage MIDI, raccourcis personnalisables, archive de projet | Chantier d'ergonomie (Q32) — réglages par fichiers en attendant |
| Thèmes de couleurs (PAL-010) | P6 |
| Zones interdites dessinées au simulateur (SIM-008) | Plus tard |
| Numéros MIDI confirmés sur l'appareil | À l'essai (exemple 10) |

## Grille de retour

| Exemple | Correct | À revoir | Idée / remarque |
|---|---|---|---|
| 1 – Couches combinées | ☐ | ☐ |  |
| 2 – Flash, blackout partiel | ☐ | ☐ |  |
| 3 – Éditeur de couches | ☐ | ☐ |  |
| 4 – Strobe plafonné | ☐ | ☐ |  |
| 5 – Fumée plafonnée | ☐ | ☐ |  |
| 6 – Zone interdite | ☐ | ☐ |  |
| 7 – Figer | ☐ | ☐ |  |
| 8 – Palettes rapides | ☐ | ☐ |  |
| 9 – Clavier | ☐ | ☐ |  |
| 10 – APC mini | ☐ | ☐ |  |
| 11 – Positions par lieu | ☐ | ☐ |  |
| 12 – Fiabilité | ☐ | ☐ |  |
| 13 – Sans interface | ☐ | ☐ |  |
