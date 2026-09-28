using Luxia.Fixtures.Model;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>Règles d'organisation des couches (doc 17 §1).</summary>
public static class LayerRules
{
    /// <summary>
    /// Familles d'attributs qu'une scène touche hors de celles de sa couche (COU-008) : avertissement non bloquant qui
    /// aide à garder « une couche par famille ». Une couche sans famille déclarée accepte tout ; les valeurs données par
    /// canal précis ne sont pas vérifiées (leur attribut dépend de l'appareil).
    /// </summary>
    /// <param name="scene">Scène.</param>
    /// <param name="layer">Sa couche.</param>
    /// <param name="palettes">Palettes (le type d'une palette référencée dit sa famille).</param>
    public static IReadOnlyList<AttributeFamily> OutOfFamily(Scene scene, Layer layer, PaletteSet palettes)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(palettes);
        if (layer.Families.Count == 0)
        {
            return [];
        }

        var kinds = palettes.Palettes.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First().Kind);
        var found = new SortedSet<AttributeFamily>();
        foreach (var value in scene.Steps.SelectMany(s => s.Values))
        {
            AttributeFamily? family = value switch
            {
                { Attribute: { } attribute } => AttributeCatalog.Get(attribute).Family,
                { Color: not null } => AttributeFamily.Color,
                { PaletteId: { } id } when kinds.TryGetValue(id, out var kind) => FamilyOf(kind),
                _ => null,
            };
            if (family is { } f && f is not (AttributeFamily.Control or AttributeFamily.Other) && !layer.Families.Contains(f))
            {
                found.Add(f);
            }
        }

        foreach (var effect in scene.Steps.SelectMany(s => s.Effects))
        {
            var family = EffectRules.Family(effect);
            if (family is not (AttributeFamily.Control or AttributeFamily.Other) && !layer.Families.Contains(family))
            {
                found.Add(family);
            }
        }

        return [.. found];
    }

    /// <summary>Libellé d'une famille d'attributs, pour les messages.</summary>
    public static string Label(AttributeFamily family) => family switch
    {
        AttributeFamily.Intensity => "intensité",
        AttributeFamily.Color => "couleur",
        AttributeFamily.Position => "position",
        AttributeFamily.Beam => "faisceau",
        AttributeFamily.EffectMotion => "mouvement d'effet",
        AttributeFamily.Programs => "programmes",
        AttributeFamily.Atmosphere => "atmosphère",
        AttributeFamily.Control => "contrôle",
        _ => "divers",
    };

    private static AttributeFamily FamilyOf(PaletteKind kind) => kind switch
    {
        PaletteKind.Position => AttributeFamily.Position,
        PaletteKind.Beam => AttributeFamily.Beam,
        PaletteKind.Intensity => AttributeFamily.Intensity,
        _ => AttributeFamily.Color,
    };
}
