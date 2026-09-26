namespace Luxia.UI.Controls;

/// <summary>
/// Historique annuler / rétablir d'un état immuable (GEN-102 : au moins 50 niveaux ; 100 ici).
/// On enregistre l'état <b>avant</b> chaque modification.
/// </summary>
/// <typeparam name="T">Type d'état (immuable).</typeparam>
public sealed class UndoHistory<T>
    where T : class
{
    /// <summary>Nombre maximal de niveaux conservés.</summary>
    public const int MaxLevels = 100;

    private readonly LinkedList<(T State, string Description)> _undo = new();
    private readonly Stack<(T State, string Description)> _redo = new();

    /// <summary>Annulation possible.</summary>
    public bool CanUndo => _undo.Count > 0;

    /// <summary>Rétablissement possible.</summary>
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Description de la prochaine annulation.</summary>
    public string? UndoDescription => _undo.Last?.Value.Description;

    /// <summary>Description du prochain rétablissement.</summary>
    public string? RedoDescription => _redo.Count > 0 ? _redo.Peek().Description : null;

    /// <summary>Enregistre l'état précédant une modification ; vide la pile « rétablir ».</summary>
    public void Record(T before, string description)
    {
        ArgumentNullException.ThrowIfNull(before);
        _undo.AddLast((before, description));
        if (_undo.Count > MaxLevels)
        {
            _undo.RemoveFirst();
        }

        _redo.Clear();
    }

    /// <summary>Annule : renvoie l'état à restaurer (ou null).</summary>
    public T? Undo(T current)
    {
        if (_undo.Last is not { } last)
        {
            return null;
        }

        _undo.RemoveLast();
        _redo.Push((current, last.Value.Description));
        return last.Value.State;
    }

    /// <summary>Rétablit : renvoie l'état à restaurer (ou null).</summary>
    public T? Redo(T current)
    {
        if (_redo.Count == 0)
        {
            return null;
        }

        var (state, description) = _redo.Pop();
        _undo.AddLast((current, description));
        return state;
    }

    /// <summary>Oublie tout l'historique.</summary>
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
