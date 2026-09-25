using System.Globalization;
using System.Text;
using Dmx.Fixtures.Model;

namespace Dmx.Fixtures.Import;

/// <summary>Utilitaires communs aux imports : clés, couleurs nommées, valeurs avec unités.</summary>
internal static class ImportText
{
    /// <summary>Clé de canal stable à partir d'un nom (minuscules, sans accent, tirets).</summary>
    public static string Key(string name)
    {
        var normalized = name.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
        }

        var key = builder.ToString().Trim('-');
        while (key.Contains("--", StringComparison.Ordinal))
        {
            key = key.Replace("--", "-", StringComparison.Ordinal);
        }

        return key.Length == 0 ? "canal" : key;
    }

    /// <summary>Clé unique dans un ensemble déjà utilisé.</summary>
    public static string UniqueKey(string name, HashSet<string> used)
    {
        var baseKey = Key(name);
        var key = baseKey;
        for (var i = 2; !used.Add(key); i++)
        {
            key = string.Create(CultureInfo.InvariantCulture, $"{baseKey}-{i}");
        }

        return key;
    }

    /// <summary>Émetteur correspondant à un nom de couleur (anglais OFL / QLC+).</summary>
    public static AttributeKind? EmitterFromColor(string? color) => color?.Trim().ToLowerInvariant().Replace(" ", string.Empty, StringComparison.Ordinal) switch
    {
        "red" => AttributeKind.Red,
        "green" => AttributeKind.Green,
        "blue" => AttributeKind.Blue,
        "white" or "coldwhite" or "cold white" => AttributeKind.White,
        "warmwhite" => AttributeKind.WarmWhite,
        "amber" => AttributeKind.Amber,
        "uv" or "indigo" => AttributeKind.Uv,
        "cyan" => AttributeKind.Cyan,
        "magenta" => AttributeKind.Magenta,
        "yellow" => AttributeKind.Yellow,
        "lime" => AttributeKind.Lime,
        _ => null,
    };

    /// <summary>
    /// Lit un nombre avec unité facultative (« 20Hz », « 100% », « -360deg », « 2.5s ») ; « slow » = 0, « fast » = 100.
    /// </summary>
    public static (double Value, string? Unit)? ParseValue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var t = text.Trim().ToLowerInvariant();
        switch (t)
        {
            case "slow" or "off" or "narrow" or "near" or "small" or "dark" or "closed" or "stop":
                return (0, "%");
            case "fast" or "on" or "wide" or "far" or "big" or "bright" or "open":
                return (100, "%");
            case "fast cw":
                return (100, "%");
            case "fast ccw":
                return (-100, "%");
            case "slow cw" or "slow ccw":
                return (0, "%");
            default:
                break;
        }

        var end = 0;
        while (end < t.Length && (char.IsDigit(t[end]) || t[end] is '.' or '-' or '+'))
        {
            end++;
        }

        if (end == 0 || !double.TryParse(t[..end], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        var unit = t[end..].Trim() switch
        {
            "" => null,
            "deg" => "°",
            "hz" => "Hz",
            "k" => "K",
            var u => u,
        };
        return (value, unit);
    }

    /// <summary>Valeur par défaut « 50% » ou « 128 » en octet.</summary>
    public static int ParseDmxValue(string? text, int fallback)
    {
        if (ParseValue(text) is not { } parsed)
        {
            return fallback;
        }

        return parsed.Unit == "%" ? (int)Math.Round(Math.Clamp(parsed.Value, 0, 100) * 255 / 100) : (int)Math.Clamp(parsed.Value, 0, 255);
    }

    /// <summary>Catégorie d'appareil depuis un libellé anglais (OFL, QLC+).</summary>
    public static FixtureCategory Category(string? category, string model)
    {
        var c = (category ?? string.Empty).ToLowerInvariant();
        var m = model.ToLowerInvariant();
        return c switch
        {
            _ when c.Contains("moving head", StringComparison.Ordinal) || c.Contains("scanner", StringComparison.Ordinal) => FixtureCategory.MovingHead,
            _ when c.Contains("pixel bar", StringComparison.Ordinal) || c.Contains("led bar", StringComparison.Ordinal) || c.Contains("matrix", StringComparison.Ordinal) => FixtureCategory.LedBar,
            _ when c.Contains("smoke", StringComparison.Ordinal) || c.Contains("hazer", StringComparison.Ordinal) || c.Contains("fog", StringComparison.Ordinal) => FixtureCategory.Smoke,
            _ when c.Contains("strobe", StringComparison.Ordinal) => FixtureCategory.Strobe,
            _ when c.Contains("dimmer", StringComparison.Ordinal) => FixtureCategory.Dimmer,
            _ when c.Contains("laser", StringComparison.Ordinal) => FixtureCategory.Laser,
            _ when c.Contains("effect", StringComparison.Ordinal) || c.Contains("flower", StringComparison.Ordinal) || c.Contains("barrel", StringComparison.Ordinal) => FixtureCategory.Effect,
            _ when c.Contains("color changer", StringComparison.Ordinal) || c.Contains("blinder", StringComparison.Ordinal) =>
                m.Contains("bar", StringComparison.Ordinal) ? FixtureCategory.LedBar : m.Contains("uv", StringComparison.Ordinal) ? FixtureCategory.Uv : FixtureCategory.Par,
            _ => FixtureCategory.Other,
        };
    }
}
