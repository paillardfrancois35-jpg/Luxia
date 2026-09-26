using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Luxia.Fixtures.Model;

namespace Luxia.Fixtures.Import;

/// <summary>
/// Import d'un appareil au format Open Fixture Library (JSON, BIB-080) : modes, canaux, capacités → plages,
/// roues, canaux fins (16 bits), matrices → cellules. Les types de capacités OFL sont convertis vers le
/// catalogue d'attributs ; ce qui ne l'est pas devient « Générique » et figure au rapport (BIB-082).
/// </summary>
public static class OflImporter
{
    /// <summary>Le document ressemble-t-il à un appareil OFL ?</summary>
    public static bool IsOfl(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document["availableChannels"] is JsonObject && document["modes"] is JsonArray;
    }

    /// <summary>Importe un fichier ; le fabricant est déduit du dossier (organisation du dépôt OFL) s'il n'est pas fourni.</summary>
    public static ImportResult ImportFile(string path, string? manufacturer = null)
    {
        try
        {
            var document = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
                ?? throw new JsonException("pas un objet JSON");
            manufacturer ??= ManufacturerFromFolder(path);
            return Import(document, manufacturer, path);
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException or FormatException)
        {
            return new ImportResult(path, null, [], $"fichier OFL illisible : {ex.Message}");
        }
    }

    /// <summary>Importe un document OFL déjà lu.</summary>
    public static ImportResult Import(JsonObject document, string manufacturer, string sourceFile)
    {
        ArgumentNullException.ThrowIfNull(document);
        var notes = new List<string>();
        var context = new Context(document, notes);

        var name = document["name"]?.GetValue<string>() ?? Path.GetFileNameWithoutExtension(sourceFile);
        var category = document["categories"] is JsonArray categories && categories.Count > 0 ? categories[0]!.GetValue<string>() : null;

        context.ReadWheels();
        context.ReadMatrix();
        context.ReadAvailableChannels();

        var modes = new List<FixtureMode>();
        foreach (var modeNode in document["modes"]!.AsArray().OfType<JsonObject>())
        {
            modes.Add(context.ReadMode(modeNode));
        }

        var fixture = new FixtureType
        {
            Manufacturer = manufacturer,
            Model = name,
            Category = ImportText.Category(category, name),
            Source = FixtureSource.Ofl,
            Author = document["meta"]?["authors"] is JsonArray authors ? string.Join(", ", authors.Select(a => a!.GetValue<string>())) : null,
            Notes = $"Importé d'Open Fixture Library ({Path.GetFileName(sourceFile)}).",
            Physical = context.ReadPhysical(),
            Wheels = context.Wheels,
            Channels = context.Channels,
            Modes = modes,
        };

        if (document["matrix"]?["pixelGroups"] is not null)
        {
            notes.Add("Groupes de pixels (pixelGroups) ignorés.");
        }

        return new ImportResult(sourceFile, fixture, notes);
    }

    private static string ManufacturerFromFolder(string path)
    {
        var folder = Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(path))) ?? "Inconnu";
        return string.Join(' ', folder.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }

    /// <summary>État de conversion d'un document.</summary>
    private sealed class Context(JsonObject document, List<string> notes)
    {
        private readonly Dictionary<string, (string Key, ChannelPart Part)> _aliases = new(StringComparer.Ordinal);
        private readonly Dictionary<string, JsonObject> _templates = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _wheelKeys = new(StringComparer.Ordinal);
        private readonly HashSet<string> _usedKeys = new(StringComparer.Ordinal);
        private readonly List<string> _pixelKeys = [];
        private int _unusedSlots;

        public List<Wheel> Wheels { get; } = [];

        public List<ChannelDefinition> Channels { get; } = [];

        public void ReadWheels()
        {
            if (document["wheels"] is not JsonObject wheels)
            {
                return;
            }

            foreach (var (wheelName, node) in wheels)
            {
                var slots = node?["slots"] as JsonArray ?? [];
                var isGobo = slots.Any(s => s?["type"]?.GetValue<string>() is "Gobo" or "GoboShake");
                var converted = new List<WheelSlot>();
                var index = 0;
                foreach (var slot in slots.OfType<JsonObject>())
                {
                    index++;
                    var type = slot["type"]?.GetValue<string>();
                    var colors = slot["colors"] is JsonArray c ? c.Select(x => x!.GetValue<string>()).ToList() : [];
                    var slotName = slot["name"]?.GetValue<string>() ?? type switch
                    {
                        "Open" => "Ouvert",
                        "Closed" => "Fermé",
                        "Gobo" => string.Create(CultureInfo.CurrentCulture, $"Gobo {index}"),
                        _ => string.Create(CultureInfo.CurrentCulture, $"Emplacement {index}"),
                    };
                    converted.Add(new WheelSlot(slotName, colors, slot["resource"]?.ToString()));
                }

                var key = ImportText.Key(wheelName);
                _wheelKeys[wheelName] = key;
                Wheels.Add(new Wheel { Key = key, Name = wheelName, Kind = isGobo ? WheelKind.Gobo : WheelKind.Color, Slots = converted });
            }
        }

        public void ReadMatrix()
        {
            if (document["matrix"] is not JsonObject matrix)
            {
                return;
            }

            if (matrix["pixelKeys"] is JsonArray z)
            {
                // pixelKeys[z][y][x], null = pas de pixel.
                foreach (var y in z.OfType<JsonArray>())
                {
                    foreach (var x in y.OfType<JsonArray>())
                    {
                        _pixelKeys.AddRange(x.Where(k => k is not null).Select(k => k!.GetValue<string>()));
                    }
                }
            }
            else if (matrix["pixelCount"] is JsonArray count)
            {
                var total = count.Aggregate(1, (acc, n) => acc * n!.GetValue<int>());
                _pixelKeys.AddRange(Enumerable.Range(1, total).Select(i => i.ToString(CultureInfo.InvariantCulture)));
            }

            if (document["templateChannels"] is JsonObject templates)
            {
                foreach (var (templateName, node) in templates)
                {
                    _templates[templateName] = node!.AsObject();
                }
            }
        }

        public void ReadAvailableChannels()
        {
            foreach (var (channelName, node) in document["availableChannels"]!.AsObject())
            {
                AddChannel(channelName, node!.AsObject(), cell: 0, fromTemplate: false);
            }
        }

        public FixtureMode ReadMode(JsonObject mode)
        {
            var slots = new List<ModeChannel>();
            foreach (var item in mode["channels"]?.AsArray() ?? [])
            {
                switch (item)
                {
                    case null:
                        slots.Add(new ModeChannel(UnusedSlot()));
                        break;
                    case JsonValue value:
                        slots.Add(Resolve(value.GetValue<string>()));
                        break;
                    case JsonObject insert when insert["insert"]?.GetValue<string>() == "matrixChannels":
                        slots.AddRange(ExpandMatrix(insert));
                        break;
                    default:
                        notes.Add($"Mode « {mode["name"]} » : élément de canal non reconnu, ignoré.");
                        break;
                }
            }

            var modeName = mode["name"]?.GetValue<string>() ?? "Mode";
            return new FixtureMode
            {
                Name = modeName,
                ShortName = mode["shortName"]?.GetValue<string>(),
                Channels = slots,
            };
        }

        public PhysicalInfo ReadPhysical()
        {
            var physical = document["physical"] as JsonObject;
            if (physical is null)
            {
                return new PhysicalInfo();
            }

            static double? Number(JsonNode? node) =>
                node is JsonValue v && v.TryGetValue<double>(out var d) ? d : null;

            return new PhysicalInfo
            {
                SourceType = physical["bulb"]?["type"]?.GetValue<string>(),
                BeamAngle = physical["lens"]?["degreesMinMax"] is JsonArray lens && lens.Count == 2 ? Number(lens[1]) : null,
                PanRange = Number(physical["focus"]?["panMax"]),
                TiltRange = Number(physical["focus"]?["tiltMax"]),
                Power = Number(physical["power"]),
            };
        }

        private ModeChannel Resolve(string name)
        {
            if (_aliases.TryGetValue(name, out var alias))
            {
                return new ModeChannel(alias.Key, alias.Part);
            }

            notes.Add($"Canal « {name} » référencé par un mode mais inconnu : remplacé par un canal générique.");
            var key = ImportText.UniqueKey(name, _usedKeys);
            Channels.Add(new ChannelDefinition { Key = key, Name = name, Attribute = AttributeKind.Generic });
            _aliases[name] = (key, ChannelPart.Coarse);
            return new ModeChannel(key);
        }

        private IEnumerable<ModeChannel> ExpandMatrix(JsonObject insert)
        {
            var keys = insert["repeatFor"] switch
            {
                JsonArray list => list.Select(k => k!.GetValue<string>()).ToList(),
                _ => _pixelKeys,
            };
            var templates = insert["templateChannels"]?.AsArray().Select(t => t?.GetValue<string>()).ToList() ?? [];
            var perPixel = insert["channelOrder"]?.GetValue<string>() != "perChannel";

            IEnumerable<(string Pixel, string? Template)> order = perPixel
                ? keys.SelectMany(p => templates.Select(t => (p, t)))
                : templates.SelectMany(t => keys.Select(p => (p, t)));

            foreach (var (pixel, template) in order)
            {
                if (template is null)
                {
                    yield return new ModeChannel(UnusedSlot());
                    continue;
                }

                var concrete = template.Replace("$pixelKey", pixel, StringComparison.Ordinal);
                if (!_aliases.ContainsKey(concrete))
                {
                    InstantiateTemplate(template, pixel);
                }

                yield return Resolve(concrete);
            }
        }

        private void InstantiateTemplate(string templateName, string pixel)
        {
            // Le nom du modèle peut être celui d'un canal fin d'un gabarit : on instancie le gabarit parent.
            var parent = _templates.FirstOrDefault(t =>
                t.Key == templateName
                || (t.Value["fineChannelAliases"] is JsonArray fines && fines.Any(f => f!.GetValue<string>() == templateName)));
            if (parent.Key is null)
            {
                return;
            }

            var cell = _pixelKeys.IndexOf(pixel) + 1;
            var concreteNode = JsonNode.Parse(parent.Value.ToJsonString()!)!.AsObject();
            if (concreteNode["fineChannelAliases"] is JsonArray fineAliases)
            {
                concreteNode["fineChannelAliases"] = new JsonArray([.. fineAliases.Select(f => (JsonNode)JsonValue.Create(f!.GetValue<string>().Replace("$pixelKey", pixel, StringComparison.Ordinal))!)]);
            }

            AddChannel(parent.Key.Replace("$pixelKey", pixel, StringComparison.Ordinal), concreteNode, cell == 0 ? 1 : cell, fromTemplate: true);
        }

        private void AddChannel(string name, JsonObject node, int cell, bool fromTemplate)
        {
            var key = ImportText.UniqueKey(name, _usedKeys);
            var fineAliases = node["fineChannelAliases"] is JsonArray fines ? fines.Select(f => f!.GetValue<string>()).ToList() : [];
            if (fineAliases.Count > 1)
            {
                notes.Add($"Canal « {name} » : résolution supérieure à 16 bits, seul le premier canal fin est conservé.");
            }

            var is16Bit = fineAliases.Count > 0;
            var fineShift = node["dmxValueResolution"]?.GetValue<string>() switch
            {
                "16bit" => 8,
                "24bit" => 16,
                _ => is16Bit ? 8 : 0,
            };

            var capabilityNodes = node["capabilities"] is JsonArray list
                ? list.OfType<JsonObject>().ToList()
                : node["capability"] is JsonObject single ? [single] : [];

            var converted = capabilityNodes.Select(c => ConvertCapability(name, c, fineShift, capabilityNodes.Count == 1)).ToList();
            var attribute = ChooseAttribute(name, converted.Select(c => c.Attribute).ToList());
            if (fromTemplate && attribute == AttributeKind.Intensity)
            {
                attribute = AttributeKind.CellIntensity;
            }

            var capabilities = capabilityNodes.Count == 1 && converted[0].Capability.Kind is CapabilityKind.Fixed or CapabilityKind.Progressive && converted[0].IsWholeRange
                ? []
                : converted.Select(c => c.Capability).ToList();

            var wheel = converted.Select(c => c.Wheel).FirstOrDefault(w => w is not null);
            Channels.Add(new ChannelDefinition
            {
                Key = key,
                Name = name,
                Attribute = attribute,
                Cell = cell,
                Resolution = is16Bit ? ChannelResolution.Bit16 : ChannelResolution.Bit8,
                Default = DefaultValue(node["defaultValue"], fineShift),
                Identify = node["highlightValue"] is JsonNode h ? ImportText.ParseDmxValue(h.ToString(), 255) : null,
                Wheel = wheel,
                Capabilities = capabilities,
            });

            _aliases[name] = (key, ChannelPart.Coarse);
            if (is16Bit)
            {
                _aliases[fineAliases[0]] = (key, ChannelPart.Fine);
            }
        }

        /// <summary>Valeur par défaut : « 50% », ou un nombre exprimé dans la résolution du canal (16 bits : 32768 → 128).</summary>
        private static int DefaultValue(JsonNode? node, int fineShift)
        {
            if (node is JsonValue value && value.TryGetValue<int>(out var raw))
            {
                return raw > 255 ? Math.Clamp(raw >> fineShift, 0, 255) : raw;
            }

            return ImportText.ParseDmxValue(node?.ToString(), 0);
        }

        private AttributeKind ChooseAttribute(string name, List<AttributeKind> attributes)
        {
            var meaningful = attributes.Where(a => a is not AttributeKind.NoFunction and not AttributeKind.Generic).ToList();
            if (meaningful.Count == 0)
            {
                return attributes.FirstOrDefault(AttributeKind.Generic);
            }

            var chosen = meaningful.GroupBy(a => a).OrderByDescending(g => g.Count()).First().Key;
            if (meaningful.Distinct().Count() > 1)
            {
                notes.Add($"Canal « {name} » : plusieurs fonctions ({string.Join(", ", meaningful.Distinct().Select(AttributeCatalog.Label))}), attribut retenu : {AttributeCatalog.Label(chosen)}.");
            }

            return chosen;
        }

        private Converted ConvertCapability(string channelName, JsonObject c, int fineShift, bool single)
        {
            var type = c["type"]?.GetValue<string>() ?? "Generic";
            var (min, max) = c["dmxRange"] is JsonArray range && range.Count == 2
                ? (range[0]!.GetValue<int>() >> fineShift, range[1]!.GetValue<int>() >> fineShift)
                : (0, 255);
            var comment = c["comment"]?.GetValue<string>();
            var parameter = ReadParameter(c);
            var kind = parameter is not null ? CapabilityKind.Progressive : CapabilityKind.Fixed;
            string? wheel = null;
            IReadOnlyList<string> colors = [];
            int? slotNumber = null;
            StrobeEffect? strobe = null;

            AttributeKind attribute;
            string label;
            switch (type)
            {
                case "NoFunction":
                    attribute = AttributeKind.NoFunction;
                    kind = CapabilityKind.NoFunction;
                    label = "Sans fonction";
                    break;
                case "ShutterStrobe":
                    attribute = AttributeKind.Shutter;
                    (strobe, kind, label) = c["shutterEffect"]?.GetValue<string>() switch
                    {
                        "Open" => (StrobeEffect.Open, CapabilityKind.Open, "Ouvert"),
                        "Closed" => (StrobeEffect.Closed, CapabilityKind.Closed, "Fermé"),
                        "Pulse" or "RampUp" or "RampDown" or "RampUpDown" => (StrobeEffect.Pulse, kind, "Pulsation"),
                        "Lightning" or "Spikes" => (StrobeEffect.Random, kind, "Éclairs"),
                        _ when c["randomTiming"]?.GetValue<bool>() == true => (StrobeEffect.Random, kind, "Strobe aléatoire"),
                        _ => (StrobeEffect.Strobe, kind, parameter is null ? "Strobe" : "Strobe lent → rapide"),
                    };
                    break;
                case "StrobeSpeed" or "StrobeDuration":
                    attribute = AttributeKind.Shutter;
                    label = "Vitesse du strobe";
                    break;
                case "Intensity":
                    attribute = AttributeKind.Intensity;
                    label = "Intensité";
                    break;
                case "ColorIntensity":
                    var color = c["color"]?.GetValue<string>();
                    attribute = ImportText.EmitterFromColor(color) ?? Unknown(channelName, $"couleur {color}");
                    label = AttributeCatalog.Label(attribute);
                    break;
                case "ColorPreset":
                    attribute = AttributeKind.ColorMacro;
                    colors = c["colors"] is JsonArray cs ? cs.Select(x => x!.GetValue<string>()).ToList() : [];
                    label = "Couleur prédéfinie";
                    break;
                case "ColorTemperature":
                    attribute = AttributeKind.ColorTemperature;
                    label = "Température de couleur";
                    break;
                case "Pan":
                    attribute = AttributeKind.Pan;
                    label = "Pan";
                    break;
                case "PanContinuous":
                    attribute = AttributeKind.PanContinuous;
                    kind = CapabilityKind.Rotation;
                    label = "Pan continu";
                    break;
                case "Tilt":
                    attribute = AttributeKind.Tilt;
                    label = "Tilt";
                    break;
                case "TiltContinuous":
                    attribute = AttributeKind.TiltContinuous;
                    kind = CapabilityKind.Rotation;
                    label = "Tilt continu";
                    break;
                case "PanTiltSpeed" or "PanTiltDuration":
                    attribute = AttributeKind.PanTiltSpeed;
                    label = "Vitesse Pan/Tilt";
                    break;
                case "WheelSlot" or "WheelShake" or "WheelRotation" or "WheelSlotRotation":
                    var wheelName = c["wheel"]?.GetValue<string>() ?? channelName;
                    var target = Wheels.FirstOrDefault(w => w.Name == wheelName);
                    wheel = target?.Key;
                    var isGobo = target?.Kind == WheelKind.Gobo || wheelName.Contains("gobo", StringComparison.OrdinalIgnoreCase);
                    attribute = type == "WheelSlotRotation" ? AttributeKind.GoboRotation : isGobo ? AttributeKind.Gobo : AttributeKind.ColorWheel;
                    if (type == "WheelSlot" && c["slotNumber"] is JsonValue sn && sn.TryGetValue<double>(out var slot))
                    {
                        slotNumber = (int)Math.Floor(slot);
                        var slotInfo = target is not null && slotNumber >= 1 && slotNumber <= target.Slots.Count ? target.Slots[slotNumber.Value - 1] : null;
                        colors = slotInfo?.Colors ?? [];
                        kind = CapabilityKind.WheelSlot;
                        label = slotInfo?.Name ?? string.Create(CultureInfo.CurrentCulture, $"Emplacement {slotNumber}");
                        if (slot % 1 != 0)
                        {
                            label = "Demi-emplacement";
                        }
                    }
                    else
                    {
                        kind = type == "WheelShake" ? kind : CapabilityKind.Rotation;
                        label = type switch
                        {
                            "WheelShake" => "Tremblement",
                            "WheelSlotRotation" => "Rotation du gobo",
                            _ => "Rotation de la roue",
                        };
                    }

                    break;
                case "Prism":
                    attribute = AttributeKind.Prism;
                    label = "Prisme";
                    break;
                case "PrismRotation":
                    attribute = AttributeKind.PrismRotation;
                    kind = CapabilityKind.Rotation;
                    label = "Rotation du prisme";
                    break;
                case "Focus":
                    attribute = AttributeKind.Focus;
                    label = "Focus";
                    break;
                case "Zoom" or "BeamAngle":
                    attribute = AttributeKind.Zoom;
                    label = "Zoom";
                    break;
                case "Iris" or "IrisEffect":
                    attribute = AttributeKind.Iris;
                    label = "Iris";
                    break;
                case "Frost" or "FrostEffect":
                    attribute = AttributeKind.Frost;
                    label = "Frost";
                    break;
                case "Effect":
                    attribute = AttributeKind.Program;
                    kind = parameter is null ? CapabilityKind.Program : kind;
                    label = c["effectName"]?.GetValue<string>() ?? c["effectPreset"]?.GetValue<string>() ?? "Programme";
                    break;
                case "EffectSpeed" or "EffectDuration" or "EffectParameter" or "Speed":
                    attribute = AttributeKind.ProgramSpeed;
                    label = "Vitesse";
                    break;
                case "SoundSensitivity":
                    attribute = AttributeKind.SoundSensitivity;
                    label = "Sensibilité son";
                    break;
                case "Fog" or "FogOutput" or "FogType":
                    attribute = AttributeKind.Smoke;
                    label = "Fumée";
                    break;
                case "Rotation":
                    attribute = AttributeKind.Rotation;
                    kind = CapabilityKind.Rotation;
                    label = "Rotation";
                    break;
                case "Maintenance":
                    var isReset = (comment ?? string.Empty).Contains("reset", StringComparison.OrdinalIgnoreCase);
                    attribute = isReset ? AttributeKind.Reset : AttributeKind.Maintenance;
                    label = isReset ? "Reset" : "Maintenance";
                    break;
                case "Generic":
                    attribute = AttributeKind.Generic;
                    label = "Générique";
                    break;
                default:
                    attribute = Unknown(channelName, $"type « {type} »");
                    label = type;
                    break;
            }

            var capability = new Capability
            {
                Min = min,
                Max = max,
                Kind = kind,
                Label = comment is { Length: > 0 } ? $"{label} – {comment}" : label,
                Strobe = strobe,
                Parameter = parameter,
                Colors = colors,
                WheelSlot = slotNumber,
            };
            return new Converted(attribute, capability, wheel, single && min == 0 && max == 255);
        }

        private AttributeKind Unknown(string channelName, string what)
        {
            notes.Add($"Canal « {channelName} » : {what} non reconnu, converti en « Générique ».");
            return AttributeKind.Generic;
        }

        private static ProgressiveParameter? ReadParameter(JsonObject c)
        {
            foreach (var (property, node) in c)
            {
                if (!property.EndsWith("Start", StringComparison.Ordinal))
                {
                    continue;
                }

                var nature = property[..^5];
                var start = ImportText.ParseValue(node?.ToString());
                var end = ImportText.ParseValue(c[nature + "End"]?.ToString());
                if (start is null || end is null)
                {
                    continue;
                }

                var label = nature switch
                {
                    "speed" => "vitesse",
                    "angle" => "angle",
                    "brightness" => "luminosité",
                    "duration" => "durée",
                    "colorTemperature" => "température",
                    "parameter" => "paramètre",
                    "distance" => "distance",
                    "soundSensitivity" => "sensibilité",
                    _ => nature,
                };
                return new ProgressiveParameter(label, start.Value.Value, end.Value.Value, start.Value.Unit ?? end.Value.Unit);
            }

            return null;
        }

        private string UnusedSlot()
        {
            _unusedSlots++;
            var key = ImportText.UniqueKey(string.Create(CultureInfo.InvariantCulture, $"inutilise-{_unusedSlots}"), _usedKeys);
            Channels.Add(new ChannelDefinition { Key = key, Name = "Sans fonction", Attribute = AttributeKind.NoFunction });
            return key;
        }

        private sealed record Converted(AttributeKind Attribute, Capability Capability, string? Wheel, bool IsWholeRange);
    }
}
