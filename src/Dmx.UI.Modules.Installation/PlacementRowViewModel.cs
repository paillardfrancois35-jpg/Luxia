using CommunityToolkit.Mvvm.ComponentModel;
using Dmx.Patch.Model;

namespace Dmx.UI.Modules.Installation;

/// <summary>Position d'un appareil dans le lieu affiché (doc 13 §5, INST-051, INST-052).</summary>
public sealed partial class PlacementRowViewModel : ObservableObject
{
    /// <summary>Crée la ligne à partir de l'appareil et de sa position dans le lieu (si placé).</summary>
    public PlacementRowViewModel(PatchedFixture fixture, FixturePlacement? placement)
    {
        FixtureId = fixture.Id;
        Name = fixture.Name;
        _x = placement?.X ?? 0;
        _y = placement?.Y ?? 0;
        _heightM = placement?.HeightM ?? 2;
        _orientationDeg = placement?.OrientationDeg ?? 0;
        _hanging = placement?.Mounting == MountingKind.Hanging;
        _absent = placement?.Absent ?? false;
    }

    /// <summary>Appareil positionné.</summary>
    public Guid FixtureId { get; }

    /// <summary>Nom affiché.</summary>
    public string Name { get; }

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _heightM;

    [ObservableProperty]
    private double _orientationDeg;

    [ObservableProperty]
    private bool _hanging;

    /// <summary>Absent ce soir (INST-052).</summary>
    [ObservableProperty]
    private bool _absent;

    /// <summary>Placement correspondant, pour l'enregistrement.</summary>
    public FixturePlacement ToPlacement() => new()
    {
        FixtureId = FixtureId,
        X = X,
        Y = Y,
        HeightM = HeightM,
        OrientationDeg = OrientationDeg,
        Mounting = Hanging ? MountingKind.Hanging : MountingKind.Standing,
        Absent = Absent,
    };
}
