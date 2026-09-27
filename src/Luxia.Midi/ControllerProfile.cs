namespace Luxia.Midi;

/// <summary>
/// Profil d'un modèle de contrôleur (doc 18b §2) : numéros de notes et de contrôleurs, messages des LED. Chargé depuis
/// un fichier de données (<c>Profiles/*.json</c>), pas écrit dans le code.
/// </summary>
public sealed record ControllerProfile
{
    /// <summary>Nom complet (« AKAI APC mini mk2 »).</summary>
    public required string Name { get; init; }

    /// <summary>Nom court affiché (« APC mini MK2 »).</summary>
    public required string ShortName { get; init; }

    /// <summary>Remarques (source des numéros, points à vérifier).</summary>
    public string? Notes { get; init; }

    /// <summary>Morceaux de nom de port MIDI qui désignent ce modèle (sans tenir compte de la casse).</summary>
    public IReadOnlyList<string> PortNames { get; init; } = [];

    /// <summary>Morceaux de nom de port à écarter (autre modèle, port secondaire).</summary>
    public IReadOnlyList<string> ExcludedPortNames { get; init; } = [];

    /// <summary>Note du pad en bas à gauche de la grille 8 × 8 ; la grille monte ligne par ligne, de gauche à droite.</summary>
    public int GridBottomLeftNote { get; init; }

    /// <summary>Notes des 8 boutons ronds du bas, de gauche à droite.</summary>
    public IReadOnlyList<int> BottomButtons { get; init; } = [];

    /// <summary>Notes des 8 boutons ronds de droite, de haut en bas.</summary>
    public IReadOnlyList<int> RightButtons { get; init; } = [];

    /// <summary>Note du bouton Shift.</summary>
    public int ShiftNote { get; init; }

    /// <summary>Contrôleurs (CC) des 9 faders, de gauche à droite (le dernier = master).</summary>
    public IReadOnlyList<int> Faders { get; init; } = [];

    /// <summary>LED des pads.</summary>
    public PadLeds Pads { get; init; } = new();

    /// <summary>LED des boutons ronds.</summary>
    public ButtonLeds Buttons { get; init; } = new();

    /// <summary>Le port MIDI de ce nom est-il un contrôleur de ce modèle ?</summary>
    public bool Matches(string portName)
    {
        ArgumentNullException.ThrowIfNull(portName);
        return PortNames.Any(n => portName.Contains(n, StringComparison.OrdinalIgnoreCase))
            && !ExcludedPortNames.Any(n => portName.Contains(n, StringComparison.OrdinalIgnoreCase));
    }
}
