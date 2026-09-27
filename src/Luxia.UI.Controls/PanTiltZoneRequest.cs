namespace Luxia.UI.Controls;

/// <summary>Demande de création (<see cref="Id"/> nul) ou de modification d'une zone.</summary>
/// <param name="Id">Zone modifiée, ou nul pour une nouvelle zone dessinée.</param>
/// <param name="Area">Rectangle demandé.</param>
public sealed record PanTiltZoneRequest(string? Id, PanTiltRect Area);
