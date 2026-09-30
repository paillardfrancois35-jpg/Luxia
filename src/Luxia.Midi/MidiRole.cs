namespace Luxia.Midi;

/// <summary>Rôle d'une platine (ERG-038) : les couches et les scènes (par défaut) ou les dimmers de groupe.</summary>
public enum MidiRole
{
    /// <summary>Colonnes du Live : pads = scènes, faders = masters des couches, fader 9 = Grand Master.</summary>
    Layers,

    /// <summary>Contrôles fins : faders 1 à 8 = dimmers de groupe, boutons du bas = remettre le dimmer à 100 %.</summary>
    Dimmers,
}

/// <summary>Attribution des rôles aux platines branchées (ERG-038).</summary>
public static class MidiRoles
{
    /// <summary>
    /// Rôle de chaque platine, dans l'ordre donné. Le réglage <see cref="MidiLayout.DimmerController"/> (morceau du nom du
    /// port ou du modèle : « MK1 ») désigne la platine des dimmers ; à défaut, dès que le projet a des dimmers de groupe et
    /// que deux platines au moins sont branchées, c'est la deuxième dans l'ordre alphabétique des ports (stable d'un
    /// démarrage à l'autre). Sans dimmer de groupe, toutes les platines gardent le rôle des couches.
    /// </summary>
    /// <param name="devices">Platines branchées : profil et port.</param>
    /// <param name="layout">Disposition (dimmers du projet, réglage).</param>
    public static IReadOnlyList<MidiRole> Assign(IReadOnlyList<(ControllerProfile Profile, string Port)> devices, MidiLayout layout)
    {
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(layout);
        var roles = new MidiRole[devices.Count];
        if (layout.Dimmers.Count == 0 && layout.DimmerController is null)
        {
            return roles;
        }

        if (layout.DimmerController is { Length: > 0 } wanted)
        {
            for (var i = 0; i < devices.Count; i++)
            {
                var (profile, port) = devices[i];
                var matches = port.Contains(wanted, StringComparison.OrdinalIgnoreCase)
                    || profile.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase)
                    || profile.ShortName.Contains(wanted, StringComparison.OrdinalIgnoreCase);
                roles[i] = matches ? MidiRole.Dimmers : MidiRole.Layers;
            }

            // Toutes les platines sont désignées : il en faut au moins une pour les couches.
            if (roles.All(r => r == MidiRole.Dimmers))
            {
                Array.Fill(roles, MidiRole.Layers);
            }

            return roles;
        }

        if (devices.Count >= 2)
        {
            var second = Enumerable.Range(0, devices.Count).OrderBy(i => devices[i].Port, StringComparer.OrdinalIgnoreCase).ElementAt(1);
            roles[second] = MidiRole.Dimmers;
        }

        return roles;
    }
}
