using Luxia.Engine;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Hosting;

/// <summary>
/// Tient le moteur à jour avec le projet (D26) : compile installation, lieu actif, couches, scènes et palettes en
/// modèle du moteur, et le recharge à chaque ouverture ou modification enregistrée. Une scène en cours d'édition
/// (programmeur) peut remplacer sa version enregistrée le temps d'un essai (SCN-034).
/// </summary>
public sealed class ShowService
{
    private readonly ProjectSession _project;
    private readonly IReadOnlyList<RenderEngine> _engines;
    private readonly ILogger _logger;
    private Scene? _workingCopy;

    /// <summary>Branche le service sur le projet et les moteurs (sortie, aperçu) ; compile tout de suite le projet ouvert.</summary>
    public ShowService(ProjectSession project, IReadOnlyList<RenderEngine> engines, ILogger<ShowService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(engines);
        _project = project;
        _engines = engines;
        _logger = logger ?? NullLogger<ShowService>.Instance;
        _project.Changed += (_, _) =>
        {
            _workingCopy = null;
            Recompile();
        };
        _project.ShowDataChanged += (_, _) => Recompile();
        Recompile();
    }

    /// <summary>Dernier résultat de compilation (problèmes à montrer à l'utilisateur).</summary>
    public CompileResult? Last { get; private set; }

    /// <summary>Patch résolu du dernier chargement (programmeur, explication des valeurs).</summary>
    public PatchContext Patch { get; private set; } = new(new Patch.Model.Installation(), new Patch.Model.VenueSet(), _ => null);

    /// <summary>Levé après chaque compilation.</summary>
    public event EventHandler? Compiled;

    /// <summary>
    /// Remplace (ou ajoute) une scène par sa version en cours d'édition, sans l'enregistrer, puis recompile :
    /// « Tester » joue ce que l'on voit dans le programmeur. <c>null</c> revient aux scènes enregistrées.
    /// </summary>
    public void SetWorkingCopy(Scene? scene)
    {
        _workingCopy = scene;
        Recompile();
    }

    /// <summary>Contenu du projet tel que compilé (avec la scène en cours d'édition, s'il y en a une).</summary>
    public ProjectContent Content()
    {
        var scenes = _project.Scenes;
        if (_workingCopy is { } copy)
        {
            var list = scenes.Scenes.Where(s => s.Id != copy.Id).ToList();
            var index = scenes.Scenes.ToList().FindIndex(s => s.Id == copy.Id);
            list.Insert(index < 0 ? list.Count : index, copy);
            scenes = scenes with { Scenes = list };
        }

        var library = _project.FixtureLibrary;
        return new ProjectContent(
            _project.Installation,
            _project.Venues,
            id => library?.Find(id),
            _project.Layers,
            scenes,
            _project.Palettes,
            _project.Safety);
    }

    /// <summary>Compile le projet et charge le résultat dans le moteur.</summary>
    public void Recompile()
    {
        var content = Content();
        var result = ShowCompiler.Compile(content);
        Patch = new PatchContext(content.Installation, content.Venues, content.TypeOf);
        Last = result;
        foreach (var engine in _engines)
        {
            engine.LoadShow(result.Model);
        }
        foreach (var issue in result.Issues)
        {
            _logger.LogWarning("Compilation du projet : {Probleme}", issue);
        }

        Compiled?.Invoke(this, EventArgs.Empty);
    }
}
