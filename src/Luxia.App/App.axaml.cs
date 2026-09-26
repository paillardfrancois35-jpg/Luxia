using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Luxia.App.ViewModels;
using Luxia.Hosting;
using Luxia.Persistence;
using Microsoft.Extensions.Logging;

namespace Luxia.App;

/// <summary>Application Avalonia : assemble les modules et crée la fenêtre principale.</summary>
/// <remarks>Les modules sont libérés à l'événement <c>Exit</c> du cycle de vie Avalonia, pas par <see cref="IDisposable"/>.</remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "Libération dans Shutdown(), appelé à la sortie de l'application.")]
public partial class App : Application
{
    private ILoggerFactory? _loggers;
    private LuxiaRuntime? _runtime;

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _loggers = TechnicalLog.Create(DataPaths.Current.Logs);
            var logger = _loggers.CreateLogger<App>();
            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                // GEN-093 : une erreur d'interface est journalisée ; le moteur et la sortie continuent.
                logger.LogError(e.Exception, "Erreur non gérée dans l'interface");
                e.Handled = true;
            };

            _runtime = new LuxiaRuntime(DataPaths.Current, _loggers);

            // LuXia.exe "dossier du projet" : ouvre ce projet (à défaut, le dernier projet ouvert).
            if (desktop.Args is [var projectFolder, ..] && Directory.Exists(projectFolder))
            {
                _runtime.Project.Open(Path.GetFullPath(projectFolder));
            }

            _runtime.Start();

            var window = new MainWindow();
            window.DataContext = new MainWindowViewModel(_runtime, new Services.DialogService(() => window));
            desktop.MainWindow = window;
            desktop.Exit += (_, _) => Shutdown();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void Shutdown()
    {
        // Arrêt propre : trame de blackout puis fermeture des sorties (sans attendre le chien de garde).
        _runtime?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _loggers?.Dispose();
    }
}
