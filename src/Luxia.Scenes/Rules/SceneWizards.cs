using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>
/// Assistants de création (SCN-014) : ils génèrent les étapes d'une scène en une opération, à partir des appareils
/// choisis (dans l'ordre) et de palettes. Chaque étape allume les appareils à 100 % (MOT-041), durées réglables.
/// </summary>
public static class SceneWizards
{
    /// <summary>
    /// Chenillard de couleurs : autant d'étapes que de couleurs ; à l'étape k, le membre i prend la couleur (i + k) :
    /// les couleurs défilent d'un appareil à l'autre (4 PAR × 4 couleurs = 4 étapes).
    /// </summary>
    public static IReadOnlyList<SceneStep> ColorChase(IReadOnlyList<ValueTarget> members, IReadOnlyList<Guid> colorPalettes, Duration hold, Duration fade)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(colorPalettes);
        if (members.Count == 0 || colorPalettes.Count == 0)
        {
            return [];
        }

        return [.. Enumerable.Range(0, colorPalettes.Count).Select(k => Step(
            members.Select((m, i) => new SceneValue { Target = m, PaletteId = colorPalettes[(i + k) % colorPalettes.Count] }),
            members,
            hold,
            fade))];
    }

    /// <summary>Alternance de deux palettes : un membre sur deux en A, les autres en B, puis l'inverse (2 étapes).</summary>
    public static IReadOnlyList<SceneStep> Alternate(IReadOnlyList<ValueTarget> members, Guid paletteA, Guid paletteB, Duration hold, Duration fade)
    {
        ArgumentNullException.ThrowIfNull(members);
        if (members.Count == 0)
        {
            return [];
        }

        return [.. new[] { (paletteA, paletteB), (paletteB, paletteA) }.Select(pair => Step(
            members.Select((m, i) => new SceneValue { Target = m, PaletteId = i % 2 == 0 ? pair.Item1 : pair.Item2 }),
            members,
            hold,
            fade))];
    }

    /// <summary>Balayage de positions : une étape par palette de position, tous les membres ensemble.</summary>
    public static IReadOnlyList<SceneStep> PositionSweep(IReadOnlyList<ValueTarget> members, IReadOnlyList<Guid> positionPalettes, Duration hold, Duration fade)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(positionPalettes);
        if (members.Count == 0 || positionPalettes.Count == 0)
        {
            return [];
        }

        return [.. positionPalettes.Select(palette => Step(members.Select(m => new SceneValue { Target = m, PaletteId = palette }), members, hold, fade))];
    }

    private static SceneStep Step(IEnumerable<SceneValue> values, IReadOnlyList<ValueTarget> members, Duration hold, Duration fade) => new()
    {
        Fade = fade,
        Hold = hold,
        Values = [.. values, .. members.Select(m => new SceneValue { Target = m, Attribute = AttributeKind.Intensity, Level = 1 })],
    };
}
