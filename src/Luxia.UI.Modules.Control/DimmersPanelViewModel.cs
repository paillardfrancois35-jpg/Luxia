using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Patch.Model;
using Luxia.Patch.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Panneau « Groupes dimmer » (ERG-037, maquette 5) : un fader par groupe d'appareils qui a un dimmer, dans l'ordre de
/// l'arbre (les 8 premiers sont ceux de la seconde platine MIDI, ERG-038). Un réglage est une retouche en direct,
/// jamais enregistrée : le moteur multiplie l'intensité des appareils du groupe par le niveau de chaque étage.
/// </summary>
public sealed partial class DimmersPanelViewModel : ViewModelBase
{
    /// <summary>Nombre de faders de la seconde platine MIDI : au-delà, un dimmer n'a pas de fader physique.</summary>
    public const int PlatineFaders = 8;

    private readonly LuxiaRuntime _runtime;
    private readonly JournalPanelViewModel _journal;

    [ObservableProperty]
    private bool _isRetouched;

    /// <summary>Crée le panneau.</summary>
    public DimmersPanelViewModel(LuxiaRuntime runtime, JournalPanelViewModel journal)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(journal);
        _runtime = runtime;
        _journal = journal;
        runtime.Project.Changed += (_, _) =>
        {
            // Projet (ré)ouvert : les retouches en direct ne survivent pas (le moteur garde les niveaux d'un groupe recompilé).
            Rebuild();
            ReleaseLevels();
        };
        runtime.Project.ShowDataChanged += (_, _) => Rebuild();
        Rebuild();
    }

    /// <summary>Un fader par groupe qui a un dimmer.</summary>
    public ObservableCollection<DimmerFaderViewModel> Faders { get; } = [];

    /// <summary>Aucun groupe n'a de dimmer.</summary>
    public bool IsEmpty => Faders.Count == 0;

    /// <summary>Lit l'état du moteur (niveaux réglés et niveaux effectifs) ; à appeler au rythme du rafraîchissement de l'écran.</summary>
    public void Refresh()
    {
        var snapshot = _runtime.Engine.Snapshot;
        foreach (var fader in Faders)
        {
            var index = snapshot.Show.IndexOfDimmerGroup(fader.Id);
            if (index < 0 || index >= snapshot.DimmerLevels.Length)
            {
                continue;
            }

            fader.Sync(Math.Round(snapshot.DimmerLevels[index] * 100));
            fader.SetEffective(snapshot.DimmerEffective[index]);
        }

        IsRetouched = Faders.Any(f => f.IsRetouched);
    }

    /// <summary>Remet tous les dimmers à 100 % (libère les retouches).</summary>
    [RelayCommand]
    private void ResetAll()
    {
        foreach (var fader in Faders.Where(f => f.IsRetouched))
        {
            fader.Percent = 100;
        }

        _runtime.TraceUi("Contrôle", "dimmers de groupe remis à 100 %");
        _journal.Log("Dimmers de groupe remis à 100 %");
    }

    private void ReleaseLevels()
    {
        foreach (var fader in Faders)
        {
            _runtime.Engine.Send(new SetGroupDimmerCommand(CommandOrigin.User, fader.Id, 1));
        }
    }

    private void Rebuild()
    {
        var set = _runtime.Project.Groups;
        var dimmers = GroupRules.Layout(set).Where(n => n.Group.HasDimmer).Select(n => n.Group).ToList();
        Faders.Clear();
        for (var i = 0; i < dimmers.Count; i++)
        {
            Faders.Add(new DimmerFaderViewModel(dimmers[i], i < PlatineFaders ? i + 1 : null, OnLevelChanged));
        }

        OnPropertyChanged(nameof(IsEmpty));
        Refresh();
    }

    private void OnLevelChanged(DimmerFaderViewModel fader, double percent)
    {
        _runtime.Engine.Send(new SetGroupDimmerCommand(CommandOrigin.User, fader.Id, percent / 100));
        _runtime.TraceUi("Contrôle", $"dimmer « {fader.Name} » {percent:0} %");
        IsRetouched = Faders.Any(f => f.IsRetouched);
    }
}

/// <summary>Un fader de dimmer de groupe.</summary>
public sealed partial class DimmerFaderViewModel : ObservableObject
{
    private readonly Action<DimmerFaderViewModel, double> _changed;
    private readonly EngineEcho _echo = new();
    private bool _syncing;

    [ObservableProperty]
    private double _percent = 100;

    [ObservableProperty]
    private string _effectiveText = "= 100 %";

    /// <summary>Crée le fader.</summary>
    public DimmerFaderViewModel(FixtureGroup group, int? fader, Action<DimmerFaderViewModel, double> changed)
    {
        Group = group;
        Fader = fader;
        _changed = changed;
    }

    /// <summary>Groupe.</summary>
    public FixtureGroup Group { get; }

    /// <summary>Identifiant du groupe.</summary>
    public Guid Id => Group.Id;

    /// <summary>Nom du groupe.</summary>
    public string Name => Group.Name;

    /// <summary>Numéro de fader de la seconde platine (ERG-038), ou <c>null</c> au-delà de 8.</summary>
    public int? Fader { get; }

    /// <summary>Repère de fader (« ② 3 »).</summary>
    public string FaderText => Fader is { } n ? $"② {n}" : string.Empty;

    /// <summary>Le dimmer n'est pas à 100 % : retouche en cours (couleur d'avertissement).</summary>
    public bool IsRetouched => Percent < 99.5;

    /// <summary>Aide au survol.</summary>
    public string Tip => $"Dimmer « {Name} » : multiplie l'intensité de ses appareils (et de ses sous-groupes). Double-clic : remettre à 100 %.";

    /// <summary>Met à jour le niveau depuis le moteur (sans renvoyer de commande).</summary>
    public void Sync(double percent)
    {
        if (!_echo.Accept(percent) || Math.Abs(percent - Percent) < 0.5)
        {
            return;
        }

        _syncing = true;
        Percent = percent;
        _syncing = false;
    }

    /// <summary>Niveau effectif : le niveau du groupe multiplié par celui de ses parents.</summary>
    public void SetEffective(double level) => EffectiveText = string.Create(CultureInfo.CurrentCulture, $"= {Math.Round(level * 100)} %");

    partial void OnPercentChanged(double value)
    {
        OnPropertyChanged(nameof(IsRetouched));
        if (!_syncing)
        {
            _echo.Sent(value);
            _changed(this, value);
        }
    }
}
