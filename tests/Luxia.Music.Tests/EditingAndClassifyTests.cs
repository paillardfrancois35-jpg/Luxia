using Luxia.Music.Base;
using Luxia.Music.Classification;
using Luxia.Music.Identification;
using Luxia.Music.Normalization;

namespace Luxia.Music.Tests;

/// <summary>Édition de la base (MUS-027), liste « À classer » (MUS-028), import et export CSV (MUS-027, MUS-029).</summary>
public sealed class EditingAndClassifyTests
{
    private static readonly TrackNormalizer Normalizer = new();

    private static MusicBase Small() => new(
        DefaultTaxonomy.Value,
        new ArtistSet
        {
            Artists =
            [
                new() { Name = "The Beatles", Styles = new Dictionary<string, double> { ["rock"] = 1 } },
                new() { Name = "Beatles", Styles = new Dictionary<string, double> { ["rocknroll"] = 1 }, Aliases = ["Les Beatles"] },
                new() { Name = "Daniel Balavoine", Styles = new Dictionary<string, double> { ["variete"] = 1 } },
                new() { Name = "Balavoine Daniel", Styles = new Dictionary<string, double> { ["variete"] = 1 } },
                new() { Name = "Daniel Balavoin", Styles = new Dictionary<string, double> { ["variete"] = 1 } },
                new() { Name = "Queen", Styles = new Dictionary<string, double> { ["rock"] = 1 } },
                new() { Name = "Stromae", Styles = new Dictionary<string, double> { ["pop"] = 1 } },
            ],
        },
        new TitleSet { Titles = [new() { Artist = "Beatles", Title = "Help!", Style = "rocknroll" }] },
        new CorrectionSet());

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

        musicBase.FindArtist("nouvel artiste")!.Styles.ShouldContainKey("latino");
        musicBase.FindArtist("queen")!.Styles.Keys.ShouldBe(["festif"]);
        musicBase.FindArtist("queen")!.Source.ShouldBe("manuel");
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
        kept.Aliases.ShouldContain("Beatles");
        kept.Aliases.ShouldContain("Les Beatles");
        kept.Styles.Keys.ShouldBe(["rock"], "le style de la fiche gardée");
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
    [Trait("Exigence", "GEN-111")]
    public void EveningJournal_ParsesQuotesBomAndOldFormat()
    {
        const string csv = "﻿heure;titre;artiste;application;style;confiance;méthode;imposé;show\r\n" +
            "21:12:05;Radio Ga Ga;Queen;Deezer;Rock;80;artiste;;\r\n" +
            "21:16:40;\"Un titre; avec \"\"guillemets\"\"\";Inconnu Total;Chrome;Inconnu;0;aucune;oui;Couplet / Refrain / Drop\r\n";

        var entries = EveningJournal.Parse(csv);

        entries.Count.ShouldBe(2);
        entries[0].Title.ShouldBe("Radio Ga Ga");
        entries[0].Confidence.ShouldBe(0.8, 1e-9);
        entries[1].Title.ShouldBe("Un titre; avec \"guillemets\"");
        entries[1].Forced.ShouldBeTrue();
        entries[1].Show.ShouldBe("Couplet / Refrain / Drop");

        var old = EveningJournal.Parse("heure;titre;artiste;style;confiance;méthode;imposé;show\r\n21:00:00;Titre;Artiste;Rock;50;artiste;;\r\n");
        old.ShouldHaveSingleItem().App.ShouldBeEmpty();
        EveningJournal.Parse("n'importe quoi\r\nautre").ShouldBeEmpty();
        EveningJournal.Parse(string.Empty).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MUS-028")]
    public void Queue_GroupsByArtist_MostPlayedFirst_AndHidesWhatIsKnown()
    {
        var musicBase = Small();
        var identifier = new StyleIdentifier(musicBase);
        var entries = new[]
        {
            Entry("Radio Ga Ga", "Queen"),
            Entry("Titre A", "Artiste Inconnu"),
            Entry("Titre A", "Artiste Inconnu"),
            Entry("Titre B", "Artiste Inconnu"),
            Entry("Autre titre", "Un Autre Inconnu"),
            Entry("Daft Punk - One More Time", "Chaîne YouTube", "Chrome"),
        };

        var list = ClassifyList.Build(entries, [], musicBase, Normalizer, identifier);

        list.Select(i => i.Artist).ShouldBe(["Artiste Inconnu", "Daft Punk", "Un Autre Inconnu"]);
        list[0].Plays.ShouldBe(3);
        list[0].Titles.Select(t => t.Title).ShouldBe(["Titre A", "Titre B"]);
        list[0].Titles[0].Plays.ShouldBe(2);
        list[0].HasArtist.ShouldBeTrue();
        list.ShouldNotContain(i => i.Artist == "Queen");
    }

    [Fact]
    [Trait("Exigence", "MUS-028")]
    public void Queue_ShrinksWhenAnArtistIsClassified_AndKeepsLowConfidenceGuesses()
    {
        var musicBase = Small();
        var identifier = new StyleIdentifier(musicBase);
        var entries = new[] { Entry("Titre A", "Artiste Inconnu"), Entry("Titre B", "Artiste Inconnu"), Entry("Autre titre", "Un Autre Inconnu") };

        ClassifyList.Build(entries, [], musicBase, Normalizer, identifier).Count.ShouldBe(2);

        musicBase.Correct("artist", "Artiste Inconnu", null, null, "pop", null, DateTimeOffset.UnixEpoch);

        var after = ClassifyList.Build(entries, [], musicBase, Normalizer, identifier);
        after.ShouldHaveSingleItem().Artist.ShouldBe("Un Autre Inconnu");

        // Un artiste proche d'un artiste connu (faute de frappe) est identifié avec peu de confiance : proposé, avec sa supposition.
        var weak = ClassifyList.Build([Entry("Un titre", "Stromaee")], [], musicBase, Normalizer, identifier).ShouldHaveSingleItem();
        weak.GuessFamilyId.ShouldBe("pop");
        weak.GuessConfidence.ShouldBeInRange(0.5, ClassifyList.LowConfidence);
    }

    [Fact]
    [Trait("Exigence", "MUS-029")]
    public void Queue_IncludesImportedPlaylistTitles_WithTheirSource()
    {
        var musicBase = Small();
        var pending = new[] { new PendingItem("Artiste Inconnu", "Titre A", string.Empty, "playlist", 0), new PendingItem("Queen", "Radio Ga Ga", string.Empty, "playlist", 0) };

        var list = ClassifyList.Build([Entry("Titre B", "Artiste Inconnu")], pending, musicBase, Normalizer, new StyleIdentifier(musicBase));

        var item = list.ShouldHaveSingleItem();
        item.Source.ShouldBe("journal, playlist");
        item.Titles.Count.ShouldBe(2);
        item.Plays.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Export_ListsEveryArtist_WithStyleAliasesAndSource()
    {
        var csv = MusicCsv.ExportArtists(Small());

        var lines = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        lines[0].ShouldBe("artiste;style;poids;alias;source");
        lines.Length.ShouldBe(8);
        lines.ShouldContain("Beatles;Rock'n'roll / Rétro;1;Les Beatles;manuel");
    }

    [Fact]
    [Trait("Exigence", "MUS-029")]
    public void Import_ClassifiesRowsWithAStyle_AndQueuesTheOthers()
    {
        var musicBase = Small();
        const string csv = "Track Name,Artist Name(s),Album Name,Genres\r\n" +
            "Radio Ga Ga,Queen,The Works,\r\n" +
            "Despacito,Luis Fonsi,Vida,latin\r\n" +
            "Titre sans style,Artiste Inconnu,Album,\r\n" +
            "Titre au style bizarre,Autre Artiste,Album,musique de chambre\r\n";

        var report = MusicCsv.Import(csv, musicBase, Normalizer, new StyleIdentifier(musicBase));

        report.Rows.ShouldBe(4);
        report.AlreadyKnown.ShouldBe(1, "Queen est connue");
        report.TitlesSet.ShouldBe(1);
        musicBase.FindTitle("luis fonsi", "despacito", string.Empty)!.Style.ShouldBe("latino");
        musicBase.FindTitle("luis fonsi", "despacito", string.Empty)!.Source.ShouldBe("import");
        report.ToClassify.Select(p => p.Title).ShouldBe(["Titre sans style", "Titre au style bizarre"]);
        report.ToClassify.ShouldAllBe(p => p.Source == "playlist");
        report.Problems.ShouldContain(p => p.Contains("style inconnu", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Exigence", "MUS-029")]
    public void Import_UnderstandsFrenchHeaders_ArtistOnlyRows_NoHeader_AndOneColumn()
    {
        var musicBase = Small();
        var identifier = new StyleIdentifier(musicBase);

        var french = MusicCsv.Import("artiste;titre;style\r\nAya Nakamura;;hip-hop\r\nJul;Tchikita;rap\r\n", musicBase, Normalizer, identifier);
        french.ArtistsSet.ShouldBe(1);
        french.TitlesSet.ShouldBe(1);
        musicBase.FindArtist("aya nakamura")!.Styles.ShouldContainKey("hiphop");

        var positional = MusicCsv.Import("Gims;La même;Hip-hop\r\n", musicBase, Normalizer, identifier);
        positional.TitlesSet.ShouldBe(1);

        var oneColumn = MusicCsv.Import("Daft Punk - One More Time\r\nQueen - Radio Ga Ga\r\n", musicBase, Normalizer, identifier);
        oneColumn.Rows.ShouldBe(2);
        oneColumn.AlreadyKnown.ShouldBe(1);
        oneColumn.ToClassify.ShouldHaveSingleItem();

        MusicCsv.Import(string.Empty, musicBase, Normalizer, identifier).Problems.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MUS-029")]
    public void GuideExamplePlaylist_GivesTheBilanAnnouncedInTheP9Guide()
    {
        // Même contenu que docs/demos/P9-exemple-playlist.csv (le guide d'essai annonce ce bilan).
        const string csv = "artiste;titre;style\nQueen;Radio Ga Ga;\nDaft Punk;One More Time;\nLes Vagabonds Imaginaires;La java du quartier;Bal\n" +
            "Orchestre Imaginaire;Valse du dimanche;\nAya Nakamura;Djadja;hip-hop\nUn Artiste Que Personne Ne Connaît;Son premier tube;\n" +
            "Maître Gims;Bella;rap\nQuartet Imaginaire;Valse des imaginaires;\n";
        var musicBase = MusicStore.Default();

        var report = MusicCsv.Import(csv, musicBase, Normalizer, new StyleIdentifier(musicBase));

        (report.Rows, report.TitlesSet, report.ArtistsSet, report.AlreadyKnown, report.ToClassify.Count).ShouldBe((8, 3, 0, 2, 3));
        report.Problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MUS-029")]
    public void Pending_IsKeptInTheProjectFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "luxia-music-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            MusicStore.LoadPending(folder).Items.ShouldBeEmpty();

            MusicStore.SavePending(folder, new PendingSet { Items = [new PendingItem("Artiste", "Titre", string.Empty, "playlist", 0)] });

            MusicStore.LoadPending(folder).Items.ShouldHaveSingleItem().Title.ShouldBe("Titre");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static EveningEntry Entry(string title, string artist, string app = "Deezer") =>
        new("21:00:00", title, artist, app, "Inconnu", 0, "aucune", false, string.Empty);
}
