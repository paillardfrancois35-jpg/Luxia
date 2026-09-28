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
  "uiScale": 1,
  "compactScenes": false,
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
      ],
      "forbiddenZones": [
        { "fixtureId": "9d0a...", "name": "Public", "panMin": 0.3, "panMax": 0.7, "tiltMin": 0.8, "tiltMax": 1 },
        { "fixtureId": "9d0a...", "name": "Limites", "panMin": 0.1, "panMax": 0.9, "tiltMin": 0.2, "tiltMax": 0.95, "allowed": true }
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
| `venues[].forbiddenZones` | Zones interdites des lyres dans ce lieu (INST-053, P5) : rectangles de Pan et Tilt **logiques normalisés 0-1** (valeurs lues au programmeur, avant inversion de montage) ; plusieurs par lyre possibles ; le moteur ramène toute cible qui y tombe au bord le plus proche (MOT-082). Rectangle vide (min ≥ max) ignoré et signalé par `valider`. **`"allowed": true`** (facultatif, 2026-09-28, F7, ERG-017) : zone **permise** au lieu d'interdite, les limites de la lyre (la cible ne sort jamais du rectangle) ; plusieurs zones permises d'une lyre : leur intersection ; absent = zone interdite (fichiers d'avant inchangés, pas de migration) |
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
Lecture : `luxia-headless relire fichier.dmxrec`.

À côté de chaque enregistrement fait depuis l'application, un **journal texte** `<même nom>.journal.txt` (SORT-066, UTF-8) :
une ligne par événement, `heure  écart(s)  nature  détail`, natures `IHM` (onglet – action), `MOTEUR` (commande traitée),
`DMX` (canaux qui changent, `canal:avant→après`, 64 au plus par ligne) et `DMX*` (première trame). Lisible à la main ; non relu par l'application.

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
| `values[].venueId` | P5 (PAL-004) : lieu d'une valeur de **position**. Absent = lieu « Générique », qui sert aussi de **repli** là où la position n'a jamais été calibrée (signalé, PAL-008). Mettre à jour une palette depuis le programmeur n'écrit que pour les appareils sélectionnés et le lieu actif ; dupliquer un lieu copie ses positions (INST-054) |
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
| `kind` | P5 : `normal` ou `flash` (scènes actives tant que maintenues en Live et sur l'APC mini, COU-005) |
| `keepOnStopAll` | P5 : couche épargnée par « Tout arrêter » (Ambiance par défaut, COU-007) |
| `restSceneId` | P5 : scène jouée dès que la couche n'a plus de scène (COU-009) |
| `families` | P5 : familles d'attributs attendues (`intensity`, `color`, `position`, `beam`, `effectMotion`, `programs`, `atmosphere`…) ; une scène qui en touche d'autres est signalée par `valider` (COU-008) ; vide = aucune vérification |

L'éditeur de couches (écran Scènes → « Couches… ») règle tout sauf `families` et `icon` ; l'ordre de la liste y donne
les priorités. Schéma : [`schemas/couches.schema.json`](schemas/couches.schema.json).

## 12b. Projet : `sûreté.json` (format 1)

Réglages des limites de sûreté (doc 02 §13, D29). Absent = valeurs par défaut ci-dessous. Les canaux concernés sont ceux
qu'une étiquette de sûreté désigne dans la bibliothèque (`strobe`, `fumée`, BIB-007) ; un canal compte comme « en strobe »
quand sa valeur tombe dans une plage `strobe` / `pulse` / `random`, et il est forcé à sa plage `open` (sinon `rest`, sinon 0).

```json
{
  "formatVersion": 1,
  "strobe": { "maxContinuousSeconds": 10, "pauseSeconds": 10, "forbidden": false, "maxSpeedPercent": 100 },
  "smoke": { "maxEmissionSeconds": 10, "minRestSeconds": 30, "restFactor": 3 }
}
```

| Propriété | Rôle |
|---|---|
| `strobe.maxContinuousSeconds` | Strobe continu maximal **par appareil** ; au-delà, pause forcée (MOT-080). Une coupure de moins de 1 s ne remet pas le compte à zéro |
| `strobe.pauseSeconds` | Durée de la pause forcée |
| `strobe.forbidden` | Strobe interdit partout |
| `strobe.maxSpeedPercent` | Plafond de vitesse, en % de chaque plage de strobe progressive (100 = aucun) |
| `smoke.maxEmissionSeconds` | Émission continue maximale de fumée (MOT-081), commande manuelle et surcharges comprises |
| `smoke.minRestSeconds` | Repos après une émission **coupée** par la limite, et plafond de tout repos |
| `smoke.restFactor` | Repos après une émission **plus courte** que la limite = durée émise × ce facteur, plafonné à `minRestSeconds` (défaut 3 : une bouffée de 2 s → 6 s de repos ; la machine ne fume jamais plus d'un quart du temps). 0 = pas de repos après une émission courte |

## 12c. Projet : `live.json` (format 1)

Réglages de l'écran Live (doc 18), tous facultatifs : sans fichier, une colonne par couche (priorité croissante), scènes
« visibles en Live » dans l'ordre de `scènes.json`. Mêmes colonnes et mêmes boutons pour l'APC mini (D30).
Schéma : [`schemas/live.schema.json`](schemas/live.schema.json).

```json
{ "formatVersion": 1, "flashSceneId": "…", "strobeSceneId": "…", "smokeBurstSeconds": 3, "activeSceneClick": "stop", "hiddenLayerIds": [] }
```

| Propriété | Rôle |
|---|---|
| `flashSceneId`, `strobeSceneId` | Scènes jouées **en flash** par les boutons FLASH (touche F) et STROBE (touche S) ; absentes = « Flash blanc » et la première scène « Strobe… » d'une couche de type Flash |
| `smokeBurstSeconds` | Durée du bouton « Rafale » de fumée (toujours bornée par `sûreté.json`) |
| `activeSceneClick` | Clic sur la scène qui joue : `stop` (défaut) ou `restart` |
| `hiddenLayerIds` | Couches absentes du Live et de l'APC mini |

## 12d. Projet : `midi.json` (format 1)

Affectations **modifiées** des contrôleurs (MIDI-007) ; absent = affectation par défaut du doc 18b §3 sur tout contrôleur.
Une affectation remplace celle du contrôle, pour tous les modèles ou pour celui dont le nom contient `model`
(MIDI-005 : deux contrôleurs, deux affectations). Pour trouver un contrôle : `luxia-headless midi` affiche chaque appui.
Schéma : [`schemas/midi.schema.json`](schemas/midi.schema.json).

```json
{
  "formatVersion": 1,
  "bindings": [
    { "model": "MK1", "control": "pad 1 1", "action": "launchScene", "sceneId": "…" },
    { "control": "droite 5", "action": "smokeBurst" },
    { "control": "fader 8", "action": "layerMaster", "layerId": "…" }
  ]
}
```

| Propriété | Rôle |
|---|---|
| `control` | `pad <colonne 1-8> <ligne 1-8>` (ligne 1 en haut), `bas <1-8>`, `droite <1-8>` (de haut en bas), `fader <1-9>` (9 = master) |
| `action` | `launchScene`, `flashScene` (scène : `sceneId`) ; `stopLayer`, `layerMaster` (couche : `layerId`) ; `grandMaster`, `blackout` (tant que maintenu, MIDI-011), `blackoutToggle` (bascule), `flash`, `strobe`, `smoke`, `smokeBurst`, `freeze`, `stopAll`, `none` |

Profils des modèles (notes, LED) : fichiers de données du module (`src/Luxia.Midi/Profiles/*.json`).

## 12d-bis. Projet : `looks.json` (format 1)

Looks du projet (doc 60 §4.8, ERG-023) : une liste nommée d'actions appelée d'un clic (panneau Looks, interventions du
pilote automatique). Facultatif : sans fichier, aucun look. Écrit par LuXia (« Capturer ce qui joue ») ou par une IA de
conception ; `valider` signale une scène ou une couche introuvable.

```json
{
  "formatVersion": 1,
  "looks": [
    {
      "id": "…", "name": "Temps mort", "color": "#FFB000", "notes": "Pause, discours : ambre doux à 40 %.",
      "actions": [
        { "kind": "stopAll" },
        { "kind": "launchScene", "sceneId": "…" },
        { "kind": "layerMaster", "layerId": "…", "level": 0.5 },
        { "kind": "grandMaster", "level": 0.4 }
      ]
    }
  ]
}
```

| Propriété | Rôle |
|---|---|
| `looks[].actions[].kind` | `launchScene`, `stopScene` (avec `sceneId`) ; `stopLayer` (avec `layerId`) ; `stopAll` (« tout arrêter », sauf les couches épargnées comme Ambiance) ; `layerMaster` (`layerId`, `level` 0-1) ; `grandMaster` (`level` 0-1 : lu, mais « Capturer ce qui joue » n'en produit pas, le Grand Master reste à l'opérateur, C13). Jouées dans l'ordre ; une action incomplète est ignorée |
| `color`, `notes` | Couleur du bouton ; explication lisible (infobulle) |

## 12e. Projet : dossier `Versions`

Copies des fichiers JSON du projet (GEN-055, D31), `Versions\AAAAMMJJ-HHMMSS\` avec un `motif.txt` : toutes les
2 minutes si le projet a changé et à chaque passage en Live ; 10 gardées ; menu Projet → Versions du projet… pour
revenir à l'une d'elles. À ne pas modifier à la main.

## 12f. Poste : `reprise.json` (format 1)

`%AppData%\LuXia\reprise.json` (GEN-095, MOT-102) : scènes qui jouent, masters, Grand Master, blackout, figé, écrit
toutes les 5 s s'il a changé ; marqué « arrêt propre » à la fermeture. Après un arrêt brutal, LuXia propose de reprendre.

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

Verbes ajoutés en P5 : `flash "scène" appui|relache` (CMD-014), `figer oui|non [suspendre]` (CMD-003), `fumee appui|relache`
ou `fumee rafale 3` (CMD-030), `canal 180 255` (surcharge brute de l'univers 1, pour éprouver les limites de sûreté),
`liberer-canaux`, `arreter-couche "couche"`, `tout-arreter tout` (sans « tout », les couches protégées continuent).
Chaque intervention d'une limite de sûreté apparaît dans le résumé (`⚠ sûreté : …`, MOT-083).

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
| 2026-09-28 | `compactScenes` des préférences (ERG-025) ; capture d'un look sans `grandMaster` (C13). |
| 2026-09-28 | `looks.json` (ERG-023) ; `uiScale` des préférences (F8) ; `spectacle.json` à côté de `controle.json` (dispositions de l'écran Contrôle). |
| 2026-09-28 | Chantier ergonomique : `allowed` des zones (zone permise, F7) ; disposition des panneaux de l'écran Contrôle dans `%AppData%\LuXia\dispositions\controle.json` (enveloppe `formatVersion` 1 autour du texte de la bibliothèque Dock, propre au poste). |
| 2026-09-27 | P5 : `sûreté.json`, `live.json`, `midi.json` (+ schémas), `forbiddenZones` des lieux, `venueId` des palettes, propriétés `kind`, `keepOnStopAll`, `restSceneId`, `families` des couches, dossier `Versions`, `reprise.json`, verbes de scénario. |
