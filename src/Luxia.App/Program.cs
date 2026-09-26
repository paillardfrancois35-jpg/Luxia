using System.Runtime.InteropServices;
using Avalonia;

namespace Luxia.App;

/// <summary>Point d'entrée de l'application LuXia.</summary>
internal static class Program
{
    /// <summary>
    /// Nom unique du verrou d'instance : deux processus LuXia se disputeraient le même port Arduino
    /// et le même fichier de préférences (constaté le 2026-09-26 : projet non retrouvé au démarrage).
    /// </summary>
    private const string SingleInstanceMutexName = "LuXia-instance-unique-9f3d7c2a";

    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            _ = MessageBox(IntPtr.Zero, "LuXia est déjà lancé (voir la barre des tâches).", "LuXia", 0);
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    /// <summary>Configuration Avalonia (utilisée aussi par le concepteur visuel).</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace(); // Remplacé par le journal technique dès que l'application démarre (App, GEN-117).
}
