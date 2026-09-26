namespace Luxia.Patch.Model;

/// <summary>
/// Appareil patché (doc 13 §3) : un modèle de la bibliothèque, dans un mode donné, à une adresse d'un univers.
/// Le modèle référencé est la <b>copie du projet</b> (GEN-053), jamais directement la bibliothèque partagée.
/// </summary>
public sealed record PatchedFixture
{
    /// <summary>Identifiant stable (GEN-052), référencé par les sélections et (plus tard) les scènes.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Modèle utilisé (identifiant dans la copie du projet).</summary>
    public required Guid FixtureTypeId { get; init; }

    /// <summary>Nom du mode utilisé (<see cref="Luxia.Fixtures.Model.FixtureMode.Name"/>).</summary>
    public required string ModeName { get; init; }

    /// <summary>Univers (1 = premier).</summary>
    public int Universe { get; init; } = 1;

    /// <summary>Adresse de départ (1 à 512).</summary>
    public required int Address { get; init; }

    /// <summary>Nom affiché (« PAR 1 »).</summary>
    public required string Name { get; init; }

    /// <summary>Numéro court affiché dans les listes et au simulateur (INST-017).</summary>
    public int Number { get; init; }

    /// <summary>Couleur d'affichage (« #RRGGBB »).</summary>
    public string Color { get; init; } = "#58A6FF";

    /// <summary>
    /// Groupe de jumeaux (INST-014) : les appareils qui partagent ce même identifiant peuvent partager une adresse
    /// (même modèle, même mode requis) ; reçoivent les mêmes valeurs. <c>null</c> = appareil normal.
    /// </summary>
    public Guid? TwinGroupId { get; init; }

    /// <summary>Options de montage (INST-021).</summary>
    public FixtureOptions Options { get; init; } = new();
}
