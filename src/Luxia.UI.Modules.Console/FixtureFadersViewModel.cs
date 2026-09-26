using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Core.Dmx;
using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Patch.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Console;

/// <summary>
/// Composant « faders d'un appareil » (CONS-060) : un appareil (modèle + mode) placé à une adresse, ses canaux
/// avec l'outil adapté (fader, boutons de plages, barre 0-255). Sert au test en direct de la bibliothèque
/// (BIB-060 à 063) et servira à la console en mode appareils (P3).
/// Pour le test en direct (appareil non patché), les valeurs sont des surcharges brutes (CMD-020) sur les canaux réels ;
/// pour un appareil patché (Console en mode appareils), ce sont des surcharges d'<b>attributs</b> (CMD-021, étape 5 de la
/// chaîne) : elles passent par le Grand Master et le blackout (CONS-022).
/// </summary>
public sealed partial class FixtureFadersViewModel : ViewModelBase, IRefreshable
{
    private static readonly TimeSpan IdentifyPeriod = TimeSpan.FromMilliseconds(400);

    private readonly LuxiaRuntime _runtime;
    private readonly byte[] _frame = new byte[DmxConstants.ChannelCount];
    private readonly short[] _overrides = new short[DmxConstants.ChannelCount];
    private long _discoveryLast;
    private double _discoveryPosition;
    private IReadOnlyList<(int Channel, byte Value)> _identifyChannels = [];
    private long _identifyStart;
    private Guid? _fixtureId;
    private bool _hasVirtualIntensity;

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
    public FixtureFadersViewModel(LuxiaRuntime runtime)
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
    /// <param name="fixtureId">Appareil patché : ses réglages deviennent des surcharges d'attributs (CONS-022).</param>
    public string? Attach(FixtureType fixture, FixtureMode mode, int address, int universe, string displayName, Guid? fixtureId = null)
    {
        var error = Setup(fixture, mode, address, universe, displayName);
        if (error is not null || fixtureId is not { } id)
        {
            return error;
        }

        _fixtureId = id;

        // BIB-006 : un appareil sans gradateur a une intensité virtuelle (0 par défaut, D27) ; on lui donne un fader.
        _hasVirtualIntensity = FixtureRules.HasVirtualIntensity(fixture, mode);
        if (_hasVirtualIntensity)
        {
            var virtualDimmer = new ChannelDefinition { Key = RigParameter.VirtualIntensityKey, Name = "Intensité (virtuelle)", Attribute = AttributeKind.Intensity };
            Channels.Insert(0, new FixtureChannelViewModel(0, 0, virtualDimmer, ChannelPart.Coarse, fixture.Physical));
        }

        return null;
    }

    /// <summary>Appareil patché affiché (null pour le test en direct de la bibliothèque).</summary>
    public Guid? FixtureId => _fixtureId;

    /// <summary>Retire l'appareil de l'affichage (Console, changement de mode ou d'univers) sans toucher aux surcharges.</summary>
    public void Detach()
    {
        StopDiscovery();
        StopIdentifyInternal();
        Channels.Clear();
        _fixtureId = null;
        _hasVirtualIntensity = false;
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
        if (_fixtureId is not { } fixtureId)
        {
            _runtime.SetChannels(Universe, [new ChannelValue(channel.AbsoluteChannel, (byte)clamped)]);
            return;
        }

        // CONS-022 : surcharge de l'attribut (étape 5), soumise au Grand Master et au blackout.
        var values = new List<AttributeValue> { new(fixtureId, channel.Definition.Key, Normalized(channel, clamped)) };

        // « Allumer en coloriant » version console (MOT-041) : sur un appareil sans gradateur, régler une couleur alors que
        // l'intensité virtuelle n'est pas prise l'allume à 100 %, comme le faisait la console avant P4.
        var dimmer = Channels.FirstOrDefault(c => c.Definition.Key == RigParameter.VirtualIntensityKey);
        if (_hasVirtualIntensity && dimmer is { IsOverridden: false } && AttributeCatalog.Get(channel.Definition.Attribute).IsEmitter && clamped > 0)
        {
            values.Add(new AttributeValue(fixtureId, RigParameter.VirtualIntensityKey, 1));
            dimmer.Value = 255;
            dimmer.IsOverridden = true;
        }

        _runtime.Engine.Send(new OverrideAttributesCommand(CommandOrigin.User, values));
    }

    /// <summary>Libère les surcharges d'attributs de l'appareil (bouton « Libérer » de l'appareil).</summary>
    [RelayCommand]
    private void Release()
    {
        if (_fixtureId is { } fixtureId)
        {
            _runtime.Engine.Send(new ReleaseAttributesCommand(CommandOrigin.User, fixtureId));
        }
        else if (Channels.Count > 0)
        {
            _runtime.ReleaseChannels(Universe, [.. Channels.Select(c => c.AbsoluteChannel)]);
        }
    }

    /// <summary>
    /// Valeur normalisée d'un canal : un octet d'un attribut 16 bits se combine avec l'autre octet tel qu'il est émis.
    /// </summary>
    private double Normalized(FixtureChannelViewModel channel, int value)
    {
        if (channel.Definition.Resolution != ChannelResolution.Bit16)
        {
            return DmxValues.From8Bit((byte)value);
        }

        var other = Channels.FirstOrDefault(c => c.Definition.Key == channel.Definition.Key && c.Part != channel.Part);
        var otherValue = other is null ? (byte)0 : (byte)other.Value;
        return channel.Part == ChannelPart.Fine
            ? DmxValues.From16Bit(otherValue, (byte)value)
            : DmxValues.From16Bit((byte)value, otherValue);
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
    public void Refresh() => Refresh(_fixtureId is null ? null : _runtime.Engine.Snapshot);

    /// <summary>
    /// Rafraîchit à partir d'un état du moteur déjà lu (la Console le lit une fois pour tous ses appareils) : un canal est
    /// « pris » s'il a une surcharge brute ou, pour un appareil patché, une surcharge de son attribut.
    /// </summary>
    public void Refresh(EngineSnapshot? snapshot)
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
            var index = _fixtureId is { } id && snapshot is not null ? snapshot.Show.IndexOf(id, channel.Definition.Key) : -1;
            var attributeTaken = index >= 0 && index < snapshot!.Overrides.Length && !double.IsNaN(snapshot.Overrides[index]);
            if (channel.AbsoluteChannel == 0)
            {
                // Intensité virtuelle : pas de canal DMX, valeur lue dans l'état du moteur.
                channel.Value = index >= 0 && index < snapshot!.Values.Length ? DmxValues.To8Bit(snapshot.Values[index]) : 0;
                channel.IsOverridden = attributeTaken;
                continue;
            }

            if (channel != DiscoveryChannel)
            {
                channel.Value = _frame[channel.AbsoluteChannel - 1];
            }

            channel.IsOverridden = _overrides[channel.AbsoluteChannel - 1] >= 0 || attributeTaken;
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
