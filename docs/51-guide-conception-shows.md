# 51 – Guide de conception des shows

> Exigence **GEN-134**. Pour l'utilisateur comme pour une IA de conception qui écrit directement les fichiers du projet (D18,
> GEN-130 à 133). Formats : [50 §10 à §12h](50-format-des-donnees.md) ; comportement : [20](20-show-et-sequences.md) ; exemples
> commentés : contenu « Phase P8 » du [show de référence](41-show-de-reference.md) (`séquences.json`, `shows.json`).

## 1. Les trois étages

| Étage | Fichier | Rôle | Règle d'or |
|---|---|---|---|
| **Scène** | `scènes.json` | Un état ou un chenillard d'une **couche** (couleurs, mouvements, effets…) | Une scène fait **une seule chose** (une couleur, un mouvement) : on les combine par les couches |
| **Séquence** | `séquences.json` | Des scènes posées sur des pistes, **en mesures** | Une séquence raconte **une phrase musicale** (8, 16 mesures) et se rejoue sur n'importe quel morceau |
| **Show** | `shows.json` | Des étapes qui jouent scènes et séquences, reliées par des transitions **musicales** | Un show réagit à la **structure** du morceau (montée, drop, break), pas au temps qui passe |

## 2. Couches : qui fait quoi

- **Intensité** décide de *combien* ça éclaire ; **Couleurs** de la teinte ; **Mouvements** de la position des lyres ; **Effets** des
  modulations (vagues, strobe) ; **Ambiance** de l'UV et de la fumée (protégée de « Stop ») ; **Flashs** des éclats maintenus.
- L'intensité se fusionne **au plus haut** (HTP) : une scène *Plein feu* à 100 % masque toute variation d'intensité d'une autre
  couche. Pour une montée en intensité, faire varier le **niveau de la couche** qui allume (action `layerLevel` en rampe), pas une
  scène d'intensité par-dessus *Plein feu* (exemple : *Montée 16 mesures*).
- Les **palettes** (couleurs, positions par lieu) rendent un show transportable : une scène qui vise la palette « Piste centre »
  reste juste quand on change de salle.

## 3. Séquences

- Positions et durées en **mesures décimales** depuis 0 : `start: 8` = mesure 9 ; `0.25` = un temps. Une piste = **une couche**,
  et seules les scènes de cette couche s'y posent ; la piste sans `layerId` porte les **actions** (niveaux en rampe, fumée, flash, noir).
- Deux blocs qui se touchent sur la même piste se **relaient sans noir** (fondu croisé de la couche) ; la même scène sur deux blocs
  consécutifs **ne se relance pas**.
- `quantize: "bar"` (défaut) : la séquence attend la mesure suivante ; `"phrase8"` pour un départ de phrase.
- `end: "loop"` pour un fond qui tourne (couplet), `"stop"` pour un moment (montée, explosion).
- Longueurs usuelles : 4 (explosion), 8 (groove, break), 16 (montée). Garder les phrases **multiples de 4 mesures**.

## 4. Shows : modèles qui marchent

| Modèle | Structure | Exemple |
|---|---|---|
| **Couplet / Refrain / Drop** | Intro → Couplet (séquence en boucle) ; au **drop** → Refrain ; à la **montée** → Montée ; au **break** → Couplet ; filet de sécurité « après 16 mesures » → Couplet | *Couplet / Refrain / Drop* |
| **Variantes au hasard** | Base ; toutes les 8 mesures, tirage pondéré entre deux ou trois variantes, `avoidRepeat` ; retour à la base | *Tirage au sort (variantes)* |
| **Ambiance parallèle** | Show `secondary: true` : UV, fumée occasionnelle (`random` par phrase) | *Ambiance UV et fumée (secondaire)* |
| **Compteur** | Variable incrémentée à l'entrée du refrain ; au 3e, variante finale | `refrains ≥ 3` → *Final* |

Règles :

1. **Quantifier** les transitions musicales (`quantize: "bar"` ou une phrase) : le changement tombe sur la musique. Les événements
   (drop, break) sont retenus jusqu'à la frontière.
2. Toujours un **filet de sécurité** temporel (« après 16 mesures ») à côté d'un événement qui peut ne jamais venir (break).
3. **Jamais de boucle sans condition** : deux étapes reliées par des transitions `always` non quantifiées sont refusées (le show
   tournerait sans fin). Une quantification suffit à casser la boucle.
4. Préférer `play` / `playSequence` (actions **continues**, arrêtées en quittant l'étape, sans coupure si l'étape suivante rejoue la
   même) à `launch` (qui laisse jouer après l'étape).
5. Une étape sans transition sortante est une **fin** : par défaut le show la **tient** (`atEnd: "hold"`) ; `restart` pour un show
   qui tourne toute la soirée.
6. Identifiants d'étapes **courts et parlants** (« 0 », « 1 », « 2a », « pont ») : ce sont eux que citent les transitions.
7. Ordre des transitions = **priorité** : mettre l'événement rare (drop) avant le filet temporel.

## 5. Variété et soirée

- Plusieurs shows par style (doc 22 : 2 à 3 principaux par famille fréquente) ; dans un show, des **variantes** tirées au sort.
- Renseigner `role`, `styles`, `energyMin` / `energyMax`, `weight` : le pilote automatique (P10) s'en servira pour choisir.

## 6. Sûreté (rappel)

Strobe et fumée restent plafonnés par `sûreté.json` quoi que demande un show (GEN-042) : ne pas compter sur un long strobe ; un bloc
de fumée long est raccourci par le limiteur. Les zones interdites des lyres s'appliquent aussi.

## 7. Vérifier sans matériel

```powershell
luxia-headless valider "<projet>"
luxia-headless jouer "<projet>" --sequence "Montée 16 mesures" --duree 34 --pas 2
luxia-headless scenario "<projet>" essai.txt
```

Scénario d'un show sans musique (doc 50 §13) :

```text
0    tempo 128
0    show "Couplet / Refrain / Drop"
3    energie 40
9    simuler drop
15   simuler break
19   simuler montee
```

Le résumé liste chaque étape activée et son motif ; `valider` liste les erreurs (fichier, objet, champ, motif).
