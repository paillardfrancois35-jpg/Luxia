using Luxia.Engine.Model;
using Luxia.Engine.Timing;
using Luxia.Messaging.Commands;

namespace Luxia.Engine;

/// <summary>
/// Lecture d'une scène dans une couche (doc 15 §2) : étape courante, temps écoulé, sens, passages, vitesse,
/// et pour chaque paramètre touché par la scène une contribution (valeur + poids) interpolée pendant les fondus.
/// </summary>
/// <remarks>
/// Toutes les transitions (entrée, changement d'étape, fondu croisé) sont traitées de la même façon : chaque paramètre
/// part de sa contribution actuelle (valeur, poids) vers celle de l'étape visée. Un paramètre absent de l'étape visée
/// garde sa valeur et voit son poids tomber à 0 : il rend la main à la valeur sous-jacente. Un paramètre qui n'avait
/// pas de poids prend directement sa valeur cible et monte en poids : il part de la valeur sous-jacente (doc 15 §5.1).
/// Les tableaux sont alloués au lancement (commande), jamais pendant les ticks (doc 03 §4.1).
/// </remarks>
internal sealed class Playback
{
    private int[][] _stepSlots = [];

    public Playback(EngineScene scene, int layerIndex, long sequence, CommandOrigin origin, bool solo)
    {
        Scene = scene;
        UpdateOwnClock();
        LayerIndex = layerIndex;
        Sequence = sequence;
        Origin = origin;
        Solo = solo;
        Speed = Math.Clamp(scene.Speed, 0.1, 10);
        Parameters = [];
        StartValue = StartWeight = TargetValue = TargetWeight = Value = Weight = FadeSeconds = DelaySeconds = [];
        Masked = Discrete = [];
    }

    public EngineScene Scene { get; private set; }

    /// <summary>Horloge principale du moteur (tempo, temps franchis à ce tick, sauts de phase).</summary>
    public MusicalClock Clock { get; init; } = new();

    /// <summary>Impulsions audio du tick (basses, aigus) et disponibilité du signal (MOT-017, SCN-052).</summary>
    public TickEvents Events { get; init; } = new();

    public int LayerIndex { get; set; }

    /// <summary>Ordre d'activation : à priorité égale, la plus récente l'emporte (doc 15 §5.2).</summary>
    public long Sequence { get; }

    public CommandOrigin Origin { get; }

    public bool Solo { get; }

    /// <summary>Flash (CMD-014) : fusionné au-dessus de toutes les couches, tant que la commande est maintenue (MOT-072).</summary>
    public bool Flash { get; init; }

    public PlaybackState State { get; private set; } = PlaybackState.FadingIn;

    public int StepIndex { get; private set; }

    public double Speed { get; set; }

    /// <summary>Poids de sortie (1 → 0 pendant le fondu de sortie).</summary>
    public double ExitWeight { get; private set; } = 1;

    /// <summary>Maintenue sur sa dernière étape (EndMode.Hold).</summary>
    public bool Holding { get; private set; }

    /// <summary>Progression 0-1 dans l'étape courante (affichage).</summary>
    public double StepProgress => Scene.Advance != StepAdvanceMode.Duration
        ? Math.Clamp(_eventProgress, 0, 1)
        : _stepLength <= 0 ? 1 : Math.Clamp(_stepElapsed / _stepLength, 0, 1);

    // Paramètres touchés par la scène (union de toutes ses étapes) et leur contribution courante.
    public int[] Parameters { get; private set; }

    public double[] StartValue { get; private set; }

    public double[] StartWeight { get; private set; }

    public double[] TargetValue { get; private set; }

    public double[] TargetWeight { get; private set; }

    public double[] Value { get; private set; }

    public double[] Weight { get; private set; }

    public double[] FadeSeconds { get; private set; }

    public double[] DelaySeconds { get; private set; }

    /// <summary>Paramètre repris par une scène plus récente de la même couche (fondu croisé) : ne contribue plus.</summary>
    public bool[] Masked { get; private set; }

    public bool[] Discrete { get; private set; }

    /// <summary>Graine aléatoire de la session (MOT-004), combinée à celle de chaque effet.</summary>
    public ulong SessionSeed { get; init; }

    // Effets (EFF-001, MOT-060) : un par identifiant (un même effet dans deux étapes successives continue sans à-coup),
    // avec sa phase et son poids ; chaque étape en fournit une définition (« instance »).
    private Guid[] _effectIds = [];
    private double[] _effectPhase = [];
    private double[] _effectStartWeight = [];
    private double[] _effectTargetWeight = [];
    private double[] _effectWeight = [];
    private int[] _effectCurrent = [];
    private double _effectFade;

    // Instances : effet unique, définition, et plage de ses canaux dans les tableaux aplatis.
    private int[] _instanceEffect = [];
    private EngineEffect[] _instanceDefinition = [];
    private int[] _instanceFirst = [];
    private int[] _instanceEnd = [];
    private int[][] _stepInstances = [];
    private EffectChannel[] _channels = [];
    private int[] _channelSlot = [];

    /// <summary>Contribution des effets absolus par paramètre, recalculée à chaque tick.</summary>
    public double[] EffectValue { get; private set; } = [];

    /// <summary>Poids combiné des effets absolus par paramètre (0 = aucun).</summary>
    public double[] EffectWeight { get; private set; } = [];

    /// <summary>Somme des écarts des effets relatifs par paramètre (MOT-061, EFF-009).</summary>
    public double[] EffectOffset { get; private set; } = [];

    /// <summary>Un effet relatif touche ce paramètre.</summary>
    public bool[] EffectRelative { get; private set; } = [];

    /// <summary>La scène a au moins un effet : la fusion doit lire les tableaux d'effets.</summary>
    public bool HasEffects => _effectIds.Length > 0;

    // Fondu par la teinte (MOT-054) : triplets de rangs (rouge, vert, bleu) dans Parameters.
    private int[][] _hueGroups = [];
    private double[] _progress = [];

    // Horloge propre de la scène (MOT-020), avance d'étape sur événement (MOT-017), suivi des sauts de phase (MOT-062).
    private MusicalClock? _own;
    private double _seenShift;
    private int _eventCount;
    private double _eventProgress;

    private double _stepElapsed;
    private double _stepLength;
    private double _stepBpm = 120;
    private double _stepFade;
    private bool _fadeMusical;
    private bool _holdMusical;
    private double _transitionElapsed;
    private double _transitionEnd;
    private FadeCurve _curve;
    private DiscreteSwitch _switch;
    private int _direction = 1;
    private int _passes;
    private double _exitElapsed;
    private double _exitDuration;
    private bool _exitStartsNow;
    private bool _stepStartsNow;

    /// <summary>Tolérance sur les comparaisons de temps : la somme de pas de 25 ms n'est pas exacte en virgule flottante.</summary>
    private const double TimeEpsilon = 1e-6;

    /// <summary>
    /// Prépare les tableaux pour la scène sur le modèle donné (lancement, ou rechargement du modèle en cours de lecture).
    /// Les contributions déjà présentes pour un même attribut d'appareil sont conservées.
    /// </summary>
    public void Bind(EngineScene scene, ShowModel model, ShowModel? previousModel = null)
    {
        var set = new SortedSet<int>();
        foreach (var step in scene.Steps)
        {
            foreach (var stepValue in step.Values)
            {
                if (stepValue.Parameter >= 0 && stepValue.Parameter < model.Parameters.Count)
                {
                    set.Add(stepValue.Parameter);
                }
            }
        }

        foreach (var step in scene.Steps)
        {
            foreach (var effect in step.Effects)
            {
                foreach (var channel in effect.Channels)
                {
                    if (channel.Parameter >= 0 && channel.Parameter < model.Parameters.Count)
                    {
                        set.Add(channel.Parameter);
                    }
                }
            }
        }

        var parameters = set.ToArray();
        var count = parameters.Length;
        var startValue = new double[count];
        var startWeight = new double[count];
        var targetValue = new double[count];
        var targetWeight = new double[count];
        var value = new double[count];
        var weight = new double[count];
        var fade = new double[count];
        var delay = new double[count];
        var masked = new bool[count];
        var discrete = new bool[count];

        for (var i = 0; i < count; i++)
        {
            discrete[i] = model.Parameters[parameters[i]].Discrete;
        }

        if (previousModel is not null)
        {
            // Rechargement : on retrouve chaque attribut par son identité (appareil, canal), pas par son indice.
            for (var old = 0; old < Parameters.Length; old++)
            {
                var p = previousModel.Parameters[Parameters[old]];
                var index = Array.BinarySearch(parameters, model.IndexOf(p.FixtureId, p.ChannelKey));
                if (index < 0)
                {
                    continue;
                }

                startValue[index] = StartValue[old];
                startWeight[index] = StartWeight[old];
                targetValue[index] = TargetValue[old];
                targetWeight[index] = TargetWeight[old];
                value[index] = Value[old];
                weight[index] = Weight[old];
                fade[index] = FadeSeconds[old];
                delay[index] = DelaySeconds[old];
                masked[index] = Masked[old];
            }
        }

        // MOT-015 : la vitesse de la scène modifiée pendant qu'elle joue s'applique tout de suite ; sinon, la vitesse réglée
        // en direct (CMD-016) est gardée.
        if (previousModel is not null && Math.Abs(Scene.Speed - scene.Speed) > 1e-9)
        {
            Speed = Math.Clamp(scene.Speed, 0.1, 10);
        }

        Scene = scene;
        UpdateOwnClock();
        Parameters = parameters;
        StartValue = startValue;
        StartWeight = startWeight;
        TargetValue = targetValue;
        TargetWeight = targetWeight;
        Value = value;
        Weight = weight;
        FadeSeconds = fade;
        DelaySeconds = delay;
        Masked = masked;
        Discrete = discrete;
        _progress = new double[count];
        BindEffects(scene, model);
        BindHueGroups(model);

        _stepSlots = new int[scene.Steps.Count][];
        for (var s = 0; s < scene.Steps.Count; s++)
        {
            var slots = new int[count];
            Array.Fill(slots, -1);
            var values = scene.Steps[s].Values;
            for (var v = 0; v < values.Count; v++)
            {
                var index = Array.BinarySearch(parameters, values[v].Parameter);
                if (index >= 0)
                {
                    slots[index] = v;
                }
            }

            _stepSlots[s] = slots;
        }

        StepIndex = scene.Steps.Count == 0 ? 0 : Math.Clamp(StepIndex, 0, scene.Steps.Count - 1);
        if (previousModel is not null && scene.Steps.Count > 0)
        {
            Retarget();
        }
    }

    private void BindEffects(EngineScene scene, ShowModel model)
    {
        var ids = new List<Guid>();
        var instanceEffect = new List<int>();
        var definitions = new List<EngineEffect>();
        var first = new List<int>();
        var end = new List<int>();
        var channels = new List<EffectChannel>();
        var slots = new List<int>();
        var stepInstances = new int[scene.Steps.Count][];
        for (var s = 0; s < scene.Steps.Count; s++)
        {
            var list = new List<int>();
            foreach (var effect in scene.Steps[s].Effects)
            {
                var e = ids.IndexOf(effect.Id);
                if (e < 0)
                {
                    e = ids.Count;
                    ids.Add(effect.Id);
                }

                if (list.Exists(i => instanceEffect[i] == e))
                {
                    continue;
                }

                list.Add(definitions.Count);
                instanceEffect.Add(e);
                definitions.Add(effect);
                first.Add(channels.Count);
                foreach (var channel in effect.Channels)
                {
                    var slot = channel.Parameter >= 0 && channel.Parameter < model.Parameters.Count
                        ? Array.BinarySearch(Parameters, channel.Parameter)
                        : -1;
                    if (slot >= 0)
                    {
                        channels.Add(channel);
                        slots.Add(slot);
                    }
                }

                end.Add(channels.Count);
            }

            stepInstances[s] = [.. list];
        }

        // Rechargement : la phase et le poids de chaque effet sont retrouvés par son identifiant.
        var effectCount = ids.Count;
        var phase = new double[effectCount];
        var startWeight = new double[effectCount];
        var targetWeight = new double[effectCount];
        var weight = new double[effectCount];
        var current = new int[effectCount];
        Array.Fill(current, -1);
        for (var e = 0; e < effectCount; e++)
        {
            var old = Array.IndexOf(_effectIds, ids[e]);
            if (old >= 0)
            {
                phase[e] = _effectPhase[old];
                startWeight[e] = _effectStartWeight[old];
                targetWeight[e] = _effectTargetWeight[old];
                weight[e] = _effectWeight[old];
            }
        }

        _effectIds = [.. ids];
        _effectPhase = phase;
        _effectStartWeight = startWeight;
        _effectTargetWeight = targetWeight;
        _effectWeight = weight;
        _effectCurrent = current;
        _instanceEffect = [.. instanceEffect];
        _instanceDefinition = [.. definitions];
        _instanceFirst = [.. first];
        _instanceEnd = [.. end];
        _stepInstances = stepInstances;
        _channels = [.. channels];
        _channelSlot = [.. slots];
        var count = Parameters.Length;
        EffectValue = new double[count];
        EffectWeight = new double[count];
        EffectOffset = new double[count];
        EffectRelative = new bool[count];

        // Chaque effet prend la définition de l'étape courante ; à défaut (effet qui sort), la plus récente avant elle.
        var stepCount = scene.Steps.Count;
        if (stepCount > 0)
        {
            var step = Math.Clamp(StepIndex, 0, stepCount - 1);
            for (var s = 1; s <= stepCount; s++)
            {
                foreach (var instance in _stepInstances[(step + s) % stepCount])
                {
                    _effectCurrent[_instanceEffect[instance]] = instance;
                }
            }
        }
    }

    private void BindHueGroups(ShowModel model)
    {
        var groups = new List<int[]>();
        foreach (var group in model.ColorGroups)
        {
            var r = Array.BinarySearch(Parameters, group.Red);
            var g = Array.BinarySearch(Parameters, group.Green);
            var b = Array.BinarySearch(Parameters, group.Blue);
            if (r >= 0 && g >= 0 && b >= 0)
            {
                groups.Add([r, g, b]);
            }
        }

        _hueGroups = [.. groups];
    }

    /// <summary>
    /// Après un rechargement (palette ou scène modifiée, PAL-005) : l'étape courante vise ses nouvelles valeurs.
    /// Une transition terminée prend la nouvelle valeur tout de suite ; une transition en cours continue vers elle.
    /// </summary>
    private void Retarget()
    {
        var done = _transitionElapsed + TimeEpsilon >= _transitionEnd;
        for (var e = 0; e < _effectIds.Length; e++)
        {
            var present = false;
            foreach (var instance in _stepInstances[StepIndex])
            {
                present |= _instanceEffect[instance] == e;
            }

            _effectTargetWeight[e] = present ? 1 : 0;
            if (done)
            {
                _effectWeight[e] = _effectStartWeight[e] = _effectTargetWeight[e];
            }
        }

        var step = Scene.Steps[StepIndex];
        var slots = _stepSlots[StepIndex];
        for (var i = 0; i < Parameters.Length; i++)
        {
            var slot = slots[i];
            if (slot >= 0)
            {
                var target = step.Values[slot].Value;
                TargetValue[i] = target;
                TargetWeight[i] = 1;
                if (done || StartWeight[i] <= 0)
                {
                    StartValue[i] = target;
                }

                if (done)
                {
                    Value[i] = target;
                    Weight[i] = 1;
                    StartWeight[i] = 1;
                }
            }
            else
            {
                TargetValue[i] = Value[i];
                TargetWeight[i] = 0;
                if (done)
                {
                    Weight[i] = 0;
                    StartWeight[i] = 0;
                }
            }
        }
    }

    /// <summary>Reprend la contribution d'une lecture plus ancienne de la couche, paramètre par paramètre (fondu croisé, MOT-030).</summary>
    public void TakeOverFrom(Playback older)
    {
        for (var i = 0; i < Parameters.Length; i++)
        {
            if (Weight[i] > 0)
            {
                continue;
            }

            var index = Array.BinarySearch(older.Parameters, Parameters[i]);
            if (index < 0 || older.Masked[index] || older.Weight[index] * older.ExitWeight <= 0)
            {
                continue;
            }

            Value[i] = older.Value[index];
            Weight[i] = older.Weight[index] * older.ExitWeight;
            older.Masked[index] = true;
        }
    }

    /// <summary>Démarre la lecture sur la première étape.</summary>
    /// <param name="entryFade">Fondu d'entrée imposé (commande, fondu croisé de la couche, fondu d'entrée de la scène), en secondes.</param>
    /// <param name="bpm">Tempo courant.</param>
    public void Start(double? entryFade, double bpm)
    {
        bpm = _own?.Bpm ?? bpm;
        _seenShift = (_own ?? Clock).ShiftTotal;
        State = PlaybackState.FadingIn;
        StepIndex = 0;
        _direction = 1;
        _passes = 0;
        Holding = false;
        EnterStep(0, entryFade, bpm);
        UpdateContributions();
        AdvanceEffects(0, bpm);
    }

    /// <summary>
    /// Fige la lecture sur une étape, prise d'emblée (CMD-017, aperçu de l'édition) : l'étape ne change plus, ses effets
    /// tournent.
    /// </summary>
    public void Pin(int index, double bpm)
    {
        bpm = _own?.Bpm ?? bpm;
        _seenShift = (_own ?? Clock).ShiftTotal;
        State = PlaybackState.Running;
        _direction = 1;
        _passes = 0;
        EnterStep(Math.Clamp(index, 0, Scene.Steps.Count - 1), 0, bpm);
        Holding = true;
        UpdateContributions();
        AdvanceEffects(0, bpm);
    }

    /// <summary>Pas à pas manuel (CMD-015) : l'étape visée démarre avec son propre fondu.</summary>
    public void Step(StepDirection direction, double bpm)
    {
        var count = Scene.Steps.Count;
        if (count == 0 || State == PlaybackState.FadingOut)
        {
            return;
        }

        bpm = _own?.Bpm ?? bpm;
        Holding = false;
        var next = direction == StepDirection.Next ? (StepIndex + 1) % count : (StepIndex - 1 + count) % count;
        EnterStep(next, null, bpm);
        UpdateContributions();
        AdvanceEffects(0, bpm);

        // Commande appliquée à l'instant du tick : l'étape part de maintenant, elle n'avance pas encore.
        _stepStartsNow = true;
    }

    /// <summary>Commence le fondu de sortie (durée en secondes, 0 = arrêt immédiat).</summary>
    /// <param name="seconds">Durée du fondu de sortie.</param>
    /// <param name="startsNow">
    /// Demandé à l'instant du tick courant (commande, remplacement) : le fondu ne compte pas encore le temps écoulé
    /// depuis le tick précédent. Faux quand la fin de scène est constatée pendant l'avancement.
    /// </param>
    public void BeginExit(double seconds, bool startsNow = true)
    {
        if (State is PlaybackState.FadingOut or PlaybackState.Done)
        {
            return;
        }

        State = PlaybackState.FadingOut;
        _exitElapsed = 0;
        _exitStartsNow = startsNow;
        _exitDuration = Math.Max(0, seconds);
        if (_exitDuration <= 0)
        {
            ExitWeight = 0;
            State = PlaybackState.Done;
        }
    }

    /// <summary>Arrêt immédiat (retrait au tick courant).</summary>
    public void Kill()
    {
        ExitWeight = 0;
        State = PlaybackState.Done;
    }

    /// <summary>
    /// Fait avancer la lecture de <paramref name="elapsed"/> secondes réelles (GEN-032).
    /// Renvoie l'action de fin de scène à exécuter par le moteur, s'il y en a une.
    /// </summary>
    public PlaybackAdvance Advance(double elapsed, double bpm, Random random, out bool stepChanged)
    {
        stepChanged = false;
        if (State == PlaybackState.Done)
        {
            return PlaybackAdvance.None;
        }

        bpm = _own?.Bpm ?? bpm;

        var result = PlaybackAdvance.None;
        if (State == PlaybackState.FadingOut)
        {
            _exitElapsed += _exitStartsNow ? 0 : elapsed;
            _exitStartsNow = false;
            ExitWeight = _exitElapsed + TimeEpsilon >= _exitDuration ? 0 : 1 - (_exitElapsed / _exitDuration);
            if (ExitWeight <= 0)
            {
                State = PlaybackState.Done;
                return PlaybackAdvance.None;
            }
        }

        // MOT-015 : la vitesse raccourcit ou allonge les durées de la scène (pas le fondu de sortie).
        // SCN-051 : la vitesse peut suivre l'énergie de la musique écoutée (0,6× calme, 1,4× explosif).
        var energyFactor = Scene.EnergySpeed && Events.AudioLive ? 0.6 + (0.8 * Math.Clamp(Events.Energy, 0, 1)) : 1;
        var scaled = _stepStartsNow ? 0 : elapsed * Speed * energyFactor;
        _stepStartsNow = false;
        _own?.Advance(scaled);
        RescaleForTempo(bpm);
        _transitionElapsed += scaled;
        UpdateContributions();
        AdvanceEffects(scaled, bpm);

        if (State == PlaybackState.FadingIn && _transitionElapsed + TimeEpsilon >= _transitionEnd)
        {
            State = PlaybackState.Running;
        }

        if (Holding || Scene.Steps.Count == 0)
        {
            return result;
        }

        _stepElapsed += scaled;
        double carry;
        if (Scene.Advance == StepAdvanceMode.Duration)
        {
            if (_stepElapsed + TimeEpsilon < _stepLength)
            {
                return result;
            }

            // Une seule étape au plus par tick, le reste du temps est reporté (précision des durées, GEN-032).
            carry = Math.Max(0, _stepElapsed - _stepLength);
        }
        else
        {
            // MOT-017 : l'étape dure jusqu'au prochain événement musical (temps, mesure, impulsion), tous les N.
            var every = Math.Max(1, Scene.AdvanceEvery);
            _eventCount += CountEvents();
            _eventProgress = (_eventCount + (scaled > 0 ? (_own ?? Clock).Phase : 0)) / every;
            if (_eventCount < every)
            {
                return result;
            }

            _eventCount = 0;
            carry = 0;
        }
        var next = NextStep(random, out var endReached);
        if (endReached)
        {
            switch (Scene.End)
            {
                case EndMode.Hold:
                    Holding = true;
                    return result;
                case EndMode.Chain:
                    Holding = true;
                    return PlaybackAdvance.Chain;
                default:
                    Holding = true;
                    return PlaybackAdvance.Stop;
            }
        }

        if (next == StepIndex && Scene.Steps.Count == 1)
        {
            // Scène à une étape en boucle : état fixe, rien à rejouer.
            _stepElapsed = 0;
            return result;
        }

        EnterStep(next, null, bpm);
        _stepElapsed = Math.Min(carry, _stepLength);
        _transitionElapsed = _stepElapsed;
        UpdateContributions();
        AdvanceEffects(0, bpm);
        stepChanged = true;
        return result;
    }

    /// <summary>Événements du tick qui comptent pour l'avance d'étape de la scène (MOT-017, SCN-052).</summary>
    private int CountEvents()
    {
        var clock = _own ?? Clock;
        return Scene.Advance switch
        {
            StepAdvanceMode.Beat => clock.BeatsCrossed,
            StepAdvanceMode.Bar => clock.BarsCrossed,
            StepAdvanceMode.BassPulse => Events.AudioLive && _own is null ? Events.BassPulses : clock.BeatsCrossed,
            StepAdvanceMode.TreblePulse => Events.AudioLive && _own is null ? Events.TreblePulses : clock.BeatsCrossed,
            _ => 0,
        };
    }

    private void UpdateOwnClock()
    {
        if (Scene.OwnBpm is { } bpm)
        {
            _own ??= new MusicalClock();
            _own.SetFixed(bpm);
        }
        else
        {
            _own = null;
        }
    }

    /// <summary>Cycles d'un effet en temps musicaux pour une position donnée de l'horloge, en temps (MOT-062).</summary>
    private static double ClockCycle(double beats, Duration period)
    {
        var length = Math.Max(0.02, period.Value * (period.Unit == DurationUnit.Bars ? Duration.BeatsPerBar : 1));
        return beats / length;
    }

    private int NextStep(Random random, out bool endReached)
    {
        endReached = false;
        var count = Scene.Steps.Count;
        switch (Scene.Loop)
        {
            case LoopMode.Infinite:
                return (StepIndex + 1) % count;

            case LoopMode.PingPong:
                if (count == 1)
                {
                    return 0;
                }

                if (StepIndex + _direction >= count || StepIndex + _direction < 0)
                {
                    _direction = -_direction;
                }

                return StepIndex + _direction;

            case LoopMode.Random:
                if (count == 1)
                {
                    return 0;
                }

                // Tirage sans répéter l'étape courante (MOT-013), avec le générateur à graine du moteur (MOT-004).
                var draw = random.Next(count - 1);
                return draw >= StepIndex ? draw + 1 : draw;

            default:
                if (StepIndex + 1 < count)
                {
                    return StepIndex + 1;
                }

                _passes++;
                var total = Scene.Loop == LoopMode.Once ? 1 : Math.Max(1, Scene.LoopCount);
                if (_passes >= total)
                {
                    endReached = true;
                    return StepIndex;
                }

                return 0;
        }
    }

    /// <summary>
    /// MOT-016 : quand le tempo change en cours d'étape, la part musicale de la durée garde sa proportion :
    /// 2 temps à 120 BPM, à mi-étape le tempo passe à 60 → il reste 1 s. Les durées en secondes ne bougent pas.
    /// </summary>
    private void RescaleForTempo(double bpm)
    {
        if (Math.Abs(bpm - _stepBpm) < 1e-9 || Scene.Steps.Count == 0)
        {
            return;
        }

        var factor = _stepBpm / bpm;
        var fade = _fadeMusical ? _stepFade * factor : _stepFade;
        var holdOld = _stepLength - _stepFade;
        var hold = _holdMusical ? holdOld * factor : holdOld;
        _stepElapsed = Rescale(_stepElapsed, _stepFade, fade, factor);
        _transitionElapsed = Rescale(_transitionElapsed, _stepFade, fade, factor);
        _transitionEnd = _fadeMusical && _transitionEnd > 0 && Math.Abs(_transitionEnd - _stepFade) < 1e-9 ? fade : _transitionEnd;
        _stepFade = fade;
        _stepLength = fade + hold;
        _stepBpm = bpm;

        double Rescale(double elapsed, double oldFade, double newFade, double k)
        {
            if (elapsed < oldFade)
            {
                return _fadeMusical ? elapsed * k : elapsed;
            }

            return newFade + ((elapsed - oldFade) * (_holdMusical ? k : 1));
        }
    }

    private void EnterStep(int index, double? forcedFade, double bpm)
    {
        StepIndex = index;
        var step = Scene.Steps[index];
        var stepFade = forcedFade ?? step.Fade.ToSeconds(bpm);
        _curve = step.Curve;
        _switch = step.Switch;
        _stepLength = stepFade + step.Hold.ToSeconds(bpm);
        _stepBpm = bpm;
        _eventCount = 0;
        _eventProgress = 0;
        _stepFade = stepFade;
        _fadeMusical = forcedFade is null && step.Fade.Unit != DurationUnit.Seconds;
        _holdMusical = step.Hold.Unit != DurationUnit.Seconds;
        _stepElapsed = 0;
        _transitionElapsed = 0;
        _transitionEnd = stepFade;

        var slots = _stepSlots[index];
        for (var i = 0; i < Parameters.Length; i++)
        {
            StartValue[i] = Value[i];
            StartWeight[i] = Weight[i];
            var slot = slots[i];
            if (slot >= 0)
            {
                var value = step.Values[slot];
                TargetValue[i] = value.Value;
                TargetWeight[i] = 1;
                FadeSeconds[i] = forcedFade ?? value.Fade?.ToSeconds(bpm) ?? stepFade;
                DelaySeconds[i] = value.Delay.ToSeconds(bpm);
                if (StartWeight[i] <= 0)
                {
                    // Pas de contribution avant : la valeur est prise d'emblée, seul le poids monte (fondu depuis le sous-jacent).
                    Value[i] = value.Value;
                    StartValue[i] = value.Value;
                }
            }
            else
            {
                // Attribut absent de l'étape : il garde sa valeur et rend la main (poids → 0).
                TargetValue[i] = Value[i];
                TargetWeight[i] = 0;
                FadeSeconds[i] = stepFade;
                DelaySeconds[i] = 0;
            }

            _transitionEnd = Math.Max(_transitionEnd, DelaySeconds[i] + FadeSeconds[i]);
        }

        // MOT-063 : un effet entre et sort avec le fondu de l'étape (sa taille monte ou descend avec son poids).
        _effectFade = stepFade;
        for (var e = 0; e < _effectIds.Length; e++)
        {
            _effectStartWeight[e] = _effectWeight[e];
            _effectTargetWeight[e] = 0;
        }

        foreach (var instance in _stepInstances[index])
        {
            var e = _instanceEffect[instance];
            _effectTargetWeight[e] = 1;
            _effectCurrent[e] = instance;
            if (_effectWeight[e] <= 0)
            {
                // Un effet qui arrive commence au début de son cycle ; un effet en temps musicaux se cale sur l'horloge (MOT-062).
                var entering = _instanceDefinition[instance].Period;
                _effectPhase[e] = entering.Unit == DurationUnit.Seconds ? 0 : ClockCycle((_own ?? Clock).EffectivePosition, entering);
            }
        }
    }

    /// <summary>
    /// Fait tourner les effets (MOT-060) et calcule leur contribution par paramètre ; <paramref name="scaled"/> tient
    /// compte de la vitesse de la scène (MOT-015). Sans allocation (doc 03 §4.1).
    /// </summary>
    private void AdvanceEffects(double scaled, double bpm)
    {
        if (_effectIds.Length == 0)
        {
            return;
        }

        Array.Clear(EffectWeight);
        Array.Clear(EffectOffset);
        Array.Clear(EffectRelative);
        var clockShift = (_own ?? Clock).ShiftTotal;
        var shift = clockShift - _seenShift;
        _seenShift = clockShift;
        var progress = _effectFade <= 0 ? 1 : Math.Clamp(_transitionElapsed / _effectFade, 0, 1);
        for (var e = 0; e < _effectIds.Length; e++)
        {
            var weight = _effectStartWeight[e] + ((_effectTargetWeight[e] - _effectStartWeight[e]) * progress);
            _effectWeight[e] = weight;
            var instance = _effectCurrent[e];
            if (instance < 0)
            {
                continue;
            }

            var effect = _instanceDefinition[instance];
            var period = Math.Max(0.02, effect.Period.ToSeconds(bpm));
            _effectPhase[e] += scaled / period;
            if (shift != 0 && effect.Period.Unit != DurationUnit.Seconds)
            {
                // MOT-062 : un recalage de l'horloge (tap, « 1 ici », latence) déplace aussi le cycle de l'effet.
                _effectPhase[e] += ClockCycle(shift, effect.Period);
            }

            if (weight <= 0)
            {
                continue;
            }

            var seed = SessionSeed ^ effect.Seed;
            for (var k = _instanceFirst[instance]; k < _instanceEnd[instance]; k++)
            {
                var channel = _channels[k];
                var slot = _channelSlot[k];
                var cycles = EffectShapes.Directed(effect.Direction, _effectPhase[e]) - channel.Lag;
                if (effect.Shape == EffectShape.Table)
                {
                    var tableValue = channel.Table is { } table
                        ? EffectShapes.Sample(table, cycles, effect.Stepped || Discrete[slot])
                        : channel.Center;
                    AddAbsolute(slot, tableValue, weight);
                    continue;
                }

                var offset = channel.Size * EffectShapes.Offset(effect.Shape, cycles, channel.Axis, effect.DutyCycle, seed + (ulong)channel.Member);
                if (effect.Relative)
                {
                    // EFF-009 : les effets relatifs d'un même attribut s'additionnent.
                    EffectOffset[slot] += offset * weight;
                    EffectRelative[slot] = true;
                }
                else
                {
                    AddAbsolute(slot, channel.Center + offset, weight);
                }
            }
        }
    }

    private void AddAbsolute(int slot, double value, double weight)
    {
        var current = EffectWeight[slot];
        if (current <= 0)
        {
            EffectValue[slot] = value;
            EffectWeight[slot] = weight;
            return;
        }

        // Deux effets absolus sur un même attribut : le suivant l'emporte selon son poids.
        EffectValue[slot] += (value - EffectValue[slot]) * weight;
        EffectWeight[slot] = current + ((1 - current) * weight);
    }

    /// <summary>
    /// Valeur et poids d'un paramètre de la scène, effets compris (MOT-061) : un effet absolu remplace la valeur de
    /// l'étape selon son poids ; un effet relatif s'ajoute à la valeur de l'étape, sinon à la valeur sous-jacente.
    /// </summary>
    /// <param name="slot">Rang du paramètre dans <see cref="Parameters"/>.</param>
    /// <param name="underlying">Valeur sous-jacente (couches inférieures) au moment de la fusion.</param>
    /// <param name="value">Valeur de la scène (entrée : sans effet).</param>
    /// <param name="weight">Poids de la scène (entrée : sans effet).</param>
    public void ApplyEffects(int slot, double underlying, ref double value, ref double weight)
    {
        var effectWeight = EffectWeight[slot];
        if (effectWeight > 0)
        {
            var effectValue = EffectValue[slot];
            if (weight <= 0)
            {
                value = effectValue;
                weight = effectWeight;
            }
            else if (Discrete[slot])
            {
                value = effectWeight >= 0.5 ? effectValue : value;
                weight = Math.Max(weight, effectWeight);
            }
            else
            {
                value += (effectValue - value) * effectWeight;
                weight += (1 - weight) * effectWeight;
            }
        }

        if (EffectRelative[slot])
        {
            if (weight <= 0)
            {
                value = underlying;
                weight = 1;
            }

            value += EffectOffset[slot];
        }

        value = Math.Clamp(value, 0, 1);
    }

    private void UpdateContributions()
    {
        for (var i = 0; i < Parameters.Length; i++)
        {
            var t = _transitionElapsed - DelaySeconds[i];
            var fade = FadeSeconds[i];
            double progress;
            if (t < 0)
            {
                progress = 0;
            }
            else if (fade <= 0 || _curve == FadeCurve.Instant)
            {
                progress = 1;
            }
            else
            {
                progress = t + TimeEpsilon >= fade ? 1 : t / fade;
                if (_curve == FadeCurve.SCurve)
                {
                    progress = progress * progress * (3 - (2 * progress));
                }
            }

            if (Discrete[i])
            {
                // MOT-012 : un attribut discret n'est jamais interpolé, il bascule franchement.
                var threshold = _switch switch
                {
                    DiscreteSwitch.Middle => 0.5,
                    DiscreteSwitch.End => 1.0,
                    _ => 0.0,
                };
                var switched = t >= 0 && (threshold <= 0 || progress >= threshold);
                Value[i] = switched ? TargetValue[i] : StartValue[i];
                Weight[i] = switched ? TargetWeight[i] : StartWeight[i];
                continue;
            }

            Weight[i] = StartWeight[i] + ((TargetWeight[i] - StartWeight[i]) * progress);
            Value[i] = StartValue[i] + ((TargetValue[i] - StartValue[i]) * progress);
            _progress[i] = progress;
        }

        if (_hueGroups.Length > 0 && Scene.Steps.Count > 0 && Scene.Steps[StepIndex].HueFade)
        {
            FadeByHue();
        }
    }

    /// <summary>
    /// MOT-054 : pendant un fondu, les émetteurs rouge, vert et bleu d'une cellule suivent la roue des teintes
    /// (plus court chemin) au lieu d'une ligne droite qui passe par des teintes « sales ».
    /// </summary>
    private void FadeByHue()
    {
        foreach (var group in _hueGroups)
        {
            int r = group[0], g = group[1], b = group[2];
            var progress = _progress[r];
            if (progress <= 0 || progress >= 1 || StartWeight[r] <= 0 || TargetWeight[r] <= 0)
            {
                continue;
            }

            var (h1, s1, v1) = ToHsv(StartValue[r], StartValue[g], StartValue[b]);
            var (h2, s2, v2) = ToHsv(TargetValue[r], TargetValue[g], TargetValue[b]);

            // Une couleur sans teinte (blanc, gris, noir) prend celle de l'autre extrémité.
            if (s1 < 1e-3)
            {
                h1 = h2;
            }
            else if (s2 < 1e-3)
            {
                h2 = h1;
            }

            var delta = h2 - h1;
            if (delta > 0.5)
            {
                delta -= 1;
            }
            else if (delta < -0.5)
            {
                delta += 1;
            }

            var h = h1 + (delta * progress);
            h -= Math.Floor(h);
            var (red, green, blue) = FromHsv(h, s1 + ((s2 - s1) * progress), v1 + ((v2 - v1) * progress));
            Value[r] = red;
            Value[g] = green;
            Value[b] = blue;
        }
    }

    private static (double H, double S, double V) ToHsv(double r, double g, double b)
    {
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        double h = 0;
        if (delta > 1e-9)
        {
            h = max == r ? ((g - b) / delta) % 6 : max == g ? ((b - r) / delta) + 2 : ((r - g) / delta) + 4;
            h /= 6;
            if (h < 0)
            {
                h += 1;
            }
        }

        return (h, max <= 0 ? 0 : delta / max, max);
    }

    private static (double R, double G, double B) FromHsv(double h, double s, double v)
    {
        var sector = h * 6;
        var i = (int)Math.Floor(sector) % 6;
        var f = sector - Math.Floor(sector);
        var p = v * (1 - s);
        var q = v * (1 - (s * f));
        var t = v * (1 - (s * (1 - f)));
        return i switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q),
        };
    }
}
