using Luxia.Music.Base;
using Luxia.Music.Identification;
using Luxia.Music.Normalization;

namespace Luxia.Music.Tests;

/// <summary>Édition de la base (MUS-027), artistes « Inconnu » (MUS-028), échange JSON des trois tables et migration du format 1 (MUS-029).</summary>
public sealed class EditingTests
{
    private static readonly TrackNormalizer Normalizer = new();

    private static MusicBase Small() => new(
        DefaultTaxonomy.Value,
        new ArtistSet
        {
            Artists =
            [
                new() { Name = "The Beatles", Style = "rock" },
                new() { Name = "Beatles", Style = "rocknroll", Aliases = ["Les Beatles"] },
                new() { Name = "Daniel Balavoine", Style = "variete" },
                new() { Name = "Balavoine Daniel", Style = "variete" },
                new() { Name = "Daniel Balavoin", Style = "variete" },
                new() { Name = "Queen", Style = "rock" },
                new() { Name = "Stromae", Style = "pop" },
            ],
        },
        new TitleSet { Titles = [new() { Artist = "Beatles", Title = "Help!", Style = "rocknroll" }] });

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Search_FindsByNameOrAlias_WithoutAccentsOrCase()
    {
        var musicBase = Small();

        musicBase.SearchArtists("BEATLES").Select(a => a.Name).ShouldBe(["Beatles", "The Beatles"]);
        musicBase.SearchArtists("les beat").Select(a => a.Name).ShouldBe(["Beatles"], "par alias");
        musicBase.SearchArtists(null).Count.ShouldBe(7);
        musicBase.SearchArtists("zzz").ShouldBeEmpty();
        musicBase.SearchArtists(null, 3).Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void SetStyle_CreatesOrUpdatesAnArtist_AndNotifies()
    {
        var musicBase = Small();
        var changes = 0;
        musicBase.Changed += (_, _) => changes++;

        musicBase.SetArtistStyle("Nouvel Artiste", "latino");
        musicBase.SetArtistStyle("queen", "festif");

        musicBase.FindArtist("nouvel artiste")!.Style.ShouldBe("latino");
        musicBase.FindArtist("nouvel artiste")!.Code.ShouldNotBeEmpty();
        musicBase.FindArtist("queen")!.Style.ShouldBe("festif");
        musicBase.ArtistCount.ShouldBe(8);
        changes.ShouldBe(2);
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Remove_DropsTheArtistTheirTitlesAndTheFuzzyIndexEntry()
    {
        var musicBase = Small();

        musicBase.RemoveArtist("Beatles").ShouldBeTrue();

        musicBase.FindArtist("beatles").ShouldBeNull();
        musicBase.FindArtist("les beatles").ShouldBeNull("l'alias disparaît aussi");
        musicBase.ArtistCount.ShouldBe(6);
        musicBase.TitlesOf("Beatles").ShouldBeEmpty();
        musicBase.FuzzyArtists("beatles", 0.8).ShouldNotContain(a => a.Artist.Name == "Beatles");
        musicBase.ToArtistSet().Artists.ShouldNotContain(a => a.Name == "Beatles");
        musicBase.RemoveArtist("Beatles").ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Alias_CannotStealAnotherArtistsName()
    {
        var musicBase = Small();

        musicBase.AddAlias("Queen", "Les Reines").ShouldBeTrue();
        musicBase.FindArtist("les reines")!.Name.ShouldBe("Queen");
        musicBase.AddAlias("Queen", "Stromae").ShouldBeFalse();
        musicBase.AddAlias("Inconnu", "x").ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Duplicates_AreFound_WithArticlesWordOrderAndTypos()
    {
        var pairs = Small().FindDuplicates();
        var names = pairs.Select(p => string.Join(" / ", new[] { p.First.Name, p.Second.Name }.Order(StringComparer.Ordinal))).ToList();

        names.ShouldContain("Beatles / The Beatles");
        names.ShouldContain("Balavoine Daniel / Daniel Balavoine");
        names.ShouldContain(n => n.Contains("Daniel Balavoin", StringComparison.Ordinal) && n.Contains("Daniel Balavoine", StringComparison.Ordinal));
        names.ShouldNotContain(n => n.Contains("Queen", StringComparison.Ordinal) || n.Contains("Stromae", StringComparison.Ordinal));
        pairs[0].Score.ShouldBe(1.0, 1e-9, "les doublons certains d'abord");
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Merge_KeepsAliasesTitlesAndStyle_AndRemovesTheOther()
    {
        var musicBase = Small();

        musicBase.Merge("The Beatles", "Beatles").ShouldBeTrue();

        var kept = musicBase.FindArtist("the beatles")!;
        kept.Aliases!.ShouldContain("Beatles");
        kept.Aliases.ShouldContain("Les Beatles");
        kept.Style.ShouldBe("rock", "le style de la fiche gardée");
        musicBase.FindArtist("beatles")!.Name.ShouldBe("The Beatles", "l'ancien nom est un alias");
        musicBase.TitlesOf("The Beatles").Select(t => t.Title).ShouldBe(["Help!"]);
        musicBase.TitlesOf("The Beatles")[0].Artist.ShouldBe("The Beatles");
        musicBase.ArtistCount.ShouldBe(6);
        musicBase.Merge("The Beatles", "The Beatles").ShouldBeFalse();
        musicBase.Merge("The Beatles", "Inconnu").ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Titles_CanBeAddedReplacedAndRemoved()
    {
        var musicBase = Small();

        musicBase.UpsertTitle(new TitleEntry { Artist = "Queen", Title = "Radio Ga Ga", Style = "80s" });
        musicBase.UpsertTitle(new TitleEntry { Artist = "Queen", Title = "radio ga ga", Style = "rock" });

        musicBase.TitlesOf("Queen").ShouldHaveSingleItem().Style.ShouldBe("rock");
        musicBase.RemoveTitle("Queen", "Radio Ga Ga").ShouldBeTrue();
        musicBase.TitlesOf("Queen").ShouldBeEmpty();
        musicBase.RemoveTitle("Queen", "Radio Ga Ga").ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Codes_AreStable_AndUnique_EvenAfterARename()
    {
        var musicBase = Small();
        var queen = musicBase.FindArtist("queen")!;

        musicBase.ToArtistSet().Artists.Select(a => a.Code).Distinct().Count().ShouldBe(7);
        musicBase.SaveArtist(queen.Code, queen with { Name = "Queen (groupe)" }, []).ShouldBeNull();

        var renamed = musicBase.FindArtistByCode(queen.Code)!;
        renamed.Name.ShouldBe("Queen (groupe)");
        musicBase.FindArtist("queen").ShouldBeNull("l'ancien nom n'est pas gardé comme alias");
        musicBase.SetArtistStyle("Nouveau", "pop");
        musicBase.FindArtist("nouveau")!.Code.ShouldNotBe(queen.Code);
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void SaveArtist_ReplacesTheWholeSheet_NameAliasesStyleAndTitles()
    {
        var musicBase = Small();
        var beatles = musicBase.FindArtist("beatles")!;

        var error = musicBase.SaveArtist(
            beatles.Code,
            beatles with { Style = "rock", Aliases = ["The Fab Four"] },
            [new TitleEntry { Title = "Yesterday", Style = "slow" }, new TitleEntry { Title = "Let it be" }]);

        error.ShouldBeNull();
        musicBase.FindArtist("the fab four")!.Name.ShouldBe("Beatles");
        musicBase.FindArtist("les beatles").ShouldBeNull("l'alias retiré de la fiche disparaît");
        musicBase.TitlesOf("Beatles").Select(t => t.Title).ShouldBe(["Let it be", "Yesterday"]);
        musicBase.TitlesOf("Beatles").Select(t => t.Artist).Distinct().ShouldBe(["Beatles"]);
        musicBase.FindArtist("beatles")!.Style.ShouldBe("rock");
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void SaveArtist_RefusesAnEmptyNameAStolenNameOrAliasAndAnUnknownStyle_ChangingNothing()
    {
        var musicBase = Small();
        var queen = musicBase.FindArtist("queen")!;
        var changes = 0;
        musicBase.Changed += (_, _) => changes++;

        musicBase.SaveArtist(queen.Code, queen with { Name = " " }, []).ShouldNotBeNull();
        musicBase.SaveArtist(queen.Code, queen with { Name = "Stromae" }, [])!.ShouldContain("Stromae");
        musicBase.SaveArtist(queen.Code, queen with { Aliases = ["Les Beatles"] }, [])!.ShouldContain("Beatles");
        musicBase.SaveArtist(queen.Code, queen with { Style = "n'importe quoi" }, []).ShouldNotBeNull();
        musicBase.SaveArtist("A99999", queen, []).ShouldNotBeNull();

        changes.ShouldBe(0);
        musicBase.FindArtist("queen")!.Aliases.ShouldBeEmpty();
        musicBase.FindArtist("queen")!.Style.ShouldBe("rock");
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void SaveArtist_WithoutACode_CreatesTheArtist()
    {
        var musicBase = Small();

        musicBase.SaveArtist(null, new ArtistEntry { Name = "Zaz", Aliases = ["Isabelle Geffroy"], Style = "variete" }, [new TitleEntry { Title = "Je veux" }]).ShouldBeNull();

        musicBase.FindArtist("isabelle geffroy")!.Name.ShouldBe("Zaz");
        musicBase.TitlesOf("Zaz").ShouldHaveSingleItem().Title.ShouldBe("Je veux");
        musicBase.ArtistCount.ShouldBe(8);
    }

    [Fact]
    [Trait("Exigence", "MUS-028")]
    [Trait("Exigence", "MUS-030")]
    public void UnknownArtists_AreFilteredByTheUnknownStyle()
    {
        var musicBase = Small();
        musicBase.InjectUnknownArtist("Mystère").ShouldBeTrue();
        musicBase.InjectUnknownArtist("  ").ShouldBeFalse();

        musicBase.SearchArtists(null, 500, Taxonomy.UnknownId).Select(a => a.Name).ShouldBe(["Mystère"]);

        musicBase.SetArtistStyle("Mystère", "pop");

        musicBase.SearchArtists(null, 500, Taxonomy.UnknownId).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MUS-028")]
    public void SetTitleStyle_CreatesTheTitleAndInjectsAMissingArtist()
    {
        var musicBase = Small();

        musicBase.SetTitleStyle("Inconnu Total", "Son titre", null, "slow");

        musicBase.FindArtist("inconnu total")!.Style.ShouldBe("inconnu");
        musicBase.TitlesOf("Inconnu Total").ShouldHaveSingleItem().Style.ShouldBe("slow");
    }

    [Fact]
    [Trait("Exigence", "MUS-029")]
    public void Exchange_ExportsThreeTables_AndReimportsThemIntoAnEmptyBase()
    {
        var source = Small();
        source.AddAlias("Queen", "Les Reines");
        source.UpsertFamily(new MusicFamily { Id = "chill", Name = "Chill", Labels = ["lounge"] });
        source.SetArtistStyle("Nouveau Chill", "chill");
        var folder = Path.Combine(Path.GetTempPath(), "luxia-music-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var file = Path.Combine(folder, "echange.json");
            MusicExchange.ExportToFile(source, file);
            File.ReadAllText(file).ShouldContain("\"formatVersion\": 1");

            var target = new MusicBase(DefaultTaxonomy.Value, new ArtistSet(), new TitleSet());
            var report = MusicExchange.ImportFromFile(target, file);

            report.Problems.ShouldBeEmpty();
            report.ArtistsAdded.ShouldBe(8);
            report.AliasesAdded.ShouldBe(2);
            target.FindArtist("les reines")!.Name.ShouldBe("Queen");
            target.FindArtist("nouveau chill")!.Style.ShouldBe("chill");
            target.FindFamily("chill")!.Labels.ShouldBe(["lounge"]);
            target.FindArtist("queen")!.Code.ShouldBe(source.FindArtist("queen")!.Code, "les codes voyagent");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    [Trait("Exigence", "MUS-029")]
    public void Import_UpdatesByCode_RefusesStolenAliases_AndNeverDeletes()
    {
        var musicBase = Small();
        var queen = musicBase.FindArtist("queen")!;

        var report = MusicExchange.Import(
            musicBase,
            new ExchangeFile
            {
                Styles = [new ExchangeStyle { Code = "rock", Label = "Rock & co", Order = 5 }],
                Artists =
                [
                    new ExchangeArtist { Code = queen.Code, Name = "Queen", Style = "pop" },
                    new ExchangeArtist { Name = "Stromae", Style = "style-inexistant" },
                    new ExchangeArtist { Name = "Nouvel Artiste", Style = "latino" },
                ],
                Aliases =
                [
                    new ExchangeAlias { Alias = "Les Beatles", Artist = queen.Code },
                    new ExchangeAlias { Alias = "Freddie", Artist = queen.Code },
                    new ExchangeAlias { Alias = "x", Artist = "A99999" },
                ],
            });

        report.ArtistsUpdated.ShouldBe(2);
        report.ArtistsAdded.ShouldBe(1);
        report.AliasesAdded.ShouldBe(1);
        report.Problems.Count.ShouldBe(3, "style inconnu, alias volé, artiste de l'alias introuvable");
        musicBase.FindArtist("queen")!.Style.ShouldBe("pop");
        musicBase.FindArtist("stromae")!.Style.ShouldBe("inconnu");
        musicBase.FindFamily("rock")!.Name.ShouldBe("Rock & co");
        musicBase.FindArtist("freddie")!.Name.ShouldBe("Queen");
        musicBase.FindArtist("les beatles")!.Name.ShouldBe("Beatles");
        musicBase.ArtistCount.ShouldBe(8);
    }

    [Fact]
    [Trait("Exigence", "MUS-029")]
    public void Import_OfAnUnreadableFile_ReportsTheReason()
    {
        var folder = Path.Combine(Path.GetTempPath(), "luxia-music-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var file = Path.Combine(folder, "casse.json");
            File.WriteAllText(file, "pas du json");

            MusicExchange.ImportFromFile(Small(), file).Problems.ShouldHaveSingleItem();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    [Trait("Exigence", "GEN-051")]
    public void Format1Files_AreMigrated_KeepingTheDominantStyle_AndTheOriginalAsBak()
    {
        var folder = Path.Combine(Path.GetTempPath(), "luxia-music-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(
                Path.Combine(folder, MusicStore.ArtistsFile),
                """{"formatVersion":1,"artists":[{"name":"Tiësto","aliases":["DJ Tiesto"],"styles":{"house":0.3,"electro":0.7},"source":"initial"},{"name":"Sans style","styles":{},"source":"manuel"}]}""");
            File.WriteAllText(
                Path.Combine(folder, MusicStore.TitlesFile),
                """{"formatVersion":1,"titles":[{"artist":"Tiësto","title":"Adagio for Strings","style":"electro","source":"correction"}]}""");
            File.WriteAllText(Path.Combine(folder, "corrections.json"), """{"formatVersion":1,"corrections":[]}""");
            File.WriteAllText(Path.Combine(folder, "aclasser.json"), """{"formatVersion":1,"items":[]}""");

            var (musicBase, messages) = MusicStore.Load(folder);

            musicBase.FindArtist("tiesto")!.Style.ShouldBe("electro");
            musicBase.FindArtist("tiesto")!.Aliases.ShouldBe(["DJ Tiesto"]);
            musicBase.FindArtist("sans style")!.Style.ShouldBe("inconnu");
            musicBase.ToArtistSet().Artists.Select(a => a.Code).ShouldBe(["A00001", "A00002"]);
            musicBase.TitlesOf("Tiësto").ShouldHaveSingleItem().Style.ShouldBe("electro");
            File.Exists(Path.Combine(folder, MusicStore.ArtistsFile + ".v1.bak")).ShouldBeTrue();
            File.Exists(Path.Combine(folder, MusicStore.TitlesFile + ".v1.bak")).ShouldBeTrue();
            File.Exists(Path.Combine(folder, "corrections.json")).ShouldBeFalse();
            File.Exists(Path.Combine(folder, "corrections.json.v1.bak")).ShouldBeTrue();
            File.Exists(Path.Combine(folder, "aclasser.json.v1.bak")).ShouldBeTrue();
            messages.ShouldNotBeEmpty();
            File.ReadAllText(Path.Combine(folder, MusicStore.ArtistsFile)).ShouldContain("\"formatVersion\": 2");
            File.ReadAllText(Path.Combine(folder, MusicStore.ArtistsFile)).ShouldNotContain("\"styles\"");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    [Trait("Exigence", "MUS-029")]
    public void TheExampleExchangeFile_OfTheGuide_GivesTheAnnouncedReport()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docs", "demos", "P9-exemple-echange.json")))
        {
            dir = dir.Parent;
        }

        dir.ShouldNotBeNull("le dépôt (docs/demos) est introuvable depuis le dossier de test");
        var musicBase = MusicStore.Default();

        var report = MusicExchange.ImportFromFile(musicBase, Path.Combine(dir.FullName, "docs", "demos", "P9-exemple-echange.json"));

        (report.ArtistsAdded, report.ArtistsUpdated, report.AliasesAdded, report.StylesChanged, report.Problems.Count).ShouldBe((4, 1, 2, 1, 1));
        musicBase.FindArtist("quartet imaginaire")!.Style.ShouldBe("chill");
        musicBase.FindArtist("un artiste que personne ne connait")!.Style.ShouldBe("inconnu");
        musicBase.FindArtist("queen")!.Name.ShouldBe("Queen");
    }
}
