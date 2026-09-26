using Dmx.Core.Dmx;

namespace Dmx.Messaging.Commands;

/// <summary>
/// CMD-024 <c>TesterSortie</c> : chenillard de test canal par canal (SORT-007, D19).
/// Chaque canal de la plage, hors canaux exclus, reçoit tour à tour la valeur de test pendant <see cref="StepDuration"/>.
/// Sera soumis aux limiteurs de sûreté dès leur existence (P5).
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="Active">Démarre (<c>true</c>) ou arrête (<c>false</c>) le test.</param>
/// <param name="Universe">Univers testé.</param>
/// <param name="Range">Plage de canaux parcourue.</param>
/// <param name="ExcludedChannels">Canaux jamais allumés (ex. 180 = fumée).</param>
/// <param name="Value">Valeur de test émise sur le canal courant.</param>
/// <param name="StepDuration">Durée d'allumage de chaque canal.</param>
/// <param name="Loop">Recommence au début après le dernier canal.</param>
/// <param name="Mode">Chenillard canal par canal, ou rampe de tous les canaux (endurance, T-SORT-07).</param>
/// <param name="HeldChannels">
/// SORT-008 : canaux maintenus à <paramref name="Value"/> pendant tout le chenillard (ex. maîtres des PAR à gradateur),
/// pour qu'un canal dépendant d'un autre (couleur qui a besoin de son maître) réagisse visiblement au passage du chenillard.
/// </param>
public sealed record TestOutputCommand(
    CommandOrigin Origin,
    bool Active,
    int Universe,
    ChannelRange Range,
    IReadOnlyList<int> ExcludedChannels,
    byte Value,
    TimeSpan StepDuration,
    bool Loop = true,
    TestPatternMode Mode = TestPatternMode.Chase,
    IReadOnlyList<int>? HeldChannels = null) : Command(Origin)
{
    /// <summary>Plage par défaut à l'écran Sorties (Q16).</summary>
    public static readonly ChannelRange DefaultRange = new(1, 16);

    /// <summary>Canaux exclus par défaut : 180 = machine à fumée du show de référence (Q16).</summary>
    public static readonly IReadOnlyList<int> DefaultExcludedChannels = [180];

    /// <summary>Valeur de test par défaut : 50 % (Q16).</summary>
    public const byte DefaultValue = 128;

    /// <summary>Canaux réellement maintenus (jamais vide, jamais <c>null</c>).</summary>
    public IReadOnlyList<int> HeldChannelsOrEmpty => HeldChannels ?? [];

    /// <summary>Arrête le test en cours.</summary>
    public static TestOutputCommand Stop(CommandOrigin origin, int universe = 1) =>
        new(origin, false, universe, DefaultRange, [], 0, TimeSpan.Zero);
}

