namespace Luxia.Midi;

/// <summary>
/// Affectation modifiée d'un contrôle (MIDI-007), écrite dans <c>midi.json</c> : elle remplace l'affectation par défaut
/// de ce contrôle, pour tous les contrôleurs ou pour un modèle (MIDI-005 : deux contrôleurs, deux affectations).
/// </summary>
public sealed record MidiBinding
{
    /// <summary>Modèle visé (morceau du nom : « MK1 », « mk2 ») ; absent = tous.</summary>
    public string? Model { get; init; }

    /// <summary>Contrôle : « pad 3 2 », « bas 1 », « droite 4 », « fader 9 ».</summary>
    public required string Control { get; init; }

    /// <summary>Action.</summary>
    public MidiAction Action { get; init; }

    /// <summary>Scène (actions de scène).</summary>
    public Guid? SceneId { get; init; }

    /// <summary>Couche (arrêt, master).</summary>
    public Guid? LayerId { get; init; }

    /// <summary>L'affectation concerne-t-elle ce modèle ?</summary>
    public bool AppliesTo(ControllerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return Model is null
            || profile.Name.Contains(Model, StringComparison.OrdinalIgnoreCase)
            || profile.ShortName.Contains(Model, StringComparison.OrdinalIgnoreCase);
    }
}
