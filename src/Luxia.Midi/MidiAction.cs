namespace Luxia.Midi;

/// <summary>Action d'un contrôle (doc 18b §3, MIDI-007) ; chacune se traduit en commandes d'origine « MIDI » (MIDI-002).</summary>
public enum MidiAction
{
    /// <summary>Aucune (contrôle désactivé).</summary>
    None,

    /// <summary>Lancer la scène (ou l'arrêter si elle joue).</summary>
    LaunchScene,

    /// <summary>Flash de la scène tant que maintenu.</summary>
    FlashScene,

    /// <summary>Arrêter la couche.</summary>
    StopLayer,

    /// <summary>Master de la couche (fader).</summary>
    LayerMaster,

    /// <summary>Grand Master (fader).</summary>
    GrandMaster,

    /// <summary>
    /// Blackout tant que le contrôle est maintenu (MIDI-011, comme Daslight) : appui = blackout, relâche = blackout annulé,
    /// même s'il avait été activé ailleurs.
    /// </summary>
    Blackout,

    /// <summary>Blackout en bascule (un appui l'active, le suivant l'annule), comme le bouton de l'écran.</summary>
    BlackoutToggle,

    /// <summary>FLASH général (maintien).</summary>
    Flash,

    /// <summary>STROBE général (maintien).</summary>
    Strobe,

    /// <summary>Fumée (maintien).</summary>
    Smoke,

    /// <summary>Rafale de fumée.</summary>
    SmokeBurst,

    /// <summary>Figer (bascule).</summary>
    Freeze,

    /// <summary>Tout arrêter (sauf couches protégées).</summary>
    StopAll,
}
