namespace Luxia.Midi;

/// <summary>Messages des LED des pads (doc 18b §4).</summary>
public sealed record PadLeds
{
    /// <summary>Pads RGB (MK2) : la vélocité est un indice de palette, choisi d'après la couleur de la scène (MIDI-010).</summary>
    public bool Rgb { get; init; }

    /// <summary>Scène disponible (MK1 : jaune ; MK2 : couleur de la scène, faible luminosité).</summary>
    public LedSpec Available { get; init; } = new(0, 5);

    /// <summary>Scène active (MK1 : vert ; MK2 : pleine luminosité).</summary>
    public LedSpec Active { get; init; } = new(0, 1);

    /// <summary>Scène en fondu d'entrée (MK1 : vert clignotant ; MK2 : pulsation).</summary>
    public LedSpec FadingIn { get; init; } = new(0, 2);

    /// <summary>Palette des pads RGB : quelques couleurs franches (indice → couleur).</summary>
    public IReadOnlyList<PaletteColor> Palette { get; init; } = [];
}
