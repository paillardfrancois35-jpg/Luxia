using Luxia.Fixtures.Model;
using Luxia.Patch;
using Luxia.Patch.Model;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Tests;

/// <summary>Installation, lieux et modèles réels du show de référence (doc 41), pour tester la compilation sur le parc.</summary>
internal sealed class ReferenceProject
{
    public static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");

    public ReferenceProject()
    {
        Library = new ProjectFixtureLibrary(Folder);
        (Installation, _) = InstallationStore.Load(Folder);
        (Venues, _) = VenueStore.Load(Folder);
    }

    public ProjectFixtureLibrary Library { get; }

    public Installation Installation { get; set; }

    public VenueSet Venues { get; set; }

    public PatchContext Patch => new(Installation, Venues, Library.Find);

    public PatchedFixture Fixture(string name) => Installation.Fixtures.Single(f => f.Name == name);

    public FixtureType Type(string fixtureName) => Library.Find(Fixture(fixtureName).FixtureTypeId)!;

    public ProjectContent Content(SceneSet? scenes = null, PaletteSet? palettes = null, LayerSet? layers = null) =>
        new(Installation, Venues, Library.Find, layers ?? LayerSet.Default(), scenes ?? new SceneSet(), palettes ?? Rules.DefaultPalettes.Create());

    public static SceneValue Value(Guid fixtureId, AttributeKind attribute, double level, int cell = 0) =>
        new() { Target = ValueTarget.Fixture(fixtureId, cell), Attribute = attribute, Level = level };
}
