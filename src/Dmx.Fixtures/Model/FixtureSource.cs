namespace Dmx.Fixtures.Model;

/// <summary>Origine d'une définition.</summary>
public enum FixtureSource
{
    /// <summary>Saisie dans l'éditeur.</summary>
    Manual,

    /// <summary>Import Open Fixture Library.</summary>
    Ofl,

    /// <summary>Import QLC+.</summary>
    QlcPlus,

    /// <summary>Modèle générique livré avec l'application.</summary>
    Generic,
}
