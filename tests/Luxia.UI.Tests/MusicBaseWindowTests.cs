using System.Text;
using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Fenêtre « Base musicale » (MUS-027, MUS-028, MUS-029) : onglets Base et À classer, import et export CSV.</summary>
public sealed class MusicBaseWindowTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private MusicBaseViewModel _vm = null!;

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
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DisposeAsync();
    }

    private void WriteJournal(string rows)
    {
        Directory.CreateDirectory(_host.Paths.Logs);
        File.WriteAllText(
            Path.Combine(_host.Paths.Logs, "soiree-20261003.csv"),
            "heure;titre;artiste;application;style;confiance;méthode;imposé;show\r\n" + rows,
            new UTF8Encoding(true));
    }

    private MusicBaseViewModel Open() => _vm = new MusicBaseViewModel(_host.Runtime, _host.Dialogs);

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Base_ListsTheArtists_AndTheSearchFilters()
    {
        Open();

        _vm.Artists.Count.ShouldBeGreaterThan(300);
        _vm.Summary.ShouldContain("artiste(s)");

        _vm.Search = "queen";

        _vm.Artists.Select(a => a.Name).ShouldContain("Queen");
        _vm.Artists.Count.ShouldBeLessThan(10);
        _vm.Search = "zzzzzz";
        _vm.Artists.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public async Task ChangingTheStyle_IsMemorised_AndAnArtistCanBeAddedAndRemoved()
    {
        Open();
        _vm.Search = "queen";
        _vm.SelectedArtist = _vm.Artists.First(a => a.Name == "Queen");

        _vm.SetStyleCommand.Execute("festif");

        _vm.Artists.First(a => a.Name == "Queen").Style.ShouldBe("Festif / Tubes de soirée");
        _host.Runtime.Music.Base.FindArtist("queen")!.Source.ShouldBe("manuel");

        _vm.Search = string.Empty;
        _vm.NewArtist = "Mon Groupe Local";
        _vm.SelectedFamily = _vm.Families.First(f => f.Id == "rock");
        _vm.AddArtistCommand.Execute(null);
        _vm.Artists.ShouldContain(a => a.Name == "Mon Groupe Local");

        _vm.SelectedArtist = _vm.Artists.First(a => a.Name == "Mon Groupe Local");
        await _vm.RemoveArtistCommand.ExecuteAsync(null);
        _host.Dialogs.Confirmations.ShouldNotBeEmpty();
        _host.Runtime.Music.Base.FindArtist("mon groupe local").ShouldBeNull();

        // L'enregistrement est différé de 1,5 s ; la fermeture du service l'écrit tout de suite.
        _host.Runtime.Music.Dispose();
        File.ReadAllText(Path.Combine(_host.ProjectFolder, "artistes.json")).ShouldContain("festif");
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public async Task Remove_IsCancelled_WhenTheUserRefuses()
    {
        Open();
        _host.Dialogs.ConfirmAnswer = false;
        _vm.Search = "queen";
        _vm.SelectedArtist = _vm.Artists.First(a => a.Name == "Queen");

        await _vm.RemoveArtistCommand.ExecuteAsync(null);

        _host.Runtime.Music.Base.FindArtist("queen").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Alias_IsAdded_OrRefusedWhenItBelongsToAnotherArtist()
    {
        Open();
        _vm.Search = "queen";
        _vm.SelectedArtist = _vm.Artists.First(a => a.Name == "Queen");

        _vm.NewAlias = "Les Reines";
        _vm.AddAliasCommand.Execute(null);
        _host.Runtime.Music.Base.FindArtist("les reines")!.Name.ShouldBe("Queen");

        _vm.NewAlias = "Stromae";
        _vm.AddAliasCommand.Execute(null);
        _vm.Message.ShouldContain("refusé");
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public async Task Duplicates_AreListed_AndMerged()
    {
        Open();
        _host.Runtime.Music.Base.SetArtistStyle("Beatles Doublon", "rock");
        _host.Runtime.Music.Base.SetArtistStyle("Doublon Beatles", "rock");

        _vm.FindDuplicatesCommand.Execute(null);

        var row = _vm.Duplicates.ShouldHaveSingleItem();
        _vm.SelectedDuplicate = row;
        await _vm.MergeCommand.ExecuteAsync("first");

        _vm.Duplicates.ShouldBeEmpty();
        var names = new[] { _host.Runtime.Music.Base.FindArtist("beatles doublon"), _host.Runtime.Music.Base.FindArtist("doublon beatles") };
        names[0]!.Name.ShouldBe(names[1]!.Name, "les deux écritures désignent la même fiche");
    }

    [Fact]
    [Trait("Exigence", "MUS-028")]
    public void ToClassify_ListsUnknownTitlesByArtist_AndAKeyClassifiesAndAdvances()
    {
        WriteJournal(
            "21:02:10;Spider Dance;Artiste Alpha;Deezer;Inconnu;0;aucune;;\r\n" +
            "21:06:30;Autre titre;Artiste Alpha;Deezer;Inconnu;0;aucune;;\r\n" +
            "21:10:00;Radio Ga Ga;Queen;Deezer;Rock;80;artiste;;\r\n" +
            "21:15:00;Un titre;Artiste Beta;Deezer;Inconnu;0;aucune;;\r\n");
        Open();

        _vm.ToClassify.Select(r => r.Item.Artist).ShouldBe(["Artiste Alpha", "Artiste Beta"]);
        _vm.ClassifySummary.ShouldContain("2 artiste(s)");
        _vm.SelectedToClassify!.Item.Artist.ShouldBe("Artiste Alpha");
        _vm.FamilyOfKey("5")!.Id.ShouldBe("rock");
        _vm.FamilyOfKey("q")!.Id.ShouldBe("rocknroll");
        _vm.FamilyOfKey("r")!.Id.ShouldBe("slow");
        _vm.FamilyOfKey("x").ShouldBeNull();

        _vm.ClassifyCommand.Execute("latino");

        _vm.ToClassify.ShouldHaveSingleItem().Item.Artist.ShouldBe("Artiste Beta");
        _vm.SelectedToClassify!.Item.Artist.ShouldBe("Artiste Beta");
        _host.Runtime.Music.Base.FindArtist("artiste alpha")!.Styles.ShouldContainKey("latino");
        _host.Runtime.Music.Base.ToCorrectionSet().Corrections.ShouldHaveSingleItem();

        _vm.ClassifyCommand.Execute("pop");

        _vm.ToClassify.ShouldBeEmpty();
        _vm.ClassifySummary.ShouldContain("Rien à classer");
    }

    [Fact]
    [Trait("Exigence", "MUS-028")]
    public void ToClassify_Skip_MovesOnWithoutClassifying()
    {
        WriteJournal(
            "21:02:10;Spider Dance;Artiste Alpha;Deezer;Inconnu;0;aucune;;\r\n" +
            "21:15:00;Un titre;Artiste Beta;Deezer;Inconnu;0;aucune;;\r\n");
        Open();

        _vm.SkipCommand.Execute(null);

        _vm.SelectedToClassify!.Item.Artist.ShouldBe("Artiste Beta");
        _vm.ToClassify.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Exigence", "MUS-029")]
    public void Import_ClassifiesRows_AndSendsTheOthersToTheClassifyTab_Persisted()
    {
        Open();
        const string csv = "Track Name,Artist Name(s)\r\nTitre sans style,Artiste Gamma\r\nRadio Ga Ga,Queen\r\n";

        _vm.ImportFile(csv);

        _vm.Message.ShouldContain("1 à classer");
        _vm.SelectedTab.ShouldBe(1);
        _vm.ToClassify.ShouldHaveSingleItem().Item.Artist.ShouldBe("Artiste Gamma");
        _vm.ToClassify[0].Item.Source.ShouldBe("playlist");
        File.ReadAllText(Path.Combine(_host.ProjectFolder, "aclasser.json")).ShouldContain("Artiste Gamma");

        // Rouvrir la fenêtre retrouve la liste (le fichier du projet la garde).
        var reopened = new MusicBaseViewModel(_host.Runtime, _host.Dialogs);
        reopened.ToClassify.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Export_GivesTheArtistsAsCsv()
    {
        Open();

        var csv = _vm.ExportText();

        csv.ShouldStartWith("artiste;style;poids;alias;source");
        csv.ShouldContain("Queen;Rock;");
    }
}
