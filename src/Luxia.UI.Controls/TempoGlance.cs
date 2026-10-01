using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Luxia.UI.Controls;

/// <summary>
/// Rappel du tempo en lecture seule (essai P7) : le BPM et les voyants des quatre temps de la mesure (le premier en orange).
/// Même présentation dans le bloc BPM, l'écran Audio et le Simulateur : un état se voit pareil partout. Largeurs fixes : rien ne
/// bouge quand les valeurs changent.
/// </summary>
public sealed class TempoGlance : UserControl
{
    /// <summary>Texte du tempo (par exemple « 128 »).</summary>
    public static readonly StyledProperty<string> BpmTextProperty = AvaloniaProperty.Register<TempoGlance, string>(nameof(BpmText), "—");

    /// <summary>Temps en cours dans la mesure (1 à 4 ; 0 = aucun voyant allumé).</summary>
    public static readonly StyledProperty<int> BeatInBarProperty = AvaloniaProperty.Register<TempoGlance, int>(nameof(BeatInBar));

    private static readonly IBrush Off = new SolidColorBrush(Color.Parse("#30363D"));
    private static readonly IBrush On = new SolidColorBrush(Color.Parse("#3FB950"));
    private static readonly IBrush First = new SolidColorBrush(Color.Parse("#F0883E"));

    private readonly TextBlock _bpm = new() { FontSize = 18, FontWeight = FontWeight.SemiBold, Width = 56, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
    private readonly Ellipse[] _dots = new Ellipse[4];

    /// <summary>Crée le rappel.</summary>
    public TempoGlance()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(_bpm);
        row.Children.Add(new TextBlock { Text = "BPM", Opacity = 0.6, VerticalAlignment = VerticalAlignment.Center });
        for (var i = 0; i < 4; i++)
        {
            _dots[i] = new Ellipse { Width = 11, Height = 11, Fill = Off, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(i == 0 ? 6 : 0, 0, 0, 0) };
            row.Children.Add(_dots[i]);
        }

        Content = row;
        ToolTip.SetTip(this, "Tempo et temps de la mesure (le 1 en orange)");
        Refresh();
    }

    /// <summary>Texte du tempo.</summary>
    public string BpmText
    {
        get => GetValue(BpmTextProperty);
        set => SetValue(BpmTextProperty, value);
    }

    /// <summary>Temps en cours dans la mesure.</summary>
    public int BeatInBar
    {
        get => GetValue(BeatInBarProperty);
        set => SetValue(BeatInBarProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BpmTextProperty || change.Property == BeatInBarProperty)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        _bpm.Text = BpmText;
        for (var i = 0; i < 4; i++)
        {
            _dots[i].Fill = BeatInBar == i + 1 ? (i == 0 ? First : On) : Off;
        }
    }
}
