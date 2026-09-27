using System.Runtime.InteropServices;
using Avalonia;

namespace Luxia.Tools.Prototype;

/// <summary>
/// Prototype ergonomique (doc 60 §7.2) : <c>LuXia-Prototype</c> ouvre la fenêtre ;
/// <c>LuXia-Prototype --captures "&lt;dossier&gt;"</c> écrit les images de la galerie et des dispositions sans écran.
/// </summary>
internal static class Program
{
    // Un seul prototype à la fois : deux fenêtres écriraient le même fichier de disposition.
    private const string SingleInstanceMutexName = "LuXia-Prototype-instance-unique-4b81e0d7";

    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "--captures")
        {
            return HeadlessCaptures.Run(Path.GetFullPath(args[1]));
        }

        using var mutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            _ = MessageBox(IntPtr.Zero, "Le prototype LuXia est déjà lancé (voir la barre des tâches).", "LuXia – prototype", 0);
            return 1;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    /// <summary>Configuration Avalonia (utilisée aussi par le concepteur visuel).</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
