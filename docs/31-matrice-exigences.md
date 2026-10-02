# 31 – Matrice exigences ↔ tests

> Générée par `python tools/matrice-exigences.py P0 P1 P2 P3 P4 P5 ERG P6 ERG2 P7 P8` (doc 30 §7). Ne pas modifier à la main.
> Le **statut** vient de la fiche de chaque exigence (`docs/exigences/<ID>.md`), qui fait foi et porte l'historique ;
> la colonne Tests liste les tests qui portent `[Trait("Exigence", …)]`.

## P0 – 45 exigences, 31 couvertes par des tests automatiques

> Réalisé : 30 · Réalisé, à valider sur matériel : 15

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [GEN-001](exigences/GEN-001.md) | I | Moteur indépendant de l'interface et du matériel | Réalisé | DependencyRulesTests.Project_DoesNotReferenceUserInterface<br>DependencyRulesTests.Project_OnlyReferencesAllowedDmxProjects<br>ReferenceShowP0Tests.Engine_InVirtualTime_ReproducesReferenceRecording<br>RenderEngineTests.Tick_WithSeveralUniverses_SubmitsOneFramePerUniverse<br>(+1) |
| [GEN-002](exigences/GEN-002.md) | I | Une seule porte d'entrée : les commandes | Réalisé |  |
| [GEN-003](exigences/GEN-003.md) | I | Atelier et Live indépendants | Réalisé | DependencyRulesTests.UserInterfaceModules_DoNotReferenceApplication |
| [GEN-030](exigences/GEN-030.md) | I | Tick à 40 Hz (25-44 Hz) | Réalisé, à valider sur matériel | TickLoopTests.RateHz_IsClampedTo25To44<br>TickLoopTests.Run_TwoSeconds_KeepsFortyHertz |
| [GEN-031](exigences/GEN-031.md) | I | Gigue du tick < 5 ms | Réalisé, à valider sur matériel | TickLoopTests.Run_TwoSeconds_KeepsFortyHertz |
| [GEN-050](exigences/GEN-050.md) | I | Fichiers JSON lisibles | Réalisé | GroupRulesTests.GroupStore_Missing_ReturnsNoGroup_ThenSaveAndLoadRoundTrips<br>SceneUsageAndStoreTests.Stores_RoundTrip_AllValueForms<br>StoresTests.InstallationStore_SaveThenLoad_RoundTrips<br>VersionedJsonFileTests.SaveThenLoad_RoundTrips<br>(+1) |
| [GEN-051](exigences/GEN-051.md) | I | Version de format et migrations | Réalisé | VersionedJsonFileTests.Load_OldVersion_MigratesAndKeepsBackup |
| [GEN-056](exigences/GEN-056.md) | I | Fichier illisible sans plantage | Réalisé | PreferencesAndProjectTests.Preferences_Corrupt_GivesDefaultsAndSetsFileAside<br>PreferencesAndProjectTests.Project_CorruptFile_ReportsMessageWithoutThrowing<br>VersionedJsonFileTests.Load_CorruptFile_IsSetAsideWithoutThrowing<br>VersionedJsonFileTests.Load_MissingVersion_IsInvalid<br>(+1) |
| [GEN-060](exigences/GEN-060.md) | I | Blackout au démarrage | Réalisé | RenderEngineTests.Tick_WithoutAnything_ProducesBlackoutFrame |
| [GEN-080](exigences/GEN-080.md) | I | Perte du PC : noir en 2 s | Réalisé, à valider sur matériel |  |
| [GEN-081](exigences/GEN-081.md) | I | Arrêt anormal de l'application : noir en 2 s | Réalisé, à valider sur matériel |  |
| [GEN-091](exigences/GEN-091.md) | I | Sortie déconnectée non bloquante, reconnexion < 3 s | Réalisé, à valider sur matériel | ArduinoOutputDriverTests.Unplug_ThenReplug_ReconnectsWithinThreeSeconds |
| [GEN-093](exigences/GEN-093.md) | I | Panne d'un module secondaire isolée | Réalisé | TickLoopTests.Run_TickThrows_LoopContinues |
| [GEN-096](exigences/GEN-096.md) | I | Pas de mise en veille du PC pendant l'émission | Réalisé, à valider sur matériel | SleepInhibitorTests.StartThenStop_SetsAndReleasesTheRequest |
| [GEN-110](exigences/GEN-110.md) | I | Journal technique | Réalisé |  |
| [GEN-120](exigences/GEN-120.md) | I | Fonctionnement hors-ligne | Réalisé |  |
| [SORT-001](exigences/SORT-001.md) | I | Univers vers plusieurs pilotes | Réalisé | OutputRouterTests.Submit_OneUniverseToTwoDrivers_BothReceiveIdenticalFrames<br>OutputRouterTests.Submit_OtherUniverse_IsNotRouted |
| [SORT-002](exigences/SORT-002.md) | I | Pilotes indépendants | Réalisé | OutputRouterTests.SlowDriver_DoesNotDelayOthers_AndKeepsOnlyLatestFrame |
| [SORT-003](exigences/SORT-003.md) | I | Seule la trame la plus récente | Réalisé | OutputRouterTests.SlowDriver_DoesNotDelayOthers_AndKeepsOnlyLatestFrame |
| [SORT-004](exigences/SORT-004.md) | I | État publié par chaque pilote | Réalisé | OutputRouterTests.StateChanges_ArePublishedOnBus |
| [SORT-005](exigences/SORT-005.md) | I | Moteur actif sans aucune sortie | Réalisé | OutputRouterTests.Submit_WithoutAnyDriver_DoesNothing |
| [SORT-006](exigences/SORT-006.md) | I | Configuration des sorties dans les préférences du poste | Réalisé | PreferencesAndProjectTests.Preferences_Missing_GivesDefaults<br>PreferencesAndProjectTests.Preferences_Update_IsPersisted |
| [SORT-007](exigences/SORT-007.md) | M | Écran « Sorties » et test de sortie | Réalisé | ReferenceShowP0Tests.ReferenceRecording_LightsChannels1To179InOrder_NeverSmoke<br>RenderEngineTests.TestPattern_OnlyCurrentChannelIsLit_WithConfiguredValue<br>RenderEngineTests.TestPattern_SkipsExcludedChannels<br>RenderEngineTests.TestPattern_WalksChannelsUsingElapsedTime |
| [SORT-010](exigences/SORT-010.md) | I | Détection automatique de l'Arduino | Réalisé, à valider sur matériel | ArduinoOutputDriverTests.Connect_DoesNotProbeNonArduinoPorts_ByDefault<br>ArduinoOutputDriverTests.Connect_FindsProjectFirmwareAmongArduinoPorts<br>ArduinoOutputDriverTests.Connect_ProbesAllPorts_WhenEnabled |
| [SORT-011](exigences/SORT-011.md) | I | Dernier port essayé en premier | Réalisé | ArduinoOutputDriverTests.Connect_TriesLastPortFirst_AndReportsSelectedPort<br>PreferencesAndProjectTests.Preferences_Update_IsPersisted |
| [SORT-012](exigences/SORT-012.md) | I | Jamais 1200 bauds, DTR actif | Réalisé | EnttecProtocolTests.SerialPort_IsNeverOpenedAt1200Baud |
| [SORT-013](exigences/SORT-013.md) | I | Reconnexion automatique | Réalisé, à valider sur matériel | ArduinoOutputDriverTests.Unplug_ThenReplug_ReconnectsWithinThreeSeconds<br>OutputRouterTests.WriteError_GoesToErrorThenReconnects |
| [SORT-014](exigences/SORT-014.md) | M | Port imposé manuellement | Réalisé | ArduinoOutputDriverTests.ForcedPort_Legacy_SendsLabel0x11WithoutIdentification<br>EnttecProtocolTests.EncodeLegacy_HasNoStartCode |
| [SORT-015](exigences/SORT-015.md) | M | Version du firmware | Réalisé | ArduinoOutputDriverTests.OldFirmware_IsAcceptedWithWarning<br>EnttecProtocolTests.TryParseIdentity_ReadsVersionAndChannels |
| [SORT-020](exigences/SORT-020.md) | I | Trame complète à chaque tick | Réalisé | ArduinoOutputDriverTests.Post_SendsFullFrameWithConfiguredChannelCount<br>EnttecProtocolTests.EncodeSendDmx_FullUniverse_Is518Bytes |
| [SORT-021](exigences/SORT-021.md) | M | Nombre de canaux émis réglable | Réalisé | ArduinoOutputDriverTests.Post_SendsFullFrameWithConfiguredChannelCount |
| [SORT-022](exigences/SORT-022.md) | I | Erreur d'écriture sans effet sur le moteur | Réalisé | ArduinoOutputDriverTests.Unplug_ThenReplug_ReconnectsWithinThreeSeconds<br>OutputRouterTests.WriteError_GoesToErrorThenReconnects |
| [SORT-023](exigences/SORT-023.md) | M | Mesures du pilote (durée, trames/s) | Réalisé |  |
| [SORT-040](exigences/SORT-040.md) | I | Rafraîchissement DMX continu par le firmware | Réalisé, à valider sur matériel |  |
| [SORT-041](exigences/SORT-041.md) | I | Canaux à 0 au démarrage de la carte | Réalisé, à valider sur matériel |  |
| [SORT-042](exigences/SORT-042.md) | I | Chien de garde du firmware (2 s) | Réalisé, à valider sur matériel |  |
| [SORT-043](exigences/SORT-043.md) | I | Messages du protocole pris en charge | Réalisé, à valider sur matériel | EnttecProtocolTests.EncodeSendDmx_ProducesLabel6WithStartCode |
| [SORT-044](exigences/SORT-044.md) | I | Firmware sans allocation dynamique | Réalisé |  |
| [SORT-045](exigences/SORT-045.md) | I | Message appliqué seulement s'il est complet | Réalisé, à valider sur matériel | EnttecProtocolTests.Parser_WrongEndByte_IsRejected |
| [SORT-046](exigences/SORT-046.md) | M | LED d'état de la carte | Réalisé, à valider sur matériel |  |
| [SORT-047](exigences/SORT-047.md) | M | Version unique du firmware | Réalisé |  |
| [SORT-048](exigences/SORT-048.md) | M | Firmware versionné et documenté | Réalisé |  |
| [SORT-049](exigences/SORT-049.md) | S | Trame DMX ajustée au nombre de canaux reçus | Réalisé, à valider sur matériel |  |
| [SORT-060](exigences/SORT-060.md) | I | Enregistreur de trames | Réalisé | ConsoleLatencyTests.Override_AppearsInRecordedFrame_OnNextTick<br>RecordingTests.IdenticalFrames_AreStoredCompactly<br>RecordingTests.RecorderDriver_AttachedHot_WritesFramesToFile<br>RecordingTests.WriteThenRead_RoundTripsFramesAndTimestamps<br>(+2) |
| [SORT-061](exigences/SORT-061.md) | I | Enregistreur activable à chaud | Réalisé | RecordingTests.RecorderDriver_AttachedHot_WritesFramesToFile |

## P1 – 23 exigences, 14 couvertes par des tests automatiques

> Non réalisé : 2 · Partiel : 3 · Réalisé : 18

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [CONS-001](exigences/CONS-001.md) | I | Faders par pages | Réalisé | ConsoleViewModelTests.PageSize_AdaptsToWidth_AndPagesCoverAll512Channels |
| [CONS-002](exigences/CONS-002.md) | I | Modes de saisie des faders | Réalisé | ConsoleViewModelTests.TypedValue_Valid_IsApplied_Invalid_IsRejected |
| [CONS-003](exigences/CONS-003.md) | I | Prise et libération d'un fader | Réalisé | ChannelOverrideTests.Override_SetsChannelValueOnNextTick<br>ChannelOverrideTests.Override_ToZero_IsStillAnOverride<br>ChannelOverrideTests.Release_ReturnsChannelToChainValue<br>ConsoleLatencyTests.Override_AppearsInRecordedFrame_OnNextTick<br>(+2) |
| [CONS-004](exigences/CONS-004.md) | I | Commandes de libération et de page | Réalisé | ChannelOverrideTests.ReleaseAll_ClearsEveryUniverse<br>ConsoleViewModelTests.PageToFull_ThenReleasePage<br>ConsoleViewModelTests.ReleaseSelection_OnlyReleasesSelectedChannels |
| [CONS-005](exigences/CONS-005.md) | I | Le fader affiche la valeur émise | Réalisé | ConsoleViewModelTests.FaderRequest_OverridesChannel_AndFaderShowsEmittedValue |
| [CONS-006](exigences/CONS-006.md) | I | Sélection multiple de faders | Réalisé | ConsoleViewModelTests.ClickWithoutModifier_OutsideSelection_SelectsOnlyThatFader<br>ConsoleViewModelTests.MultiSelection_Absolute_SetsSameValue<br>ConsoleViewModelTests.MultiSelection_Relative_MovesAllByTheSameDelta |
| [CONS-007](exigences/CONS-007.md) | M | Appareil, attribut et plage sur le fader | Réalisé | ConsoleViewModelTests.Fader_PatchedChannelWithCapabilities_ShowsRangeNameInsteadOfPercent<br>ConsoleViewModelTests.MonitorHover_PatchedChannel_ShowsFixtureAndAttribute<br>PatchLookupTests.FindChannel_DifferentUniverse_ReturnsNull<br>PatchLookupTests.FindChannel_OutsideRange_ReturnsNull<br>(+1) |
| [CONS-008](exigences/CONS-008.md) | M | Surcharges soumises au blackout et à la sûreté | Partiel | RenderChainTests.RawOverrides_OfDimmedChannels_AreSilencedByBlackout |
| [CONS-009](exigences/CONS-009.md) | M | Choix de l'univers | Réalisé |  |
| [CONS-010](exigences/CONS-010.md) | S | Instantanés de console | Réalisé | ConsoleViewModelTests.Snapshot_SaveReleaseRecall_RestoresOverrides<br>ReferenceShowP1Tests.ReferenceShow_LoadsWithSnapshots<br>ReferenceShowP1Tests.Snapshot_RecalledByEngine_ProducesExactlyItsChannels |
| [CONS-040](exigences/CONS-040.md) | I | Moniteur de sortie 512 cases | Réalisé |  |
| [CONS-041](exigences/CONS-041.md) | I | Infos au survol du moniteur | Réalisé | ConsoleViewModelTests.MonitorHover_DescribesChannel<br>ConsoleViewModelTests.MonitorHover_PatchedChannel_ShowsFixtureAndAttribute |
| [CONS-043](exigences/CONS-043.md) | M | Délimitation des appareils et surcharges dans le moniteur | Réalisé | ConsoleViewModelTests.FixtureBoundaries_ReflectsPatch<br>PatchLookupTests.FixtureRanges_ReturnsOneRangePerFixture |
| [CONS-044](exigences/CONS-044.md) | S | Moniteur dans une fenêtre séparée | Non réalisé |  |
| [GEN-090](exigences/GEN-090.md) | I | Latence action → trame < 50 ms | Réalisé | ConsoleLatencyTests.Override_ReachesDriver_InLessThan50Milliseconds |
| [GEN-100](exigences/GEN-100.md) | I | Interface en français | Réalisé |  |
| [GEN-101](exigences/GEN-101.md) | I | Thème sombre | Réalisé |  |
| [GEN-103](exigences/GEN-103.md) | I | Confirmation des actions destructrices | Réalisé | ConsoleViewModelTests.Snapshot_Delete_AsksConfirmation<br>InstallationViewModelTests.DeleteFixture_AsksConfirmation |
| [GEN-104](exigences/GEN-104.md) | I | Indicateur d'état permanent | Partiel |  |
| [GEN-107](exigences/GEN-107.md) | M | Glisser-déposer | Partiel |  |
| [GEN-108](exigences/GEN-108.md) | S | Taille de police réglable | Non réalisé |  |
| [GEN-109](exigences/GEN-109.md) | I | Opérations longues sans figer l'interface | Réalisé | LibraryViewModelTests.Import_SavesNewModels_SkipsExisting_AndReports |
| [MOT-074](exigences/MOT-074.md) | I | Surcharges conformes à la chaîne de rendu | Réalisé |  |

## P2 – 34 exigences, 30 couvertes par des tests automatiques

> Non réalisé : 2 · Partiel : 1 · Réalisé : 29 · Réalisé, à valider sur matériel : 2

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [BIB-001](exigences/BIB-001.md) | I | Modèle de données complet d'un appareil | Réalisé | FixtureLibraryTests.SaveThenLoad_IsLossless<br>ParkLibraryTests.Lcb803_SectionsBecomeCells<br>ParkLibraryTests.ParkLibrary_LoadsEightModels_WithoutMessage<br>ParkLibraryTests.Wzybuta_20And64Channels_12CellsIn64 |
| [BIB-002](exigences/BIB-002.md) | I | Au moins un mode ; définitions de canaux partagées | Réalisé | FixtureValidatorTests.FixtureWithoutMode_IsAnError |
| [BIB-003](exigences/BIB-003.md) | I | Attribut 16 bits = un seul attribut sur deux canaux | Réalisé | FixtureEditsTests.SetResolution_16Bit_AddsFineAfterCoarse_AndBackTo8BitRemovesIt<br>FixtureValidatorTests.CoarseWithoutFine_IsAWarning<br>OflImporterTests.Spot_FineChannelsAreOneAttribute_AndWheelsCarryColors<br>ParkLibraryTests.Lyre_11Channels_Has16BitPanTilt_9ChannelsCoarseOnly<br>(+2) |
| [BIB-004](exigences/BIB-004.md) | I | Validation d'un modèle | Réalisé | FixtureValidatorTests.FineOf8BitChannel_IsAnError<br>FixtureValidatorTests.GapBetweenRanges_IsAWarning<br>FixtureValidatorTests.ModeWithoutChannel_IsAnError<br>FixtureValidatorTests.OrphanFineChannel_IsAnError<br>(+5) |
| [BIB-005](exigences/BIB-005.md) | I | Nombre de canaux et réglage sur l'appareil par mode | Réalisé | FixtureRulesTests.SettingSheet_ShowsChannelCountAndDeviceSetting<br>ParkLibraryTests.Lpc008s_ModesAndIntensityRules |
| [BIB-006](exigences/BIB-006.md) | I | Intensité virtuelle et « Suit l'intensité » | Réalisé | FixtureDecoderTests.Decode_NoDimmerChannel_UsesVirtualIntensity<br>FixtureRulesTests.FollowsIntensity_Override_WinsOverDeduction<br>FixtureRulesTests.SevenChannelMode_WithDimmer_NothingFollowsIntensity<br>FixtureRulesTests.ThreeChannelMode_HasVirtualIntensity_AndColorsFollowIntensity<br>(+4) |
| [BIB-007](exigences/BIB-007.md) | I | Étiquettes de sûreté déduites et modifiables | Réalisé | FixtureRulesTests.ProgramChannel_WithStrobeRange_IsTaggedStrobe<br>FixtureRulesTests.SafetyTags_AreDeducedFromAttribute<br>OflImporterTests.Fog_IsSmokeTagged<br>QlcImporterTests.ClassicPar_GroupsAndStrobeInProgram |
| [BIB-008](exigences/BIB-008.md) | M | Couleur des emplacements de roue | Réalisé | OflImporterTests.Spot_FineChannelsAreOneAttribute_AndWheelsCarryColors |
| [BIB-009](exigences/BIB-009.md) | M | Version du modèle incrémentée | Réalisé | FixtureLibraryTests.Save_IncrementsVersion_AndRenameMovesFile<br>LibraryViewModelTests.Save_BlockedByErrors_ThenSavedWithVersion |
| [BIB-010](exigences/BIB-010.md) | S | Dériver un modèle d'un autre | Réalisé | FixtureEditsTests.Derive_KeepsContent_WithNewIdAndOrigin<br>LibraryViewModelTests.Duplicate_GenericGivesEditableCopy |
| [BIB-020](exigences/BIB-020.md) | I | Liste par fabricant, recherche et filtres | Réalisé | LibraryViewModelTests.List_GroupsByManufacturer_AndFilters |
| [BIB-021](exigences/BIB-021.md) | I | Modes en onglets, canaux réordonnables | Réalisé | FixtureEditsTests.AddAndRemoveSlot<br>FixtureEditsTests.MoveSlot_ReordersChannels<br>LibraryViewModelTests.Editor_ModesChannelsAndRanges |
| [BIB-022](exigences/BIB-022.md) | I | Tableau des plages et barre 0-255 | Réalisé | FixtureLibraryTests.MoveBoundary_KeepsRangesAdjacent |
| [BIB-023](exigences/BIB-023.md) | I | Saisie rapide de plages | Réalisé | FixtureLibraryTests.FillNextGap_AddsRangeInFirstHole<br>FixtureLibraryTests.Split_InEightEqualRanges_CoversWholeChannel |
| [BIB-024](exigences/BIB-024.md) | I | Annuler / rétablir dans l'éditeur | Réalisé | LibraryViewModelTests.Editor_UndoRedo |
| [BIB-025](exigences/BIB-025.md) | M | Éditeur de roues | Réalisé | FixtureEditsTests.Wheels_AddUpdateRemove |
| [BIB-026](exigences/BIB-026.md) | M | Aperçu de la fiche de réglage | Réalisé | FixtureRulesTests.SettingSheet_ShowsChannelCountAndDeviceSetting |
| [BIB-027](exigences/BIB-027.md) | S | Notice et photo liées au modèle | Partiel |  |
| [BIB-060](exigences/BIB-060.md) | I | Test en direct d'un modèle | Réalisé, à valider sur matériel | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| [BIB-061](exigences/BIB-061.md) | I | Clic sur une plage ou une borne | Réalisé, à valider sur matériel | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| [BIB-062](exigences/BIB-062.md) | M | Mode découverte | Réalisé | FixtureLibraryTests.SplitAt_CreatesBoundaryAtDiscoveredValue<br>LibraryViewModelTests.Discovery_NewBoundaryHere_SplitsRangesInEditor |
| [BIB-063](exigences/BIB-063.md) | M | Sortie du test et nettoyage à la fermeture | Réalisé | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| [BIB-080](exigences/BIB-080.md) | I | Import Open Fixture Library | Réalisé | OflImporterTests.Fog_IsSmokeTagged<br>OflImporterTests.PixelBar_MatrixBecomesEightCells<br>OflImporterTests.ReferenceFiles_ImportToValidModels<br>OflImporterTests.RgbPar_ModesChannelsAndStrobeRanges<br>(+1) |
| [BIB-081](exigences/BIB-081.md) | M | Import QLC+ | Réalisé | QlcImporterTests.Bar_HeadsBecomeCells<br>QlcImporterTests.ClassicPar_GroupsAndStrobeInProgram<br>QlcImporterTests.ReferenceFiles_ImportToValidModels<br>QlcImporterTests.Wash_PresetsAndFineChannels |
| [BIB-082](exigences/BIB-082.md) | I | Rapport d'import | Réalisé | LibraryViewModelTests.Import_SavesNewModels_SkipsExisting_AndReports<br>OflImporterTests.BrokenFile_GivesErrorWithoutThrowing<br>OflImporterTests.UnknownCapability_BecomesGeneric_AndIsReported<br>QlcImporterTests.Batch_ImportsWholeFolder_AndReportsProgress |
| [BIB-083](exigences/BIB-083.md) | M | Import par lots sans figer l'interface | Réalisé | LibraryViewModelTests.Import_SavesNewModels_SkipsExisting_AndReports<br>QlcImporterTests.Batch_ImportsWholeFolder_AndReportsProgress |
| [BIB-084](exigences/BIB-084.md) | S | Export OFL | Non réalisé |  |
| [CONS-060](exigences/CONS-060.md) | I | Composant « faders d'un appareil » réutilisable | Réalisé | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| [GEN-020](exigences/GEN-020.md) | I | Valeurs internes normalisées 0-1 | Réalisé | DmxConversionTests.Half_Is128In8Bit_And0x8000In16Bit<br>DmxConversionTests.SixteenBit_KeepsMoreResolutionThan8Bit<br>DmxConversionTests.To8Bit_ClampsAndRounds |
| [GEN-021](exigences/GEN-021.md) | I | Affichage dans l'unité la plus parlante | Réalisé | DmxConversionTests.Describe_UsesMostMeaningfulUnit |
| [GEN-052](exigences/GEN-052.md) | I | Identifiants stables | Réalisé | PreferencesAndProjectTests.Project_CreateThenOpen<br>StoresTests.InstallationStore_SaveThenLoad_RoundTrips |
| [GEN-058](exigences/GEN-058.md) | S | Chemins relatifs (projet déplaçable) | Non réalisé |  |
| [GEN-102](exigences/GEN-102.md) | I | Annuler / rétablir (50 niveaux minimum) | Réalisé | LibraryViewModelTests.Editor_UndoRedo<br>LibraryViewModelTests.History_Keeps100Levels |
| [GEN-105](exigences/GEN-105.md) | M | Recherche dans les longues listes | Réalisé |  |

## P3 – 51 exigences, 33 couvertes par des tests automatiques

> Non réalisé : 5 · Partiel : 5 · Réalisé : 38 · Validé : 3

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [CONS-020](exigences/CONS-020.md) | I | Faders regroupés par appareil patché | Réalisé | ConsoleViewModelTests.DeviceMode_BuildsOneGroupPerPatchedFixture_AndFaderOverridesTheAttribute<br>ConsoleViewModelTests.DeviceMode_Leaving_KeepsOverridesAsRealConsoleValues |
| [CONS-021](exigences/CONS-021.md) | I | Outil adapté par type d'attribut | Partiel | ConsoleViewModelTests.DeviceMode_BuildsOneGroupPerPatchedFixture_AndFaderOverridesTheAttribute |
| [CONS-022](exigences/CONS-022.md) | I | Surcharge d'attribut soumise à la chaîne de rendu | Validé | ConsoleViewModelTests.DeviceMode_BuildsOneGroupPerPatchedFixture_AndFaderOverridesTheAttribute<br>RenderChainTests.AttributeOverride_WinsOverScenes_AndStaysUnderGrandMaster |
| [CONS-023](exigences/CONS-023.md) | M | Clic sur une plage et balayage | Réalisé |  |
| [CONS-024](exigences/CONS-024.md) | M | Bouton Identifier par appareil | Réalisé | ConsoleViewModelTests.Identify_LightsIntensityAndColorEmitters_AndReleasesOnStop<br>ConsoleViewModelTests.NeedsBackgroundRefresh_TrueOnlyWhileADeviceIsIdentifying |
| [GEN-004](exigences/GEN-004.md) | M | Composant d'édition réutilisable dans un autre écran | Réalisé |  |
| [GEN-053](exigences/GEN-053.md) | I | Copie des modèles d'appareils dans le projet | Réalisé | FixtureUpdateImpactTests.ForLibraryUpdate_ModeRemovedInNewDefinition_IsFlagged<br>InstallationViewModelTests.AddFixture_Multiple_CreatesConsecutiveAddressesAndCopiesModelIntoProject<br>InstallationViewModelTests.UpdateFromLibrary_WithImpact_AsksConfirmation<br>ReferenceShowP3Tests.ReferenceShow_ProjectFixtureLibrary_HasEveryUsedModel<br>(+3) |
| [GEN-114](exigences/GEN-114.md) | M | Menu « À propos » avec diagnostic copiable | Réalisé |  |
| [GEN-115](exigences/GEN-115.md) | I | Une seule instance de l'application à la fois | Réalisé |  |
| [GEN-116](exigences/GEN-116.md) | S | Icône de l'application | Réalisé |  |
| [GEN-122](exigences/GEN-122.md) | M | Sorties réseau locales autorisées (Art-Net) | Réalisé |  |
| [INST-001](exigences/INST-001.md) | I | Un ou plusieurs univers, numérotés et nommables | Réalisé | StoresTests.InstallationStore_Missing_ReturnsDefaultWithOneUniverse |
| [INST-002](exigences/INST-002.md) | I | Lien univers → pilotes dans les préférences | Réalisé |  |
| [INST-003](exigences/INST-003.md) | I | Vue barre d'univers | Réalisé |  |
| [INST-010](exigences/INST-010.md) | I | Ajouter un appareil au patch | Réalisé | InstallationViewModelTests.AddFixture_Multiple_CreatesConsecutiveAddressesAndCopiesModelIntoProject<br>ReferenceShowP3Tests.ReferenceShow_Installation_LoadsAndMatchesAddressPlan |
| [INST-011](exigences/INST-011.md) | I | Ajout multiple d'appareils identiques | Réalisé | InstallationViewModelTests.AddFixture_Multiple_CreatesConsecutiveAddressesAndCopiesModelIntoProject<br>PatchRulesTests.PlanMultiple_FourParsSevenChannels_GivesReferenceShowAddresses<br>PatchRulesTests.PlanMultiple_WithGap_LeavesReserve |
| [INST-012](exigences/INST-012.md) | I | Première adresse libre proposée | Réalisé | InstallationViewModelTests.SuggestAddress_ProposesFirstFreeAddress<br>PatchRulesTests.FindFreeAddress_NoRoomLeft_ReturnsNull<br>PatchRulesTests.FindFreeAddress_SkipsOccupiedRanges |
| [INST-013](exigences/INST-013.md) | I | Détection des chevauchements en temps réel | Réalisé | InstallationViewModelTests.OverlappingFixtures_AreFlagged<br>PatchRulesTests.DetectOverlaps_DifferentUniverses_NoOverlap<br>PatchRulesTests.DetectOverlaps_FindsOverlappingRange<br>ReferenceShowP3Tests.ReferenceShow_Installation_HasNoOverlap |
| [INST-014](exigences/INST-014.md) | M | Doublon volontaire (jumeaux) | Réalisé | InstallationViewModelTests.Twins_SameAddress_AreNotFlaggedAsOverlap<br>PatchRulesTests.DetectOverlaps_SameGroupButDifferentMode_StillOverlaps<br>PatchRulesTests.DetectOverlaps_Twins_SameAddress_NoOverlap<br>RenderChainTests.Twins_ShareParameters_AndReceiveSameValues |
| [INST-015](exigences/INST-015.md) | I | Déplacer un appareil | Réalisé | InstallationViewModelTests.RenameAndMove_UpdateThePatchedFixture |
| [INST-016](exigences/INST-016.md) | I | Changer le mode d'un appareil patché | Validé | FixtureUpdateImpactTests.ForModeChange_FromRichToSimpleMode_ReportsLostChannels<br>FixtureUpdateImpactTests.ForModeChange_SameMode_IsEmpty<br>InstallationViewModelTests.ChangeMode_UsedByThreeScenes_ReportsImpact_AndKeepsScenes<br>InstallationViewModelTests.ChangeMode_WithImpact_AsksConfirmation<br>(+1) |
| [INST-017](exigences/INST-017.md) | I | Nom, couleur et numéro court | Réalisé | InstallationViewModelTests.RenameAndMove_UpdateThePatchedFixture |
| [INST-018](exigences/INST-018.md) | I | Fiche d'installation | Réalisé |  |
| [INST-019](exigences/INST-019.md) | I | Identifier un appareil / chenillard d'identification | Réalisé | InstallationViewModelTests.Identify_LightsIntensityChannel_AndChaseAdvancesToNextFixture<br>InstallationViewModelTests.NeedsBackgroundRefresh_TrueOnlyWhileIdentifying |
| [INST-020](exigences/INST-020.md) | M | Supprimer un appareil | Réalisé | InstallationViewModelTests.DeleteFixture_AsksConfirmation |
| [INST-021](exigences/INST-021.md) | M | Options de montage par appareil | Partiel | FixtureDecoderTests.Decode_InvertedPan_ReversesDirection<br>ShowCompilerTests.SwapPanTilt_SendsPanValueToTiltChannel |
| [INST-030](exigences/INST-030.md) | I | Sélection manuelle ordonnée | Réalisé | InstallationViewModelTests.CreateSelection_FromCheckedFixtures_ThenReverse |
| [INST-031](exigences/INST-031.md) | I | Sélections automatiques | Réalisé | AutoSelectionsTests.Build_AllFixtures_IsOrderedByAddress<br>AutoSelectionsTests.Build_GroupsByCategoryAndByModel<br>AutoSelectionsTests.Build_NewFixtureAdded_AppearsWithoutAnyStoredState<br>InstallationViewModelTests.AutoSelections_IncludeAllAndByCategory |
| [INST-032](exigences/INST-032.md) | I | Sélections manuelles créées et réordonnées | Partiel |  |
| [INST-033](exigences/INST-033.md) | M | Opérations d'ordre sur une sélection | Réalisé | InstallationViewModelTests.CreateSelection_FromCheckedFixtures_ThenReverse<br>InstallationViewModelTests.ReorderSelection_ByPosition_UsesActiveVenuePlacements<br>SelectionRulesTests.FirstAndSecondHalf_SplitInTheMiddle<br>SelectionRulesTests.OddAndEven_SplitByRank<br>(+3) |
| [INST-034](exigences/INST-034.md) | M | Sélection de cellules | Validé | SelectionRulesTests.ExpandCells_BarsBecomeOrderedSections_CollapseGoesBack |
| [INST-050](exigences/INST-050.md) | I | Créer, dupliquer, activer un lieu | Réalisé | InstallationViewModelTests.CreateVenue_ThenActivate_ChangesActiveVenue<br>InstallationViewModelTests.Venues_HaveAGenericVenueByDefault<br>ReferenceShowP3Tests.ReferenceShow_Venue_PlacesEveryFixture_NoneAbsent<br>StoresTests.VenueStore_Missing_ReturnsDefaultGenericVenue<br>(+1) |
| [INST-051](exigences/INST-051.md) | I | Éditeur de plan | Partiel | InstallationViewModelTests.SavePlacements_PersistsPositionAndAbsence |
| [INST-052](exigences/INST-052.md) | I | Appareil absent | Réalisé | InstallationViewModelTests.SavePlacements_PersistsPositionAndAbsence<br>RenderChainTests.AbsentFixture_IsEmittedAtZero<br>SimulatorViewModelTests.Refresh_AbsentFixture_IsHidden |
| [MOT-075](exigences/MOT-075.md) | I | Identifier un appareil au-dessus de tout | Réalisé |  |
| [SIM-001](exigences/SIM-001.md) | I | Affichage du plan du lieu actif | Réalisé | SimulatorViewModelTests.Refresh_OnlyShowsPlacedAndPresentFixtures |
| [SIM-002](exigences/SIM-002.md) | I | Rendu 30 images/s sans ralentir le moteur | Réalisé |  |
| [SIM-003](exigences/SIM-003.md) | I | Décodage des trames via le patch | Réalisé | FixtureDecoderTests.Decode_AllChannelsAtZero_IsDarkAndOff<br>FixtureDecoderTests.Decode_ColorWheelAtOpenSlot_IsWhiteNotBlack<br>FixtureDecoderTests.Decode_ColorWheelCapability_UsesSlotColor<br>FixtureDecoderTests.Decode_MultiCellFixture_DecodesEachCellSeparately<br>(+3) |
| [SIM-004](exigences/SIM-004.md) | I | Faisceau des lyres | Réalisé | FixtureDecoderTests.Decode_InvertedPan_ReversesDirection<br>FixtureDecoderTests.Decode_PanTilt_ComputesAngleFromAmplitude |
| [SIM-005](exigences/SIM-005.md) | I | Survol / clic sur un appareil | Réalisé | SimulatorViewModelTests.OnFixtureHovered_DescribesFixture |
| [SIM-006](exigences/SIM-006.md) | I | Choix de la source affiché en permanence | Partiel | SimulatorViewModelTests.Blind_ShowsPreviewEngine_AndSaysSo |
| [SIM-007](exigences/SIM-007.md) | M | Fenêtre détachable et plein écran | Non réalisé |  |
| [SIM-008](exigences/SIM-008.md) | M | Zones interdites et repères du lieu | Non réalisé |  |
| [SIM-009](exigences/SIM-009.md) | M | Appareils identifiés et en erreur mis en évidence | Réalisé | SimulatorViewModelTests.Refresh_FixtureTypeMissingFromProjectLibrary_IsFlaggedAsError |
| [SIM-010](exigences/SIM-010.md) | M | Sélection au clic / au lasso | Réalisé | EditBenchPanelsTests.Plan_ClickCtrlClickRectangle_BuildTheSharedSelection<br>PlanAndStepStripTests.FixturesIn_Rectangle_KeepsOnlyFixturesInside |
| [SIM-012](exigences/SIM-012.md) | I | Protection photosensible (strobe) | Réalisé | FixtureDecoderTests.Decode_StrobeCapability_IsFlaggedAsStrobing |
| [SIM-013](exigences/SIM-013.md) | S | Vue de face | Non réalisé |  |
| [SORT-008](exigences/SORT-008.md) | M | Canaux maintenus pendant le test de sortie | Réalisé | RenderEngineTests.TestPattern_HeldChannels_RespectExcludedChannels<br>RenderEngineTests.TestPattern_HeldChannels_StayLitForTheWholeChase |
| [SORT-062](exigences/SORT-062.md) | I | Pilote Simulateur | Réalisé |  |
| [SORT-063](exigences/SORT-063.md) | S | Lecteur d'enregistrements | Non réalisé |  |
| [SORT-064](exigences/SORT-064.md) | S | Pilote Art-Net | Non réalisé |  |

## P4 – 87 exigences, 71 couvertes par des tests automatiques

> Partiel : 4 · Réalisé : 48 · Validé : 35

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [CONS-025](exigences/CONS-025.md) | M | Capturer les surcharges dans une scène | Réalisé |  |
| [CONS-042](exigences/CONS-042.md) | M | Origine de la valeur au survol du moniteur | Validé |  |
| [GEN-010](exigences/GEN-010.md) | I | Commandes horodatées, avec origine, au tick suivant | Réalisé | LayerMergeTests.CommandLog_KeepsReceptionTime_Origin_AndGroupsFaderMoves<br>RenderEngineTests.Send_BetweenTwoTicks_TakesEffectOnNextTick |
| [GEN-011](exigences/GEN-011.md) | I | Ordre d'arrivée des commandes | Réalisé | LayerMergeTests.TwoLaunches_InSameTick_SameExclusiveLayer_LastOneWins<br>RenderEngineTests.Commands_InSameTick_AreAppliedInArrivalOrder |
| [GEN-012](exigences/GEN-012.md) | M | Commande refusée : événement avec le motif | Réalisé | LayerMergeTests.UnknownScene_IsRejected_WithEventAndLogEntry |
| [GEN-013](exigences/GEN-013.md) | I | Publication non bloquante des événements | Réalisé | EnginePerformanceTests.SlowSubscriber_DoesNotDelayTicks |
| [GEN-022](exigences/GEN-022.md) | I | Couleurs logiques converties selon les émetteurs | Réalisé | ColorConversionTests.LedBar_24Channels_ColorOnWholeFixture_ReachesEverySection<br>ColorConversionTests.Rgb_Par_TakesColorDirectly |
| [GEN-023](exigences/GEN-023.md) | I | Durées en secondes ou en temps musicaux | Réalisé | EditBenchPanelsTests.Properties_SceneFades_AcceptBeatsAndStayEmptyWhenCleared<br>EditBenchPanelsTests.Properties_StepDurations_InBeatsAndBars_AreSavedInTheirUnit<br>EditBenchPanelsTests.Properties_Wizard_GeneratesStepsInBeats<br>MusicalClockTests.Clock_Fixed120_CountsBeatsAndBars<br>(+1) |
| [GEN-032](exigences/GEN-032.md) | I | Calculs sur le temps écoulé réel | Réalisé | RenderEngineTests.TestPattern_WalksChannelsUsingElapsedTime<br>ScenePlaybackTests.IrregularTicks_FadeStillEndsOnTime<br>ScenePlaybackTests.LinearFade_ZeroToFullInTwoSeconds_EightyRegularSteps |
| [GEN-033](exigences/GEN-033.md) | I | Horloges injectables | Réalisé |  |
| [GEN-040](exigences/GEN-040.md) | I | Chaîne de rendu appliquée dans l'ordre, à chaque tick | Réalisé | RenderChainTests.Untouched_Parameter_EmitsChannelDefault |
| [GEN-041](exigences/GEN-041.md) | I | Blackout et Grand Master sur les seules intensités | Validé | RenderChainTests.Blackout_ZeroesIntensitiesOnly_AndReleaseRestoresInstantly |
| [GEN-042](exigences/GEN-042.md) | I | Surcharges brutes soumises au blackout et à la sûreté | Réalisé | RenderChainTests.RawOverrides_OfDimmedChannels_AreSilencedByBlackout<br>SafetyTests.Smoke_Held20s_CutAt10s_ThenRest30s<br>SafetyTests.Strobe_Requested30s_CutAfter10s_ThenPause_ThenAllowedAgain |
| [GEN-043](exigences/GEN-043.md) | M | Chaîne de rendu explicable | Validé | RenderChainTests.Sources_ExplainWhereEachValueComesFrom |
| [GEN-063](exigences/GEN-063.md) | M | Mode aveugle en Atelier | Validé | EditBenchPanelsTests.Plan_ShowsOutput_OrPreviewInBlind<br>ScenesViewModelTests.Blind_SendsProgrammerToPreviewOnly<br>SimulatorViewModelTests.Blind_ShowsPreviewEngine_AndSaysSo |
| [GEN-082](exigences/GEN-082.md) | I | Blackout accessible en permanence | Validé |  |
| [GEN-106](exigences/GEN-106.md) | M | Nom, couleur et icône des objets | Réalisé |  |
| [GEN-112](exigences/GEN-112.md) | M | Journal des commandes consultable | Réalisé | LayerMergeTests.CommandLog_KeepsReceptionTime_Origin_AndGroupsFaderMoves<br>LiveViewModelTests.StatusBand_AndCommandJournal |
| [GEN-113](exigences/GEN-113.md) | S | Enregistrement des trames d'une session | Partiel |  |
| [GEN-117](exigences/GEN-117.md) | I | Toute exception journalisée | Réalisé |  |
| [GEN-118](exigences/GEN-118.md) | I | Enregistrement robuste aux refus passagers | Réalisé |  |
| [GEN-119](exigences/GEN-119.md) | M | Numéro de compilation affiché en développement | Réalisé |  |
| [GEN-130](exigences/GEN-130.md) | I | Format des fichiers documenté | Réalisé | ReferenceShowP4Tests.ReferenceShow_P4_IsValid_WithoutAnyProblem |
| [GEN-131](exigences/GEN-131.md) | I | Outil de validation d'un projet | Validé | HeadlessToolsTests.Validate_MissingPalette_GivesFileObjectAndField<br>HeadlessToolsTests.Validate_UnreadableFile_IsReported<br>ReferenceShowP4Tests.ReferenceShow_P4_IsValid_WithoutAnyProblem<br>ReferenceShowP5Tests.ReferenceShow_P5_ContentIsThere<br>(+3) |
| [GEN-132](exigences/GEN-132.md) | M | Outil qui joue une scène et la résume | Validé | HeadlessToolsTests.Run_Scene_SummarizesWhoLightsUp_AndWritesReplayableRecording |
| [GEN-133](exigences/GEN-133.md) | I | Contenu généré rangé à part, jamais écrasant | Réalisé | SceneImportTests.Merge_AddsNewScenes_NeverOverwrites_AndCategorizes |
| [MOT-001](exigences/MOT-001.md) | I | Ordre de la boucle de rendu | Réalisé |  |
| [MOT-002](exigences/MOT-002.md) | I | Budget de 5 ms par tick | Réalisé | EnginePerformanceTests.Tick_With100Fixtures20Layers40Playbacks_StaysUnderFiveMilliseconds_WithoutAllocating |
| [MOT-003](exigences/MOT-003.md) | I | Fil d'exécution dédié, sans opération bloquante | Réalisé |  |
| [MOT-004](exigences/MOT-004.md) | I | Déterminisme : aléatoire à graine journalisée | Réalisé | EffectTests.RandomShape_SameSeed_SameValues_MembersDiffer<br>ScenePlaybackTests.LoopRandom_NeverRepeatsCurrentStep_AndIsReproducibleWithSeed |
| [MOT-010](exigences/MOT-010.md) | I | Étape = fondu d'entrée + maintien | Validé | ScenePlaybackTests.Step_FadeOneSecond_HoldTwo_NextStepAtThreeSeconds |
| [MOT-011](exigences/MOT-011.md) | I | Interpolation des attributs continus selon la courbe | Validé | ScenePlaybackTests.LinearFade_ZeroToFullInTwoSeconds_EightyRegularSteps<br>ScenePlaybackTests.SCurve_IsSmoothAtBothEnds_AndCrossesHalfWayInTheMiddle<br>ScenePlaybackTests.StepChange_InterpolatesFromPreviousStepValue |
| [MOT-012](exigences/MOT-012.md) | I | Attributs discrets : bascule franche | Validé | ReferenceShowP4Tests.ColorWheel_OnlyEverShowsSlotMedians<br>ScenePlaybackTests.DiscreteAttribute_SwitchesFrankly_AtChosenPoint |
| [MOT-013](exigences/MOT-013.md) | I | Modes de boucle | Validé | ScenePlaybackTests.LoopCount_PlaysNPasses<br>ScenePlaybackTests.LoopInfinite_WrapsAround<br>ScenePlaybackTests.LoopOnce_ThenStops<br>ScenePlaybackTests.LoopPingPong_GoesBackAndForth<br>(+1) |
| [MOT-014](exigences/MOT-014.md) | I | Fin de scène : arrêt, maintien, enchaînement | Validé | ScenePlaybackTests.EndChain_LaunchesNextSceneInSameLayer<br>ScenePlaybackTests.EndHold_StaysOnLastStep<br>ScenePlaybackTests.EndStop_FadesOutWithSceneFadeOut |
| [MOT-015](exigences/MOT-015.md) | I | Vitesse de lecture | Validé | EffectTests.SceneSpeed_DoublesEffectSpeed<br>ScenePlaybackTests.Speed_Doubled_MakesStepsTwiceShorter<br>ScenesViewModelTests.Speed_IncreasedStepByStep_WhilePlaying_AppliesLive_WithoutError |
| [MOT-019](exigences/MOT-019.md) | M | Pas à pas : étape suivante / précédente | Réalisé | ScenePlaybackTests.ManualNextAndPrevious_ChangeStep |
| [MOT-030](exigences/MOT-030.md) | I | Fondu croisé dans une couche exclusive | Validé | LayerMergeTests.CrossFade_AttributeInBothScenes_InterpolatesDirectly<br>LayerMergeTests.CrossFade_AttributeOnlyInNewScene_FadesFromUnderlying |
| [MOT-031](exigences/MOT-031.md) | I | Fusion entre couches et modes d'intensité | Réalisé | LayerMergeTests.Intensity_FourModes<br>LayerMergeTests.Intensity_Htp_HighestContributionWins<br>LayerMergeTests.NonIntensity_LtpByPriority_HighestLayerWins_EvenIfLaunchedFirst<br>LayerMergeTests.NonIntensity_SamePriority_MostRecentWins |
| [MOT-032](exigences/MOT-032.md) | I | Attribut non touché = valeur par défaut | Validé | RenderChainTests.Untouched_Parameter_EmitsChannelDefault |
| [MOT-033](exigences/MOT-033.md) | I | Master de couche | Réalisé | LayerMergeTests.LayerMaster_ScalesIntensity_NotColors_UnlessOptionSet |
| [MOT-034](exigences/MOT-034.md) | M | Source de chaque valeur finale | Réalisé | RenderChainTests.Sources_ExplainWhereEachValueComesFrom |
| [MOT-040](exigences/MOT-040.md) | I | « Suit l'intensité » en fin de chaîne | Validé | GroupDimmerTests.VirtualIntensity_Rgb3Channels_FollowsTheGroupDimmer<br>ReferenceShowP4Tests.WarmWhite_OnFourPars_HasDimmerAndColor<br>RenderChainTests.FollowsIntensity_Rgb3Channels_WhiteAt80Percent_ThenGrandMasterHalf<br>ShowCompilerTests.Par3Channels_GetsVirtualIntensity_ThatItsEmittersFollow |
| [MOT-041](exigences/MOT-041.md) | I | « Allumer en coloriant » | Validé | CompiledShowPlaybackTests.WarmWhiteOnFourPars_ThenPaletteChange_UpdatesOutput<br>ControlSessionTests.Edit_ColorWithoutIntensity_AlsoLightsTheFixture<br>EffectsPanelTests.ColorEffect_LightsTargetsWithoutIntensity<br>ReferenceShowP4Tests.Trap_ColorWithoutIntensity_LeavesSevenChannelParsDark<br>(+1) |
| [MOT-050](exigences/MOT-050.md) | I | Couleur logique vers RVB | Réalisé | ColorConversionTests.Rgb_Par_TakesColorDirectly |
| [MOT-051](exigences/MOT-051.md) | I | Couleur logique vers RVBW (extraction du blanc) | Réalisé | ColorConversionTests.Rgbw_Par_WhiteLogical_GoesToWhiteEmitter_ByDefault<br>ColorConversionTests.Rgbw_WhiteModes |
| [MOT-052](exigences/MOT-052.md) | I | Couleur logique vers roue de couleur | Validé | ColorConversionTests.ColorWheel_NeverPicksHalfColors<br>ColorConversionTests.ColorWheel_Red_PicksRedSlot_AtItsMedian<br>ColorConversionTests.ColorWheel_WhiteLogical_PicksOpenPosition |
| [MOT-053](exigences/MOT-053.md) | M | UV et ambre en émetteurs indépendants | Réalisé | ColorConversionTests.Uv_Fixture_IgnoresColor_UnlessUvIsSpecified |
| [MOT-054](exigences/MOT-054.md) | M | Interpolation des couleurs sans teintes « sales » | Validé | EffectCompilerTests.ColorGroups_OnePerRgbCell<br>EffectTests.HueFade_RedToGreen_PassesThroughYellow<br>EffectsPanelTests.StepHueFade_WrittenFromProperties |
| [MOT-070](exigences/MOT-070.md) | I | Blackout | Validé | RenderChainTests.Blackout_ZeroesIntensitiesOnly_AndReleaseRestoresInstantly |
| [MOT-071](exigences/MOT-071.md) | I | Grand Master | Validé | GroupDimmerTests.GroupDimmer_AndGrandMaster_Multiply<br>RenderChainTests.GrandMaster_MultipliesIntensities_NotColors |
| [MOT-090](exigences/MOT-090.md) | I | Conversion des attributs en octets selon le patch | Réalisé | RenderChainTests.Scene_Values_AreConvertedTo8And16Bits_WithInversion<br>ShowCompilerTests.Parameters_OfReferenceRig_HaveRolesAddressesAnd16Bits |
| [MOT-091](exigences/MOT-091.md) | I | Appareils absents émis à 0 | Réalisé | RenderChainTests.AbsentFixture_IsEmittedAtZero<br>ShowCompilerTests.AbsentFixture_InActiveVenue_IsMarkedAbsent |
| [MOT-092](exigences/MOT-092.md) | I | Jumeaux : mêmes valeurs | Réalisé | RenderChainTests.Twins_ShareParameters_AndReceiveSameValues<br>ShowCompilerTests.Twins_ShareParametersOfFirstFixture |
| [MOT-093](exigences/MOT-093.md) | I | Une trame par univers à chaque tick | Réalisé | RenderChainTests.EveryTick_SubmitsAFrame_EvenWhenNothingChanges |
| [MOT-100](exigences/MOT-100.md) | I | Publication de l'état observable | Réalisé | EnginePerformanceTests.SlowSubscriber_DoesNotDelayTicks |
| [MOT-101](exigences/MOT-101.md) | I | Événements de scène et de refus | Réalisé | LayerMergeTests.UnknownScene_IsRejected_WithEventAndLogEntry |
| [MOT-103](exigences/MOT-103.md) | M | Mode sans interface piloté par scénario | Validé | HeadlessToolsTests.Run_Scene_SummarizesWhoLightsUp_AndWritesReplayableRecording<br>HeadlessToolsTests.Scenario_Parse_ResolvesNames_AndReportsBadLines |
| [PAL-001](exigences/PAL-001.md) | I | Créer une palette depuis le programmeur | Réalisé | ScenesViewModelTests.SaveAsPositionPalette_FromProgrammer |
| [PAL-002](exigences/PAL-002.md) | I | Palettes couleur par intention | Validé | ShowCompilerTests.PaletteReference_IsTranslatedPerFixture_AndModelSpecificValueWins |
| [PAL-003](exigences/PAL-003.md) | I | Palettes automatiques | Réalisé |  |
| [PAL-005](exigences/PAL-005.md) | I | Les scènes suivent les palettes | Validé | CompiledShowPlaybackTests.WarmWhiteOnFourPars_ThenPaletteChange_UpdatesOutput<br>LayerMergeTests.LoadShow_WhilePlaying_UpdatesRunningSceneValues<br>ScenesViewModelTests.UpdateArmed_NextPaletteClick_UpdatesItFromProgrammer |
| [PAL-006](exigences/PAL-006.md) | I | Suppression d'une palette utilisée | Réalisé | SceneUsageAndStoreTests.PaletteUsage_ListsSteps_AndFreezeReplacesReferenceByValue<br>ScenesViewModelTests.PaletteReference_Recorded_ThenDeletedWithFreeze |
| [PAL-007](exigences/PAL-007.md) | M | Grilles de palettes | Partiel |  |
| [PAL-009](exigences/PAL-009.md) | M | Jeu de palettes couleur par défaut | Réalisé | SceneUsageAndStoreTests.MissingFiles_GiveDefaultPalettesAndLayers |
| [SCN-001](exigences/SCN-001.md) | I | Créer, dupliquer, renommer, supprimer une scène | Validé | GamePanelsTests.NewScene_InTheColumnLayer_OpensTheEditor_Undoable<br>SceneUsageAndStoreTests.Stores_RoundTrip_AllValueForms<br>ScenesViewModelTests.NewScene_RecordColor_LightsWhenColoring_ThenPlays |
| [SCN-002](exigences/SCN-002.md) | I | Étapes : ajouter, insérer, dupliquer, supprimer, réordonner | Validé | EditBenchPanelsTests.Properties_EditFieldsAndSteps_AreSavedAndUndoable<br>ScenesViewModelTests.Steps_AddDuplicateMove_AndGroupTiming |
| [SCN-003](exigences/SCN-003.md) | I | Durées d'une étape et courbe | Validé |  |
| [SCN-004](exigences/SCN-004.md) | I | Modification groupée des durées | Réalisé | ScenesViewModelTests.Steps_AddDuplicateMove_AndGroupTiming |
| [SCN-005](exigences/SCN-005.md) | I | Paramètres de lecture d'une scène | Réalisé |  |
| [SCN-007](exigences/SCN-007.md) | I | Cibles : appareil, cellule, sélection | Validé | ShowCompilerTests.AutoSelection_ByCategory_IncludesFixturePatchedLater<br>ShowCompilerTests.AutoSelection_WithCell_TargetsThatCellOfEachMember<br>ShowCompilerTests.FixtureValue_WinsOverSelectionValue_WhateverTheOrder<br>ShowCompilerTests.ManualSelection_KeepsItsOrder_ForTheFan |
| [SCN-008](exigences/SCN-008.md) | I | Valeur directe, palette ou plage | Réalisé | EditBenchPanelsTests.Settings_PaletteAndRange_AreApplied<br>ScenesViewModelTests.PaletteReference_Recorded_ThenDeletedWithFreeze<br>ShowCompilerTests.PaletteReference_IsTranslatedPerFixture_AndModelSpecificValueWins |
| [SCN-009](exigences/SCN-009.md) | I | Drapeau « Visible en Live » | Réalisé |  |
| [SCN-010](exigences/SCN-010.md) | M | Retard par membre (« fan ») | Validé | ReferenceShowP4Tests.Wave_StartsEachParHalfASecondAfterItsNeighbour<br>ScenePlaybackTests.PerValueDelay_SpreadsTheFade_AcrossMembers<br>ScenesViewModelTests.Programmer_FanAndOwnFade_OnIndividuallySelectedFixtures_AndOnSelection<br>ShowCompilerTests.ManualSelection_KeepsItsOrder_ForTheFan |
| [SCN-011](exigences/SCN-011.md) | M | Fondu propre à un attribut | Réalisé | ScenePlaybackTests.PerAttributeFade_ColorsSlow_PositionInstant<br>ScenesViewModelTests.Programmer_FanAndOwnFade_OnIndividuallySelectedFixtures_AndOnSelection |
| [SCN-012](exigences/SCN-012.md) | M | Catégories et filtre des scènes | Réalisé | ScenesViewModelTests.Filter_ByNameAndCategory |
| [SCN-013](exigences/SCN-013.md) | M | Rapport des utilisations d'une scène | Réalisé | SceneUsageAndStoreTests.SceneUsage_ListsScenesThatChainToIt<br>ScenesViewModelTests.DeleteScene_ThenUndo_RestoresIt |
| [SCN-030](exigences/SCN-030.md) | I | Sélection d'appareils dans le programmeur | Partiel | ScenesViewModelTests.Programmer_SelectionShortcut_ThenColor_OverridesAttributesLive |
| [SCN-031](exigences/SCN-031.md) | I | Outils d'attributs adaptés à la sélection | Partiel | ScenesViewModelTests.Programmer_SelectionShortcut_ThenColor_OverridesAttributesLive |
| [SCN-032](exigences/SCN-032.md) | I | Seuls les attributs modifiés sont enregistrés | Validé | ScenesViewModelTests.OnlyTouchedAttributes_AreRecorded_AndRemoveTakesOneOut |
| [SCN-033](exigences/SCN-033.md) | I | Enregistrer : remplacer, fusionner, nouvelle étape | Validé | ScenesViewModelTests.NewScene_RecordColor_LightsWhenColoring_ThenPlays |
| [SCN-034](exigences/SCN-034.md) | I | Tester : seule ou dans son contexte | Validé | EffectsPanelTests.Launch_InEdit_SwitchesToLive_SoTheSceneIsSeen<br>LayerMergeTests.Solo_MasksOtherPlaybacks_UntilStopped |
| [SCN-035](exigences/SCN-035.md) | I | Aveugle | Validé | ScenesViewModelTests.Blind_SendsProgrammerToPreviewOnly |
| [SCN-036](exigences/SCN-036.md) | I | Option « allumer en coloriant » | Validé | ScenesViewModelTests.NewScene_RecordColor_LightsWhenColoring_ThenPlays |
| [SCN-037](exigences/SCN-037.md) | M | Enregistrer depuis la sortie | Réalisé | ScenesViewModelTests.CaptureOutput_ThenCopyPasteMirror |
| [SCN-038](exigences/SCN-038.md) | M | Copier / coller, miroir | Réalisé | ScenesViewModelTests.CaptureOutput_ThenCopyPasteMirror |
| [SCN-039](exigences/SCN-039.md) | I | Annuler / rétablir dans l'éditeur de scènes | Validé | ScenesViewModelTests.DeleteScene_ThenUndo_RestoresIt |

## P5 – 73 exigences, 51 couvertes par des tests automatiques

> Partiel : 5 · Reporté (chantier ergonomie) : 10 · Réalisé : 21 · Validé : 37

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [BIB-101](exigences/BIB-101.md) | I | Plage « Pas de strobe » du LPC008S, du LPC120 et de la LCB803 | Validé | SafetyCompilerTests.StrobeChannels_OfReferenceRig_UseTheNoStrobeRanges<br>SafetyTests.Strobe_ValueInNoStrobeRange_IsNeverCounted |
| [CONS-061](exigences/CONS-061.md) | S | Une page de console peut être affectée aux faders d'un APC mini | Reporté (chantier ergonomie) |  |
| [COU-001](exigences/COU-001.md) | I | Créer, renommer, réordonner | Validé | LayersEditorViewModelTests.AddRenameReorderSave_UpdatesPrioritiesAndFile<br>LayersEditorViewModelTests.Delete_LayerWithScenes_IsRefused<br>LayersEditorViewModelTests.Editor_ListsDefaultLayers_InPriorityOrder |
| [COU-002](exigences/COU-002.md) | I | Une scène appartient à une couche | Réalisé | LayersEditorViewModelTests.Delete_LayerWithScenes_IsRefused |
| [COU-003](exigences/COU-003.md) | I | Exclusivité | Validé | LayerMergeTests.TwoLaunches_InSameTick_SameExclusiveLayer_LastOneWins<br>ReferenceShowP5Tests.Layers_IntensityTimesColor_LightThePars_ColorAloneDoesNot |
| [COU-004](exigences/COU-004.md) | I | Couche non exclusive | Réalisé |  |
| [COU-005](exigences/COU-005.md) | I | Couche de type Flash | Validé | GamePanelsTests.FlashLayer_PlaysOnlyWhileHeld<br>LiveCommandTests.Flash_WhileHeld_OverridesAllLayers_ThenReturnsInstantly<br>LiveViewModelTests.FlashLayerScene_PlaysOnlyWhileHeld<br>MidiControllerTests.Pad_OfFlashLayer_FlashesWhileHeld<br>(+1) |
| [COU-006](exigences/COU-006.md) | I | Modèle de couches par défaut pour un nouveau projet | Réalisé | LayersEditorViewModelTests.Editor_ListsDefaultLayers_InPriorityOrder<br>SceneUsageAndStoreTests.MissingFiles_GiveDefaultPalettesAndLayers |
| [COU-007](exigences/COU-007.md) | M | Arrêter la couche | Réalisé | LiveCommandTests.StopAll_SparesProtectedLayers_UnlessEverything |
| [COU-008](exigences/COU-008.md) | M | Avertissement | Validé | ProjectProblemsTests.ProjectProblems_ShowTheOutOfFamilyWarning<br>ReferenceShowP4Tests.ReferenceShow_P4_IsValid_WithoutAnyProblem |
| [COU-009](exigences/COU-009.md) | S | Scène de repos par couche | Validé | LayersEditorViewModelTests.RestScene_ChoicesAreTheLayerScenes<br>LiveCommandTests.RestScene_PlaysWhenTheLayerIsEmpty |
| [GEN-054](exigences/GEN-054.md) | I | Sauvegarde automatique du projet ouvert | Validé | ReliabilityTests.Versions_OnlyWhenChanged_KeepTen |
| [GEN-055](exigences/GEN-055.md) | M | Conservation des N dernières versions du projet | Validé | ReliabilityTests.Restore_BringsBackTheFiles_AndKeepsTheCurrentStateAsAVersion<br>ReliabilityTests.Versions_OnlyWhenChanged_KeepTen |
| [GEN-057](exigences/GEN-057.md) | M | Export / import d'un projet complet sous forme d'archive unique | Reporté (chantier ergonomie) |  |
| [GEN-061](exigences/GEN-061.md) | I | Fondu au noir à la fermeture | Validé |  |
| [GEN-062](exigences/GEN-062.md) | I | Le passage Atelier ↔ Live ne doit jamais interrompre la restitution en cours | Réalisé |  |
| [GEN-064](exigences/GEN-064.md) | I | Démarrage jusqu'à « prêt en Live » en moins de 10 s | Validé |  |
| [GEN-065](exigences/GEN-065.md) | M | Fenêtre de démarrage avec étapes et pourcentage | Réalisé |  |
| [GEN-070](exigences/GEN-070.md) | I | Toute entrée | Réalisé |  |
| [GEN-071](exigences/GEN-071.md) | I | Raccourcis clavier globaux en Live, actifs quel que soit le focus | Partiel | LiveViewModelTests.Keys_FlashHeldWithAutoRepeat_ThenReleased |
| [GEN-072](exigences/GEN-072.md) | M | Les deux modèles d'APC mini sont reconnus automatiquement et peuvent être branchés simulta | Validé | MidiControllerTests.Profiles_RecognizeBothModels<br>MidiServiceTests.BothModels_AreDetected_AndDriveTheEngine |
| [GEN-073](exigences/GEN-073.md) | M | Débrancher / rebrancher un contrôleur MIDI en cours de soirée est géré sans redémarrage | Validé | MidiServiceTests.Unplug_ThenReplug_RestoresTheLeds |
| [GEN-074](exigences/GEN-074.md) | S | Les affectations MIDI sont modifiables par « apprentissage » | Reporté (chantier ergonomie) |  |
| [GEN-083](exigences/GEN-083.md) | I | Strobe | Réalisé | SafetyCompilerTests.Settings_AreCarriedToTheEngine<br>SafetyTests.Strobe_Forbidden_ForcesRestImmediately<br>SafetyTests.Strobe_MaxSpeed_CapsTheProgressiveRange<br>SafetyTests.Strobe_Requested30s_CutAfter10s_ThenPause_ThenAllowedAgain |
| [GEN-084](exigences/GEN-084.md) | I | Fumée | Validé | LiveCommandTests.Smoke_HoldAndBurst_GoThroughTheLimiter<br>SafetyCompilerTests.Settings_AreCarriedToTheEngine<br>SafetyTests.Smoke_Held20s_CutAt10s_ThenRest30s<br>SafetyTests.Smoke_ShortPuff_RestsThreeTimesItsDuration |
| [GEN-085](exigences/GEN-085.md) | I | Zones interdites Pan/Tilt par lieu et par lyre | Réalisé | SafetyTests.Zone_TargetInside_IsBroughtToTheNearestEdge |
| [GEN-086](exigences/GEN-086.md) | M | Un signal visuel permanent en Live indique toute limite de sûreté active ou tout verrou | Réalisé |  |
| [GEN-094](exigences/GEN-094.md) | M | Utilisation CPU moyenne < 15 % en Live | Validé |  |
| [GEN-095](exigences/GEN-095.md) | M | Reprise après plantage | Validé | ReliabilityTests.AbruptStop_ThenRestart_OffersToResumeTheSameScenes<br>ReliabilityTests.CleanStop_OffersNothing |
| [INST-053](exigences/INST-053.md) | I | Zones interdites par lyre, définies en visant à la main | Réalisé | SafetyCompilerTests.Zones_OfActiveVenue_TargetThePanTiltParameters<br>ZonesEditorViewModelTests.AddWithoutLyre_ExplainsWhatToDo<br>ZonesEditorViewModelTests.AimTwoCorners_Save_ThenTheEngineKeepsTheLyreOut |
| [INST-054](exigences/INST-054.md) | I | Les palettes de position sont stockées par lieu | Validé | VenuePaletteTests.CopyVenue_CopiesTheEffectivePositions |
| [INST-070](exigences/INST-070.md) | M | L'assistant enchaîne les étapes ci-dessus, chacune pouvant être passée | Reporté (chantier ergonomie) |  |
| [INST-071](exigences/INST-071.md) | M | Test appareil par appareil avec résultat | Reporté (chantier ergonomie) |  |
| [INST-072](exigences/INST-072.md) | M | Calibration des positions | Partiel |  |
| [LIVE-001](exigences/LIVE-001.md) | I | Bandeau d'état permanent | Partiel | LiveViewModelTests.StatusBand_AndCommandJournal |
| [LIVE-002](exigences/LIVE-002.md) | I | Colonnes de couches | Réalisé | GamePanelsTests.LayerMaster_SendsCommand_StopLayer_Stops<br>LiveViewModelTests.ClickScene_Launches_ClickAgain_Stops<br>LiveViewModelTests.Columns_AreTheLayers_WithTheirLiveScenes_InOrder |
| [LIVE-003](exigences/LIVE-003.md) | I | Un clic sur une scène la lance | Validé | GamePanelsTests.Columns_AreAllLayersByPriority_WithAllTheirScenes<br>GamePanelsTests.Press_LaunchesThenStops_TheEngineDecides<br>LiveCommandTests.LaunchWithStopIfPlaying_TogglesInTheEngine_EvenWhenSentTwiceBeforeATick<br>LiveUvRepeatTests.FullOn_ThenTenClicksOnUv_EachClickTogglesTheUv<br>(+4) |
| [LIVE-004](exigences/LIVE-004.md) | I | Actions permanentes toujours visibles | Validé | LiveViewModelTests.Keys_FlashHeldWithAutoRepeat_ThenReleased |
| [LIVE-005](exigences/LIVE-005.md) | I | Palettes rapides | Validé | LiveViewModelTests.QuickPalette_OverridesTheSelection_ThenReleaseGivesBack |
| [LIVE-006](exigences/LIVE-006.md) | M | Disposition personnalisable | Partiel |  |
| [LIVE-007](exigences/LIVE-007.md) | M | Mini-simulateur optionnel dans l'écran Live | Reporté (chantier ergonomie) |  |
| [LIVE-008](exigences/LIVE-008.md) | I | Indication visible de toute limite de sûreté active et de tout verrou | Réalisé | LiveUvRepeatTests.SmokeCut_PillShowsTheTimeLeftBeforeTheRestEnds |
| [LIVE-009](exigences/LIVE-009.md) | M | Journal défilant des derniers événements | Réalisé | GamePanelsTests.Journal_ShowsLaunchedScenes<br>LiveViewModelTests.Journal_ShowsSceneStarts |
| [LIVE-010](exigences/LIVE-010.md) | I | Alerte non bloquante et visible si la sortie est déconnectée ou si un module est en erreur | Validé | LiveViewModelTests.StatusBand_AndCommandJournal |
| [LIVE-011](exigences/LIVE-011.md) | M | Accès à l'assistant d'installation | Reporté (chantier ergonomie) |  |
| [LIVE-040](exigences/LIVE-040.md) | I | Raccourcis du tableau ci-dessus | Validé | LiveViewModelTests.Keys_ArrowsChooseLayer_DigitsLaunchItsScenes_GFreezes_PageDownLowersMaster<br>LiveViewModelTests.Keys_FlashHeldWithAutoRepeat_ThenReleased<br>LiveViewModelTests.Keys_UpDownArrows_DriveTheMasterOfTheFramedLayer<br>LiveViewModelTests.LayerMaster_RefreshBeforeTheEngineTick_DoesNotJumpBack |
| [LIVE-041](exigences/LIVE-041.md) | S | Raccourcis personnalisables | Reporté (chantier ergonomie) |  |
| [LIVE-060](exigences/LIVE-060.md) | I | L'écran Live se rafraîchit à ≥ 20 images/s sans affecter le moteur | Réalisé | LiveViewModelTests.Refresh_IsFarUnderTheFrameBudget |
| [LIVE-061](exigences/LIVE-061.md) | I | Latence clic → sortie < 50 ms | Validé |  |
| [MIDI-001](exigences/MIDI-001.md) | I | Détection automatique des APC mini MK1 et MK2 | Validé | MidiControllerTests.Profiles_RecognizeBothModels<br>MidiServiceTests.BothModels_AreDetected_AndDriveTheEngine |
| [MIDI-002](exigences/MIDI-002.md) | I | Affectation par défaut du §3 | Validé | MidiControllerTests.BottomButton_StopsItsLayer_AndRightButtons_AreTheLiveActions<br>MidiControllerTests.Pad_LaunchesTheSceneOfItsColumnAndRow_WithMidiOrigin<br>MidiControllerTests.Pad_OfFlashLayer_FlashesWhileHeld<br>MidiControllerTests.ShiftBottom_ChangesTheScenePage |
| [MIDI-003](exigences/MIDI-003.md) | I | Retour lumineux du §4, mis à jour à chaque changement d'état | Validé | MidiControllerTests.Leds_AfterReset_AreAllSentAgain<br>MidiControllerTests.Leds_Mk1_YellowAvailable_GreenActive_BlinkingWhileFadingIn<br>MidiControllerTests.Leds_Mk2_UseTheSceneColor_DimWhenAvailable_FullWhenActive<br>MidiServiceTests.Dispose_TurnsAllLedsOff<br>(+1) |
| [MIDI-004](exigences/MIDI-004.md) | I | Reprise douce des faders | Validé | MidiControllerTests.Fader_LosesControl_WhenTheValueIsChangedElsewhere<br>MidiControllerTests.Fader_MovedFast_KeepsControl_WhileTheEngineLagsBehind<br>MidiControllerTests.Fader_TakesOverOnlyAfterCrossingTheCurrentValue |
| [MIDI-005](exigences/MIDI-005.md) | I | Les deux contrôleurs peuvent être branchés simultanément, avec des affectations différente | Réalisé | MidiControllerTests.Binding_ReplacesTheDefault_ForItsModelOnly<br>MidiServiceTests.BothModels_AreDetected_AndDriveTheEngine<br>MidiServiceTests.WithDimmers_TheSecondPlatineServesThemWhileTheFirstKeepsTheScenes |
| [MIDI-006](exigences/MIDI-006.md) | I | Débranchement / rebranchement à chaud | Validé | MidiServiceTests.Unplug_ThenReplug_RestoresTheLeds |
| [MIDI-007](exigences/MIDI-007.md) | M | Affectations modifiables et enregistrées dans le projet | Réalisé | MidiControllerTests.Binding_ReplacesTheDefault_ForItsModelOnly<br>MidiControllerTests.BlackoutToggle_ByBinding_TogglesOnPressOnly<br>MidiControllerTests.Control_Parses<br>MidiControllerTests.Control_Unreadable_IsNull |
| [MIDI-008](exigences/MIDI-008.md) | S | Apprentissage | Reporté (chantier ergonomie) |  |
| [MIDI-009](exigences/MIDI-009.md) | S | Disposition alternative « palettes » | Reporté (chantier ergonomie) |  |
| [MIDI-010](exigences/MIDI-010.md) | M | Sur MK2, la couleur des pads reprend la couleur des scènes | Validé | MidiControllerTests.Leds_Mk2_UseTheSceneColor_DimWhenAvailable_FullWhenActive<br>MidiControllerTests.NearestPalette_IgnoresBrightness |
| [MIDI-011](exigences/MIDI-011.md) | I | Blackout du contrôleur tant que maintenu (comme Daslight) | Validé | MidiControllerTests.BlackoutNote_IsMomentary_EvenIfBlackoutWasAlreadyOnFromTheScreen<br>MidiControllerTests.BlackoutToggle_ByBinding_TogglesOnPressOnly |
| [MOT-042](exigences/MOT-042.md) | I | Le modèle de couches par défaut | Validé | DefaultContentTests.NewProject_HasFullOnScene_InIntensityLayer_LightingEveryFixture<br>ReferenceShowP5Tests.Layers_IntensityTimesColor_LightThePars_ColorAloneDoesNot |
| [MOT-072](exigences/MOT-072.md) | I | Flash | Validé | LiveCommandTests.Flash_OfALayerScene_DoesNotReplaceTheScenePlayingInThatLayer<br>LiveCommandTests.Flash_WhileHeld_OverridesAllLayers_ThenReturnsInstantly<br>ReferenceShowP5Tests.PartialBlackoutFlash_KeepsTheUv_ThenGivesBack |
| [MOT-073](exigences/MOT-073.md) | I | Figer | Validé | LiveCommandTests.Freeze_KeepsOutput_WhilePlaybacksGoOn_BlackoutStillActive<br>LiveCommandTests.Freeze_WithSuspendedPlaybacks_StopsTheirProgress |
| [MOT-080](exigences/MOT-080.md) | I | Limiteur de strobe | Validé | ReferenceShowP5Tests.StrobeScene_IsCutAfterTenSeconds_ThenResumesAfterThePause<br>SafetyCompilerTests.StrobeChannels_OfReferenceRig_UseTheNoStrobeRanges<br>SafetyTests.Strobe_Forbidden_ForcesRestImmediately<br>SafetyTests.Strobe_MaxSpeed_CapsTheProgressiveRange<br>(+3) |
| [MOT-081](exigences/MOT-081.md) | I | Limiteur de fumée | Validé | LiveUvRepeatTests.SmokeBurst_AfterTheRest_Emits3sWithoutAnyLimit<br>LiveUvRepeatTests.SmokeCut_PillShowsTheTimeLeftBeforeTheRestEnds<br>ReferenceShowP5Tests.LongSmoke_IsCutAtTenSeconds<br>SafetyCompilerTests.SmokeChannel_OfReferenceRig_IsChannel180<br>(+2) |
| [MOT-082](exigences/MOT-082.md) | I | Zones interdites | Partiel | ReferenceShowP5Tests.Trap_LyreTowardsThePublic_StopsAtTheZoneEdge<br>ReferenceShowP6Tests.Trap_BigCircle_NeverCrossesTheForbiddenZone<br>SafetyCompilerTests.Zones_OfActiveVenue_TargetThePanTiltParameters<br>SafetyTests.NearestAllowed_WithOverlappingZones_AvoidsAllOfThem<br>(+3) |
| [MOT-083](exigences/MOT-083.md) | I | Toute intervention d'un limiteur publie LimiteSécuritéAtteinte | Réalisé | SafetyTests.Strobe_Requested30s_CutAfter10s_ThenPause_ThenAllowedAgain<br>SafetyTests.Zone_TargetInside_IsBroughtToTheNearestEdge |
| [MOT-102](exigences/MOT-102.md) | M | Instantané de reprise | Réalisé | ReliabilityTests.AbruptStop_ThenRestart_OffersToResumeTheSameScenes |
| [PAL-004](exigences/PAL-004.md) | I | Palettes de position par lieu | Validé | VenuePaletteTests.GenericVenue_HasNoKey<br>VenuePaletteTests.Merge_ReplacesOnlyTheCapturedFixtures_InTheActiveVenue_AndAddsAFallback<br>VenuePaletteTests.SameScene_TwoVenues_DifferentPositions_WithGenericFallbackSignalled |
| [PAL-008](exigences/PAL-008.md) | M | Palettes de position manquantes dans un lieu | Validé | VenuePaletteTests.SameScene_TwoVenues_DifferentPositions_WithGenericFallbackSignalled |
| [PAL-010](exigences/PAL-010.md) | S | Palettes de combinaisons de couleurs | Validé | EffectCompilerTests.Alternate_FromTheme_SteppedTables<br>EffectCompilerTests.Themes_AddedToProjectsWithoutAny_NotReAddedWhenOneExists<br>EffectsPanelTests.ThemeEditor_NewTheme_FromWindow_UsedByTheEffect_DefaultThemesLocked<br>ReferenceShowP6Tests.ReferenceShow_P6_ContentIsThere_AndValid |
| [SORT-065](exigences/SORT-065.md) | M | Enregistrement des trames visible, chemin copiable | Réalisé |  |
| [SORT-066](exigences/SORT-066.md) | M | Journal de l'enregistrement : actions, commandes et canaux entrelacés | Réalisé | LiveUvRepeatTests.Recording_JournalInterleavesClicksCommandsAndChannelChanges |

## ERG – 27 exigences, 19 couvertes par des tests automatiques

> Abandonné : 4 · En cours : 1 · Réalisé : 2 · Validé : 20

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [ERG-001](exigences/ERG-001.md) | I | Ancrage de panneaux | Validé | ControlLayoutTests.DefaultLayout_IsTheGameScreen_FiveBigPanels_NoEditingPanel<br>ControlLayoutTests.Floating_OrClosedInItsWindow_ComesBackHome_AndTheEmptyWindowGoes<br>ControlLayoutTests.InitLayout_GivesEachPanelItsContext<br>LayoutStoreTests.ShowPanel_AbsentFromLayout_IsCreatedInItsHomeGroup |
| [ERG-002](exigences/ERG-002.md) | I | Enregistrement de la disposition | Validé | ControlLayoutTests.Load_Missing_IsNullWithoutMessage_Unreadable_IsSetAside<br>ControlLayoutTests.SaveThenLoad_ClosedPanel_StaysClosed_AndComesBackHome<br>LayoutStoreTests.Delete_ThenLoad_GivesNothing<br>LayoutStoreTests.Load_MissingFile_GivesNothingAndNoMessage<br>(+5) |
| [ERG-003](exigences/ERG-003.md) | I | Grille Pan / Tilt | Validé | EditBenchPanelsTests.Settings_AimTwoLyres_Relative_EachGetsItsOwnValues<br>PanTiltGeometryTests.FromCorners_AnyOrder_GivesOrderedRect<br>PanTiltGeometryTests.FromScreen_OutsideGrid_IsClamped<br>PanTiltGeometryTests.HitTest_HandlesBodyAndOutside<br>(+7) |
| [ERG-004](exigences/ERG-004.md) | I | Sélecteur de couleur | Validé | ColorPickerLayoutTests.BrightnessAt_BarTopAndBottom_IsFullAndBlack<br>ColorPickerLayoutTests.For_Size_SquareBarAndSwatchesDoNotOverlap<br>ColorPickerLayoutTests.HueSaturationAt_Corners_GivesHueAcrossAndSaturationDown<br>ColorPickerLayoutTests.SquarePoint_IsInverseOfHueSaturationAt<br>(+4) |
| [ERG-005](exigences/ERG-005.md) | M | Galerie des composants | Réalisé |  |
| [ERG-006](exigences/ERG-006.md) | I | Mesures du prototype | En cours |  |
| [ERG-007](exigences/ERG-007.md) | I | Maquettes de la disposition Contrôle | Réalisé |  |
| [ERG-008](exigences/ERG-008.md) | I | 8e couche par défaut « Libre » | Validé | SceneUsageAndStoreTests.DefaultLayers_HaveEightNonFlashAndFlash_WithFreeLayerWithoutFamilies |
| [ERG-009](exigences/ERG-009.md) | I | Identité visuelle | Validé |  |
| [ERG-010](exigences/ERG-010.md) | I | Mode LIVE de l'écran Contrôle | Abandonné | ControlSessionTests.EditAndBlind_WithoutScene_AreRefusedWithAReason<br>ControlSessionTests.Live_OverrideStays_WhenASceneOnTheSameChannelIsLaunched<br>ControlSessionTests.Live_SettingIsTemporaryOverride_ReleasedByReleaseAll<br>ControlSessionTests.ProjectReopened_BackToLive_WithNothingPushed |
| [ERG-011](exigences/ERG-011.md) | I | Mode ÉDITION | Abandonné | ControlSessionTests.Edit_ColorWithoutIntensity_AlsoLightsTheFixture<br>ControlSessionTests.Edit_ShowsTheStepOnOutput_UntilBackToLive<br>ControlSessionTests.Edit_WritesIntoStep_OneUndoEntryPerGesture<br>ControlSessionTests.Remove_InEdit_TakesTheFixtureOutOfTheStep<br>(+1) |
| [ERG-012](exigences/ERG-012.md) | I | Mode AVEUGLE | Abandonné | ControlSessionTests.Blind_WritesStep_WithoutChangingTheOutput |
| [ERG-013](exigences/ERG-013.md) | I | Zones du lieu dans l'écran Contrôle | Validé | ControlSessionTests.EditVenues_IsOneUndoableGesture<br>EditBenchPanelsTests.Settings_Zones_DrawAllowedAndForbidden_Modify_Delete |
| [ERG-014](exigences/ERG-014.md) | I | Plan des appareils = la sélection | Validé | EditBenchPanelsTests.Plan_ClickCtrlClickRectangle_BuildTheSharedSelection<br>EditBenchPanelsTests.Plan_QuickSelections_EveryOther_Invert_None<br>EditBenchPanelsTests.Plan_ShowsOutput_OrPreviewInBlind<br>PlanAndStepStripTests.FixturesIn_Rectangle_KeepsOnlyFixturesInside |
| [ERG-015](exigences/ERG-015.md) | M | Bande d'étapes | Validé | PlanAndStepStripTests.Widths_AreProportionalToDurations_AndFill<br>PlanAndStepStripTests.Widths_ShortOrZeroStep_KeepsMinimumWidth<br>PlanAndStepStripTests.Widths_TooNarrow_EveryCellAtMinimum |
| [ERG-016](exigences/ERG-016.md) | I | Propriétés de la scène éditée | Validé | ControlSessionTests.ChangeScenes_DeleteEditedScene_BackToLive_AndUndoBringsItBack<br>ControlSessionTests.UpdateScene_StepsAndProperties_AreUndoable<br>EditBenchPanelsTests.Properties_EditFieldsAndSteps_AreSavedAndUndoable<br>EditBenchPanelsTests.Properties_StepContent_IsReadable |
| [ERG-017](exigences/ERG-017.md) | I | Zone permise | Validé | AllowedZoneStoreTests.SaveThenLoad_AllowedFlag_RoundTrips_AndOldFilesReadAsForbidden<br>EditBenchPanelsTests.Settings_Zones_ChosenFromTheList_Renamed_Deleted<br>EditBenchPanelsTests.Settings_Zones_DrawAllowedAndForbidden_Modify_Delete<br>PanTiltGeometryTests.PickZone_BigSelected_ClickInTheSmall_TakesTheSmall_ButItsHandlesStillWin<br>(+4) |
| [ERG-018](exigences/ERG-018.md) | I | Colonnes de l'écran Contrôle | Validé | GamePanelsTests.Columns_AreAllLayersByPriority_WithAllTheirScenes<br>GamePanelsTests.Compact_IsKeptOnThisComputer<br>GamePanelsTests.ContextMenu_RenameDuplicateColorLayerHideDelete<br>GamePanelsTests.FlashLayer_PlaysOnlyWhileHeld<br>(+6) |
| [ERG-019](exigences/ERG-019.md) | I | Réglages des appareils | Validé | EditBenchPanelsTests.Settings_AimTwoLyres_Relative_EachGetsItsOwnValues<br>EditBenchPanelsTests.Settings_ColorInLive_OverridesOutput_AndKeepsTheRequestedColorShown<br>EditBenchPanelsTests.Settings_HeaderSaysWhereSettingsGo<br>EditBenchPanelsTests.Settings_IntensityInEdit_IsWrittenAfterTheGesture<br>(+4) |
| [ERG-020](exigences/ERG-020.md) | I | Démonstration | Validé |  |
| [ERG-021](exigences/ERG-021.md) | I | Verrou soirée | Validé | DraftSessionTests.BeginDraft_IsRefusedUnderTheEveningLock_OrForAnUnknownScene<br>EditorViewModelTests.EveningLock_RefusesToOpen<br>GameViewModelTests.LockedEveningLock_RefusesTheEditWindow_WithItsReason_ButStillPlays<br>GameViewModelTests.TheEveningLock_CannotBeSetWhileTheEditorIsOpen |
| [ERG-022](exigences/ERG-022.md) | M | Taille de l'interface | Validé | PreferencesAndProjectTests.UiScale_DefaultsTo100Percent_AndRoundTrips |
| [ERG-023](exigences/ERG-023.md) | I | Looks | Validé | GamePanelsTests.Looks_CaptureWhatPlays_ThenReplayItFromAnotherState<br>GamePanelsTests.Looks_Capture_LeavesTheGrandMasterToTheOperator<br>GamePanelsTests.Looks_FunctionKeys_PlayByRank<br>GamePanelsTests.Looks_RenameColorDelete_AndLockRefusesEditingButNotPlaying<br>(+3) |
| [ERG-024](exigences/ERG-024.md) | I | Disposition Spectacle | Abandonné |  |
| [ERG-025](exigences/ERG-025.md) | M | Scènes resserrées | Validé |  |
| [ERG-026](exigences/ERG-026.md) | I | Stop et Tout stopper | Validé | GameViewModelTests.StopAndStopAll_StayAvailable_LockComprised |
| [ERG-027](exigences/ERG-027.md) | M | Marges | Validé |  |

## P6 – 19 exigences, 19 couvertes par des tests automatiques

> Réalisé : 2 · Validé : 17

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [EFF-001](exigences/EFF-001.md) | I | Ajouter un ou plusieurs effets à une étape, avec les paramètres du §6 | Validé | EffectCompilerTests.Problems_Reported_EffectDropped<br>EffectCompilerTests.Wave_OnFourPars_OneDimmerPerMember_InPatchOrder<br>EffectTests.EffectOnlyInFirstStep_FadesOutWithSecondStepFade<br>EffectTests.SameEffectInTwoSteps_ContinuesWithoutRestart<br>(+4) |
| [EFF-002](exigences/EFF-002.md) | I | Formes d'intensité | Validé | EffectCompilerTests.Wave_OnFourPars_OneDimmerPerMember_InPatchOrder<br>EffectTests.RandomShape_SameSeed_SameValues_MembersDiffer<br>EffectTests.Shapes_ExpectedOffsets<br>EffectTests.Sine_FourPars_NinetyDegreesApart_ExpectedValues<br>(+2) |
| [EFF-003](exigences/EFF-003.md) | I | Formes de position | Validé | EffectCompilerTests.Circle_AroundPositionPalette_CenterFromPalette_SizeInDegrees<br>EffectTests.PositionShapes_CircleAndEight<br>EffectTests.RelativeCircle_AroundStepPosition<br>EffectsPanelTests.PositionShape_ShowsPlane_AndDegrees<br>(+1) |
| [EFF-004](exigences/EFF-004.md) | I | Formes de couleur | Validé | EffectCompilerTests.Alternate_FromTheme_SteppedTables<br>EffectCompilerTests.Gradient_FromPaletteColors<br>EffectCompilerTests.MultiHead_64Channels_TwelveHeads_Rainbow<br>EffectCompilerTests.Rainbow_PlayedByEngine_ColorChangesOverCycle<br>(+3) |
| [EFF-005](exigences/EFF-005.md) | I | Phase répartie selon l'ordre de la sélection, modes linéaire / miroir / groupes / aléatoir | Validé | EffectCompilerTests.Lag_Mirror_OddCount_CenterLeads<br>EffectCompilerTests.Lag_PhaseModes<br>EffectCompilerTests.Lag_Random_SameEffect_SameOrder_AllDistinct<br>EffectTests.Directions_BackwardAndPingPong<br>(+2) |
| [EFF-006](exigences/EFF-006.md) | I | Aperçu en direct au simulateur pendant le réglage des paramètres | Validé | EffectTests.ShowStep_PinsStepWithEffects_AboveLayers_NotListedAsPlaying<br>EffectsPanelTests.Blind_EffectPlaysOnPreviewOnly<br>EffectsPanelTests.Edit_AddWaveFromLibrary_OnPlanSelection_PlaysOnOutputAtOnce_UndoRemovesIt |
| [EFF-007](exigences/EFF-007.md) | M | Bibliothèque d'effets prédéfinis | Validé | EffectCompilerTests.Library_Apply_CopiesWithNewIdAndTarget<br>EffectsPanelTests.Edit_AddWaveFromLibrary_OnPlanSelection_PlaysOnOutputAtOnce_UndoRemovesIt<br>EffectsPanelTests.Library_TemplateRemovedByMistake_ComesBackWithRestore<br>EffectsPanelTests.SaveAsTemplate_AddsToProjectLibrary_WithoutTargets |
| [EFF-008](exigences/EFF-008.md) | M | Effets sur cellules | Validé | EffectCompilerTests.MultiHead_64Channels_TwelveHeads_Rainbow<br>EffectCompilerTests.PerCell_OnBars_EachSegmentIsAMember<br>ReferenceShowP6Tests.MultiHead_64Channels_HeadsShowDifferentColors<br>ReferenceShowP6Tests.SegmentChase_OneSectionOfTheBarsAtATime<br>(+1) |
| [EFF-009](exigences/EFF-009.md) | S | Combinaison de deux effets sur un même attribut | Réalisé | EffectTests.TwoRelativeEffects_SameAttribute_Add |
| [EFF-011](exigences/EFF-011.md) | I | Effet d'intensité visible | Validé | EffectsPanelTests.IntensityEffect_OnNewScene_GivesWhiteToUncoloredTargets_KeepsExistingColors |
| [EFF-012](exigences/EFF-012.md) | I | Type d'effet lisible et formes limitées à la famille | Validé | EffectsPanelTests.Edit_ShapesAreLimitedToTheFamily_AndNameIsMarkedModified |
| [ERG-028](exigences/ERG-028.md) | M | Molette | Validé | EffectsPanelTests.Dial_Fraction<br>EffectsPanelTests.Dial_Parse_TypedValues<br>EffectsPanelTests.Dials_WriteIntoTheEffect_OneGesture |
| [ERG-029](exigences/ERG-029.md) | I | Panneau Effets de l'écran Contrôle | Validé | EffectsPanelTests.Edit_AddWaveFromLibrary_OnPlanSelection_PlaysOnOutputAtOnce_UndoRemovesIt<br>EffectsPanelTests.PositionShape_ShowsPlane_AndDegrees |
| [ERG-030](exigences/ERG-030.md) | I | En-têtes de couche toujours visibles | Validé | TempoBarTests.ColumnScrollPosition_SurvivesARebuildOfTheScenes |
| [ERG-031](exigences/ERG-031.md) | M | Éditeur de thèmes | Validé | EffectsPanelTests.ThemeEditor_NewTheme_FromWindow_UsedByTheEffect_DefaultThemesLocked |
| [MOT-060](exigences/MOT-060.md) | I | Un effet calcule, pour chaque membre de sa sélection, une valeur = f | Validé | EffectCompilerTests.Rainbow_PlayedByEngine_ColorChangesOverCycle<br>EffectTests.SceneSpeed_DoublesEffectSpeed<br>EffectTests.Sine_FourPars_NinetyDegreesApart_ExpectedValues |
| [MOT-061](exigences/MOT-061.md) | I | Effet relatif | Validé | EffectTests.AbsoluteEffect_ReplacesStepValue<br>EffectTests.RelativeCircle_AroundStepPosition<br>EffectTests.RelativeEffect_WithoutStepValue_AddsToUnderlyingLayer |
| [MOT-063](exigences/MOT-063.md) | M | Un effet entre et sort avec le poids de sa scène | Réalisé | EffectTests.EffectEntersWithSceneFade_NoJump<br>EffectTests.EffectLeavesWithSceneFadeOut |
| [SCN-014](exigences/SCN-014.md) | S | Assistants de création | Validé | EffectsPanelTests.Wizard_ColorChase_ReplacesSteps_Undoable<br>SceneWizardsTests.Alternate_TwoSteps_Swapped<br>SceneWizardsTests.ColorChase_FourParsFourColors_FourSteps_ColorsShiftByOne<br>SceneWizardsTests.PositionSweep_OneStepPerPalette_AllMembers |

## ERG2 – 8 exigences, 6 couvertes par des tests automatiques

> Validé : 8

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [ERG-032](exigences/ERG-032.md) | I | Écran de jeu | Validé | ControlLayoutTests.DefaultLayout_IsTheGameScreen_FiveBigPanels_NoEditingPanel<br>ControlLayoutTests.Store_KeepsTheGameLayoutInItsOwnFile_TheOldOnesAreIgnored<br>GameViewModelTests.EditBand_AsksForTheEditWindow_WithTheSceneId_AndChangesNothingElse<br>GameViewModelTests.LockedEveningLock_RefusesTheEditWindow_WithItsReason_ButStillPlays<br>(+3) |
| [ERG-033](exigences/ERG-033.md) | I | Fenêtre d'édition de scène | Validé | DraftSessionTests.BeginDraft_ChoosesTheSceneInEdit_WithoutWritingAnything<br>DraftSessionTests.BeginDraft_IsRefusedUnderTheEveningLock_OrForAnUnknownScene<br>DraftSessionTests.Editing_KeepsTheDraftInMemory_TheProjectSceneStaysAsItWas_TheEngineGetsTheDraft<br>DraftSessionTests.EndDraft_LeavesNoSceneEdited_NoOutputChange_AndTheDraftIsGone<br>(+17) |
| [ERG-034](exigences/ERG-034.md) | I | Aperçu du brouillon | Validé | DraftSessionTests.ApplyDraft_WritesTheSceneIntoTheProject_AndTheDraftGoesOn<br>DraftSessionTests.Blind_TheOutputKeepsTheSavedScene_OnlyThePreviewSeesTheDraft<br>DraftSessionTests.DiscardDraft_GoesBackToTheOriginalScene_KeepingWhatWasApplied<br>DraftSessionTests.SuspendShow_LetsTheDraftSceneBePlayedWithoutTheEditedStepOnTop<br>(+3) |
| [ERG-035](exigences/ERG-035.md) | M | Tailles minimales des cibles | Validé |  |
| [ERG-036](exigences/ERG-036.md) | I | Groupes d'appareils | Validé | DimmerCompilerTests.Compile_FaultyGroups_AreReportedAsWarnings_AndNeverBlock<br>DimmerCompilerTests.Compile_GroupsTree_GivesParentFirstAndTheGroupOfEachFixtureParameter<br>DimmerCompilerTests.Compile_WithoutGroups_GivesTheEngineNoGroup<br>DimmerGroupsViewModelTests.AddFixture_MovesItFromTheUnassignedGroup_AndRemoveGivesItBack<br>(+19) |
| [ERG-037](exigences/ERG-037.md) | I | Dimmers de groupe | Validé | DimmerCompilerTests.EndToEnd_GroupDimmer_HalvesTheFourParsOnTheWire_LeavingTheLyreAlone<br>DimmerGroupsViewModelTests.Levels_AreReadFromTheEngine_ReadOnly_WithFaderNumbersAndTheFlow<br>DimmersPanelTests.AnOutsideChange_MidiForExample_IsShownOnTheFader_WithoutSendingACommandBack<br>DimmersPanelTests.MovingAFader_SendsTheCommand_TheEngineMultiplies_AndTheEffectiveLevelFollows<br>(+12) |
| [ERG-038](exigences/ERG-038.md) | I | Seconde platine MIDI | Validé | DimmerGroupsViewModelTests.DimmerPlatine_ChosenInTheInterface_IsWrittenToMidiJson<br>DimmerGroupsViewModelTests.FaderMarkers_PastEight_NameThePage<br>DimmersPanelTests.OneFaderPerDimmerGroup_InTreeOrder_WithTheirPlatineNumber<br>DimmersPanelTests.WithMoreThanEightDimmers_TheMarkersFollowThePlatinePage_AndTheOtherPageIsDimmed<br>(+13) |
| [ERG-039](exigences/ERG-039.md) | I | Fader de couche = niveau de couche | Validé |  |

## P7 – 46 exigences, 31 couvertes par des tests automatiques

> Non réalisé : 3 · Partiel : 4 · Réalisé : 7 · Validé : 32

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [AUD-001](exigences/AUD-001.md) | I | Capture du son joué par le PC | Validé | AudioListenerTests.Listening_AnalysesTheSource_AndFeedsTheEngine<br>PcmMixerTests.Float32AndInt32_AreBothSupported<br>PcmMixerTests.Mono24Bit_KeepsTheSignAndScale<br>PcmMixerTests.Stereo16Bit_IsAveragedToMono<br>(+1) |
| [AUD-002](exigences/AUD-002.md) | I | Suivi du changement de périphérique par défaut | Validé | AudioListenerTests.DefaultOutputChange_WithoutSystemNotification_ReconnectsAndWarns<br>AudioListenerTests.DeviceChange_LeavesAVisibleNotice_ThenTheRecoveryOne<br>AudioListenerTests.DeviceChange_ReconnectsOnTheNewDevice |
| [AUD-003](exigences/AUD-003.md) | M | Choix manuel d'un périphérique de sortie à écouter, ou d'une entrée | Validé | AudioViewModelTests.ChoosingTheMicrophone_IsRememberedAndUsedByTheListening |
| [AUD-004](exigences/AUD-004.md) | I | L'analyse est indépendante du volume | Validé | AnalyzerTests.Silence_OnlyNoiseFloor_GivesNoTempoAndNoGrid<br>AnalyzerTests.Tempo_DoesNotDependOnTheVolume<br>PulseAndEnergyTests.Energy_DoesNotDependOnTheVolume |
| [AUD-005](exigences/AUD-005.md) | I | Détection du silence | Validé | AnalyzerTests.Silence_IsDetected_AndResetsTheTracking<br>AudioListenerTests.Listening_AfterAPauseWithoutBlocks_ComesBackWhenTheMusicReturns<br>AudioListenerTests.Listening_WithoutBlocks_IsNotLive<br>MusicalClockTests.Audio_OctaveCorrection_DoesNotSurviveASilence<br>(+1) |
| [AUD-006](exigences/AUD-006.md) | I | L'analyse audio fonctionne dans son propre fil d'exécution | Validé | AudioListenerHardwareTests.ToggleWhileSoundPlays_DoesNotHang<br>AudioListenerTests.AFailingEventSubscriber_NeverStopsTheListening<br>AudioListenerTests.CaptureError_DoesNotThrow_AndRetries<br>AudioListenerTests.RapidStartStop_WithASourceThatFailsOnceReleased_NeverThrowsOutOfATimer<br>(+2) |
| [AUD-007](exigences/AUD-007.md) | M | Charge CPU de l'analyse < 5 % d'un cœur | Validé | AnalyzerTests.Analysis_OfOneMinuteOfSound_TakesAFewPercentOfRealTime |
| [AUD-020](exigences/AUD-020.md) | I | Estimation du tempo dans une plage réglable | Validé | AnalyzerTests.Tempo_OfASyntheticGroove_IsFoundWithinOnePercent<br>AudioFileAnalysisTests.WavOfKicksAt120_IsAnalysedAt120_WithPulses<br>AudioViewModelTests.GameSwitch_FollowsTheMusic_GreysTheManualControls_AndKeepsTheCorrections<br>MusicalClockTests.Audio_Source_FollowsTheTempoAndThePhaseOfTheFeed<br>(+2) |
| [AUD-021](exigences/AUD-021.md) | I | Suivi de la phase | Validé | AnalyzerTests.BeatPhase_IsConsistentWithTheKicks |
| [AUD-022](exigences/AUD-022.md) | I | Indice de confiance | Validé |  |
| [AUD-023](exigences/AUD-023.md) | I | Correction d'octave | Validé | MusicalClockTests.Audio_TimesTwo_BeatsFollowTheDoubledTempo_NotTheMusicsOwn<br>MusicalClockTests.Audio_TimesTwo_FollowsTheAnalysisWhenItChangesOctaveItself<br>MusicalClockTests.Audio_TimesTwo_IsKeptWhileTheSameSongPlays<br>MusicalClockTests.Scale_DoublesAndHalves_WithinBounds<br>(+1) |
| [AUD-024](exigences/AUD-024.md) | I | Détection du premier temps de la mesure | Partiel | AnalyzerTests.Downbeat_FromAnAccentedFirstBeat_IsKnown<br>MusicalClockTests.Audio_Downbeat_ThatDisagreesForASecond_MovesTheBarPosition<br>MusicalClockTests.ManualBarResync_IsNotOverruledByTheGuessedDownbeat_UntilANewSong<br>MusicalClockTests.ResyncBar_MakesTheCurrentBeatTheFirstOfABar<br>(+1) |
| [AUD-025](exigences/AUD-025.md) | I | Tap tempo | Validé | MusicalClockTests.Tap_FourTapsAtHalfSecond_Gives120<br>MusicalClockTests.Tap_SnapsThePhaseToTheTap<br>MusicalClockTests.Tap_ThreeTaps_DoNotChangeTheTempoYet<br>MusicalClockTests.Tap_TwoSecondsWithoutTap_RestartsTheCount<br>(+4) |
| [AUD-026](exigences/AUD-026.md) | I | Changement de morceau | Validé | AnalyzerTests.NewSong_AfterASilence_IsFoundInUnderEightSeconds<br>MusicalClockTests.Audio_NewSong_DropsTheOctaveCorrection<br>MusicalClockTests.Audio_Source_SnapsToANewSongTempo |
| [AUD-027](exigences/AUD-027.md) | I | Décalage de latence global réglable ± 250 ms | Validé | AudioViewModelTests.Calibration_WithoutTheScene_ExplainsWhere |
| [AUD-028](exigences/AUD-028.md) | M | Le BPM corrigé par l'utilisateur | Non réalisé |  |
| [AUD-029](exigences/AUD-029.md) | M | Mesures à 4 temps par défaut | Validé |  |
| [AUD-040](exigences/AUD-040.md) | I | Détection des attaques dans deux bandes | Validé | PulseAndEnergyTests.Pulses_BassGuitarNotes_AreNotKicks_ButDrumKicksAre<br>PulseAndEnergyTests.Pulses_KickAndHats_AreCountedPerBand<br>PulseAndEnergyTests.Pulses_NoBassPassage_GivesNoBassPulse |
| [AUD-041](exigences/AUD-041.md) | I | Chaque impulsion porte une force | Validé | PulseAndEnergyTests.Pulses_CarryAStrength_BetweenZeroAndOne |
| [AUD-042](exigences/AUD-042.md) | I | Seuil de sensibilité et temps mort minimal entre deux impulsions réglables globalement | Validé | PulseAndEnergyTests.Pulses_LongDeadTime_ThinsThemOut<br>PulseAndEnergyTests.Sensitivity_ChangesTheNumberOfPulses_Visibly |
| [AUD-043](exigences/AUD-043.md) | M | Latence de détection < 60 ms | Validé |  |
| [AUD-044](exigences/AUD-044.md) | S | Bande médiums | Non réalisé |  |
| [AUD-060](exigences/AUD-060.md) | I | Mesure continue de l'énergie perçue | Validé | PulseAndEnergyTests.Energy_ALoudGroove_IsHigherThanAQuietPad |
| [AUD-061](exigences/AUD-061.md) | I | Niveaux discrets avec hystérésis | Validé | PulseAndEnergyTests.EnergyLevel_DoesNotOscillate_OnAStableGroove<br>PulseAndEnergyTests.EnergyLevel_OnAStablePassage_StaysStable |
| [AUD-062](exigences/AUD-062.md) | I | Détection de Break | Validé | PulseAndEnergyTests.BreakThenDrop_AreDetected_InOrder |
| [AUD-063](exigences/AUD-063.md) | M | Détection de montée | Réalisé |  |
| [AUD-064](exigences/AUD-064.md) | M | Tendance | Validé |  |
| [AUD-080](exigences/AUD-080.md) | I | Écran Audio | Validé | AudioViewModelTests.Refresh_WithoutSound_ShowsNothingHeard<br>AudioViewModelTests.Screen_ListsTheDevices_AndStartsStopped |
| [AUD-081](exigences/AUD-081.md) | I | Réglages | Validé | AudioViewModelTests.Latency_FollowsTheChosenDevice<br>AudioViewModelTests.Settings_AreAppliedAfterAShortWhile_AndRemembered<br>TempoBarTests.Bar_StartsAt120_Fixed_AndShowsTheFirstBeat |
| [AUD-082](exigences/AUD-082.md) | M | Enregistrement de l'analyse | Non réalisé |  |
| [GEN-024](exigences/GEN-024.md) | I | Mesure à 4 temps | Réalisé |  |
| [GEN-026](exigences/GEN-026.md) | I | Tempo en BPM, bornes et plages | Réalisé |  |
| [GEN-034](exigences/GEN-034.md) | I | L'horloge musicale continue de battre au dernier tempo connu si le signal audio disparaît | Validé | MusicalClockTests.Audio_Source_KeepsTheLastTempoWhenTheSoundIsLostOrUnsure |
| [GEN-035](exigences/GEN-035.md) | M | Un décalage de latence global | Validé | MusicalClockTests.Latency_AdvancesTheEventsAndIsBounded |
| [LIVE-020](exigences/LIVE-020.md) | I | Affichage du tempo à l'écran de jeu | Réalisé |  |
| [LIVE-021](exigences/LIVE-021.md) | I | Commandes de tempo à l'écran de jeu | Réalisé |  |
| [MOT-016](exigences/MOT-016.md) | I | Durées musicales | Validé | EditBenchPanelsTests.Properties_StepDurations_InBeatsAndBars_AreSavedInTheirUnit<br>MusicalClockTests.Step_SecondsHold_IgnoresTheTempoChange<br>MusicalClockTests.Step_TempoHalvedMidStep_RemainingMusicalTimeDoubles<br>ReferenceShowP7Tests.OnePerBeat_FollowsAScenarioTempoChange |
| [MOT-017](exigences/MOT-017.md) | I | Avance à l'événement | Validé | MusicalReactivityTests.Step_AdvancesOnTheMusicalEvent_AndPulsesFallBackToBeatsWithoutAudio<br>MusicalReactivityTests.Step_AutoAdvance_MakesAShortFlash_WhileTheNextStepWaitsForTheEvent<br>MusicalReactivityTests.Step_EventAdvance_IgnoresTheHoldDuration_AndFollowsTempoChanges<br>MusicalReactivityTests.Step_OnBassPulses_AdvancesOnTheKicks_NotOnTheBeats<br>(+3) |
| [MOT-018](exigences/MOT-018.md) | M | Quantification du lancement | Validé | MusicalReactivityTests.Quantize_Bar_LaunchWaitsForTheNextBar_AndIsPublishedWhileWaiting<br>MusicalReactivityTests.Quantize_Beat_OnTheBeat_StartsImmediately<br>MusicalReactivityTests.Quantize_Phrase4_WaitsSixteenBeats<br>MusicalReactivityTests.Quantize_PressingAgain_CancelsTheWait_AndStopCancelsIt<br>(+3) |
| [MOT-020](exigences/MOT-020.md) | M | Une scène peut suivre l'horloge principale ou une horloge fixe propre | Validé | MusicalReactivityTests.OwnClock_MusicalDurationsUseTheSceneTempo<br>MusicalReactivityTests.OwnClock_ScenePlaysAtItsOwnTempo_WhileTheMainClockIsFaster<br>ReferenceShowP7Tests.SlowMovement_FollowsItsOwnTempo_WhateverTheMainClock |
| [MOT-062](exigences/MOT-062.md) | I | Vitesse d'effet en Hz ou en temps musicaux | Validé | MusicalReactivityTests.MusicalEffect_CycleStartsOnTheClock_NotOnTheLaunch<br>MusicalReactivityTests.MusicalEffect_FollowsTheClockWhenItIsResynchronised |
| [SCN-006](exigences/SCN-006.md) | I | Paramètres musicaux d'une scène | Réalisé |  |
| [SCN-050](exigences/SCN-050.md) | I | Les paramètres ci-dessus sont réglables par scène | Partiel |  |
| [SCN-051](exigences/SCN-051.md) | M | Modulation par l'énergie | Partiel | MusicalReactivityTests.EnergySpeed_MakesTheSceneFasterWhenTheMusicIsMoreEnergetic |
| [SCN-052](exigences/SCN-052.md) | M | En l'absence de signal audio | Réalisé | MusicalReactivityTests.Step_AdvancesOnTheMusicalEvent_AndPulsesFallBackToBeatsWithoutAudio |
| [SIM-011](exigences/SIM-011.md) | M | Bandeau musical du simulateur | Partiel |  |

## P8 – 23 exigences, 0 couvertes par des tests automatiques

> Reporté : 1 · Sans fiche : 22

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [GEN-025](exigences/GEN-025.md) | S | Les mesures à 3 temps | Reporté |  |
| [GEN-134](exigences/GEN-134.md) | M | (fiche manquante) | Sans fiche |  |
| [LIVE-023](exigences/LIVE-023.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-001](exigences/SHOW-001.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-002](exigences/SHOW-002.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-003](exigences/SHOW-003.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-004](exigences/SHOW-004.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-005](exigences/SHOW-005.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-006](exigences/SHOW-006.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-007](exigences/SHOW-007.md) | M | (fiche manquante) | Sans fiche |  |
| [SHOW-008](exigences/SHOW-008.md) | S | (fiche manquante) | Sans fiche |  |
| [SHOW-020](exigences/SHOW-020.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-021](exigences/SHOW-021.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-022](exigences/SHOW-022.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-023](exigences/SHOW-023.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-024](exigences/SHOW-024.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-025](exigences/SHOW-025.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-026](exigences/SHOW-026.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-027](exigences/SHOW-027.md) | I | (fiche manquante) | Sans fiche |  |
| [SHOW-028](exigences/SHOW-028.md) | M | (fiche manquante) | Sans fiche |  |
| [SHOW-029](exigences/SHOW-029.md) | M | (fiche manquante) | Sans fiche |  |
| [SHOW-030](exigences/SHOW-030.md) | M | (fiche manquante) | Sans fiche |  |
| [SHOW-031](exigences/SHOW-031.md) | S | (fiche manquante) | Sans fiche |  |
