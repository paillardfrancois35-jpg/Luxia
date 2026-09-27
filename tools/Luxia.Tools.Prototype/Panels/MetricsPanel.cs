using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Luxia.Tools.Prototype.Panels;

/// <summary>
/// Panneau Mesures (ERG-006) : images par seconde de la fenêtre, demandes par seconde des composants, mémoire.
/// Sert à vérifier qu'un glisser sur la grille ou le sélecteur reste fluide sous Avalonia 12 avec Dock.
/// </summary>
internal static class MetricsPanel
{
    public static Control Create(DemoState state)
    {
        var text = new TextBlock { Margin = new Thickness(12, 36, 12, 12), FontSize = 13, LineHeight = 22 };
        var frames = 0;
        var lastRequests = state.RequestCount;
        var peakFps = 0;
        var watch = Stopwatch.StartNew();
        DispatcherTimer? timer = null;
        TopLevel? top = null;

        void OnFrame(TimeSpan _)
        {
            frames++;
            top?.RequestAnimationFrame(OnFrame);
        }

        text.AttachedToVisualTree += (_, _) =>
        {
            top = TopLevel.GetTopLevel(text);
            top?.RequestAnimationFrame(OnFrame);
            timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) =>
            {
                var seconds = Math.Max(watch.Elapsed.TotalSeconds, 0.001);
                var fps = (int)Math.Round(frames / seconds);
                peakFps = Math.Max(peakFps, fps);
                var requests = (state.RequestCount - lastRequests) / seconds;
                using var process = Process.GetCurrentProcess();
                text.Text = string.Create(
                    CultureInfo.CurrentCulture,
                    $"Images par seconde : {fps} (pointe {peakFps})\nDemandes des composants : {requests:0} / s\nMémoire gérée : {GC.GetTotalMemory(false) / 1048576.0:0.0} Mo\nMémoire du processus : {process.WorkingSet64 / 1048576.0:0} Mo\nFenêtres ouvertes : {WindowCount()}");
                frames = 0;
                lastRequests = state.RequestCount;
                watch.Restart();
            });
            timer.Start();
        };
        text.DetachedFromVisualTree += (_, _) =>
        {
            timer?.Stop();
            top = null;
        };
        return new ScrollViewer { Content = text };
    }

    private static int WindowCount() =>
        Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.Count
            : 1;
}
