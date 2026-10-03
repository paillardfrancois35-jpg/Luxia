using Luxia.Music.Base;
using Luxia.Music.Identification;
using Luxia.Music.Normalization;

namespace Luxia.Music.Tests;

/// <summary>Chaîne d'identification du style (doc 21 §3.3, MUS-021) sur une petite base écrite à la main.</summary>
public sealed class StyleIdentifierTests
{
    private static readonly TrackNormalizer Normalizer = new();

    private readonly StyleIdentifier _identifier;
    private readonly MusicBase _base;

    public StyleIdentifierTests()
    {
        _base = new MusicBase(
            DefaultTaxonomy.Value,
            new ArtistSet
            {
                Artists =
                [
                    Artist("Queen", ("rock", 0.8), ("pop", 0.2)),
                    Artist("Daniel Balavoine", ("variete", 1.0)),
                    Artist("Calvin Harris", ("electro", 1.0)),
                    Artist("Rihanna", ("pop", 0.6), ("hiphop", 0.4)),
                    Artist("Maître Gims", ("hiphop", 1.0), "Gims"),
                    Artist("Earth, Wind & Fire", ("disco", 1.0)),
                    Artist("Sia", ("pop", 1.0)),
                    Artist("Moby", ("electro", 0.5), ("slow", 0.5)),
                ],
            },
            new TitleSet
            {
                Titles =
                [
                    new TitleEntry { Artist = "Queen", Title = "Love of My Life", Style = "slow" },
                    new TitleEntry { Artist = "Queen", Title = "Another One Bites the Dust", Style = "disco", Aliases = ["Another One Bites The Dust (Single)"] },
                    new TitleEntry { Artist = "Moby", Title = "Porcelain", Version = "extended mix", Style = "electro" },
                ],
            },
            new CorrectionSet());
        _identifier = new StyleIdentifier(_base);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void ExactTitle_BeatsTheArtistStyle_With095()
    {
        var result = Identify("Love of My Life", "Queen", "Deezer");

        result.FamilyId.ShouldBe("slow");
        result.Method.ShouldBe(IdentificationMethod.ExactTitle);
        result.Confidence.ShouldBe(0.95, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void TitleAlias_IsAnExactTitle()
    {
        var result = Identify("Another One Bites The Dust (Single)", "Queen", "Deezer");

        result.FamilyId.ShouldBe("disco");
        result.Method.ShouldBe(IdentificationMethod.ExactTitle);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void FuzzyTitle_OfAKnownArtist_IsBetween07And09()
    {
        var result = Identify("Anothr One Bites the Dust", "Queen", "Deezer");

        result.FamilyId.ShouldBe("disco");
        result.Method.ShouldBe(IdentificationMethod.FuzzyTitle);
        result.Confidence.ShouldBeInRange(0.7, 0.9);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void KnownArtistUnknownTitle_UsesTheDominantStyle_With08()
    {
        var result = Identify("Radio Ga Ga", "Queen", "Deezer");

        result.FamilyId.ShouldBe("rock");
        result.Method.ShouldBe(IdentificationMethod.ExactArtist);
        result.Confidence.ShouldBe(0.8, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void ArtistAlias_IsAnExactArtist()
    {
        var result = Identify("La même", "Gims", "Deezer");

        result.FamilyId.ShouldBe("hiphop");
        result.Method.ShouldBe(IdentificationMethod.ExactArtist);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void ArtistWithSharedStyles_IsLessSure()
    {
        var result = Identify("Whatever", "Moby", "Deezer");

        result.Method.ShouldBe(IdentificationMethod.ExactArtist);
        result.Confidence.ShouldBe(0.7, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void FuzzyArtist_IsBetween05And075()
    {
        var result = Identify("Tous les cris", "Daniel Balavoin", "Deezer");

        result.FamilyId.ShouldBe("variete");
        result.Method.ShouldBe(IdentificationMethod.FuzzyArtist);
        result.Confidence.ShouldBeInRange(0.5, 0.75);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void UnknownArtistWithKnownGuest_UsesTheGuest_With05()
    {
        var result = Identify("Un tube (feat. Sia)", "Quelqu'un d'inconnu", "Deezer");

        result.FamilyId.ShouldBe("pop");
        result.Method.ShouldBe(IdentificationMethod.Guest);
        result.Confidence.ShouldBe(0.5, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void SecondCredit_IsTriedWhenTheFullNameIsUnknown()
    {
        var result = Identify("We Found Love", "Quelqu'un d'inconnu & Calvin Harris", "Deezer");

        result.FamilyId.ShouldBe("electro");
        result.Method.ShouldBe(IdentificationMethod.ExactArtist);
        result.Confidence.ShouldBeLessThan(0.8);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void GenreFromThePlayer_IsTheLastResort_With04()
    {
        var track = Normalizer.Normalize("Titre inconnu", "Artiste inconnu", "Deezer");

        var result = _identifier.Identify(track, "Eurodance");

        result.FamilyId.ShouldBe("electro");
        result.Method.ShouldBe(IdentificationMethod.PlayerGenre);
        result.Confidence.ShouldBe(0.4, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void KnownArtist_BeatsTheGenreOfThePlayer()
    {
        var track = Normalizer.Normalize("Radio Ga Ga", "Queen", "Deezer");

        _identifier.Identify(track, "Latin").FamilyId.ShouldBe("rock");
    }

    [Fact]
    [Trait("Exigence", "MUS-006")]
    public void NothingFound_IsUnknown_WithZeroConfidence()
    {
        var result = Identify("Titre inconnu", "Artiste inconnu", "Deezer");

        result.IsKnown.ShouldBeFalse();
        result.FamilyId.ShouldBe(Taxonomy.UnknownId);
        result.Confidence.ShouldBe(0);
        result.Method.ShouldBe(IdentificationMethod.None);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void Version_HasItsOwnStyle_ElseFallsBackToTheOriginalWithLessConfidence()
    {
        Identify("Porcelain (Extended Mix)", "Moby", "Deezer").Confidence.ShouldBe(0.95, 1e-9);

        // Version inconnue d'un titre connu seulement en version « extended mix » : on retombe sur l'artiste, sans titre exact.
        var other = Identify("Porcelain (Club Mix)", "Moby", "Deezer");
        other.Method.ShouldNotBe(IdentificationMethod.ExactTitle);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void YouTubeForms_ReachTheSameStyleAsDeezer()
    {
        var fromDeezer = Identify("Love of My Life", "Queen", "Deezer");
        var fromYouTube = Identify("Queen - Love of My Life (Official Video)", "QueenVEVO", "Chrome");
        var fromTopic = Identify("Love of My Life", "Queen - Topic", "Chrome");

        fromYouTube.FamilyId.ShouldBe(fromDeezer.FamilyId);
        fromTopic.FamilyId.ShouldBe(fromDeezer.FamilyId);
        fromYouTube.Method.ShouldBe(IdentificationMethod.ExactTitle);
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void AmbiguousPipe_IsDecidedByTheBase()
    {
        // « Titre | Artiste » : la base connaît Queen, donc la lecture « artiste = Queen » est retenue.
        var result = Identify("Radio Ga Ga | Queen", "Une chaîne", "Chrome");

        result.FamilyId.ShouldBe("rock");
        result.Method.ShouldBe(IdentificationMethod.ExactArtist);
        result.Hypothesis.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MUS-024")]
    public void Correction_IsFirst_WithConfidenceOne()
    {
        _base.Correct("artist", "Queen", null, null, "festif", "rock", DateTimeOffset.UnixEpoch);

        var result = Identify("Radio Ga Ga", "Queen", "Deezer");

        result.FamilyId.ShouldBe("festif");
        result.Method.ShouldBe(IdentificationMethod.Correction);
        result.Confidence.ShouldBe(1.0, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MUS-024")]
    public void TitleCorrection_OnlyConcernsThatTitle()
    {
        _base.Correct("title", "Queen", "Radio Ga Ga", null, "80s", "rock", DateTimeOffset.UnixEpoch);

        Identify("Radio Ga Ga", "Queen", "Deezer").FamilyId.ShouldBe("80s");
        Identify("Radio Ga Ga", "Queen", "Deezer").Method.ShouldBe(IdentificationMethod.Correction);
        Identify("Bohemian Rhapsody", "Queen", "Deezer").FamilyId.ShouldBe("rock");
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void ThresholdIsAdjustable_StricterRefusesTheFuzzyArtist()
    {
        var strict = new StyleIdentifier(_base, new IdentifierOptions { FuzzyThreshold = 0.97 });

        strict.Identify(Normalizer.Normalize("Tous les cris", "Daniel Balavoin", "Deezer")).IsKnown.ShouldBeFalse();
    }

    private StyleResult Identify(string title, string artist, string app) =>
        _identifier.Identify(Normalizer.Normalize(title, artist, app));

    private static ArtistEntry Artist(string name, (string, double) first, params object[] rest)
    {
        var styles = new Dictionary<string, double> { [first.Item1] = first.Item2 };
        var aliases = new List<string>();
        foreach (var item in rest)
        {
            if (item is ValueTuple<string, double> style)
            {
                styles[style.Item1] = style.Item2;
            }
            else if (item is string alias)
            {
                aliases.Add(alias);
            }
        }

        return new ArtistEntry { Name = name, Aliases = aliases, Styles = styles };
    }
}
