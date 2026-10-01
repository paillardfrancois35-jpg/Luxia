# INST-019 – Identifier un appareil / chenillard d'identification

| Champ | Valeur |
|---|---|
| **Statut** | Réalisé |
| **Priorité** | I |
| **Phase** | P3 |
| **Source** | [doc 13 – 3. Patch des appareils](../13-installation-et-lieux.md) |
| **Remarque** | Avancée manuelle (bouton « Suivant ») ; l'avancée automatique minutée (proposée en variante par le texte de l'exigence) n'est pas faite. |
| **Liens** | CMD-023, CONS-024, SIM-009 |

## Description

> Bouton **Identifier** par appareil (CMD-023) et **Identifier tout à la suite** (chenillard d'identification, un appareil à la fois, avancée manuelle ou automatique).

**Critère d'acceptation** : Chenillard d'identification sur le parc.

## Réalisation

- `src/Luxia.UI.Modules.Installation/InstallationViewModel.cs` : `ToggleIdentifyFixture` (par appareil), `IdentifyNextCommand` (chenillard, avance manuelle dans l'ordre du patch).
- `src/Luxia.UI.Modules.Installation/InstallationView.axaml` : bouton Identifier par ligne.

## Tests

- `InstallationViewModelTests.Identify_LightsIntensityChannel_AndChaseAdvancesToNextFixture`

## Historique

| Date | Par | Type | Entrée |
|---|---|---|---|
| 2026-09-24 | Conception | Création | Exigence rédigée au cahier des charges (doc 13, §3). |
| 2026-09-26 | Claude | Décision | Avancée manuelle seulement (bouton « Suivant ») : l'avancée automatique minutée n'apporte rien de plus pour la mise au point (l'utilisateur regarde l'appareil, pas un chronomètre) et complique l'état de l'écran. Reconsidérable si demandé à l'usage. |
| 2026-09-26 | Claude | Développement | `389fe64` feat(installation): écran Installation — patch, sélections, lieux (doc 13) |
| 2026-09-26 | Claude | Correction | L'utilisateur a testé « Identifier » sur un PAR réel (adresse 1) : rien ne semblait se passer. Deux causes trouvées : (1) `ToggleIdentifyFixture` appelait `StopIdentify()` (qui remet l'état à zéro) avant de tester si c'était le même appareil qui était déjà identifié — un second clic relançait donc au lieu d'arrêter ; corrigé en capturant l'état avant l'arrêt. (2) Confirmé par un second essai en direct : le gradateur seul ne suffit pas sur ce PAR RVB, il faut aussi les canaux couleur. Voir CMD-023 pour le correctif (`IdentifyRules`). |
