namespace Dmx.Messaging.Commands;

/// <summary>Origine d'une commande (doc 02 §6.1).</summary>
public enum CommandOrigin
{
    /// <summary>Action de l'utilisateur à la souris ou au clavier.</summary>
    User,

    /// <summary>Contrôleur MIDI.</summary>
    Midi,

    /// <summary>Directeur automatique.</summary>
    Director,

    /// <summary>Séquenceur de show.</summary>
    Show,

    /// <summary>Timeline par morceau.</summary>
    Timeline,

    /// <summary>Télécommande (futur).</summary>
    Remote,

    /// <summary>Outil sans interface, tests.</summary>
    Tool,
}
