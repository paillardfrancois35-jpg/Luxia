using System.Globalization;
using Luxia.Fixtures.Model;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>
/// Règles du programmeur (doc 16 §4) : ce qu'une valeur règle sur sa cible (« emplacement »), remplacement d'une
/// valeur par une autre, enregistrement dans une étape (remplacer / fusionner, SCN-033), « allumer en coloriant »
/// (MOT-041), copie en miroir (SCN-038).
/// </summary>
public static class ProgrammerRules
{
    /// <summary>Emplacement couleur : couleur logique, palette couleur ou émetteur réglé à la main.</summary>
    public const string ColorSlot = "couleur";

    /// <summary>Emplacement intensité : niveau ou palette d'intensité.</summary>
    public const string IntensitySlot = "attr:Intensity";

    /// <summary>Emplacement position : Pan, Tilt ou palette de position.</summary>
    public const string PositionSlot = "position";

    /// <summary>Clé stable d'une cible (deux cibles égales ont la même clé).</summary>
    public static string TargetKey(ValueTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.FixtureId is { } fixture)
        {
            return string.Create(CultureInfo.InvariantCulture, $"f:{fixture}:{target.Cell}");
        }

        if (target.SelectionId is { } selection)
        {
            return $"s:{selection}";
        }

        return target.Auto is { } auto ? $"a:{auto.Kind}:{auto.Category}:{auto.Model}" : "?";
    }

    /// <summary>
    /// Ce qu'une valeur règle sur sa cible : deux valeurs de même cible et même emplacement se remplacent.
    /// Pan et Tilt ont chacun leur emplacement, mais une palette de position les règle tous les deux.
    /// </summary>
    public static string Slot(SceneValue value, Func<Guid, Palette?> palettes)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(palettes);
        if (value.PaletteId is { } id)
        {
            return palettes(id)?.Kind switch
            {
                PaletteKind.Color => ColorSlot,
                PaletteKind.Intensity => IntensitySlot,
                PaletteKind.Position => PositionSlot,
                _ => $"palette:{id}",
            };
        }

        if (value.Color is not null)
        {
            return ColorSlot;
        }

        if (value.Channel is { } channel)
        {
            return $"canal:{channel}";
        }

        return value.Attribute switch
        {
            AttributeKind.Intensity or AttributeKind.CellIntensity => IntensitySlot,
            { } attribute => $"attr:{attribute}",
            null => "?",
        };
    }

    /// <summary>Famille d'un emplacement, pour « Retirer » un groupe d'attributs d'un coup (SCN-032).</summary>
    public static string Family(string slot, AttributeKind? attribute = null)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (slot == ColorSlot || slot == IntensitySlot || slot == PositionSlot)
        {
            return slot;
        }

        return attribute is AttributeKind.Pan or AttributeKind.Tilt ? PositionSlot : slot;
    }

    /// <summary>Ajoute (ou remplace) une valeur : celle de même cible et même emplacement disparaît.</summary>
    public static IReadOnlyList<SceneValue> Set(IReadOnlyList<SceneValue> values, SceneValue value, Func<Guid, Palette?> palettes)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(value);
        var key = (TargetKey(value.Target), Slot(value, palettes));
        var result = values.Where(v => (TargetKey(v.Target), Slot(v, palettes)) != key).ToList();

        // Une palette de position remplace aussi les réglages Pan / Tilt de la même cible, et réciproquement.
        if (key.Item2 == PositionSlot)
        {
            result.RemoveAll(v => TargetKey(v.Target) == key.Item1 && v.Attribute is AttributeKind.Pan or AttributeKind.Tilt);
        }
        else if (value.Attribute is AttributeKind.Pan or AttributeKind.Tilt)
        {
            result.RemoveAll(v => TargetKey(v.Target) == key.Item1 && Slot(v, palettes) == PositionSlot);
        }

        result.Add(value);
        return result;
    }

    /// <summary>Retire des valeurs : celles des cibles données (toutes si <c>null</c>) dont l'emplacement est de la famille donnée.</summary>
    public static IReadOnlyList<SceneValue> Remove(
        IReadOnlyList<SceneValue> values, IReadOnlyCollection<string>? targetKeys, string family, Func<Guid, Palette?> palettes)
    {
        ArgumentNullException.ThrowIfNull(values);
        return [.. values.Where(v =>
            (targetKeys is not null && !targetKeys.Contains(TargetKey(v.Target)))
            || Family(Slot(v, palettes), v.Attribute) != family)];
    }

    /// <summary>« Remplacer » (SCN-033) : l'étape prend exactement les valeurs du programmeur.</summary>
    public static SceneStep Replace(SceneStep step, IReadOnlyList<SceneValue> programmer)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(programmer);
        return step with { Values = [.. programmer] };
    }

    /// <summary>« Fusionner » (SCN-033) : les valeurs du programmeur remplacent celles de même cible et même emplacement, le reste de l'étape est gardé.</summary>
    public static SceneStep Merge(SceneStep step, IReadOnlyList<SceneValue> programmer, Func<Guid, Palette?> palettes)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(programmer);
        IReadOnlyList<SceneValue> values = step.Values;
        foreach (var value in programmer)
        {
            values = Set(values, value, palettes);
        }

        return step with { Values = values };
    }

    /// <summary>
    /// « Allumer en coloriant » (MOT-041) : toute cible qui reçoit une couleur sans aucune valeur d'intensité dans le
    /// programmeur reçoit l'intensité 100 % à l'enregistrement. Une intensité réglée explicitement (même à 0) est respectée.
    /// </summary>
    public static IReadOnlyList<SceneValue> LightWhenColoring(IReadOnlyList<SceneValue> values, Func<Guid, Palette?> palettes)
    {
        ArgumentNullException.ThrowIfNull(values);
        var lit = values.Where(v => Slot(v, palettes) == IntensitySlot).Select(v => TargetKey(v.Target)).ToHashSet();
        var result = values.ToList();
        foreach (var value in values.Where(v => Slot(v, palettes) == ColorSlot))
        {
            if (lit.Add(TargetKey(value.Target)))
            {
                result.Add(new SceneValue { Target = value.Target, Attribute = AttributeKind.Intensity, Level = 1 });
            }
        }

        return result;
    }

    /// <summary>Copie « en miroir » (SCN-038) : Pan inversé (lyres symétriques), le reste inchangé.</summary>
    public static SceneValue Mirror(SceneValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Attribute == AttributeKind.Pan && value.Level is { } level ? value with { Level = 1 - level } : value;
    }
}
