using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Scenes.Compilation;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>Un appareil patché dans la liste de sélection du programmeur (SCN-030).</summary>
public sealed partial class ProgrammerFixtureViewModel : ViewModelBase
{
    private readonly ProgrammerViewModel _owner;
    private bool _silent;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _hasValues;

    /// <summary>Crée la ligne.</summary>
    public ProgrammerFixtureViewModel(ProgrammerViewModel owner, FixtureInfo info)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(info);
        _owner = owner;
        Info = info;
    }

    /// <summary>Appareil résolu.</summary>
    public FixtureInfo Info { get; }

    /// <summary>Identifiant.</summary>
    public Guid Id => Info.Fixture.Id;

    /// <summary>Nom (« PAR 1 »).</summary>
    public string Name => Info.Fixture.Name;

    /// <summary>Modèle et mode.</summary>
    public string Model => $"{Info.Type.DisplayName} · {Info.Mode.Name}{(Info.Absent ? " · absent" : string.Empty)}";

    /// <summary>Couleur d'affichage de l'appareil (INST-017).</summary>
    public string Color => Info.Fixture.Color;

    /// <summary>Coche sans prévenir le programmeur (sélection par un raccourci).</summary>
    public void SetSelected(bool value)
    {
        _silent = true;
        IsSelected = value;
        _silent = false;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        if (!_silent)
        {
            _owner.OnFixtureToggled();
        }
    }
}
