using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmx.Core.Dmx;
using Dmx.Fixtures.Model;
using Dmx.Hosting;
using Dmx.Messaging.Commands;
using Dmx.Patch.Rules;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Console;

/// <summary>
/// Composant « faders d'un appareil » (CONS-060) : un appareil (modèle + mode) placé à une adresse, ses canaux
/// avec l'outil adapté (fader, boutons de plages, barre 0-255). Sert au test en direct de la bibliothèque
/// (BIB-060 à 063) et servira à la console en mode appareils (P3).
/// En P2, les valeurs sont envoyées en surcharges brutes (CMD-020) sur les canaux réels.
/// </summary>
public sealed partial class FixtureFadersViewModel : ViewModelBase, IRefreshable
{
    private static readonly TimeSpan IdentifyPeriod = TimeSpan.FromMilliseconds(400);

    private readonly DmxRuntime _runtime;
    private readonly byte[] _frame = new byte[DmxConstants.ChannelCount];
    private readonly short[] _overrides = new short[DmxConstants.ChannelCount];
    private long _discoveryLast;
    private double _discoveryPosition;
    private IReadOnlyList<(int Channel, byte Value)> _identifyChannels = [];
    private long _identifyStart;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _identifying;

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private FixtureChannelViewModel? _discoveryChannel;

    [ObservableProperty]
    private bool _discoveryRunning;

    [ObservableProperty]
    private decimal _discoverySpeed = 10;

    [ObservableProperty]
    private string _discoveryText = string.Empty;

    /// <summary>Crée le composant (inactif).</summary>
    public FixtureFadersViewModel(DmxRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
    }

    /// <summary>Demande de nouvelle borne à la valeur courante de la découverte (clé du canal, valeur).</summary>
    public event EventHandler<(string ChannelKey, int Value)>? NewBoundaryRequested;

    /// <summary>Canaux de l'appareil.</summary>
    public System.Collections.ObjectModel.ObservableCollection<FixtureChannelViewModel> Channels { get; } = [];

    /// <summary>Univers.</summary>
    public int Universe { get; private set; } = 1;

    /// <summary>Adresse de l'appareil.</summary>
    public int Address { get; private set; } = 1;

    /// <summary>
    /// Place l'appareil (patch temporaire) et active le composant : les canaux sont pris à leur valeur par défaut.
    /// Renvoie un message d'erreur si l'adresse ne convient pas. Sert au test en direct de la bibliothèque
    /// (BIB-060 à 063) : appareil jetable, surcharges libérées à l'arrêt (<see cref="Stop"/>).
    /// </summary>
    public string? Start(FixtureType fixture, FixtureMode mode, int address, int universe = 1)
    {
        var error = Setup(fixture, mode, address, universe, displayName: null);
        if (error is not null)
        {
            return error;
        }

        // Valeurs par défaut du modèle (étape 1 de la chaîne de rendu) : l'appareil démarre dans un état connu.
        _runtime.SetChannels(universe, [.. Channels.Select(c => new ChannelValue(c.AbsoluteChannel, (byte)(c.Part == ChannelPart.Fine ? 0 : c.Definition.Default)))]);
        return null;
    }

    /// <summary>
    /// Place l'appareil **déjà patché** (Console en mode appareils, CONS-020) : à la différence de <see cref="Start"/>,
    /// n'écrit aucune valeur par défaut (ce sont de vraies surcharges de console, pas un essai jetable) et
    /// <see cref="Detach"/> ne les libère pas (elles se libèrent par les commandes habituelles de la console, CONS-004).
    /// </summary>
    /// <param name="fixture">Modèle.</param>
    /// <param name="mode">Mode utilisé.</param>
    /// <param name="address">Adresse patchée.</param>
    /// <param name="universe">Univers patché.</param>
    /// <param name="displayName">Nom donné par l'utilisateur au patch (INST-017), affiché au lieu du nom du modèle.</param>
    public string? Attach(FixtureType fixture, FixtureMode mode, int address, int universe, string displayName) =>
        Setup(fixture, mode, address, universe, displayName);

    /// <summary>Retire l'appareil de l'affichage (Console, changement de mode ou d'univers) sans toucher aux surcharges.</summary>
    public void Detach()
    {
        StopDiscovery();
        StopIdentifyInternal();
        Channels.Clear();
        IsActive = false;
        Summary = string.Empty;
    }

    /// <summary>
    /// Identifier l'appareil (CMD-023, INST-019, CONS-024, SIM-009) : ses canaux d'intensité clignotent,
    /// sans toucher couleur ni position ; les autres appareils ne changent pas.
    /// </summary>
    [RelayCommand]
    private void ToggleIdentify()
    {
        if (Identifying)
        {
            StopIdentifyInternal();
            return;
        }

        if (_identifyChannels.Count == 0)
        {
            return;
        }

        Identifying = true;
        _identifyStart = Stopwatch.GetTimestamp();
    }

    private void StopIdentifyInternal()
    {
        if (!Identifying)
        {
            return;
        }

        Identifying = false;
        _runtime.ReleaseChannels(Universe, [.. _identifyChannels.Select(c => c.Channel)]);
    }

    private void AdvanceIdentify()
    {
        if (!Identifying)
        {
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(_identifyStart);
        var on = elapsed.Ticks / IdentifyPeriod.Ticks % 2 == 0;
        _runtime.SetChannels(Universe, [.. _identifyChannels.Select(c => new ChannelValue(c.Channel, on ? c.Value : (byte)0))]);
    }

    /// <summary>Arrête le test : patch temporaire et surcharges supprimés (BIB-063).</summary>
    [RelayCommand]
    public void Stop()
    {
        var channels = Channels.Select(c => c.AbsoluteChannel).ToList();
        var universe = Universe;
        Detach();
        if (channels.Count > 0)
        {
            _runtime.ReleaseChannels(universe, channels);
        }
    }

    private string? Setup(FixtureType fixture, FixtureMode mode, int address, int universe, string? displayName)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(mode);
        if (address < 1 || address + mode.ChannelCount - 1 > DmxConstants.ChannelCount)
        {
            return string.Create(CultureInfo.CurrentCulture, $"Adresse invalide : le mode occupe {mode.ChannelCount} canaux, l'adresse doit être entre 1 et {DmxConstants.ChannelCount - mode.ChannelCount + 1}.");
        }

        Detach();
        Address = address;
        Universe = universe;
        _identifyChannels = IdentifyRules.IdentifyChannels(fixture, mode, address);
        for (var i = 0; i < mode.Channels.Count; i++)
        {
            var slot = mode.Channels[i];
            var definition = fixture.Channel(slot.Channel);
            if (definition is not null)
            {
                Channels.Add(new FixtureChannelViewModel(i + 1, address + i, definition, slot.Part, fixture.Physical));
            }
        }

        IsActive = true;
        var name = string.IsNullOrWhiteSpace(displayName) ? fixture.DisplayName : displayName;
        Summary = string.Create(CultureInfo.CurrentCulture, $"{name} – {mode.Name} – adresse {address} à {address + mode.ChannelCount - 1} (univers {universe})");
        return null;
    }

    /// <summary>Règle un canal (fader, saisie).</summary>
    public void SetValue(FixtureChannelViewModel channel, int value)
    {
        ArgumentNullException.ThrowIfNull(channel);
        var clamped = Math.Clamp(value, 0, 255);
        channel.Value = clamped;
        channel.IsOverridden = true;
        _runtime.SetChannels(Universe, [new ChannelValue(channel.AbsoluteChannel, (byte)clamped)]);
    }

    /// <summary>Clic sur une plage : sa valeur médiane est émise (BIB-061).</summary>
    public void SelectRange(FixtureChannelViewModel channel, Capability capability)
    {
        ArgumentNullException.ThrowIfNull(capability);
        SetValue(channel, capability.Median);
    }

    /// <summary>Démarre la découverte d'un canal : balayage lent de 0 à 255 (BIB-062).</summary>
    public void StartDiscovery(FixtureChannelViewModel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        if (DiscoveryChannel is { } previous)
        {
            previous.IsDiscovering = false;
        }

        DiscoveryChannel = channel;
        channel.IsDiscovering = true;
        _discoveryPosition = 0;
        _discoveryLast = Stopwatch.GetTimestamp();
        DiscoveryRunning = true;
        SetValue(channel, 0);
    }

    /// <summary>Pause / reprise du balayage.</summary>
    [RelayCommand]
    private void ToggleDiscovery()
    {
        if (DiscoveryChannel is null)
        {
            return;
        }

        DiscoveryRunning = !DiscoveryRunning;
        _discoveryLast = Stopwatch.GetTimestamp();
    }

    /// <summary>Crée une borne de plage à la valeur courante (« Nouvelle plage ici »).</summary>
    [RelayCommand]
    private void NewBoundaryHere()
    {
        if (DiscoveryChannel is { Part: ChannelPart.Coarse } channel && channel.Value > 0)
        {
            NewBoundaryRequested?.Invoke(this, (channel.Definition.Key, channel.Value));
        }
    }

    /// <summary>Recule / avance d'un pas pendant la découverte.</summary>
    [RelayCommand]
    private void DiscoveryStep(string? delta)
    {
        if (DiscoveryChannel is { } channel && int.TryParse(delta, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d))
        {
            DiscoveryRunning = false;
            _discoveryPosition = Math.Clamp(channel.Value + d, 0, 255);
            SetValue(channel, (int)_discoveryPosition);
        }
    }

    [RelayCommand]
    private void StopDiscovery()
    {
        if (DiscoveryChannel is { } channel)
        {
            channel.IsDiscovering = false;
        }

        DiscoveryChannel = null;
        DiscoveryRunning = false;
        DiscoveryText = string.Empty;
    }

    /// <inheritdoc />
    public void Refresh()
    {
        if (!IsActive)
        {
            return;
        }

        AdvanceDiscovery();
        AdvanceIdentify();
        _runtime.Engine.CopyLastFrame(Universe, _frame);
        _runtime.Engine.CopyOverrides(Universe, _overrides);
        foreach (var channel in Channels)
        {
            if (channel != DiscoveryChannel)
            {
                channel.Value = _frame[channel.AbsoluteChannel - 1];
            }

            channel.IsOverridden = _overrides[channel.AbsoluteChannel - 1] >= 0;
        }
    }

    private void AdvanceDiscovery()
    {
        if (DiscoveryChannel is not { } channel)
        {
            return;
        }

        if (DiscoveryRunning)
        {
            var elapsed = Stopwatch.GetElapsedTime(_discoveryLast).TotalSeconds;
            _discoveryLast = Stopwatch.GetTimestamp();
            _discoveryPosition += elapsed * (double)DiscoverySpeed;
            if (_discoveryPosition >= 255)
            {
                _discoveryPosition = 255;
                DiscoveryRunning = false;
            }

            var value = (int)_discoveryPosition;
            if (value != channel.Value)
            {
                SetValue(channel, value);
            }
        }

        DiscoveryText = string.Create(CultureInfo.CurrentCulture, $"Découverte de « {channel.Title} » : {channel.CurrentRange}");
    }
}
