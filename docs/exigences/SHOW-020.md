# SHOW-020 – Éditeur graphique

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P8 |
| **Source** | [doc 20 – 3.5 Exigences](../20-show-et-sequences.md) |
| **Remarque** | Éditeur par cartes et diagramme dessiné automatiquement (Q45) ; combinaisons ET / OU / NON affichées, modifiées dans le fichier. |
| **Liens** | — |

## Description

> Éditeur graphique : poser des étapes, des transitions, les relier ; divergences / convergences OU et ET ; macro-étapes.

**Critère d'acceptation** : Construire l'exemple du §3.4.

## Réalisation

- src/Luxia.Show/Model/ShowDefinition.cs
- src/Luxia.Show/Runtime/ShowRun.cs
- src/Luxia.UI.Modules.Control/Sequencing/ShowEditorViewModel.cs
- src/Luxia.UI.Modules.Control/Sequencing/StepCardViewModel.cs
- src/Luxia.UI.Modules.Control/Views/ShowDiagram.cs
- src/Luxia.UI.Modules.Control/Views/ShowEditorWindow.axaml

## Tests

- ShowExecutionTests.AndDivergenceAndConvergence_RunBranchesInParallel_ThenJoin
- ShowExecutionTests.MacroStep_WaitsForItsSubShowToEnd
- SequencingScreensTests.ShowEditor_AddStepAndTransition_RenameStep_Validate
- SequencingScreensTests.Lock_RefusesTheEditors
- SequencingScreensTests.BothEditorsOpen_KeepTheirOwnDrafts
- SequencingScreensTests.ReopeningTheProject_AbandonsTheDraft
- ShowExecutionTests.MacroStep_KeepsAScene_ThatItsSubShowAlsoPlays

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 20, 3.5 Exigences). |
| 2026-10-02 | Claude | Développement | P8 lots 1, 2 et 4 : maquette 12 soumise ; `shows.json` (doc 50 §12h) ; l'exemple du doc 20 §3.4 est écrit dans le show de référence (*Couplet / Refrain / Drop*, `722853b`). |
| 2026-10-02 | Claude | Développement | P8 lot 5 (`4ceb412`) : cartes (identifiant, nom, initiale, macro-étape, tirage au sort, actions, transitions avec étapes amont et aval — plusieurs = ET —, condition, quantification, poids, priorité) ; diagramme en rangées depuis les étapes initiales, retours en pointillés ; l'exemple du §3.4 se construit (show de référence). Écart : les réceptivités combinées se modifient dans le fichier. |
| 2026-10-02 | Claude | Développement | Relecture : les fenêtres de séquence et de show ouvertes ensemble effaçaient le brouillon l'une de l'autre dans les séquenceurs (`SetSequenceDraft` / `SetShowDraft` séparés) ; un projet rouvert pendant l'édition abandonne le brouillon (il ne peut plus être écrit dans un autre projet) ; identifiant d'étape, étapes amont et aval écrits en quittant le champ (la carte se reconstruisait à chaque lettre et le champ perdait le curseur). |
| 2026-10-02 | Claude | Développement | Seconde relecture : quand une macro-étape démarrait, le show parent arrêtait les scènes que son sous-show venait de reprendre (« voulue ailleurs » ignorait les sous-shows du parent) ; corrigé et testé. Exemples ajoutés au show de référence : *Branches parallèles et macro-étape* (divergence et convergence en ET, macro-étape) et son sous-show *Bloc refrain (macro-étape)*, documentés au guide P8 (catalogue §1, exemple 8). |
