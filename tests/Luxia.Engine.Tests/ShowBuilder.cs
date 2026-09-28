using Luxia.Engine.Model;

namespace Luxia.Engine.Tests;

/// <summary>Appareil de test : ses paramètres par clé de canal.</summary>
internal sealed record TestFixture(Guid Id, IReadOnlyDictionary<string, int> Parameters)
{
    public int this[string key] => Parameters[key];
}

/// <summary>Construit un <see cref="ShowModel"/> de test avec des appareils proches du parc réel.</summary>
internal sealed class ShowBuilder
{
    private readonly List<RigParameter> _parameters = [];
    private readonly List<EngineLayer> _layers = [];
    private readonly List<EngineScene> _scenes = [];
    private readonly Dictionary<Guid, Guid> _aliases = [];

    public int Add(RigParameter parameter)
    {
        _parameters.Add(parameter);
        return _parameters.Count - 1;
    }

    /// <summary>PAR 7 canaux (LPC008S) : gradateur, R, V, B, strobe, sélecteur de fonction (discret), vitesse.</summary>
    public TestFixture Par7(int address, string name = "PAR", bool absent = false)
    {
        var id = Guid.NewGuid();
        var map = new Dictionary<string, int>
        {
            ["dim"] = Add(P(id, name, "dim", address, ParameterRole.Intensity, absent: absent)),
            ["r"] = Add(P(id, name, "r", address + 1, ParameterRole.Emitter, absent: absent)),
            ["g"] = Add(P(id, name, "g", address + 2, ParameterRole.Emitter, absent: absent)),
            ["b"] = Add(P(id, name, "b", address + 3, ParameterRole.Emitter, absent: absent)),
            ["strobe"] = Add(P(id, name, "strobe", address + 4, ParameterRole.Other, absent: absent)),
            ["fn"] = Add(P(id, name, "fn", address + 5, ParameterRole.Other, discrete: true, absent: absent)),
        };
        return new TestFixture(id, map);
    }

    /// <summary>PAR 3 canaux sans gradateur : intensité virtuelle, R, V, B qui la suivent (BIB-006, MOT-040).</summary>
    public TestFixture Par3(int address, string name = "PAR 3CH")
    {
        var id = Guid.NewGuid();
        var dimmer = Add(new RigParameter { FixtureId = id, ChannelKey = RigParameter.VirtualIntensityKey, Label = name + " – Intensité", Role = ParameterRole.Intensity });
        var map = new Dictionary<string, int>
        {
            [RigParameter.VirtualIntensityKey] = dimmer,
            ["r"] = Add(P(id, name, "r", address, ParameterRole.Emitter) with { IntensitySource = dimmer }),
            ["g"] = Add(P(id, name, "g", address + 1, ParameterRole.Emitter) with { IntensitySource = dimmer }),
            ["b"] = Add(P(id, name, "b", address + 2, ParameterRole.Emitter) with { IntensitySource = dimmer }),
        };
        return new TestFixture(id, map);
    }

    /// <summary>Lyre : Pan et Tilt 16 bits (défaut au centre), roue de couleur (discrète), gradateur.</summary>
    public TestFixture Lyre(int address, string name = "Lyre", bool invertPan = false)
    {
        var id = Guid.NewGuid();
        var map = new Dictionary<string, int>
        {
            ["pan"] = Add(new RigParameter
            {
                FixtureId = id,
                ChannelKey = "pan",
                Label = name + " – pan",
                Default = 0.5,
                Inverted = invertPan,
                Outputs = [new ChannelAddress(1, address, address + 1)],
            }),
            ["tilt"] = Add(new RigParameter
            {
                FixtureId = id,
                ChannelKey = "tilt",
                Label = name + " – tilt",
                Default = 0.5,
                Outputs = [new ChannelAddress(1, address + 2, address + 3)],
            }),
            ["color"] = Add(P(id, name, "color", address + 4, ParameterRole.Other, discrete: true)),
            ["dim"] = Add(P(id, name, "dim", address + 5, ParameterRole.Intensity)),
        };
        return new TestFixture(id, map);
    }

    public void Twin(TestFixture twin, TestFixture reference) => _aliases[twin.Id] = reference.Id;

    public EngineLayer Layer(
        string name,
        int priority,
        bool exclusive = true,
        IntensityMode mode = IntensityMode.Htp,
        double master = 1,
        bool masterOnAll = false,
        double crossFade = 0)
    {
        var layer = new EngineLayer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Priority = priority,
            Exclusive = exclusive,
            IntensityMode = mode,
            Master = master,
            MasterOnAllAttributes = masterOnAll,
            CrossFade = Duration.FromSeconds(crossFade),
        };
        _layers.Add(layer);
        return layer;
    }

    /// <summary>Remplace une couche (même identifiant) par une version modifiée.</summary>
    public EngineLayer Update(EngineLayer layer)
    {
        _layers[_layers.FindIndex(l => l.Id == layer.Id)] = layer;
        return layer;
    }

    public EngineScene Scene(string name, EngineLayer layer, params EngineStep[] steps) =>
        Scene(new EngineScene { Id = Guid.NewGuid(), Name = name, LayerId = layer.Id, Steps = steps });

    public EngineScene Scene(EngineScene scene)
    {
        _scenes.RemoveAll(s => s.Id == scene.Id);
        _scenes.Add(scene);
        return scene;
    }

    /// <summary>Limites de sûreté du modèle (aucune par défaut).</summary>
    public SafetyModel Safety { get; set; } = SafetyModel.None;

    /// <summary>Triplets rouge / vert / bleu (fondu par la teinte, MOT-054).</summary>
    public List<ColorGroup> ColorGroups { get; } = [];

    public ShowModel Build() => new([.. _parameters], [.. _layers], [.. _scenes], new Dictionary<Guid, Guid>(_aliases), Safety, [.. ColorGroups]);

    public static EngineStep Step(double fade, double hold, params StepValue[] values) =>
        new() { Fade = Duration.FromSeconds(fade), Hold = Duration.FromSeconds(hold), Values = values };

    public static StepValue V(int parameter, double value) => new(parameter, value);

    private static RigParameter P(Guid id, string name, string key, int channel, ParameterRole role, bool discrete = false, bool absent = false) => new()
    {
        FixtureId = id,
        ChannelKey = key,
        Label = $"{name} – {key}",
        Role = role,
        Discrete = discrete,
        Absent = absent,
        Outputs = [new ChannelAddress(1, channel)],
    };
}
