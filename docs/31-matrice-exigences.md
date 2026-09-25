# 31 – Matrice exigences ↔ tests

> Générée par `python tools/matrice-exigences.py P0 P1 P2 P3` (doc 30 §7). Ne pas modifier à la main.
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
| [GEN-050](exigences/GEN-050.md) | I | Fichiers JSON lisibles | Réalisé | StoresTests.InstallationStore_SaveThenLoad_RoundTrips<br>VersionedJsonFileTests.SaveThenLoad_RoundTrips<br>VersionedJsonFileTests.Save_WritesIndentedUtf8WithVersionFirst_AndReadableAccents |
| [GEN-051](exigences/GEN-051.md) | I | Version de format et migrations | Réalisé | VersionedJsonFileTests.Load_OldVersion_MigratesAndKeepsBackup |
| [GEN-056](exigences/GEN-056.md) | I | Fichier illisible sans plantage | Réalisé | PreferencesAndProjectTests.Preferences_Corrupt_GivesDefaultsAndSetsFileAside<br>PreferencesAndProjectTests.Project_CorruptFile_ReportsMessageWithoutThrowing<br>VersionedJsonFileTests.Load_CorruptFile_IsSetAsideWithoutThrowing<br>VersionedJsonFileTests.Load_MissingVersion_IsInvalid |
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

## P1 – 22 exigences, 13 couvertes par des tests automatiques

> Non réalisé : 2 · Partiel : 2 · Reporté (P4-P5) : 1 · Réalisé : 17

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [CONS-001](exigences/CONS-001.md) | I | Faders par pages | Réalisé | ConsoleViewModelTests.PageSize_AdaptsToWidth_AndPagesCoverAll512Channels |
| [CONS-002](exigences/CONS-002.md) | I | Modes de saisie des faders | Réalisé | ConsoleViewModelTests.TypedValue_Valid_IsApplied_Invalid_IsRejected |
| [CONS-003](exigences/CONS-003.md) | I | Prise et libération d'un fader | Réalisé | ChannelOverrideTests.Override_SetsChannelValueOnNextTick<br>ChannelOverrideTests.Override_ToZero_IsStillAnOverride<br>ChannelOverrideTests.Release_ReturnsChannelToChainValue<br>ConsoleLatencyTests.Override_AppearsInRecordedFrame_OnNextTick<br>(+2) |
| [CONS-004](exigences/CONS-004.md) | I | Commandes de libération et de page | Réalisé | ChannelOverrideTests.ReleaseAll_ClearsEveryUniverse<br>ConsoleViewModelTests.PageToFull_ThenReleasePage<br>ConsoleViewModelTests.ReleaseSelection_OnlyReleasesSelectedChannels |
| [CONS-005](exigences/CONS-005.md) | I | Le fader affiche la valeur émise | Réalisé | ConsoleViewModelTests.FaderRequest_OverridesChannel_AndFaderShowsEmittedValue |
| [CONS-006](exigences/CONS-006.md) | I | Sélection multiple de faders | Réalisé | ConsoleViewModelTests.ClickWithoutModifier_OutsideSelection_SelectsOnlyThatFader<br>ConsoleViewModelTests.MultiSelection_Absolute_SetsSameValue<br>ConsoleViewModelTests.MultiSelection_Relative_MovesAllByTheSameDelta |
| [CONS-007](exigences/CONS-007.md) | M | Appareil, attribut et plage sur le fader | Réalisé | ConsoleViewModelTests.Fader_PatchedChannelWithCapabilities_ShowsRangeNameInsteadOfPercent<br>ConsoleViewModelTests.MonitorHover_PatchedChannel_ShowsFixtureAndAttribute<br>PatchLookupTests.FindChannel_DifferentUniverse_ReturnsNull<br>PatchLookupTests.FindChannel_OutsideRange_ReturnsNull<br>(+1) |
| [CONS-008](exigences/CONS-008.md) | M | Surcharges soumises au blackout et à la sûreté | Reporté (P4-P5) |  |
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

## P2 – 34 exigences, 30 couvertes par des tests automatiques

> Non réalisé : 2 · Partiel : 1 · Réalisé : 29 · Réalisé, à valider sur matériel : 2

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [BIB-001](exigences/BIB-001.md) | I | Modèle de données complet d'un appareil | Réalisé | FixtureLibraryTests.SaveThenLoad_IsLossless<br>ParkLibraryTests.Lcb803_SectionsBecomeCells<br>ParkLibraryTests.ParkLibrary_LoadsSevenModels_WithoutMessage<br>ParkLibraryTests.Wzybuta_20And64Channels_12CellsIn64 |
| [BIB-002](exigences/BIB-002.md) | I | Au moins un mode ; définitions de canaux partagées | Réalisé | FixtureValidatorTests.FixtureWithoutMode_IsAnError |
| [BIB-003](exigences/BIB-003.md) | I | Attribut 16 bits = un seul attribut sur deux canaux | Réalisé | FixtureEditsTests.SetResolution_16Bit_AddsFineAfterCoarse_AndBackTo8BitRemovesIt<br>FixtureValidatorTests.CoarseWithoutFine_IsAWarning<br>OflImporterTests.Spot_FineChannelsAreOneAttribute_AndWheelsCarryColors<br>ParkLibraryTests.Lyre_11Channels_Has16BitPanTilt_9ChannelsCoarseOnly<br>(+2) |
| [BIB-004](exigences/BIB-004.md) | I | Validation d'un modèle | Réalisé | FixtureValidatorTests.FineOf8BitChannel_IsAnError<br>FixtureValidatorTests.GapBetweenRanges_IsAWarning<br>FixtureValidatorTests.ModeWithoutChannel_IsAnError<br>FixtureValidatorTests.OrphanFineChannel_IsAnError<br>(+5) |
| [BIB-005](exigences/BIB-005.md) | I | Nombre de canaux et réglage sur l'appareil par mode | Réalisé | FixtureRulesTests.SettingSheet_ShowsChannelCountAndDeviceSetting<br>ParkLibraryTests.Lpc008s_ModesAndIntensityRules |
| [BIB-006](exigences/BIB-006.md) | I | Intensité virtuelle et « Suit l'intensité » | Réalisé | FixtureDecoderTests.Decode_NoDimmerChannel_UsesVirtualIntensity<br>FixtureRulesTests.FollowsIntensity_Override_WinsOverDeduction<br>FixtureRulesTests.SevenChannelMode_WithDimmer_NothingFollowsIntensity<br>FixtureRulesTests.ThreeChannelMode_HasVirtualIntensity_AndColorsFollowIntensity<br>(+2) |
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

## P3 – 47 exigences, 29 couvertes par des tests automatiques

> Non réalisé : 7 · Partiel : 6 · Réalisé : 34

| Exigence | Pri. | Titre | Statut | Tests automatiques |
|---|---|---|---|---|
| [CONS-020](exigences/CONS-020.md) | I | Faders regroupés par appareil patché | Réalisé | ConsoleViewModelTests.DeviceMode_BuildsOneGroupPerPatchedFixture_AndFaderOverridesTheRealChannel<br>ConsoleViewModelTests.DeviceMode_Leaving_KeepsOverridesAsRealConsoleValues |
| [CONS-021](exigences/CONS-021.md) | I | Outil adapté par type d'attribut | Partiel | ConsoleViewModelTests.DeviceMode_BuildsOneGroupPerPatchedFixture_AndFaderOverridesTheRealChannel |
| [CONS-022](exigences/CONS-022.md) | I | Surcharge d'attribut soumise à la chaîne de rendu | Partiel |  |
| [CONS-023](exigences/CONS-023.md) | M | Clic sur une plage et balayage | Réalisé |  |
| [CONS-024](exigences/CONS-024.md) | M | Bouton Identifier par appareil | Réalisé | ConsoleViewModelTests.Identify_LightsIntensityChannel_WithoutTouchingColor_AndReleasesOnStop |
| [GEN-004](exigences/GEN-004.md) | M | Composant d'édition réutilisable dans un autre écran | Réalisé |  |
| [GEN-053](exigences/GEN-053.md) | I | Copie des modèles d'appareils dans le projet | Réalisé | FixtureUpdateImpactTests.ForLibraryUpdate_ModeRemovedInNewDefinition_IsFlagged<br>InstallationViewModelTests.AddFixture_Multiple_CreatesConsecutiveAddressesAndCopiesModelIntoProject<br>InstallationViewModelTests.UpdateFromLibrary_WithImpact_AsksConfirmation<br>StoresTests.ProjectFixtureLibrary_EnsureCopied_CopiesFixtureIntoProjectFolder<br>(+2) |
| [GEN-122](exigences/GEN-122.md) | M | Sorties réseau locales autorisées (Art-Net) | Réalisé |  |
| [INST-001](exigences/INST-001.md) | I | Un ou plusieurs univers, numérotés et nommables | Réalisé | StoresTests.InstallationStore_Missing_ReturnsDefaultWithOneUniverse |
| [INST-002](exigences/INST-002.md) | I | Lien univers → pilotes dans les préférences | Réalisé |  |
| [INST-003](exigences/INST-003.md) | I | Vue barre d'univers | Réalisé |  |
| [INST-010](exigences/INST-010.md) | I | Ajouter un appareil au patch | Réalisé | InstallationViewModelTests.AddFixture_Multiple_CreatesConsecutiveAddressesAndCopiesModelIntoProject |
| [INST-011](exigences/INST-011.md) | I | Ajout multiple d'appareils identiques | Réalisé | InstallationViewModelTests.AddFixture_Multiple_CreatesConsecutiveAddressesAndCopiesModelIntoProject<br>PatchRulesTests.PlanMultiple_FourParsSevenChannels_GivesReferenceShowAddresses<br>PatchRulesTests.PlanMultiple_WithGap_LeavesReserve |
| [INST-012](exigences/INST-012.md) | I | Première adresse libre proposée | Réalisé | InstallationViewModelTests.SuggestAddress_ProposesFirstFreeAddress<br>PatchRulesTests.FindFreeAddress_NoRoomLeft_ReturnsNull<br>PatchRulesTests.FindFreeAddress_SkipsOccupiedRanges |
| [INST-013](exigences/INST-013.md) | I | Détection des chevauchements en temps réel | Réalisé | InstallationViewModelTests.OverlappingFixtures_AreFlagged<br>PatchRulesTests.DetectOverlaps_DifferentUniverses_NoOverlap<br>PatchRulesTests.DetectOverlaps_FindsOverlappingRange |
| [INST-014](exigences/INST-014.md) | M | Doublon volontaire (jumeaux) | Réalisé | InstallationViewModelTests.Twins_SameAddress_AreNotFlaggedAsOverlap<br>PatchRulesTests.DetectOverlaps_SameGroupButDifferentMode_StillOverlaps<br>PatchRulesTests.DetectOverlaps_Twins_SameAddress_NoOverlap |
| [INST-015](exigences/INST-015.md) | I | Déplacer un appareil | Réalisé | InstallationViewModelTests.RenameAndMove_UpdateThePatchedFixture |
| [INST-016](exigences/INST-016.md) | I | Changer le mode d'un appareil patché | Réalisé | FixtureUpdateImpactTests.ForModeChange_FromRichToSimpleMode_ReportsLostChannels<br>FixtureUpdateImpactTests.ForModeChange_SameMode_IsEmpty<br>InstallationViewModelTests.ChangeMode_WithImpact_AsksConfirmation |
| [INST-017](exigences/INST-017.md) | I | Nom, couleur et numéro court | Réalisé | InstallationViewModelTests.RenameAndMove_UpdateThePatchedFixture |
| [INST-018](exigences/INST-018.md) | I | Fiche d'installation | Réalisé |  |
| [INST-019](exigences/INST-019.md) | I | Identifier un appareil / chenillard d'identification | Réalisé | InstallationViewModelTests.Identify_LightsIntensityChannel_AndChaseAdvancesToNextFixture |
| [INST-020](exigences/INST-020.md) | M | Supprimer un appareil | Réalisé | InstallationViewModelTests.DeleteFixture_AsksConfirmation |
| [INST-021](exigences/INST-021.md) | M | Options de montage par appareil | Partiel | FixtureDecoderTests.Decode_InvertedPan_ReversesDirection |
| [INST-030](exigences/INST-030.md) | I | Sélection manuelle ordonnée | Réalisé | InstallationViewModelTests.CreateSelection_FromCheckedFixtures_ThenReverse |
| [INST-031](exigences/INST-031.md) | I | Sélections automatiques | Réalisé | AutoSelectionsTests.Build_AllFixtures_IsOrderedByAddress<br>AutoSelectionsTests.Build_GroupsByCategoryAndByModel<br>AutoSelectionsTests.Build_NewFixtureAdded_AppearsWithoutAnyStoredState<br>InstallationViewModelTests.AutoSelections_IncludeAllAndByCategory |
| [INST-032](exigences/INST-032.md) | I | Sélections manuelles créées et réordonnées | Partiel |  |
| [INST-033](exigences/INST-033.md) | M | Opérations d'ordre sur une sélection | Réalisé | InstallationViewModelTests.CreateSelection_FromCheckedFixtures_ThenReverse<br>InstallationViewModelTests.ReorderSelection_ByPosition_UsesActiveVenuePlacements<br>SelectionRulesTests.FirstAndSecondHalf_SplitInTheMiddle<br>SelectionRulesTests.OddAndEven_SplitByRank<br>(+3) |
| [INST-034](exigences/INST-034.md) | M | Sélection de cellules | Non réalisé |  |
| [INST-050](exigences/INST-050.md) | I | Créer, dupliquer, activer un lieu | Réalisé | InstallationViewModelTests.CreateVenue_ThenActivate_ChangesActiveVenue<br>InstallationViewModelTests.Venues_HaveAGenericVenueByDefault<br>StoresTests.VenueStore_Missing_ReturnsDefaultGenericVenue<br>StoresTests.VenueStore_SaveThenLoad_KeepsActiveVenue |
| [INST-051](exigences/INST-051.md) | I | Éditeur de plan | Partiel | InstallationViewModelTests.SavePlacements_PersistsPositionAndAbsence |
| [INST-052](exigences/INST-052.md) | I | Appareil absent | Réalisé | InstallationViewModelTests.SavePlacements_PersistsPositionAndAbsence<br>SimulatorViewModelTests.Refresh_AbsentFixture_IsHidden |
| [SIM-001](exigences/SIM-001.md) | I | Affichage du plan du lieu actif | Réalisé | SimulatorViewModelTests.Refresh_OnlyShowsPlacedAndPresentFixtures |
| [SIM-002](exigences/SIM-002.md) | I | Rendu 30 images/s sans ralentir le moteur | Réalisé |  |
| [SIM-003](exigences/SIM-003.md) | I | Décodage des trames via le patch | Réalisé | FixtureDecoderTests.Decode_AllChannelsAtZero_IsDarkAndOff<br>FixtureDecoderTests.Decode_ColorWheelCapability_UsesSlotColor<br>FixtureDecoderTests.Decode_MultiCellFixture_DecodesEachCellSeparately<br>FixtureDecoderTests.Decode_NoDimmerChannel_UsesVirtualIntensity<br>(+2) |
| [SIM-004](exigences/SIM-004.md) | I | Faisceau des lyres | Réalisé | FixtureDecoderTests.Decode_InvertedPan_ReversesDirection<br>FixtureDecoderTests.Decode_PanTilt_ComputesAngleFromAmplitude |
| [SIM-005](exigences/SIM-005.md) | I | Survol / clic sur un appareil | Réalisé | SimulatorViewModelTests.OnFixtureHovered_DescribesFixture |
| [SIM-006](exigences/SIM-006.md) | I | Choix de la source affiché en permanence | Partiel |  |
| [SIM-007](exigences/SIM-007.md) | M | Fenêtre détachable et plein écran | Non réalisé |  |
| [SIM-008](exigences/SIM-008.md) | M | Zones interdites et repères du lieu | Non réalisé |  |
| [SIM-009](exigences/SIM-009.md) | M | Appareils identifiés et en erreur mis en évidence | Réalisé | SimulatorViewModelTests.Refresh_FixtureTypeMissingFromProjectLibrary_IsFlaggedAsError |
| [SIM-010](exigences/SIM-010.md) | M | Sélection au clic / au lasso | Non réalisé |  |
| [SIM-012](exigences/SIM-012.md) | I | Protection photosensible (strobe) | Réalisé | FixtureDecoderTests.Decode_StrobeCapability_IsFlaggedAsStrobing |
| [SIM-013](exigences/SIM-013.md) | S | Vue de face | Non réalisé |  |
| [SORT-008](exigences/SORT-008.md) | M | Canaux maintenus pendant le test de sortie | Réalisé | RenderEngineTests.TestPattern_HeldChannels_RespectExcludedChannels<br>RenderEngineTests.TestPattern_HeldChannels_StayLitForTheWholeChase |
| [SORT-062](exigences/SORT-062.md) | I | Pilote Simulateur | Réalisé |  |
| [SORT-063](exigences/SORT-063.md) | S | Lecteur d'enregistrements | Non réalisé |  |
| [SORT-064](exigences/SORT-064.md) | S | Pilote Art-Net | Non réalisé |  |
