# CONS-092 – Survol immédiat du moniteur (numéro de canal + cadre)

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | S |
| **Phase** | P3 |
| **Source** | Test utilisateur du guide P1 (exemple 4), 2026-09-25 |
| **Remarque** | — |
| **Liens** | CONS-043 |

## Description

> Sur le moniteur de sortie, les cases sont trop petites et nombreuses pour repérer celle survolée. Il faut afficher le
> **numéro de canal immédiatement** au survol (pas en info-bulle standard, qui met ~1 s à apparaître) et **encadrer** la case
> survolée.

**Critère d'acceptation** : survoler une case du moniteur affiche aussitôt son numéro (et sa valeur si « Valeurs » est coché) ;
la case survolée est visuellement encadrée.

## Réalisation

- `src/Dmx.UI.Controls/OutputMonitor.cs` : `InvalidateVisual()` au changement de case survolée, cadre dessiné aussitôt (`HoverPen`). Le numéro de canal + la valeur étaient déjà affichés sans délai (`HoverText`, hors info-bulle standard) : seul le cadre manquait.

## Tests

- Aucun test automatique (rendu visuel) : vérification par le guide de démonstration ou sur le matériel.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-25 | Utilisateur | Test | Guide P1, exemple 4 (CONS-043) : pas d'info-bulle constatée (ou trop lente) ; demande d'un numéro de canal immédiat au survol et d'un cadre sur la case. Non bloquant. |
| 2026-09-25 | Claude | Création | Exigence CONS-092 rédigée à partir du retour utilisateur ; reportée en P3, avec la délimitation par appareil déjà prévue pour CONS-043. |
| 2026-09-26 | Claude | Note | Le numéro de canal et la valeur étaient déjà immédiats (`HoverText` lié directement, jamais un `ToolTip` standard) : seul le cadre manquait pour repérer la case parmi les 512. |
| 2026-09-26 | Claude | Développement | `5d60230` feat(console): mode appareils (CONS-020 à 024), CONS-007, CONS-043, CONS-092 |
