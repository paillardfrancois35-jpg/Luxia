using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmx.Core.Dmx;
using Dmx.Core.Snapshots;
using Dmx.Hosting;
using Dmx.Messaging.Commands;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Console;

/// <summary>
/// Console en mode canaux (doc 11 §3) : faders par pages, prise / libération, sélection multiple, moniteur de sortie,
/// instantanés. Toute action passe par une commande du moteur (P3, GEN-002).
/// </summary>
public sealed partial class ConsoleViewModel : ViewModelBase, IRefreshable
{
    /// <summary>Largeur d'une tranche de fader, en pixels (sert au choix du nombre de faders par page).</summary>
    public const double StripWidth = 42;

    private readonly DmxRuntime _runtime;
    private readonly IDialogService _dialogs;
    private readonly byte[] _frame = new byte[DmxConstants.ChannelCount];
    private readonly short[] _overrides = new short[DmxConstants.ChannelCount];
    private readonly SortedSet<int> _selection = [];
    private int _lastClicked;

    [ObservableProperty]
    private int _selectedUniverse = 1;

    [ObservableProperty]
    private int _pageSize = 32;

    [ObservableProperty]
    private int _pageIndex;

    [ObservableProperty]
    private string _pageLabel = string.Empty;

    [ObservableProperty]
    private bool _isRelative = true;

    [ObservableProperty]
    private bool _showValues;

    [ObservableProperty]
    private string _hoverText = "Survolez une case du moniteur.";

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private SnapshotViewModel? _selectedSnapshot;

    [ObservableProperty]
    private bool _hasProject;

    /// <summary>Crée la console.</summary>
    public ConsoleViewModel(DmxRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _dialogs = dialogs;
        Universes = [.. Enumerable.Range(1, runtime.Engine.UniverseCount)];
        _runtime.Project.Changed += (_, _) => LoadSnapshots();
        LoadSnapshots();
        BuildPage();
    }

    /// <summary>Levé après chaque rafraîchissement (la vue met alors le moniteur à jour).</summary>
    public event EventHandler? Refreshed;

    /// <summary>Univers disponibles (CONS-009).</summary>
    public IReadOnlyList<int> Universes { get; }

    /// <summary>Faders de la page courante.</summary>
    public ObservableCollection<ChannelViewModel> Channels { get; } = [];

    /// <summary>Instantanés du projet.</summary>
    public ObservableCollection<SnapshotViewModel> Snapshots { get; } = [];

    /// <summary>Dernière trame émise de l'univers affiché.</summary>
    public ReadOnlySpan<byte> Frame => _frame;

    /// <summary>Surcharges de l'univers affiché (-1 = libre).</summary>
    public ReadOnlySpan<short> Overrides => _overrides;

    /// <summary>Premier canal de la page.</summary>
    public int FirstChannel => (PageIndex * PageSize) + 1;

    /// <summary>Dernier canal de la page.</summary>
    public int LastChannel => Math.Min(FirstChannel + PageSize - 1, DmxConstants.ChannelCount);

    private int PageCount => (DmxConstants.ChannelCount + PageSize - 1) / PageSize;

    /// <summary>Choisit 16, 32 ou 48 faders par page selon la largeur disponible (CONS-001).</summary>
    public void AdaptPageSize(double availableWidth)
    {
        var size = availableWidth >= 48 * StripWidth ? 48 : availableWidth >= 32 * StripWidth ? 32 : 16;
        if (size == PageSize)
        {
            return;
        }

        var first = FirstChannel;
        PageSize = size;
        PageIndex = (first - 1) / size;
        BuildPage();
    }

    /// <inheritdoc />
    public void Refresh()
    {
        _runtime.Engine.CopyLastFrame(SelectedUniverse, _frame);
        _runtime.Engine.CopyOverrides(SelectedUniverse, _overrides);
        foreach (var channel in Channels)
        {
            channel.Update(_frame[channel.Channel - 1], _overrides[channel.Channel - 1] >= 0);
        }

        var overridden = _runtime.Engine.OverrideCount(SelectedUniverse);
        Summary = string.Create(
            CultureInfo.CurrentCulture,
            $"{overridden} canal{(overridden > 1 ? "x" : string.Empty)} pris · {_selection.Count} sélectionné{(_selection.Count > 1 ? "s" : string.Empty)}");
        Refreshed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Clic sur un fader : sélection (CONS-006).</summary>
    public void OnFaderPressed(ChannelViewModel channel, bool control, bool shift)
    {
        ArgumentNullException.ThrowIfNull(channel);
        var c = channel.Channel;
        if (control)
        {
            if (!_selection.Remove(c))
            {
                _selection.Add(c);
            }
        }
        else if (shift && _lastClicked > 0)
        {
            for (var i = Math.Min(_lastClicked, c); i <= Math.Max(_lastClicked, c); i++)
            {
                _selection.Add(i);
            }
        }
        else if (!_selection.Contains(c))
        {
            _selection.Clear();
            _selection.Add(c);
        }

        _lastClicked = c;
        UpdateSelectionFlags();
    }

    /// <summary>Demande de valeur d'un fader : la commande porte tous les faders sélectionnés (CONS-003, CONS-006).</summary>
    public void OnFaderRequest(ChannelViewModel channel, FaderValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(request);
        var targets = _selection.Contains(channel.Channel) && _selection.Count > 1 ? [.. _selection] : new[] { channel.Channel };
        var values = new List<ChannelValue>(targets.Length);
        foreach (var target in targets)
        {
            var value = target == channel.Channel || !IsRelative
                ? request.NewValue
                : Math.Clamp(Current(target) + request.Delta, 0, 255);
            values.Add(new ChannelValue(target, (byte)value));
        }

        Send(values);
    }

    /// <summary>Saisie directe d'une valeur (CONS-002).</summary>
    public void OnValueTyped(ChannelViewModel channel, string text)
    {
        ArgumentNullException.ThrowIfNull(channel);
        var trimmed = (text ?? string.Empty).Trim().TrimEnd('%').Trim();
        if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) || value is < 0 or > 255)
        {
            Message = $"Valeur invalide pour le canal {channel.Channel} : « {text} » (0 à 255).";
            channel.ValueText = channel.Value.ToString(CultureInfo.CurrentCulture);
            return;
        }

        OnFaderRequest(channel, new FaderValueRequest(value, value - channel.Value));
    }

    /// <summary>Survol du moniteur (CONS-041).</summary>
    public void OnMonitorHover(int channel)
    {
        if (channel <= 0)
        {
            HoverText = "Survolez une case du moniteur.";
            return;
        }

        var value = _frame[channel - 1];
        var overridden = _overrides[channel - 1] >= 0 ? " – pris à la console" : string.Empty;

        // CONS-041 : appareil et attribut dès que le patch existera (P3).
        HoverText = string.Create(CultureInfo.CurrentCulture, $"Canal {channel} – valeur {value} ({Math.Round(value * 100 / 255.0)} %){overridden}");
    }

    /// <summary>Clic sur le moniteur : affiche la page du canal.</summary>
    public void GoToChannel(int channel)
    {
        if (channel is < 1 or > DmxConstants.ChannelCount)
        {
            return;
        }

        PageIndex = (channel - 1) / PageSize;
        BuildPage();
    }

    partial void OnSelectedUniverseChanged(int value)
    {
        _selection.Clear();
        BuildPage();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        PageIndex = (PageIndex - 1 + PageCount) % PageCount;
        BuildPage();
    }

    [RelayCommand]
    private void NextPage()
    {
        PageIndex = (PageIndex + 1) % PageCount;
        BuildPage();
    }

    /// <summary>Libère les faders sélectionnés (CONS-004).</summary>
    [RelayCommand]
    private void ReleaseSelection()
    {
        if (_selection.Count > 0)
        {
            _runtime.ReleaseChannels(SelectedUniverse, [.. _selection]);
        }
    }

    [RelayCommand]
    private void ReleasePage() =>
        _runtime.ReleaseChannels(SelectedUniverse, [.. Enumerable.Range(FirstChannel, LastChannel - FirstChannel + 1)]);

    [RelayCommand]
    private void ReleaseAll()
    {
        _runtime.ReleaseChannels(null, null);
        Message = "Tous les faders sont libérés.";
    }

    [RelayCommand]
    private void PageToZero() => SetPage(0);

    [RelayCommand]
    private void PageToFull() => SetPage(255);

    [RelayCommand]
    private void ClearSelection()
    {
        _selection.Clear();
        UpdateSelectionFlags();
    }

    /// <summary>Rappelle l'instantané choisi (CONS-010).</summary>
    [RelayCommand]
    private void RecallSnapshot(SnapshotViewModel? snapshot)
    {
        snapshot ??= SelectedSnapshot;
        if (snapshot is null)
        {
            return;
        }

        _runtime.RecallSnapshot(snapshot.Snapshot);
        if (snapshot.Snapshot.Universe != SelectedUniverse && Universes.Contains(snapshot.Snapshot.Universe))
        {
            SelectedUniverse = snapshot.Snapshot.Universe;
        }

        Message = $"Instantané « {snapshot.Name} » rappelé.";
    }

    /// <summary>Mémorise les faders pris de l'univers affiché.</summary>
    [RelayCommand]
    private async Task SaveSnapshotAsync()
    {
        if (_runtime.Project.Folder is null)
        {
            Message = "Ouvrez ou créez un projet pour mémoriser des instantanés.";
            return;
        }

        var channels = Enumerable.Range(1, DmxConstants.ChannelCount)
            .Where(c => _overrides[c - 1] >= 0)
            .Select(c => new SnapshotChannel(c, (byte)_overrides[c - 1]))
            .ToList();
        if (channels.Count == 0)
        {
            Message = "Aucun fader pris : rien à mémoriser.";
            return;
        }

        var name = await _dialogs.AskTextAsync("Nouvel instantané", "Nom de l'instantané :").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var existing = _runtime.Project.Console.Snapshots.FirstOrDefault(s => string.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existing is not null && !await _dialogs.ConfirmAsync("Remplacer l'instantané", $"L'instantané « {existing.Name} » existe déjà. Le remplacer ?").ConfigureAwait(true))
        {
            return;
        }

        var snapshot = new ConsoleSnapshot { Id = existing?.Id ?? Guid.NewGuid(), Name = name.Trim(), Universe = SelectedUniverse, Channels = channels, Category = existing?.Category, Description = existing?.Description };
        var list = _runtime.Project.Console.Snapshots.Where(s => s.Id != snapshot.Id).Append(snapshot).ToList();
        _runtime.Project.SaveConsole(_runtime.Project.Console with { Snapshots = list });
        LoadSnapshots();
        Message = $"Instantané « {snapshot.Name} » mémorisé ({channels.Count} canaux).";
    }

    /// <summary>Supprime l'instantané choisi, après confirmation (GEN-103).</summary>
    [RelayCommand]
    private async Task DeleteSnapshotAsync()
    {
        if (SelectedSnapshot is not { } selected)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync("Supprimer l'instantané", $"Supprimer définitivement « {selected.Name} » ?").ConfigureAwait(true))
        {
            return;
        }

        var list = _runtime.Project.Console.Snapshots.Where(s => s.Id != selected.Snapshot.Id).ToList();
        _runtime.Project.SaveConsole(_runtime.Project.Console with { Snapshots = list });
        LoadSnapshots();
        Message = $"Instantané « {selected.Name} » supprimé.";
    }

    private void LoadSnapshots()
    {
        HasProject = _runtime.Project.Folder is not null;
        Snapshots.Clear();
        foreach (var snapshot in _runtime.Project.Console.Snapshots.OrderBy(s => s.Category, StringComparer.CurrentCulture).ThenBy(s => s.Name, StringComparer.CurrentCulture))
        {
            Snapshots.Add(new SnapshotViewModel(snapshot));
        }
    }

    private void SetPage(byte value) =>
        Send([.. Enumerable.Range(FirstChannel, LastChannel - FirstChannel + 1).Select(c => new ChannelValue(c, value))]);

    private void Send(List<ChannelValue> values)
    {
        _runtime.SetChannels(SelectedUniverse, values);
        foreach (var channel in Channels)
        {
            var match = values.FindIndex(v => v.Channel == channel.Channel);
            if (match >= 0)
            {
                channel.SetLocal(values[match].Value);
                channel.IsOverridden = true;
            }
        }
    }

    private int Current(int channel)
    {
        var visible = Channels.FirstOrDefault(c => c.Channel == channel);
        return visible?.Value ?? _frame[channel - 1];
    }

    private void BuildPage()
    {
        Channels.Clear();
        for (var c = FirstChannel; c <= LastChannel; c++)
        {
            Channels.Add(new ChannelViewModel(c));
        }

        PageLabel = string.Create(CultureInfo.CurrentCulture, $"{FirstChannel}-{LastChannel}");
        OnPropertyChanged(nameof(FirstChannel));
        OnPropertyChanged(nameof(LastChannel));
        UpdateSelectionFlags();
        Refresh();
    }

    private void UpdateSelectionFlags()
    {
        foreach (var channel in Channels)
        {
            channel.IsSelected = _selection.Contains(channel.Channel);
        }
    }
}
