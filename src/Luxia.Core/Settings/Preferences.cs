using System.Text.Json.Serialization;
using Luxia.Core.Dmx;

namespace Luxia.Core.Settings;

/// <summary>
/// Préférences du poste (doc 02 §10.1, <c>%AppData%\LuXia\preferences.json</c>).
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

    /// <summary>
    /// Taille de l'interface (F8, ERG-022) : 1 = 100 %, 1,25, 1,5 (écran tactile, lecture de loin). Propre au poste.
    /// </summary>
    public double UiScale { get; init; } = 1;

    /// <summary>
    /// Boutons de scène resserrés dans l'écran Contrôle (une ligne, environ moitié moins hauts ; essai 1.005.226).
    /// Propre au poste.
    /// </summary>
    public bool CompactScenes { get; init; }

    /// <summary>Écoute de la musique (doc 19, AUD-081) ; propre au poste.</summary>
    public AudioPreferences Audio { get; init; } = new();

    /// <summary>Sorties.</summary>
    public OutputPreferences Outputs { get; init; } = new();

    /// <summary>Réglages du test de sortie (SORT-007).</summary>
    public TestOutputPreferences TestOutput { get; init; } = new();
}

/// <summary>Écoute de la musique (doc 19 §6, AUD-081).</summary>
public sealed record AudioPreferences
{
    /// <summary>L'écoute du son joué par le PC démarre avec l'application.</summary>
    public bool Listen { get; init; }

    /// <summary>Périphérique écouté (AUD-003) ; vide = le son joué par le PC, sur la sortie par défaut.</summary>
    public string? DeviceId { get; init; }

    /// <summary>Sensibilité des impulsions, de 0 à 1 (AUD-042).</summary>
    public double PulseSensitivity { get; init; } = 0.5;

    /// <summary>Lissage de l'énergie en secondes (AUD-060).</summary>
    public double EnergySmoothingSeconds { get; init; } = 2;

    /// <summary>Tempo minimal exploré (AUD-020).</summary>
    public double MinBpm { get; init; } = 70;

    /// <summary>Tempo maximal exploré (AUD-020).</summary>
    public double MaxBpm { get; init; } = 180;

    /// <summary>Centre de la préférence d'octave, en BPM (AUD-023).</summary>
    public double PreferredBpm { get; init; } = 118;

    /// <summary>Décalage de latence global en secondes (GEN-035, AUD-027), de −0,25 à +0,25.</summary>
    public double LatencySeconds { get; init; }
}

/// <summary>Configuration des sorties (SORT-001, SORT-006).</summary>
public sealed record OutputPreferences
{
    /// <summary>Affectations univers → pilote. Par défaut : univers 1 vers l'Arduino.</summary>
    public IReadOnlyList<OutputAssignment> Assignments { get; init; } = [new(1, OutputDriverKind.Arduino)];

    /// <summary>Réglages du pilote Arduino.</summary>
    public ArduinoPreferences Arduino { get; init; } = new();

    /// <summary>Dossier des enregistrements de trames (vide = <c>Documents\LuXia\Enregistrements</c>).</summary>
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

    /// <summary>
    /// Canaux maintenus à la valeur de test pendant tout le chenillard (SORT-008, Q23) : les maîtres des appareils
    /// à gradateur maître (sinon leurs autres canaux — couleur, roue… — restent invisibles pendant leur propre passage).
    /// </summary>
    public string HeldChannels { get; init; } = string.Empty;

    /// <summary>Valeur de test en pourcentage (50 % par défaut).</summary>
    public int ValuePercent { get; init; } = 50;

    /// <summary>Durée d'allumage de chaque canal, en millisecondes.</summary>
    public int StepMilliseconds { get; init; } = 1000;

    /// <summary>Valeur DMX correspondant au pourcentage (arrondi, 50 % = 128), recalculée : jamais enregistrée.</summary>
    [JsonIgnore]
    public byte ValueByte => (byte)Math.Round(Math.Clamp(ValuePercent, 0, 100) * 255 / 100.0, MidpointRounding.AwayFromZero);
}
