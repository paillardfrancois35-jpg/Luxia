using Dmx.Core.Time;
using Dmx.Hosting;
using Dmx.Output.Arduino;
using Dmx.Persistence;
using Dmx.UI.Controls;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dmx.UI.Tests;

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
        Runtime = new DmxRuntime(Paths, NullLoggerFactory.Instance, new NoSerialPorts(), Clock);
    }

    public VirtualClock Clock { get; } = new();

    public DataPaths Paths { get; }

    public DmxRuntime Runtime { get; }

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

    public Task<string?> AskTextAsync(string title, string prompt, string? initialValue = null) =>
        Task.FromResult(TextAnswers.Count > 0 ? TextAnswers.Dequeue() : null);

    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);

    public Task<IReadOnlyList<string>> PickFilesAsync(string title, bool allowMultiple, params string[] extensions) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
