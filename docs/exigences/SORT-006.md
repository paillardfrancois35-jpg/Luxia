# SORT-006 – Configuration des sorties dans les préférences du poste

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P0 |
| **Source** | [doc 10 – 3. Exigences – routage et supervision (côté PC)](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | Q15 |

## Description

> La configuration des sorties (univers → pilotes, paramètres) est enregistrée dans les **préférences du poste**, pas dans le projet (un projet doit s'ouvrir sur un autre PC avec un autre port).

**Critère d'acceptation** : Ouvrir le projet sur un autre PC : pas d'erreur, sortie à configurer ou détectée.

## Réalisation

- `src/Luxia.Core/Settings/Preferences.cs`

## Tests

- `PreferencesAndProjectTests.Preferences_Missing_GivesDefaults`
- `PreferencesAndProjectTests.Preferences_Update_IsPersisted`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 3. Exigences – routage et supervision (côté PC)). |
| 2026-09-24 | Claude | Question | Q15 : le doc 41 §11 mettait la configuration de sortie dans le show de référence, en contradiction avec SORT-006. |
| 2026-09-24 | Utilisateur | Réponse | Utilisateur : SORT-006 fait foi ; doc 41 §11 corrigé. |
| 2026-09-24 | Claude | Développement | `c0cce3e` feat(sortie): routeur, pilotes Nul / Enregistreur / Arduino, protocole Enttec |
| 2026-09-24 | Claude | Développement | `5271ed9` feat(persistance): fichiers JSON versionnés, migrations, préférences, projet |
