using Luxia.Fixtures.Model;

namespace Luxia.Patch.Rules;

/// <summary>
/// Rapport d'impact d'un changement de définition (INST-016 : changer le mode d'un appareil patché ;
/// GEN-053 : mettre à jour la copie du projet depuis la bibliothèque partagée). Il ne porte que sur les canaux :
/// les scènes touchées (doc 13 §3, INST-016) ne sont pas listées ici (<c>Luxia.Patch</c> ne connaît pas les scènes).
/// </summary>
/// <param name="LostChannels">Noms des canaux (attributs) présents avant et absents après.</param>
/// <param name="GainedChannels">Noms des canaux (attributs) absents avant et présents après.</param>
/// <param name="ModeRemoved">Le mode d'origine n'existe plus dans la nouvelle définition.</param>
public sealed record FixtureUpdateImpact(IReadOnlyList<string> LostChannels, IReadOnlyList<string> GainedChannels, bool ModeRemoved)
{
    /// <summary>Aucun impact : les canaux occupés par le mode sont inchangés.</summary>
    public bool IsEmpty => !ModeRemoved && LostChannels.Count == 0 && GainedChannels.Count == 0;

    /// <summary>Phrase résumant l'impact (« Strobe, Programme perdus » — doc 13 §3, INST-016).</summary>
    public string Summary()
    {
        if (ModeRemoved)
        {
            return "Le mode n'existe plus dans la nouvelle définition : l'appareil doit être repatché.";
        }

        if (IsEmpty)
        {
            return "Aucun impact.";
        }

        var parts = new List<string>();
        if (LostChannels.Count > 0)
        {
            parts.Add($"{string.Join(", ", LostChannels)} perdus");
        }

        if (GainedChannels.Count > 0)
        {
            parts.Add($"{string.Join(", ", GainedChannels)} gagnés");
        }

        return string.Join(" ; ", parts);
    }

    /// <summary>Compare deux modes d'un même modèle (INST-016) par les canaux qu'ils occupent.</summary>
    public static FixtureUpdateImpact ForModeChange(FixtureType fixture, string oldModeName, string newModeName)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var oldMode = fixture.Modes.FirstOrDefault(m => m.Name == oldModeName);
        var newMode = fixture.Modes.FirstOrDefault(m => m.Name == newModeName);
        return Compare(fixture, oldMode, fixture, newMode);
    }

    /// <summary>
    /// Compare le mode d'un appareil patché entre l'ancienne et la nouvelle définition du modèle (GEN-053 :
    /// mise à jour depuis la bibliothèque partagée). <paramref name="modeName"/> est cherché par nom dans les deux.
    /// </summary>
    public static FixtureUpdateImpact ForLibraryUpdate(FixtureType oldFixture, FixtureType newFixture, string modeName)
    {
        ArgumentNullException.ThrowIfNull(oldFixture);
        ArgumentNullException.ThrowIfNull(newFixture);
        var oldMode = oldFixture.Modes.FirstOrDefault(m => m.Name == modeName);
        var newMode = newFixture.Modes.FirstOrDefault(m => m.Name == modeName);
        return Compare(oldFixture, oldMode, newFixture, newMode);
    }

    private static FixtureUpdateImpact Compare(FixtureType oldFixture, FixtureMode? oldMode, FixtureType newFixture, FixtureMode? newMode)
    {
        if (oldMode is null || newMode is null)
        {
            var remaining = oldMode?.Channels.Select(c => oldFixture.Channel(c.Channel)?.Name ?? c.Channel) ?? [];
            return new FixtureUpdateImpact([.. remaining], [], ModeRemoved: true);
        }

        var oldNames = new HashSet<string>(oldMode.Channels.Select(c => oldFixture.Channel(c.Channel)?.Name ?? c.Channel));
        var newNames = new HashSet<string>(newMode.Channels.Select(c => newFixture.Channel(c.Channel)?.Name ?? c.Channel));
        var lost = oldNames.Except(newNames).Order().ToList();
        var gained = newNames.Except(oldNames).Order().ToList();
        return new FixtureUpdateImpact(lost, gained, ModeRemoved: false);
    }
}
