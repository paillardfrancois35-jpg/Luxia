using CommunityToolkit.Mvvm.ComponentModel;
using Dmx.Fixtures.Model;
using Dmx.Patch.Model;

namespace Dmx.UI.Modules.Installation;

/// <summary>Une ligne de la liste du patch (doc 13 §3).</summary>
public sealed partial class PatchRowViewModel : ObservableObject
{
    /// <summary>Crée la ligne à partir de l'appareil patché et de son modèle.</summary>
    public PatchRowViewModel(PatchedFixture fixture, FixtureType? type)
    {
        Fixture = fixture;
        Type = type;
        Modes = type?.Modes.Select(m => m.Name).ToList() ?? [];
        _editName = fixture.Name;
        _editUniverse = fixture.Universe;
        _editAddress = fixture.Address;
        _editModeName = fixture.ModeName;
    }

    /// <summary>Nom en cours d'édition (INST-017), appliqué par « Renommer ».</summary>
    [ObservableProperty]
    private string _editName;

    /// <summary>Univers en cours d'édition (INST-015), appliqué par « Déplacer ».</summary>
    [ObservableProperty]
    private int _editUniverse;

    /// <summary>Adresse en cours d'édition (INST-015), appliquée par « Déplacer ».</summary>
    [ObservableProperty]
    private int _editAddress;

    /// <summary>Mode en cours d'édition (INST-016), appliqué par « Changer de mode ».</summary>
    [ObservableProperty]
    private string _editModeName;

    /// <summary>Appareil patché.</summary>
    public PatchedFixture Fixture { get; }

    /// <summary>Modèle (copie du projet) ; <c>null</c> si absent de la copie (incohérence à signaler).</summary>
    public FixtureType? Type { get; }

    /// <summary>Identifiant stable de l'appareil (GEN-052).</summary>
    public Guid Id => Fixture.Id;

    /// <summary>Numéro court (INST-017).</summary>
    public int Number => Fixture.Number;

    /// <summary>Nom affiché.</summary>
    public string Name => Fixture.Name;

    /// <summary>Nom du modèle (« Fabricant Modèle »).</summary>
    public string ModelName => Type?.DisplayName ?? "(modèle introuvable dans la bibliothèque du projet)";

    /// <summary>Mode utilisé.</summary>
    public string ModeName => Fixture.ModeName;

    /// <summary>Modes disponibles pour ce modèle (INST-016).</summary>
    public IReadOnlyList<string> Modes { get; }

    /// <summary>Univers.</summary>
    public int Universe => Fixture.Universe;

    /// <summary>Adresse de départ.</summary>
    public int Address => Fixture.Address;

    /// <summary>Nombre de canaux occupés.</summary>
    public int ChannelCount => Type?.Modes.FirstOrDefault(m => m.Name == Fixture.ModeName)?.ChannelCount ?? 0;

    /// <summary>Plage occupée, affichée (« 8-10 »).</summary>
    public string RangeText => ChannelCount > 0 ? $"{Address}-{Address + ChannelCount - 1}" : Address.ToString(System.Globalization.CultureInfo.CurrentCulture);

    /// <summary>Couleur d'affichage.</summary>
    public string Color => Fixture.Color;

    /// <summary>Réglage à faire sur l'appareil (INST-018).</summary>
    public string? DeviceSetting => Type?.Modes.FirstOrDefault(m => m.Name == Fixture.ModeName)?.DeviceSetting;

    [ObservableProperty]
    private bool _hasOverlap;

    [ObservableProperty]
    private bool _identifying;

    /// <summary>Coché pour entrer dans une nouvelle sélection manuelle (INST-030).</summary>
    [ObservableProperty]
    private bool _isChecked;
}
