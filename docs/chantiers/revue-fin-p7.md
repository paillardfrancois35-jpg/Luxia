# Revue globale de fin de phase – P7 « Audio et tempo »

> Faite le 2026-10-02, avant la fusion de `p7/audio-tempo` dans `main`, à la demande de l'utilisateur : relecture de la documentation,
> du code et des fiches d'exigences. Elle reprend les pré-analyses du 2026-10-01 ([code](analyse-code-p7.md),
> [documentation](analyse-docs-p7.md), [ergonomie](analyse-ergonomique-p7.md) §10) et dit, ligne par ligne, ce qui est fait, décidé ou
> rangé. Outil réutilisable : `python tools/audit-documentation.py` (liens, fiches, tests et fichiers cités).

## 1. Chiffres

| Mesure | Valeur |
|---|---|
| Tests (hors matériel) | **886**, tous verts (847 à la pré-analyse) ; dont, pour P7, 41 tests d'écoute et d'analyse, l'horloge et la réactivité musicale (moteur), le bloc BPM et l'écran Audio (interface) et les trames de référence |
| Avertissements de compilation | 0 ; `dotnet format --verify-no-changes` propre ; analyseurs de code mort (IDE0051, 0052, 0059, 0060) : 1 signalement d'une signature d'événement, conservé |
| Dépôt | 177 fichiers modifiés depuis `main`, + 11 600 lignes |
| Fiches d'exigences | 449 (6 créées pour P7 : GEN-024, GEN-026, LIVE-020, LIVE-021, SCN-006, SIM-011 ; l'audit n'en trouve plus de manquante) ; 380 problèmes de renvois trouvés par l'audit, tous corrigés sauf 3 faux positifs connus |
| Gros fichiers | `RenderEngine.cs` 1 407 (+ `RenderEngine.Tempo.cs` 92), `Playback.cs` 1 070 (+ `Playback.Rhythm.cs` 87) |

## 2. Documentation

| Contrôle | Résultat |
|---|---|
| Liens relatifs de tous les documents | ✅ 1 faux positif (accent du nom de dossier), 1 modèle (`../NN-document.md`) |
| Fiches : fichiers et tests cités existent | ✅ 134 fiches anciennes pointaient des projets `Dmx.*` (renommés `Luxia.*`) : corrigé ; 7 noms de tests ou fichiers renommés : corrigés |
| Fiches : historique chronologique, entrée de développement, statut Validé avec sa validation | ✅ sauf GEN-030 et GEN-031 (deux dates hors ordre en 2026-09, historique en ajout seul : laissé) ; LIVE-040 : entrée de validation ajoutée |
| Fiches manquantes de P7 | ✅ GEN-024, GEN-026, LIVE-020, LIVE-021, SCN-006, SIM-011 créées |
| Matrice [31](../31-matrice-exigences.md) et index des fiches | ✅ régénérés (toutes les phases) |
| Glossaire | ✅ trié alphabétiquement, termes de P7 expliqués (break, drop, kick, beat, montée, fréquence, latence) |
| Rapport chiffré [P7-audio-rapport.md](../essais/P7-audio-rapport.md) | ✅ troisième édition (21 tempos sur 25 justes ; impulsions et part sur les temps mesurées avec l'algorithme final) et tableau des références avec leurs sources (D17) |

### Écarts de l'analyse de la documentation (D1 à D19)

| # | État | Remarque |
|---|---|---|
| D1 à D4 | ✅ | Doc 19 : break dès 1 mesure, règle du « 1 ici », tap sans option « corrige l'audio », temps morts globaux |
| D5, D6 | ✅ | Doc 02 aligné (bornes 20-400, plage d'exploration) ; fiches GEN-024 et GEN-026 créées |
| D7 | ✅ | Doc 16 §5 : fréquence ×4 à ÷ 8 et périodes d'effet |
| D8 | ✅ | Doc 02 : CMD-042 corrigé, CMD-043 (latence) ajoutée, `Immediate` documenté |
| D9 | 🔎 | EVT-020, 021, 024 restent « Partiel » : à décider avec le séquenceur (P8) |
| D10 | ✅ | Bloc `audio` de `preferences.json` documenté (fait avant l'essai) |
| D11 | ✅ | Doc 19 §10 : algorithme final des impulsions, plancher, limite des événements |
| D12 | ✅ | Doc 60 §4.10 |
| D13 | ✅ | Doc 03 §11 : huit pièges de P7 |
| D14 | ✅ | Doc 30 §7.1 : correspondance T-AUD ↔ tests réels |
| D15 | ✅ | Doc 32 : nombre de tests, « À faire pour valider P7 » |
| D16 | ✅ | Guide P7 : autorisation du micro |
| D17 | ✅ | Références et sources dans le rapport |
| D18 | ✅ | Doc 99 : idées ajoutées (tap corrige l'audio, premier temps auto / manuel, badge ♪, plus les idées de l'essai) |
| D19 | ✅ | Limite des durées musicales sur GEN-023, MOT-016 et doc 16 |

## 3. Code

### Lignes de l'analyse de code (C1 à C17)

| # | État | Remarque |
|---|---|---|
| C1 | ✅ | « 1 ici » prime sur l'analyse |
| C2 | ⏳ | Politique du premier temps automatique / manuel : au doc 99 (à rediscuter avec des mesures) |
| C3 | ✅ | Interblocage à l'arrêt : source libérée hors verrou, hors fil de capture (renforcé par le plantage 1.009.090) |
| C4 | ✅ | Délégué d'événement mis en cache (plus d'allocation par trame) ; publication à 40 Hz déjà en place |
| C5 | ⏳ | Constantes dispersées de l'analyse : à regrouper à la prochaine évolution de l'algorithme (valeurs documentées au doc 19 §10) |
| C6 | ✅ | `Playback.Clock` et `Events` en `required` |
| C7 | ⏳ | Phase de l'aperçu non calée sur le direct (E17 au doc 99) |
| C8 | ✅ | Chemins non testés : `PcmMixer` extrait et testé, analyse de fichier testée, quantification (arrêt de couche, tout arrêter, rechargement), liaisons du bloc BPM ; **reste** : endurance 6 h (T-AUD-06, avec P10) |
| C9 | ✅ | `TODO` remplacé par une limite écrite (code, doc 15, fiche MOT-016) : plus aucun `TODO` dans le dépôt |
| C10 | ✅ | `RenderEngine.Tempo.cs` et `Playback.Rhythm.cs` (classes partielles) |
| C11 | ⏳ | Clones d'instantané : à mesurer avant (la charge reste faible) |
| C12 | ✅ | Reconnexion à 2, 5 puis 10 s |
| C13 | — | Ancien écran Scènes : ne pas investir |
| C14 | ✅ | Message d'autorisation du micro |
| C15, C16 | ✅ | Compatibilité des fichiers ; démarrage |
| C17 | ⏳ | **En tête de P8** : durées en s / temps / mesures dans les Propriétés (E2) |

### Relecture faite pendant la revue

- Code mort : membres inutilisés trouvés par les analyseurs et par une recherche des références (`FrameAnalyzer.Hop`, `Playback.Kill`, `BarText`,
  voyants de l'écran Audio devenus inutiles) : retirés. Restent, signalés sans suppression car ils appartiennent à d'autres phases :
  `TickLoop.IsRunning`, `LuxiaRuntime.StopAllScenes`, `TestOutputCommand.DefaultExcludedChannels`.
- Code d'essai laissé en production : variable d'environnement `LUXIA_SENS` retirée de `AudioFileAnalysis`.
- Robustesse (trouvée par les essais) : exceptions dans les timers de l'écoute, accès COM après libération, abonné défaillant, arrêt depuis le fil
  de capture, liste d'événements plafonnée, état de saisie du BPM : corrigés et testés (doc 03 §11).
- Commentaires : chaque type et membre publics ont leur commentaire XML (le projet ne compile pas sans) ; les décisions non évidentes (`CheckDefaultOutput`,
  `Guard`, phase attendue après ×2, claquement du kick) sont expliquées à l'endroit du code, en français.

## 4. Points à décider ou à garder en tête

1. **Statuts des fiches de P7** : 33 fiches « Réalisé, à valider sur matériel » dont 16 de P7 ; l'essai de l'utilisateur les a vérifiées. À la validation : « Validé » avec
   une entrée « Validation » pour celles que l'essai a couvertes.
2. **Exigences prioritaires encore partielles** (AUD-024 premier temps ≈ 50 %, SCN-050 réglages par effet, EVT-020 / 021 / 024 sans événement sur le bus) : le doc 30 §7 veut
   « toutes les exigences I validées » pour clore une phase. Proposition : les garder « Partiel » avec leur limite écrite, reportées à P8 (séquenceur), et le dire à la validation.
3. **Détection des kicks** : réglages à préserver (« le jour et la nuit », essai 1.009.090) : toute évolution de l'algorithme doit rejouer `audio-diag` et
   le rapport chiffré, et le test `Pulses_BassGuitarNotes_AreNotKicks_ButDrumKicksAre`.
