using Luxia.Core.Time;
using Luxia.Hosting;
using Luxia.Output.Arduino;
using Luxia.Persistence;
using Luxia.UI.Controls;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.UI.Tests;

/// <summary>
/// Assemblage de test : dossiers temporaires, aucun port série, horloge virtuelle, boucle non démarrée
/// (les ticks sont déclenchés à la main).
/// </summary>
internal sealed class TestHost : IAsyncDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dmx-ui-tests", Guid.NewGuid().ToString("N"));

    public TestHost()
    {
        Paths = new DataPaths(Path.Combine(_root, "Documents"), Path.Combine(_root, "AppData"));
        Runtime = new LuxiaRuntime(Paths, NullLoggerFactory.Instance, new NoSerialPorts(), Clock);
    }

    public VirtualClock Clock { get; } = new();

    public DataPaths Paths { get; }

    public LuxiaRuntime Runtime { get; }

    public FakeDialogs Dialogs { get; } = new();

    public string ProjectFolder => Path.Combine(_root, "Projet");

    /// <summary>Exécute un tick du moteur (les commandes envoyées sont appliquées).</summary>
    public void Tick()
    {
        Clock.Advance(TimeSpan.FromMilliseconds(25));
        Runtime.Engine.Tick();
    }

    public byte[] Frame()
    {
        var frame = new byte[512];
        Runtime.Engine.CopyLastFrame(1, frame);
        return frame;
    }

    public async ValueTask DisposeAsync()
    {
        await Runtime.DisposeAsync();
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private sealed class NoSerialPorts : ISerialPortProvider
    {
        public IReadOnlyList<SerialPortInfo> GetPorts() => [];

        public ISerialConnection Open(string portName) => throw new IOException("pas de port en test");
    }
}

/// <summary>Boîtes de dialogue simulées : réponses préparées par le test.</summary>
internal sealed class FakeDialogs : IDialogService
{
    public bool ConfirmAnswer { get; set; } = true;

    public Queue<string?> TextAnswers { get; } = new();

    public List<string> Confirmations { get; } = [];

    public Task<bool> ConfirmAsync(string title, string message)
    {
        Confirmations.Add(message);
        return Task.FromResult(ConfirmAnswer);
    }

    /// <summary>Réponse à « enregistrer les modifications ? » (Oui par défaut).</summary>
    public SaveChoice SaveAnswer { get; set; } = SaveChoice.Save;

    /// <summary>Questions « enregistrer les modifications ? » posées.</summary>
    public List<string> SaveQuestions { get; } = [];

    public Task<SaveChoice> AskSaveAsync(string title, string message)
    {
        SaveQuestions.Add(message);
        return Task.FromResult(SaveAnswer);
    }

    public Task<string?> AskTextAsync(string title, string prompt, string? initialValue = null) =>
        Task.FromResult(TextAnswers.Count > 0 ? TextAnswers.Dequeue() : null);

    public List<string> ShownInfo { get; } = [];

    public Task ShowInfoAsync(string title, string message)
    {
        ShownInfo.Add(message);
        return Task.CompletedTask;
    }

    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);

    public Task<IReadOnlyList<string>> PickFilesAsync(string title, bool allowMultiple, params string[] extensions) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
