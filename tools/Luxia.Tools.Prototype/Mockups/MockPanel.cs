using Dock.Model.Mvvm.Controls;

namespace Luxia.Tools.Prototype.Mockups;

/// <summary>Panneau de maquette : un <see cref="Tool"/> qui connaît la maquette à montrer (jamais enregistré).</summary>
internal sealed class MockPanel : Tool
{
    public required MockScenario Scenario { get; init; }
}
