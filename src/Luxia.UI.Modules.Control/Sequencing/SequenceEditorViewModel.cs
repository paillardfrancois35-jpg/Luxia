using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Show.Model;
using Luxia.Show.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control.Sequencing;

/// <summary>
/// Fenêtre d'édition d'une séquence (SHOW-001 à SHOW-008, maquette 11) : une piste par couche plus la piste d'actions, des blocs en
/// mesures posés par glisser-déposer depuis la bibliothèque, déplacés et allongés à la souris, aimantés à la grille ; propriétés de
/// la séquence et du bloc choisi ; essai au métronome ou au tempo du direct.
/// </summary>
public sealed partial class SequenceEditorViewModel : DraftEditorViewModel<Sequence>
{
    private bool _loading;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private decimal _bars = 8;

    [ObservableProperty]
    private bool _loop;

    [ObservableProperty]
    private Choice<ShowQuantize> _quantize = ShowOptions.Quantizes[2];

    [ObservableProperty]
    private Choice<double> _speed = ShowOptions.Speeds[1];

    [ObservableProperty]
    private Choice<double> _snap = ShowOptions.Snaps[0];

    [ObservableProperty]
    private string _search = string.Empty;

    [ObservableProperty]
    private BlockRef? _selected;

    [ObservableProperty]
    private string _selectedTitle = "Aucun bloc choisi";

    [ObservableProperty]
    private Choice<Guid>? _blockScene;

    [ObservableProperty]
    private decimal _blockStartBar = 1;

    [ObservableProperty]
    private decimal _blockStartBeat = 1;

    [ObservableProperty]
    private decimal _blockLength = 1;

    [ObservableProperty]
    private Choice<bool> _blockUnitBars = ShowOptions.LengthUnits[0];

    [ObservableProperty]
    private bool _blockKeep;

    [ObservableProperty]
    private Choice<Guid>? _actionLayer;

    [ObservableProperty]
    private Choice<Guid>? _actionScene;

    [ObservableProperty]
    private decimal? _actionFrom;

    [ObservableProperty]
    private decimal _actionTo = 100;

    /// <summary>Crée l'éditeur.</summary>
    public SequenceEditorViewModel(LuxiaRuntime runtime, IDialogService dialogs, JournalPanelViewModel? journal = null)
        : base(runtime, dialogs, journal)
    {
    }

    /// <inheritdoc />
    public override string Kind => "séquence";

    /// <summary>Pistes affichées : une par couche (par priorité), puis la piste d'actions.</summary>
    public IReadOnlyList<TimelineTrack> Tracks { get; private set; } = [];

    /// <summary>Longueur du brouillon en mesures (frise).</summary>
    public double Length => Draft?.Bars ?? 8;

    /// <summary>Blocs à dessiner.</summary>
    public IReadOnlyList<TimelineBlock> Blocks { get; private set; } = [];

    /// <summary>Scènes et actions à glisser sur les pistes, par couche.</summary>
    public ObservableCollection<LibraryGroup> Library { get; } = [];

    /// <summary>Scènes de la couche du bloc choisi.</summary>
    public IReadOnlyList<Choice<Guid>> BlockScenes { get; private set; } = [];

    /// <summary>Couches proposées à une action de niveau.</summary>
    public IReadOnlyList<Choice<Guid>> LayerChoices { get; private set; } = [];

    /// <summary>Scènes proposées à un flash.</summary>
    public IReadOnlyList<Choice<Guid>> SceneChoices { get; private set; } = [];

    /// <summary>Le bloc choisi porte une scène.</summary>
    public bool SelectedIsScene => SelectedBlock?.SceneId is not null;

    /// <summary>Le bloc choisi est une action de niveau (couche ou Grand Master).</summary>
    public bool SelectedIsLevel => SelectedBlock?.Action?.Kind is BlockActionKind.LayerLevel or BlockActionKind.GrandMaster;

    /// <summary>Le bloc choisi est une action de niveau de couche.</summary>
    public bool SelectedIsLayerLevel => SelectedBlock?.Action?.Kind == BlockActionKind.LayerLevel;

    /// <summary>Le bloc choisi est un flash.</summary>
    public bool SelectedIsFlash => SelectedBlock?.Action?.Kind == BlockActionKind.Flash;

    /// <summary>Un bloc est choisi.</summary>
    public bool HasSelection => SelectedBlock is not null;

    /// <inheritdoc />
    protected override string ItemLabel => Draft is null ? string.Empty : ShowRules.SequenceItem(Draft);

    private SequenceBlock? SelectedBlock => Selected is { } r && Draft is not null && r.Track < Draft.Tracks.Count && r.Block < Draft.Tracks[r.Track].Blocks.Count
        ? Draft.Tracks[r.Track].Blocks[r.Block]
        : null;

    /// <summary>Choisit un bloc (clic sur la frise) ; nul = aucun.</summary>
    public void Select(BlockRef? block)
    {
        Selected = block;
        LoadSelection();
        Rebuild();
    }

    /// <summary>
    /// Pose un élément de la bibliothèque sur une piste (glisser-déposer) : une scène va sur la piste de sa couche, une action sur la
    /// piste d'actions, au début donné (en mesures depuis 0, déjà aimanté), pour une mesure (ou ce qui reste jusqu'à la fin).
    /// </summary>
    public string? Drop(LibraryItem item, int row, double start)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Draft is null || row < 0 || row >= Tracks.Count)
        {
            return null;
        }

        var track = Tracks[row];
        if (item.SceneId is { } sceneId)
        {
            var scene = Runtime.Project.Scenes.Scenes.FirstOrDefault(s => s.Id == sceneId);
            if (scene is null)
            {
                return "Scène inconnue.";
            }

            if (track.LayerId != scene.LayerId)
            {
                // D39 : une scène ne va que sur la piste de sa couche ; on la pose sur la bonne piste, au même instant.
                row = Tracks.ToList().FindIndex(t => t.LayerId == scene.LayerId);
                if (row < 0)
                {
                    return "La couche de cette scène n'a pas de piste.";
                }
            }
        }
        else if (track.LayerId is not null)
        {
            row = Tracks.Count - 1;
        }

        var layerId = Tracks[row].LayerId;
        var length = Math.Max(Snap.Value, Math.Min(1, (double)Bars - start));
        var block = new SequenceBlock
        {
            Start = Math.Max(0, start),
            Length = length,
            SceneId = item.SceneId,
            Action = item.ActionKind is { } kind ? new BlockAction { Kind = kind, LayerId = kind == BlockActionKind.LayerLevel ? Runtime.Project.Layers.Layers.OrderBy(l => l.Priority).FirstOrDefault()?.Id : null } : null,
        };
        Change(s => WithTrack(s, layerId, blocks => [.. blocks, block]), $"Poser « {item.Label} »");
        Select(FindBlock(layerId, block));
        return null;
    }

    /// <summary>Déplace un bloc dans sa piste (début en mesures, déjà aimanté).</summary>
    public void Move(BlockRef block, double start)
    {
        ArgumentNullException.ThrowIfNull(block);
        Edit(block, b => b with { Start = Math.Max(0, start) }, "Déplacer le bloc");
    }

    /// <summary>Change la durée d'un bloc (en mesures, déjà aimantée).</summary>
    public void Resize(BlockRef block, double length)
    {
        ArgumentNullException.ThrowIfNull(block);
        Edit(block, b => b with { Length = Math.Max(Snap.Value, length) }, "Durée du bloc");
    }

    /// <summary>Retire le bloc choisi (Suppr).</summary>
    [RelayCommand]
    public void DeleteBlock()
    {
        if (Selected is not { } block || Draft is null)
        {
            return;
        }

        Change(s => s with { Tracks = [.. s.Tracks.Select((t, i) => i == block.Track ? t with { Blocks = [.. t.Blocks.Where((_, j) => j != block.Block)] } : t)] }, "Retirer le bloc");
        Select(null);
    }

    /// <summary>Aimante une position (en mesures) à la grille choisie.</summary>
    public double SnapTo(double bars) => Math.Round(bars / Snap.Value) * Snap.Value;

    /// <inheritdoc />
    protected override Guid IdOf(Sequence item) => item.Id;

    /// <inheritdoc />
    protected override string NameOf(Sequence item) => item.Name;

    /// <inheritdoc />
    protected override string ColorOf(Sequence item) => item.Color;

    /// <inheritdoc />
    protected override void Save(Sequence item)
    {
        // Les pistes vides ne sont pas enregistrées : la frise les montre toutes de toute façon.
        var cleaned = item with { Tracks = [.. item.Tracks.Where(t => t.Blocks.Count > 0)] };
        var set = Runtime.Project.Sequences;
        var list = set.Sequences.ToList();
        var index = list.FindIndex(s => s.Id == item.Id);
        if (index >= 0)
        {
            list[index] = cleaned;
        }
        else
        {
            list.Add(cleaned);
        }

        Runtime.Project.SaveSequences(set with { Sequences = list });
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CompileIssue> Validate(Sequence item) =>
        ShowRules.Validate(item, Runtime.Project.Sequences with { Sequences = [.. Runtime.Project.Sequences.Sequences.Where(s => s.Id != item.Id), item] }, Runtime.Project.Shows, Runtime.Project.Scenes, Runtime.Project.Layers);

    /// <inheritdoc />
    protected override void PushDraft(Sequence? item, bool previewOnly) => Runtime.SetSequenceDraft(item, previewOnly);

    /// <inheritdoc />
    protected override SequencerCommand PlayCommandFor(bool stopping) => stopping
        ? new StopSequenceCommand(CommandOrigin.User, Draft?.Id ?? Guid.Empty)
        : new LaunchSequenceCommand(CommandOrigin.User, Draft?.Id ?? Guid.Empty);

    /// <inheritdoc />
    protected override void OnOpened()
    {
        Selected = null;
        BuildLibrary();
        LoadSelection();
    }

    /// <inheritdoc />
    protected override void OnDraftChanged(bool rebuild)
    {
        if (Draft is null)
        {
            return;
        }

        _loading = true;
        Name = Draft.Name;
        Bars = (decimal)Draft.Bars;
        Loop = Draft.End == SequenceEnd.Loop;
        Quantize = ShowOptions.Quantizes.FirstOrDefault(q => q.Value == Draft.Quantize) ?? ShowOptions.Quantizes[2];
        Speed = ShowOptions.Speeds.FirstOrDefault(s => Math.Abs(s.Value - Draft.Speed) < 1e-9) ?? ShowOptions.Speeds[1];
        _loading = false;
        // Les champs du bloc choisi suivent les gestes à la souris (déplacer, allonger) et l'annulation.
        if (SelectedBlock is null)
        {
            Selected = null;
        }

        LoadSelection();

        Rebuild();
    }

    partial void OnNameChanged(string value)
    {
        if (!_loading && !string.IsNullOrWhiteSpace(value))
        {
            Change(s => s with { Name = value.Trim() }, "Nom de la séquence");
        }
    }

    partial void OnBarsChanged(decimal value)
    {
        if (!_loading && value > 0)
        {
            Change(s => s with { Bars = (double)value }, "Longueur de la séquence");
        }
    }

    partial void OnLoopChanged(bool value)
    {
        if (!_loading)
        {
            Change(s => s with { End = value ? SequenceEnd.Loop : SequenceEnd.Stop }, value ? "En boucle" : "Une seule fois");
        }
    }

    partial void OnQuantizeChanged(Choice<ShowQuantize> value)
    {
        if (!_loading && value is not null)
        {
            Change(s => s with { Quantize = value.Value }, "Démarrage de la séquence");
        }
    }

    partial void OnSpeedChanged(Choice<double> value)
    {
        if (!_loading && value is not null)
        {
            Change(s => s with { Speed = value.Value }, "Vitesse de la séquence");
        }
    }

    partial void OnSearchChanged(string value) => BuildLibrary();

    partial void OnBlockSceneChanged(Choice<Guid>? value)
    {
        if (!_loading && value is not null && Selected is { } block)
        {
            Edit(block, b => b with { SceneId = value.Value }, "Scène du bloc");
        }
    }

    partial void OnBlockStartBarChanged(decimal value) => EditStart();

    partial void OnBlockStartBeatChanged(decimal value) => EditStart();

    partial void OnBlockLengthChanged(decimal value) => EditLength();

    partial void OnBlockUnitBarsChanged(Choice<bool> value) => EditLength();

    partial void OnBlockKeepChanged(bool value)
    {
        if (!_loading && Selected is { } block)
        {
            Edit(block, b => b with { End = value ? BlockEnd.Keep : BlockEnd.Stop }, value ? "Laisser jouer après le bloc" : "Arrêter à la fin du bloc");
        }
    }

    partial void OnActionLayerChanged(Choice<Guid>? value)
    {
        if (!_loading && value is not null && Selected is { } block)
        {
            Edit(block, b => b with { Action = b.Action! with { LayerId = value.Value } }, "Couche de l'action");
        }
    }

    partial void OnActionSceneChanged(Choice<Guid>? value)
    {
        if (!_loading && value is not null && Selected is { } block)
        {
            Edit(block, b => b with { Action = b.Action! with { SceneId = value.Value } }, "Scène du flash");
        }
    }

    partial void OnActionFromChanged(decimal? value)
    {
        if (!_loading && Selected is { } block)
        {
            Edit(block, b => b with { Action = b.Action! with { From = value is { } v ? (double)Math.Clamp(v, 0, 100) / 100 : null } }, "Niveau de départ");
        }
    }

    partial void OnActionToChanged(decimal value)
    {
        if (!_loading && Selected is { } block)
        {
            Edit(block, b => b with { Action = b.Action! with { To = (double)Math.Clamp(value, 0, 100) / 100 } }, "Niveau d'arrivée");
        }
    }

    partial void OnSelectedChanged(BlockRef? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectedIsScene));
        OnPropertyChanged(nameof(SelectedIsLevel));
        OnPropertyChanged(nameof(SelectedIsLayerLevel));
        OnPropertyChanged(nameof(SelectedIsFlash));
    }

    private void EditStart()
    {
        if (!_loading && Selected is { } block)
        {
            var start = ((double)Math.Max(1, BlockStartBar) - 1) + (((double)Math.Clamp(BlockStartBeat, 1, 4) - 1) / Show.Runtime.Sequencer.BeatsPerBar);
            Edit(block, b => b with { Start = start }, "Début du bloc");
        }
    }

    private void EditLength()
    {
        if (!_loading && Selected is { } block && BlockLength > 0)
        {
            var length = BlockUnitBars.Value ? (double)BlockLength : (double)BlockLength / Show.Runtime.Sequencer.BeatsPerBar;
            Edit(block, b => b with { Length = length }, "Durée du bloc");
        }
    }

    private void Edit(BlockRef block, Func<SequenceBlock, SequenceBlock> change, string description)
    {
        if (Draft is null || block.Track >= Draft.Tracks.Count || block.Block >= Draft.Tracks[block.Track].Blocks.Count)
        {
            return;
        }

        Change(s => s with { Tracks = [.. s.Tracks.Select((t, i) => i == block.Track ? t with { Blocks = [.. t.Blocks.Select((b, j) => j == block.Block ? change(b) : b)] } : t)] }, description);
    }

    // Ajoute la piste de la couche si elle manque, puis change ses blocs.
    private static Sequence WithTrack(Sequence sequence, Guid? layerId, Func<IReadOnlyList<SequenceBlock>, IReadOnlyList<SequenceBlock>> change)
    {
        var tracks = sequence.Tracks.ToList();
        var index = tracks.FindIndex(t => t.LayerId == layerId);
        if (index < 0)
        {
            tracks.Add(new SequenceTrack { LayerId = layerId });
            index = tracks.Count - 1;
        }

        tracks[index] = tracks[index] with { Blocks = change(tracks[index].Blocks) };
        return sequence with { Tracks = tracks };
    }

    private BlockRef? FindBlock(Guid? layerId, SequenceBlock block)
    {
        if (Draft is null)
        {
            return null;
        }

        var track = Draft.Tracks.ToList().FindIndex(t => t.LayerId == layerId);
        var index = track < 0 ? -1 : Draft.Tracks[track].Blocks.ToList().IndexOf(block);
        return index < 0 ? null : new BlockRef(track, index);
    }

    private void LoadSelection()
    {
        _loading = true;
        try
        {
            var project = Runtime.Project;
            LayerChoices = [.. project.Layers.Layers.OrderBy(l => l.Priority).Select(l => new Choice<Guid>(l.Id, l.Name))];
            SceneChoices = [.. project.Scenes.Scenes.Select(s => new Choice<Guid>(s.Id, s.Name))];
            OnPropertyChanged(nameof(LayerChoices));
            OnPropertyChanged(nameof(SceneChoices));
            if (SelectedBlock is not { } block || Selected is not { } reference || Draft is null)
            {
                SelectedTitle = "Aucun bloc choisi : cliquez un bloc de la frise, ou glissez une scène de la liste.";
                BlockScenes = [];
                OnPropertyChanged(nameof(BlockScenes));
                OnSelectedChanged(null);
                return;
            }

            var layerId = Draft.Tracks[reference.Track].LayerId;
            BlockScenes = [.. project.Scenes.Scenes.Where(s => s.LayerId == layerId).Select(s => new Choice<Guid>(s.Id, s.Name))];
            OnPropertyChanged(nameof(BlockScenes));
            BlockScene = BlockScenes.FirstOrDefault(c => c.Value == block.SceneId);
            BlockStartBar = (decimal)Math.Floor(block.Start + 1e-9) + 1;
            BlockStartBeat = (decimal)Math.Round(((block.Start - Math.Floor(block.Start + 1e-9)) * Show.Runtime.Sequencer.BeatsPerBar) + 1, 2);
            var wholeBars = Math.Abs(block.Length - Math.Round(block.Length)) < 1e-9;
            BlockUnitBars = ShowOptions.LengthUnits[wholeBars ? 0 : 1];
            BlockLength = (decimal)Math.Round(wholeBars ? block.Length : block.Length * Show.Runtime.Sequencer.BeatsPerBar, 3);
            BlockKeep = block.End == BlockEnd.Keep;
            ActionLayer = LayerChoices.FirstOrDefault(c => c.Value == block.Action?.LayerId);
            ActionScene = SceneChoices.FirstOrDefault(c => c.Value == block.Action?.SceneId);
            ActionFrom = block.Action?.From is { } from ? (decimal)Math.Round(from * 100) : null;
            ActionTo = (decimal)Math.Round((block.Action?.To ?? 1) * 100);
            SelectedTitle = $"Bloc choisi : « {Label(block)} »";
            OnSelectedChanged(Selected);
        }
        finally
        {
            _loading = false;
        }
    }

    private void Rebuild()
    {
        if (Draft is null)
        {
            Tracks = [];
            Blocks = [];
            OnPropertyChanged(nameof(Tracks));
            OnPropertyChanged(nameof(Blocks));
            return;
        }

        var project = Runtime.Project;
        var tracks = project.Layers.Layers.OrderBy(l => l.Priority)
            .Select(l => new TimelineTrack(l.Id, string.IsNullOrEmpty(l.Icon) ? l.Name : $"{l.Icon}  {l.Name}", l.Color, "couche"))
            .Append(new TimelineTrack(null, "⚙  Actions", "#8B949E", "niveaux, fumée, flash, noir"))
            .ToList();
        var blocks = new List<TimelineBlock>();
        for (var t = 0; t < Draft.Tracks.Count; t++)
        {
            var row = tracks.FindIndex(r => r.LayerId == Draft.Tracks[t].LayerId);
            if (row < 0)
            {
                continue;
            }

            for (var b = 0; b < Draft.Tracks[t].Blocks.Count; b++)
            {
                var block = Draft.Tracks[t].Blocks[b];
                var color = block.SceneId is { } id ? project.Scenes.Scenes.FirstOrDefault(s => s.Id == id)?.Color ?? "#8B949E" : "#8B949E";
                var reference = new BlockRef(t, b);
                blocks.Add(new TimelineBlock(
                    reference,
                    row,
                    block.Start,
                    block.Length,
                    Label(block),
                    color,
                    block.Action is not null,
                    reference == Selected,
                    block.Action is { Kind: BlockActionKind.LayerLevel or BlockActionKind.GrandMaster } a ? (a.From, a.To) : null,
                    block.End == BlockEnd.Keep));
            }
        }

        Tracks = tracks;
        Blocks = blocks;
        OnPropertyChanged(nameof(Tracks));
        OnPropertyChanged(nameof(Blocks));
        OnPropertyChanged(nameof(Length));
    }

    private string Label(SequenceBlock block)
    {
        var project = Runtime.Project;
        string Name(Guid? id) => id is { } value ? project.Scenes.Scenes.FirstOrDefault(s => s.Id == value)?.Name ?? project.Layers.Layers.FirstOrDefault(l => l.Id == value)?.Name ?? "?" : "?";
        return block.Action switch
        {
            null => Name(block.SceneId),
            { Kind: BlockActionKind.LayerLevel } a => string.Create(CultureInfo.CurrentCulture, $"Niveau {Name(a.LayerId)} {(a.From is { } f ? $"{f * 100:0} → " : "→ ")}{a.To * 100:0} %"),
            { Kind: BlockActionKind.GrandMaster } a => string.Create(CultureInfo.CurrentCulture, $"Grand Master {(a.From is { } f ? $"{f * 100:0} → " : "→ ")}{a.To * 100:0} %"),
            { Kind: BlockActionKind.Smoke } => "Fumée",
            { Kind: BlockActionKind.Flash } a => $"Flash « {Name(a.SceneId)} »",
            _ => "Noir",
        };
    }

    private void BuildLibrary()
    {
        Library.Clear();
        var project = Runtime.Project;
        var filter = Search.Trim();
        foreach (var layer in project.Layers.Layers.OrderBy(l => l.Priority))
        {
            var items = project.Scenes.Scenes
                .Where(s => s.LayerId == layer.Id && (filter.Length == 0 || s.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)))
                .Select(s => new LibraryItem(s.Name, s.Color, s.Id, null))
                .ToList();
            if (items.Count > 0)
            {
                Library.Add(new LibraryGroup(string.IsNullOrEmpty(layer.Icon) ? layer.Name : $"{layer.Icon}  {layer.Name}", layer.Color, items));
            }
        }

        Library.Add(new LibraryGroup("⚙  Actions", "#8B949E",
        [
            new LibraryItem("Niveau de couche (rampe)", "#8B949E", null, BlockActionKind.LayerLevel),
            new LibraryItem("Grand Master (rampe)", "#8B949E", null, BlockActionKind.GrandMaster),
            new LibraryItem("Fumée (rafale)", "#6E7681", null, BlockActionKind.Smoke),
            new LibraryItem("Flash d'une scène", "#FFFFFF", null, BlockActionKind.Flash),
            new LibraryItem("Noir court", "#30363D", null, BlockActionKind.Blackout),
        ]));
    }
}

/// <summary>Repère d'un bloc dans le brouillon : rang de la piste, rang du bloc dans la piste.</summary>
/// <param name="Track">Rang de la piste dans la séquence.</param>
/// <param name="Block">Rang du bloc dans la piste.</param>
public sealed record BlockRef(int Track, int Block);

/// <summary>Une piste de la frise.</summary>
/// <param name="LayerId">Couche, ou nulle pour la piste d'actions.</param>
/// <param name="Title">Titre.</param>
/// <param name="Color">Couleur.</param>
/// <param name="Subtitle">Sous-titre.</param>
public sealed record TimelineTrack(Guid? LayerId, string Title, string Color, string Subtitle);

/// <summary>Un bloc à dessiner sur la frise.</summary>
/// <param name="Ref">Repère dans le brouillon.</param>
/// <param name="Row">Rang de la piste affichée.</param>
/// <param name="Start">Début en mesures.</param>
/// <param name="Length">Durée en mesures.</param>
/// <param name="Label">Texte.</param>
/// <param name="Color">Couleur « #RRGGBB ».</param>
/// <param name="IsAction">Bloc d'action.</param>
/// <param name="IsSelected">Bloc choisi.</param>
/// <param name="Ramp">Rampe de niveau (départ, arrivée) à dessiner, ou nulle.</param>
/// <param name="Keeps">La scène continue après le bloc.</param>
public sealed record TimelineBlock(BlockRef Ref, int Row, double Start, double Length, string Label, string Color, bool IsAction, bool IsSelected, (double? From, double To)? Ramp, bool Keeps);

/// <summary>Un groupe de la bibliothèque (une couche, ou les actions).</summary>
/// <param name="Title">Titre.</param>
/// <param name="Color">Couleur.</param>
/// <param name="Items">Éléments.</param>
public sealed record LibraryGroup(string Title, string Color, IReadOnlyList<LibraryItem> Items);

/// <summary>Un élément à glisser : une scène ou une action.</summary>
/// <param name="Label">Texte.</param>
/// <param name="Color">Couleur.</param>
/// <param name="SceneId">Scène, ou nulle.</param>
/// <param name="ActionKind">Action, ou nulle.</param>
public sealed record LibraryItem(string Label, string Color, Guid? SceneId, BlockActionKind? ActionKind);
