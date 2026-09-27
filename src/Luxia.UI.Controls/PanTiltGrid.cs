using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Luxia.UI.Controls;

/// <summary>
/// Grille Pan / Tilt (ERG-003, doc 60 §5) : un point par appareil, les zones interdites ou permises (F7) dessinées
/// par-dessus. Comme le <see cref="Fader"/>, elle <b>demande</b> les visées (<see cref="MoveRequested"/>) et affiche
/// ensuite celles que le modèle de vue a retenues.
/// <para>Visée : un clic amène la sélection sous le curseur (plusieurs appareils : leur centre, en gardant leurs
/// écarts), puis le glisser la suit ; Maj + glisser = réglage fin relatif (÷10, sans saut) ; molette = Tilt fin,
/// Maj + molette = Pan fin, Ctrl = ×10 ; flèches = pas fin.</para>
/// <para>Zones (<see cref="IsZoneEditing"/>) : glisser dans le vide dessine une zone, le corps la déplace, les
/// 8 poignées la redimensionnent, Suppr la retire.</para>
/// </summary>
public sealed class PanTiltGrid : Control
{
    /// <summary>Points de visée des appareils.</summary>
    public static readonly StyledProperty<IReadOnlyList<PanTiltMarker>?> MarkersProperty =
        AvaloniaProperty.Register<PanTiltGrid, IReadOnlyList<PanTiltMarker>?>(nameof(Markers));

    /// <summary>Zones dessinées.</summary>
    public static readonly StyledProperty<IReadOnlyList<PanTiltZoneMarker>?> ZonesProperty =
        AvaloniaProperty.Register<PanTiltGrid, IReadOnlyList<PanTiltZoneMarker>?>(nameof(Zones));

    /// <summary>Course du Pan en degrés (affichage).</summary>
    public static readonly StyledProperty<double> PanRangeProperty =
        AvaloniaProperty.Register<PanTiltGrid, double>(nameof(PanRange), 540);

    /// <summary>Course du Tilt en degrés (affichage).</summary>
    public static readonly StyledProperty<double> TiltRangeProperty =
        AvaloniaProperty.Register<PanTiltGrid, double>(nameof(TiltRange), 270);

    /// <summary>Mode d'édition des zones : la souris agit sur les zones, plus sur les visées.</summary>
    public static readonly StyledProperty<bool> IsZoneEditingProperty =
        AvaloniaProperty.Register<PanTiltGrid, bool>(nameof(IsZoneEditing));

    /// <summary>Zone sélectionnée (poignées affichées en édition).</summary>
    public static readonly StyledProperty<string?> SelectedZoneIdProperty =
        AvaloniaProperty.Register<PanTiltGrid, string?>(nameof(SelectedZoneId));

    private const double Padding = 18;
    private const double HandleSize = 8;

    private static readonly IBrush Background = new SolidColorBrush(Color.Parse("#0D1117"));
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 1);
    private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.Parse("#1B222C")), 1);
    private static readonly IPen AxisPen = new Pen(new SolidColorBrush(Color.Parse("#30363D")), 1, new DashStyle([4, 3], 0));
    private static readonly IBrush SecondaryText = new SolidColorBrush(Color.Parse("#8B949E"));
    private static readonly IBrush ForbiddenFill = new SolidColorBrush(Color.Parse("#F85149"), 0.22);
    private static readonly IPen ForbiddenPen = new Pen(new SolidColorBrush(Color.Parse("#F85149")), 1.5);
    private static readonly IBrush OutsideAllowedFill = new SolidColorBrush(Colors.Black, 0.55);
    private static readonly IPen AllowedPen = new Pen(new SolidColorBrush(Color.Parse("#3FB950")), 1.5, new DashStyle([5, 3], 0));
    private static readonly IPen SelectedZonePen = new Pen(new SolidColorBrush(Color.Parse("#58A6FF")), 2);
    private static readonly IPen DrawingPen = new Pen(new SolidColorBrush(Color.Parse("#58A6FF")), 1, new DashStyle([3, 3], 0));
    private static readonly IPen SelectedMarkerPen = new Pen(Brushes.White, 2);
    private static readonly IPen MarkerPen = new Pen(new SolidColorBrush(Color.Parse("#6E7681")), 1);
    private static readonly IPen CrossPen = new Pen(new SolidColorBrush(Color.Parse("#58A6FF"), 0.5), 1);
    private static readonly IPen FocusPen = new Pen(new SolidColorBrush(Color.Parse("#C9D1D9")), 1, new DashStyle([2, 2], 0));

    // Glisser en cours : visées de la sélection au moment de l'appui (et après le saut éventuel).
    private IReadOnlyList<PanTiltTarget>? _dragOrigin;
    private Point _dragStart;
    private bool _fineDrag;

    // Édition de zone en cours.
    private PanTiltZoneMarker? _zoneOrigin;
    private PanTiltHandle _zoneHandle;
    private (double Pan, double Tilt)? _drawStart;
    private PanTiltRect? _drawing;

    static PanTiltGrid()
    {
        AffectsRender<PanTiltGrid>(MarkersProperty, ZonesProperty, PanRangeProperty, TiltRangeProperty, IsZoneEditingProperty, SelectedZoneIdProperty);
        FocusableProperty.OverrideDefaultValue<PanTiltGrid>(true);
    }

    /// <summary>Levé quand l'utilisateur demande de nouvelles visées pour les appareils sélectionnés.</summary>
    public event EventHandler<IReadOnlyList<PanTiltTarget>>? MoveRequested;

    /// <summary>Levé quand l'utilisateur dessine, déplace ou redimensionne une zone.</summary>
    public event EventHandler<PanTiltZoneRequest>? ZoneRequested;

    /// <summary>Levé quand l'utilisateur sélectionne une zone (nul : aucune).</summary>
    public event EventHandler<string?>? ZoneSelected;

    /// <summary>Levé quand l'utilisateur demande de retirer une zone (Suppr).</summary>
    public event EventHandler<string>? ZoneDeleteRequested;

    /// <inheritdoc cref="MarkersProperty"/>
    public IReadOnlyList<PanTiltMarker>? Markers
    {
        get => GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    /// <inheritdoc cref="ZonesProperty"/>
    public IReadOnlyList<PanTiltZoneMarker>? Zones
    {
        get => GetValue(ZonesProperty);
        set => SetValue(ZonesProperty, value);
    }

    /// <inheritdoc cref="PanRangeProperty"/>
    public double PanRange
    {
        get => GetValue(PanRangeProperty);
        set => SetValue(PanRangeProperty, value);
    }

    /// <inheritdoc cref="TiltRangeProperty"/>
    public double TiltRange
    {
        get => GetValue(TiltRangeProperty);
        set => SetValue(TiltRangeProperty, value);
    }

    /// <inheritdoc cref="IsZoneEditingProperty"/>
    public bool IsZoneEditing
    {
        get => GetValue(IsZoneEditingProperty);
        set => SetValue(IsZoneEditingProperty, value);
    }

    /// <inheritdoc cref="SelectedZoneIdProperty"/>
    public string? SelectedZoneId
    {
        get => GetValue(SelectedZoneIdProperty);
        set => SetValue(SelectedZoneIdProperty, value);
    }

    private Rect Area => new Rect(Bounds.Size).Deflate(Padding);

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var area = Area;
        if (area.Width <= 0 || area.Height <= 0)
        {
            return;
        }

        context.FillRectangle(Background, new Rect(Bounds.Size), 4);
        RenderGrid(context, area);
        RenderZones(context, area);
        RenderMarkers(context, area);
        context.DrawRectangle(BorderPen, area);
        if (IsFocused)
        {
            context.DrawRectangle(FocusPen, new Rect(Bounds.Size).Deflate(2), 3);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        Focus();
        var point = e.GetPosition(this);
        if (IsZoneEditing)
        {
            BeginZoneEdit(point);
        }
        else
        {
            BeginAim(point, (e.KeyModifiers & KeyModifiers.Shift) != 0);
        }

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);
        var area = Area;
        if (_dragOrigin is { } origin)
        {
            var (dPan, dTilt) = PanTiltGeometry.ScreenDelta(point - _dragStart, area);
            var scale = _fineDrag ? 0.1 : 1;
            Request(PanTiltGeometry.MoveGroup(origin, dPan * scale, dTilt * scale));
        }
        else if (_zoneOrigin is { } zone)
        {
            var (pan, tilt) = PanTiltGeometry.FromScreen(point, area);
            var area2 = _zoneHandle == PanTiltHandle.Body
                ? MoveZoneBy(zone.Area, point - _dragStart, area)
                : PanTiltGeometry.ResizeZone(zone.Area, _zoneHandle, pan, tilt);
            ZoneRequested?.Invoke(this, new PanTiltZoneRequest(zone.Id, area2));
        }
        else if (_drawStart is { } start)
        {
            var (pan, tilt) = PanTiltGeometry.FromScreen(point, area);
            _drawing = PanTiltGeometry.FromCorners(start.Pan, start.Tilt, pan, tilt);
            InvalidateVisual();
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);
        if (_drawing is { } drawn && drawn.PanSize >= PanTiltGeometry.MinZoneSize && drawn.TiltSize >= PanTiltGeometry.MinZoneSize)
        {
            ZoneRequested?.Invoke(this, new PanTiltZoneRequest(null, drawn));
        }

        _dragOrigin = null;
        _zoneOrigin = null;
        _drawStart = null;
        _drawing = null;
        e.Pointer.Capture(null);
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerWheelChanged(e);
        var step = PanTiltGeometry.FineStep * ((e.KeyModifiers & KeyModifiers.Control) != 0 ? 10 : 1);
        var panAxis = e.Delta.X != 0 || (e.KeyModifiers & KeyModifiers.Shift) != 0;
        var sign = Math.Sign(e.Delta.Y != 0 ? e.Delta.Y : e.Delta.X);
        Nudge(panAxis ? sign * step : 0, panAxis ? 0 : sign * step);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (IsZoneEditing && e.Key is Key.Delete or Key.Back && SelectedZoneId is { } zoneId)
        {
            ZoneDeleteRequested?.Invoke(this, zoneId);
            e.Handled = true;
            return;
        }

        var step = PanTiltGeometry.FineStep * ((e.KeyModifiers & KeyModifiers.Control) != 0 ? 10 : 1);
        (double, double)? delta = e.Key switch
        {
            Key.Left => (-step, 0),
            Key.Right => (step, 0),
            Key.Up => (0, step),
            Key.Down => (0, -step),
            _ => null,
        };
        if (delta is { } d)
        {
            Nudge(d.Item1, d.Item2);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private static PanTiltRect MoveZoneBy(PanTiltRect zone, Vector screenDelta, Rect area)
    {
        var (dPan, dTilt) = PanTiltGeometry.ScreenDelta(screenDelta, area);
        return PanTiltGeometry.MoveZone(zone, dPan, dTilt);
    }

    private List<PanTiltTarget> SelectedTargets()
    {
        var targets = new List<PanTiltTarget>();
        foreach (var marker in Markers ?? [])
        {
            if (marker.IsSelected)
            {
                targets.Add(new PanTiltTarget(marker.Id, marker.Pan, marker.Tilt));
            }
        }

        return targets;
    }

    private void BeginAim(Point point, bool fine)
    {
        var selected = SelectedTargets();
        if (selected.Count == 0)
        {
            return;
        }

        _dragStart = point;
        _fineDrag = fine;
        if (fine)
        {
            _dragOrigin = selected;
            return;
        }

        var (pan, tilt) = PanTiltGeometry.FromScreen(point, Area);
        _dragOrigin = PanTiltGeometry.MoveGroupTo(selected, pan, tilt);
        Request(_dragOrigin);
    }

    private void BeginZoneEdit(Point point)
    {
        var area = Area;
        var zones = Zones ?? [];
        _dragStart = point;

        // D'abord les poignées de la zone sélectionnée, puis le corps des zones (la dernière dessinée est au-dessus).
        var selected = zones.FirstOrDefault(z => z.Id == SelectedZoneId);
        if (selected is not null)
        {
            var hit = PanTiltGeometry.HitTest(selected.Area, area, point, HandleSize);
            if (hit != PanTiltHandle.None)
            {
                _zoneOrigin = selected;
                _zoneHandle = hit;
                return;
            }
        }

        for (var i = zones.Count - 1; i >= 0; i--)
        {
            if (PanTiltGeometry.HitTest(zones[i].Area, area, point, 0) == PanTiltHandle.Body)
            {
                _zoneOrigin = zones[i];
                _zoneHandle = PanTiltHandle.Body;
                ZoneSelected?.Invoke(this, zones[i].Id);
                return;
            }
        }

        ZoneSelected?.Invoke(this, null);
        _drawStart = PanTiltGeometry.FromScreen(point, area);
    }

    private void Nudge(double dPan, double dTilt)
    {
        var selected = SelectedTargets();
        if (selected.Count > 0 && !IsZoneEditing)
        {
            Request(PanTiltGeometry.MoveGroup(selected, dPan, dTilt));
        }
    }

    private void Request(IReadOnlyList<PanTiltTarget> targets) => MoveRequested?.Invoke(this, targets);

    private void RenderGrid(DrawingContext context, Rect area)
    {
        // Une ligne tous les huitièmes, les axes au milieu, les degrés aux quarts.
        for (var i = 1; i < 8; i++)
        {
            var x = area.X + (area.Width * i / 8);
            var y = area.Y + (area.Height * i / 8);
            var pen = i == 4 ? AxisPen : GridPen;
            context.DrawLine(pen, new Point(x, area.Top), new Point(x, area.Bottom));
            context.DrawLine(pen, new Point(area.Left, y), new Point(area.Right, y));
        }

        for (var i = 0; i <= 4; i++)
        {
            var value = i / 4.0;
            var pan = Text($"{PanTiltGeometry.ToDegrees(value, PanRange):0}°", 10, SecondaryText);
            context.DrawText(pan, new Point(area.X + (area.Width * value) - (pan.Width / 2), area.Bottom + 3));
            if (i is > 0 and < 4)
            {
                var tilt = Text($"{PanTiltGeometry.ToDegrees(value, TiltRange):0}°", 10, SecondaryText);
                context.DrawText(tilt, new Point(area.X - tilt.Width - 3 > 0 ? area.X - tilt.Width - 3 : 1, area.Bottom - (area.Height * value) - (tilt.Height / 2)));
            }
        }
    }

    private void RenderZones(DrawingContext context, Rect area)
    {
        foreach (var zone in Zones ?? [])
        {
            var r = PanTiltGeometry.ZoneToScreen(zone.Area, area);
            if (zone.Kind == PanTiltZoneKind.Allowed)
            {
                // L'extérieur de la zone permise est assombri : c'est là que l'appareil n'ira pas.
                context.FillRectangle(OutsideAllowedFill, new Rect(area.Left, area.Top, area.Width, r.Top - area.Top));
                context.FillRectangle(OutsideAllowedFill, new Rect(area.Left, r.Bottom, area.Width, area.Bottom - r.Bottom));
                context.FillRectangle(OutsideAllowedFill, new Rect(area.Left, r.Top, r.Left - area.Left, r.Height));
                context.FillRectangle(OutsideAllowedFill, new Rect(r.Right, r.Top, area.Right - r.Right, r.Height));
                context.DrawRectangle(AllowedPen, r);
            }
            else
            {
                context.FillRectangle(ForbiddenFill, r);
                context.DrawRectangle(ForbiddenPen, r);
            }

            var name = Text(zone.Name, 11, Brushes.White);
            context.DrawText(name, new Point(r.X + 4, r.Y + 2));
        }

        if (IsZoneEditing && Zones?.FirstOrDefault(z => z.Id == SelectedZoneId) is { } selected)
        {
            context.DrawRectangle(SelectedZonePen, PanTiltGeometry.ZoneToScreen(selected.Area, area));
            foreach (var (_, center) in PanTiltGeometry.Handles(selected.Area, area))
            {
                var handle = new Rect(center.X - (HandleSize / 2), center.Y - (HandleSize / 2), HandleSize, HandleSize);
                context.FillRectangle(Brushes.White, handle);
                context.DrawRectangle(SelectedZonePen, handle);
            }
        }

        if (_drawing is { } drawing)
        {
            context.DrawRectangle(DrawingPen, PanTiltGeometry.ZoneToScreen(drawing, area));
        }
    }

    private void RenderMarkers(DrawingContext context, Rect area)
    {
        var markers = Markers ?? [];
        var selectedCount = 0;
        PanTiltMarker? single = null;
        foreach (var marker in markers)
        {
            if (marker.IsSelected)
            {
                selectedCount++;
                single = marker;
            }
        }

        // Réticule sur l'appareil quand un seul est sélectionné : la lecture de la visée est immédiate.
        if (selectedCount == 1 && single is not null)
        {
            var p = PanTiltGeometry.ToScreen(single.Pan, single.Tilt, area);
            context.DrawLine(CrossPen, new Point(area.Left, p.Y), new Point(area.Right, p.Y));
            context.DrawLine(CrossPen, new Point(p.X, area.Top), new Point(p.X, area.Bottom));
        }

        // Les appareils non sélectionnés d'abord, estompés, pour que la sélection reste au-dessus.
        foreach (var pass in new[] { false, true })
        {
            foreach (var marker in markers)
            {
                if (marker.IsSelected != pass)
                {
                    continue;
                }

                var p = PanTiltGeometry.ToScreen(marker.Pan, marker.Tilt, area);
                var fill = new SolidColorBrush(marker.Color, marker.IsSelected ? 1 : 0.45);
                context.DrawEllipse(fill, marker.IsSelected ? SelectedMarkerPen : MarkerPen, p, 7, 7);
                var label = Text(marker.Label, 11, marker.IsSelected ? Brushes.White : SecondaryText);
                context.DrawText(label, new Point(p.X + 10, p.Y - (label.Height / 2)));
            }
        }

        var readout = selectedCount switch
        {
            0 => IsZoneEditing ? "Zones : glisser pour dessiner" : "Aucun appareil sélectionné",
            1 => string.Create(
                CultureInfo.CurrentCulture,
                $"{single!.Label} · Pan {PanTiltGeometry.ToDegrees(single.Pan, PanRange):0.0}° · Tilt {PanTiltGeometry.ToDegrees(single.Tilt, TiltRange):0.0}°"),
            _ => string.Create(CultureInfo.CurrentCulture, $"{selectedCount} appareils (en relatif)"),
        };
        var text = Text(readout, 11, SecondaryText);
        context.DrawText(text, new Point(area.X, Math.Max(area.Y - text.Height - 2, 0)));
    }

    private static FormattedText Text(string value, double size, IBrush brush) =>
        new(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, size, brush);
}
