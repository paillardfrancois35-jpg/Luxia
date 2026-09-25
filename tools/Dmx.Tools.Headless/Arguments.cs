using System.Globalization;

namespace Dmx.Tools.Headless;

/// <summary>Lecture minimale de la ligne de commande : <c>commande [valeur] --option valeur --drapeau</c>.</summary>
internal sealed class Arguments
{
    private readonly Dictionary<string, string?> _options = new(StringComparer.OrdinalIgnoreCase);

    public Arguments(string[] args)
    {
        Command = args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty;
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                var name = args[i][2..];
                var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal) ? args[++i] : null;
                _options[name] = value;
            }
            else
            {
                Positional.Add(args[i]);
            }
        }
    }

    public string Command { get; }

    public List<string> Positional { get; } = [];

    public bool Has(string name) => _options.ContainsKey(name);

    public string? Get(string name) => _options.GetValueOrDefault(name);

    public double GetDouble(string name, double fallback) =>
        double.TryParse(Get(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    public int GetInt(string name, int fallback) =>
        int.TryParse(Get(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;
}
