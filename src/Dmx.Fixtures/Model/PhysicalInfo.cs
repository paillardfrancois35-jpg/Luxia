namespace Dmx.Fixtures.Model;

/// <summary>Caractéristiques physiques (doc 12 §2.1).</summary>
public sealed record PhysicalInfo
{
    /// <summary>Type de source (« LED RGB », « LED RGBW », « LED UV »…).</summary>
    public string? SourceType { get; init; }

    /// <summary>Angle du faisceau, en degrés.</summary>
    public double? BeamAngle { get; init; }

    /// <summary>Amplitude Pan, en degrés (affichage en degrés, GEN-021).</summary>
    public double? PanRange { get; init; }

    /// <summary>Amplitude Tilt, en degrés.</summary>
    public double? TiltRange { get; init; }

    /// <summary>Puissance, en watts.</summary>
    public double? Power { get; init; }

    /// <summary>Temps de chauffe (fumée), en secondes.</summary>
    public double? WarmupSeconds { get; init; }
}
