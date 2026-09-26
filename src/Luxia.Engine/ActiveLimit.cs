using Luxia.Messaging.Events;

namespace Luxia.Engine;

/// <summary>Limite de sûreté en train d'agir (GEN-086, LIVE-008).</summary>
/// <param name="Kind">Limite.</param>
/// <param name="FixtureId">Appareil.</param>
/// <param name="Label">Appareil ou canal, lisible.</param>
/// <param name="Detail">Ce que fait le limiteur.</param>
public readonly record struct ActiveLimit(SafetyLimitKind Kind, Guid FixtureId, string Label, string Detail);
