# Pré-analyse de la documentation – P7 « Audio et tempo »

> Rédigée le 2026-10-01, **avant** l'essai, pour être reprise à la fin de la phase. Méthode : relecture croisée des
> documents touchés (02, 15, 16, 19, 30, 32, 40, 41, 50, 60, 99, glossaire, guide et rapport P7, fiches) avec le code livré,
> recherche des écarts (règle du doc 40 §6 : « tout écart au développement entraîne une mise à jour du cahier des charges »)
> et des oublis. Statuts : ✅ fait · ⏳ à faire · 🔎 à confirmer à l'essai. Documents sœurs :
> [analyse de code](analyse-code-p7.md), [analyse ergonomique](analyse-ergonomique-p7.md).

## 1. Écarts entre le cahier des charges et le code (à régulariser)

| # | Document | Écart | Action |
|---|---|---|---|
| D1 | [19](../19-audio-et-tempo.md) AUD-062 | Le texte dit « ≥ 2 mesures » ; le code détecte un break dès **une mesure** (1,5 à 4 s, décision D36 écrite au doc 02 mais pas dans l'exigence) | ⏳ corriger la ligne AUD-062 (« ≥ 1 mesure, durée minimale réglable à terme ») et renvoyer à D36 |
| D2 | 19 AUD-024 | Règle nouvelle non écrite : **le « 1 » posé à la main prime sur l'analyse jusqu'au prochain morceau** (défaut C1 de l'analyse de code) | ⏳ ajouter la phrase au critère d'acceptation |
| D3 | 19 AUD-025 / §3.1 | « Passage automatique en source Tap (réglable : ou *tap corrige l'audio*) » : l'option « tap corrige l'audio » n'existe pas, un tap passe en source Tap | ⏳ écrire le comportement réel ; l'option devient une idée (doc 99) |
| D4 | 19 AUD-042 | « Réglables globalement (et par scène, SCN-050) » : les temps morts ne sont pas exposés à l'écran, rien n'est réglable par scène | ⏳ préciser (global seulement, temps morts par l'API) ; fiche AUD-042 à jour ✅ |
| D5 | 02 GEN-026 | « BPM borné à une plage réglable (par défaut 60-200) » : le moteur borne à **20-400** (constantes), l'écoute explore **70-180** réglable à l'écran | ⏳ aligner le texte sur le code (deux plages : bornes du moteur, plage d'exploration de l'écoute) ; fiche GEN-026 à créer |
| D6 | 02 GEN-024 | Mesure à 4 temps : réalisée (`Duration.BeatsPerBar`) mais **aucune fiche** | ⏳ créer la fiche (Réalisé) |
| D7 | 16 §5 | Le tableau des multiplicateurs (×4, ×2, ×1, ½, ¼, 1 mesure, 2 mesures, 4 mesures) ne correspond pas aux réglages livrés (« tous les N » événements de 1 à 64, périodes d'effet libres) | ⏳ remplacer par les réglages réels (la note §5b dit déjà le réalisé) |
| D8 | 02 §6.2 (commandes) | `AjusterTempo` est décrit « ×2, ÷2, ±1 BPM, recaler la phase » : livré = ×2, ÷2, ± valeur libre, « 1 ici » (recaler la **mesure**) ; **`SetTempoLatencyCommand` et `LaunchSceneCommand.Immediate` absents du catalogue** | ⏳ compléter le catalogue (nouvelle ligne CMD pour la latence) |
| D9 | 02 §6.3 (événements) | EVT-020, 021, 024 en Partiel (instantané et flux audio, pas de bus) | 🔎 décider avec P8 (le séquenceur a besoin des temps) : soit les publier, soit réécrire les lignes |
| D10 | 50 (préférences) | Le bloc d'exemple de `preferences.json` ne montre pas la nouvelle section `audio` (`listen`, `deviceId`, `pulseSensitivity`, `energySmoothingSeconds`, `minBpm`, `maxBpm`, `preferredBpm`, `latencySeconds`) | ⏳ ajouter le bloc et le tableau des champs ; ajouter une ligne d'historique des formats |
| D11 | 19 §10 | Le plancher d'impulsion (0,1 de flux), les temps morts par défaut et la limite de 30 événements gardés ne sont pas chiffrés | ⏳ les ajouter à la ligne « Impulsions » (les constantes iront dans une classe, voir C5 de l'analyse de code) |

## 2. Ce qui manque

| # | Document | Manque | Action |
|---|---|---|---|
| D12 | [60](../60-ergonomie.md) | Rien sur le **bloc BPM**, l'écran **Audio**, le **voyant d'attente ⏳**, la **quantification**, le volet « Au rythme » : la charte d'interaction ne couvre pas l'horloge musicale | ⏳ §4.10 « Horloge musicale et écoute » avec les règles (état toujours visible, un seul endroit pour chaque réglage, mise en page stable) ; voir l'analyse ergonomique |
| D13 | [03](../03-regles-de-developpement.md) §11 (pièges) | Quatre pièges vécus pendant P7 : (a) **HTP** : une scène d'intensité pleine (« Plein feu ») masque toute scène qui joue sur l'intensité des mêmes appareils ; (b) **tout ce qui parle à Windows (COM, WASAPI, Bluetooth) est lent** (0,4 à 1 s) : jamais sur le chemin de démarrage ni sur le fil de l'interface ; (c) **scripts PowerShell 5.1** : un fichier UTF-8 sans BOM est lu en ANSI ; (d) **lancer `LuXia.exe` pour un essai de fumée pilote le vrai matériel** | ⏳ quatre lignes au §11 |
| D14 | [30](../30-plan-de-tests.md) | Pas de correspondance entre **T-AUD-01 à 06 / SC-07** et les tests réellement écrits | ⏳ tableau : T-AUD-01 → `AnalyzerTests`, `PulseAndEnergyTests` ; T-AUD-02 → rapport `essais/P7-audio-rapport.md` ; T-AUD-03 → `MusicalClockTests` ; T-AUD-04 → `AudioListenerTests`, `ReferenceShowP7Tests` ; T-AUD-05 → guide P7 ; **T-AUD-06 non fait** ; SC-07 → exemples 5, 8 et 13 du guide |
| D15 | [32](../32-passation.md) | Le bloc « P7 développée » dit « plus de 850 tests » (847 réels) ; la section « À faire pour valider P7 » n'existe pas ; le fichier fait 362 lignes et grandit à chaque phase | ⏳ corriger le chiffre ; ajouter « À faire pour valider P7 » (essai, revérifications, analyse ergonomique, fusion `v1.009`) ; envisager `32b-historique.md` pour le détail des phases validées, ne gardant que l'état courant en tête |
| D16 | 19 (guide d'essai lié) | Le guide P7 ne dit rien de l'**autorisation du micro** de Windows (exemple 18) ni de la lenteur de la première ouverture de la liste des périphériques | ⏳ deux phrases au guide |
| D17 | [41](../41-show-de-reference.md) | Les BPM de référence du jeu audio vivent dans `tests/assets/audio/annotations.csv` (non versionné) : rien dans le dépôt ne permet de les retrouver | ⏳ dupliquer la liste et ses sources dans `essais/P7-audio-rapport.md` (tableau « référence » avec la source de chacune) |
| D19 | fiches GEN-023, MOT-016 | Annoncées « Réalisé » ; la saisie des durées musicales n'existe plus que dans l'ancien écran Scènes (C17 de l'analyse de code) | ⏳ ajouter la limite aux deux fiches et au doc 16 §2 tant que les Propriétés n'offrent pas les unités |
| D18 | [99](../99-idees.md) | Idées de P7 bien notées ; manquent : « le tap corrige l'audio », premier temps automatique / manuel, mise en garde sur Plein feu, badge ♪ des scènes qui dépendent de l'horloge | ⏳ les ajouter après décision (analyse ergonomique) |

## 3. Cohérence des fiches et de la matrice

| Constat | Action |
|---|---|
| **Sans fiche** : GEN-024, GEN-026 (P7), LIVE-020, LIVE-021, SCN-006, SIM-011 (anciens) | ⏳ créer GEN-024 et GEN-026 ; les quatre autres hors P7, à traiter à part |
| Statuts « Réalisé, à valider sur matériel » : AUD-001, 002, 003, 027 | ⏳ après l'essai, entrée « Validation » et passage à Validé (ou correction) |
| Statuts « Partiel » : AUD-021, 024, SCN-050, 051, EVT-020, 021, 024 | 🔎 chacun a sa limite écrite dans la fiche ; relire après l'essai |
| Historique des fiches | En ajout seul, une entrée « Développement » par exigence ; manque l'entrée « Utilisateur | Test » de l'essai informel (« très correct, impressionnant ») | ⏳ l'ajouter aux fiches AUD-001, 020, 080 |
| Matrice `31` et index des fiches | Régénérés à chaque livraison ; à régénérer après les fiches ci-dessus | ⏳ |

## 4. Exactitude de ce qui est écrit (relecture ligne à ligne)

| Document | Point vérifié | Résultat |
|---|---|---|
| 15 §16 | Horloge, recalcul des durées, avance, quantification, horloge propre, effets calés | ✅ conforme au code ; ajouter la règle du « 1 ici » (C1) |
| 19 §10 | Chaîne d'analyse, tempo, temps, horloge, impulsions, énergie, événements, périphériques, latence, jeu de test, limites | ✅ conforme ; mettre à jour le plancher et la règle du « 1 ici » (D2, D11) |
| 50 §12 (scènes) et schéma `scenes.schema.json` | `advance`, `advanceEvery`, `quantize`, `ownBpm`, `energySpeed` | ✅ |
| Guide P7 | Numérotation 1 à 28, scènes citées, piège Plein feu | ✅ après la correction du 2026-10-01 ; 🔎 vérifier chaque consigne à l'essai |
| `essais/P7-audio-rapport.md` | Chiffres (84 %, 21 sur 25) recalculés depuis le tableau ; lecture des colonnes | ✅ ; ajouter la colonne « source de la référence » (D17) |
| Fiches AUD / SCN / EVT | Statut et références de tests existants | ✅ (les noms de tests ont été vérifiés par la compilation des projets de tests, pas par un script) ; ⏳ un script de contrôle « chaque test cité existe » |

## 5. Hygiène

| Sujet | Constat | Suite |
|---|---|---|
| Liens | Les liens relatifs des nouveaux documents ont été écrits à la main | ⏳ petit script de contrôle des liens `docs/**/*.md` (inexistant aujourd'hui) |
| Dates et versions | Toutes au format AAAA-MM-JJ ; version « 1.009.NNN » dans le guide | 🔎 mettre le numéro réel au moment de l'essai |
| Vocabulaire | « Écoute » (action), « Audio » (source du tempo et écran), « Écouter », « Suivre le tempo de la musique » : quatre mots pour deux notions | ⏳ voir l'analyse ergonomique § 3 avant de figer le glossaire |
| Langue | Commentaires, documents et interface en français ; identifiants en anglais | ✅ |

## 6. À rejouer à la fin de la phase

1. Reprendre les tableaux §1 à §3 : chaque ligne ⏳ doit être ✅ ou décidée.
2. Confronter chaque document au code **après** les correctifs de l'essai (un correctif change souvent une règle écrite).
3. Régénérer matrice et index ; relire `32-passation.md` du début pour qu'une discussion neuve puisse reprendre.
4. Étiqueter la version seulement quand le doc 32 §1, le doc 40 et la matrice disent la même chose.
