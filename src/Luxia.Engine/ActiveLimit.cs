using Luxia.Messaging.Events;

namespace Luxia.Engine;

/// <summary>Limite de sûreté en train d'agir (GEN-086, LIVE-008).</summary>
/// <param name="Kind">Limite.</param>
/// <param name="FixtureId">Appareil.</param>
/// <param name="Label">Appareil ou canal, lisible.</param>
/// <param name="Detail">Ce que fait le limiteur.</param>
/// <param name="RemainingSeconds">Temps avant la levée de la limite (pause du strobe, repos de la fumée), si elle en a un.</param>
public readonly record struct ActiveLimit(SafetyLimitKind Kind, Guid FixtureId, string Label, string Detail, double? RemainingSeconds = null);
