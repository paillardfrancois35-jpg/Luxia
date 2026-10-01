namespace Luxia.Messaging.Commands;

/// <summary>Réglage ponctuel du tempo (CMD-042).</summary>
public enum TempoAdjustment
{
    /// <summary>Tempo × 2 (corrige une erreur d'octave, AUD-023).</summary>
    TimesTwo,

    /// <summary>Tempo ÷ 2.</summary>
    DivideByTwo,

    /// <summary>Ajoute la valeur donnée (en BPM, négative pour retirer).</summary>
    AddBpm,

    /// <summary>« Le 1 est maintenant » : le temps en cours devient le premier de la mesure (AUD-024).</summary>
    ResyncBar,
}
