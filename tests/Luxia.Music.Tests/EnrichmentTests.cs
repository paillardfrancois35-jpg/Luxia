using System.Net;
using Luxia.Music.Base;
using Luxia.Music.Classification;
using Luxia.Music.Enrichment;

namespace Luxia.Music.Tests;

/// <summary>Outil d'enrichissement en ligne (MUS-040 à MUS-042), sans aucun accès réseau : réponses simulées, temps simulé.</summary>
public sealed class EnrichmentTests
{
    private const string MusicBrainzJson = """
        {"created":"2026-10-03T20:00:00Z","count":2,"offset":0,"artists":[
          {"id":"a1","score":100,"name":"Kokwak","type":"Person",
           "tags":[{"count":3,"name":"hardstyle"},{"count":1,"name":"electronic"},{"count":1,"name":"seen live"}],
           "genres":[{"count":2,"name":"hardstyle","id":"g1"}]},
          {"id":"a2","score":70,"name":"Kokwak Tribute","tags":[{"count":9,"name":"rock"}]}]}
        """;

    [Fact]
    [Trait("Exigence", "MUS-040")]
    public void TagMapper_SumsTheWeightsOfTheTagsOfAFamily()
    {
        var musicBase = MusicStore.Default();

        var result = TagMapper.Map([new("hardstyle", 6), new("electronic", 3), new("seen live", 9), new("rock", 1)], musicBase)!.Value;

        result.Family.Id.ShouldBe("electro");
        result.Confidence.ShouldBeGreaterThan(0.5);
        result.Confidence.ShouldBeLessThanOrEqualTo(0.9);
    }

    [Fact]
    [Trait("Exigence", "MUS-040")]
    public void TagMapper_FewVotes_LowersTheConfidence_AndUnknownTagsGiveNothing()
    {
        var musicBase = MusicStore.Default();

        var weak = TagMapper.Map([new("house", 1)], musicBase)!.Value;
        var strong = TagMapper.Map([new("house", 40), new("deep house", 30)], musicBase)!.Value;

        weak.Confidence.ShouldBeLessThan(strong.Confidence);
        strong.Confidence.ShouldBe(0.9, 1e-9);
        TagMapper.Map([new("seen live", 5), new("favorites", 3)], musicBase).ShouldBeNull();
        TagMapper.Map([], musicBase).ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "MUS-040")]
    public void MusicBrainz_ParsesTheTagsAndGenres_OfTheMatchingArtistOnly()
    {
        var tags = MusicBrainzSource.Parse(MusicBrainzJson, "Kokwak");

        tags.Select(t => t.Name).ShouldBe(["hardstyle", "electronic", "seen live"]);
        tags[0].Count.ShouldBe(5, "les étiquettes et les genres s'additionnent");
        MusicBrainzSource.Parse(MusicBrainzJson, "Quelqu'un d'autre").ShouldBeEmpty("aucun artiste ne ressemble assez");
        MusicBrainzSource.Parse("{\"artists\":[]}", "Kokwak").ShouldBeEmpty();
        MusicBrainzSource.Parse("{}", "Kokwak").ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MUS-040")]
    public void LastFm_ParsesTheTopTags()
    {
        const string json = "{\"toptags\":{\"tag\":[{\"count\":100,\"name\":\"rock\"},{\"count\":60,\"name\":\"classic rock\"},{\"count\":20,\"name\":\"british\"}]}}";

        var tags = LastFmSource.Parse(json);

        tags.Select(t => t.Name).ShouldBe(["rock", "classic rock", "british"]);
        LastFmSource.Parse("{\"error\":6,\"message\":\"The artist you supplied could not be found\"}").ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MUS-042")]
    public async Task MusicBrainz_IntroducesItself_AndWaitsBetweenRequests()
    {
        var handler = new FakeHandler(_ => Response(HttpStatusCode.OK, MusicBrainzJson));
        var time = new FakeTime();
        var waits = new List<TimeSpan>();
        using var http = new HttpClient(handler);
        var source = new MusicBrainzSource(http, time, (span, _) =>
        {
            waits.Add(span);
            time.Advance(span);
            return Task.CompletedTask;
        });

        var first = await source.GetTagsAsync("Kokwak", CancellationToken.None);
        var second = await source.GetTagsAsync("Kokwak", CancellationToken.None);

        first.ShouldNotBeNull().Count.ShouldBe(3);
        second.ShouldNotBeNull();
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].RequestUri!.Query.ShouldContain("artist%3A%22Kokwak%22");
        handler.Requests[0].Headers.UserAgent.ToString().ShouldContain("LuXia");
        waits.ShouldHaveSingleItem().ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(1), "une requête par seconde au plus");
    }

    [Fact]
    [Trait("Exigence", "MUS-042")]
    public async Task Sources_ReturnNull_OnNetworkOrServiceErrors()
    {
        using var failing = new HttpClient(new FakeHandler(_ => throw new HttpRequestException("pas de réseau")));
        using var busy = new HttpClient(new FakeHandler(_ => Response(HttpStatusCode.ServiceUnavailable, "{}")));

        (await new MusicBrainzSource(failing, new FakeTime(), (_, _) => Task.CompletedTask).GetTagsAsync("Kokwak", CancellationToken.None)).ShouldBeNull();
        (await new MusicBrainzSource(busy, new FakeTime(), (_, _) => Task.CompletedTask).GetTagsAsync("Kokwak", CancellationToken.None)).ShouldBeNull();
        (await new LastFmSource(busy, "clé").GetTagsAsync("Kokwak", CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "MUS-042")]
    public async Task LastFm_UsesThePersonalKey()
    {
        var handler = new FakeHandler(_ => Response(HttpStatusCode.OK, "{\"toptags\":{\"tag\":[{\"count\":100,\"name\":\"rock\"}]}}"));
        using var http = new HttpClient(handler);

        var tags = await new LastFmSource(http, "MA-CLE").GetTagsAsync("Queen", CancellationToken.None);

        tags.ShouldNotBeNull().ShouldHaveSingleItem();
        handler.Requests[0].RequestUri!.Query.ShouldContain("api_key=MA-CLE");
        handler.Requests[0].RequestUri!.Query.ShouldContain("artist=Queen");
    }

    [Fact]
    [Trait("Exigence", "MUS-042")]
    public async Task Cache_AvoidsAskingTwice_ExpiresAfter90Days_AndOfflineUsesOnlyTheCache()
    {
        var folder = Path.Combine(Path.GetTempPath(), "luxia-enrich-" + Guid.NewGuid().ToString("N"));
        try
        {
            var inner = new CountingSource([new("rock", 10)]);
            var time = new FakeTime();
            var cached = new CachedTagSource(inner, folder, false, time);

            (await cached.GetTagsAsync("Queen", CancellationToken.None)).ShouldNotBeNull();
            (await cached.GetTagsAsync("Queen", CancellationToken.None)).ShouldNotBeNull();
            inner.Calls.ShouldBe(1);
            (cached.Hits, cached.Requests).ShouldBe((1, 1));

            time.Advance(TimeSpan.FromDays(91));
            (await cached.GetTagsAsync("Queen", CancellationToken.None)).ShouldNotBeNull();
            inner.Calls.ShouldBe(2, "le cache de plus de 90 jours est redemandé");

            var offline = new CachedTagSource(inner, folder, true, time);
            (await offline.GetTagsAsync("Queen", CancellationToken.None)).ShouldNotBeNull();
            (await offline.GetTagsAsync("Inconnu", CancellationToken.None)).ShouldBeNull();
            inner.Calls.ShouldBe(2, "hors ligne : aucune requête");

            var failing = new CachedTagSource(new CountingSource(null), folder, false, time);
            (await failing.GetTagsAsync("Erreur", CancellationToken.None)).ShouldBeNull();
            Directory.EnumerateFiles(folder).Count().ShouldBe(1, "une erreur n'est pas mise en cache");
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [Fact]
    [Trait("Exigence", "MUS-040")]
    public async Task Enricher_ProposesAFamilyPerArtist_TriesTheNextSource_AndHonoursTheLimit()
    {
        var musicBase = MusicStore.Default();
        var empty = new FakeSource("Source A", new Dictionary<string, IReadOnlyList<TagCount>?> { ["Artiste Deux"] = [] });
        var second = new FakeSource("Source B", new Dictionary<string, IReadOnlyList<TagCount>?>
        {
            ["Artiste Un"] = [new("house", 20), new("deep house", 10)],
            ["Artiste Deux"] = [new("reggae", 8)],
            ["Artiste Trois"] = [new("seen live", 4)],
            ["Artiste Quatre"] = [new("rock", 9)],
        });
        var enricher = new Enricher([empty, second], musicBase);
        var lines = new List<string>();

        var proposals = await enricher.ProposeAsync(["Artiste Un", "Artiste Deux", "Artiste Trois", "artiste un", "Artiste Quatre"], 3, new Progress<string>(lines.Add), CancellationToken.None);

        proposals.Select(p => (p.Artist, p.Style, p.Source)).ShouldBe([("Artiste Un", "house", "Source B"), ("Artiste Deux", "reggae", "Source B")]);
        proposals[0].Tags.ShouldBe(["house", "deep house"]);
        proposals[0].Confidence.ShouldBeInRange(0.5, 0.9);
        empty.Asked.ShouldBe(["Artiste Un", "Artiste Deux", "Artiste Trois"], "le doublon est écarté et « Artiste Quatre » dépasse la limite de 3 artistes");
        lines.ShouldContain(l => l.Contains("Artiste Trois", StringComparison.Ordinal) && l.Contains("aucune proposition", StringComparison.Ordinal));
        musicBase.FindArtist("artiste un").ShouldBeNull("rien n'entre dans la base sans validation (MUS-041)");
    }

    [Fact]
    [Trait("Exigence", "MUS-040")]
    public async Task Enricher_TellsAFailedRequestApartFromNoRecognisedTag()
    {
        // Essai P9 (ex. 21) : une requête sans réponse s'affichait comme « aucune proposition ».
        var source = new FakeSource("Source A", new Dictionary<string, IReadOnlyList<TagCount>?>
        {
            ["Artiste Muet"] = null,
            ["Artiste Sans Etiquette"] = [new("seen live", 4)],
            ["Artiste Rock"] = [new("rock", 9)],
        });
        var enricher = new Enricher([source], MusicStore.Default());
        var lines = new List<string>();

        var proposals = await enricher.ProposeAsync(["Artiste Muet", "Artiste Sans Etiquette", "Artiste Rock"], 10, new Progress<string>(lines.Add), CancellationToken.None);

        proposals.ShouldHaveSingleItem().Artist.ShouldBe("Artiste Rock");
        enricher.Unanswered.ShouldBe(["Artiste Muet"]);
        lines.ShouldContain(l => l.Contains("Artiste Muet", StringComparison.Ordinal) && l.Contains("sans réponse", StringComparison.Ordinal));
        lines.ShouldContain(l => l.Contains("Artiste Sans Etiquette", StringComparison.Ordinal) && l.Contains("aucune proposition", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Exigence", "MUS-041")]
    public void Proposals_AreKeptInTheProjectFolder_UntilValidated()
    {
        var folder = Path.Combine(Path.GetTempPath(), "luxia-music-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            MusicStore.LoadProposals(folder).Items.ShouldBeEmpty();

            MusicStore.SaveProposals(folder, new ProposalSet { Items = [new Proposal { Artist = "Kokwak", Style = "electro", Confidence = 0.8, Source = "MusicBrainz", Tags = ["hardstyle"] }] });

            var loaded = MusicStore.LoadProposals(folder).Items.ShouldHaveSingleItem();
            loaded.Artist.ShouldBe("Kokwak");
            loaded.Tags.ShouldBe(["hardstyle"]);
            File.ReadAllText(Path.Combine(folder, "propositions.json")).ShouldContain("\"formatVersion\": 1");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static HttpResponseMessage Response(HttpStatusCode code, string json) =>
        new(code) { Content = new StringContent(json) };

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(answer(request));
        }
    }

    private sealed class FakeTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 3, 20, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan span) => _now += span;

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class CountingSource(IReadOnlyList<TagCount>? tags) : ITagSource
    {
        public int Calls { get; private set; }

        public string Name => "Compteur";

        public Task<IReadOnlyList<TagCount>?> GetTagsAsync(string artist, CancellationToken cancellation)
        {
            Calls++;
            return Task.FromResult(tags);
        }
    }

    private sealed class FakeSource(string name, Dictionary<string, IReadOnlyList<TagCount>?> answers) : ITagSource
    {
        public List<string> Asked { get; } = [];

        public string Name => name;

        public Task<IReadOnlyList<TagCount>?> GetTagsAsync(string artist, CancellationToken cancellation)
        {
            Asked.Add(artist);
            return Task.FromResult(answers.GetValueOrDefault(artist));
        }
    }
}
