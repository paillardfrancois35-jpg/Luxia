using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Panneau Journal (LIVE-009) : ce qui vient de se passer, le plus récent en haut — scènes lancées et arrêtées,
/// limites de sûreté, sortie, commandes refusées, et ce que l'écran a enregistré (Ctrl+Z pour annuler).
/// </summary>
public sealed class JournalPanelViewModel : ViewModelBase
{
    private const int Size = 80;
    private readonly ConcurrentQueue<string> _incoming = new();

    /// <summary>Branche le journal sur le bus du moteur.</summary>
    public JournalPanelViewModel(LuxiaRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        runtime.Bus.Subscribe<SceneStarted>(e => Log($"▶ {e.SceneName} ({(e.Origin == CommandOrigin.Midi ? "MIDI" : e.Origin == CommandOrigin.User ? "utilisateur" : e.Origin.ToString())})"));
        runtime.Bus.Subscribe<SceneStopped>(e => Log($"■ {e.SceneName}"));
        runtime.Bus.Subscribe<SafetyLimitReached>(e => Log($"⚠ {e.Label.Split(" – ")[0]} : {e.Detail}"));
        runtime.Bus.Subscribe<OutputStateChanged>(e => Log($"⇄ {e.DriverName} : {e.State}"));
        runtime.Bus.Subscribe<CommandRejected>(e => Log($"✕ commande refusée : {e.Reason}"));
    }

    /// <summary>Lignes, la plus récente en tête.</summary>
    public ObservableCollection<string> Lines { get; } = [];

    /// <summary>Ajoute une ligne (appelable depuis n'importe quel fil).</summary>
    public void Log(string text) => _incoming.Enqueue(string.Create(CultureInfo.CurrentCulture, $"{DateTime.Now:HH:mm:ss}  {text}"));

    /// <summary>Affiche les lignes reçues (fil de l'interface).</summary>
    public void Refresh()
    {
        while (_incoming.TryDequeue(out var line))
        {
            Lines.Insert(0, line);
        }

        while (Lines.Count > Size)
        {
            Lines.RemoveAt(Lines.Count - 1);
        }
    }
}
