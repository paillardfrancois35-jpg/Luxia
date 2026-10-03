using Luxia.Music.Identification;
using Luxia.Music.Normalization;

namespace Luxia.Music.Tests;

/// <summary>Rapprochement flou (T-MUS-02) : fautes, mots inversés, mots manquants, et les faux positifs à éviter.</summary>
public sealed class FuzzyMatchTests
{
    private const double Threshold = 0.8;

    [Theory]
    [InlineData("daniel balavoine", "daniel balavoine")]
    [InlineData("daniel balavoine", "daniel balavoin")]
    [InlineData("michel sardou", "micheal sardou")]
    [InlineData("jean jacques goldman", "jean jaques goldman")]
    [InlineData("gilbert montagne", "gilbert montagnee")]
    [InlineData("the rhythm of the night", "the rythm of the night")]
    [InlineData("bohemian rhapsody", "bohemian rhapsodie")]
    [Trait("Exigence", "MUS-021")]
    public void TypoInALongWord_IsTolerated(string a, string b) =>
        FuzzyMatch.Score(TextKey.Of(a), TextKey.Of(b)).ShouldBeGreaterThanOrEqualTo(Threshold);

    [Theory]
    [InlineData("daniel balavoine", "balavoine daniel")]
    [InlineData("calvin harris rihanna", "rihanna calvin harris")]
    [InlineData("love tonight", "tonight love")]
    [Trait("Exigence", "MUS-021")]
    public void InvertedWords_AreTolerated(string a, string b) =>
        FuzzyMatch.Score(TextKey.Of(a), TextKey.Of(b)).ShouldBeGreaterThanOrEqualTo(Threshold);

    [Theory]
    [InlineData("hotel california", "hotel california eagles")]
    [InlineData("dont stop me now", "dont stop me now queen")]
    [Trait("Exigence", "MUS-021")]
    public void OneMissingWordOnALongTitle_StaysBelowExact_ButClose(string a, string b)
    {
        var score = FuzzyMatch.Score(a, b);

        score.ShouldBeLessThan(1);
        score.ShouldBeGreaterThan(0.7);
    }

    [Theory]
    [InlineData("abba", "abbe")]
    [InlineData("pink", "pins")]
    [InlineData("u2", "u3")]
    [InlineData("24k magic", "25k magic")]
    [InlineData("seven nation army", "eleven nation army")]
    [InlineData("daft punk", "deep purple")]
    [InlineData("queen", "green")]
    [InlineData("shakira", "shakur")]
    [Trait("Exigence", "MUS-021")]
    public void ShortWordsNumbersAndDifferentNames_AreNotConfused(string a, string b) =>
        FuzzyMatch.Score(TextKey.Of(a), TextKey.Of(b)).ShouldBeLessThan(Threshold);

    [Fact]
    [Trait("Exigence", "MUS-021")]
    public void EmptyAndIdentical()
    {
        FuzzyMatch.Score(string.Empty, "queen").ShouldBe(0);
        FuzzyMatch.Score("queen", "queen").ShouldBe(1);
    }
}
