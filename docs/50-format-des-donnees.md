# 50 – Format des données

> Documentation des fichiers lus et écrits par l'application (GEN-050, GEN-051, GEN-130), tenue à jour **au fil du développement**.
> Destinée à l'utilisateur comme à une IA de conception (doc 02 §17b). Schémas JSON vérifiables : à venir avec GEN-131 (P4).

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
| Préférences du poste | `%AppData%\DMX\preferences.json` |
| Projets | `Documents\DMX\Projets\<nom>\` (un dossier par projet) |
| Bibliothèque d'appareils | `Documents\DMX\Bibliothèque\<fabricant>\<modèle>.json` (P2) |
| Journaux | `Documents\DMX\Journaux\technique-AAAAMMJJ.log` |
| Enregistrements de trames | `Documents\DMX\Enregistrements\*.dmxrec` |

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

## 5. Enregistrement de trames `.dmxrec` (format binaire 1)

Fichier binaire compact (SORT-060), petit-boutiste.

| Partie | Contenu |
|---|---|
| En-tête (24 octets) | `"DMXREC"` (6 octets ASCII) · version `uint16` · univers `uint16` · fréquence `float32` (Hz) · début `int64` (ticks .NET UTC) · nombre de canaux `uint16` |
| Trame (répétée) | temps écoulé depuis la première trame en ms `uint32` · longueur `uint16` · valeurs (longueur octets) |

Une **longueur 0** signifie « trame identique à la précédente » : un univers au repos ne coûte que 6 octets par trame.
Une fin de fichier tronquée (arrêt brutal) est tolérée à la lecture : seules les trames complètes sont relues.
Lecture : `dmx-headless relire fichier.dmxrec`.

## 6. Historique

| Date | Modification |
|---|---|
| 2026-09-24 | P0 : règles communes, `preferences.json`, `projet.json`, `.dmxrec`. |
