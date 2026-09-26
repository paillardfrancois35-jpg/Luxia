# INST-034 – Sélection de cellules

| Champ | Valeur |
|---|---|
| **Statut** | Non réalisé |
| **Priorité** | M |
| **Phase** | P3 |
| **Source** | [doc 13 – 4. Sélections](../13-installation-et-lieux.md) |
| **Remarque** | Le modèle (`SelectionItem.Cell`) et le décodeur (cellules, doc 12 §2.6) existent ; seule la sélection de cellules individuelles dans l'écran manque. |
| **Liens** | INST-030 |

## Description

> Une sélection peut contenir des **cellules** (ex. les 8 segments de deux barres = 16 cellules ordonnées).

**Critère d'acceptation** : Chenillard sur 16 segments.

## Réalisation

- Non fait côté écran. `src/Dmx.Patch/Model/SelectionItem.cs` porte déjà un numéro de cellule (0 = appareil entier) ; `FixtureDecoder` décode les cellules séparément (utilisé par le simulateur).

## Tests

- —

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §4). |
| 2026-09-26 | Claude | Décision | Reporté : la case à cocher du patch (onglet « Univers et patch ») ne coche que l'appareil entier ; sélectionner un segment précis d'une barre demande une petite UI par cellule (liste dépliable ou clic sur le plan). Peu utile avant les effets par cellule (phase, doc 16 §6, P6) : reporté à ce moment, sans bloquer P3 (barres pilotables en entier dès maintenant). |
