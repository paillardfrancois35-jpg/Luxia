using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Dmx.Fixtures.Model;

namespace Dmx.Fixtures.Import;

/// <summary>
/// Import d'un appareil QLC+ (<c>.qxf</c>, XML, BIB-081) : canaux (préréglages ou groupes → attributs),
/// capacités → plages, modes, têtes → cellules. Les canaux « … Fine » sont rattachés à leur canal grossier (16 bits).
/// </summary>
public static class QlcImporter
{
    /// <summary>Importe un fichier <c>.qxf</c>.</summary>
    public static ImportResult ImportFile(string path)
    {
        try
        {
            var document = XDocument.Load(path);
            return Import(document, path);
        }
        catch (Exception ex) when (ex is XmlException or IOException or InvalidOperationException or FormatException)
        {
            return new ImportResult(path, null, [], $"fichier QLC+ illisible : {ex.Message}");
        }
    }

    /// <summary>Importe un document déjà lu.</summary>
    public static ImportResult Import(XDocument document, string sourceFile)
    {
        ArgumentNullException.ThrowIfNull(document);
        var root = document.Root ?? throw new InvalidOperationException("document vide");
        XNamespace ns = root.Name.Namespace;
        var notes = new List<string>();

        var manufacturer = root.Element(ns + "Manufacturer")?.Value.Trim() ?? "Inconnu";
        var model = root.Element(ns + "Model")?.Value.Trim() ?? Path.GetFileNameWithoutExtension(sourceFile);
        var type = root.Element(ns + "Type")?.Value;

        // 1. Canaux déclarés.
        var used = new HashSet<string>(StringComparer.Ordinal);
        var raw = new List<RawChannel>();
        foreach (var channel in root.Elements(ns + "Channel"))
        {
            var name = channel.Attribute("Name")?.Value ?? "Canal";
            raw.Add(ReadChannel(ns, channel, name, notes));
        }

        // 2. Canaux fins rattachés à leur canal grossier.
        var byName = raw.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        var fineOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var fine in raw.Where(r => r.IsFine))
        {
            var coarseName = CoarseName(fine.Name);
            if (coarseName is not null && byName.TryGetValue(coarseName, out var coarse) && !coarse.IsFine)
            {
                fineOf[fine.Name] = coarse.Name;
            }
            else
            {
                notes.Add($"Canal fin « {fine.Name} » sans canal grossier : gardé comme canal 8 bits.");
            }
        }

        var keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in raw.Where(r => !fineOf.ContainsKey(r.Name)))
        {
            keys[r.Name] = ImportText.UniqueKey(r.Name, used);
        }

        // 3. Modes, et cellules d'après les têtes du premier mode qui en déclare.
        var cells = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var modes = new List<FixtureMode>();
        foreach (var mode in root.Elements(ns + "Mode"))
        {
            var numbered = mode.Elements(ns + "Channel")
                .Select(c => (Number: int.Parse(c.Attribute("Number")?.Value ?? "0", CultureInfo.InvariantCulture), Name: c.Value.Trim()))
                .OrderBy(c => c.Number)
                .ToList();

            var heads = mode.Elements(ns + "Head").ToList();
            for (var h = 0; h < heads.Count; h++)
            {
                foreach (var number in heads[h].Elements(ns + "Channel").Select(c => int.Parse(c.Value, CultureInfo.InvariantCulture)))
                {
                    var channelName = numbered.FirstOrDefault(n => n.Number == number).Name;
                    if (channelName is not null)
                    {
                        cells.TryAdd(fineOf.GetValueOrDefault(channelName, channelName), h + 1);
                    }
                }
            }

            var slots = new List<ModeChannel>();
            foreach (var (_, channelName) in numbered)
            {
                if (fineOf.TryGetValue(channelName, out var coarse))
                {
                    slots.Add(new ModeChannel(keys[coarse], ChannelPart.Fine));
                }
                else if (keys.TryGetValue(channelName, out var key))
                {
                    slots.Add(new ModeChannel(key));
                }
                else
                {
                    notes.Add($"Mode « {mode.Attribute("Name")?.Value} » : canal « {channelName} » inconnu, ignoré.");
                }
            }

            modes.Add(new FixtureMode { Name = mode.Attribute("Name")?.Value ?? "Mode", Channels = slots });
        }

        var channels = raw
            .Where(r => !fineOf.ContainsKey(r.Name))
            .Select(r => new ChannelDefinition
            {
                Key = keys[r.Name],
                Name = r.Name,
                Attribute = r.Attribute,
                Cell = cells.GetValueOrDefault(r.Name),
                Resolution = fineOf.ContainsValue(r.Name) ? ChannelResolution.Bit16 : ChannelResolution.Bit8,
                Default = r.Default,
                Capabilities = r.Capabilities,
            })
            .ToList();

        var physical = root.Descendants(ns + "Physical").FirstOrDefault();
        var fixture = new FixtureType
        {
            Manufacturer = manufacturer,
            Model = model,
            Category = ImportText.Category(type, model),
            Source = FixtureSource.QlcPlus,
            Author = root.Element(ns + "Creator")?.Element(ns + "Author")?.Value,
            Notes = $"Importé de QLC+ ({Path.GetFileName(sourceFile)}).",
            Physical = new PhysicalInfo
            {
                SourceType = physical?.Element(ns + "Bulb")?.Attribute("Type")?.Value,
                BeamAngle = Number(physical?.Element(ns + "Lens")?.Attribute("DegreesMax")?.Value),
                PanRange = Number(physical?.Element(ns + "Focus")?.Attribute("PanMax")?.Value),
                TiltRange = Number(physical?.Element(ns + "Focus")?.Attribute("TiltMax")?.Value),
                Power = Number(physical?.Element(ns + "Technical")?.Attribute("PowerConsumption")?.Value),
            },
            Channels = channels,
            Modes = modes,
        };

        return new ImportResult(sourceFile, fixture, notes);
    }

    private static RawChannel ReadChannel(XNamespace ns, XElement channel, string name, List<string> notes)
    {
        var preset = channel.Attribute("Preset")?.Value;
        var group = channel.Element(ns + "Group");
        var groupName = group?.Value.Trim();
        var colour = channel.Element(ns + "Colour")?.Value.Trim();
        var isFine = group?.Attribute("Byte")?.Value == "1"
            || (preset?.EndsWith("Fine", StringComparison.Ordinal) ?? false);

        var attribute = preset is not null ? FromPreset(preset) : FromGroup(groupName, colour, name);
        if (attribute is null)
        {
            notes.Add($"Canal « {name} » : {(preset is not null ? $"préréglage « {preset} »" : $"groupe « {groupName} »")} non reconnu, converti en « Générique ».");
            attribute = AttributeKind.Generic;
        }

        var capabilities = channel.Elements(ns + "Capability").Select(c => ReadCapability(c, attribute.Value)).ToList();
        if (capabilities.Count == 1 && capabilities[0].Min == 0 && capabilities[0].Max == 255)
        {
            capabilities.Clear();
        }

        var @default = int.TryParse(channel.Attribute("Default")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) ? d : 0;
        return new RawChannel(name, attribute.Value, isFine, @default, capabilities);
    }

    private static Capability ReadCapability(XElement capability, AttributeKind attribute)
    {
        var min = int.Parse(capability.Attribute("Min")?.Value ?? "0", CultureInfo.InvariantCulture);
        var max = int.Parse(capability.Attribute("Max")?.Value ?? "255", CultureInfo.InvariantCulture);
        var label = capability.Value.Trim();
        var preset = capability.Attribute("Preset")?.Value ?? string.Empty;
        var colors = new[] { capability.Attribute("Color")?.Value, capability.Attribute("Color2")?.Value, capability.Attribute("Res")?.Value }
            .Where(c => c is not null && c.StartsWith('#'))
            .Select(c => c!)
            .ToList();

        StrobeEffect? strobe = null;
        var kind = CapabilityKind.Fixed;
        var lower = label.ToLowerInvariant();
        if (preset is "ShutterOpen" || (attribute == AttributeKind.Shutter && lower.Contains("open", StringComparison.Ordinal)))
        {
            (strobe, kind) = (StrobeEffect.Open, CapabilityKind.Open);
        }
        else if (preset is "ShutterClose" || (attribute == AttributeKind.Shutter && (lower.Contains("close", StringComparison.Ordinal) || lower.Contains("blackout", StringComparison.Ordinal))))
        {
            (strobe, kind) = (StrobeEffect.Closed, CapabilityKind.Closed);
        }
        else if (preset.Contains("Random", StringComparison.Ordinal) && preset.Contains("Strobe", StringComparison.Ordinal))
        {
            strobe = StrobeEffect.Random;
        }
        else if (preset.StartsWith("Pulse", StringComparison.Ordinal))
        {
            strobe = StrobeEffect.Pulse;
        }
        else if (preset.Contains("Strobe", StringComparison.Ordinal) || (attribute == AttributeKind.Shutter && lower.Contains("strobe", StringComparison.Ordinal)))
        {
            (strobe, kind) = (StrobeEffect.Strobe, CapabilityKind.Progressive);
        }
        else if (colors.Count > 0 || preset is "ColorMacro" or "ColorDoubleMacro" or "GoboMacro")
        {
            kind = CapabilityKind.WheelSlot;
        }
        else if (lower is "no function" or "nothing")
        {
            kind = CapabilityKind.NoFunction;
        }

        return new Capability { Min = min, Max = max, Label = label.Length == 0 ? "Plage" : label, Kind = kind, Strobe = strobe, Colors = colors };
    }

    private static AttributeKind? FromPreset(string preset)
    {
        var p = preset.EndsWith("Fine", StringComparison.Ordinal) ? preset[..^4] : preset;
        if (p.StartsWith("Intensity", StringComparison.Ordinal))
        {
            var color = p["Intensity".Length..];
            return color is "MasterDimmer" or "Dimmer" ? AttributeKind.Intensity : ImportText.EmitterFromColor(color);
        }

        return p switch
        {
            "PositionPan" or "PositionXAxis" => AttributeKind.Pan,
            "PositionTilt" or "PositionYAxis" => AttributeKind.Tilt,
            _ when p.StartsWith("Speed", StringComparison.Ordinal) && (p.Contains("Pan", StringComparison.Ordinal) || p.Contains("Tilt", StringComparison.Ordinal)) => AttributeKind.PanTiltSpeed,
            "ColorMacro" => AttributeKind.ColorMacro,
            "ColorWheel" => AttributeKind.ColorWheel,
            "ColorCTOMixer" or "ColorCTBMixer" or "ColorCTCMixer" => AttributeKind.ColorTemperature,
            "GoboWheel" or "GoboIndex" => AttributeKind.Gobo,
            _ when p.StartsWith("Shutter", StringComparison.Ordinal) && p.Contains("Iris", StringComparison.Ordinal) => AttributeKind.Iris,
            _ when p.StartsWith("Shutter", StringComparison.Ordinal) => AttributeKind.Shutter,
            _ when p.StartsWith("BeamFocus", StringComparison.Ordinal) => AttributeKind.Focus,
            _ when p.StartsWith("BeamZoom", StringComparison.Ordinal) => AttributeKind.Zoom,
            _ when p.StartsWith("PrismRotation", StringComparison.Ordinal) => AttributeKind.PrismRotation,
            "NoFunction" => AttributeKind.NoFunction,
            _ => null,
        };
    }

    private static AttributeKind? FromGroup(string? group, string? colour, string name) => group switch
    {
        "Intensity" => colour is null or "Generic" ? AttributeKind.Intensity : ImportText.EmitterFromColor(colour) ?? AttributeKind.Intensity,
        "Colour" => AttributeKind.ColorWheel,
        "Gobo" => AttributeKind.Gobo,
        "Pan" => AttributeKind.Pan,
        "Tilt" => AttributeKind.Tilt,
        "Speed" => name.Contains("pan", StringComparison.OrdinalIgnoreCase) || name.Contains("tilt", StringComparison.OrdinalIgnoreCase) ? AttributeKind.PanTiltSpeed : AttributeKind.ProgramSpeed,
        "Shutter" => AttributeKind.Shutter,
        "Prism" => AttributeKind.Prism,
        "Beam" => AttributeKind.Zoom,
        "Effect" => AttributeKind.Program,
        "Maintenance" => name.Contains("reset", StringComparison.OrdinalIgnoreCase) ? AttributeKind.Reset : AttributeKind.Maintenance,
        "Nothing" => AttributeKind.NoFunction,
        _ => null,
    };

    private static string? CoarseName(string fineName)
    {
        foreach (var suffix in new[] { " fine", " Fine", "Fine", " (fine)" })
        {
            if (fineName.EndsWith(suffix, StringComparison.Ordinal))
            {
                return fineName[..^suffix.Length].TrimEnd();
            }
        }

        return null;
    }

    private static double? Number(string? text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0 ? v : null;

    private sealed record RawChannel(string Name, AttributeKind Attribute, bool IsFine, int Default, IReadOnlyList<Capability> Capabilities);
}
