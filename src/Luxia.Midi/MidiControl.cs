using System.Globalization;

namespace Luxia.Midi;

/// <summary>Famille de contrôle physique.</summary>
public enum MidiControlKind
{
    /// <summary>Pad de la grille (colonne, ligne ; ligne 1 = en haut).</summary>
    Pad,

    /// <summary>Bouton rond du bas (1 à 8).</summary>
    Bottom,

    /// <summary>Bouton rond de droite (1 à 8, de haut en bas).</summary>
    Right,

    /// <summary>Fader (1 à 9, 9 = master).</summary>
    Fader,
}

/// <summary>Contrôle physique d'un contrôleur, indépendant du modèle (« pad 3 2 », « bas 1 », « droite 4 », « fader 9 »).</summary>
/// <param name="Kind">Famille.</param>
/// <param name="X">Colonne du pad, ou numéro du bouton / fader (à partir de 1).</param>
/// <param name="Y">Ligne du pad (1 = en haut) ; 0 sinon.</param>
public readonly record struct MidiControl(MidiControlKind Kind, int X, int Y = 0)
{
    /// <summary>Lit un contrôle écrit « pad 3 2 », « bas 1 », « droite 4 » ou « fader 9 » ; <c>null</c> si illisible.</summary>
    public static MidiControl? Parse(string? text)
    {
        var words = (text ?? string.Empty).Split([' ', ':', ','], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2 || !int.TryParse(words[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x))
        {
            return null;
        }

        var y = words.Length > 2 && int.TryParse(words[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var row) ? row : 0;
        MidiControl? control = words[0].ToLowerInvariant() switch
        {
            "pad" when x is >= 1 and <= 8 && y is >= 1 and <= 8 => new MidiControl(MidiControlKind.Pad, x, y),
            "bas" when x is >= 1 and <= 8 => new MidiControl(MidiControlKind.Bottom, x),
            "droite" when x is >= 1 and <= 8 => new MidiControl(MidiControlKind.Right, x),
            "fader" when x is >= 1 and <= 9 => new MidiControl(MidiControlKind.Fader, x),
            _ => null,
        };
        return control;
    }

    /// <inheritdoc />
    public override string ToString() => Kind switch
    {
        MidiControlKind.Pad => string.Create(CultureInfo.InvariantCulture, $"pad {X} {Y}"),
        MidiControlKind.Bottom => string.Create(CultureInfo.InvariantCulture, $"bas {X}"),
        MidiControlKind.Right => string.Create(CultureInfo.InvariantCulture, $"droite {X}"),
        _ => string.Create(CultureInfo.InvariantCulture, $"fader {X}"),
    };
}
