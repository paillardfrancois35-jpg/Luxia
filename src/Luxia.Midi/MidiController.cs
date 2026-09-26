using System.Globalization;
using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Messaging.Commands;

namespace Luxia.Midi;

/// <summary>
/// Un contrôleur branché (doc 18b) : traduit ses messages en commandes d'origine « MIDI » (MIDI-002) et calcule le retour
/// lumineux de ses LED d'après l'état du moteur (MIDI-003), quelle que soit l'origine des actions. Sans état partagé :
/// deux contrôleurs branchés ensemble ont chacun le leur (MIDI-005).
/// </summary>
/// <remarks>
/// Affectation par défaut (doc 18b §3) : colonnes = couches du Live, lignes = scènes (ligne 1 en haut) ; boutons du bas =
/// arrêter la couche ; Shift + bas 1 / 2 = page de scènes précédente / suivante, Shift + bas 3 / 4 = couches précédentes
/// / suivantes ; boutons de droite = Blackout, Flash, Strobe, Fumée, (Tap, P7), Figer, (Auto, P10), Tout arrêter ;
/// faders 1-8 = masters des couches affichées, fader 9 = Grand Master.
/// </remarks>
public sealed class MidiController
{
    private readonly SoftTakeover[] _faders = [.. Enumerable.Range(0, 9).Select(_ => new SoftTakeover())];
    private readonly Dictionary<int, MidiMessage> _leds = [];
    private readonly HashSet<(MidiAction Action, Guid? Id)> _held = [];
    private bool _shift;

    /// <summary>Crée le contrôleur.</summary>
    public MidiController(ControllerProfile profile, string portName)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(portName);
        Profile = profile;
        PortName = portName;
    }

    /// <summary>Profil du modèle.</summary>
    public ControllerProfile Profile { get; }

    /// <summary>Port MIDI d'entrée.</summary>
    public string PortName { get; }

    /// <summary>Page de scènes (8 lignes par page).</summary>
    public int ScenePage { get; private set; }

    /// <summary>Page de couches (8 colonnes par page).</summary>
    public int LayerPage { get; private set; }

    /// <summary>Traduit un message en commandes (vide si le message n'a pas d'effet).</summary>
    public IReadOnlyList<Command> Handle(MidiMessage message, MidiLayout layout, EngineSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (message.IsControlChange)
        {
            var fader = IndexOf(Profile.Faders, message.Data1);
            return fader < 0 ? [] : Fader(new MidiControl(MidiControlKind.Fader, fader + 1), message.Data2 / 127.0, layout, snapshot);
        }

        if (!message.IsNoteOn && !message.IsNoteOff)
        {
            return [];
        }

        var pressed = message.IsNoteOn;
        if (message.Data1 == Profile.ShiftNote)
        {
            _shift = pressed;
            return [];
        }

        if (ControlOf(message.Data1) is not { } control)
        {
            return [];
        }

        if (control.Kind == MidiControlKind.Bottom && _shift && Binding(control, layout) is null)
        {
            if (pressed)
            {
                Page(control.X, layout);
            }

            return [];
        }

        var (action, id) = ActionOf(control, layout);
        return Button(action, id, pressed, layout, snapshot);
    }

    /// <summary>
    /// Messages des LED qui ont changé depuis le dernier appel (MIDI-003) : scènes (disponible, active, en fondu),
    /// couches qui jouent, blackout, figé, fumée, boutons maintenus.
    /// </summary>
    public IReadOnlyList<MidiMessage> Leds(MidiLayout layout, EngineSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(snapshot);
        var changes = new List<MidiMessage>();
        void Set(int note, MidiMessage led)
        {
            if (!_leds.TryGetValue(note, out var previous) || previous != led)
            {
                _leds[note] = led;
                changes.Add(led);
            }
        }

        for (var index = 0; index < 64; index++)
        {
            var note = Profile.GridBottomLeftNote + index;
            var control = ControlOf(note)!.Value;
            var (action, id) = ActionOf(control, layout);
            Set(note, PadLed(note, action, id, layout, snapshot));
        }

        for (var i = 0; i < Profile.BottomButtons.Count; i++)
        {
            var (action, id) = ActionOf(new MidiControl(MidiControlKind.Bottom, i + 1), layout);
            var on = action == MidiAction.StopLayer && id is { } layer && snapshot.Playbacks.Any(p => p.LayerId == layer && !p.Flash && IsPlaying(p));
            Set(Profile.BottomButtons[i], ButtonLed(Profile.BottomButtons[i], on));
        }

        for (var i = 0; i < Profile.RightButtons.Count; i++)
        {
            var (action, id) = ActionOf(new MidiControl(MidiControlKind.Right, i + 1), layout);
            var on = action switch
            {
                MidiAction.Blackout => snapshot.Blackout,
                MidiAction.Freeze => snapshot.Frozen,
                MidiAction.Smoke => snapshot.Smoking || _held.Contains((action, id)),
                MidiAction.Flash or MidiAction.Strobe or MidiAction.FlashScene => _held.Contains((action, id)),
                _ => false,
            };
            Set(Profile.RightButtons[i], ButtonLed(Profile.RightButtons[i], on));
        }

        return changes;
    }

    /// <summary>Oublie l'état des LED : le prochain appel à <see cref="Leds"/> les renvoie toutes (rebranchement, MIDI-006).</summary>
    public void ResetLeds() => _leds.Clear();

    /// <summary>Messages qui éteignent toutes les LED (fermeture de l'application).</summary>
    public IReadOnlyList<MidiMessage> AllOff() =>
        [.. Enumerable.Range(Profile.GridBottomLeftNote, 64).Concat(Profile.BottomButtons).Concat(Profile.RightButtons).Select(n => MidiMessage.NoteOn(0, n, 0))];

    /// <summary>Indice de palette le plus proche d'une couleur, luminosité mise de côté (MIDI-010).</summary>
    internal static int NearestPaletteIndex(IReadOnlyList<PaletteColor> palette, string color)
    {
        if (palette.Count == 0)
        {
            return 3;
        }

        var (r, g, b) = Normalized(color);
        return palette.MinBy(p =>
        {
            var (pr, pg, pb) = Normalized(p.Color);
            return ((pr - r) * (pr - r)) + ((pg - g) * (pg - g)) + ((pb - b) * (pb - b));
        })!.Index;
    }

    private IReadOnlyList<Command> Button(MidiAction action, Guid? id, bool pressed, MidiLayout layout, EngineSnapshot snapshot)
    {
        if (pressed)
        {
            _held.Add((action, id));
        }
        else
        {
            _held.Remove((action, id));
        }

        const CommandOrigin Midi = CommandOrigin.Midi;
        switch (action)
        {
            case MidiAction.LaunchScene when pressed && id is { } scene:
                var playing = snapshot.Playbacks.Any(p => p.SceneId == scene && !p.Flash && IsPlaying(p));
                return playing && !layout.ActiveClickRestarts
                    ? [new StopSceneCommand(Midi, scene)]
                    : [new LaunchSceneCommand(Midi, scene)];
            case MidiAction.FlashScene when id is { } flash:
                return [new FlashSceneCommand(Midi, flash, pressed)];
            case MidiAction.StopLayer when pressed && id is { } layer:
                return [new StopLayerCommand(Midi, layer)];
            case MidiAction.Blackout when pressed:
                return [new BlackoutCommand(Midi, !snapshot.Blackout)];
            case MidiAction.Flash when layout.FlashSceneId is { } flashScene:
                return [new FlashSceneCommand(Midi, flashScene, pressed)];
            case MidiAction.Strobe when layout.StrobeSceneId is { } strobeScene:
                return [new FlashSceneCommand(Midi, strobeScene, pressed)];
            case MidiAction.Smoke when snapshot.Show.Safety.SmokeChannels.Count > 0:
                return [new SmokeCommand(Midi, pressed)];
            case MidiAction.SmokeBurst when pressed && snapshot.Show.Safety.SmokeChannels.Count > 0:
                return [new SmokeCommand(Midi, false, TimeSpan.FromSeconds(Math.Clamp(layout.SmokeBurstSeconds, 0.5, 60)))];
            case MidiAction.Freeze when pressed:
                return [new FreezeCommand(Midi, !snapshot.Frozen)];
            case MidiAction.StopAll when pressed:
                return [new StopLayerCommand(Midi)];
            default:
                return [];
        }
    }

    private IReadOnlyList<Command> Fader(MidiControl control, double value, MidiLayout layout, EngineSnapshot snapshot)
    {
        var (action, id) = ActionOf(control, layout);
        var takeover = _faders[Math.Clamp(control.X - 1, 0, _faders.Length - 1)];
        switch (action)
        {
            case MidiAction.GrandMaster:
                return takeover.Move(value, snapshot.GrandMaster) is { } level ? [new SetGrandMasterCommand(CommandOrigin.Midi, level)] : [];
            case MidiAction.LayerMaster when id is { } layer:
                var index = IndexOfLayer(snapshot.Show, layer);
                var current = index >= 0 && index < snapshot.LayerMasters.Length ? snapshot.LayerMasters[index] : 1;
                return takeover.Move(value, current) is { } master ? [new SetLayerMasterCommand(CommandOrigin.Midi, layer, master)] : [];
            default:
                return [];
        }
    }

    private void Page(int button, MidiLayout layout)
    {
        var maxScenes = layout.Columns.Select(c => c.Scenes.Count).DefaultIfEmpty(0).Max();
        var scenePages = Math.Max(1, (maxScenes + 7) / 8);
        var layerPages = Math.Max(1, (layout.Columns.Count + 7) / 8);
        switch (button)
        {
            case 1:
                ScenePage = Math.Max(0, ScenePage - 1);
                break;
            case 2:
                ScenePage = Math.Min(scenePages - 1, ScenePage + 1);
                break;
            case 3:
                LayerPage = Math.Max(0, LayerPage - 1);
                break;
            case 4:
                LayerPage = Math.Min(layerPages - 1, LayerPage + 1);
                break;
        }
    }

    /// <summary>Action d'un contrôle : affectation modifiée (MIDI-007), sinon affectation par défaut (doc 18b §3).</summary>
    private (MidiAction Action, Guid? Id) ActionOf(MidiControl control, MidiLayout layout)
    {
        if (Binding(control, layout) is { } binding)
        {
            return (binding.Action, binding.SceneId ?? binding.LayerId);
        }

        switch (control.Kind)
        {
            case MidiControlKind.Pad:
                var column = ColumnAt(control.X, layout);
                var row = (ScenePage * 8) + control.Y - 1;
                if (column is null || row >= column.Scenes.Count)
                {
                    return (MidiAction.None, null);
                }

                return (column.IsFlash ? MidiAction.FlashScene : MidiAction.LaunchScene, column.Scenes[row].SceneId);
            case MidiControlKind.Bottom:
                return ColumnAt(control.X, layout) is { } stop ? (MidiAction.StopLayer, stop.LayerId) : (MidiAction.None, null);
            case MidiControlKind.Fader when control.X == 9:
                return (MidiAction.GrandMaster, null);
            case MidiControlKind.Fader:
                return ColumnAt(control.X, layout) is { } master ? (MidiAction.LayerMaster, master.LayerId) : (MidiAction.None, null);
            default:
                return control.X switch
                {
                    1 => (MidiAction.Blackout, null),
                    2 => (MidiAction.Flash, null),
                    3 => (MidiAction.Strobe, null),
                    4 => (MidiAction.Smoke, null),
                    6 => (MidiAction.Freeze, null),
                    8 => (MidiAction.StopAll, null),
                    _ => (MidiAction.None, null),
                };
        }
    }

    private MidiBinding? Binding(MidiControl control, MidiLayout layout) =>
        layout.Bindings.LastOrDefault(b => b.AppliesTo(Profile) && MidiControl.Parse(b.Control) == control);

    private MidiColumn? ColumnAt(int x, MidiLayout layout)
    {
        var index = (LayerPage * 8) + x - 1;
        return index >= 0 && index < layout.Columns.Count ? layout.Columns[index] : null;
    }

    private MidiControl? ControlOf(int note)
    {
        var grid = note - Profile.GridBottomLeftNote;
        if (grid is >= 0 and < 64)
        {
            return new MidiControl(MidiControlKind.Pad, (grid % 8) + 1, 8 - (grid / 8));
        }

        var bottom = IndexOf(Profile.BottomButtons, note);
        if (bottom >= 0)
        {
            return new MidiControl(MidiControlKind.Bottom, bottom + 1);
        }

        var right = IndexOf(Profile.RightButtons, note);
        return right >= 0 ? new MidiControl(MidiControlKind.Right, right + 1) : null;
    }

    private MidiMessage PadLed(int note, MidiAction action, Guid? id, MidiLayout layout, EngineSnapshot snapshot)
    {
        if (action is not (MidiAction.LaunchScene or MidiAction.FlashScene) || id is not { } scene)
        {
            return MidiMessage.NoteOn(0, note, 0);
        }

        PlaybackInfo? playback = null;
        foreach (var p in snapshot.Playbacks)
        {
            if (p.SceneId == scene && IsPlaying(p))
            {
                playback = p;
            }
        }

        var spec = playback is { State: PlaybackState.FadingIn } && !playback.Value.Flash
            ? Profile.Pads.FadingIn
            : playback is not null || _held.Contains((action, id)) ? Profile.Pads.Active : Profile.Pads.Available;
        if (!Profile.Pads.Rgb)
        {
            return MidiMessage.NoteOn(spec.Channel, note, spec.Velocity);
        }

        var color = layout.Columns.SelectMany(c => c.Scenes).FirstOrDefault(s => s.SceneId == scene)?.Color ?? "#FFFFFF";
        return MidiMessage.NoteOn(spec.Channel, note, NearestPaletteIndex(Profile.Pads.Palette, color));
    }

    private MidiMessage ButtonLed(int note, bool on) => MidiMessage.NoteOn(0, note, on ? Profile.Buttons.On : 0);

    private static bool IsPlaying(PlaybackInfo playback) => playback.State is not (PlaybackState.FadingOut or PlaybackState.Done);

    private static int IndexOf(IReadOnlyList<int> values, int value)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] == value)
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOfLayer(ShowModel show, Guid id)
    {
        for (var i = 0; i < show.Layers.Count; i++)
        {
            if (show.Layers[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private static (double R, double G, double B) Normalized(string hex)
    {
        var text = hex.TrimStart('#');
        if (text.Length != 6 || !int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return (1, 1, 1);
        }

        double r = (rgb >> 16) & 0xFF, g = (rgb >> 8) & 0xFF, b = rgb & 0xFF;
        var max = Math.Max(r, Math.Max(g, b));
        return max <= 0 ? (0, 0, 0) : (r / max, g / max, b / max);
    }
}
