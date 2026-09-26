using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;
using Luxia.Patch.Model;

namespace Luxia.Scenes.Compilation;

/// <summary>Appareil patché résolu : modèle, mode, appareil de référence (jumeaux), présence dans le lieu actif.</summary>
/// <remarks>
/// Classe et non <c>record</c> : les canaux sont calculés une fois depuis le mode, une copie « <c>with</c> » les
/// aurait gardés en changeant de mode (piège vécu en écrivant le rapport d'impact d'un changement de mode).
/// </remarks>
public sealed class FixtureInfo
{
    /// <summary>Résout un appareil.</summary>
    /// <param name="fixture">Appareil patché.</param>
    /// <param name="type">Modèle (copie du projet).</param>
    /// <param name="mode">Mode utilisé.</param>
    /// <param name="referenceId">Appareil dont il partage les paramètres (lui-même, sauf jumeau, INST-014).</param>
    /// <param name="absent">Absent du lieu actif (INST-052).</param>
    public FixtureInfo(PatchedFixture fixture, FixtureType type, FixtureMode mode, Guid referenceId, bool absent)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(mode);
        Fixture = fixture;
        Type = type;
        Mode = mode;
        ReferenceId = referenceId;
        Absent = absent;
        Channels = [.. FixtureRules.ChannelsOf(type, mode)];
        Cells = [.. Channels.Select(c => c.Cell).Distinct().Order()];
        NeedsVirtualIntensity = Channels.Any(c => FixtureRules.FollowsIntensity(type, mode, c) && DimmerFor(c) is null);
    }

    /// <summary>Appareil patché.</summary>
    public PatchedFixture Fixture { get; }

    /// <summary>Modèle (copie du projet).</summary>
    public FixtureType Type { get; }

    /// <summary>Mode utilisé.</summary>
    public FixtureMode Mode { get; }

    /// <summary>Appareil dont il partage les paramètres (lui-même, sauf jumeau).</summary>
    public Guid ReferenceId { get; }

    /// <summary>Absent du lieu actif.</summary>
    public bool Absent { get; }

    /// <summary>Définitions de canaux du mode (sans doublon, dans l'ordre du mode).</summary>
    public IReadOnlyList<ChannelDefinition> Channels { get; }

    /// <summary>Cellules présentes dans le mode (0 = appareil entier), dans l'ordre.</summary>
    public IReadOnlyList<int> Cells { get; }

    /// <summary>
    /// L'appareil a besoin d'une intensité virtuelle (BIB-006) : un de ses canaux suit l'intensité sans avoir de
    /// gradateur (maître ou de sa cellule) pour le faire.
    /// </summary>
    public bool NeedsVirtualIntensity { get; }

    /// <summary>Le même appareil dans un autre mode (rapport d'impact d'un changement de mode, SC-03).</summary>
    public FixtureInfo WithMode(FixtureMode mode) =>
        new(Fixture with { ModeName = mode.Name }, Type, mode, ReferenceId, Absent);

    /// <summary>Gradateur qui pilote un canal : celui de sa cellule, sinon le gradateur maître, sinon aucun.</summary>
    public ChannelDefinition? DimmerFor(ChannelDefinition channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        return Channels.FirstOrDefault(c => c.Attribute == AttributeKind.CellIntensity && c.Cell == channel.Cell && c.Cell > 0)
            ?? Channels.FirstOrDefault(c => c.Attribute == AttributeKind.Intensity);
    }
}
