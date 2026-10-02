# SHOW-029 – Variables simples du show

| Champ | Valeur |
|---|---|
| **Statut** | Validé |
| **Priorité** | M |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Variables numériques du show, modifiées par action, comparées dans les conditions. |
| **Liens** | — |

## Description

> Variables simples du show (compteurs : « nombre de refrains joués ») utilisables dans les conditions.

**Critère d'acceptation** : Après 3 refrains → variante finale.

## Réalisation

- src/Luxia.Show/Runtime/ShowRun.cs

## Tests

- ShowExecutionTests.Variables_CountChoruses_ThenTheFinalVariant

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lot 4 (`d46142e`). |
| 2026-10-02 | Utilisateur | Test | Ex. 12 et 13 : Final au 3e refrain ✅ ; compteur remis à zéro à l'Intro (correction de l'ex. 13). |
| 2026-10-02 | Utilisateur | Test | Re-vérification v1.010.104, ex. 13 : compteur de refrains remis à zéro à l'Intro (le morceau suivant repart au couplet) ✅. |
| 2026-10-02 | Utilisateur | Validation | Validé avec la phase P8 (v1.010, essai v1.010.079 → .104, contrôle final v1.010.116). ex. 12, 13. |
