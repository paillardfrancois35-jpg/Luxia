# Pré-analyse de code – P7 « Audio et tempo »

> Rédigée le 2026-10-01, **avant** l'essai de l'utilisateur, pour être reprise et complétée à la fin de la phase (colonne
> « Suite »). Méthode : métriques du dépôt, relecture des modules ajoutés ou touchés (`Luxia.Audio`, `MusicalClock`,
> `Playback`, `RenderEngine`, écran Audio, bloc BPM), essais de robustesse sur le matériel, recherche des chemins non
> testés. Statuts : ✅ corrigé dans cette passe · ⏳ recommandé · 🔎 à surveiller à l'essai.
> Documents sœurs : [analyse de la documentation](analyse-docs-p7.md), [analyse ergonomique](analyse-ergonomique-p7.md).

## 1. Constat global

| Mesure | Valeur |
|---|---|
| Ajouts de la phase (depuis `main`) | 16 commits, 151 fichiers, +8 600 lignes (dont docs, fiches, scènes) ; projets `Luxia.Audio` (≈ 1 900 lignes) et `Luxia.UI.Modules.Audio` (≈ 600) |
| Tests | 847 (hors matériel), tous verts ; P7 : ≈ 68 (audio 21, horloge 18, réactivité 12, bloc BPM 6, écran Audio 5, contenu 6) + 1 essai sur matériel |
| Avertissements de compilation | 0 ; `dotnet format --verify-no-changes` propre |
| `TODO` | 1 : `ShowCompiler.cs:322` (MOT-016, retard et répartition en unités mixtes) — voir C9 |
| Cadence moteur | test de performance inchangé (100 appareils, 20 couches, 40 lectures < 5 ms, sans allocation) : l'horloge, la quantification et la lecture audio n'allouent rien par tick |
| Analyse audio | < 5 % d'un cœur en temps réel ; ≈ 15 s par morceau hors ligne (décodage MP3 compris) |
| Gros fichiers | `RenderEngine.cs` 1 487 lignes (+ 170), `Playback.cs` 1 133 (+ 130), `LuxiaRuntime.cs` 799 (+ 70) |

## 2. Défauts trouvés et correctifs

| # | Gravité | Constat | Suite |
|---|---|---|---|
| C1 | **Haute** | **« 1 ici » était annulé par l'écoute.** En source Audio, l'horloge se range sur le premier temps deviné par l'analyse dès qu'il diffère d'une seconde ; le recalage manuel de l'utilisateur était donc écrasé au bout d'une seconde (le premier temps n'est juste qu'à environ 50 %). Aurait fait échouer l'exemple 3 de l'essai | ✅ `MusicalClock` : le « 1 » posé à la main prime jusqu'au prochain morceau (tempo qui saute de plus de 6 %) ou au changement de source ; 2 tests (`ManualBarResync_IsNotOverruledByTheGuessedDownbeat_UntilANewSong`, `Audio_Downbeat_ThatDisagreesForASecond_MovesTheBarPosition`) |
| C2 | Moyenne | **Politique du premier temps trop confiante.** Sans recalage manuel, l'horloge avance d'un à trois temps chaque fois que l'analyse contredit l'horloge plus d'une seconde. Comme l'analyse se trompe une fois sur deux, une scène « à chaque mesure » peut **changer de temps fort en plein morceau** ; chaque saut déplace aussi la phase des effets calés (`ShiftTotal`) | ⏳ à décider après l'essai (ex. 6) : exiger plusieurs mesures d'accord et une confiance, ou proposer « Premier temps : automatique / manuel » (défaut manuel). **Ajouter un compteur et une ligne de journal technique à chaque correction** pour mesurer sur de vrais morceaux ; 🔎 |
| C3 | Moyenne | **Risque d'interblocage à l'arrêt de la capture.** `AudioListener.Stop`, `SetDevice` et `OnStopped` libèrent la source **en tenant le verrou** `_gate` ; le fil de capture NAudio, pendant ce temps, peut attendre ce même verrou dans `OnBlock`. Si `Dispose` attend la fin du fil, les deux se bloquent | ✅ vérifié sur matériel : 40 cycles démarrage / arrêt pendant qu'un son joue, sans blocage (`AudioListenerHardwareTests`, catégorie Matériel). ⏳ refactoriser (sortir la source du verrou avant `StopCapture` / `Dispose`) si un blocage est vu à l'essai (exemples 15 et 28) |
| C4 | Basse | **Allocations dans le chemin audio** : une fermeture par trame (`e => EventRaised?.Invoke(e)` dans `AudioAnalyzer.OnFrame`, 172 fois par seconde), deux enregistrements par bloc (`AudioListener.Publish`, ≈ 100 par seconde), `RecentEvents.ToArray()` à chaque rafraîchissement de l'écran Audio | ⏳ délégué mis en cache, publication limitée à 40 Hz ; sans effet mesurable aujourd'hui (charge < 5 %), à faire avec C5 |
| C5 | Basse | **Constantes dispersées et couplées** : 172 trames par seconde codé en dur dans `EnergyTracker.Track`, seuils d'énergie, de break, de montée, poids (0,55 / 0,25 / 0,20), plancher d'impulsion (0,1), temps morts par défaut | ⏳ regrouper dans une classe de constantes documentée (ou dans `AudioTuning` pour ce qui se règle), avec la fréquence de trame fournie par l'analyseur |
| C6 | Basse | `Playback.Clock` et `Playback.Events` ont un initialiseur par défaut (`= new()`) : chaque lecture crée une horloge et une liste inutiles avant d'être remplacées (par lancement, pas par tick) | ⏳ propriétés `required` |
| C7 | Basse | **Moteur d'aperçu** : `Preview.Bpm = Engine.Bpm` à chaque tick (chaque appel repasse l'aperçu en source Fixe et vide le tap) ; la **phase** de l'horloge d'aperçu n'est pas celle du direct : en édition, les effets en temps musicaux et les scènes quantifiées ne sont pas calés sur la musique | ⏳ recopier aussi la position ; en attendant, le dire dans l'aide de l'aperçu ; 🔎 |
| C8 | Moyenne | **Chemins non testés** (voir § 3) | ⏳ |
| C9 | Basse | `TODO` de `ShowCompiler.cs:322` : retards et répartitions exprimés dans une autre unité que la durée de l'étape ne suivent pas le tempo (MOT-016 réalisé pour l'étape et le fondu seulement) | ⏳ soit le réaliser (convertir au tempo courant à chaque tick, peu utilisé), soit le retirer et garder la limite écrite (fiche MOT-016, doc 15 §16) |
| C10 | Basse | **Taille de `RenderEngine` et `Playback`** : P7 y a ajouté trois responsabilités (horloge, quantification, lecture audio) ; déjà recommandé après « Contrôle 2 » | ⏳ classes partielles : `RenderEngine.Tempo.cs` (horloge, audio, quantification), `Playback.Rhythm.cs` (événements, horloge propre, recalcul) ; mécanique, sans risque |
| C11 | Basse | **Clones d'instantané** : à chaque rafraîchissement (20 fois par seconde) l'écran de jeu en demande trois (colonnes, dimmers, bloc BPM) et l'écran Audio un quatrième ; `Snapshot` copie tous les tableaux | ⏳ un instantané par rafraîchissement, partagé ; mesurer avant (le test de démarrage et le CPU de la barre d'état suffisent) |
| C12 | Basse | **Reconnexion sans temporisation croissante** : périphérique absent → nouvel essai toutes les 2 s, un avertissement journalisé à chaque fois (≈ 1 800 lignes par heure) | ⏳ 2, 5 puis 10 s, et un seul journal par changement d'état |
| C13 | Info | L'ancien écran Scènes (`SceneEditorViewModel`) n'offre pas les réglages « Au rythme » | Ne pas investir : l'écran doit disparaître (lot B de l'analyse ergonomique de « Contrôle 2 ») |
| C14 | Moyenne | **Autorisation du microphone** : Windows peut bloquer l'accès d'une application de bureau au micro (Paramètres → Confidentialité → Microphone → « Autoriser les applications de bureau ») ; l'erreur s'affichera comme « Écoute impossible » | ⏳ message explicite dans l'écran Audio et le guide (exemple 18) ; 🔎 |
| C15 | Info | **Compatibilité des fichiers** : les nouvelles options de scène (`advance`, `quantize`, `ownBpm`, `energySpeed`) sont omises quand elles valent leur défaut ; les anciens projets se chargent sans changement, format inchangé (version 1) | ✅ |
| C17 | **Haute (avant tout retrait d'écran)** | **Les durées musicales ne se saisissent plus que dans l'ancien écran Scènes.** La fenêtre d'édition (Propriétés, étapes) n'offre que des **secondes** pour le fondu et le maintien (`StepFadeSeconds`, `StepHoldSeconds`) ; le choix s / temps / mesures (`DurationField.Units`) n'existe que dans `ScenesView`. Seul le panneau Effets a une bascule « temps » pour la période. Si l'on retire Live et Scènes comme prévu, **MOT-016 et GEN-023 n'auraient plus d'interface** (le moteur, lui, les gère) | ⏳ porter le choix d'unité dans les Propriétés (fondu, maintien, fondu d'entrée et de sortie de la scène) **avant** le retrait de l'écran Scènes ; 🔎 confirmer à l'essai que l'utilisateur en a l'usage |
| C16 | Info | **Démarrage** : l'énumération des périphériques (0,6 à 1 s) et l'ouverture de la capture (0,4 s) étaient sur le chemin de démarrage | ✅ corrigé (voir doc 99, « suivi du temps de chargement ») |

## 3. Chemins non testés (C8), à couvrir en fin de phase

| Zone | Manque | Test proposé |
|---|---|---|
| `WasapiSource.Convert` | Conversion des échantillons (16, 24, 32 bits, flottant, plus de deux canaux) et moyenne des canaux : logique pure noyée dans la classe | L'extraire (`PcmMixer`) et la tester avec des tampons construits à la main |
| `AudioFileAnalysis` | Seulement exercée par l'outil en ligne de commande | Un test sur un WAV synthétique écrit dans un dossier temporaire |
| Moteur, quantification | Arrêt d'une **couche** ou « tout arrêter » pendant l'attente ; rechargement du projet avec un lancement en attente dont la scène a disparu | 2 tests dans `MusicalReactivityTests` |
| `Playback.RescaleForTempo` | Fondu musical, étape mixte (fondu en temps, maintien en secondes), retards propres à une valeur | 2 tests dans `MusicalClockTests` |
| Bloc BPM | Case « Écoute » et bouton « 🎧 Audio » avec une écoute simulée ; état « Audio » affiché | 2 tests dans `TempoBarTests` (la fabrique simulée existe dans `AudioViewModelTests`) |
| Volet « Au rythme » | Liaisons des cinq réglages de la fenêtre d'édition (aller-retour modèle ↔ champ) | 1 test dans `EditBenchPanelsTests` |
| Périphérique choisi puis débranché | Pas de test de l'état et de la reprise | 1 test dans `AudioListenerTests` (la source simulée sait lever `Stopped`) |
| Endurance | T-AUD-06 (6 h de capture continue : mémoire, décrochage) | Essai sur le matériel de 1 h avant validation, 6 h avec P10 (GEN-092) |
| Non-régression de l'analyse | Aucune trace du résultat par morceau d'une version à l'autre | Enregistrer le rapport en JSON et comparer (BPM, confiance, nombre d'événements) à chaque évolution de l'algorithme |

## 4. Qualité de l'algorithme (à rejouer avec les résultats d'essai)

| Sujet | Constat | Piste |
|---|---|---|
| Tempos | 21 sur 25 justes à ± 2 % ; échecs : octaves et 6/8 (*Perfect*, *Someone Like You*, *Blinding Lights*) | Proposer une **seconde hypothèse** de tempo (× ou ÷ 1,5, 2) en un clic ; historique de la soirée comme a priori |
| Premier temps | ≈ 50 % de temps « connu », non chiffré contre une vérité | Annoter le premier temps de 8 à 10 morceaux (`annotations.csv` a la colonne), chiffrer, puis améliorer (accent des accords, changements de phrase) |
| Convergence | Les morceaux à sections changeantes oscillent d'un pas (« non » dans la colonne) | Hystérésis sur la sortie du tempo (ne changer qu'après N estimations concordantes) ; réserve : réactivité au changement de morceau |
| Références de tempo | 16 sur 25 « de mémoire » | Les confirmer (songbpm, tunebat) ; facile, à faire avec l'utilisateur ou en ligne |
| Break, drop | Un break d'une mesure est plus sensible que le cahier des charges (D36) ; rap et pop à arrêts brefs : beaucoup de faux positifs (*HUMBLE* : 7) | Annoter les instants de 5 morceaux pour chiffrer (AUD-062 : ≥ 80 % à ± 1 temps non mesuré) ; ajouter une durée minimale réglable |
| Impulsions | 2 à 3 basses par seconde sur tous les morceaux : les notes graves comptent comme des attaques | Séparer « kick » et « basse tenue » (attaque plus courte) si l'essai le montre |

## 5. À rejouer à la fin de la phase

1. Mêmes mesures qu'au § 1 (lignes, tests, avertissements, cadence, CPU avec l'écoute active, **temps de démarrage** dans le journal technique).
2. Reprendre chaque ligne C1 à C16 : passer à ✅ ou décider ; ajouter ce que l'essai a révélé (`essais/P7-resultats.md`).
3. Relancer `luxia-headless audio tests/assets/audio` et comparer au rapport `essais/P7-audio-rapport.md`.
4. Vérifier que les exigences passées « Réalisé, à valider sur matériel » (AUD-001, 002, 003, 027) ont leur entrée « Validation » dans leur fiche.
