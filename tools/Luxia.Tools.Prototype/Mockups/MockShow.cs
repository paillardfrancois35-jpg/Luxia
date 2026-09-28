using Avalonia.Media;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>Couche fictive (reprend les couches par défaut de LuXia).</summary>
internal sealed record MockLayer(string Name, string Icon, Color Color, int Master, bool Paused, IReadOnlyList<MockScene> Scenes);

/// <summary>Scène fictive (noms et couleurs du show de référence).</summary>
internal sealed record MockScene(string Name, Color Color, double? Progress = null, string? StepInfo = null);

/// <summary>Appareil fictif placé sur le plan (lieu « Générique », 12 × 8 m).</summary>
internal sealed record MockFixture(string Id, string Name, string Kind, double X, double Y, Color Output);

/// <summary>Données des maquettes : le show de référence, en fictif (aucun fichier lu).</summary>
internal static class MockShow
{
    public const double VenueWidth = 12;
    public const double VenueDepth = 8;

    private static Color C(string hex) => Color.Parse(hex);

    /// <summary>Couches et scènes. Les scènes « qui jouent » ont une progression.</summary>
    public static IReadOnlyList<MockLayer> Layers { get; } =
    [
        new("Intensité", "☀", C("#E3B341"), 100, false,
        [
            new("Plein feu", C("#E3B341"), 1),
            new("Intensité 50 %", C("#A67C00")),
            new("Fondu lent (4 s)", C("#BC4CE0")),
            new("Instantané", C("#39C5CF")),
        ]),
        new("Couleurs", "◐", C("#DB61A2"), 100, false,
        [
            new("Blanc chaud sur les 4 PAR", C("#FFC773")),
            new("Bleu sur tout le parc", C("#1F6FEB")),
            new("Chenillard 4 couleurs", C("#DB61A2"), 0.55, "étape 2 / 4"),
            new("Rouge – couleur seule", C("#FF0000")),
            new("Ambre – couleur seule", C("#FFB000")),
            new("Bleu – couleur seule", C("#0000FF")),
            new("Roue de couleur qui bascule", C("#F0883E")),
        ]),
        new("Mouvements", "↻", C("#58A6FF"), 80, false,
        [
            new("Lyres sur 3 positions", C("#58A6FF"), 0.3, "étape 1 / 3"),
            new("Lyres : piste centre", C("#58A6FF")),
            new("Lyres : plafond", C("#8B949E")),
            new("Vague gauche → droite", C("#E3B341")),
        ]),
        new("Faisceau", "◎", C("#A371F7"), 100, false, []),
        new("Effets", "✦", C("#F0883E"), 100, false,
        [
            new("Strobe PAR (plafonné à 10 s)", C("#F0883E")),
            new("All Strobes", C("#FFFFFF")),
        ]),
        new("Ambiance", "☁", C("#8957E5"), 60, false,
        [
            new("UV plein", C("#8957E5"), 1),
            new("Fumée courte (3 s)", C("#8B949E")),
            new("Fumée longue (plafonnée à 10 s)", C("#6E7681")),
        ]),
        new("Flashs", "⚡", C("#F85149"), 100, false,
        [
            new("Flash blanc", C("#FFFFFF")),
            new("Strobe flash", C("#FFFFFF")),
            new("Blackout partiel (sauf UV)", C("#30363D")),
        ]),
    ];

    /// <summary>Le parc réel de l'utilisateur, placé comme dans le lieu « Générique » (scène en haut, public en bas).</summary>
    public static IReadOnlyList<MockFixture> Fixtures { get; } =
    [
        new("par1", "PAR 1", "PAR", 1.5, 5.5, C("#FF4FA0")),
        new("par2", "PAR 2", "PAR", 1.5, 4.5, C("#4F7BFF")),
        new("par3", "PAR 3", "PAR", 1.5, 3.3, C("#FF4FA0")),
        new("par4", "PAR 4", "PAR", 1.5, 1.9, C("#4F7BFF")),
        new("gpar1", "Gros PAR 1", "PAR", 9.6, 5.5, C("#FFC773")),
        new("gpar2", "Gros PAR 2", "PAR", 9.6, 4.3, C("#FFC773")),
        new("barre1", "Barre 1", "Barre", 3.0, 0.45, C("#FF4FA0")),
        new("barre2", "Barre 2", "Barre", 9.4, 0.45, C("#4F7BFF")),
        new("lyre1", "Lyre 1", "Lyre", 4.3, 2.3, C("#FFFFFF")),
        new("lyre2", "Lyre 2", "Lyre", 7.2, 2.3, C("#FFFFFF")),
        new("effet", "Effet multi-têtes", "Effet", 5.0, 0.6, C("#30363D")),
        new("uv1", "UV 1", "UV", 3.5, 7.2, C("#8957E5")),
        new("uv2", "UV 2", "UV", 8.5, 7.2, C("#8957E5")),
        new("fumee", "Fumée", "Fumée", 10.6, 1.6, C("#30363D")),
    ];

    public const string Chaser = "Chenillard 4 couleurs";
    public const string LyresScene = "Lyres sur 3 positions";

    /// <summary>Scène par son nom.</summary>
    public static MockScene Find(string name) => Layers.SelectMany(l => l.Scenes).First(s => s.Name == name);

    /// <summary>Étapes d'une scène : nom, couleur, fondu (s), maintien (s).</summary>
    public static IReadOnlyList<(string Label, Color Color, double Fade, double Hold)> Steps(string scene) => scene == LyresScene
        ?
        [
            ("Piste centre", C("#58A6FF"), 2.0, 4.0),
            ("Plafond", C("#8B949E"), 2.0, 4.0),
            ("Scène", C("#A371F7"), 2.0, 4.0),
        ]
        :
        [
            ("Rose", C("#FF4FA0"), 0.5, 1.0),
            ("Bleu", C("#4F7BFF"), 0.5, 1.0),
            ("Ambre", C("#FFB000"), 0.5, 1.0),
            ("Vert", C("#3FB950"), 1.5, 0.5),
        ];

    /// <summary>Résumé d'une scène sous son titre.</summary>
    public static string Summary(string scene) => scene == LyresScene
        ? "Couche Mouvements · joue en ce moment · 3 étapes · 2 appareils"
        : "Couche Couleurs · joue en ce moment · 4 étapes · 8 appareils";

    /// <summary>Contenu d'une étape : qui, quoi, utilisé ou non.</summary>
    public static IReadOnlyList<(string Who, string What, bool Used)> StepContent(string scene, int step) => scene == LyresScene
        ?
        [
            ("Lyre 1, 2", $"Position : palette « {Steps(scene)[step].Label} »", true),
            ("Lyre 1, 2", "Vitesse des moteurs : —", false),
            ("Lyre 1, 2", "Couleur : — (couche Couleurs)", false),
        ]
        :
        [
            ("PAR 1 à 4", "Couleur : bleu (0 / 30 / 100 %)", true),
            ("PAR 1 à 4", "Intensité : 100 %", true),
            ("Barres 1, 2", "Couleur : bleu", true),
            ("Gros PAR 1, 2", "Couleur : —", false),
        ];

    /// <summary>Note de la scène.</summary>
    public static string Notes(string scene) => scene == LyresScene
        ? "Les lyres parcourent trois palettes ; les zones du lieu s'appliquent toujours."
        : "Chenillard doux pour l'accueil ; ralentir sur les slows (molette Vitesse).";

    /// <summary>Palettes de couleur du projet.</summary>
    public static IReadOnlyList<(string Name, Color Color)> ColorPalettes { get; } =
    [
        ("Rouge", C("#FF0000")), ("Ambre", C("#FFB000")), ("Blanc chaud", C("#FFC773")), ("Bleu", C("#0000FF")),
        ("Rose", C("#FF4FA0")), ("Vert", C("#00FF00")), ("UV", C("#8957E5")), ("Blanc", C("#FFFFFF")),
    ];

    /// <summary>Palettes de position (lieu « Générique »).</summary>
    public static IReadOnlyList<string> PositionPalettes { get; } = ["Piste centre", "Plafond", "Scène", "Boule à facettes", "Public ⚠"];

    /// <summary>Journal fictif, le plus récent en tête.</summary>
    public static IReadOnlyList<string> Log { get; } =
    [
        "21:42:31  Lyre 1, Lyre 2 : couleur réglée",
        "21:42:18  Lancer « Lyres sur 3 positions » (Mouvements)",
        "21:42:02  Lancer « Chenillard 4 couleurs » (Couleurs)",
        "21:41:55  Lancer « UV plein » (Ambiance)",
        "21:41:40  Lancer « Plein feu » (Intensité)",
        "21:41:12  Sortie Arduino : prête (COM5, 40 trames / s)",
        "21:41:10  Projet ouvert : Show de travail (lieu Générique)",
    ];
}
