using System.Diagnostics;
using System.Globalization;
using Luxia.Music.Base;
using Luxia.Music.Identification;
using Luxia.Music.Normalization;

namespace Luxia.Music.Tests;

/// <summary>Base de départ (Q48), taux d'identification (T-MUS-03), performance (T-MUS-05), session de style, corrections (MUS-024 à 026).</summary>
public sealed class BaseAndSessionTests
{
    private static readonly TrackNormalizer Normalizer = new();

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void Seed_HasSeveralHundredArtists_AllInKnownFamilies_WithoutDuplicates()
    {
        var seed = SeedData.Artists();
        var families = DefaultTaxonomy.Value.Families.Select(f => f.Id).ToHashSet();

        seed.Artists.Count.ShouldBeGreaterThanOrEqualTo(300);
        seed.Artists.SelectMany(a => a.Styles.Keys).Distinct().ShouldBeSubsetOf(families);
        seed.Artists.ShouldAllBe(a => a.Source == SeedData.Source && a.Styles.Count > 0);

        var keys = seed.Artists.Select(a => TextKey.Of(a.Name)).ToList();
        keys.Distinct().Count().ShouldBe(keys.Count, "deux artistes de la base de départ ont le même nom");
    }

    [Fact]
    [Trait("Exigence", "MUS-023")]
    public void Taxonomy_Has14Families_FindableByIdNamePartOrLabel()
    {
        var musicBase = MusicStore.Default();

        musicBase.Taxonomy.Families.Count.ShouldBe(14);
        musicBase.FindFamily("rock")!.Id.ShouldBe("rock");
        musicBase.FindFamily("Électro / Dance")!.Id.ShouldBe("electro");
        musicBase.FindFamily("Électro")!.Id.ShouldBe("electro");
        musicBase.FindFamily("House")!.Id.ShouldBe("house");
        musicBase.FindFamily("Funk")!.Id.ShouldBe("disco");
        musicBase.FindFamily("eurodance")!.Id.ShouldBe("electro");
        musicBase.FindFamily("Rétro")!.Id.ShouldBe("rocknroll");
        musicBase.FindFamily("n'importe quoi").ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    [Trait("Exigence", "MUS-006")]
    public void IdentificationRate_OnTheAnnotatedSet_IsAbove80PercentForKnownArtists_AndNoFalsePositive()
    {
        var musicBase = MusicStore.Default();
        var identifier = new StyleIdentifier(musicBase);
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "identification-annotee.tsv");
        var known = 0;
        var correct = 0;
        var unknownExpected = 0;
        var falsePositives = new List<string>();
        var wrong = new List<string>();
        foreach (var line in File.ReadAllLines(path).Where(l => l.Length > 0 && l[0] != '#'))
        {
            var f = line.Split('\t');
            var result = identifier.Identify(Normalizer.Normalize(f[2], f[1], f[0]));
            if (f[3] == "inconnu")
            {
                unknownExpected++;
                if (result.IsKnown)
                {
                    falsePositives.Add($"{f[1]} / {f[2]} -> {result.FamilyId}");
                }

                continue;
            }

            known++;
            if (result.FamilyId == f[3])
            {
                correct++;
            }
            else
            {
                wrong.Add($"{f[1]} / {f[2]} : attendu {f[3]}, trouvé {result.FamilyId}");
            }
        }

        var rate = (double)correct / known;
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Taux d'identification : {correct}/{known} = {rate:P1} ; inconnus attendus : {unknownExpected}, faux positifs : {falsePositives.Count}"));
        foreach (var w in wrong)
        {
            Console.WriteLine("  ≠ " + w);
        }

        rate.ShouldBeGreaterThan(0.8, string.Join("\n", wrong));
        falsePositives.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MUS-022")]
    public void Identification_On50000Titles_And10000Artists_TakesLessThan200Milliseconds()
    {
        var random = new Random(1234);
        string Word() => new string(Enumerable.Range(0, random.Next(4, 9)).Select(_ => (char)('a' + random.Next(26))).ToArray());
        var families = DefaultTaxonomy.Value.Families.Select(f => f.Id).ToArray();
        var artists = Enumerable.Range(0, 10_000)
            .Select(i => new ArtistEntry { Name = $"{Word()} {Word()} {i}", Styles = new Dictionary<string, double> { [families[i % families.Length]] = 1.0 } })
            .ToList();
        var titles = Enumerable.Range(0, 50_000)
            .Select(i => new TitleEntry { Artist = artists[i % artists.Count].Name, Title = $"{Word()} {Word()} {Word()}", Style = families[(i / 3) % families.Length] })
            .ToList();
        var musicBase = new MusicBase(DefaultTaxonomy.Value, new ArtistSet { Artists = artists }, new TitleSet { Titles = titles }, new CorrectionSet());
        var identifier = new StyleIdentifier(musicBase);

        musicBase.ArtistCount.ShouldBe(10_000);
        musicBase.TitleCount.ShouldBe(50_000);

        // Exacts, flous (faute dans un long mot) et inconnus : le pire cas est l'inconnu (tous les rapprochements sont essayés).
        var tracks = new List<NormalizedTrack>();
        for (var i = 0; i < 60; i++)
        {
            var artist = artists[random.Next(artists.Count)].Name;
            var title = titles[random.Next(titles.Count)];
            tracks.Add(Normalizer.Normalize(title.Title, title.Artist, "Deezer"));
            tracks.Add(Normalizer.Normalize("Un titre quelconque", artist + "x", "Deezer"));
            tracks.Add(Normalizer.Normalize(Word() + " " + Word(), Word() + " " + Word(), "Deezer"));
        }

        identifier.Identify(tracks[0]);
        var worst = TimeSpan.Zero;
        foreach (var track in tracks)
        {
            var started = Stopwatch.GetTimestamp();
            identifier.Identify(track);
            var elapsed = Stopwatch.GetElapsedTime(started);
            if (elapsed > worst)
            {
                worst = elapsed;
            }
        }

        Console.WriteLine($"Identification, base de 50 000 titres : pire cas {worst.TotalMilliseconds:0.0} ms sur {tracks.Count} titres");
        worst.TotalMilliseconds.ShouldBeLessThan(200);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void Store_LoadsTheSeedWithoutFiles_AndRoundTripsCorrections()
    {
        var folder = Path.Combine(Path.GetTempPath(), "luxia-music-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var (musicBase, messages) = MusicStore.Load(folder);
            messages.ShouldBeEmpty();
            musicBase.ArtistCount.ShouldBeGreaterThanOrEqualTo(300);

            var session = new StyleSession(musicBase, Normalizer);
            session.Update("Titre quelconque", "Queen", "Deezer");
            session.Correct(CorrectionScope.Title, "slow", DateTimeOffset.Parse("2026-10-03T21:00:00Z", CultureInfo.InvariantCulture)).ShouldBeNull();
            MusicStore.Save(folder, musicBase);

            File.Exists(Path.Combine(folder, MusicStore.ArtistsFile)).ShouldBeTrue();
            var (reloaded, reloadMessages) = MusicStore.Load(folder);
            reloadMessages.ShouldBeEmpty();
            reloaded.ToCorrectionSet().Corrections.Count.ShouldBe(1);
            reloaded.ToCorrectionSet().Corrections[0].NewStyle.ShouldBe("slow");
            reloaded.ToTitleSet().Titles.ShouldContain(t => t.Title == "Titre quelconque" && t.Source == "correction");
            new StyleIdentifier(reloaded).Identify(Normalizer.Normalize("Titre quelconque", "Queen", "Deezer")).FamilyId.ShouldBe("slow");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void Store_SetsAsideACorruptFile_AndFallsBackToTheSeed()
    {
        var folder = Path.Combine(Path.GetTempPath(), "luxia-music-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, MusicStore.ArtistsFile), "{ pas du json");

            var (musicBase, messages) = MusicStore.Load(folder);

            messages.ShouldNotBeEmpty();
            musicBase.ArtistCount.ShouldBeGreaterThanOrEqualTo(300);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    [Trait("Exigence", "MUS-006")]
    public void Session_WithoutTrack_HasNoStyle_AndAnUnknownTrackIsUnknown()
    {
        var session = new StyleSession(MusicStore.Default(), Normalizer);

        session.State.StyleName.ShouldBeNull();
        session.State.HasTrack.ShouldBeFalse();

        session.Update("Un titre inconnu", "Personne", "Deezer");

        session.State.StyleName.ShouldBe("Inconnu");
        session.State.Effective.Confidence.ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void Session_PublishesTheStyleOfTheTrack_WithConfidenceAndMethod()
    {
        var session = new StyleSession(MusicStore.Default(), Normalizer);
        var states = new List<StyleState>();
        session.StyleChanged += (_, s) => states.Add(s);

        session.Update("Radio Ga Ga", "Queen", "Deezer");

        states.Count.ShouldBe(1);
        states[0].StyleName.ShouldBe("Rock");
        states[0].Effective.Method.ShouldBe(IdentificationMethod.ExactArtist);
        states[0].Effective.Confidence.ShouldBeGreaterThan(0.7);
    }

    [Fact]
    [Trait("Exigence", "MUS-026")]
    public void ForcedStyle_BeatsTheDetection_AndEndsWithTheTrackByDefault()
    {
        var session = new StyleSession(MusicStore.Default(), Normalizer);
        session.Update("Radio Ga Ga", "Queen", "Deezer");

        session.Force("Latino").ShouldBeTrue();

        session.State.StyleName.ShouldBe("Latino");
        session.State.Forced.ShouldBeTrue();
        session.State.Effective.Confidence.ShouldBe(1);
        session.State.Detected.FamilyId.ShouldBe("rock");

        session.Update("Dancing Queen", "ABBA", "Deezer");

        session.State.Forced.ShouldBeFalse();
        session.State.StyleName.ShouldBe("Disco / Funk / Soul");
    }

    [Fact]
    [Trait("Exigence", "MUS-026")]
    public void ForcedStyle_CanLastAcrossTracks_UntilCancelled()
    {
        var session = new StyleSession(MusicStore.Default(), Normalizer) { ForceUntilTrackEnd = false };
        session.Force("Slow");
        session.Update("Radio Ga Ga", "Queen", "Deezer");
        session.Update("Dancing Queen", "ABBA", "Deezer");

        session.State.StyleName.ShouldBe("Slow / Ballade");

        session.Force(null);

        session.State.StyleName.ShouldBe("Disco / Funk / Soul");
        session.Force("n'importe quoi").ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MUS-024")]
    public void Correction_ForTheArtist_IsImmediate_AndAppliesToOtherTitlesOfTheArtist()
    {
        var session = new StyleSession(MusicStore.Default(), Normalizer);
        var changes = 0;
        session.Base.Changed += (_, _) => changes++;
        session.Update("Radio Ga Ga", "Queen", "Deezer");

        session.Correct(CorrectionScope.Artist, "Festif", DateTimeOffset.UnixEpoch).ShouldBeNull();

        session.State.StyleName.ShouldBe("Festif / Tubes de soirée");
        session.State.Effective.Method.ShouldBe(IdentificationMethod.Correction);
        session.State.Effective.Confidence.ShouldBe(1);
        changes.ShouldBe(1);
        session.Base.ToCorrectionSet().Corrections.Single().OldStyle.ShouldBe("rock");

        session.Update("Bohemian Rhapsody", "Queen", "Deezer");

        session.State.StyleName.ShouldBe("Festif / Tubes de soirée");
    }

    [Fact]
    [Trait("Exigence", "MUS-024")]
    public void Correction_ForTheTitle_LeavesTheOtherTitlesAlone()
    {
        var session = new StyleSession(MusicStore.Default(), Normalizer);
        session.Update("Love of My Life", "Queen", "Deezer");

        session.Correct(CorrectionScope.Title, "Slow", DateTimeOffset.UnixEpoch).ShouldBeNull();
        session.State.StyleName.ShouldBe("Slow / Ballade");

        session.Update("Radio Ga Ga", "Queen", "Deezer");
        session.State.StyleName.ShouldBe("Rock");

        session.Update("Love of My Life", "Queen", "Deezer");
        session.State.StyleName.ShouldBe("Slow / Ballade");
        session.State.Effective.Method.ShouldBe(IdentificationMethod.Correction);
    }

    [Fact]
    [Trait("Exigence", "MUS-024")]
    public void Correction_NeedsATrackAnArtistAndAKnownFamily()
    {
        var session = new StyleSession(MusicStore.Default(), Normalizer);

        session.Correct(CorrectionScope.Artist, "Rock", DateTimeOffset.UnixEpoch).ShouldNotBeNull();

        session.Update("Summer", string.Empty, "Lecteur multimédia");
        session.Correct(CorrectionScope.Artist, "Rock", DateTimeOffset.UnixEpoch).ShouldNotBeNull();

        session.Update("Radio Ga Ga", "Queen", "Deezer");
        session.Correct(CorrectionScope.Artist, "n'importe quoi", DateTimeOffset.UnixEpoch).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void Serial_ChangesWithTheTrack_NotWithAForcedOrCorrectedStyle()
    {
        var session = new StyleSession(MusicStore.Default(), Normalizer);

        var first = session.Update("Radio Ga Ga", "Queen", "Deezer").Serial;
        session.Force("Latino");
        session.State.Serial.ShouldBe(first, "un style imposé n'est pas un nouveau morceau");
        session.Correct(CorrectionScope.Artist, "Festif", DateTimeOffset.UnixEpoch);
        session.State.Serial.ShouldBe(first, "une correction non plus");

        var second = session.Update("Radio Ga Ga", "Queen", "Deezer").Serial;
        second.ShouldBeGreaterThan(first, "le même titre rejoué est un nouveau morceau");
        session.Clear().Serial.ShouldBeGreaterThan(second);
    }

    [Fact]
    [Trait("Exigence", "MUS-006")]
    public void Clear_GivesNoStyle_AndLiftsAForcedStyle()
    {
        var session = new StyleSession(MusicStore.Default(), Normalizer);
        session.Update("Radio Ga Ga", "Queen", "Deezer");
        session.Force("Latino");

        session.Clear();
        session.Update("Dancing Queen", "ABBA", "Deezer");

        session.State.Forced.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void ReplaceBase_RecomputesTheCurrentTrack()
    {
        var session = new StyleSession(MusicStore.Default(), Normalizer);
        session.Update("Radio Ga Ga", "Queen", "Deezer");
        var empty = new MusicBase(DefaultTaxonomy.Value, new ArtistSet(), new TitleSet(), new CorrectionSet());

        session.ReplaceBase(empty);

        session.State.StyleName.ShouldBe("Inconnu");
    }
}
