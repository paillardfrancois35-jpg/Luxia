using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Luxia.Tools.Prototype.Docking;

namespace Luxia.Tools.Prototype;

/// <summary>Application du prototype : thème sombre, thème Dock, gabarit des panneaux.</summary>
public sealed class App : Application
{
    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Au niveau de l'application, pas de la fenêtre : un panneau détaché vit dans une autre fenêtre (HostWindow)
        // et doit y retrouver son contenu.
        DataTemplates.Add(new PanelTemplate(DemoState.Shared));
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(new ShellViewModel(DemoState.Shared, LayoutStore.ForCurrentUser()));
        }

        base.OnFrameworkInitializationCompleted();
    }
}
