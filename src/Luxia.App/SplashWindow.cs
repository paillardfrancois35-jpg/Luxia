using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Luxia.App;

/// <summary>
/// Fenêtre de démarrage (GEN-065) : affichée dès le lancement, elle dit ce que LuXia prépare (préférences, projet,
/// moteur et sorties, écrans) avec un pourcentage, puis disparaît quand la fenêtre principale s'ouvre.
/// </summary>
internal sealed class SplashWindow : Window
{
    private readonly TextBlock _step = new() { Text = "Démarrage…", Foreground = Brushes.Gainsboro, FontSize = 13 };
    private readonly ProgressBar _bar = new() { Minimum = 0, Maximum = 100, Height = 6 };
    private readonly TextBlock _percent = new() { Text = "0 %", Foreground = Brushes.Gray, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right };

    /// <summary>Crée la fenêtre.</summary>
    public SplashWindow()
    {
        Title = "LuXia";
        Width = 420;
        Height = 150;
        CanResize = false;
        WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = true;
        Topmost = true;
        Background = new SolidColorBrush(Color.Parse("#0D1117"));
        Content = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#30363D")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(24, 20),
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "LuXia", Foreground = Brushes.White, FontSize = 22, FontWeight = FontWeight.Bold },
                    _step,
                    _bar,
                    _percent,
                },
            },
        };
    }

    /// <summary>Affiche une étape et son avancement (0-100).</summary>
    public void Report(string step, int percent)
    {
        _step.Text = step;
        _bar.Value = percent;
        _percent.Text = $"{percent} %";
    }
}
