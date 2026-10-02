using Luxia.Engine.Model;
using Luxia.Engine.Timing;
using Luxia.Messaging.Commands;

namespace Luxia.Engine.Sequencing;

/// <summary>
/// Séquenceur de shows et de séquences (doc 20, D37), appelé par le moteur sur son fil, à chaque tick, après les commandes reçues
/// et avant l'avancement des scènes : ses lancements tombent au temps près sur l'horloge. Implémenté hors du moteur
/// (<c>Luxia.Show</c>) ; le moteur ne connaît que cette interface.
/// </summary>
public interface ISequencer
{
    /// <summary>Applique une commande qui lui est destinée (CMD-050 à 052) ; renvoie le motif d'un refus, ou <c>null</c>.</summary>
    string? Apply(SequencerCommand command, ISequencerHost host);

    /// <summary>Fait évoluer shows et séquences d'un tick. Ne doit jamais bloquer ni lever d'exception.</summary>
    void Tick(ISequencerHost host);
}

/// <summary>Ce que le moteur montre et permet au séquenceur, le temps d'un tick (sur le fil du moteur).</summary>
public interface ISequencerHost
{
    /// <summary>Instant du tick (horloge du moteur).</summary>
    TimeSpan Now { get; }

    /// <summary>Secondes écoulées depuis le tick précédent.</summary>
    double Elapsed { get; }

    /// <summary>Tempo de l'horloge musicale.</summary>
    double Bpm { get; }

    /// <summary>Position de l'horloge en temps (latence comprise), depuis son origine.</summary>
    double BeatPosition { get; }

    /// <summary>Signaux musicaux du tick (écoute ou simulation).</summary>
    MusicSignals Music { get; }

    /// <summary>Modèle compilé du projet (scènes, couches).</summary>
    ShowModel Show { get; }

    /// <summary>La scène joue (ni en fondu de sortie, ni terminée).</summary>
    bool IsPlaying(Guid sceneId);

    /// <summary>Niveau courant d'une couche (0 à 1), 1 si elle est inconnue.</summary>
    double LayerLevel(Guid layerId);

    /// <summary>Grand Master courant (0 à 1).</summary>
    double GrandMaster { get; }

    /// <summary>
    /// Applique une commande du catalogue tout de suite, dans ce tick (origine <see cref="CommandOrigin.Show"/> attendue) ;
    /// renvoie le motif d'un refus. <paramref name="log"/> faux : pas d'entrée au journal des commandes (pas d'une rampe).
    /// </summary>
    string? Execute(Command command, bool log = true);

    /// <summary>Publie un événement sur le bus (ne bloque pas).</summary>
    void Publish<TEvent>(TEvent evt)
        where TEvent : class;
}

/// <summary>Signaux musicaux vus par le séquenceur (D38).</summary>
/// <param name="AudioLive">Un son exploitable est entendu (ou simulé).</param>
/// <param name="Energy">Énergie (0 à 1).</param>
/// <param name="EnergyLevel">Niveau d'énergie : 0 Calme à 3 Explosif.</param>
/// <param name="Cues">Événements du tick (drop, break, montée, silence, reprise, morceau changé).</param>
/// <param name="Style">Style courant (simulé avant P9) ; <c>null</c> = inconnu.</param>
public readonly record struct MusicSignals(bool AudioLive, double Energy, int EnergyLevel, MusicCues Cues, string? Style);
