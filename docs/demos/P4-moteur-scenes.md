# Démonstration P4 – Moteur + Scènes

> Guide de découverte de la phase P4 (doc 40 §7, doc 41 §11). Durée : 1 h à 1 h 30.
> Matériel : PC seul (tout est jouable au simulateur) ; Arduino et appareils branchés pour comparer au matériel réel.
> Branche Git : `p4/moteur-scenes`.

## Ce que livre P4

| Élément | Où |
|---|---|
| **Moteur complet** : fondus, boucles, fins de scène, couches (HTP / LTP, 4 modes d'intensité), masters, Grand Master, blackout, « suit l'intensité », conversions de couleur | Dans toute l'application |
| Bouton **BLACKOUT** toujours visible (touche **B**) et **Grand Master** | En haut de la fenêtre |
| Écran **Scènes** : liste, lecture, éditeur, étapes, **programmeur**, **palettes** | `LuXia.exe` → Scènes |
| **Aveugle** : éditer sans toucher à la sortie, aperçu au simulateur | Scènes → en-tête du Programmeur, case « Aveugle » ; Simulateur (« APERÇU ») |
| Console en mode Appareils : surcharges d'**attributs** (soumises au GM et au blackout) ; écart conservé en relatif | Console |
| Import de scènes (IA de conception), relecture des fichiers, problèmes du projet | Menu **Projet** |
| Outil sans interface : `valider`, `jouer`, `scenario` | `luxia-headless` |
| 10 scènes et des palettes sur le parc réel | `samples/Show de référence/scènes.json`, `palettes.json` (catégorie « Phase P4 ») |

## Préparer : régénérer le show de travail

Le show de référence a changé (palettes, sélections, scènes) : **régénérez la copie de travail** (doc 32 §4), jamais l'original.

```bash
rm -rf "samples/Show de travail"
```

```bash
cp -r "samples/Show de référence" "samples/Show de travail"
```

Puis renommez `"name"` en « Show de travail » dans `samples/Show de travail/projet.json`, lancez LuXia et **Projet → Ouvrir…** →
`samples/Show de travail` (vérifiez le dossier dans **Aide → À propos** en cas de doute). Menu **Projet → Problèmes du projet…** :
« Aucun problème ».

---

## Exemple 1 – Blanc chaud et lien au dimmer (MOT-040, GEN-041, GEN-082)

1. Écran **Scènes**, scène **Blanc chaud sur les 4 PAR** → ▶.
2. **Observez** : les 4 PAR en blanc chaud (simulateur et matériel).
3. Baissez le **Grand Master** à 50 % : les PAR baissent **sans changer de teinte**.
4. **BLACKOUT** (ou touche **B**) : noir immédiat ; re-cliquez : retour instantané, même couleur. Les lyres ne bougent pas (GEN-041).
5. **Ce que ça illustre** : les masters et le blackout n'agissent que sur les intensités.

## Exemple 2 – Une palette, des appareils différents (GEN-022, PAL-002)

1. ■ Tout arrêter, puis ▶ **Bleu sur tout le parc**.
2. **Observez** : PAR, gros PAR (RVBW), barres, effet en bleu ; les lyres passent sur l'emplacement **bleu de leur roue** ; les UV restent éteints (la palette ne leur dit rien).
3. **Modifier** : dans le panneau Palettes, clic droit sur **Bleu** → *Mettre à jour depuis le programmeur* après avoir réglé une autre couleur dans le programmeur (exemple 6) : toutes les scènes qui l'utilisent suivent (PAL-005).

## Exemple 3 – Chenillard 4 couleurs (MOT-010, MOT-013)

1. ▶ **Chenillard 4 couleurs** : rouge, vert, bleu, jaune tournent d'un PAR à l'autre toutes les 0,5 s.
2. Dans l'éditeur (sélectionnez la scène) : **Vitesse** à 2 → deux fois plus vite, en direct. **Boucle** « Aller-retour » ou « Aléatoire » pour comparer.
3. La liste indique l'étape en cours ; l'étape jouée est marquée « ▶ en cours » dans le bandeau des étapes.

## Exemple 4 – Lyres sur 3 positions (MOT-011)

1. ▶ **Lyres sur 3 positions** : Piste centre → Plafond → Croisé, fondus de 1,5 s.
2. **À régler sur place** : ces positions sont des propositions. Sélectionnez les lyres dans le programmeur, réglez Pan/Tilt, puis clic droit sur la palette → *Mettre à jour depuis le programmeur*.

## Exemple 5 – Fondu lent contre instantané, roue qui bascule (MOT-011, MOT-012)

1. ▶ **Fondu lent (4 s)** puis ■ : entrée et sortie en 4 s.
2. ▶ **Instantané** : la couleur arrive tout de suite.
3. ▶ **Roue de couleur qui bascule franchement** : sur les lyres, l'intensité monte en 2 s mais la **roue saute** directement à l'emplacement suivant, sans passer par les couleurs voisines.

## Exemple 6 – Créer une scène avec le programmeur (SCN-030 à 036)

1. **Nouvelle…** → « Mon essai ». Dans le programmeur, bouton **Tous les PAR** (sélection automatique).
2. Couleur : Rouge 100 %. Hors aveugle, les PAR réagissent tout de suite (mais restent éteints : leur gradateur est à 0).
3. **Remplacer l'étape** : « Allumer en coloriant » étant coché, l'étape reçoit aussi l'intensité 100 %.
4. **Vider** le programmeur, puis **▶ Tester** : la scène s'allume en rouge.
5. **+ Ajouter** une étape, réglez une autre couleur, **Remplacer l'étape** : c'est un chenillard. Réglez les durées dans « Fondu d'entrée » et « Maintien ».
6. **Annuler** / **Rétablir** (en haut à droite) défont vos modifications.
7. **Ce que ça illustre** : seuls les attributs touchés sont enregistrés (le ● orange les marque ; « Retirer » en enlève un).

## Exemple 7 – Aveugle et aperçu (SCN-035, GEN-063)

1. Dans l'en-tête du panneau **Programmeur**, cochez **Aveugle** (aucune scène n'a besoin d'être choisie). Réglez une couleur dans le programmeur : **rien ne change** sur les appareils.
2. Écran **Simulateur** : la source indique **APERÇU** en orange et montre votre réglage.
3. Décochez : les réglages passent à la sortie.

## Exemple 8 – Vague gauche → droite (SCN-010)

1. ▶ **Vague gauche → droite** : chaque PAR démarre 0,5 s après son voisin (ordre de la sélection « PAR gauche → droite »).
2. **Modifier** : dans le programmeur, « Retard réparti sur la sélection » à 3, sélection enregistrée, puis « Appliquer aux réglages faits ».

## Exemple 9 – Piège : couleur sans intensité (DEMO-4, D15, D27)

1. ▶ **Piège : couleur sans intensité** : les PAR 7 canaux **restent éteints**.
2. **Pourquoi** : la scène ne donne qu'une couleur ; le gradateur des PAR vaut 0 par défaut. C'est la convention des consoles (intensité séparée de la couleur) ; le programmeur l'évite avec « Allumer en coloriant ».

## Exemple 10 – Console : attributs, écart en relatif, origine (CONS-022, CONS-091, CONS-042)

1. Console, mode **Appareils** : un réglage de gradateur est maintenant **soumis au Grand Master** (baissez-le pour voir).
2. Un appareil RVB 3 canaux montre un fader « Intensité (virtuelle) » ; monter le rouge allume l'appareil.
3. Mode **Canaux**, sélection de 3 faders à 55, 105, 155, glisser en relatif vers le haut au-delà de 255, puis redescendre : l'écart +50 est conservé.
4. Survolez le moniteur pendant qu'une scène joue : l'origine de la valeur s'affiche (couche / scène, surcharge, blackout, défaut).

## Exemple 11 – Sans interface (GEN-131, GEN-132, MOT-103)

```bash
luxia-headless valider "samples/Show de travail"
```

```bash
luxia-headless jouer "samples/Show de travail" --scene "Chenillard 4 couleurs" --duree 3 --pas 0.5
```

Le résumé dit, instant par instant, qui s'allume, en quelle couleur et où pointent les lyres ; `--enregistrer essai.dmxrec`
produit un enregistrement rejouable. Un scénario de commandes horodatées (format : doc 50 §13) se joue avec
`luxia-headless scenario <projet> fichier.txt`.

## Exemple 12 – Changer le mode d'un appareil utilisé par des scènes (SC-03)

1. Créez une scène qui règle le **Strobe** des PAR (programmeur, outil « Strobe / Obturateur ») et enregistrez-la.
2. Installation → PAR 1 → mode « 3 canaux » → *Changer de mode* : la confirmation liste cette valeur de strobe, qui ne
   s'appliquera plus (pas de canal Strobe en 3 canaux). Les scènes P4, elles, n'y figurent pas : couleur et intensité existent
   aussi en 3 canaux (l'intensité devient l'intensité virtuelle).
3. Annulez, ou revenez en « 7 canaux » ensuite.

---

## Ce qui n'est pas encore là

| Manque | Quand |
|---|---|
| Éditeur de couches, flashs, figer, limites de sûreté (strobe, fumée, zones) | P5 |
| Palettes de position par lieu, repli sur « Générique » | P5 |
| Sélection au plan (clic, lasso), sélection de cellules | Plus tard (SIM-010, INST-034) |
| Roue chromatique et pad Pan/Tilt dans le programmeur (faders à la place) | À voir ensemble |
| Interpolation des couleurs par teinte (MOT-054) | P6 |
| Tempo réel (les durées en temps utilisent 120 BPM fixe) | P7 |
| Journal des commandes affiché (il est tenu par le moteur) | Live, P5 |

## Questions ouvertes à trancher avec vous

- **Q27** : modèle réel des gros PAR (LPC010 ou LPC120).
- **Q28** : plage « sans strobe » du canal Strobe du LPC008S et de la barre LCB803 (le simulateur les montre clignotants à 0).

## Grille de retour

| Exemple | Correct | À revoir | Idée / remarque |
|---|---|---|---|
| 1 – Blanc chaud, GM, blackout | ☐ | ☐ | |
| 2 – Une palette, des appareils | ☐ | ☐ | |
| 3 – Chenillard | ☐ | ☐ | |
| 4 – Lyres 3 positions | ☐ | ☐ | |
| 5 – Fondus, roue | ☐ | ☐ | |
| 6 – Programmeur | ☐ | ☐ | |
| 7 – Aveugle | ☐ | ☐ | |
| 8 – Vague | ☐ | ☐ | |
| 9 – Piège | ☐ | ☐ | |
| 10 – Console | ☐ | ☐ | |
| 11 – Sans interface | ☐ | ☐ | |
| 12 – Changement de mode | ☐ | ☐ | |
