# SORT-065 – Enregistrement des trames visible, chemin copiable

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | M |
| **Phase** | P5 |
| **Source** | [doc 10 – 7. Pilotes Simulateur, Enregistreur, Art-Net](../10-sortie-dmx-et-firmware.md) |
| **Remarque** | — |
| **Liens** | SORT-060, SORT-061 |

## Description

> Enregistrement **visible** : voyant rouge clignotant et « Enregistrement en cours » à l'écran Sorties, voyant « REC » dans la barre d'état sur tous les écrans ; bouton « Arrêter l'enregistrement » ; chemin du fichier **sélectionnable** et bouton « Copier le chemin ».

**Critère d'acceptation** : Revue (demande utilisateur, essai P5).

## Réalisation

- `src/Luxia.UI.Modules.Outputs/OutputsView.axaml`, `OutputsView.axaml.cs`, `OutputsViewModel.cs`
- `src/Luxia.App/MainWindow.axaml` (voyant REC de la barre d'état)
- `src/Luxia.App/App.axaml` (style `Ellipse.rec` clignotant)

## Tests

- —

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 10, 7. Pilotes Simulateur, Enregistreur, Art-Net). |
| 2026-09-27 | Utilisateur | Question | Essai P5 : « Il faut que tu me permettes de copier le texte ici… Enregistrement en cours, on a juste le bouton allumé en bleu, pas clair du tout. Un enregistrement en cours = un point rouge clignotant. Tu peux mettre quelque part « Enregistrement en cours » et ce voyant qui clignote ? » |
| 2026-09-27 | Claude | Création | Exigence SORT-065 ajoutée au doc 10 (demande en session, doc 03 §10). |
| 2026-09-27 | Claude | Développement | Écran Sorties : bouton « ● Enregistrer les trames » / « ■ Arrêter l'enregistrement », point rouge clignotant et « Enregistrement en cours » ; chemin sélectionnable et bouton « Copier le chemin ». Barre d'état : voyant « REC » clignotant sur tous les écrans pendant l'enregistrement. |
