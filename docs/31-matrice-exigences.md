# 31 – Matrice exigences ↔ tests

> Générée par `python tools/matrice-exigences.py P0 P1 P2` (doc 30 §7). Ne pas modifier à la main.
> « Automatique » : au moins un test référence l'exigence. « Manuel » : vérification par le guide de démonstration
> de la phase (docs/demos) ou sur le matériel. Le statut « validé » est donné par l'utilisateur lors de la livraison.

## P0 – 44 exigences, 30 couvertes par des tests automatiques

| Exigence | Pri. | Énoncé (début) | Couverture | Tests |
|---|---|---|---|---|
| GEN-001 | I | Le moteur de rendu doit pouvoir calculer des trames sans qu'aucun module d'interface, d'au… | Automatique | DependencyRulesTests.Project_DoesNotReferenceUserInterface<br>DependencyRulesTests.Project_OnlyReferencesAllowedDmxProjects<br>ReferenceShowP0Tests.Engine_InVirtualTime_ReproducesReferenceRecording<br>RenderEngineTests.Tick_WithSeveralUniverses_SubmitsOneFramePerUniverse<br>(+1) |
| GEN-002 | I | Aucun module ne doit modifier l'état de restitution autrement que par une commande du cata… | Manuel |  |
| GEN-003 | I | Les modules d'édition (Atelier) ne doivent pas dépendre du module Live, et réciproquement. | Automatique | DependencyRulesTests.UserInterfaceModules_DoNotReferenceApplication |
| GEN-030 | I | Le moteur est cadencé par un **tick** régulier, par défaut **40 Hz**, réglable de 25 à 44 … | Automatique | TickLoopTests.RateHz_IsClampedTo25To44<br>TickLoopTests.Run_TwoSeconds_KeepsFortyHertz |
| GEN-031 | I | La gigue du tick doit rester inférieure à 5 ms (99e centile) sur un PC standard, interface… | Automatique | TickLoopTests.Run_TwoSeconds_KeepsFortyHertz |
| GEN-050 | I | Tous les fichiers de données sont en **JSON UTF-8 indenté**, lisibles et modifiables à la … | Automatique | VersionedJsonFileTests.SaveThenLoad_RoundTrips<br>VersionedJsonFileTests.Save_WritesIndentedUtf8WithVersionFirst_AndReadableAccents |
| GEN-051 | I | Chaque fichier porte un **numéro de version de format**. L'application migre automatiqueme… | Automatique | VersionedJsonFileTests.Load_OldVersion_MigratesAndKeepsBackup |
| GEN-056 | I | Un fichier illisible ou incohérent ne doit jamais faire planter l'application : il est sig… | Automatique | PreferencesAndProjectTests.Preferences_Corrupt_GivesDefaultsAndSetsFileAside<br>PreferencesAndProjectTests.Project_CorruptFile_ReportsMessageWithoutThrowing<br>VersionedJsonFileTests.Load_CorruptFile_IsSetAsideWithoutThrowing<br>VersionedJsonFileTests.Load_MissingVersion_IsInvalid |
| GEN-060 | I | Au démarrage, la sortie émet un **blackout** tant que l'utilisateur n'a rien lancé. | Automatique | RenderEngineTests.Tick_WithoutAnything_ProducesBlackoutFrame |
| GEN-080 | I | **Perte du PC** : si l'interface DMX ne reçoit plus de trame valide pendant **2 s**, elle … | Manuel |  |
| GEN-081 | I | Si l'application se ferme anormalement, le comportement GEN-080 s'applique. | Manuel |  |
| GEN-091 | I | Une **sortie déconnectée** ne bloque ni le moteur ni l'interface ; reconnexion automatique… | Automatique | ArduinoOutputDriverTests.Unplug_ThenReplug_ReconnectsWithinThreeSeconds |
| GEN-093 | I | Une erreur dans un module secondaire (audio, style, MIDI, simulateur) ne doit ni arrêter l… | Automatique | TickLoopTests.Run_TickThrows_LoopContinues |
| GEN-110 | I | **Journal technique** : démarrage, erreurs, connexions/déconnexions, avertissements ; fich… | Manuel |  |
| GEN-120 | I | Toutes les fonctions de l'application utilisées en soirée fonctionnent **sans accès Intern… | Manuel |  |
| SORT-001 | I | Chaque univers est associé à **zéro, un ou plusieurs** pilotes. Une même trame peut partir… | Automatique | OutputRouterTests.Submit_OneUniverseToTwoDrivers_BothReceiveIdenticalFrames<br>OutputRouterTests.Submit_OtherUniverse_IsNotRouted |
| SORT-002 | I | Chaque pilote tourne indépendamment : un pilote lent ou en erreur ne retarde ni le moteur … | Automatique | OutputRouterTests.SlowDriver_DoesNotDelayOthers_AndKeepsOnlyLatestFrame |
| SORT-003 | I | Si un pilote n'a pas fini d'émettre la trame précédente, seule **la plus récente** est con… | Automatique | OutputRouterTests.SlowDriver_DoesNotDelayOthers_AndKeepsOnlyLatestFrame |
| SORT-004 | I | Chaque pilote publie son état : `Déconnecté`, `Connexion…`, `Connecté`, `Erreur` (+ messag… | Automatique | OutputRouterTests.StateChanges_ArePublishedOnBus |
| SORT-005 | I | Le moteur continue de fonctionner quand aucun pilote n'est connecté. | Automatique | OutputRouterTests.Submit_WithoutAnyDriver_DoesNothing |
| SORT-006 | I | La configuration des sorties (univers → pilotes, paramètres) est enregistrée dans les **pr… | Automatique | PreferencesAndProjectTests.Preferences_Missing_GivesDefaults<br>PreferencesAndProjectTests.Preferences_Update_IsPersisted |
| SORT-007 | M | Écran « Sorties » : liste des pilotes, état, port, trames/s, bouton reconnecter, bouton te… | Automatique | ReferenceShowP0Tests.ReferenceRecording_LightsChannels1To179InOrder_NeverSmoke<br>RenderEngineTests.TestPattern_OnlyCurrentChannelIsLit_WithConfiguredValue<br>RenderEngineTests.TestPattern_SkipsExcludedChannels<br>RenderEngineTests.TestPattern_WalksChannelsUsingElapsedTime |
| SORT-010 | I | **Détection automatique** : énumération des ports série ; pour chaque port candidat (prior… | Automatique | ArduinoOutputDriverTests.Connect_DoesNotProbeNonArduinoPorts_ByDefault<br>ArduinoOutputDriverTests.Connect_FindsProjectFirmwareAmongArduinoPorts<br>ArduinoOutputDriverTests.Connect_ProbesAllPorts_WhenEnabled |
| SORT-011 | I | Le dernier port utilisé est mémorisé et essayé en premier. | Automatique | ArduinoOutputDriverTests.Connect_TriesLastPortFirst_AndReportsSelectedPort<br>PreferencesAndProjectTests.Preferences_Update_IsPersisted |
| SORT-012 | I | Le port n'est **jamais ouvert à 1200 bauds** (cette vitesse déclenche le mode programmatio… | Automatique | EnttecProtocolTests.SerialPort_IsNeverOpenedAt1200Baud |
| SORT-013 | I | **Reconnexion automatique** : en cas de perte (débranchement, erreur d'écriture), tentativ… | Automatique | ArduinoOutputDriverTests.Unplug_ThenReplug_ReconnectsWithinThreeSeconds<br>OutputRouterTests.WriteError_GoesToErrorThenReconnects |
| SORT-014 | M | Un port choisi manuellement peut forcer la connexion (identification facultative, pour les… | Automatique | ArduinoOutputDriverTests.ForcedPort_Legacy_SendsLabel0x11WithoutIdentification<br>EnttecProtocolTests.EncodeLegacy_HasNoStartCode |
| SORT-015 | M | L'identification récupère la **version du firmware** et l'affiche ; une version trop ancie… | Automatique | ArduinoOutputDriverTests.OldFirmware_IsAcceptedWithWarning<br>EnttecProtocolTests.TryParseIdentity_ReadsVersionAndChannels |
| SORT-020 | I | À chaque tick, le pilote envoie la **trame complète** de l'univers (et non les seules diff… | Automatique | ArduinoOutputDriverTests.Post_SendsFullFrameWithConfiguredChannelCount<br>EnttecProtocolTests.EncodeSendDmx_FullUniverse_Is518Bytes |
| SORT-021 | M | Le nombre de canaux émis est réglable (par défaut : 512 ; option « jusqu'au dernier canal … | Automatique | ArduinoOutputDriverTests.Post_SendsFullFrameWithConfiguredChannelCount |
| SORT-022 | I | Une erreur d'écriture ne lève jamais d'erreur vers le moteur : elle bascule le pilote en `… | Automatique | ArduinoOutputDriverTests.Unplug_ThenReplug_ReconnectsWithinThreeSeconds<br>OutputRouterTests.WriteError_GoesToErrorThenReconnects |
| SORT-023 | M | Le pilote mesure et publie la durée d'écriture et les trames réellement émises par seconde… | Manuel |  |
| SORT-040 | I | La ligne DMX est rafraîchie **en continu par le firmware** (bibliothèque DMXSerial en mode… | Manuel |  |
| SORT-041 | I | Au démarrage, tous les canaux sont à 0. | Manuel |  |
| SORT-042 | I | **Chien de garde** : sans message DMX valide pendant **2 s**, tous les canaux passent à 0 … | Manuel |  |
| SORT-043 | I | Prise en charge des messages du §5.3 (labels 6, 10, 77 ; 3 en M ; 0x11 en S). | Automatique | EnttecProtocolTests.EncodeSendDmx_ProducesLabel6WithStartCode |
| SORT-044 | I | Aucune allocation dynamique de mémoire ; tampon de réception unique de 513 octets. | Manuel |  |
| SORT-045 | I | Les valeurs d'un message ne sont appliquées qu'après réception complète et valide du messa… | Automatique | EnttecProtocolTests.Parser_WrongEndByte_IsRejected |
| SORT-046 | M | LED interne : clignote à chaque message valide ; allumée fixe si chien de garde déclenché … | Manuel |  |
| SORT-047 | M | La version du firmware est une constante unique, renvoyée par les messages 3 et 77. | Manuel |  |
| SORT-048 | M | Le firmware est **versionné dans le dépôt** (`firmware/arduino-dmx`) avec la liste des bib… | Manuel |  |
| SORT-049 | S | Le nombre de canaux émis sur la ligne est ajusté à la longueur du dernier message reçu (fr… | Manuel |  |
| SORT-060 | I | **Enregistreur** : écrit chaque trame avec son horodatage (temps écoulé depuis le début, e… | Automatique | ConsoleLatencyTests.Override_AppearsInRecordedFrame_OnNextTick<br>RecordingTests.IdenticalFrames_AreStoredCompactly<br>RecordingTests.RecorderDriver_AttachedHot_WritesFramesToFile<br>RecordingTests.WriteThenRead_RoundTripsFramesAndTimestamps<br>(+2) |
| SORT-061 | I | L'Enregistreur peut être activé/désactivé à chaud, et utilisé en même temps que l'Arduino. | Automatique | RecordingTests.RecorderDriver_AttachedHot_WritesFramesToFile |

## P1 – 22 exigences, 11 couvertes par des tests automatiques

| Exigence | Pri. | Énoncé (début) | Couverture | Tests |
|---|---|---|---|---|
| CONS-001 | I | Affichage de faders par pages (16, 32 ou 48 par page selon la largeur), numérotés 1 à 512,… | Automatique | ConsoleViewModelTests.PageSize_AdaptsToWidth_AndPagesCoverAll512Channels |
| CONS-002 | I | Réglage de chaque fader à la souris (glisser), à la molette (±1, Maj+molette ±10), au clav… | Automatique | ConsoleViewModelTests.TypedValue_Valid_IsApplied_Invalid_IsRejected |
| CONS-003 | I | Toucher un fader le **prend** : sa valeur surcharge la sortie (étape 11 de la chaîne de re… | Automatique | ChannelOverrideTests.Override_SetsChannelValueOnNextTick<br>ChannelOverrideTests.Override_ToZero_IsStillAnOverride<br>ChannelOverrideTests.Release_ReturnsChannelToChainValue<br>ConsoleLatencyTests.Override_AppearsInRecordedFrame_OnNextTick<br>(+2) |
| CONS-004 | I | Commandes : libérer un fader, libérer la page, **tout libérer** ; mettre la page à 0 ; met… | Automatique | ChannelOverrideTests.ReleaseAll_ClearsEveryUniverse<br>ConsoleViewModelTests.PageToFull_ThenReleasePage<br>ConsoleViewModelTests.ReleaseSelection_OnlyReleasesSelectedChannels |
| CONS-005 | I | Le fader affiche en permanence la **valeur réellement émise**, qu'il soit pris ou non (il … | Automatique | ConsoleViewModelTests.FaderRequest_OverridesChannel_AndFaderShowsEmittedValue |
| CONS-006 | I | Sélection multiple de faders (Ctrl/Maj+clic) : un déplacement agit sur tous (en relatif ou… | Automatique | ConsoleViewModelTests.ClickWithoutModifier_OutsideSelection_SelectsOnlyThatFader<br>ConsoleViewModelTests.MultiSelection_Absolute_SetsSameValue<br>ConsoleViewModelTests.MultiSelection_Relative_MovesAllByTheSameDelta |
| CONS-007 | M | Si le canal appartient à un appareil patché, affichage du nom de l'appareil, de l'attribut… | Manuel |  |
| CONS-008 | M | Les surcharges de la console restent soumises au blackout et aux limites de sûreté (GEN-04… | Manuel |  |
| CONS-009 | M | Choix de l'univers affiché (si plusieurs). | Manuel |  |
| CONS-010 | S | Mémoriser / rappeler des « instantanés » de console (état de tous les faders pris) pour le… | Automatique | ConsoleViewModelTests.Snapshot_SaveReleaseRecall_RestoresOverrides<br>ReferenceShowP1Tests.ReferenceShow_LoadsWithSnapshots<br>ReferenceShowP1Tests.Snapshot_RecalledByEngine_ProducesExactlyItsChannels |
| CONS-040 | I | Grille de 512 cases par univers, la couleur/luminosité de chaque case représentant la vale… | Manuel |  |
| CONS-041 | I | Au survol d'une case : numéro de canal, valeur, appareil et attribut (si patché). | Automatique | ConsoleViewModelTests.MonitorHover_DescribesChannel |
| CONS-043 | M | Les canaux appartenant à un même appareil sont délimités visuellement ; les canaux surchar… | Manuel |  |
| CONS-044 | S | Le moniteur peut s'ouvrir dans une fenêtre séparée. | Manuel |  |
| GEN-090 | I | Latence entre une action utilisateur (clic, touche, fader, pad MIDI) et la trame émise < *… | Automatique | ConsoleLatencyTests.Override_ReachesDriver_InLessThan50Milliseconds |
| GEN-100 | I | Interface entièrement en **français**. | Manuel |  |
| GEN-101 | I | **Thème sombre** par défaut (usage dans le noir), contrastes suffisants ; aucune zone blan… | Manuel |  |
| GEN-103 | I | Toute action destructrice (suppression, écrasement) demande confirmation en Atelier ; en L… | Automatique | ConsoleViewModelTests.Snapshot_Delete_AsksConfirmation |
| GEN-104 | I | Indicateur permanent : état de la sortie (connectée / déconnectée / simulée), trames/s, bl… | Manuel |  |
| GEN-107 | M | Glisser-déposer disponible pour les opérations naturelles (patch, placement au plan, scène… | Manuel |  |
| GEN-108 | S | Taille de police de l'interface réglable. | Manuel |  |
| GEN-109 | I | Aucune opération longue (import, analyse, sauvegarde) ne fige l'interface ; une progressio… | Automatique | LibraryViewModelTests.Import_SavesNewModels_SkipsExisting_AndReports |

## P2 – 34 exigences, 30 couvertes par des tests automatiques

| Exigence | Pri. | Énoncé (début) | Couverture | Tests |
|---|---|---|---|---|
| BIB-001 | I | Le modèle de données permet de décrire l'ensemble des éléments du §2 (identité, physique, … | Automatique | FixtureLibraryTests.SaveThenLoad_IsLossless<br>ParkLibraryTests.ParkLibrary_LoadsSixModels_WithoutMessage |
| BIB-002 | I | Un modèle possède **au moins un mode** ; un mode référence des définitions de canaux (une … | Automatique | FixtureValidatorTests.FixtureWithoutMode_IsAnError |
| BIB-003 | I | Un attribut 16 bits est décrit comme **un seul attribut** occupant deux canaux (grossier +… | Automatique | FixtureEditsTests.SetResolution_16Bit_AddsFineAfterCoarse_AndBackTo8BitRemovesIt<br>FixtureValidatorTests.CoarseWithoutFine_IsAWarning<br>OflImporterTests.Spot_FineChannelsAreOneAttribute_AndWheelsCarryColors<br>ParkLibraryTests.Lyre_11Channels_Has16BitPanTilt_9ChannelsCoarseOnly<br>(+1) |
| BIB-004 | I | **Validation** à l'enregistrement : plages qui se chevauchent (erreur) ; trous entre plage… | Automatique | FixtureValidatorTests.FineOf8BitChannel_IsAnError<br>FixtureValidatorTests.GapBetweenRanges_IsAWarning<br>FixtureValidatorTests.ModeWithoutChannel_IsAnError<br>FixtureValidatorTests.OrphanFineChannel_IsAnError<br>(+5) |
| BIB-005 | I | Le mode affiche son **nombre de canaux** et le **réglage à faire sur l'appareil** (ex. « A… | Automatique | FixtureRulesTests.SettingSheet_ShowsChannelCountAndDeviceSetting<br>ParkLibraryTests.Lpc008s_ModesAndIntensityRules |
| BIB-006 | I | L'intensité virtuelle est déduite automatiquement (§2.7) ; la propriété **« Suit l'intensi… | Automatique | FixtureRulesTests.FollowsIntensity_Override_WinsOverDeduction<br>FixtureRulesTests.SevenChannelMode_WithDimmer_NothingFollowsIntensity<br>FixtureRulesTests.ThreeChannelMode_HasVirtualIntensity_AndColorsFollowIntensity<br>LibraryViewModelTests.Editor_ModesChannelsAndRanges<br>(+1) |
| BIB-007 | I | Les étiquettes de sûreté (`strobe`, `fumée`) sont déduites de l'attribut et modifiables (e… | Automatique | FixtureRulesTests.ProgramChannel_WithStrobeRange_IsTaggedStrobe<br>FixtureRulesTests.SafetyTags_AreDeducedFromAttribute<br>OflImporterTests.Fog_IsSmokeTagged<br>QlcImporterTests.ClassicPar_GroupsAndStrobeInProgram |
| BIB-008 | M | Les plages de type **emplacement de roue** portent une couleur ; le simulateur et les pale… | Automatique | OflImporterTests.Spot_FineChannelsAreOneAttribute_AndWheelsCarryColors |
| BIB-009 | M | Un modèle porte une **version** incrémentée à chaque modification enregistrée. | Automatique | FixtureLibraryTests.Save_IncrementsVersion_AndRenameMovesFile<br>LibraryViewModelTests.Save_BlockedByErrors_ThenSavedWithVersion |
| BIB-010 | S | Un modèle peut être **dérivé** d'un autre (copier puis modifier), avec mention de l'origin… | Automatique | FixtureEditsTests.Derive_KeepsContent_WithNewIdAndOrigin<br>LibraryViewModelTests.Duplicate_GenericGivesEditableCopy |
| BIB-020 | I | Liste des modèles groupés par fabricant, avec recherche et filtres (fabricant, catégorie). | Automatique | LibraryViewModelTests.List_GroupsByManufacturer_AndFilters |
| BIB-021 | I | Édition des modes en onglets ; ajout, suppression, réordonnancement des canaux d'un mode p… | Automatique | FixtureEditsTests.AddAndRemoveSlot<br>FixtureEditsTests.MoveSlot_ReordersChannels<br>LibraryViewModelTests.Editor_ModesChannelsAndRanges |
| BIB-022 | I | Édition des plages d'un canal dans un tableau + **barre 0-255** colorée par plage, redimen… | Automatique | FixtureLibraryTests.MoveBoundary_KeepsRangesAdjacent |
| BIB-023 | I | Saisie rapide de plages : « découper en N plages égales », « remplir le trou suivant ». | Automatique | FixtureLibraryTests.FillNextGap_AddsRangeInFirstHole<br>FixtureLibraryTests.Split_InEightEqualRanges_CoversWholeChannel |
| BIB-024 | I | Annuler / rétablir (GEN-102). | Automatique | LibraryViewModelTests.Editor_UndoRedo |
| BIB-025 | M | Éditeur de roues : emplacements avec nom, couleur (sélecteur), image de gobo. | Automatique | FixtureEditsTests.Wheels_AddUpdateRemove |
| BIB-026 | M | Aperçu de la fiche « réglage sur l'appareil » (mode + adresse) telle qu'elle apparaîtra à … | Automatique | FixtureRulesTests.SettingSheet_ShowsChannelCountAndDeviceSetting |
| BIB-027 | S | Joindre la notice PDF et une photo au modèle (fichiers liés). | Manuel |  |
| BIB-060 | I | Bouton **Tester en direct** : patch temporaire du modèle (mode courant) à une adresse choi… | Automatique | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| BIB-061 | I | Clic sur une plage → émission de sa valeur médiane ; curseur de balayage dans la plage ; c… | Automatique | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| BIB-062 | M | Mode « **découverte** » : balayer lentement un canal de 0 à 255 en affichant la valeur, av… | Automatique | FixtureLibraryTests.SplitAt_CreatesBoundaryAtDiscoveredValue<br>LibraryViewModelTests.Discovery_NewBoundaryHere_SplitsRangesInEditor |
| BIB-063 | M | La sortie du test est la sortie active (Arduino et/ou simulateur) ; à la fermeture du test… | Automatique | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| BIB-080 | I | **Import Open Fixture Library** (fichier JSON d'un appareil ou dossier) : modes, canaux, c… | Automatique | OflImporterTests.Fog_IsSmokeTagged<br>OflImporterTests.PixelBar_MatrixBecomesEightCells<br>OflImporterTests.ReferenceFiles_ImportToValidModels<br>OflImporterTests.RgbPar_ModesChannelsAndStrobeRanges<br>(+1) |
| BIB-081 | M | **Import QLC+** (`.qxf`) : canaux (groupes → attributs), capacités → plages, modes, têtes … | Automatique | QlcImporterTests.Bar_HeadsBecomeCells<br>QlcImporterTests.ClassicPar_GroupsAndStrobeInProgram<br>QlcImporterTests.ReferenceFiles_ImportToValidModels<br>QlcImporterTests.Wash_PresetsAndFineChannels |
| BIB-082 | I | Rapport d'import : éléments non convertis ou approximés, listés clairement ; l'import ne b… | Automatique | LibraryViewModelTests.Import_SavesNewModels_SkipsExisting_AndReports<br>OflImporterTests.BrokenFile_GivesErrorWithoutThrowing<br>OflImporterTests.UnknownCapability_BecomesGeneric_AndIsReported<br>QlcImporterTests.Batch_ImportsWholeFolder_AndReportsProgress |
| BIB-083 | M | Import de **lots** (dossier entier) sans figer l'interface (GEN-109). | Automatique | LibraryViewModelTests.Import_SavesNewModels_SkipsExisting_AndReports<br>QlcImporterTests.Batch_ImportsWholeFolder_AndReportsProgress |
| BIB-084 | S | Export au format OFL (partage, contribution). | Manuel |  |
| CONS-060 | I | Le composant « faders d'un appareil » est réutilisable dans l'éditeur de bibliothèque pour… | Automatique | LibraryViewModelTests.LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases |
| GEN-020 | I | En interne, toute valeur d'attribut est une **valeur logique normalisée** entre 0 et 1, av… | Automatique | DmxConversionTests.Half_Is128In8Bit_And0x8000In16Bit<br>DmxConversionTests.SixteenBit_KeepsMoreResolutionThan8Bit<br>DmxConversionTests.To8Bit_ClampsAndRounds |
| GEN-021 | I | L'interface affiche les valeurs dans l'unité la plus parlante : **%** pour les intensités,… | Automatique | DmxConversionTests.Describe_UsesMostMeaningfulUnit |
| GEN-052 | I | Les objets se référencent par un **identifiant stable** (généré à la création), jamais par… | Automatique | PreferencesAndProjectTests.Project_CreateThenOpen |
| GEN-058 | S | Les chemins sont relatifs au dossier du projet ou de la bibliothèque (projet déplaçable). | Manuel |  |
| GEN-102 | I | **Annuler / rétablir** dans tous les éditeurs de l'Atelier (au moins 50 niveaux). | Automatique | LibraryViewModelTests.Editor_UndoRedo<br>LibraryViewModelTests.History_Keeps100Levels |
| GEN-105 | M | Recherche / filtre dans toute liste de plus de 20 éléments (modèles, scènes, palettes…). | Manuel |  |
