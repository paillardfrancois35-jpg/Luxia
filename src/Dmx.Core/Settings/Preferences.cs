using Dmx.Core.Dmx;

namespace Dmx.Core.Settings;

/// <summary>
/// Préférences du poste (doc 02 §10.1, <c>%AppData%\DMX\preferences.json</c>).
/// Contient la configuration des sorties, qui n'appartient jamais au projet (SORT-006).
/// </summary>
public sealed record Preferences
{
    /// <summary>Version courante du format du fichier de préférences.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Fréquence du moteur (GEN-030).</summary>
    public double TickRateHz { get; init; } = DmxConstants.DefaultTickRateHz;

    /// <summary>Dernier projet ouvert.</summary>
    public string? LastProjectPath { get; init; }

    /// <summary>Sorties.</summary>
    public OutputPreferences Outputs { get; init; } = new();

    /// <summary>Réglages du test de sortie (SORT-007).</summary>
    public TestOutputPreferences TestOutput { get; init; } = new();
}

/// <summary>Configuration des sorties (SORT-001, SORT-006).</summary>
public sealed record OutputPreferences
{
    /// <summary>Affectations univers → pilote. Par défaut : univers 1 vers l'Arduino.</summary>
    public IReadOnlyList<OutputAssignment> Assignments { get; init; } = [new(1, OutputDriverKind.Arduino)];

    /// <summary>Réglages du pilote Arduino.</summary>
    public ArduinoPreferences Arduino { get; init; } = new();

    /// <summary>Dossier des enregistrements de trames (vide = <c>Documents\DMX\Enregistrements</c>).</summary>
    public string? RecordingsFolder { get; init; }
}

/// <summary>Affectation d'un pilote à un univers.</summary>
/// <param name="Universe">Univers (1 = premier).</param>
/// <param name="Driver">Type de pilote.</param>
public sealed record OutputAssignment(int Universe, OutputDriverKind Driver);

/// <summary>Types de pilotes configurables.</summary>
public enum OutputDriverKind
{
    /// <summary>Aucun matériel.</summary>
    Null,

    /// <summary>Arduino Leonardo + shield DMX.</summary>
    Arduino,
}

/// <summary>Protocole de dialogue avec l'Arduino.</summary>
public enum ArduinoProtocol
{
    /// <summary>Sous-ensemble Enttec DMX USB Pro (firmware du projet, doc 10 §5).</summary>
    Enttec,

    /// <summary>Ancien format du POC (label 0x11), port forcé uniquement (SORT-014).</summary>
    Legacy,
}

/// <summary>Réglages du pilote Arduino (doc 10 §4).</summary>
public sealed record ArduinoPreferences
{
    /// <summary>Dernier port utilisé, essayé en premier (SORT-011).</summary>
    public string? LastPort { get; init; }

    /// <summary>Port imposé, sans détection (SORT-014). Vide = détection automatique.</summary>
    public string? ForcedPort { get; init; }

    /// <summary>Protocole utilisé sur un port imposé.</summary>
    public ArduinoProtocol Protocol { get; init; } = ArduinoProtocol.Enttec;

    /// <summary>Nombre de canaux émis (SORT-021), 1 à 512.</summary>
    public int ChannelCount { get; init; } = DmxConstants.ChannelCount;

    /// <summary>
    /// Sonder aussi les ports série qui ne sont pas des cartes Arduino. Désactivé par défaut :
    /// certains ports (Bluetooth) bloquent longtemps à l'ouverture et un octet envoyé à un appareil inconnu n'est pas anodin.
    /// </summary>
    public bool ProbeAllPorts { get; init; }
}

/// <summary>Réglages du chenillard de test (SORT-007, Q16).</summary>
public sealed record TestOutputPreferences
{
    /// <summary>Plage de canaux parcourue.</summary>
    public string Range { get; init; } = "1-16";

    /// <summary>Canaux exclus (180 = fumée du show de référence).</summary>
    public string ExcludedChannels { get; init; } = "180";

    /// <summary>Valeur de test en pourcentage (50 % par défaut).</summary>
    public int ValuePercent { get; init; } = 50;

    /// <summary>Durée d'allumage de chaque canal, en millisecondes.</summary>
    public int StepMilliseconds { get; init; } = 1000;

    /// <summary>Valeur DMX correspondant au pourcentage (arrondi, 50 % = 128).</summary>
    public byte ValueByte => (byte)Math.Round(Math.Clamp(ValuePercent, 0, 100) * 255 / 100.0, MidpointRounding.AwayFromZero);
}
