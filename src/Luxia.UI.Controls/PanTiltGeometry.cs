using Avalonia;

namespace Luxia.UI.Controls;

/// <summary>
/// Calculs de la grille Pan / Tilt (ERG-003), séparés du contrôle pour être testés sans affichage.
/// Pan à l'horizontale (0 à gauche), Tilt à la verticale (0 en bas) ; toutes les valeurs sont normalisées 0-1.
/// </summary>
public static class PanTiltGeometry
{
    /// <summary>Plus petite zone admise (2 % de la course dans chaque sens).</summary>
    public const double MinZoneSize = 0.02;

    /// <summary>Pas fin de la molette et des flèches (1/1000 de la course ≈ 0,5° sur une lyre à 540°).</summary>
    public const double FineStep = 0.001;

    /// <summary>Position à l'écran d'une visée.</summary>
    public static Point ToScreen(double pan, double tilt, Rect area) =>
        new(area.X + (pan * area.Width), area.Bottom - (tilt * area.Height));

    /// <summary>Visée correspondant à un point de l'écran (bornée à la grille).</summary>
    public static (double Pan, double Tilt) FromScreen(Point point, Rect area) =>
        (Clamp01((point.X - area.X) / Math.Max(area.Width, 1)), Clamp01((area.Bottom - point.Y) / Math.Max(area.Height, 1)));

    /// <summary>Écart d'écran converti en écart de visée (sans bornes).</summary>
    public static (double DPan, double DTilt) ScreenDelta(Vector delta, Rect area) =>
        (delta.X / Math.Max(area.Width, 1), -delta.Y / Math.Max(area.Height, 1));

    /// <summary>Barycentre d'un groupe de visées.</summary>
    public static (double Pan, double Tilt) Centroid(IReadOnlyList<PanTiltTarget> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            return (0.5, 0.5);
        }

        double pan = 0, tilt = 0;
        foreach (var p in points)
        {
            pan += p.Pan;
            tilt += p.Tilt;
        }

        return (pan / points.Count, tilt / points.Count);
    }

    /// <summary>
    /// Déplace un groupe du même écart en <b>gardant sa forme</b> (plusieurs appareils en relatif) : l'écart est
    /// réduit pour qu'aucun appareil ne sorte de la course, plutôt que d'écraser un appareil contre le bord.
    /// </summary>
    public static IReadOnlyList<PanTiltTarget> MoveGroup(IReadOnlyList<PanTiltTarget> points, double dPan, double dTilt)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            return points;
        }

        double panMin = 1, panMax = 0, tiltMin = 1, tiltMax = 0;
        foreach (var p in points)
        {
            panMin = Math.Min(panMin, p.Pan);
            panMax = Math.Max(panMax, p.Pan);
            tiltMin = Math.Min(tiltMin, p.Tilt);
            tiltMax = Math.Max(tiltMax, p.Tilt);
        }

        dPan = Math.Clamp(dPan, -panMin, Math.Max(1 - panMax, -panMin));
        dTilt = Math.Clamp(dTilt, -tiltMin, Math.Max(1 - tiltMax, -tiltMin));
        var moved = new PanTiltTarget[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            moved[i] = p with { Pan = Clamp01(p.Pan + dPan), Tilt = Clamp01(p.Tilt + dTilt) };
        }

        return moved;
    }

    /// <summary>Amène le barycentre du groupe sur la visée donnée, en gardant sa forme (clic dans la grille).</summary>
    public static IReadOnlyList<PanTiltTarget> MoveGroupTo(IReadOnlyList<PanTiltTarget> points, double pan, double tilt)
    {
        var (cPan, cTilt) = Centroid(points);
        return MoveGroup(points, pan - cPan, tilt - cTilt);
    }

    /// <summary>Rectangle à partir de deux coins quelconques (dessin d'une nouvelle zone).</summary>
    public static PanTiltRect FromCorners(double pan1, double tilt1, double pan2, double tilt2) =>
        new(Clamp01(Math.Min(pan1, pan2)), Clamp01(Math.Max(pan1, pan2)), Clamp01(Math.Min(tilt1, tilt2)), Clamp01(Math.Max(tilt1, tilt2)));

    /// <summary>Déplace une zone sans la déformer ni la faire sortir de la grille.</summary>
    public static PanTiltRect MoveZone(PanTiltRect zone, double dPan, double dTilt)
    {
        dPan = Math.Clamp(dPan, -zone.PanMin, 1 - zone.PanMax);
        dTilt = Math.Clamp(dTilt, -zone.TiltMin, 1 - zone.TiltMax);
        return new PanTiltRect(zone.PanMin + dPan, zone.PanMax + dPan, zone.TiltMin + dTilt, zone.TiltMax + dTilt);
    }

    /// <summary>
    /// Redimensionne une zone en tirant une poignée jusqu'à la visée donnée. Le bord tiré s'arrête à
    /// <see cref="MinZoneSize"/> du bord opposé : une zone ne se retourne pas et ne disparaît pas.
    /// </summary>
    public static PanTiltRect ResizeZone(PanTiltRect zone, PanTiltHandle handle, double pan, double tilt)
    {
        pan = Clamp01(pan);
        tilt = Clamp01(tilt);
        var (panMin, panMax, tiltMin, tiltMax) = (zone.PanMin, zone.PanMax, zone.TiltMin, zone.TiltMax);
        if (handle is PanTiltHandle.Left or PanTiltHandle.TopLeft or PanTiltHandle.BottomLeft)
        {
            panMin = Math.Min(pan, panMax - MinZoneSize);
        }

        if (handle is PanTiltHandle.Right or PanTiltHandle.TopRight or PanTiltHandle.BottomRight)
        {
            panMax = Math.Max(pan, panMin + MinZoneSize);
        }

        if (handle is PanTiltHandle.Bottom or PanTiltHandle.BottomLeft or PanTiltHandle.BottomRight)
        {
            tiltMin = Math.Min(tilt, tiltMax - MinZoneSize);
        }

        if (handle is PanTiltHandle.Top or PanTiltHandle.TopLeft or PanTiltHandle.TopRight)
        {
            tiltMax = Math.Max(tilt, tiltMin + MinZoneSize);
        }

        return new PanTiltRect(panMin, panMax, tiltMin, tiltMax);
    }

    /// <summary>Rectangle d'écran d'une zone.</summary>
    public static Rect ZoneToScreen(PanTiltRect zone, Rect area)
    {
        var topLeft = ToScreen(zone.PanMin, zone.TiltMax, area);
        var bottomRight = ToScreen(zone.PanMax, zone.TiltMin, area);
        return new Rect(topLeft, bottomRight);
    }

    /// <summary>Centre d'écran de chacune des 8 poignées d'une zone.</summary>
    public static IEnumerable<(PanTiltHandle Handle, Point Center)> Handles(PanTiltRect zone, Rect area)
    {
        var r = ZoneToScreen(zone, area);
        yield return (PanTiltHandle.TopLeft, r.TopLeft);
        yield return (PanTiltHandle.Top, new Point(r.Center.X, r.Top));
        yield return (PanTiltHandle.TopRight, r.TopRight);
        yield return (PanTiltHandle.Right, new Point(r.Right, r.Center.Y));
        yield return (PanTiltHandle.BottomRight, r.BottomRight);
        yield return (PanTiltHandle.Bottom, new Point(r.Center.X, r.Bottom));
        yield return (PanTiltHandle.BottomLeft, r.BottomLeft);
        yield return (PanTiltHandle.Left, new Point(r.Left, r.Center.Y));
    }

    /// <summary>
    /// Zone prise par un clic : la plus petite qui contient le point, pour qu'une zone dessinée dans une grande (permise)
    /// reste attrapable (essai 1.005.202).
    /// </summary>
    public static PanTiltZoneMarker? ZoneAt(IEnumerable<PanTiltZoneMarker> zones, Rect area, Point point) => zones
        .Where(z => HitTest(z.Area, area, point, 0) == PanTiltHandle.Body)
        .OrderBy(z => z.Area.PanSize * z.Area.TiltSize)
        .FirstOrDefault();

    /// <summary>Partie d'une zone sous un point de l'écran : poignée (à <paramref name="tolerance"/> près), corps ou rien.</summary>
    public static PanTiltHandle HitTest(PanTiltRect zone, Rect area, Point point, double tolerance)
    {
        foreach (var (handle, center) in Handles(zone, area))
        {
            if (Math.Abs(point.X - center.X) <= tolerance && Math.Abs(point.Y - center.Y) <= tolerance)
            {
                return handle;
            }
        }

        return ZoneToScreen(zone, area).Contains(point) ? PanTiltHandle.Body : PanTiltHandle.None;
    }

    /// <summary>Valeur normalisée exprimée en degrés sur une course donnée (F4 : degrés pour Pan / Tilt).</summary>
    public static double ToDegrees(double value, double rangeDegrees) => value * rangeDegrees;

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);
}
