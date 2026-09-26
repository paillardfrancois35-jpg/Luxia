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

    /// <summary>Blackout (bascule).</summary>
    Blackout,

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
