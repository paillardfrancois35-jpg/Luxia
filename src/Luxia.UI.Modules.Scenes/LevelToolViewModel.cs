using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Fixtures.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>
/// Outil d'un attribut dans le programmeur (SCN-031) : fader 0-100 %, boutons de plages, marque « modifié »
/// et bouton « Retirer » (SCN-032).
/// </summary>
public sealed partial class LevelToolViewModel : ViewModelBase
{
    private readonly ProgrammerViewModel _owner;
    private bool _showing;

    [ObservableProperty]
    private double _percent;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private string _valueText = string.Empty;

    /// <summary>Crée l'outil.</summary>
    public LevelToolViewModel(ProgrammerViewModel owner, string title, AttributeKind attribute, string family, IReadOnlyList<RangeChoice> ranges)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
        Title = title;
        Attribute = attribute;
        Family = family;
        Ranges = [.. ranges.Select(r => new RangeButtonViewModel(this, r))];
    }

    /// <summary>Titre (« Intensité », « Gobo »…).</summary>
    public string Title { get; }

    /// <summary>Attribut réglé.</summary>
    public AttributeKind Attribute { get; }

    /// <summary>Famille d'emplacements retirée par « Retirer » (ProgrammerRules.Family).</summary>
    public string Family { get; }

    /// <summary>Plages du canal (premier appareil sélectionné qui a cet attribut).</summary>
    public IReadOnlyList<RangeButtonViewModel> Ranges { get; }

    /// <summary>L'attribut a des plages nommées.</summary>
    public bool HasRanges => Ranges.Count > 1;

    /// <summary>Affiche une valeur lue au moteur, sans la renvoyer.</summary>
    public void Show(double level, bool modified, string text)
    {
        _showing = true;
        Percent = Math.Round(level * 1000) / 10;
        IsModified = modified;
        ValueText = text;
        _showing = false;
    }

    partial void OnPercentChanged(double value)
    {
        if (!_showing)
        {
            _owner.SetLevel(this, Math.Clamp(value, 0, 100) / 100);
        }
    }

    [RelayCommand]
    private void Remove() => _owner.RemoveFamily(Family);

    [RelayCommand]
    private void PickRange(RangeChoice? choice)
    {
        if (choice is not null)
        {
            _owner.SetRange(this, choice);
        }
    }
}
