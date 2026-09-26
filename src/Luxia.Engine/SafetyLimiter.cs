using System.Globalization;
using Luxia.Core.Dmx;
using Luxia.Engine.Model;
using Luxia.Messaging.Events;

namespace Luxia.Engine;

/// <summary>
/// Étape 9 de la chaîne de rendu (doc 02 §9, doc 15 §9) : zones interdites Pan/Tilt sur les paramètres, puis limiteurs de
/// strobe et de fumée sur les <b>octets finaux</b> — après les surcharges brutes de la console et le test de sortie, pour
/// que la fumée et le strobe ne soient jamais contournables (GEN-042). Utilisé uniquement par le fil du moteur.
/// </summary>
internal sealed class SafetyLimiter
{
    /// <summary>Un strobe interrompu moins longtemps que ce délai reste le même épisode (on ne remet pas le compteur à zéro).</summary>
    private const double StrobeGapSeconds = 1;

    private readonly List<ActiveLimit> _active = [];
    private SafetyModel _model = SafetyModel.None;
    private StrobeGroup[] _strobeGroups = [];
    private SmokeState[] _smoke = [];
    private bool[] _zoneReported = [];
    private bool _smokeHeld;
    private double _smokeBurst;
    private byte _smokeLevel = 255;

    /// <summary>Limites en train d'agir (pour l'affichage en Live, GEN-086).</summary>
    public IReadOnlyList<ActiveLimit> Active => _active;

    /// <summary>Fumée manuelle en cours (maintien ou rafale, CMD-030).</summary>
    public bool ManualSmoke => _smokeHeld || _smokeBurst > 0;

    /// <summary>Commande de fumée manuelle (CMD-030) : maintien tant que <paramref name="pressed"/>, ou rafale.</summary>
    public void SetManualSmoke(bool pressed, double? burstSeconds, double level)
    {
        _smokeLevel = (byte)Math.Clamp(Math.Round(level * 255), 1, 255);
        if (burstSeconds is { } burst)
        {
            _smokeBurst = Math.Max(_smokeBurst, burst);
            return;
        }

        _smokeHeld = pressed;
    }

    /// <summary>Charge un modèle ; l'état des compteurs est gardé pour les canaux qui existent encore.</summary>
    public void Load(SafetyModel model)
    {
        var previousStrobe = _strobeGroups.ToDictionary(g => g.FixtureId);
        var previousSmoke = _smoke.ToDictionary(s => (s.Channel.Universe, s.Channel.Channel));
        _model = model;
        _strobeGroups = [.. model.StrobeChannels
            .GroupBy(c => c.FixtureId)
            .Select(g =>
            {
                var group = new StrobeGroup(g.Key, [.. g]);
                if (previousStrobe.TryGetValue(g.Key, out var old))
                {
                    group.CopyStateFrom(old);
                }

                return group;
            })];
        _smoke = [.. model.SmokeChannels.Select(c =>
        {
            var state = new SmokeState(c);
            if (previousSmoke.TryGetValue((c.Universe, c.Channel), out var old))
            {
                state.CopyStateFrom(old);
            }

            return state;
        })];
        _zoneReported = new bool[model.Zones.Count];
    }

    /// <summary>Zones interdites : ramène la cible Pan/Tilt au point autorisé le plus proche (MOT-082).</summary>
    /// <param name="values">Valeurs logiques finales (modifiées sur place).</param>
    /// <param name="mirror">Autre tableau de valeurs à tenir identique pour Pan et Tilt (état publié).</param>
    /// <param name="now">Instant du tick.</param>
    /// <param name="publish">Publication d'un événement.</param>
    public void ApplyZones(double[] values, double[] mirror, TimeSpan now, Action<SafetyLimitReached> publish)
    {
        var zones = _model.Zones;
        for (var i = 0; i < zones.Count; i++)
        {
            var guard = zones[i];
            if (guard.Pan >= values.Length || guard.Tilt >= values.Length)
            {
                continue;
            }

            var pan = values[guard.Pan];
            var tilt = values[guard.Tilt];
            if (!NearestAllowed(guard.Zones, ref pan, ref tilt))
            {
                _zoneReported[i] = false;
                continue;
            }

            values[guard.Pan] = mirror[guard.Pan] = pan;
            values[guard.Tilt] = mirror[guard.Tilt] = tilt;
            _active.Add(new ActiveLimit(SafetyLimitKind.Zone, guard.FixtureId, guard.Label, "position ramenée hors de la zone interdite"));
            if (!_zoneReported[i])
            {
                _zoneReported[i] = true;
                publish(new SafetyLimitReached(
                    SafetyLimitKind.Zone,
                    guard.FixtureId,
                    guard.Label,
                    string.Create(CultureInfo.CurrentCulture, $"cible dans une zone interdite : ramenée à Pan {pan:P0}, Tilt {tilt:P0}"),
                    now));
            }
        }
    }

    /// <summary>Début d'un tick : les limites affichées sont recalculées.</summary>
    public void BeginTick() => _active.Clear();

    /// <summary>Strobe et fumée sur les trames finales (MOT-080, MOT-081).</summary>
    /// <param name="frames">Trames des univers (indice 0 = univers 1), modifiées sur place.</param>
    /// <param name="elapsed">Temps écoulé depuis le tick précédent, en secondes.</param>
    /// <param name="now">Instant du tick.</param>
    /// <param name="publish">Publication d'un événement.</param>
    public void ApplyToFrames(DmxFrame[] frames, double elapsed, TimeSpan now, Action<SafetyLimitReached> publish)
    {
        // Fumée manuelle (CMD-030) : ajoutée avant le limiteur, qui s'applique donc aussi à elle (GEN-084).
        if (ManualSmoke)
        {
            foreach (var smoke in _smoke)
            {
                if (Value(frames, smoke.Channel) is { } current && current < _smokeLevel)
                {
                    Write(frames, smoke.Channel, _smokeLevel);
                }
            }

            _smokeBurst = Math.Max(0, _smokeBurst - elapsed);
        }

        foreach (var group in _strobeGroups)
        {
            ApplyStrobe(group, frames, elapsed, now, publish);
        }

        foreach (var smoke in _smoke)
        {
            ApplySmoke(smoke, frames, elapsed, now, publish);
        }
    }

    private void ApplyStrobe(StrobeGroup group, DmxFrame[] frames, double elapsed, TimeSpan now, Action<SafetyLimitReached> publish)
    {
        var limits = _model.Strobe;
        var requested = false;
        foreach (var channel in group.Channels)
        {
            if (Value(frames, channel) is { } value && channel.IsActive(value))
            {
                requested = true;
                break;
            }
        }

        string? reason = null;
        if (limits.Forbidden)
        {
            reason = "strobe interdit";
        }
        else if (group.PauseRemaining > 0)
        {
            group.PauseRemaining -= elapsed;
            if (group.PauseRemaining > 0)
            {
                reason = string.Create(CultureInfo.CurrentCulture, $"pause forcée ({group.PauseRemaining:0} s restantes)");
            }
            else
            {
                group.StrobeSeconds = 0;
            }
        }

        if (reason is null && requested)
        {
            group.StrobeSeconds += elapsed;
            group.GapSeconds = 0;
            if (group.StrobeSeconds > limits.MaxContinuousSeconds)
            {
                group.PauseRemaining = limits.PauseSeconds;
                group.StrobeSeconds = 0;
                reason = string.Create(
                    CultureInfo.CurrentCulture,
                    $"strobe coupé après {limits.MaxContinuousSeconds:0.#} s continues, pause de {limits.PauseSeconds:0.#} s");
                group.Reported = false;
            }
        }
        else if (reason is null)
        {
            group.GapSeconds += elapsed;
            if (group.GapSeconds >= StrobeGapSeconds)
            {
                group.StrobeSeconds = 0;
            }
        }

        if (reason is not null)
        {
            // Pause, interdiction : toutes les voies de strobe de l'appareil au repos.
            if (requested)
            {
                foreach (var channel in group.Channels)
                {
                    Write(frames, channel, channel.Rest);
                }

                Report(group, SafetyLimitKind.Strobe, group.Channels[0], reason, now, publish);
            }
            else
            {
                group.Reported = false;
            }

            return;
        }

        if (!requested)
        {
            group.Reported = false;
            return;
        }

        if (limits.MaxSpeed < 1)
        {
            var capped = false;
            foreach (var channel in group.Channels)
            {
                capped |= CapSpeed(frames, channel, limits.MaxSpeed);
            }

            if (capped)
            {
                Report(group, SafetyLimitKind.Strobe, group.Channels[0], string.Create(CultureInfo.CurrentCulture, $"vitesse de strobe plafonnée à {limits.MaxSpeed:P0}"), now, publish);
                return;
            }
        }

        group.Reported = false;
    }

    private void ApplySmoke(SmokeState smoke, DmxFrame[] frames, double elapsed, TimeSpan now, Action<SafetyLimitReached> publish)
    {
        var limits = _model.Smoke;
        var channel = smoke.Channel;
        var requested = Value(frames, channel) is { } value && channel.IsActive(value);
        string? reason = null;
        if (smoke.RestRemaining > 0)
        {
            smoke.RestRemaining -= elapsed;
            if (smoke.RestRemaining > 0 && requested)
            {
                reason = string.Create(CultureInfo.CurrentCulture, $"repos minimal de la fumée ({smoke.RestRemaining:0} s restantes)");
            }
        }
        else if (requested)
        {
            smoke.EmittingSeconds += elapsed;
            if (smoke.EmittingSeconds > limits.MaxEmissionSeconds)
            {
                smoke.EmittingSeconds = 0;
                smoke.RestRemaining = limits.MinRestSeconds;
                smoke.Reported = false;
                reason = string.Create(
                    CultureInfo.CurrentCulture,
                    $"fumée coupée après {limits.MaxEmissionSeconds:0.#} s d'émission, repos de {limits.MinRestSeconds:0.#} s");
            }
        }
        else if (smoke.EmittingSeconds > 0)
        {
            // Fin d'une émission : le repos minimal commence (GEN-084).
            smoke.EmittingSeconds = 0;
            smoke.RestRemaining = limits.MinRestSeconds;
        }

        if (reason is null)
        {
            if (!requested)
            {
                smoke.Reported = false;
            }

            return;
        }

        Write(frames, channel, channel.Rest);
        _active.Add(new ActiveLimit(SafetyLimitKind.Smoke, channel.FixtureId, channel.Label, reason));
        if (!smoke.Reported)
        {
            smoke.Reported = true;
            publish(new SafetyLimitReached(SafetyLimitKind.Smoke, channel.FixtureId, channel.Label, reason, now));
        }
    }

    private void Report(StrobeGroup group, SafetyLimitKind kind, GuardedChannel channel, string reason, TimeSpan now, Action<SafetyLimitReached> publish)
    {
        _active.Add(new ActiveLimit(kind, channel.FixtureId, channel.Label, reason));
        if (group.Reported)
        {
            return;
        }

        group.Reported = true;
        publish(new SafetyLimitReached(kind, channel.FixtureId, channel.Label, reason, now));
    }

    private static bool CapSpeed(DmxFrame[] frames, GuardedChannel channel, double maxSpeed)
    {
        if (Value(frames, channel) is not { } value)
        {
            return false;
        }

        foreach (var range in channel.Progressive)
        {
            if (!range.Contains(value))
            {
                continue;
            }

            var span = range.Max - range.Min;
            var limit = range.Increasing
                ? range.Min + (int)Math.Floor(span * maxSpeed)
                : range.Max - (int)Math.Floor(span * maxSpeed);
            var capped = range.Increasing ? Math.Min(value, limit) : Math.Max(value, limit);
            if (capped == value)
            {
                return false;
            }

            Write(frames, channel, (byte)capped);
            return true;
        }

        return false;
    }

    private static int? Value(DmxFrame[] frames, GuardedChannel channel) =>
        channel.Universe >= 1 && channel.Universe <= frames.Length && channel.Channel is >= 1 and <= DmxConstants.ChannelCount
            ? frames[channel.Universe - 1].Values[channel.Channel - 1]
            : null;

    private static void Write(DmxFrame[] frames, GuardedChannel channel, byte value)
    {
        if (channel.Universe >= 1 && channel.Universe <= frames.Length && channel.Channel is >= 1 and <= DmxConstants.ChannelCount)
        {
            frames[channel.Universe - 1].Values[channel.Channel - 1] = value;
        }
    }

    /// <summary>
    /// Point autorisé le plus proche d'une cible (distance euclidienne dans le plan Pan/Tilt normalisé).
    /// Renvoie <c>false</c> si la cible est déjà autorisée.
    /// </summary>
    internal static bool NearestAllowed(IReadOnlyList<PanTiltZone> zones, ref double pan, ref double tilt)
    {
        var inside = false;
        foreach (var zone in zones)
        {
            inside |= zone.Contains(pan, tilt);
        }

        if (!inside)
        {
            return false;
        }

        // Candidats : projections sur les bords de chaque zone (et coins), gardés s'ils ne tombent dans aucune zone.
        var bestPan = pan;
        var bestTilt = tilt;
        var best = double.MaxValue;
        foreach (var zone in zones)
        {
            Span<(double Pan, double Tilt)> candidates =
            [
                (zone.PanMin, tilt), (zone.PanMax, tilt), (pan, zone.TiltMin), (pan, zone.TiltMax),
                (zone.PanMin, zone.TiltMin), (zone.PanMin, zone.TiltMax), (zone.PanMax, zone.TiltMin), (zone.PanMax, zone.TiltMax),
            ];
            foreach (var (p, t) in candidates)
            {
                if (p is < 0 or > 1 || t is < 0 or > 1)
                {
                    continue;
                }

                var allowed = true;
                foreach (var other in zones)
                {
                    allowed &= !other.Contains(p, t);
                }

                var distance = ((p - pan) * (p - pan)) + ((t - tilt) * (t - tilt));
                if (allowed && distance < best)
                {
                    best = distance;
                    bestPan = p;
                    bestTilt = t;
                }
            }
        }

        if (best == double.MaxValue)
        {
            // Aucun bord libre (zones qui couvrent tout) : position de repli au coin (0, 0).
            bestPan = 0;
            bestTilt = 0;
        }

        pan = bestPan;
        tilt = bestTilt;
        return true;
    }

    private sealed class StrobeGroup(Guid fixtureId, GuardedChannel[] channels)
    {
        public Guid FixtureId { get; } = fixtureId;

        public GuardedChannel[] Channels { get; } = channels;

        public double StrobeSeconds { get; set; }

        public double GapSeconds { get; set; }

        public double PauseRemaining { get; set; }

        public bool Reported { get; set; }

        public void CopyStateFrom(StrobeGroup other)
        {
            StrobeSeconds = other.StrobeSeconds;
            GapSeconds = other.GapSeconds;
            PauseRemaining = other.PauseRemaining;
            Reported = other.Reported;
        }
    }

    private sealed class SmokeState(GuardedChannel channel)
    {
        public GuardedChannel Channel { get; } = channel;

        public double EmittingSeconds { get; set; }

        public double RestRemaining { get; set; }

        public bool Reported { get; set; }

        public void CopyStateFrom(SmokeState other)
        {
            EmittingSeconds = other.EmittingSeconds;
            RestRemaining = other.RestRemaining;
            Reported = other.Reported;
        }
    }
}

/// <summary>Limite de sûreté en train d'agir (GEN-086, LIVE-008).</summary>
/// <param name="Kind">Limite.</param>
/// <param name="FixtureId">Appareil.</param>
/// <param name="Label">Appareil ou canal, lisible.</param>
/// <param name="Detail">Ce que fait le limiteur.</param>
public readonly record struct ActiveLimit(SafetyLimitKind Kind, Guid FixtureId, string Label, string Detail);
