namespace Dmx.Patch.Model;

/// <summary>Univers de l'installation (INST-001) : numéroté, nommable.</summary>
public sealed record PatchUniverse
{
    /// <summary>Numéro (1 = premier).</summary>
    public required int Number { get; init; }

    /// <summary>Nom affiché (facultatif).</summary>
    public string? Name { get; init; }
}
