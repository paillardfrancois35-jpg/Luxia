using System.Globalization;
using System.Reflection;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Luxia.Tools.Prototype.Docking;

namespace Luxia.Tools.Prototype;

/// <summary>
/// Coquille du prototype : disposition affichée, passage d'une disposition prête à l'autre, panneaux à réafficher,
/// enregistrement (ERG-002). Suivant la charte (doc 60 §4.2), pas de bouton « Enregistrer » : la disposition est
/// écrite dès qu'elle change (vérification toutes les 2 s) et à la fermeture.
/// </summary>
internal sealed partial class ShellViewModel : ObservableObject
{
    private readonly DemoState _state;
    private readonly LayoutStore _store;
    private readonly DispatcherTimer _autosave;
    private string? _savedText;

    [ObservableProperty]
    private IRootDock? _layout;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsControl), nameof(IsShow))]
    private LayoutPreset _preset;

    [ObservableProperty]
    private string _status = "";

    public ShellViewModel(DemoState state, LayoutStore store)
    {
        _state = state;
        _store = store;
        Load(LayoutPreset.Control);
        _autosave = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, (_, _) => SaveIfChanged());
        _autosave.Start();
    }

    /// <summary>Fabrique Dock de la disposition affichée.</summary>
    public PrototypeDockFactory Factory { get; private set; } = new();

    /// <summary>Titre de la fenêtre, avec le numéro de version à vérifier (GEN-119).</summary>
    public static string Title => "LuXia – prototype ergonomique v" + Version;

    /// <summary>Version affichée (celle de la compilation).</summary>
    public static string Version =>
        typeof(ShellViewModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";

    /// <summary>Disposition Contrôle affichée.</summary>
    public bool IsControl
    {
        get => Preset == LayoutPreset.Control;
        set
        {
            if (value)
            {
                SwitchTo(LayoutPreset.Control);
            }
        }
    }

    /// <summary>Disposition Spectacle affichée.</summary>
    public bool IsShow
    {
        get => Preset == LayoutPreset.Show;
        set
        {
            if (value)
            {
                SwitchTo(LayoutPreset.Show);
            }
        }
    }

    /// <summary>Où est chaque panneau du catalogue (menu Panneaux).</summary>
    public IReadOnlyList<(PanelInfo Panel, PanelPlace Place)> PanelStates() =>
        PanelCatalog.All.Select(p => (p, Layout is null ? PanelPlace.Absent : DockTree.Find(Layout, p.Id).Place)).ToList();

    /// <summary>Réaffiche un panneau : à sa place s'il a été fermé, sinon dans le premier groupe d'onglets.</summary>
    [RelayCommand]
    public void ShowPanel(string id)
    {
        if (Layout is null)
        {
            return;
        }

        if (Factory.ShowPanel(Layout, Preset, id) is null)
        {
            Status = $"Panneau « {PanelCatalog.Get(id).Title} » : aucun groupe d'onglets où le remettre.";
            return;
        }

        _state.Write($"Disposition : panneau « {PanelCatalog.Get(id).Title} » affiché");
        SaveIfChanged();
    }

    /// <summary>Oublie la disposition enregistrée et revient à la disposition prête.</summary>
    [RelayCommand]
    public void ResetLayout()
    {
        _store.Delete(Preset);
        SetLayout(Factory.CreateLayout(Preset));
        SaveIfChanged();
        Status = $"Disposition « {PresetName(Preset)} » rétablie telle que livrée.";
        _state.Write("Disposition : rétablie par défaut");
    }

    /// <summary>Enregistre la disposition si elle a changé depuis le dernier enregistrement.</summary>
    public void SaveIfChanged()
    {
        if (Layout is null)
        {
            return;
        }

        try
        {
            var text = _store.Serialize(Layout);
            if (text == _savedText)
            {
                return;
            }

            _store.SaveSerialized(Preset, text);
            _savedText = text;
            Status = string.Create(CultureInfo.CurrentCulture, $"Disposition « {PresetName(Preset)} » enregistrée à {DateTime.Now:HH:mm:ss} — {_store.PathFor(Preset)}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or System.Text.Json.JsonException)
        {
            Status = $"Disposition non enregistrée : {ex.Message}";
        }
    }

    /// <summary>À la fermeture : dernier enregistrement.</summary>
    public void Close()
    {
        _autosave.Stop();
        SaveIfChanged();
    }

    private static string PresetName(LayoutPreset preset) => preset == LayoutPreset.Show ? "Spectacle" : "Contrôle";

    private void SwitchTo(LayoutPreset preset)
    {
        if (preset == Preset && Layout is not null)
        {
            return;
        }

        SaveIfChanged();
        Load(preset);
        _state.Write($"Disposition : {PresetName(preset)}");
    }

    private void Load(LayoutPreset preset)
    {
        Preset = preset;
        var loaded = _store.Load(preset, out var message);
        SetLayout(loaded ?? Factory.CreateLayout(preset));
        _savedText = loaded is null ? null : _store.Serialize(Layout!);
        Status = message ?? (loaded is null
            ? $"Disposition « {PresetName(preset)} » telle que livrée."
            : $"Disposition « {PresetName(preset)} » reprise de {_store.PathFor(preset)}");
    }

    private void SetLayout(IRootDock layout)
    {
        // Une fabrique par disposition affichée : ses registres (fenêtres, panneaux) ne se mélangent pas.
        CloseFloatingWindows();
        Factory = new PrototypeDockFactory();
        Factory.InitLayout(layout);
        Layout = layout;
    }

    private void CloseFloatingWindows()
    {
        if (Layout?.Windows is not { } windows)
        {
            return;
        }

        foreach (var window in windows.ToList())
        {
            window.Host?.Exit();
        }
    }
}
