namespace Luxia.Tools.Prototype.Mockups;

/// <summary>Onglet affiché dans « Réglages des appareils ».</summary>
internal enum MockTab
{
    Color,
    Position,
}

/// <summary>Une maquette : un mode, une scène en édition, une sélection, un onglet.</summary>
/// <param name="FileName">Nom de l'image.</param>
/// <param name="Title">Titre court (légende).</param>
/// <param name="Mode">Mode d'édition affiché.</param>
/// <param name="Tab">Onglet des réglages.</param>
/// <param name="Selected">Appareils sélectionnés (identifiants de <see cref="MockShow.Fixtures"/>).</param>
/// <param name="EditStep">Étape sélectionnée de la scène en édition (0 = première).</param>
/// <param name="ZoneEditing">La grille Pan / Tilt édite les zones.</param>
/// <param name="EditScene">Scène sélectionnée pour l'édition (bande de droite du bouton de scène).</param>
internal sealed record MockScenario(
    string FileName,
    string Title,
    MockMode Mode,
    MockTab Tab,
    IReadOnlyList<string> Selected,
    int EditStep,
    bool ZoneEditing,
    string EditScene = MockShow.Chaser)
{
    public static IReadOnlyList<MockScenario> All { get; } =
    [
        new("maquette-1-controle-live", "Contrôle · LIVE : on joue, on retouche deux lyres en direct", MockMode.Live, MockTab.Color, ["lyre1", "lyre2"], 1, false),
        new("maquette-2-controle-edition", "Contrôle · ÉDITION : on corrige l'étape 2 du chenillard sur les 4 PAR", MockMode.Edit, MockTab.Color, ["par1", "par2", "par3", "par4"], 1, false),
        new("maquette-3-controle-aveugle", "Contrôle · AVEUGLE : on prépare la position des lyres sans rien changer à la sortie", MockMode.Blind, MockTab.Position, ["lyre1", "lyre2"], 2, false, MockShow.LyresScene),
        new("maquette-4-controle-zones", "Contrôle · zones de la lyre 1 (propres au lieu) : interdite (public) et permise (limites)", MockMode.Edit, MockTab.Position, ["lyre1"], 0, true, MockShow.LyresScene),
    ];
}
