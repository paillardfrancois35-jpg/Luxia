namespace Luxia.Fixtures.Model;

/// <summary>Noms français des catégories d'appareils, au pluriel (« Tous les PAR », « Toutes les lyres »).</summary>
public static class FixtureCategoryLabels
{
    /// <summary>Nom au pluriel d'une catégorie.</summary>
    public static string Plural(FixtureCategory category) => category switch
    {
        FixtureCategory.Par => "PAR",
        FixtureCategory.LedBar => "barres LED",
        FixtureCategory.MovingHead => "lyres",
        FixtureCategory.Effect => "effets",
        FixtureCategory.Strobe => "stroboscopes",
        FixtureCategory.Uv => "UV",
        FixtureCategory.Smoke => "machines à fumée",
        FixtureCategory.Laser => "lasers",
        FixtureCategory.Dimmer => "gradateurs",
        _ => "autres",
    };
}
