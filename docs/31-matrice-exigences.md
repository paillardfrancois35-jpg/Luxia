# 31 – Matrice exigences ↔ tests

> Générée par `python tools/matrice-exigences.py P0 P1 P2` (doc 30 §7). Ne pas modifier à la main.
> Statut : « Réalisé » (fait, testé automatiquement ou revu), « Réalisé, à valider sur matériel », « Partiel »,
> « Reporté (Pn) », « Non réalisé ». Le statut est tenu dans le dictionnaire STATUS du script ; la colonne Tests liste
> les tests qui portent [Trait("Exigence", …)]. La validation finale est donnée par l'utilisateur à la livraison.

## P0 – 44 exigences, 30 couvertes par des tests automatiques

> Réalisé : 30 · Réalisé, à valider sur matériel : 14

| Exigence | Pri. | Énoncé (début) | Statut | Remarque | Tests automatiques |
|---|---|---|---|---|---|
| GEN-001 | I | Le moteur de rendu doit pouvoir calculer des trames sans qu'aucun module d'interface, d'au… | Réalisé |  | DependencyRulesTests.Project_DoesNotReferenceUserInterface<br>DependencyRulesTests.Project_OnlyReferencesAllowedDmxProjects<br>ReferenceShowP0Tests.Engine_InVirtualTime_ReproducesReferenceRecording<br>RenderEngineTests.Tick_WithSeveralUniverses_SubmitsOneFramePerUniverse<br>(+1) |
| GEN-002 | I | Aucun module ne doit modifier l'état de restitution autrement que par une commande du cata… | Réalisé | Revue : toute action passe par une commande (test de sortie = CMD-024, console = CMD-020/022). |  |
| GEN-003 | I | Les modules d'édition (Atelier) ne doivent pas dépendre du module Live, et réciproquement. | Réalisé |  | DependencyRulesTests.UserInterfaceModules_DoNotReferenceApplication |
| GEN-030 | I | Le moteur est cadencé par un **tick** régulier, par défaut **40 Hz**, réglable de 25 à 44 … | Réalisé, à valider sur matériel | Mesure 1 h à refaire, veille du PC désactivée (`dmx-headless gigue --duree 3600`). | TickLoopTests.RateHz_IsClampedTo25To44<br>TickLoopTests.Run_TwoSeconds_KeepsFortyHertz |
| GEN-031 | I | La gigue du tick doit rester inférieure à 5 ms (99e centile) sur un PC standard, interface… | Réalisé, à valider sur matériel | p99 0,8-0,9 ms sur 25 min ; mesure 1 h à refaire. | TickLoopTests.Run_TwoSeconds_KeepsFortyHertz |
| GEN-050 | I | Tous les fichiers de données sont en **JSON UTF-8 indenté**, lisibles et modifiables à la … | Réalisé |  | VersionedJsonFileTests.SaveThenLoad_RoundTrips<br>VersionedJsonFileTests.Save_WritesIndentedUtf8WithVersionFirst_AndReadableAccents |
| GEN-051 | I | Chaque fichier porte un **numéro de version de format**. L'application migre automatiqueme… | Réalisé |  | VersionedJsonFileTests.Load_OldVersion_MigratesAndKeepsBackup |
| GEN-056 | I | Un fichier illisible ou incohérent ne doit jamais faire planter l'application : il est sig… | Réalisé |  | PreferencesAndProjectTests.Preferences_Corrupt_GivesDefaultsAndSetsFileAside<br>PreferencesAndProjectTests.Project_CorruptFile_ReportsMessageWithoutThrowing<br>VersionedJsonFileTests.Load_CorruptFile_IsSetAsideWithoutThrowing<br>VersionedJsonFileTests.Load_MissingVersion_IsInvalid |
| GEN-060 | I | Au démarrage, la sortie émet un **blackout** tant que l'utilisateur n'a rien lancé. | Réalisé |  | RenderEngineTests.Tick_WithoutAnything_ProducesBlackoutFrame |
| GEN-080 | I | **Perte du PC** : si l'interface DMX ne reçoit plus de trame valide pendant **2 s**, elle … | Réalisé, à valider sur matériel | Chien de garde 2 s du firmware (T-SORT-06) ; firmware à téléverser. |  |
| GEN-081 | I | Si l'application se ferme anormalement, le comportement GEN-080 s'applique. | Réalisé, à valider sur matériel | Idem GEN-080 (tuer le processus). |  |
| GEN-091 | I | Une **sortie déconnectée** ne bloque ni le moteur ni l'interface ; reconnexion automatique… | Réalisé, à valider sur matériel | Testé avec faux port série ; T-SORT-05 sur matériel. | ArduinoOutputDriverTests.Unplug_ThenReplug_ReconnectsWithinThreeSeconds |
| GEN-093 | I | Une erreur dans un module secondaire (audio, style, MIDI, simulateur) ne doit ni arrêter l… | Réalisé |  | TickLoopTests.Run_TickThrows_LoopContinues |
| GEN-110 | I | **Journal technique** : démarrage, erreurs, connexions/déconnexions, avertissements ; fich… | Réalisé | Serilog, Documents\DMX\Journaux, 5 Mo × 20 fichiers. |  |
| GEN-120 | I | Toutes les fonctions de l'application utilisées en soirée fonctionnent **sans accès Intern… | Réalisé | Revue : aucune fonction n'utilise le réseau. |  |
| SORT-001 | I | Chaque univers est associé à **zéro, un ou plusieurs** pilotes. Une même trame peut partir… | Réalisé |  | OutputRouterTests.Submit_OneUniverseToTwoDrivers_BothReceiveIdenticalFrames<br>OutputRouterTests.Submit_OtherUniverse_IsNotRouted |
| SORT-002 | I | Chaque pilote tourne indépendamment : un pilote lent ou en erreur ne retarde ni le moteur … | Réalisé |  | OutputRouterTests.SlowDriver_DoesNotDelayOthers_AndKeepsOnlyLatestFrame |
| SORT-003 | I | Si un pilote n'a pas fini d'émettre la trame précédente, seule **la plus récente** est con… | Réalisé |  | OutputRouterTests.SlowDriver_DoesNotDelayOthers_AndKeepsOnlyLatestFrame |
| SORT-004 | I | Chaque pilote publie son état : `Déconnecté`, `Connexion…`, `Connecté`, `Erreur` (+ messag… | Réalisé |  | OutputRouterTests.StateChanges_ArePublishedOnBus |
| SORT-005 | I | Le moteur continue de fonctionner quand aucun pilote n'est connecté. | Réalisé |  | OutputRouterTests.Submit_WithoutAnyDriver_DoesNothing |
| SORT-006 | I | La configuration des sorties (univers → pilotes, paramètres) est enregistrée dans les **pr… | Réalisé |  | PreferencesAndProjectTests.Preferences_Missing_GivesDefaults<br>PreferencesAndProjectTests.Preferences_Update_IsPersisted |
| SORT-007 | M | Écran « Sorties » : liste des pilotes, état, port, trames/s, bouton reconnecter, bouton te… | Réalisé |  | ReferenceShowP0Tests.ReferenceRecording_LightsChannels1To179InOrder_NeverSmoke<br>RenderEngineTests.TestPattern_OnlyCurrentChannelIsLit_WithConfiguredValue<br>RenderEngineTests.TestPattern_SkipsExcludedChannels<br>RenderEngineTests.TestPattern_WalksChannelsUsingElapsedTime |
| SORT-010 | I | **Détection automatique** : énumération des ports série ; pour chaque port candidat (prior… | Réalisé, à valider sur matériel | T-SORT-04 (3 ports USB). Sonde les cartes Arduino et le dernier port ; option « tous les ports ». | ArduinoOutputDriverTests.Connect_DoesNotProbeNonArduinoPorts_ByDefault<br>ArduinoOutputDriverTests.Connect_FindsProjectFirmwareAmongArduinoPorts<br>ArduinoOutputDriverTests.Connect_ProbesAllPorts_WhenEnabled |
| SORT-011 | I | Le dernier port utilisé est mémorisé et essayé en premier. | Réalisé |  | ArduinoOutputDriverTests.Connect_TriesLastPortFirst_AndReportsSelectedPort<br>PreferencesAndProjectTests.Preferences_Update_IsPersisted |
| SORT-012 | I | Le port n'est **jamais ouvert à 1200 bauds** (cette vitesse déclenche le mode programmatio… | Réalisé |  | EnttecProtocolTests.SerialPort_IsNeverOpenedAt1200Baud |
| SORT-013 | I | **Reconnexion automatique** : en cas de perte (débranchement, erreur d'écriture), tentativ… | Réalisé, à valider sur matériel | T-SORT-05 (10 débranchements). | ArduinoOutputDriverTests.Unplug_ThenReplug_ReconnectsWithinThreeSeconds<br>OutputRouterTests.WriteError_GoesToErrorThenReconnects |
| SORT-014 | M | Un port choisi manuellement peut forcer la connexion (identification facultative, pour les… | Réalisé |  | ArduinoOutputDriverTests.ForcedPort_Legacy_SendsLabel0x11WithoutIdentification<br>EnttecProtocolTests.EncodeLegacy_HasNoStartCode |
| SORT-015 | M | L'identification récupère la **version du firmware** et l'affiche ; une version trop ancie… | Réalisé |  | ArduinoOutputDriverTests.OldFirmware_IsAcceptedWithWarning<br>EnttecProtocolTests.TryParseIdentity_ReadsVersionAndChannels |
| SORT-020 | I | À chaque tick, le pilote envoie la **trame complète** de l'univers (et non les seules diff… | Réalisé |  | ArduinoOutputDriverTests.Post_SendsFullFrameWithConfiguredChannelCount<br>EnttecProtocolTests.EncodeSendDmx_FullUniverse_Is518Bytes |
| SORT-021 | M | Le nombre de canaux émis est réglable (par défaut : 512 ; option « jusqu'au dernier canal … | Réalisé |  | ArduinoOutputDriverTests.Post_SendsFullFrameWithConfiguredChannelCount |
| SORT-022 | I | Une erreur d'écriture ne lève jamais d'erreur vers le moteur : elle bascule le pilote en `… | Réalisé |  | ArduinoOutputDriverTests.Unplug_ThenReplug_ReconnectsWithinThreeSeconds<br>OutputRouterTests.WriteError_GoesToErrorThenReconnects |
| SORT-023 | M | Le pilote mesure et publie la durée d'écriture et les trames réellement émises par seconde… | Réalisé | Durée d'écriture et trames/s affichées dans l'écran Sorties. |  |
| SORT-040 | I | La ligne DMX est rafraîchie **en continu par le firmware** (bibliothèque DMXSerial en mode… | Réalisé, à valider sur matériel | Firmware compilé (DMXSerial), non téléversé. |  |
| SORT-041 | I | Au démarrage, tous les canaux sont à 0. | Réalisé, à valider sur matériel | Firmware : tous canaux à 0 au démarrage. |  |
| SORT-042 | I | **Chien de garde** : sans message DMX valide pendant **2 s**, tous les canaux passent à 0 … | Réalisé, à valider sur matériel | Firmware : chien de garde 2 s. |  |
| SORT-043 | I | Prise en charge des messages du §5.3 (labels 6, 10, 77 ; 3 en M ; 0x11 en S). | Réalisé, à valider sur matériel | Protocole testé côté PC ; firmware à téléverser. | EnttecProtocolTests.EncodeSendDmx_ProducesLabel6WithStartCode |
| SORT-044 | I | Aucune allocation dynamique de mémoire ; tampon de réception unique de 513 octets. | Réalisé | Revue du firmware : tampon unique de 513 octets, pas d'allocation. |  |
| SORT-045 | I | Les valeurs d'un message ne sont appliquées qu'après réception complète et valide du messa… | Réalisé, à valider sur matériel | Firmware : application à la réception de 0xE7 seulement. | EnttecProtocolTests.Parser_WrongEndByte_IsRejected |
| SORT-046 | M | LED interne : clignote à chaque message valide ; allumée fixe si chien de garde déclenché … | Réalisé, à valider sur matériel | LED : clignote / fixe / éteinte. |  |
| SORT-047 | M | La version du firmware est une constante unique, renvoyée par les messages 3 et 77. | Réalisé | Constante FW_VERSION (1.0) renvoyée par les labels 3 et 77. |  |
| SORT-048 | M | Le firmware est **versionné dans le dépôt** (`firmware/arduino-dmx`) avec la liste des bib… | Réalisé | firmware/arduino-dmx + README (bibliothèques, cavaliers, commandes). |  |
| SORT-049 | S | Le nombre de canaux émis sur la ligne est ajusté à la longueur du dernier message reçu (fr… | Réalisé, à valider sur matériel | maxChannel = canaux reçus (24 minimum). |  |
| SORT-060 | I | **Enregistreur** : écrit chaque trame avec son horodatage (temps écoulé depuis le début, e… | Réalisé |  | ConsoleLatencyTests.Override_AppearsInRecordedFrame_OnNextTick<br>RecordingTests.IdenticalFrames_AreStoredCompactly<br>RecordingTests.RecorderDriver_AttachedHot_WritesFramesToFile<br>RecordingTests.WriteThenRead_RoundTripsFramesAndTimestamps<br>(+2) |
| SORT-061 | I | L'Enregistreur peut être activé/désactivé à chaud, et utilisé en même temps que l'Arduino. | Réalisé |  | RecordingTests.RecorderDriver_AttachedHot_WritesFramesToFile |

## P1 – 22 exigences, 11 couvertes par des tests automatiques

> Non réalisé : 2 · Partiel : 3 · Reporté (P3) : 1 · Reporté (P4-P5) : 1 · Réalisé : 15

| Exigence | Pri. | Énoncé (début) | Statut | Remarque | Tests automatiques |
|---|---|---|---|---|---|
| CONS-001 | I | Affichage de faders par pages (16, 32 ou 48 par page selon la largeur), numérotés 1 à 512,… | Réalisé |  | ConsoleViewModelTests.PageSize_AdaptsToWidth_AndPagesCoverAll512Channels |
| CONS-002 | I | Réglage de chaque fader à la souris (glisser), à la molette (±1, Maj+molette ±10), au clav… | Réalisé |  | ConsoleViewModelTests.TypedValue_Valid_IsApplied_Invalid_IsRejected |
| CONS-003 | I | Toucher un fader le **prend** : sa valeur surcharge la sortie (étape 11 de la chaîne de re… | Réalisé |  | ChannelOverrideTests.Override_SetsChannelValueOnNextTick<br>ChannelOverrideTests.Override_ToZero_IsStillAnOverride<br>ChannelOverrideTests.Release_ReturnsChannelToChainValue<br>ConsoleLatencyTests.Override_AppearsInRecordedFrame_OnNextTick<br>(+2) |
| CONS-004 | I | Commandes : libérer un fader, libérer la page, **tout libérer** ; mettre la page à 0 ; met… | Réalisé |  | ChannelOverrideTests.ReleaseAll_ClearsEveryUniverse<br>ConsoleViewModelTests.PageToFull_ThenReleasePage<br>ConsoleViewModelTests.ReleaseSelection_OnlyReleasesSelectedChannels |
| CONS-005 | I | Le fader affiche en permanence la **valeur réellement émise**, qu'il soit pris ou non (il … | Réalisé |  | ConsoleViewModelTests.FaderRequest_OverridesChannel_AndFaderShowsEmittedValue |
| CONS-006 | I | Sélection multiple de faders (Ctrl/Maj+clic) : un déplacement agit sur tous (en relatif ou… | Réalisé |  | ConsoleViewModelTests.ClickWithoutModifier_OutsideSelection_SelectsOnlyThatFader<br>ConsoleViewModelTests.MultiSelection_Absolute_SetsSameValue<br>ConsoleViewModelTests.MultiSelection_Relative_MovesAllByTheSameDelta |
| CONS-007 | M | Si le canal appartient à un appareil patché, affichage du nom de l'appareil, de l'attribut… | Reporté (P3) | Nom d'appareil / attribut / plage : nécessite le patch. |  |
| CONS-008 | M | Les surcharges de la console restent soumises au blackout et aux limites de sûreté (GEN-04… | Reporté (P4-P5) | Blackout (P4) et limites de sûreté (P5) pas encore implémentés ; TODO dans RenderEngine. |  |
| CONS-009 | M | Choix de l'univers affiché (si plusieurs). | Réalisé | Liste des univers configurés (un seul aujourd'hui). |  |
| CONS-010 | S | Mémoriser / rappeler des « instantanés » de console (état de tous les faders pris) pour le… | Réalisé |  | ConsoleViewModelTests.Snapshot_SaveReleaseRecall_RestoresOverrides<br>ReferenceShowP1Tests.ReferenceShow_LoadsWithSnapshots<br>ReferenceShowP1Tests.Snapshot_RecalledByEngine_ProducesExactlyItsChannels |
| CONS-040 | I | Grille de 512 cases par univers, la couleur/luminosité de chaque case représentant la vale… | Réalisé | OutputMonitor, 512 cases, valeurs affichables. |  |
| CONS-041 | I | Au survol d'une case : numéro de canal, valeur, appareil et attribut (si patché). | Réalisé |  | ConsoleViewModelTests.MonitorHover_DescribesChannel |
| CONS-043 | M | Les canaux appartenant à un même appareil sont délimités visuellement ; les canaux surchar… | Partiel | Canaux surchargés marqués ; délimitation par appareil en P3. |  |
| CONS-044 | S | Le moniteur peut s'ouvrir dans une fenêtre séparée. | Non réalisé | Priorité S. |  |
| GEN-090 | I | Latence entre une action utilisateur (clic, touche, fader, pad MIDI) et la trame émise < *… | Réalisé |  | ConsoleLatencyTests.Override_ReachesDriver_InLessThan50Milliseconds |
| GEN-100 | I | Interface entièrement en **français**. | Réalisé | Interface en français. |  |
| GEN-101 | I | **Thème sombre** par défaut (usage dans le noir), contrastes suffisants ; aucune zone blan… | Réalisé | Thème sombre Fluent, panneaux sombres. |  |
| GEN-103 | I | Toute action destructrice (suppression, écrasement) demande confirmation en Atelier ; en L… | Réalisé |  | ConsoleViewModelTests.Snapshot_Delete_AsksConfirmation |
| GEN-104 | I | Indicateur permanent : état de la sortie (connectée / déconnectée / simulée), trames/s, bl… | Partiel | Sortie, trames/s et enregistrement affichés ; blackout (P4) et mode auto (P10) affichés « — ». |  |
| GEN-107 | M | Glisser-déposer disponible pour les opérations naturelles (patch, placement au plan, scène… | Partiel | Glisser-déposer des positions d'un mode ; patch, plan, couches à venir (P3+). |  |
| GEN-108 | S | Taille de police de l'interface réglable. | Non réalisé | Priorité S. |  |
| GEN-109 | I | Aucune opération longue (import, analyse, sauvegarde) ne fige l'interface ; une progressio… | Réalisé |  | LibraryViewModelTests.Import_SavesNewModels_SkipsExisting_AndReports |

## P2 – 34 exigences, 30 couvertes par des tests automatiques

> Non réalisé : 2 · Partiel : 1 · Réalisé : 29 · Réalisé, à valider sur matériel : 2

| Exigence | Pri. | Énoncé (début) | Statut | Remarque | Tests automatiques |
|---|---|---|---|---|---|
| BIB-001 | I | Le modèle de données permet de décrire l'ensemble des éléments du §2 (identité, physique, … | Réalisé | Parc décrit sauf barre LCB803 (notice incomplète, Q24) ; WZYBUTA à vérifier (Q25). | FixtureLibraryTests.SaveThenLoad_IsLossless<br>ParkLibraryTests.ParkLibrary_LoadsSixModels_WithoutMessage |
| BIB-002 | I | Un modèle possède **au moins un mode** ; un mode référence des définitions de canaux (une … | Réalisé |  | FixtureValidatorTests.FixtureWithoutMode_IsAnError |
| BIB-003 | I | Un attribut 16 bits est décrit comme **un seul attribut** occupant deux canaux (grossier +… | Réalisé |  | FixtureEditsTests.SetResolution_16Bit_AddsFineAfterCoarse_AndBackTo8BitRemovesIt<br>FixtureValidatorTests.CoarseWithoutFine_IsAWarning<br>OflImporterTests.Spot_FineChannelsAreOneAttribute_AndWheelsCarryColors<br>ParkLibraryTests.Lyre_11Channels_Has16BitPanTilt_9ChannelsCoarseOnly<br>(+1) |
| BIB-004 | I | **Validation** à l'enregistrement : plages qui se chevauchent (erreur) ; trous entre plage… | Réalisé |  | FixtureValidatorTests.FineOf8BitChannel_IsAnError<br>FixtureValidatorTests.GapBetweenRanges_IsAWarning<br>FixtureValidatorTests.ModeWithoutChannel_IsAnError<br>FixtureValidatorTests.OrphanFineChannel_IsAnError<br>(+5) |
| BIB-005 | I | Le mode affiche son **nombre de canaux** et le **réglage à faire sur l'appareil** (ex. « A… | Réalisé |  | FixtureRulesTests.SettingSheet_ShowsChannelCountAndDeviceSetting<br>ParkLibraryTests.Lpc008s_ModesAndIntensityRules |
| BIB-006 | I | L'intensité virtuelle est déduite automatiquement (§2.7) ; la propriété **« Suit l'intensi… | Réalisé |  | FixtureRulesTests.FollowsIntensity_Override_WinsOverDeduction<br>FixtureRulesTests.SevenChannelMode_WithDimmer_NothingFollowsIntensity<br>FixtureRulesTests.ThreeChannelMode_HasVirtualIntensity_AndColorsFollowIntensity<br>LibraryViewModelTests.Editor_ModesChannelsAndRanges<br>(+1) |
| BIB-007 | I | Les étiquettes de sûreté (`strobe`, `fumée`) sont déduites de l'attribut et modifiables (e… | Réalisé |  | FixtureRulesTests.ProgramChannel_WithStrobeRange_IsTaggedStrobe<br>FixtureRulesTests.SafetyTags_AreDeducedFromAttribute<br>OflImporterTests.Fog_IsSmokeTagged<br>QlcImporterTests.ClassicPar_GroupsAndStrobeInProgram |
| BIB-008 | M | Les plages de type **emplacement de roue** portent une couleur ; le simulateur et les pale… | Réalisé |  | OflImporterTests.Spot_FineChannelsAreOneAttribute_AndWheelsCarryColors |
| BIB-009 | M | Un modèle porte une **version** incrémentée à chaque modification enregistrée. | Réalisé |  | FixtureLibraryTests.Save_IncrementsVersion_AndRenameMovesFile<br>LibraryViewModelTests.Save_BlockedByErrors_ThenSavedWithVersion |
| BIB-010 | S | Un modèle peut être **dérivé** d'un autre (copier puis modifier), avec mention de l'origin… | Réalisé |  | FixtureEditsTests.Derive_KeepsContent_WithNewIdAndOrigin<br>LibraryViewModelTests.Duplicate_GenericGivesEditableCopy |
| BIB-020 | I | Liste des modèles groupés par fabricant, avec recherche et filtres (fabricant, catégorie). | Réalisé |  | LibraryViewModelTests.List_GroupsByManufacturer_AndFilters |
| BIB-021 | I | Édition des modes en onglets ; ajout, suppression, réordonnancement des canaux d'un mode p… | Réalisé |  | FixtureEditsTests.AddAndRemoveSlot<br>FixtureEditsTests.MoveSlot_ReordersChannels<br>LibraryViewModelTests.Editor_ModesChannelsAndRanges |
| BIB-022 | I | Édition des plages d'un canal dans un tableau + **barre 0-255** colorée par plage, redimen… | Réalisé |  | FixtureLibraryTests.MoveBoundary_KeepsRangesAdjacent |
| BIB-023 | I | Saisie rapide de plages : « découper en N plages égales », « remplir le trou suivant ». | Réalisé |  | FixtureLibraryTests.FillNextGap_AddsRangeInFirstHole<br>FixtureLibraryTests.Split_InEightEqualRanges_CoversWholeChannel |
| BIB-024 | I | Annuler / rétablir (GEN-102). | Réalisé |  | LibraryViewModelTests.Editor_UndoRedo |
| BIB-025 | M | Éditeur de roues : emplacements avec nom, couleur (sélecteur), image de gobo. | Réalisé |  | FixtureEditsTests.Wheels_AddUpdateRemove |
| BIB-026 | M | Aperçu de la fiche « réglage sur l'appareil » (mode + adresse) telle qu'elle apparaîtra à … | Réalisé |  | FixtureRulesTests.SettingSheet_ShowsChannelCountAndDeviceSetting |
| BIB-027 | S | Joindre la notice PDF et une photo au modèle (fichiers liés). | Partiel | Chemin de la notice saisissable ; ouverture de la notice / photo non réalisée (S). |  |
| BIB-060 | I | Bouton **Tester en direct** : patch temporaire du modèle (mode courant) à une adresse choi… | Réalisé, à valider sur matériel | T-BIB-05 : vérification plage par plage de chaque appareil. | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| BIB-061 | I | Clic sur une plage → émission de sa valeur médiane ; curseur de balayage dans la plage ; c… | Réalisé, à valider sur matériel | T-BIB-05. | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| BIB-062 | M | Mode « **découverte** » : balayer lentement un canal de 0 à 255 en affichant la valeur, av… | Réalisé |  | FixtureLibraryTests.SplitAt_CreatesBoundaryAtDiscoveredValue<br>LibraryViewModelTests.Discovery_NewBoundaryHere_SplitsRangesInEditor |
| BIB-063 | M | La sortie du test est la sortie active (Arduino et/ou simulateur) ; à la fermeture du test… | Réalisé |  | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| BIB-080 | I | **Import Open Fixture Library** (fichier JSON d'un appareil ou dossier) : modes, canaux, c… | Réalisé | Testé sur 5 fichiers rédigés au format OFL ; à confirmer avec de vrais fichiers téléchargés. | OflImporterTests.Fog_IsSmokeTagged<br>OflImporterTests.PixelBar_MatrixBecomesEightCells<br>OflImporterTests.ReferenceFiles_ImportToValidModels<br>OflImporterTests.RgbPar_ModesChannelsAndStrobeRanges<br>(+1) |
| BIB-081 | M | **Import QLC+** (`.qxf`) : canaux (groupes → attributs), capacités → plages, modes, têtes … | Réalisé | Testé sur 3 fichiers rédigés au format QLC+ ; à confirmer avec de vrais fichiers. | QlcImporterTests.Bar_HeadsBecomeCells<br>QlcImporterTests.ClassicPar_GroupsAndStrobeInProgram<br>QlcImporterTests.ReferenceFiles_ImportToValidModels<br>QlcImporterTests.Wash_PresetsAndFineChannels |
| BIB-082 | I | Rapport d'import : éléments non convertis ou approximés, listés clairement ; l'import ne b… | Réalisé |  | LibraryViewModelTests.Import_SavesNewModels_SkipsExisting_AndReports<br>OflImporterTests.BrokenFile_GivesErrorWithoutThrowing<br>OflImporterTests.UnknownCapability_BecomesGeneric_AndIsReported<br>QlcImporterTests.Batch_ImportsWholeFolder_AndReportsProgress |
| BIB-083 | M | Import de **lots** (dossier entier) sans figer l'interface (GEN-109). | Réalisé |  | LibraryViewModelTests.Import_SavesNewModels_SkipsExisting_AndReports<br>QlcImporterTests.Batch_ImportsWholeFolder_AndReportsProgress |
| BIB-084 | S | Export au format OFL (partage, contribution). | Non réalisé | Priorité S (export OFL). |  |
| CONS-060 | I | Le composant « faders d'un appareil » est réutilisable dans l'éditeur de bibliothèque pour… | Réalisé |  | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| GEN-020 | I | En interne, toute valeur d'attribut est une **valeur logique normalisée** entre 0 et 1, av… | Réalisé |  | DmxConversionTests.Half_Is128In8Bit_And0x8000In16Bit<br>DmxConversionTests.SixteenBit_KeepsMoreResolutionThan8Bit<br>DmxConversionTests.To8Bit_ClampsAndRounds |
| GEN-021 | I | L'interface affiche les valeurs dans l'unité la plus parlante : **%** pour les intensités,… | Réalisé |  | DmxConversionTests.Describe_UsesMostMeaningfulUnit |
| GEN-052 | I | Les objets se référencent par un **identifiant stable** (généré à la création), jamais par… | Réalisé |  | PreferencesAndProjectTests.Project_CreateThenOpen |
| GEN-058 | S | Les chemins sont relatifs au dossier du projet ou de la bibliothèque (projet déplaçable). | Non réalisé | Priorité S : chemins de notice des modèles d'exemple relatifs au dépôt. |  |
| GEN-102 | I | **Annuler / rétablir** dans tous les éditeurs de l'Atelier (au moins 50 niveaux). | Réalisé |  | LibraryViewModelTests.Editor_UndoRedo<br>LibraryViewModelTests.History_Keeps100Levels |
| GEN-105 | M | Recherche / filtre dans toute liste de plus de 20 éléments (modèles, scènes, palettes…). | Réalisé | Recherche et filtres dans la bibliothèque (seule liste > 20 éléments à ce jour). |  |
