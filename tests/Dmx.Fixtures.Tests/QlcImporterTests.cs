using Dmx.Fixtures.Import;
using Dmx.Fixtures.Model;
using Dmx.Fixtures.Rules;

namespace Dmx.Fixtures.Tests;

/// <summary>T-BIB-02 (QLC+) et import par lots (BIB-083).</summary>
public sealed class QlcImporterTests
{
    private static readonly string Assets = Path.Combine(AppContext.BaseDirectory, "assets");

    [Theory]
    [InlineData("Acme-Wash-7.qxf")]
    [InlineData("Generic-Par-Classic.qxf")]
    [InlineData("Acme-Bar-4.qxf")]
    [Trait("Exigence", "BIB-081")]
    public void ReferenceFiles_ImportToValidModels(string file)
    {
        var result = QlcImporter.ImportFile(Path.Combine(Assets, "qlc", file));

        result.Succeeded.ShouldBeTrue(result.Error);
        FixtureValidator.Validate(result.Fixture!).Where(i => i.Severity == IssueSeverity.Error).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "BIB-081")]
    [Trait("Exigence", "BIB-003")]
    public void Wash_PresetsAndFineChannels()
    {
        var result = QlcImporter.ImportFile(Path.Combine(Assets, "qlc", "Acme-Wash-7.qxf"));
        var wash = result.Fixture!;

        wash.Manufacturer.ShouldBe("Acme");
        wash.Category.ShouldBe(FixtureCategory.MovingHead);
        wash.Channel("pan")!.Resolution.ShouldBe(ChannelResolution.Bit16);
        wash.Modes[0].Channels[1].ShouldBe(new ModeChannel("pan", ChannelPart.Fine));
        wash.Modes[0].ChannelCount.ShouldBe(12);
        wash.Channel("white")!.Attribute.ShouldBe(AttributeKind.White);
        wash.Channel("shutter")!.Capabilities.Select(c => c.Strobe).ShouldBe([StrobeEffect.Closed, StrobeEffect.Strobe, StrobeEffect.Open]);
        wash.Channel("colour-macros")!.Capabilities[0].Colors.ShouldBe(["#ff0000"]);
        wash.Physical.PanRange.ShouldBe(540);
        result.Notes.ShouldContain(n => n.Contains("EffectSparkle", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Exigence", "BIB-081")]
    [Trait("Exigence", "BIB-007")]
    public void ClassicPar_GroupsAndStrobeInProgram()
    {
        var result = QlcImporter.ImportFile(Path.Combine(Assets, "qlc", "Generic-Par-Classic.qxf"));
        var par = result.Fixture!;

        par.Channel("master")!.Attribute.ShouldBe(AttributeKind.Intensity);
        par.Channel("red")!.Attribute.ShouldBe(AttributeKind.Red);
        FixtureRules.HasVirtualIntensity(par, par.Modes[0]).ShouldBeTrue();
        FixtureRules.Safety(par.Channel("program")!).ShouldBe(SafetyTags.Strobe);
        par.Channel("mystery")!.Attribute.ShouldBe(AttributeKind.Generic);
        result.Notes.ShouldContain(n => n.Contains("Teleport", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Exigence", "BIB-081")]
    public void Bar_HeadsBecomeCells()
    {
        var bar = QlcImporter.ImportFile(Path.Combine(Assets, "qlc", "Acme-Bar-4.qxf")).Fixture!;

        bar.Category.ShouldBe(FixtureCategory.LedBar);
        FixtureRules.CellCount(bar, bar.Modes[0]).ShouldBe(4);
        bar.Channel("red-3")!.Cell.ShouldBe(3);
    }

    [Fact]
    [Trait("Exigence", "BIB-083")]
    [Trait("Exigence", "BIB-082")]
    public void Batch_ImportsWholeFolder_AndReportsProgress()
    {
        var files = FixtureImporter.FindFiles(Assets);
        var progress = new List<(int, int)>();

        var results = FixtureImporter.ImportFiles(files, new SyncProgress(progress));

        results.Count.ShouldBe(8);
        results.ShouldAllBe(r => r.Succeeded);
        progress.Last().ShouldBe((8, 8));
        results.Select(r => r.Fixture!.Source).Distinct().Order().ShouldBe([FixtureSource.Ofl, FixtureSource.QlcPlus]);
    }

    [Fact]
    public void OwnFormat_IsRecognized()
    {
        var folder = Path.Combine(Path.GetTempPath(), "dmx-import-" + Guid.NewGuid().ToString("N"));
        var library = new FixtureLibrary(folder);
        var entry = library.Save(Samples.Par);

        var result = FixtureImporter.ImportFile(entry.FilePath!);

        result.Fixture!.Id.ShouldBe(entry.Fixture.Id);
        Directory.Delete(folder, recursive: true);
    }

    private sealed class SyncProgress(List<(int, int)> list) : IProgress<(int Done, int Total)>
    {
        public void Report((int Done, int Total) value) => list.Add(value);
    }
}
