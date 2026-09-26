namespace Luxia.Hosting;

/// <summary>Version enregistrée d'un projet (GEN-055).</summary>
/// <param name="Name">Nom du dossier (« 20260927-221530 »).</param>
/// <param name="SavedAt">Date de la version.</param>
/// <param name="Reason">Motif (« automatique », « passage en Live », « avant restauration »).</param>
public sealed record ProjectVersion(string Name, DateTime SavedAt, string Reason);
