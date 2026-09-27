using Avalonia;
using Avalonia.Controls;
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
            MainWindowViewModel? shell = null;

            // GEN-117 : toute exception finit dans le journal technique, d'où qu'elle vienne.
            // 1. Interface : journalisée, signalée dans la barre d'état ; le moteur et la sortie continuent (GEN-093).
            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                logger.LogError(e.Exception, "Erreur non gérée dans l'interface");
                shell?.ReportError(e.Exception);
                e.Handled = true;
            };

            // 2. Autres fils et tâches non surveillées.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                logger.LogCritical(e.ExceptionObject as Exception, "Erreur non gérée (fil {Fil}), arrêt : {Arret}", Environment.CurrentManagedThreadId, e.IsTerminating);
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                logger.LogError(e.Exception, "Erreur d'une tâche de fond non surveillée");
                e.SetObserved();
            };

            // 3. Une exception levée en écrivant une valeur saisie dans un champ est interceptée par Avalonia, qui l'affiche
            //    en rouge sous le champ sans jamais la journaliser (vérifié le 2026-09-26, Avalonia 12) : on écoute ces
            //    erreurs pour toute l'application. Les journaux internes d'Avalonia (liaisons cassées) vont aussi au journal.
            Exception? lastBindingError = null;
            DataValidationErrors.ErrorsProperty.Changed.Subscribe(new Services.Observer<AvaloniaPropertyChangedEventArgs<IEnumerable<object>?>>(e =>
            {
                foreach (var error in e.NewValue.GetValueOrDefault() ?? [])
                {
                    if (error is Exception exception && !ReferenceEquals(exception, lastBindingError))
                    {
                        lastBindingError = exception;
                        logger.LogError(exception, "Valeur refusée par un champ ({Controle}, écran {Ecran})", e.Sender.GetType().Name, (e.Sender as Control)?.DataContext?.GetType().Name);
                        shell?.ReportError(exception);
                    }
                }
            }));
            Avalonia.Logging.Logger.Sink = new Services.AvaloniaLogSink(_loggers.CreateLogger("Avalonia"));

            // GEN-065 : fenêtre de démarrage tout de suite, le travail lourd hors du fil de l'interface pour qu'elle s'affiche.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) => Shutdown();
            var splash = new SplashWindow();
            splash.Show();
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    splash.Report("Préférences, bibliothèque et dernier projet…", 10);
                    await Task.Delay(50).ConfigureAwait(true); // laisse la fenêtre se dessiner
                    var args = desktop.Args;
                    var loggers = _loggers;
                    _runtime = await Task.Run(() =>
                    {
                        var runtime = new LuxiaRuntime(DataPaths.Current, loggers, midiPorts: new Midi.WinMmMidiPorts());

                        // LuXia.exe "dossier du projet" : ouvre ce projet (à défaut, le dernier projet ouvert).
                        if (args is [var projectFolder, ..] && Directory.Exists(projectFolder))
                        {
                            runtime.Project.Open(Path.GetFullPath(projectFolder));
                        }

                        return runtime;
                    }).ConfigureAwait(true);

                    // GEN-060 : le moteur démarre en émettant un blackout ; les sorties (Arduino) sont recherchées ici.
                    splash.Report("Moteur DMX et sorties…", 60);
                    var runtimeStarted = _runtime;
                    await Task.Run(() => runtimeStarted.Start()).ConfigureAwait(true);

                    splash.Report("Écrans…", 85);
                    await Task.Delay(20).ConfigureAwait(true);
                    var window = new MainWindow();
                    shell = new MainWindowViewModel(_runtime, new Services.DialogService(() => window));
                    window.DataContext = shell;
                    desktop.MainWindow = window;
                    splash.Report("Prêt", 100);
                    window.Show();
                    desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    splash.Close();
                }
                catch (Exception exception)
                {
                    logger.LogCritical(exception, "Démarrage impossible");
                    splash.Close();
                    desktop.Shutdown(1);
                }
            });
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
