using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Patch.Model;
using Luxia.UI.Controls;

using Luxia.UI.Modules.Control;

namespace Luxia.UI.Modules.Scenes;

/// <summary>Une zone interdite dans l'éditeur (bornes en % de Pan et de Tilt).</summary>
public sealed partial class ZoneRowViewModel : ViewModelBase
{
    [ObservableProperty]
    private Choice<Guid> _mover;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private decimal _panMin;

    [ObservableProperty]
    private decimal _panMax;

    [ObservableProperty]
    private decimal _tiltMin;

    [ObservableProperty]
    private decimal _tiltMax;

    private (decimal Pan, decimal Tilt)? _cornerA;

    /// <summary>Crée la ligne.</summary>
    public ZoneRowViewModel(ForbiddenZone zone, Choice<Guid> mover)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(mover);
        _mover = mover;
        _name = zone.Name ?? "Zone";
        _panMin = Percent(zone.PanMin);
        _panMax = Percent(zone.PanMax);
        _tiltMin = Percent(zone.TiltMin);
        _tiltMax = Percent(zone.TiltMax);
    }

    /// <summary>Premier coin visé : gardé jusqu'au second pour former le rectangle.</summary>
    public void SetFirstCorner(decimal pan, decimal tilt)
    {
        _cornerA = (pan, tilt);
        (PanMin, PanMax, TiltMin, TiltMax) = (pan, pan, tilt, tilt);
    }

    /// <summary>Coin opposé : complète le rectangle avec le premier coin (ou les bornes actuelles).</summary>
    public void SetSecondCorner(decimal pan, decimal tilt)
    {
        var (panA, tiltA) = _cornerA ?? (PanMin, TiltMin);
        (PanMin, PanMax) = (Math.Min(panA, pan), Math.Max(panA, pan));
        (TiltMin, TiltMax) = (Math.Min(tiltA, tilt), Math.Max(tiltA, tilt));
    }

    /// <summary>Zone enregistrée (bornes 0-1).</summary>
    public ForbiddenZone ToZone() => new()
    {
        FixtureId = Mover.Value,
        Name = string.IsNullOrWhiteSpace(Name) ? null : Name.Trim(),
        PanMin = (double)Math.Min(PanMin, PanMax) / 100,
        PanMax = (double)Math.Max(PanMin, PanMax) / 100,
        TiltMin = (double)Math.Min(TiltMin, TiltMax) / 100,
        TiltMax = (double)Math.Max(TiltMin, TiltMax) / 100,
    };

    private static decimal Percent(double value) => (decimal)Math.Round(value * 100, 1);
}
