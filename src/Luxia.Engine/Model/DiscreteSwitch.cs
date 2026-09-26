namespace Luxia.Engine.Model;

/// <summary>Moment de bascule d'un attribut discret pendant un fondu (MOT-012).</summary>
public enum DiscreteSwitch
{
    /// <summary>Au début du fondu (défaut).</summary>
    Start,

    /// <summary>Au milieu du fondu.</summary>
    Middle,

    /// <summary>À la fin du fondu.</summary>
    End,
}
