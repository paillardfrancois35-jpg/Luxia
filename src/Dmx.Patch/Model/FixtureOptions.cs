namespace Dmx.Patch.Model;

/// <summary>Options par appareil patché (INST-021) : montage physique différent du modèle standard.</summary>
public sealed record FixtureOptions
{
    /// <summary>Inverse le sens du Pan.</summary>
    public bool InvertPan { get; init; }

    /// <summary>Inverse le sens du Tilt.</summary>
    public bool InvertTilt { get; init; }

    /// <summary>Échange Pan et Tilt (appareil monté sur le côté).</summary>
    public bool SwapPanTilt { get; init; }

    /// <summary>Décalage de Pan, en degrés (recalage du zéro mécanique).</summary>
    public double PanOffsetDegrees { get; init; }

    /// <summary>Borne basse de Pan propre à cet appareil (degrés) ; <c>null</c> = amplitude du modèle.</summary>
    public double? PanMinDegrees { get; init; }

    /// <summary>Borne haute de Pan propre à cet appareil (degrés) ; <c>null</c> = amplitude du modèle.</summary>
    public double? PanMaxDegrees { get; init; }

    /// <summary>Borne basse de Tilt propre à cet appareil (degrés) ; <c>null</c> = amplitude du modèle.</summary>
    public double? TiltMinDegrees { get; init; }

    /// <summary>Borne haute de Tilt propre à cet appareil (degrés) ; <c>null</c> = amplitude du modèle.</summary>
    public double? TiltMaxDegrees { get; init; }
}
