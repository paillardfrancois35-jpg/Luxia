# 50 – Format des données

> Documentation des fichiers lus et écrits par l'application (GEN-050, GEN-051, GEN-130), tenue à jour **au fil du développement**.
> Destinée à l'utilisateur comme à une IA de conception (doc 02 §17b). Schémas JSON : dossier [`schemas/`](schemas/) ; vérification complète d'un projet : `luxia-headless valider` (GEN-131).

## 1. Règles communes

| Règle | Détail |
|---|---|
| Encodage | JSON **UTF-8 sans BOM**, indenté de 2 espaces, fins de ligne `\n` ; accents écrits tels quels |
| Noms de propriétés | `camelCase`, en anglais (identifiants du code, doc 03 §2) |
| Énumérations | en texte `camelCase` (ex. `"arduino"`, `"enttec"`) |
| Valeurs absentes | propriété omise (valeur par défaut) |
| Version | propriété **`formatVersion`** (entier), toujours **en premier** |
| Tolérance à la lecture | commentaires `//` et virgules finales acceptés (fichiers modifiés à la main) ; casse des propriétés indifférente |
| Migration | un fichier d'une version antérieure est migré à l'ouverture ; l'original est gardé en **`<fichier>.v<N>.bak`** |
| Fichier illisible | renommé en **`<fichier>.illisible-AAAAMMJJ-HHMMSS`**, signalé, le reste se charge (GEN-056) |
| Format plus récent | refusé **sans modification** du fichier |
| Écriture | dans `<fichier>.tmp` puis remplacement : jamais de fichier à moitié écrit |
| Noms de fichiers et dossiers | en français (visibles par l'utilisateur) |

## 2. Emplacements

| Données | Emplacement |
|---|---|
| Préférences du poste | `%AppData%\LuXia\preferences.json` |
| Projets | `Documents\LuXia\Projets\<nom>\` (un dossier par projet) |
| Bibliothèque d'appareils | `Documents\LuXia\Bibliothèque\<fabricant>\<modèle>.json` (P2) |
| Journaux | `Documents\LuXia\Journaux\technique-AAAAMMJJ.log` |
| Enregistrements de trames | `Documents\LuXia\Enregistrements\*.dmxrec` |

## 3. `preferences.json` (format 1)

Préférences **du poste** : ne voyagent pas avec un projet (SORT-006).

```json
{
  "formatVersion": 1,
  "tickRateHz": 40,
  "lastProjectPath": null,
  "outputs": {
    "assignments": [ { "universe": 1, "driver": "arduino" } ],
    "arduino": {
      "lastPort": "COM5",
      "forcedPort": null,
      "protocol": "enttec",
      "channelCount": 512,
      "probeAllPorts": false
    },
    "recordingsFolder": null
  },
  "testOutput": {
    "range": "1-16",
    "excludedChannels": "180",
    "heldChannels": "1, 8, 15, 22",
    "valuePercent": 50,
    "stepMilliseconds": 1000
  }
}
```

| Propriété | Valeurs | Rôle |
|---|---|---|
| `tickRateHz` | 25 à 44 | Fréquence du moteur (GEN-030) |
| `outputs.assignments[].driver` | `arduino`, `null` | Pilote affecté à l'univers (SORT-001) |
| `outputs.arduino.lastPort` | `COMn` | Port essayé en premier (SORT-011), mis à jour automatiquement |
| `outputs.arduino.forcedPort` | `COMn` ou absent | Port imposé sans détection (SORT-014) |
| `outputs.arduino.protocol` | `enttec`, `legacy` | `legacy` = ancien firmware POC (label 0x11), port imposé uniquement |
| `outputs.arduino.channelCount` | 1 à 512 | Canaux émis (SORT-021) |
| `outputs.arduino.probeAllPorts` | booléen | Sonder aussi les ports série qui ne sont pas des cartes Arduino |
| `testOutput.range` | `"1-16"` | Plage du chenillard de test |
| `testOutput.excludedChannels` | `"180"`, `"1, 5-8"` | Canaux jamais allumés par le test |
| `testOutput.heldChannels` | `""`, `"1, 8, 15, 22"` | Canaux maintenus à la valeur de test pendant tout le chenillard (SORT-008) |
| `testOutput.valuePercent` | 0 à 100 | Valeur de test (50 % = 128) |

## 4. Projet : `projet.json` (format 1)

Un projet est un **dossier** ; `projet.json` en est la fiche d'identité. Les autres fichiers (installation, scènes…) s'ajoutent phase par phase.

```json
{
  "formatVersion": 1,
  "id": "08f9f670-752e-4bf2-a786-84f919593d35",
  "name": "Show de référence",
  "description": "Show complet construit phase par phase avec le parc réel (doc 41).",
  "createdUtc": "2026-09-24T21:20:46.398855Z",
  "modifiedUtc": "2026-09-24T21:20:46.398855Z"
}
```

| Propriété | Rôle |
|---|---|
| `id` | Identifiant stable (GUID) : jamais modifié, sert aux références (GEN-052) |
| `name` | Nom affiché |
| `createdUtc`, `modifiedUtc` | Dates ISO 8601 en UTC |

## 5. Projet : `console.json` (format 1)

Instantanés de console (CONS-010) : faders pris, rappelables d'un clic.

```json
{
  "formatVersion": 1,
  "snapshots": [
    {
      "id": "5bf11614-9c8b-51d5-b3ba-35dda6554557",
      "name": "PAR 1 en blanc",
      "category": "Phase P1",
      "description": "Ce qu'on doit observer…",
      "universe": 1,
      "channels": [ { "channel": 1, "value": 255 }, { "channel": 2, "value": 255 } ]
    }
  ]
}
```

| Propriété | Rôle |
|---|---|
| `id` | Identifiant stable |
| `name` | Nom affiché (unique dans le projet, sans tenir compte de la casse) |
| `category` | Regroupement ; « Phase Pn » pour le contenu livré par une phase et pas encore validé (doc 41 REF-3) |
| `universe` | Univers (1 = premier) |
| `channels` | Canaux (1-512) et valeurs (0-255) imposés au rappel ; les autres faders de l'univers sont libérés |

## 6. Projet : `installation.json` (format 1)

Univers, patch, sélections manuelles (doc 13, INST-001 à 034). Les sélections **automatiques** (Tous, par modèle, par
catégorie) ne sont pas enregistrées : recalculées à la volée (D24).

```json
{
  "formatVersion": 1,
  "universes": [ { "number": 1, "name": "Salle" } ],
  "fixtures": [
    {
      "id": "5c9e2f10-...",
      "fixtureTypeId": "0b6d...",
      "modeName": "7 canaux",
      "universe": 1,
      "address": 1,
      "name": "PAR 1",
      "number": 1,
      "color": "#58A6FF",
      "twinGroupId": null,
      "options": { "invertPan": false, "invertTilt": false, "swapPanTilt": false, "panOffsetDegrees": 0 }
    }
  ],
  "selections": [
    { "id": "e1a4...", "name": "PAR gauche", "color": "#58A6FF", "items": [ { "fixtureId": "5c9e2f10-...", "cell": 0 } ] }
  ]
}
```

| Propriété | Rôle |
|---|---|
| `fixtures[].fixtureTypeId` | Identifiant du modèle dans la **copie du projet** (§8, GEN-053), jamais la bibliothèque partagée directement |
| `fixtures[].modeName` | Nom du mode utilisé (`modes[].name` du modèle) |
| `fixtures[].twinGroupId` | Jumeaux (INST-014) : deux appareils du même groupe peuvent partager une adresse |
| `fixtures[].options` | Montage (INST-021) : inversions, échange Pan/Tilt, décalage et bornes propres à l'appareil |
| `selections[].items[].cell` | 0 = appareil entier, sinon numéro de cellule (doc 12 §2.6, INST-034) |

## 7. Projet : `lieux.json` (format 1)

Lieux du projet (doc 13 §5) : plan, position des appareils, présence, lieu actif.

```json
{
  "formatVersion": 1,
  "venues": [
    {
      "id": "a1b2...",
      "name": "Générique",
      "widthM": 12,
      "depthM": 8,
      "placements": [
        { "fixtureId": "5c9e2f10-...", "x": 1.5, "y": 0.5, "heightM": 2, "orientationDeg": 0, "mounting": "standing", "absent": false }
      ]
    }
  ],
  "activeVenueId": "a1b2..."
}
```

| Propriété | Rôle |
|---|---|
| `venues[].placements[].mounting` | `standing` (posé) ou `hanging` (suspendu) |
| `venues[].placements[].absent` | Appareil non emporté ce soir (INST-052) : non émis, absent du simulateur et des sélections actives |
| `activeVenueId` | Lieu actif ; absent ou introuvable = le premier lieu de la liste |

## 8. Bibliothèque : modèle d'appareil (format 1)

Un fichier par modèle : `Bibliothèque\<fabricant>\<modèle>.json` (doc 12 §8). Exemple réduit (PAR 3 / 7 canaux) :

```json
{
  "formatVersion": 1,
  "id": "0b6d…",
  "manufacturer": "Betopper",
  "model": "LPC008S",
  "category": "par",
  "version": 1,
  "source": "manual",
  "notes": "Menu d001 = 3 canaux, A001 = 7 canaux.",
  "physical": { "sourceType": "LED RGB 3-en-1", "power": 180 },
  "channels": [
    { "key": "dim", "name": "CH1 Gradation générale", "attribute": "intensity" },
    { "key": "r", "name": "Rouge", "attribute": "red" },
    { "key": "strobe", "name": "Strobe", "attribute": "shutter",
      "capabilities": [
        { "min": 0, "max": 10, "kind": "closed", "label": "Éteint", "strobe": "closed" },
        { "min": 11, "max": 255, "kind": "progressive", "label": "Strobe lent → rapide", "strobe": "strobe",
          "parameter": { "nature": "fréquence", "start": 1, "end": 20, "unit": "Hz" } } ] }
  ],
  "modes": [
    { "name": "3 canaux", "shortName": "3CH", "deviceSetting": "d001", "channels": [ { "channel": "r" } ] },
    { "name": "7 canaux", "shortName": "7CH", "deviceSetting": "A001",
      "channels": [ { "channel": "dim" }, { "channel": "r" }, { "channel": "strobe" } ] }
  ]
}
```

| Élément | Propriétés | Valeurs |
|---|---|---|
| Modèle | `manufacturer`, `model`, `reference`, `category`, `version`, `author`, `source`, `derivedFrom`, `notes`, `manual`, `photo`, `physical`, `wheels`, `channels`, `modes`, `whiteMode` | `whiteMode` (facultatif, MOT-051) : `extract` (défaut : blanc = min(R,V,B) retiré des couleurs), `off`, `boost` (blanc ajouté sans rien retirer) ; `category` : `par`, `ledBar`, `movingHead`, `effect`, `strobe`, `uv`, `smoke`, `laser`, `dimmer`, `other` ; `source` : `manual`, `ofl`, `qlcPlus`, `generic` |
| `physical` | `sourceType`, `beamAngle`, `panRange`, `tiltRange` (degrés), `power` (W), `warmupSeconds` | |
| Canal | `key` (unique), `name`, `attribute`, `cell` (0 = appareil), `resolution` (`bit8` / `bit16`), `default`, `rest`, `identify`, `inverted`, `followsIntensity` (absent = déduit), `safety` (absent = déduit ; `strobe`, `smoke`, `movement`, combinables « strobe, movement »), `wheel` (clé de roue), `capabilities`, `notes` | `attribute` : `intensity`, `cellIntensity`, `red`, `green`, `blue`, `white`, `warmWhite`, `amber`, `uv`, `cyan`, `magenta`, `yellow`, `lime`, `colorWheel`, `colorMacro`, `colorTemperature`, `pan`, `tilt`, `panContinuous`, `tiltContinuous`, `panTiltSpeed`, `shutter`, `gobo`, `goboRotation`, `prism`, `prismRotation`, `focus`, `zoom`, `iris`, `frost`, `rotation`, `rotationSpeed`, `program`, `programSpeed`, `soundSensitivity`, `mode`, `smoke`, `fan`, `reset`, `maintenance`, `lampControl`, `generic`, `noFunction` |
| Plage | `min`, `max` (inclus, 0-255), `kind`, `label`, `strobe`, `parameter` { `nature`, `start`, `end`, `unit` }, `colors` (« #RRGGBB », 1 ou 2), `wheelSlot`, `autoPalette` | `kind` : `fixed`, `progressive`, `wheelSlot`, `rotation`, `program`, `noFunction`, `closed`, `open` ; `strobe` : `closed`, `open`, `strobe`, `pulse`, `random` |
| Mode | `name`, `shortName`, `deviceSetting`, `channels` : liste ordonnée de { `channel` (clé), `part` (`coarse` par défaut, `fine` pour l'octet fin d'un canal 16 bits) } | la position 1 est à l'adresse de l'appareil |
| Roue | `key`, `name`, `kind` (`color` / `gobo`), `slots` : { `name`, `colors`, `image` } | |

Règles de validation : doc 12 §3 (BIB-004) et §10.

**Copie du projet (GEN-053)** : même format, un fichier par modèle sous `<projet>/Bibliothèque/<fabricant>/<modèle>.json`.
Alimentée au patch (§6) ; une modification de la bibliothèque partagée ne s'y répercute que sur action explicite
(« Mettre à jour »), avec le rapport d'impact (canaux perdus / gagnés, doc 13 §3 INST-016). Les modèles génériques de
l'application n'y sont jamais copiés (D24).

## 9. Enregistrement de trames `.dmxrec` (format binaire 1)

Fichier binaire compact (SORT-060), petit-boutiste.

| Partie | Contenu |
|---|---|
| En-tête (24 octets) | `"DMXREC"` (6 octets ASCII) · version `uint16` · univers `uint16` · fréquence `float32` (Hz) · début `int64` (ticks .NET UTC) · nombre de canaux `uint16` |
| Trame (répétée) | temps écoulé depuis la première trame en ms `uint32` · longueur `uint16` · valeurs (longueur octets) |

Une **longueur 0** signifie « trame identique à la précédente » : un univers au repos ne coûte que 6 octets par trame.
Une fin de fichier tronquée (arrêt brutal) est tolérée à la lecture : seules les trames complètes sont relues.
Lecture : `dmx-headless relire fichier.dmxrec`.

## 10. Projet : `scènes.json` (format 1)

Scènes du projet (doc 16, SCN-001 à 039). Une scène ne contient **que les attributs qu'elle touche**. Les valeurs visent
des **attributs** d'appareils, jamais des canaux : la traduction en octets est faite par l'application (D26, D27).
Schéma JSON : [`schemas/scenes.schema.json`](schemas/scenes.schema.json).

```json
{
  "formatVersion": 1,
  "scenes": [
    {
      "id": "8f0c…",
      "name": "Blanc chaud sur les 4 PAR",
      "color": "#FFC773",
      "icon": "☀",
      "category": "Phase P4",
      "notes": "Ce que la scène montre.",
      "visibleInLive": true,
      "layerId": "7c1a0001-0000-4000-8000-000000000002",
      "loop": "infinite",
      "loopCount": 1,
      "end": "stop",
      "chainSceneId": null,
      "fadeIn": null,
      "fadeOut": { "value": 2, "unit": "seconds" },
      "speed": 1,
      "steps": [
        {
          "name": "Entrée",
          "fade": { "value": 1, "unit": "seconds" },
          "hold": { "value": 2, "unit": "beats" },
          "curve": "linear",
          "switch": "start",
          "values": [
            { "target": { "auto": { "kind": "byModel", "model": "Betopper LPC008S" } }, "paletteId": "9a1e0001-0000-4000-8000-000000000002" },
            { "target": { "fixtureId": "743c…", "cell": 0 }, "attribute": "intensity", "level": 1 },
            { "target": { "selectionId": "e1a4…" }, "color": { "r": 1, "g": 0.5, "b": 0, "uv": 0.2 }, "spread": { "value": 1.5, "unit": "seconds" } },
            { "target": { "fixtureId": "c58c…" }, "channel": "gobo", "range": { "min": 32, "max": 39 } }
          ]
        }
      ]
    }
  ]
}
```

| Propriété | Valeurs | Rôle |
|---|---|---|
| `layerId` | identifiant d'une couche (§12) | Couche d'appartenance ; inconnue = première couche (avertissement) |
| `loop` | `once`, `count`, `infinite`, `pingPong`, `random` | Boucle (MOT-013) ; `loopCount` pour `count` |
| `end` | `stop`, `hold`, `chain` | Fin d'une scène jouée une ou N fois (MOT-014) ; `chainSceneId` pour `chain` |
| `fadeIn` / `fadeOut` | durée ou `null` | Fondu d'entrée (défaut : celui de la 1ʳᵉ étape) ; de sortie (défaut : arrêt immédiat) |
| `speed` | 0,1 à 10 | Vitesse (MOT-015) |
| durée (`fade`, `hold`, `delay`, `spread`…) | `{ "value": n, "unit": "seconds" \| "beats" \| "bars" }` | Secondes ou temps musicaux (GEN-023) ; en P4, 120 BPM fixe |
| `steps[].curve` | `linear`, `sCurve`, `instant` | Courbe du fondu (MOT-011) |
| `steps[].switch` | `start`, `middle`, `end` | Moment où bascule un attribut discret (roue, gobo, programme) (MOT-012) |
| `values[].target` | **une seule** forme : `fixtureId` (+ `cell`, 0 = appareil entier) ; `selectionId` (sélection manuelle, dans son ordre) ; `auto` = `{ "kind": "allFixtures" }`, `{ "kind": "byCategory", "category": "par" }` ou `{ "kind": "byModel", "model": "Fabricant Modèle" }` | Cible (SCN-007) ; une sélection automatique suit le patch |
| `values[]` : forme | **une seule** : `attribute` (ou `channel`) + `level` (0-1) ; `attribute` (ou `channel`) + `range` (`min`, `max`, `position` 0-1 facultative, sinon la médiane) ; `color` (`r`, `g`, `b` 0-1 ; `white`, `amber`, `uv` facultatifs) ; `paletteId` | Valeur (SCN-008) |
| `values[].attribute` | nom d'attribut du doc 12 §2.3 en camelCase (`intensity`, `red`, `pan`, `tilt`, `gobo`, `shutter`…) | Tous les canaux de cet attribut dans la cible ; `intensity` sur un appareil sans gradateur = intensité virtuelle |
| `values[].channel` | clé d'une définition de canal du modèle | Canal précis (appareil à plusieurs canaux du même attribut) |
| `values[].fade` / `delay` / `spread` | durées | Fondu propre (SCN-011), retard, retard réparti sur les membres dans l'ordre de la sélection (SCN-010) |

Règles de résolution (D27) : dans une étape, une valeur sur une **cellule** l'emporte sur l'appareil, qui l'emporte sur
une **sélection** ; à précision égale, la dernière de la liste. Une couleur est traduite par appareil (RVB, RVBW selon
`whiteMode` du modèle, emplacement de roue le plus proche, UV/ambre seulement s'ils sont précisés). **Une couleur seule
n'allume pas un appareil dont l'intensité vaut 0** : ajouter `intensity` (le programmeur le fait tout seul, MOT-041).

**Contenu écrit par une IA de conception (GEN-133)** : ranger les scènes dans la catégorie `"Proposé par IA"`, créer de
**nouveaux** identifiants (ne jamais réutiliser celui d'une scène existante), puis vérifier avec
`luxia-headless valider <projet>` et `luxia-headless jouer <projet> --scene "<nom>"`. Dans l'application : menu
**Projet → Relire les scènes et palettes**, sans redémarrer.

## 11. Projet : `palettes.json` (format 1)

Palettes (doc 17 §2). Absent = jeu par défaut (13 couleurs, 4 intensités, identifiants fixes `9a1e0001-…`).
Schéma JSON : [`schemas/palettes.schema.json`](schemas/palettes.schema.json).

```json
{
  "formatVersion": 1,
  "palettes": [
    { "id": "9a1e0001-0000-4000-8000-000000000005", "name": "Ambre", "kind": "color", "light": { "r": 1, "g": 0.5, "b": 0 },
      "values": [ { "fixtureTypeId": "76a6…", "attribute": "white", "level": 0.2 } ] },
    { "id": "9a1e0001-0000-4000-8000-000000000101", "name": "Plein", "kind": "intensity", "color": "#E3B341", "level": 1 },
    { "id": "3d5e…", "name": "Piste centre", "kind": "position",
      "values": [ { "fixtureId": "c58c…", "attribute": "pan", "level": 0.5 }, { "fixtureId": "c58c…", "attribute": "tilt", "level": 0.7 } ] },
    { "id": "71aa…", "name": "Gobo étoile", "kind": "beam", "values": [ { "fixtureTypeId": "a96c…", "channel": "gobo", "level": 0.141 } ] }
  ]
}
```

| Propriété | Rôle |
|---|---|
| `kind` | `color` (`light` : couleur logique), `intensity` (`level`), `position` (Pan/Tilt par appareil), `beam` (gobo, prisme… par modèle ou appareil) |
| `values[]` | Valeurs par appareil (`fixtureId`) ou par modèle (`fixtureTypeId`) : pour une couleur, elles **priment** sur la traduction automatique pour ce modèle (PAL-002) |
| `color`, `icon` | Bouton (GEN-106) ; une palette couleur sans `color` affiche sa propre couleur |

L'ordre du tableau est l'ordre des grilles (PAL-007). Une palette se référence par son `id` : la renommer ne casse rien.

## 12. Projet : `couches.json` (format 1)

Couches (doc 17 §1). Absent = modèle par défaut du doc 17 §1.3 (D28), identifiants fixes `7c1a0001-…-00000000000N`
(Intensité 1, Couleurs 2, Mouvements 3, Faisceau 4, Effets 5, Ambiance 6, Flashs 99). Éditeur : P5.

```json
{
  "formatVersion": 1,
  "layers": [
    { "id": "7c1a0001-0000-4000-8000-000000000002", "name": "Couleurs", "color": "#DB61A2", "icon": "◐", "priority": 2,
      "exclusive": true, "master": 1, "intensityMode": "htp", "masterOnAllAttributes": false,
      "crossFade": { "value": 0.5, "unit": "seconds" } }
  ]
}
```

| Propriété | Rôle |
|---|---|
| `priority` | Ordre de fusion LTP : la plus haute l'emporte (MOT-031) |
| `exclusive` | Lancer une scène remplace celle qui joue, en fondu croisé de `crossFade` (MOT-030) |
| `intensityMode` | `htp`, `priority`, `additive`, `multiplicative` (doc 15 §5.2) |
| `master`, `masterOnAllAttributes` | Master de la couche (MOT-033) |

## 13. Scénario de commandes `luxia-headless` (texte)

Pour piloter le moteur sans interface (MOT-103) : `luxia-headless scenario <projet> fichier.txt [--duree 60] [--enregistrer f.dmxrec]`.
Une commande par ligne, `temps commande arguments` (temps en secondes, virgule ou point) ; `#` = commentaire ; nom de
scène ou de couche entre guillemets s'il contient des espaces (ou identifiant).

```text
# démonstration
0     lancer "Blanc chaud sur les 4 PAR"
2     lancer "Lyres sur 3 positions" solo
5     blackout oui
5,5   blackout non
6     grand-master 50
7     master-couche "Couleurs" 30
8     vitesse "Chenillard 4 couleurs" 2
9     etape-suivante "Chenillard 4 couleurs"
10    arreter "Blanc chaud sur les 4 PAR"
11    tout-arreter
12    fin
```

Le résultat est un **résumé lisible** (qui s'allume, en quelle couleur, à quel niveau, où pointent les lyres, à quel
moment) et, avec `--enregistrer`, un fichier `.dmxrec` (§9). `luxia-headless jouer <projet> --scene "nom"` joue une
seule scène (GEN-132).

## 14. Historique

| Date | Modification |
|---|---|
| 2026-09-24 | P0 : règles communes, `preferences.json`, `projet.json`, `.dmxrec`. |
| 2026-09-24 | P1 : `console.json` (instantanés). |
| 2026-09-25 | P2 : modèle d'appareil de la bibliothèque. |
| 2026-09-26 | P3 : `installation.json`, `lieux.json`, copie de la bibliothèque dans le projet (GEN-053), `testOutput.heldChannels` (SORT-008). |
| 2026-09-26 | P4 : `scènes.json`, `palettes.json`, `couches.json`, scénario de commandes, schémas JSON ; `whiteMode` facultatif sur le modèle d'appareil (MOT-051). |
