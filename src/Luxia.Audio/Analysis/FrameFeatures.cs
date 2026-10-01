namespace Luxia.Audio.Analysis;

/// <summary>Mesures d'une trame d'analyse (un saut de quelques millisecondes), doc 19 §4 et §5.</summary>
/// <param name="Flux">Flux spectral large bande (attaques), adapté au volume : base du tempo et des temps.</param>
/// <param name="BassFlux">Flux spectral des basses (~40-150 Hz) : kick.</param>
/// <param name="TrebleFlux">Flux spectral des aigus (~2-10 kHz) : caisse claire, charleston.</param>
/// <param name="Level">Niveau efficace du son avant normalisation (0 à 1 environ).</param>
/// <param name="Bass">Énergie normalisée des basses.</param>
/// <param name="Mid">Énergie normalisée des médiums.</param>
/// <param name="Treble">Énergie normalisée des aigus.</param>
/// <param name="Silent">Le niveau est sous le seuil de silence (AUD-005).</param>
/// <param name="Gain">Gain de la normalisation du volume appliqué aux bandes (pour retrouver leur niveau réel : bande ÷ gain).</param>
/// <param name="UpperFlux">Flux spectral au-dessus des basses (médiums et aigus) : le claquement d'une attaque de batterie, absent d'une note de basse.</param>
internal readonly record struct FrameFeatures(double Flux, double BassFlux, double TrebleFlux, double Level, double Bass, double Mid, double Treble, bool Silent, double Gain = 1, double UpperFlux = 0);
