using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Dock.Model.Mvvm.Controls;
using Luxia.Tools.Prototype.Panels;

namespace Luxia.Tools.Prototype.Docking;

/// <summary>
/// Contenu d'un panneau selon son identifiant. Les vues sont sans état propre (tout est dans <see cref="DemoState"/>) :
/// Dock peut les reconstruire à chaque déplacement ou détachement sans rien perdre.
/// </summary>
internal sealed class PanelTemplate(DemoState state) : IDataTemplate
{
    /// <inheritdoc />
    public bool Match(object? data) => data is Tool;

    /// <inheritdoc />
    public Control? Build(object? param) => param is Tool tool ? PanelViews.Create(tool.Id, state) : null;
}
