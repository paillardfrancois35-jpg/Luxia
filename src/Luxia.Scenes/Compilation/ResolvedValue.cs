namespace Luxia.Scenes.Compilation;

/// <summary>Valeur de scène résolue pour un paramètre du moteur.</summary>
/// <param name="FixtureId">Appareil de référence (le premier d'un groupe de jumeaux).</param>
/// <param name="ChannelKey">Clé du canal (ou <see cref="Engine.Model.RigParameter.VirtualIntensityKey"/>).</param>
/// <param name="Level">Valeur normalisée 0-1.</param>
/// <param name="MemberIndex">Rang du membre dans la cible (0 = premier), pour le « fan » temporel (SCN-010).</param>
/// <param name="MemberCount">Nombre de membres de la cible.</param>
public readonly record struct ResolvedValue(Guid FixtureId, string ChannelKey, double Level, int MemberIndex, int MemberCount);
