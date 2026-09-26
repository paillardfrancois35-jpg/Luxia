namespace Luxia.Fixtures.Model;

/// <summary>Paramètre d'une plage progressive (ex. strobe de 1 Hz à 20 Hz).</summary>
/// <param name="Nature">Nature : « vitesse », « fréquence », « angle », « pourcentage »…</param>
/// <param name="Start">Valeur à la borne basse.</param>
/// <param name="End">Valeur à la borne haute.</param>
/// <param name="Unit">Unité (« Hz », « ° », « % »), facultative.</param>
public sealed record ProgressiveParameter(string Nature, double Start, double End, string? Unit = null);
