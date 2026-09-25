# Démonstration P2 – Bibliothèque d'appareils

> Guide de découverte de la phase P2 (doc 40 §7, doc 41 §11). Durée : 45 minutes à 1 h 30 (vérification plage par plage).
> Matériel : PC ; pour le test en direct : Arduino (firmware 1.0) + un appareil à la fois (PAR, lyre, UV, effet…).
> Branche Git : `p2/bibliotheque`.

## Ce que livre P2

| Élément | Où |
|---|---|
| Écran **Bibliothèque** : liste par fabricant, recherche, filtres, éditeur, validation, annuler / rétablir, test en direct, découverte, import | `DMX.exe` → Bibliothèque |
| Définitions des appareils du parc | `samples/Bibliothèque/` (6 modèles) |
| Génériques livrés (gradateur, RGB, RGBW, fumée, strobe, canal) | intégrés à l'application (lecture seule) |
| Import Open Fixture Library (`.json`) et QLC+ (`.qxf`), fichier ou dossier | boutons « Importer… » |

## Préparer : charger les définitions du parc

1. Lancer l'application : **Bibliothèque** dans la navigation.
2. **Importer un dossier…** → choisir `samples/Bibliothèque`.
3. Le rapport d'import indique « ✓ » pour 7 modèles ; ils sont copiés dans `Documents\DMX\Bibliothèque\<fabricant>\<modèle>.json`.
   Relancer l'import : « = déjà dans la bibliothèque, non importé » (rien n'est écrasé).

| Modèle | Modes | Particularités |
|---|---|---|
| Betopper LPC008S | 3CH (`d001`), 7CH (`A001`) | Intensité virtuelle en 3 canaux |
| Betopper LPC010 | 4CH (`d001`), 8CH (`A001`) | Tableau lu dans la notice (PDF image) |
| Betopper LPC120 | 4CH (`d001`), 8CH (`A001`) | CH8 = paramètre qui dépend de CH7 |
| Tomshine Mini lyre gobo | 9CH, 11CH | Pan / Tilt 16 bits en 11CH ; canal Contrôle 200-209 = **Reset** |
| BeamZ BUV463 | 7CH | 4 rangées UV = 4 cellules |
| WZYBUTA Effet 4 têtes 150 W | 20CH, 64CH | 12 cellules RGBW en 64CH ; dernier canal = **Reset** (251-255) ; 3 canaux laser inutilisés (option absente) |
| BeamZ LCB803 | 3, 6, 12, 24, 48CH | 2 / 4 / 8 sections = cellules ; 24CH retenu pour le show |


---

## Exemple 1 – Parcourir et rechercher (BIB-020)

Tapez « lyre » dans la recherche, puis filtrez la catégorie « UV ». Les génériques sont regroupés sous « Génériques ».

## Exemple 2 – Lire une définition : le PAR LPC008S (BIB-005, BIB-006)

Ouvrez **Betopper LPC008S**, onglet **Canaux et modes** :
- deux onglets de mode : « 3 canaux (3) » et « 7 canaux (7) » ; sous l'onglet, la **fiche** : « régler l'appareil sur d001 » / « A001 » ;
- mode 3 canaux, canal Rouge : « Suit l'intensité : Automatique – déduit : **oui (intensité virtuelle)** » ;
- mode 7 canaux, même canal : « déduit : **non** » (le canal Gradation générale porte la luminosité) ;
- canal Sélecteur de fonction : 6 plages et leur barre colorée 0-255.

## Exemple 3 – Tester en direct un PAR (BIB-060, BIB-061, BIB-063)

PAR à l'**adresse 1**, en **A001** (7 canaux). Sélectionnez le mode 7 canaux, onglet **Tester en direct**, adresse 1, **Démarrer le test**.

**Observer** : la fiche « adresse 1 à 7 » ; un fader par canal, pris (orange) à sa **valeur par défaut** ; chaque canal affiche sa valeur dans l'unité la plus parlante (%, nom de plage).
- montez **Gradation** et **Rouge** : le PAR s'allume en rouge ;
- canal Sélecteur de fonction : cliquez « 101-150 Fondu » → la valeur **médiane (126)** est émise, le PAR passe en fondu automatique ;
- sur la petite barre colorée, cliquez **une frontière** : la valeur exacte de la borne est émise (ex. 51) ;
- **Arrêter** : tous les canaux sont libérés (plus rien de pris dans la Console).

## Exemple 4 – Vérifier la lyre plage par plage (T-BIB-05)

Lyre à l'adresse 111 (mode 11CH). Test en direct à l'adresse **111** :
- l'obturateur est **ouvert par défaut** (12) : montez le Gradateur, la lyre éclaire ;
- Pan et Tilt : 128 = centre ; l'affichage indique la valeur (le degré s'affiche quand l'amplitude est connue) ;
- cliquez les plages de l'**Obturateur** une à une (Éteint, Allumé, Strobe lent → rapide, pulsations, aléatoire) ;
- **Piège (règle montrée)** : le canal **Contrôle** a une plage **200-209 Reset**. Ne cliquez dessus que volontairement : la lyre se réinitialise.
  C'est pour cela que ce canal a l'attribut « Reset » (jamais animé par les effets, doc 12 §2.3) et qu'aucun instantané n'y touche.

## Exemple 5 – Découvrir un canal inconnu (BIB-062) : l'effet WZYBUTA ou la roue de couleur de la lyre

1. **Dupliquer** d'abord le modèle si c'est un générique (sinon il est en lecture seule).
2. Test en direct, puis **Découvrir…** sous le canal (ex. « Roue de couleur » de la lyre).
3. La valeur monte seule de 0 à 255 (vitesse réglable, pause, ±1) ; le bandeau affiche la valeur et la plage courante.
4. Quand la couleur change sur l'appareil : **Nouvelle plage ici** → une borne est créée à cette valeur dans la définition.
5. Retour à **Canaux et modes** : renommez les plages créées (« Rouge », « Vert »…), mettez leur couleur (`#FF0000`).
6. **Enregistrer** : la version du modèle passe à 2.

Pour l'effet WZYBUTA, découvrez le canal 2 (rotation continue) : dans chaque sens, la vitesse monte-t-elle ou descend-elle avec la valeur ? (Q25)

## Exemple 6 – Créer un modèle de A à Z (BIB-021 à 024)

**+ Nouveau modèle** : fabricant, modèle, puis
- **+ Nouveau canal** plusieurs fois ; pour chacun : nom, **attribut**, cellule ;
- réordonner par **glisser-déposer** dans la liste des positions (ou ▲ ▼) ;
- plages : **Découper en 8 plages égales**, puis déplacer une frontière à la souris sur la barre ;
- **+ Mode**, puis « Canal existant… » → Ajouter : les modes partagent les définitions (BIB-002) ;
- cochez **16 bits** sur un canal : l'octet fin est ajouté après l'octet grossier dans chaque mode (déplaçable ensuite) ;
- **Ctrl+Z / Ctrl+Y** : annuler / rétablir (100 niveaux) ;
- onglet **Validation** : les erreurs (plages qui se chevauchent, mode vide, canal fin orphelin…) et les avertissements (trous) s'actualisent à chaque modification ;
- **Enregistrer** est **refusé** tant qu'il reste une erreur (piège volontaire : retirez tous les canaux d'un mode, essayez d'enregistrer).

## Exemple 7 – Importer depuis OFL ou QLC+ (BIB-080 à 083)

Des fichiers d'essai (rédigés au format OFL / QLC+) sont dans `tests/Dmx.Fixtures.Tests/assets/` :
**Importer un dossier…** → `tests/Dmx.Fixtures.Tests/assets` → 8 modèles (dont une barre de 8 pixels = 8 cellules, une lyre avec roues).
Le **rapport** liste ce qui a été approximé (« type BladeInsertion non reconnu, converti en Générique », « préréglage EffectSparkle… »).

Avec de vrais fichiers téléchargés à la maison depuis open-fixture-library.org ou QLC+, même démarche (jamais en soirée : GEN-120).

## Ce qui n'est pas encore là

| Élément | Quand |
|---|---|
| Copie des modèles dans le projet, « Mettre à jour » avec rapport d'impacts (GEN-053, doc 12 §8) | P3 (patch) |
| Pad XY Pan/Tilt, sélecteur de couleur (CONS-021) dans les faders d'appareil | P3 (console mode appareils) |
| Ouvrir la notice / la photo depuis l'éditeur (BIB-027, S) ; export OFL (BIB-084, S) | non réalisés |
| Sélection d'une plage dans la barre de l'éditeur, gobo en image | améliorations possibles |

## Grille de retour

| Exemple | Correct | À revoir | Idée / remarque |
|---|---|---|---|
| 1 – Parcourir | ☐ | ☐ | |
| 2 – Lire une définition | ☐ | ☐ | |
| 3 – Tester un PAR | ☐ | ☐ | |
| 4 – Lyre plage par plage | ☐ | ☐ | |
| 5 – Découverte | ☐ | ☐ | |
| 6 – Créer un modèle | ☐ | ☐ | |
| 7 – Importer | ☐ | ☐ | |
