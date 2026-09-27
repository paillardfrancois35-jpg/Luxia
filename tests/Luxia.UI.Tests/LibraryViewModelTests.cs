using Luxia.Fixtures;
using Luxia.Fixtures.Import;
using Luxia.Fixtures.Model;
using Luxia.UI.Modules.Library;

namespace Luxia.UI.Tests;

/// <summary>Écran Bibliothèque : édition, annuler / rétablir, enregistrement, import, test en direct (doc 12 §4-6).</summary>
public sealed class LibraryViewModelTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private LibraryViewModel _library = null!;

    public ValueTask InitializeAsync()
    {
        _library = new LibraryViewModel(_host.Runtime, _host.Dialogs);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "BIB-020")]
    public void List_GroupsByManufacturer_AndFilters()
    {
        _library.Groups.Single().Name.ShouldBe(GenericFixtures.Manufacturer);

        _library.Search = "fumée";

        _library.Groups.Single().Items.Single().Name.ShouldBe("Machine à fumée");
    }

    [Fact]
    [Trait("Exigence", "BIB-024")]
    [Trait("Exigence", "GEN-102")]
    public void Editor_UndoRedo()
    {
        _library.NewCommand.Execute(null);
        var editor = _library.Editor!;

        editor.Model = "PAR maison";
        editor.Manufacturer = "Moi";
        editor.Current.DisplayName.ShouldBe("Moi PAR maison");

        editor.UndoCommand.Execute(null);
        editor.Current.Manufacturer.ShouldBe("Nouveau fabricant");
        editor.Manufacturer.ShouldBe("Nouveau fabricant");

        editor.RedoCommand.Execute(null);
        editor.Current.DisplayName.ShouldBe("Moi PAR maison");
    }

    [Fact]
    [Trait("Exigence", "GEN-102")]
    public void History_Keeps100Levels()
    {
        _library.NewCommand.Execute(null);
        var editor = _library.Editor!;
        for (var i = 0; i < 120; i++)
        {
            editor.Model = $"M{i}";
        }

        var undone = 0;
        while (editor.UndoCommand.CanExecute(null))
        {
            editor.UndoCommand.Execute(null);
            undone++;
        }

        undone.ShouldBe(100);
    }

    [Fact]
    [Trait("Exigence", "BIB-004")]
    [Trait("Exigence", "BIB-009")]
    public void Save_BlockedByErrors_ThenSavedWithVersion()
    {
        _library.NewCommand.Execute(null);
        var editor = _library.Editor!;
        editor.Manufacturer = "Moi";
        editor.Model = "PAR";
        editor.RemoveSlotCommand.Execute(null); // mode sans canal : erreur

        _library.SaveCommand.Execute(null);
        _library.Message!.ShouldContain("Enregistrement impossible");

        editor.UndoCommand.Execute(null);
        _library.SaveCommand.Execute(null);
        File.Exists(Path.Combine(_host.Paths.Library, "Moi", "PAR.json")).ShouldBeTrue();
        editor.IsDirty.ShouldBeFalse();

        editor.Notes = "retouche";
        _library.SaveCommand.Execute(null);
        _library.Editor!.Current.Version.ShouldBe(2);
    }

    [Fact]
    [Trait("Exigence", "BIB-021")]
    [Trait("Exigence", "BIB-006")]
    public void Editor_ModesChannelsAndRanges()
    {
        _library.NewCommand.Execute(null);
        var editor = _library.Editor!;
        editor.NewChannelCommand.Execute(null);
        editor.NewChannelCommand.Execute(null);
        editor.Slots.Count.ShouldBe(3);

        editor.MoveSlot(2, 0);
        editor.Slots[0].Title.ShouldBe("Canal 3");

        var channel = editor.SelectedChannel!;
        channel.Attribute = Fixtures.Model.AttributeCatalog.Get(AttributeKind.Red);
        channel.SplitCount = 4;
        channel.SplitRangesCommand.Execute(null);

        var red = editor.Current.Channel(channel.Key)!;
        red.Attribute.ShouldBe(AttributeKind.Red);
        red.Capabilities.Count.ShouldBe(4);
        channel.FollowsIntensityHint.ShouldContain("non"); // le mode a un canal d'intensité (canal 1)

        channel.MoveBoundary(0, 100);
        editor.Current.Channel(channel.Key)!.Capabilities[0].Max.ShouldBe(100);
        editor.Current.Channel(channel.Key)!.Capabilities[1].Min.ShouldBe(101);
    }

    [Fact]
    [Trait("Exigence", "BIB-010")]
    public void Duplicate_GenericGivesEditableCopy()
    {
        _library.Open(GenericFixtures.Rgb.Id);
        _library.Editor!.IsReadOnly.ShouldBeTrue();

        _library.DuplicateCommand.Execute(null);

        _library.Editor!.IsReadOnly.ShouldBeFalse();
        _library.Editor.Current.DerivedFrom.ShouldBe(GenericFixtures.Rgb.Id);
    }

    [Fact]
    [Trait("Exigence", "BIB-060")]
    [Trait("Exigence", "BIB-061")]
    [Trait("Exigence", "BIB-063")]
    [Trait("Exigence", "CONS-060")]
    public void LiveTest_PatchesAtAddress_RangeClickSendsMedian_StopReleases()
    {
        _library.Open(GenericFixtures.Strobe.Id);
        _library.TestAddress = 100;

        _library.StartLiveTestCommand.Execute(null);
        _host.Tick();
        _library.LiveTest.Channels.Select(c => c.AbsoluteChannel).ShouldBe([100, 101]);
        _host.Runtime.Engine.OverrideCount(1).ShouldBe(2);

        var speed = _library.LiveTest.Channels[1];
        _library.LiveTest.SelectRange(speed, speed.Ranges[1].Capability);
        _host.Tick();
        _host.Frame()[100].ShouldBe((byte)133); // médiane de 10-255

        _library.StopLiveTestCommand.Execute(null);
        _host.Tick();
        _host.Runtime.Engine.OverrideCount(1).ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "BIB-062")]
    public void Discovery_NewBoundaryHere_SplitsRangesInEditor()
    {
        _library.Open(GenericFixtures.Dimmer.Id);
        _library.DuplicateCommand.Execute(null);
        _library.StartLiveTestCommand.Execute(null);
        var channel = _library.LiveTest.Channels[0];

        _library.LiveTest.StartDiscovery(channel);
        _library.LiveTest.DiscoveryStepCommand.Execute("60");
        _host.Dialogs.TextAnswers.Enqueue("Allumé");
        _library.LiveTest.NewBoundaryHereCommand.Execute(null);

        _library.Editor!.Current.Channels[0].Capabilities.Select(c => (c.Min, c.Max, c.Label)).ShouldBe([(0, 59, "Plage 1"), (60, 255, "Allumé")]);
    }

    [Fact]
    [Trait("Exigence", "BIB-083")]
    [Trait("Exigence", "BIB-082")]
    [Trait("Exigence", "GEN-109")]
    public async Task Import_SavesNewModels_SkipsExisting_AndReports()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "samples", "Bibliothèque");
        var files = FixtureImporter.FindFiles(folder);

        await _library.ImportAsync(files);
        _host.Runtime.Library.Entries.Count(e => !e.IsBuiltIn).ShouldBe(8);
        _library.ImportReport.Count(l => l.StartsWith('✓')).ShouldBe(8);

        await _library.ImportAsync(files);
        _library.ImportReport.ShouldAllBe(l => l.StartsWith('='));
        _host.Runtime.Library.Entries.Count(e => !e.IsBuiltIn).ShouldBe(8);
    }

    [Fact]
    [Trait("Exigence", "BIB-098")]
    public async Task Import_WithOverwrite_ReplacesExistingModel_KeepingItsId()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "samples", "Bibliothèque");
        var files = FixtureImporter.FindFiles(folder);
        await _library.ImportAsync(files);
        var before = _host.Runtime.Library.Entries.First(e => !e.IsBuiltIn);

        _library.OverwriteExisting = true;
        await _library.ImportAsync(files);

        _library.ImportReport.Count(l => l.Contains("remplacé", StringComparison.Ordinal)).ShouldBe(8);
        _host.Runtime.Library.Entries.Count(e => !e.IsBuiltIn).ShouldBe(8);
        var after = _host.Runtime.Library.Entries.First(e => e.Fixture.Id == before.Fixture.Id);
        after.Fixture.Version.ShouldBe(before.Fixture.Version + 1);
    }

    [Fact]
    [Trait("Exigence", "BIB-093")]
    public void Rebuild_WithActiveSearch_ExpandsMatchingGroups()
    {
        _library.Groups.Single().IsExpanded.ShouldBeFalse();

        _library.Search = "fumée";

        _library.Groups.Single().IsExpanded.ShouldBeTrue();

        _library.Search = string.Empty;

        _library.Groups.Single().IsExpanded.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "BIB-100")]
    public void Save_Rejected_SwitchesToValidationTabAndShowsErrorCount()
    {
        _library.Open(GenericFixtures.Dimmer.Id);
        _library.DuplicateCommand.Execute(null);
        var editor = _library.Editor!;
        editor.Apply("Vider le mode", f => f with { Modes = [f.Modes[0] with { Channels = [] }] });

        _library.SaveCommand.Execute(null);

        editor.HasErrors.ShouldBeTrue();
        editor.ErrorCount.ShouldBeGreaterThan(0);
        editor.SelectedTabIndex.ShouldBe(FixtureEditorViewModel.ValidationTabIndex);
    }
}
