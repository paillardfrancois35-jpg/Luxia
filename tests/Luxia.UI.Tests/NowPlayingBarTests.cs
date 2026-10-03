using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Bloc « Morceau en cours » de l'écran de jeu (P9, LIVE-022, MUS-007, MUS-024, MUS-026).</summary>
public sealed class NowPlayingBarTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private GameViewModel _vm = null!;

    public ValueTask InitializeAsync()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
        foreach (var file in Directory.EnumerateFiles(source, "*.json", SearchOption.AllDirectories))
        {
            var target = Path.Combine(_host.ProjectFolder, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        _host.Runtime.Project.Open(_host.ProjectFolder).ShouldBeTrue();
        _vm = new GameViewModel(_host.Runtime, _host.Dialogs);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "MUS-006")]
    public void WithoutTrack_ExplainsWhatToDo_AndOffersNoCorrection()
    {
        _vm.Refresh();

        _vm.NowPlaying.HasTrack.ShouldBeFalse();
        _vm.NowPlaying.CanAct.ShouldBeFalse();
        _vm.NowPlaying.EmptyText.ShouldNotBeNullOrWhiteSpace();
        _vm.NowPlaying.Style.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "LIVE-022")]
    public void IdentifiedTrack_ShowsTitleArtistStyleAndConfidence()
    {
        _vm.NowPlaying.SetManual("Radio Ga Ga", "Queen");

        _vm.NowPlaying.HasTrack.ShouldBeTrue();
        _vm.NowPlaying.Title.ShouldBe("Radio Ga Ga");
        _vm.NowPlaying.Artist.ShouldBe("Queen");
        _vm.NowPlaying.Style.ShouldBe("Rock");
        _vm.NowPlaying.Confidence.ShouldNotBeNullOrWhiteSpace();
        _vm.NowPlaying.StyleColor.ShouldBe("#3FB950", "confiance de 80 % : vert");
        _vm.NowPlaying.IsForced.ShouldBeFalse();
        _vm.NowPlaying.CanAct.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "LIVE-022")]
    public void UnknownTrack_IsGray_AndTheStyleIsUnknown()
    {
        _vm.NowPlaying.SetManual("Titre inconnu", "Artiste Totalement Inconnu");

        _vm.NowPlaying.Style.ShouldBe("Inconnu");
        _vm.NowPlaying.Confidence.ShouldBeEmpty();
        _vm.NowPlaying.StyleColor.ShouldBe("#8B949E");
    }

    [Fact]
    [Trait("Exigence", "MUS-026")]
    public void ForcedStyle_IsShown_AndReleasedWithTheDetection()
    {
        _vm.NowPlaying.SetManual("Radio Ga Ga", "Queen");

        _vm.NowPlaying.Force("latino");

        _vm.NowPlaying.IsForced.ShouldBeTrue();
        _vm.NowPlaying.Style.ShouldBe("Latino");

        _vm.NowPlaying.Force(null);

        _vm.NowPlaying.IsForced.ShouldBeFalse();
        _vm.NowPlaying.Style.ShouldBe("Rock");
    }

    [Fact]
    [Trait("Exigence", "MUS-024")]
    public void Correction_ChangesTheStyleAtOnce_AndWritesTheProjectFiles()
    {
        _vm.NowPlaying.SetManual("Radio Ga Ga", "Queen");

        _vm.NowPlaying.Correct("artist:festif");

        _vm.NowPlaying.Style.ShouldBe("Festif / Tubes de soirée");
        _vm.NowPlaying.Confidence.Replace(' ', ' ').Replace(' ', ' ').ShouldBe("100 %");
        _vm.Journal.Refresh();
        _vm.Journal.Lines.ShouldContain(l => l.Contains("style corrigé", StringComparison.Ordinal));

        // L'enregistrement dans le projet est différé de 1,5 s ; la fermeture du service l'écrit tout de suite.
        _host.Runtime.Music.Dispose();
        File.Exists(Path.Combine(_host.ProjectFolder, "corrections.json")).ShouldBeTrue();
        File.ReadAllText(Path.Combine(_host.ProjectFolder, "corrections.json")).ShouldContain("festif");
    }

    [Fact]
    [Trait("Exigence", "MUS-024")]
    public void Correction_WithoutTrack_IsRefusedInTheJournal()
    {
        _vm.NowPlaying.Correct("artist:rock");
        _vm.Journal.Refresh();

        _vm.Journal.Lines.ShouldContain(l => l.Contains("correction du style", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Exigence", "MUS-007")]
    public void ManualEntry_IgnoresAnEmptyTitle()
    {
        _vm.NowPlaying.SetManual("  ", "Queen");

        _vm.NowPlaying.HasTrack.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MUS-023")]
    public void Menus_ListTheFamiliesOfTheTaxonomy()
    {
        _vm.NowPlaying.Families.Count.ShouldBe(14);
        _vm.NowPlaying.Families.Select(f => f.Name).ShouldContain("Électro / Dance");
    }
}
