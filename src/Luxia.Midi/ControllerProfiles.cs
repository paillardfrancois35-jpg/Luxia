using System.Text.Json;

namespace Luxia.Midi;

/// <summary>Profils des contrôleurs pris en charge, lus dans les ressources du module (GEN-072, MIDI-001).</summary>
public static class ControllerProfiles
{
    private static readonly Lazy<List<ControllerProfile>> Loaded = new(Load);

    /// <summary>Tous les profils (MK2 avant MK1 : son nom contient aussi « APC mini »).</summary>
    public static IReadOnlyList<ControllerProfile> All => Loaded.Value;

    /// <summary>Profil correspondant à un nom de port MIDI, ou <c>null</c>.</summary>
    public static ControllerProfile? Match(string portName) => All.FirstOrDefault(p => p.Matches(portName));

    private static List<ControllerProfile> Load()
    {
        var assembly = typeof(ControllerProfiles).Assembly;
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { ReadCommentHandling = JsonCommentHandling.Skip };
        var profiles = new List<ControllerProfile>();
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("Luxia.Midi.Profiles.", StringComparison.Ordinal)).Order().Reverse())
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            profiles.Add(JsonSerializer.Deserialize<ControllerProfile>(stream, options)
                ?? throw new InvalidDataException($"Profil MIDI illisible : {name}"));
        }

        return profiles;
    }
}
