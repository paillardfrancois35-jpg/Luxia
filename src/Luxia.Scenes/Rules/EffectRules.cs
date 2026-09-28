using System.Globalization;
using Luxia.Fixtures.Model;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>
/// Règles d'un effet de scène (doc 16 §6) indépendantes du patch : réglages hors bornes, couleurs manquantes.
/// Utilisées par <c>valider</c> (GEN-131) et par le panneau « Effets » ; la compilation signale le reste
/// (cible vide, attribut absent des appareils).
/// </summary>
public static class EffectRules
{
    /// <summary>Durée de cycle minimale (au-delà de 50 Hz, le moteur à 40 Hz ne peut plus rendre la forme).</summary>
    public const double MinPeriodSeconds = 0.05;

    /// <summary>Problèmes d'un effet, en français, avec le champ concerné.</summary>
    public static IEnumerable<(string Field, string Message)> Problems(SceneEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (effect.Period.Value <= 0)
        {
            yield return ("period", "durée d'un cycle nulle : 1 s sera utilisée");
        }
        else if (effect.Period.ToSeconds(120) < MinPeriodSeconds)
        {
            yield return ("period", string.Create(CultureInfo.CurrentCulture, $"cycle plus court que {MinPeriodSeconds} s : trop rapide pour être rendu à 40 images par seconde"));
        }

        if (effect.Size < 0)
        {
            yield return ("size", "taille négative");
        }
        else if (!effect.IsPosition && !effect.IsColor && effect.Size > 1)
        {
            yield return ("size", "taille au-delà de 1 (100 %) : elle sera bornée");
        }

        if (effect.Center is < 0 or > 1)
        {
            yield return ("center", "centre hors de 0 à 1 : il sera borné");
        }

        if (effect.DutyCycle is <= 0 or > 1)
        {
            yield return ("dutyCycle", "rapport cyclique hors de 0 à 1");
        }

        if (effect.Spread < 0)
        {
            yield return ("spread", "décalage négatif : utiliser le sens « arrière »");
        }

        if (effect.PhaseMode == EffectPhaseMode.Groups && effect.GroupSize < 1)
        {
            yield return ("groupSize", "taille de groupe inférieure à 1");
        }

        if (effect.IsColor && effect.Shape != SceneEffectShape.Rainbow && effect.ThemeId is null && effect.Colors.Count == 0)
        {
            yield return ("colors", "aucune couleur ni thème");
        }

        if (!effect.IsColor && !effect.IsPosition && AttributeCatalog.Get(effect.Attribute).Family is AttributeFamily.Control)
        {
            yield return ("attribute", "un effet sur un attribut de contrôle (reset, mode…) n'a pas de sens");
        }
    }

    /// <summary>Famille d'attributs animée par un effet (vérification des familles d'une couche, COU-008).</summary>
    public static AttributeFamily Family(SceneEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        return effect.IsColor ? AttributeFamily.Color
            : effect.IsPosition ? AttributeFamily.Position
            : AttributeCatalog.Get(effect.Attribute).Family;
    }
}
