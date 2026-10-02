# SHOW-024 – Validation à l'édition

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | valider ; refus au lancement d'un show fautif ; problèmes affichés en direct sous la frise ou le diagramme de l'éditeur. |
| **Liens** | — |

## Description

> **Validation** à l'édition : étape initiale absente, étape inatteignable, transition sans condition dans une boucle (risque de boucle infinie dans un même tick → interdit), scène référencée supprimée.

**Critère d'acceptation** : Jeu de shows invalides → erreurs attendues.

## Réalisation

- src/Luxia.Show/Rules/ShowRules.cs
- src/Luxia.Hosting/Tools/ProjectValidator.cs
- src/Luxia.UI.Modules.Control/Sequencing/DraftEditorViewModel.cs (`Issues`)

## Tests

- ShowRulesTests.Show_WithoutInitialStep_IsAnError
- ShowRulesTests.LoopOfImmediateTransitions_IsAnError_ButAQuantizedOneIsNot
- ShowRulesTests.DeletedSceneAndUnknownReferences_AreErrors
- ShowRulesTests.MacroStepThatContainsItself_IsAnError
- ShowExecutionTests.ShowWithAnImmediateLoop_IsRefusedAtLaunch
- ReferenceShowP8Tests.Pitfall_IsRefused_AndNothingLights

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lots 2 et 4 (`683cfc8`, `722853b`) : étape inatteignable en avertissement (le show joue quand même). |
| 2026-10-02 | Claude | Développement | P8 lot 5 (`4ceb412`) : l'éditeur valide le brouillon à chaque geste (⛔ erreur, ⚠ avertissement). |
