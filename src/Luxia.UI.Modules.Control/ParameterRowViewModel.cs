using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Fixtures.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>Une plage nommée d'un canal (« Rouge », « Strobe lent »…), cliquable.</summary>
/// <param name="Attribute">Attribut réglé.</param>
/// <param name="Label">Nom de la plage.</param>
/// <param name="Min">Début (0-255).</param>
/// <param name="Max">Fin (0-255).</param>
/// <param name="Color">Couleur de la pastille.</param>
public sealed record RangeChoice(AttributeKind Attribute, string Label, int Min, int Max, string Color);

/// <summary>
/// Un paramètre des appareils sélectionnés dans « Réglages des appareils » : pastille d'état (doc 60 §4.3), valeur
/// lisible, curseur, plages nommées.
/// </summary>
public sealed partial class ParameterRowViewModel : ViewModelBase
{
    private readonly Action<ParameterRowViewModel, double> _levelChanged;
    private bool _syncing;

    [ObservableProperty]
    private double _level;

    [ObservableProperty]
    private string _valueText = "—";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateTip))]
    private ParameterState _state;

    [ObservableProperty]
    private string _stateColor = "#00000000";

    [ObservableProperty]
    private string _stateStroke = ControlColors.Secondary;

    /// <summary>Crée la ligne.</summary>
    public ParameterRowViewModel(AttributeKind attribute, IReadOnlyList<RangeChoice> ranges, Action<ParameterRowViewModel, double> levelChanged)
    {
        Attribute = attribute;
        Ranges = ranges;
        _levelChanged = levelChanged;
    }

    /// <summary>Attribut réglé.</summary>
    public AttributeKind Attribute { get; }

    /// <summary>Nom lisible (glossaire des attributs).</summary>
    public string Name => AttributeCatalog.Label(Attribute);

    /// <summary>Plages nommées (vide : curseur seul).</summary>
    public IReadOnlyList<RangeChoice> Ranges { get; }

    /// <summary>Le paramètre a des plages nommées.</summary>
    public bool HasRanges => Ranges.Count > 0;

    /// <summary>Infobulle de la pastille (F5 : chaque signe s'explique).</summary>
    public string StateTip => State switch
    {
        ParameterState.LiveOverride => "Surcharge LIVE : temporaire, par-dessus les scènes ; « Libérer » la retire.",
        ParameterState.InScene => "Réglé par la scène (en LIVE : une scène qui joue ; en ÉDITION : l'étape éditée).",
        _ => "Non utilisé ici : l'appareil garde ce que donnent les autres couches.",
    };

    /// <summary>Affiche une valeur lue au moteur, sans renvoyer de commande.</summary>
    public void Show(double percent, string text, ParameterState state)
    {
        _syncing = true;
        Level = percent;
        _syncing = false;
        ValueText = text;
        State = state;
        StateColor = ControlColors.Of(state);
        StateStroke = state == ParameterState.Unused ? ControlColors.Secondary : ControlColors.Of(state);
    }

    partial void OnLevelChanged(double value)
    {
        if (!_syncing)
        {
            _levelChanged(this, value);
        }
    }
}
