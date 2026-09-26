# CONS-091 – Écart conservé au-delà des bornes en déplacement relatif multiple

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P4 |
| **Source** | Test utilisateur du guide P1 (exemple 3), 2026-09-25 |
| **Remarque** | Valeur virtuelle par fader sélectionné, gardée à travers plusieurs glissés, oubliée au changement de sélection et à la libération. |
| **Liens** | CONS-006 |

## Description

> En déplacement **relatif** d'une sélection multiple, si un des faders atteint 0 ou 255 avant les autres (ex. sélection à 55, 105, 155 ;
> on monte jusqu'à 155, 205, 255 : le 3ᵉ canal est à sa borne), un dépassement du geste (faux mouvement de souris) fait perdre
> l'écart d'origine entre les canaux : en rebaissant, le 3ᵉ canal redescend immédiatement avec les autres au lieu de rester
> à 255 tant que les autres n'ont pas rattrapé son écart.

**Comportement attendu** : chaque fader de la sélection garde un accumulateur **virtuel** (peut dépasser 0-255, jamais émis tel quel),
qui mémorise l'écart d'origine avec les autres faders de la sélection. La valeur émise est le virtuel **borné** à 0-255.
En rebaissant après un dépassement, un fader resterait à sa borne tant que son virtuel n'est pas revenu dans 0-255, puis
repartirait avec l'écart d'origine intact. Cet accumulateur se conserve tant que la sélection reste active, y compris à travers
plusieurs relâchers de souris (un nouveau glissé continue d'accumuler sur la sélection en cours) ; il est réinitialisé aux
valeurs réelles seulement à la libération de la sélection (Échap, Désélectionner, Tout libérer, changement de sélection).
Vaut aussi en dépassement négatif (sous 0).

**Critère d'acceptation** : sélection à (55, 105, 155), écarts +50 entre chaque. Monter jusqu'à ce que le 3ᵉ atteigne 255
(à 155, 205, 255), continuer à monter (255 reste affiché), puis rebaisser : le 3ᵉ doit rester à 255 jusqu'à ce que les
écarts soient rattrapés, puis redescendre avec les autres en conservant +50 entre chaque, jusqu'à revenir à (55, 105, 155)
si le geste inverse exact est refait.

## Réalisation

- —

## Tests

- Aucun test automatique : à écrire avec le développement.

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-25 | Utilisateur | Test | Guide P1, exemple 3 (CONS-006) : sélection multiple en relatif, comportement à revoir décrit ci-dessus. Non bloquant, reporté à une phase ultérieure. |
| 2026-09-25 | Claude | Création | Exigence CONS-091 rédigée à partir du retour utilisateur ; reportée en P4 (retouches sur la Console après P3). |
| 2026-09-26 | Claude | Développement | `7049044` feat(console): surcharges d'attributs en mode appareils, écart conservé en relatif, origine au survol |
| 2026-09-26 | Claude | Test | Critère d'acceptation rejoué à l'identique : `MultiSelection_Relative_KeepsGapsBeyondBounds_AcrossSeveralDrags`. |
