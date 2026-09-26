using CommunityToolkit.Mvvm.Input;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>Bouton de plage d'un outil (SCN-031) : un clic règle la plage sur la sélection.</summary>
public sealed partial class RangeButtonViewModel : ViewModelBase
{
    private readonly LevelToolViewModel _tool;

    /// <summary>Crée le bouton.</summary>
    public RangeButtonViewModel(LevelToolViewModel tool, RangeChoice choice)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(choice);
        _tool = tool;
        Choice = choice;
    }

    /// <summary>Plage.</summary>
    public RangeChoice Choice { get; }

    /// <summary>Libellé.</summary>
    public string Label => Choice.Label;

    /// <summary>Couleur (emplacement de roue) ou gris.</summary>
    public string Color => Choice.Color;

    /// <summary>Bornes « 8-15 ».</summary>
    public string Bounds => $"{Choice.Min}-{Choice.Max}";

    [RelayCommand]
    private void Pick() => _tool.PickRangeCommand.Execute(Choice);
}
