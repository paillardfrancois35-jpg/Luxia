using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmx.Core.Dmx;
using Dmx.Fixtures.Model;
using Dmx.Hosting;
using Dmx.Messaging.Commands;
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
    private readonly DmxRuntime _runtime;
    private readonly byte[] _frame = new byte[DmxConstants.ChannelCount];
    private readonly short[] _overrides = new short[DmxConstants.ChannelCount];
    private long _discoveryLast;
    private double _discoveryPosition;

    [ObservableProperty]
    private bool _isActive;

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
    /// Renvoie un message d'erreur si l'adresse ne convient pas.
    /// </summary>
    public string? Start(FixtureType fixture, FixtureMode mode, int address, int universe = 1)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(mode);
        if (address < 1 || address + mode.ChannelCount - 1 > DmxConstants.ChannelCount)
        {
            return string.Create(CultureInfo.CurrentCulture, $"Adresse invalide : le mode occupe {mode.ChannelCount} canaux, l'adresse doit être entre 1 et {DmxConstants.ChannelCount - mode.ChannelCount + 1}.");
        }

        Stop();
        Address = address;
        Universe = universe;
        for (var i = 0; i < mode.Channels.Count; i++)
        {
            var slot = mode.Channels[i];
            var definition = fixture.Channel(slot.Channel);
            if (definition is not null)
            {
                Channels.Add(new FixtureChannelViewModel(i + 1, address + i, definition, slot.Part));
            }
        }

        // Valeurs par défaut du modèle (étape 1 de la chaîne de rendu) : l'appareil démarre dans un état connu.
        _runtime.SetChannels(universe, [.. Channels.Select(c => new ChannelValue(c.AbsoluteChannel, (byte)(c.Part == ChannelPart.Fine ? 0 : c.Definition.Default)))]);
        IsActive = true;
        Summary = string.Create(CultureInfo.CurrentCulture, $"{fixture.DisplayName} – {mode.Name} – adresse {address} à {address + mode.ChannelCount - 1} (univers {universe})");
        return null;
    }

    /// <summary>Arrête le test : patch temporaire et surcharges supprimés (BIB-063).</summary>
    [RelayCommand]
    public void Stop()
    {
        StopDiscovery();
        if (Channels.Count > 0)
        {
            _runtime.ReleaseChannels(Universe, [.. Channels.Select(c => c.AbsoluteChannel)]);
        }

        Channels.Clear();
        IsActive = false;
        Summary = string.Empty;
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
