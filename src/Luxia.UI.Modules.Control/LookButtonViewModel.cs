using Luxia.Scenes.Model;

namespace Luxia.UI.Modules.Control;

/// <summary>Un look affiché : nom, couleur, actions lisibles.</summary>
/// <param name="Look">Look.</param>
/// <param name="Lines">Actions en français, une par ligne.</param>
/// <param name="Key">Touche qui le joue (« F1 » à « F12 »), ou vide.</param>
public sealed record LookButtonViewModel(Look Look, IReadOnlyList<string> Lines, string Key = "")
{
    /// <summary>Nom.</summary>
    public string Name => Look.Name;

    /// <summary>Couleur.</summary>
    public string Color => Look.Color;

    /// <summary>Infobulle : notes puis actions.</summary>
    public string Tip => string.Join(Environment.NewLine, (Look.Notes is { } notes ? [notes, string.Empty] : Array.Empty<string>()).Concat(Lines));
}
