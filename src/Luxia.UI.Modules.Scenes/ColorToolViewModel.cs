using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>
/// Outil couleur du programmeur (SCN-031) : couleur logique R, V, B (+ blanc, ambre, UV si la sélection en a),
/// traduite ensuite pour chaque appareil (GEN-022) ; pastille d'aperçu, « modifié », « Retirer ».
/// </summary>
public sealed partial class ColorToolViewModel : ViewModelBase
{
    private readonly ProgrammerViewModel _owner;
    private bool _showing;
    private bool _whiteTouched;
    private bool _amberTouched;
    private bool _uvTouched;

    [ObservableProperty]
    private double _red;

    [ObservableProperty]
    private double _green;

    [ObservableProperty]
    private double _blue;

    [ObservableProperty]
    private double _white;

    [ObservableProperty]
    private double _amber;

    [ObservableProperty]
    private double _uv;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private string _swatch = "#000000";

    [ObservableProperty]
    private bool _hasWhite;

    [ObservableProperty]
    private bool _hasAmber;

    [ObservableProperty]
    private bool _hasUv;

    /// <summary>Crée l'outil.</summary>
    public ColorToolViewModel(ProgrammerViewModel owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
    }

    /// <summary>Couleur logique actuellement réglée.</summary>
    public LogicalColor Current => new()
    {
        R = Red / 100,
        G = Green / 100,
        B = Blue / 100,
        White = _whiteTouched ? White / 100 : null,
        Amber = _amberTouched ? Amber / 100 : null,
        Uv = _uvTouched ? Uv / 100 : null,
    };

    /// <summary>Affiche une couleur (programmeur ou moteur), sans la renvoyer.</summary>
    public void Show(LogicalColor color, bool modified)
    {
        ArgumentNullException.ThrowIfNull(color);
        _showing = true;
        Red = Round(color.R);
        Green = Round(color.G);
        Blue = Round(color.B);
        White = Round(color.White ?? 0);
        Amber = Round(color.Amber ?? 0);
        Uv = Round(color.Uv ?? 0);
        _whiteTouched = color.White is not null;
        _amberTouched = color.Amber is not null;
        _uvTouched = color.Uv is not null;
        IsModified = modified;
        Swatch = color.Hex;
        _showing = false;
    }

    partial void OnRedChanged(double value) => Push();

    partial void OnGreenChanged(double value) => Push();

    partial void OnBlueChanged(double value) => Push();

    partial void OnWhiteChanged(double value)
    {
        _whiteTouched |= !_showing;
        Push();
    }

    partial void OnAmberChanged(double value)
    {
        _amberTouched |= !_showing;
        Push();
    }

    partial void OnUvChanged(double value)
    {
        _uvTouched |= !_showing;
        Push();
    }

    [RelayCommand]
    private void Remove() => _owner.RemoveFamily(ProgrammerRules.ColorSlot);

    private void Push()
    {
        if (_showing)
        {
            return;
        }

        Swatch = Current.Hex;
        _owner.SetColor(Current);
    }

    private static double Round(double level) => Math.Round(Math.Clamp(level, 0, 1) * 1000) / 10;
}
