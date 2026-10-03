namespace Luxia.Media;

/// <summary>
/// Source des sessions média du système (MUS-001). L'adaptateur Windows est dans <c>Luxia.Media.Windows</c> ; les tests
/// et les outils fournissent une source simulée. Aucune exception ne doit sortir d'une source.
/// </summary>
public interface IMediaSessionSource : IDisposable
{
    /// <summary>Levé (sur un fil quelconque) quand une session apparaît, disparaît ou change (titre, état, position).</summary>
    event EventHandler? Changed;

    /// <summary>Photographie des sessions à cet instant (vide tant que le système n'a pas répondu).</summary>
    IReadOnlyList<MediaSessionInfo> Sessions();
}
