using Dmx.Core.Settings;
using Dmx.Persistence.Json;

namespace Dmx.Persistence.Tests;

public sealed class PreferencesAndProjectTests : IDisposable
{
    private readonly TempFolder _temp = new();

    [Fact]
    [Trait("Exigence", "SORT-006")]
    public void Preferences_Missing_GivesDefaults()
    {
        var store = new PreferencesStore(_temp.File("preferences.json"));

        store.Load().Status.ShouldBe(LoadStatus.Missing);

        store.Current.Outputs.Assignments.Single().Driver.ShouldBe(OutputDriverKind.Arduino);
        store.Current.TestOutput.ExcludedChannels.ShouldBe("180");
        store.Current.TestOutput.ValueByte.ShouldBe((byte)128);
    }

    [Fact]
    [Trait("Exigence", "SORT-006")]
    [Trait("Exigence", "SORT-011")]
    public void Preferences_Update_IsPersisted()
    {
        var path = _temp.File("preferences.json");
        var store = new PreferencesStore(path);
        store.Update(p => p with { Outputs = p.Outputs with { Arduino = p.Outputs.Arduino with { LastPort = "COM7" } } });

        var reloaded = new PreferencesStore(path);
        reloaded.Load().Status.ShouldBe(LoadStatus.Loaded);

        reloaded.Current.Outputs.Arduino.LastPort.ShouldBe("COM7");
    }

    [Fact]
    [Trait("Exigence", "GEN-056")]
    public void Preferences_Corrupt_GivesDefaultsAndSetsFileAside()
    {
        var path = _temp.File("preferences.json");
        File.WriteAllText(path, "{{{");
        var store = new PreferencesStore(path);

        var result = store.Load();

        result.Status.ShouldBe(LoadStatus.Invalid);
        store.Current.TickRateHz.ShouldBe(40);
        store.Current.Outputs.Arduino.LastPort.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "GEN-052")]
    public void Project_CreateThenOpen()
    {
        var folder = Path.Combine(_temp.Path, "Mon show");

        var created = ProjectStore.Create(folder, "Mon show", "essai");
        var report = ProjectStore.Open(folder);

        report.Succeeded.ShouldBeTrue();
        report.Info!.Id.ShouldBe(created.Id);
        report.Info.Name.ShouldBe("Mon show");
        File.Exists(Path.Combine(folder, ProjectStore.ProjectFileName)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "GEN-056")]
    public void Project_CorruptFile_ReportsMessageWithoutThrowing()
    {
        File.WriteAllText(_temp.File(ProjectStore.ProjectFileName), "nope");

        var report = ProjectStore.Open(_temp.Path);

        report.Succeeded.ShouldBeFalse();
        report.Messages.Single().ShouldContain("illisible");
    }

    [Fact]
    public void DataPaths_FollowDocumentedLayout()
    {
        var paths = new DataPaths(@"C:\U\Documents\DMX", @"C:\U\AppData\DMX");

        paths.Library.ShouldBe(@"C:\U\Documents\DMX\Bibliothèque");
        paths.Logs.ShouldBe(@"C:\U\Documents\DMX\Journaux");
        paths.PreferencesFile.ShouldBe(@"C:\U\AppData\DMX\preferences.json");
    }

    public void Dispose() => _temp.Dispose();
}
