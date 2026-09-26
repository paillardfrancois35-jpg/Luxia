namespace Luxia.Engine.Tests;

/// <summary>
/// Tests qui mesurent du temps réel (gigue de la boucle, budget d'un tick) : jamais exécutés en même temps que les
/// autres tests de l'assemblage, sinon la charge des uns fausse les mesures des autres.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RealTimeTests
{
    public const string Name = "Temps réel";
}
