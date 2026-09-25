# CONS-092 – Survol immédiat du moniteur (numéro de canal + cadre)

| Champ | Valeur |
|---|---|
| **Statut** | À faire |
| **Priorité** | S |
| **Phase** | P3 |
| **Source** | Test utilisateur du guide P1 (exemple 4), 2026-09-25 |
| **Remarque** | Extension de CONS-043 (moniteur de sortie), en même temps que la délimitation par appareil. |
| **Liens** | CONS-043 |

## Description

> Sur le moniteur de sortie, les cases sont trop petites et nombreuses pour repérer celle survolée. Il faut afficher le
> **numéro de canal immédiatement** au survol (pas en info-bulle standard, qui met ~1 s à apparaître) et **encadrer** la case
> survolée.

**Critère d'acceptation** : survoler une case du moniteur affiche aussitôt son numéro (et sa valeur si « Valeurs » est coché) ;
la case survolée est visuellement encadrée.

## Réalisation

- —

## Tests

- Aucun test automatique : à écrire avec le développement.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-25 | Utilisateur | Test | Guide P1, exemple 4 (CONS-043) : pas d'info-bulle constatée (ou trop lente) ; demande d'un numéro de canal immédiat au survol et d'un cadre sur la case. Non bloquant. |
| 2026-09-25 | Claude | Création | Exigence CONS-092 rédigée à partir du retour utilisateur ; reportée en P3, avec la délimitation par appareil déjà prévue pour CONS-043. |
