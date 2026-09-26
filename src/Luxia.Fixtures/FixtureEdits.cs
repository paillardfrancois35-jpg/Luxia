using System.Globalization;
using Luxia.Fixtures.Model;

namespace Luxia.Fixtures;

/// <summary>
/// Opérations d'édition d'un modèle, sans effet de bord : chacune renvoie un nouveau modèle.
/// Utilisées par l'éditeur (annuler / rétablir = retour à un état précédent) et testables sans interface.
/// </summary>
public static class FixtureEdits
{
    /// <summary>Nouveau modèle vide avec un mode d'un canal.</summary>
    public static FixtureType NewFixture(string manufacturer = "Nouveau fabricant", string model = "Nouveau modèle") => new()
    {
        Manufacturer = manufacturer,
        Model = model,
        Channels = [new ChannelDefinition { Key = "canal-1", Name = "Canal 1", Attribute = AttributeKind.Intensity }],
        Modes = [new FixtureMode { Name = "1 canal", ShortName = "1CH", Channels = [new ModeChannel("canal-1")] }],
    };

    /// <summary>Copie dérivée d'un modèle (BIB-010) : nouvel identifiant, origine mentionnée.</summary>
    public static FixtureType Derive(FixtureType source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source with
        {
            Id = Guid.NewGuid(),
            Model = source.Model + " (copie)",
            Version = 1,
            Source = FixtureSource.Manual,
            DerivedFrom = source.Id,
            Notes = string.IsNullOrWhiteSpace(source.Notes)
                ? $"Dérivé de {source.DisplayName}."
                : $"Dérivé de {source.DisplayName}. {source.Notes}",
        };
    }

    /// <summary>Modifie une définition de canal.</summary>
    public static FixtureType UpdateChannel(FixtureType fixture, string key, Func<ChannelDefinition, ChannelDefinition> change)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(change);
        return fixture with { Channels = [.. fixture.Channels.Select(c => c.Key == key ? change(c) : c)] };
    }

    /// <summary>Modifie un mode.</summary>
    public static FixtureType UpdateMode(FixtureType fixture, int modeIndex, Func<FixtureMode, FixtureMode> change)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(change);
        return fixture with { Modes = [.. fixture.Modes.Select((m, i) => i == modeIndex ? change(m) : m)] };
    }

    /// <summary>Ajoute un mode vide.</summary>
    public static FixtureType AddMode(FixtureType fixture, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var modeName = name ?? UniqueName("Mode", fixture.Modes.Select(m => m.Name));
        return fixture with { Modes = [.. fixture.Modes, new FixtureMode { Name = modeName }] };
    }

    /// <summary>Supprime un mode.</summary>
    public static FixtureType RemoveMode(FixtureType fixture, int modeIndex)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        return fixture with { Modes = [.. fixture.Modes.Where((_, i) => i != modeIndex)] };
    }

    /// <summary>Ajoute une position à un mode (à la fin, ou à la position indiquée, 0 = première).</summary>
    public static FixtureType AddSlot(FixtureType fixture, int modeIndex, ModeChannel slot, int? position = null) =>
        UpdateMode(fixture, modeIndex, m =>
        {
            var slots = m.Channels.ToList();
            slots.Insert(Math.Clamp(position ?? slots.Count, 0, slots.Count), slot);
            return m with { Channels = slots };
        });

    /// <summary>Retire une position d'un mode (la définition reste disponible pour les autres modes).</summary>
    public static FixtureType RemoveSlot(FixtureType fixture, int modeIndex, int position) =>
        UpdateMode(fixture, modeIndex, m => m with { Channels = [.. m.Channels.Where((_, i) => i != position)] });

    /// <summary>Déplace une position dans un mode (glisser-déposer, BIB-021).</summary>
    public static FixtureType MoveSlot(FixtureType fixture, int modeIndex, int from, int to) =>
        UpdateMode(fixture, modeIndex, m =>
        {
            if (from < 0 || from >= m.Channels.Count || from == to)
            {
                return m;
            }

            var slots = m.Channels.ToList();
            var item = slots[from];
            slots.RemoveAt(from);
            slots.Insert(Math.Clamp(to, 0, slots.Count), item);
            return m with { Channels = slots };
        });

    /// <summary>Crée une définition de canal et renvoie sa clé.</summary>
    public static (FixtureType Fixture, string Key) NewChannel(FixtureType fixture, string name, AttributeKind attribute)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var key = UniqueName("canal", fixture.Channels.Select(c => c.Key), separator: "-");
        return (fixture with { Channels = [.. fixture.Channels, new ChannelDefinition { Key = key, Name = name, Attribute = attribute }] }, key);
    }

    /// <summary>Supprime une définition et toutes ses positions dans les modes.</summary>
    public static FixtureType RemoveChannel(FixtureType fixture, string key)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        return fixture with
        {
            Channels = [.. fixture.Channels.Where(c => c.Key != key)],
            Modes = [.. fixture.Modes.Select(m => m with { Channels = [.. m.Channels.Where(s => s.Channel != key)] })],
        };
    }

    /// <summary>
    /// Change la résolution d'un canal. Passage en 16 bits : l'octet fin est ajouté juste après l'octet grossier dans
    /// chaque mode (il peut ensuite être déplacé, BIB-003). Retour en 8 bits : les octets fins sont retirés.
    /// </summary>
    public static FixtureType SetResolution(FixtureType fixture, string key, ChannelResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var updated = UpdateChannel(fixture, key, c => c with { Resolution = resolution });
        return updated with
        {
            Modes = [.. updated.Modes.Select(m =>
            {
                var slots = m.Channels.Where(s => !(s.Channel == key && s.Part == ChannelPart.Fine)).ToList();
                if (resolution == ChannelResolution.Bit16)
                {
                    var coarse = slots.FindIndex(s => s.Channel == key);
                    if (coarse >= 0)
                    {
                        slots.Insert(coarse + 1, new ModeChannel(key, ChannelPart.Fine));
                    }
                }

                return m with { Channels = slots };
            })],
        };
    }

    /// <summary>Remplace les plages d'un canal (triées par borne basse).</summary>
    public static FixtureType SetCapabilities(FixtureType fixture, string key, IEnumerable<Capability> capabilities) =>
        UpdateChannel(fixture, key, c => c with { Capabilities = [.. capabilities.OrderBy(r => r.Min)] });

    /// <summary>Modifie une roue.</summary>
    public static FixtureType UpdateWheel(FixtureType fixture, int index, Func<Wheel, Wheel> change)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(change);
        return fixture with { Wheels = [.. fixture.Wheels.Select((w, i) => i == index ? change(w) : w)] };
    }

    /// <summary>Ajoute une roue.</summary>
    public static FixtureType AddWheel(FixtureType fixture, WheelKind kind)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var key = UniqueName("roue", fixture.Wheels.Select(w => w.Key), separator: "-");
        var name = kind == WheelKind.Color ? "Roue de couleur" : "Roue de gobos";
        return fixture with { Wheels = [.. fixture.Wheels, new Wheel { Key = key, Name = name, Kind = kind }] };
    }

    /// <summary>Supprime une roue (les canaux qui la référençaient n'en ont plus).</summary>
    public static FixtureType RemoveWheel(FixtureType fixture, int index)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        if (index < 0 || index >= fixture.Wheels.Count)
        {
            return fixture;
        }

        var key = fixture.Wheels[index].Key;
        return fixture with
        {
            Wheels = [.. fixture.Wheels.Where((_, i) => i != index)],
            Channels = [.. fixture.Channels.Select(c => c.Wheel == key ? c with { Wheel = null } : c)],
        };
    }

    /// <summary>Nom unique « base 1 », « base 2 »…</summary>
    public static string UniqueName(string baseName, IEnumerable<string> existing, string separator = " ")
    {
        ArgumentNullException.ThrowIfNull(existing);
        var used = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; ; i++)
        {
            var name = string.Create(CultureInfo.InvariantCulture, $"{baseName}{separator}{i}");
            if (!used.Contains(name))
            {
                return name;
            }
        }
    }
}
