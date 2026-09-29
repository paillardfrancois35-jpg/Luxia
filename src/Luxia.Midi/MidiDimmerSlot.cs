namespace Luxia.Midi;

/// <summary>Un dimmer de groupe offert au contrôleur : son groupe et son nom (ERG-037, ERG-038).</summary>
/// <param name="GroupId">Groupe.</param>
/// <param name="Name">Nom du groupe.</param>
public sealed record MidiDimmerSlot(Guid GroupId, string Name);
