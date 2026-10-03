using Luxia.Music.Normalization;

namespace Luxia.Music.Tests;

/// <summary>Normalisation des titres bruts (doc 21 §3.1) : MUS-005 (YouTube), MUS-020 (règles), T-MUS-01 (jeu de titres).</summary>
public sealed class TrackNormalizerTests
{
    private static readonly TrackNormalizer Normalizer = new();

    /// <summary>Lignes du jeu de titres : application, artiste brut, titre brut, artiste, titre, invités, versions.</summary>
    public static TheoryData<string, string, string, string, string, string, string> Cases
    {
        get
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "normalisation-200.tsv");
            var data = new TheoryData<string, string, string, string, string, string, string>();
            foreach (var line in File.ReadAllLines(path).Where(l => l.Length > 0 && l[0] != '#'))
            {
                // Les colonnes finales vides (invités, versions) peuvent manquer : un éditeur retire les tabulations de fin de ligne.
                var f = line.Split('\t');
                f.Length.ShouldBeInRange(5, 7, $"nombre de colonnes de la ligne « {line} »");
                Array.Resize(ref f, 7);
                data.Add(f[0], f[1], f[2], f[3], f[4], f[5] ?? string.Empty, f[6] ?? string.Empty);
            }

            return data;
        }
    }

    [Fact]
    [Trait("Exigence", "MUS-020")]
    public void TestSet_HasAtLeast200Titles() => Cases.Count.ShouldBeGreaterThanOrEqualTo(200);

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Exigence", "MUS-020")]
    [Trait("Exigence", "MUS-005")]
    public void RawTitle_IsNormalizedAsExpected(string app, string artist, string title, string expectedArtist, string expectedTitle, string guests, string versions)
    {
        var result = Normalizer.Normalize(title, artist, app).Primary;

        result.Artist.ShouldBe(expectedArtist, $"artiste de « {artist} » / « {title} »");
        result.Title.ShouldBe(expectedTitle, $"titre de « {artist} » / « {title} »");
        result.Guests.ShouldBe(guests.Length == 0 ? [] : guests.Split(';'), $"invités de « {title} »");
        result.Versions.ShouldBe(versions.Length == 0 ? [] : versions.Split(';'), $"versions de « {title} »");
    }

    [Theory]
    [InlineData("Queen", "queen")]
    [InlineData("Don't Stop Me Now", "dont stop me now")]
    [InlineData("S.O.S.", "sos")]
    [InlineData("Earth, Wind & Fire", "earth wind et fire")]
    [InlineData("  Mötley   Crüe ", "motley crue")]
    [InlineData("Œuvre d'Æther", "oeuvre daether")]
    [InlineData("Rock'n'Roll", "rocknroll")]
    [InlineData("AC/DC", "ac dc")]
    [InlineData("", "")]
    [InlineData("---", "")]
    [Trait("Exigence", "MUS-020")]
    public void Key_IsLowerCaseWithoutAccentsNorPunctuation(string text, string expected) =>
        TextKey.Of(text).ShouldBe(expected);

    [Fact]
    [Trait("Exigence", "MUS-020")]
    public void Key_OfNull_IsEmpty() => TextKey.Of(null).ShouldBe(string.Empty);

    [Fact]
    [Trait("Exigence", "MUS-005")]
    public void PipeWithChannelInTheRemixTag_PutsTheTitleOnTheLeft_ThenKeepsTheOtherReading()
    {
        // Essai PoC-3 (YouTube Music) : « Titre (Remix de la chaîne) | Artiste », la chaîne n'est pas l'artiste.
        var result = Normalizer.Normalize("Tous les cris les S.O.S. (Kokwak Hardstyle Remix) | Daniel Balavoine", "Kokwak", "Chrome");

        result.Primary.Artist.ShouldBe("daniel balavoine");
        result.Primary.Origin.ShouldBe(HypothesisOrigin.TitleSplit);
        result.Hypotheses.ShouldContain(h => h.Artist == "kokwak" && h.Origin == HypothesisOrigin.ChannelAsArtist);
    }

    [Fact]
    [Trait("Exigence", "MUS-005")]
    public void AmbiguousPipe_KeepsBothReadings_ForTheIdentificationToDecide()
    {
        var result = Normalizer.Normalize("Don't Stop Me Now | Queen", "XYZ", "Chrome");

        result.Hypotheses.ShouldContain(h => h.Artist == "dont stop me now" && h.Title == "queen");
        result.Hypotheses.ShouldContain(h => h.Artist == "queen" && h.Title == "dont stop me now");
        result.Hypotheses.ShouldContain(h => h.Artist == "xyz" && h.Origin == HypothesisOrigin.ChannelAsArtist);
    }

    [Fact]
    [Trait("Exigence", "MUS-005")]
    public void Credits_AreSplitOneByOne_AndTheFullNameIsKept()
    {
        var hypothesis = Normalizer.Normalize("September", "Earth, Wind & Fire", "Deezer").Primary;

        hypothesis.Artist.ShouldBe("earth wind et fire");
        hypothesis.Credits.ShouldBe(["earth", "wind", "fire"]);
    }

    [Fact]
    [Trait("Exigence", "MUS-020")]
    public void Version_IsKeptApart_AndFlaggedAsVersion()
    {
        var hypothesis = Normalizer.Normalize("Cold Heart (PNAU Remix)", "Elton John & Dua Lipa", "Deezer").Primary;

        hypothesis.Title.ShouldBe("cold heart");
        hypothesis.Versions.ShouldBe(["pnau remix"]);
        hypothesis.IsVersion.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "MUS-020")]
    public void EmptyInput_GivesOneEmptyHypothesis()
    {
        var result = Normalizer.Normalize(null, null);

        result.Hypotheses.Count.ShouldBe(1);
        result.Primary.Title.ShouldBeEmpty();
        result.Primary.Artist.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MUS-020")]
    public void Rules_AreEditable_NoiseWordAddedByTheUserIsRemoved()
    {
        var custom = NormalizationRules.Default with { NoiseWords = [.. NormalizationRules.Default.NoiseWords, "bootlegtv"] };

        new TrackNormalizer(custom).Normalize("Titre (BootlegTV)", "Artiste", "Deezer").Primary.Title.ShouldBe("titre");
        Normalizer.Normalize("Titre (BootlegTV)", "Artiste", "Deezer").Primary.Title.ShouldBe("titre bootlegtv");
    }

    [Fact]
    [Trait("Exigence", "MUS-020")]
    public void Rules_RoundTripThroughTheProjectFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "luxia-music-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            NormalizationStore.Load(folder).Value.ShouldBe(NormalizationRules.Default);

            var custom = NormalizationRules.Default with { NoiseWords = ["foo", "bar"], UntrustedApps = ["monnavigateur"] };
            NormalizationStore.Save(folder, custom);
            var loaded = NormalizationStore.Load(folder);

            loaded.Message.ShouldBeNull();
            loaded.Value.NoiseWords.ShouldBe(["foo", "bar"]);
            loaded.Value.UntrustedApps.ShouldBe(["monnavigateur"]);
            File.ReadAllText(Path.Combine(folder, NormalizationStore.FileName)).ShouldContain("\"formatVersion\": 1");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
