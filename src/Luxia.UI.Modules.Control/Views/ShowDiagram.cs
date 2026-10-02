using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Luxia.UI.Modules.Control.Sequencing;

namespace Luxia.UI.Modules.Control.Views;

/// <summary>
/// Diagramme d'un show, dessiné automatiquement (Q45, maquette 12) : les étapes en rangées (distance depuis les étapes initiales),
/// les transitions vers le bas en traits pleins avec leur condition, les retours en arrière en pointillés par un couloir à droite ;
/// double bord = étape initiale, fond coloré = étape active pendant l'essai. Un clic sur une étape le signale (la fenêtre montre sa
/// carte).
/// </summary>
public sealed class ShowDiagram : Avalonia.Controls.Control
{
    /// <summary>Étapes.</summary>
    public static readonly StyledProperty<IReadOnlyList<DiagramNode>?> NodesProperty = AvaloniaProperty.Register<ShowDiagram, IReadOnlyList<DiagramNode>?>(nameof(Nodes));

    /// <summary>Liaisons.</summary>
    public static readonly StyledProperty<IReadOnlyList<DiagramEdge>?> EdgesProperty = AvaloniaProperty.Register<ShowDiagram, IReadOnlyList<DiagramEdge>?>(nameof(Edges));

    private const double CellWidth = 250;
    private const double RowHeight = 118;
    private const double BoxWidth = 180;
    private const double BoxHeight = 46;
    private const double Pad = 24;
    private static readonly Color Accent = Color.Parse("#39C5CF");

    static ShowDiagram()
    {
        AffectsRender<ShowDiagram>(NodesProperty, EdgesProperty);
        AffectsMeasure<ShowDiagram>(NodesProperty, EdgesProperty);
    }

    /// <summary>Étapes.</summary>
    public IReadOnlyList<DiagramNode>? Nodes
    {
        get => GetValue(NodesProperty);
        set => SetValue(NodesProperty, value);
    }

    /// <summary>Liaisons.</summary>
    public IReadOnlyList<DiagramEdge>? Edges
    {
        get => GetValue(EdgesProperty);
        set => SetValue(EdgesProperty, value);
    }

    /// <summary>Une étape a été cliquée.</summary>
    public event EventHandler<string>? NodeClicked;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var nodes = Nodes ?? [];
        if (nodes.Count == 0)
        {
            return new Size(300, 120);
        }

        var columns = nodes.GroupBy(n => n.Row).Max(g => g.Count());
        var rows = nodes.Max(n => n.Row) + 1;
        var backs = (Edges ?? []).Count(IsBack);
        return new Size((Pad * 2) + (columns * CellWidth) + 40 + (backs * 14), (Pad * 2) + (rows * RowHeight));
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var nodes = Nodes ?? [];
        var edges = Edges ?? [];
        context.FillRectangle(new SolidColorBrush(Color.Parse("#161B22")), new Rect(Bounds.Size), 8);
        if (nodes.Count == 0)
        {
            context.DrawText(Text("Aucune étape : « + étape » pour commencer.", 13, "#8B949E"), new Point(Pad, Pad));
            return;
        }

        var boxes = Layout(nodes);
        var line = new Pen(new SolidColorBrush(Color.Parse("#8B949E")), 1.5);
        var dashed = new Pen(new SolidColorBrush(Color.Parse("#8B949E")), 1.5, new DashStyle([4, 3], 0));
        var tick = new Pen(new SolidColorBrush(Color.Parse("#E6EDF3")), 2);
        var corridor = boxes.Values.Max(b => b.Right) + 30;
        var placed = new List<Rect>();
        foreach (var edge in edges)
        {
            if (!boxes.TryGetValue(edge.From, out var a) || !boxes.TryGetValue(edge.To, out var b))
            {
                continue;
            }

            if (IsBack(edge))
            {
                // Retour en arrière (ou vers soi) : pointillés par un couloir à droite.
                var start = new Point(a.Right, a.Center.Y + 8);
                var end = new Point(b.Right, b.Center.Y - 8);
                var x = corridor;
                corridor += 14;
                context.DrawGeometry(null, dashed, Polyline(start, new Point(x, start.Y), new Point(x, end.Y), end));
                Arrow(context, end, left: true);
                Label(context, edge.Label, new Point(x + 4, ((start.Y + end.Y) / 2) - 8), placed);
            }
            else if (Row(edge.From) == Row(edge.To))
            {
                // Même rangée : par en dessous des étapes (le libellé ne chevauche aucune étape).
                var start = new Point(a.Center.X + 20, a.Bottom);
                var end = new Point(b.Center.X - 20, b.Bottom);
                var under = a.Bottom + 18;
                context.DrawGeometry(null, line, Polyline(start, new Point(start.X, under), new Point(end.X, under), end));
                Arrow(context, end, left: false, up: true);
                var mid = (start.X + end.X) / 2;
                context.DrawLine(tick, new Point(mid, under - 7), new Point(mid, under + 7));
                Label(context, edge.Label, new Point(Math.Min(start.X, end.X) + 6, under + 4), placed);
            }
            else
            {
                // Vers le bas : coude au milieu de l'espace entre les rangées, trait de transition sur la descente.
                var start = new Point(a.Center.X, a.Bottom);
                var end = new Point(b.Center.X, b.Top);
                var midY = a.Bottom + ((RowHeight - BoxHeight) / 2) - 6;
                context.DrawGeometry(null, line, Polyline(start, new Point(start.X, midY), new Point(end.X, midY), end));
                Arrow(context, end, left: false, down: true);
                var tickY = midY + ((end.Y - midY) / 2);
                context.DrawLine(tick, new Point(end.X - 7, tickY), new Point(end.X + 7, tickY));
                Label(context, edge.Label, new Point(end.X + 10, tickY - 8), placed);
            }
        }

        foreach (var node in nodes)
        {
            var box = boxes[node.Id];
            var fill = node.Active ? new SolidColorBrush(Color.FromArgb(220, Accent.R, Accent.G, Accent.B)) : new SolidColorBrush(Color.Parse("#21262D"));
            context.FillRectangle(fill, box, 6);
            context.DrawRectangle(new Pen(new SolidColorBrush(node.Active ? Accent : Color.Parse("#30363D")), 1.5), box, 6);
            if (node.Initial)
            {
                context.DrawRectangle(new Pen(new SolidColorBrush(Color.Parse("#8B949E")), 1.5), box.Inflate(4), 8);
            }

            var dark = node.Active ? "#0D1117" : null;
            using (context.PushClip(box.Deflate(4)))
            {
                context.DrawText(Text(node.Id, 13, dark ?? "#8B949E", true), new Point(box.X + 10, box.Y + 13));
                context.DrawText(Text((node.Macro ? "▣ " : string.Empty) + node.Name, 14, dark ?? "#E6EDF3", true), new Point(box.X + 44, box.Y + 13));
            }
        }

        context.DrawText(Text("Double bord = étape initiale · fond coloré = étape active à l'essai · pointillés = retour en arrière · ⏱ = attend une frontière musicale", 11, "#8B949E"), new Point(Pad, Bounds.Height - 18));
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        var point = e.GetPosition(this);
        foreach (var (id, box) in Layout(Nodes ?? []))
        {
            if (box.Contains(point))
            {
                NodeClicked?.Invoke(this, id);
                e.Handled = true;
                return;
            }
        }
    }

    private bool IsBack(DiagramEdge edge) => Row(edge.To) < Row(edge.From) || edge.To == edge.From;

    private int Row(string id) => (Nodes ?? []).FirstOrDefault(n => n.Id == id)?.Row ?? 0;

    private static Dictionary<string, Rect> Layout(IReadOnlyList<DiagramNode> nodes)
    {
        var result = new Dictionary<string, Rect>(StringComparer.Ordinal);
        if (nodes.Count == 0)
        {
            return result;
        }

        var counts = nodes.GroupBy(n => n.Row).ToDictionary(g => g.Key, g => g.Count());
        var columns = counts.Values.Max();
        foreach (var node in nodes)
        {
            var offset = (columns - counts[node.Row]) / 2.0;
            var x = Pad + ((node.Column + offset) * CellWidth) + ((CellWidth - BoxWidth) / 2);
            var y = Pad + (node.Row * RowHeight);
            result[node.Id] = new Rect(x, y, BoxWidth, BoxHeight);
        }

        return result;
    }

    private static StreamGeometry Polyline(params Point[] points)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(points[0], false);
        foreach (var point in points.Skip(1))
        {
            ctx.LineTo(point);
        }

        ctx.EndFigure(false);
        return geometry;
    }

    private static void Arrow(DrawingContext context, Point tip, bool left, bool down = false, bool up = false)
    {
        var brush = new SolidColorBrush(Color.Parse("#8B949E"));
        var geometry = down
            ? Polyline(new Point(tip.X - 5, tip.Y - 8), tip, new Point(tip.X + 5, tip.Y - 8))
            : up
                ? Polyline(new Point(tip.X - 5, tip.Y + 8), tip, new Point(tip.X + 5, tip.Y + 8))
            : left
                ? Polyline(new Point(tip.X + 8, tip.Y - 5), tip, new Point(tip.X + 8, tip.Y + 5))
                : Polyline(new Point(tip.X - 8, tip.Y - 5), tip, new Point(tip.X - 8, tip.Y + 5));
        context.DrawGeometry(null, new Pen(brush, 1.5), geometry);
    }

    // Les libellés ne se chevauchent pas : décalés vers le bas tant qu'ils touchent un libellé déjà posé.
    private static void Label(DrawingContext context, string text, Point at, List<Rect> placed)
    {
        var formatted = Text(text.Length > 34 ? text[..33] + "…" : text, 11, "#C9D1D9");
        var rect = new Rect(at, new Size(formatted.Width, formatted.Height));
        while (placed.Any(r => r.Intersects(rect)))
        {
            rect = rect.Translate(new Vector(0, formatted.Height + 1));
        }

        placed.Add(rect);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(200, 22, 27, 34)), rect.Inflate(1));
        context.DrawText(formatted, rect.Position);
    }

    private static FormattedText Text(string text, double size, string color, bool bold = false) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(FontFamily.Default, FontStyle.Normal, bold ? FontWeight.SemiBold : FontWeight.Normal), size, new SolidColorBrush(Color.Parse(color)));
}
