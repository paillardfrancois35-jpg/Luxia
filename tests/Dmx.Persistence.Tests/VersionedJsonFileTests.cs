using System.Text.Json.Nodes;
using Dmx.Persistence.Json;

namespace Dmx.Persistence.Tests;

public sealed class VersionedJsonFileTests : IDisposable
{
    private readonly TempFolder _temp = new();

    /// <summary>Document de test en version 2 : le champ « nom » de la v1 est devenu « name » + « tags ».</summary>
    public sealed record Sample(string Name, IReadOnlyList<string> Tags, SampleKind Kind = SampleKind.FirstKind);

    public enum SampleKind
    {
        FirstKind,
        SecondKind,
    }

    private static readonly DocumentType<Sample> V2 = new(
        "exemple",
        2,
        [new JsonMigration(1, doc =>
        {
            doc["name"] = doc["nom"]!.GetValue<string>();
            doc.Remove("nom");
            doc["tags"] = new JsonArray();
        })]);

    [Fact]
    [Trait("Exigence", "GEN-050")]
    public void Save_WritesIndentedUtf8WithVersionFirst_AndReadableAccents()
    {
        var path = _temp.File("a.json");

        VersionedJsonFile.Save(path, new Sample("Éclairage d'été", ["un"], SampleKind.SecondKind), V2);

        var text = File.ReadAllText(path);
        text.ShouldStartWith("{\n  \"formatVersion\": 2,\n  \"name\": \"Éclairage d'été\"");
        text.ShouldContain("\"kind\": \"secondKind\"");
        File.ReadAllBytes(path)[0].ShouldBe((byte)'{'); // pas de BOM
    }

    [Fact]
    [Trait("Exigence", "GEN-050")]
    public void SaveThenLoad_RoundTrips()
    {
        var path = _temp.File("a.json");
        var sample = new Sample("x", ["a", "b"]);

        VersionedJsonFile.Save(path, sample, V2);
        var result = VersionedJsonFile.Load(path, V2);

        result.Status.ShouldBe(LoadStatus.Loaded);
        result.Value!.Name.ShouldBe("x");
        result.Value.Tags.ShouldBe(["a", "b"]);
    }

    [Fact]
    [Trait("Exigence", "GEN-051")]
    public void Load_OldVersion_MigratesAndKeepsBackup()
    {
        var path = _temp.File("a.json");
        File.WriteAllText(path, """{ "formatVersion": 1, "nom": "ancien" }""");

        var result = VersionedJsonFile.Load(path, V2);

        result.Status.ShouldBe(LoadStatus.Migrated);
        result.Value!.Name.ShouldBe("ancien");
        File.Exists(path + ".v1.bak").ShouldBeTrue();
        File.ReadAllText(path + ".v1.bak").ShouldContain("\"nom\"");
        File.ReadAllText(path).ShouldContain("\"formatVersion\": 2");
    }

    [Fact]
    [Trait("Exigence", "GEN-056")]
    public void Load_CorruptFile_IsSetAsideWithoutThrowing()
    {
        var path = _temp.File("a.json");
        File.WriteAllText(path, "{ ceci n'est pas du JSON");

        var result = VersionedJsonFile.Load(path, V2);

        result.Status.ShouldBe(LoadStatus.Invalid);
        result.Message!.ShouldContain("mis de côté");
        File.Exists(path).ShouldBeFalse();
        File.Exists(result.SetAsidePath).ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "GEN-056")]
    public void Load_MissingVersion_IsInvalid()
    {
        var path = _temp.File("a.json");
        File.WriteAllText(path, """{ "name": "x" }""");

        VersionedJsonFile.Load(path, V2).Status.ShouldBe(LoadStatus.Invalid);
    }

    [Fact]
    public void Load_TooRecent_IsNotTouched()
    {
        var path = _temp.File("a.json");
        const string content = """{ "formatVersion": 9, "name": "futur" }""";
        File.WriteAllText(path, content);

        var result = VersionedJsonFile.Load(path, V2);

        result.Status.ShouldBe(LoadStatus.TooRecent);
        File.ReadAllText(path).ShouldBe(content);
    }

    [Fact]
    public void Load_Missing_ReportsMissing() =>
        VersionedJsonFile.Load(_temp.File("absent.json"), V2).Status.ShouldBe(LoadStatus.Missing);

    [Fact]
    public void Load_ToleratesCommentsAndTrailingCommas()
    {
        var path = _temp.File("a.json");
        File.WriteAllText(path, """
            {
              // édité à la main
              "formatVersion": 2,
              "name": "x",
              "tags": [],
            }
            """);

        VersionedJsonFile.Load(path, V2).Status.ShouldBe(LoadStatus.Loaded);
    }

    public void Dispose() => _temp.Dispose();
}
