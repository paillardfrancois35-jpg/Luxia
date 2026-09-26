using Luxia.Engine.Model;
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

    public int LayerIndex { get; set; }

    /// <summary>Ordre d'activation : à priorité égale, la plus récente l'emporte (doc 15 §5.2).</summary>
    public long Sequence { get; }

    public CommandOrigin Origin { get; }

    public bool Solo { get; }

    public PlaybackState State { get; private set; } = PlaybackState.FadingIn;

    public int StepIndex { get; private set; }

    public double Speed { get; set; }

    /// <summary>Poids de sortie (1 → 0 pendant le fondu de sortie).</summary>
    public double ExitWeight { get; private set; } = 1;

    /// <summary>Maintenue sur sa dernière étape (EndMode.Hold).</summary>
    public bool Holding { get; private set; }

    /// <summary>Progression 0-1 dans l'étape courante (affichage).</summary>
    public double StepProgress => _stepLength <= 0 ? 1 : Math.Clamp(_stepElapsed / _stepLength, 0, 1);

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

    private double _stepElapsed;
    private double _stepLength;
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

    /// <summary>
    /// Après un rechargement (palette ou scène modifiée, PAL-005) : l'étape courante vise ses nouvelles valeurs.
    /// Une transition terminée prend la nouvelle valeur tout de suite ; une transition en cours continue vers elle.
    /// </summary>
    private void Retarget()
    {
        var done = _transitionElapsed + TimeEpsilon >= _transitionEnd;
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
        State = PlaybackState.FadingIn;
        StepIndex = 0;
        _direction = 1;
        _passes = 0;
        Holding = false;
        EnterStep(0, entryFade, bpm);
        UpdateContributions();
    }

    /// <summary>Pas à pas manuel (CMD-015) : l'étape visée démarre avec son propre fondu.</summary>
    public void Step(StepDirection direction, double bpm)
    {
        var count = Scene.Steps.Count;
        if (count == 0 || State == PlaybackState.FadingOut)
        {
            return;
        }

        Holding = false;
        var next = direction == StepDirection.Next ? (StepIndex + 1) % count : (StepIndex - 1 + count) % count;
        EnterStep(next, null, bpm);
        UpdateContributions();

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
        var scaled = _stepStartsNow ? 0 : elapsed * Speed;
        _stepStartsNow = false;
        _transitionElapsed += scaled;
        UpdateContributions();

        if (State == PlaybackState.FadingIn && _transitionElapsed + TimeEpsilon >= _transitionEnd)
        {
            State = PlaybackState.Running;
        }

        if (Holding || Scene.Steps.Count == 0)
        {
            return result;
        }

        _stepElapsed += scaled;
        if (_stepElapsed + TimeEpsilon < _stepLength)
        {
            return result;
        }

        // Une seule étape au plus par tick, le reste du temps est reporté (précision des durées, GEN-032).
        var carry = Math.Max(0, _stepElapsed - _stepLength);
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
        stepChanged = true;
        return result;
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

    private void EnterStep(int index, double? forcedFade, double bpm)
    {
        StepIndex = index;
        var step = Scene.Steps[index];
        var stepFade = forcedFade ?? step.Fade.ToSeconds(bpm);
        _curve = step.Curve;
        _switch = step.Switch;
        _stepLength = stepFade + step.Hold.ToSeconds(bpm);
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
        }
    }
}
