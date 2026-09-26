using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Engine;
using Luxia.Fixtures.Model;
using Luxia.Hosting;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>
/// Palettes (doc 17 §2) : grilles par type (PAL-007), application à la sélection du programmeur (SCN-008),
/// création depuis le programmeur (PAL-001), mise à jour (PAL-005), suppression avec rapport et figeage (PAL-006),
/// palettes automatiques de la sélection (PAL-003).
/// </summary>
public sealed partial class PalettesViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;
    private readonly ProgrammerViewModel _programmer;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _hasAutomatic;

    /// <summary>
    /// Mode « mise à jour » armé : le prochain clic sur une palette la met à jour avec le programmeur (PAL-005) au lieu de
    /// l'appliquer. Ajouté après l'essai P4 : le seul clic droit n'était pas trouvé, les réglages partaient dans les étapes.
    /// </summary>
    [ObservableProperty]
    private bool _updateArmed;

    /// <summary>Crée le panneau.</summary>
    public PalettesViewModel(LuxiaRuntime runtime, IDialogService dialogs, ProgrammerViewModel programmer)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(programmer);
        _runtime = runtime;
        _dialogs = dialogs;
        _programmer = programmer;
        _programmer.Changed += (_, _) => RefreshAutoPalettes();
    }

    /// <summary>Palettes couleur.</summary>
    public ObservableCollection<PaletteButtonViewModel> Colors { get; } = [];

    /// <summary>Palettes d'intensité.</summary>
    public ObservableCollection<PaletteButtonViewModel> Intensities { get; } = [];

    /// <summary>Palettes de position.</summary>
    public ObservableCollection<PaletteButtonViewModel> Positions { get; } = [];

    /// <summary>Palettes de faisceau.</summary>
    public ObservableCollection<PaletteButtonViewModel> Beams { get; } = [];

    /// <summary>Palettes automatiques des appareils sélectionnés.</summary>
    public ObservableCollection<AutoPaletteButtonViewModel> Automatic { get; } = [];

    /// <summary>Relit les palettes du projet.</summary>
    public void Reload()
    {
        Colors.Clear();
        Intensities.Clear();
        Positions.Clear();
        Beams.Clear();
        foreach (var palette in _runtime.Project.Palettes.Palettes)
        {
            var button = new PaletteButtonViewModel(this, palette);
            var grid = palette.Kind switch
            {
                PaletteKind.Intensity => Intensities,
                PaletteKind.Position => Positions,
                PaletteKind.Beam => Beams,
                _ => Colors,
            };
            grid.Add(button);
        }

        RefreshAutoPalettes();
    }

    /// <summary>Applique une palette à la sélection du programmeur, ou la met à jour si le mode « mise à jour » est armé.</summary>
    internal void Apply(PaletteButtonViewModel button)
    {
        if (UpdateArmed)
        {
            UpdateArmed = false;
            _ = UpdateFromProgrammerAsync(button);
            return;
        }

        if (_programmer.SelectedFixtures.Count == 0)
        {
            Message = "Sélectionnez d'abord des appareils dans le programmeur.";
            return;
        }

        Message = null;
        _programmer.ApplyPalette(button.Palette);
    }

    /// <summary>« Enregistrer comme palette » (PAL-001) : couleur, intensité, position ou faisceau de la sélection.</summary>
    [RelayCommand]
    private async Task SaveFromProgrammerAsync(string? kind)
    {
        if (!Enum.TryParse<PaletteKind>(kind, out var paletteKind))
        {
            return;
        }

        if (_programmer.SelectedFixtures.Count == 0)
        {
            Message = "Sélectionnez les appareils dont on enregistre les réglages.";
            return;
        }

        var name = await _dialogs.AskTextAsync("Nouvelle palette", "Nom de la palette :").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var palette = Capture(new Palette { Name = name.Trim(), Kind = paletteKind });
        if (palette is null)
        {
            Message = "Rien à enregistrer : la sélection n'a pas cet attribut.";
            return;
        }

        Save([.. _runtime.Project.Palettes.Palettes, palette]);
        Message = $"Palette « {palette.Name} » créée.";
    }

    /// <summary>Met une palette à jour depuis le programmeur (PAL-005) : toutes les scènes qui la référencent suivent.</summary>
    internal async Task UpdateFromProgrammerAsync(PaletteButtonViewModel button)
    {
        if (_programmer.SelectedFixtures.Count == 0)
        {
            Message = "Sélectionnez les appareils dont on reprend les réglages.";
            return;
        }

        var updated = Capture(button.Palette with { Values = button.Palette.Kind is PaletteKind.Color or PaletteKind.Intensity ? button.Palette.Values : [] });
        if (updated is null)
        {
            Message = "Rien à reprendre : la sélection n'a pas cet attribut.";
            return;
        }

        var ok = await _dialogs.ConfirmAsync("Mettre à jour la palette", $"Remplacer « {button.Palette.Name} » par les réglages actuels ? Les scènes qui l'utilisent suivront.").ConfigureAwait(true);
        if (ok)
        {
            Replace(updated);
            Message = $"Palette « {updated.Name} » mise à jour.";
        }
    }

    /// <summary>Renomme une palette (les scènes la référencent par identifiant, GEN-052 : rien ne casse).</summary>
    internal async Task RenameAsync(PaletteButtonViewModel button)
    {
        var name = await _dialogs.AskTextAsync("Renommer la palette", "Nouveau nom :", button.Palette.Name).ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(name))
        {
            Replace(button.Palette with { Name = name.Trim() });
        }
    }

    /// <summary>Change la couleur d'affichage du bouton (GEN-106).</summary>
    internal async Task RecolorAsync(PaletteButtonViewModel button)
    {
        var color = await _dialogs.AskTextAsync("Couleur du bouton", "Couleur #RRGGBB (vide = couleur de la palette) :", button.Palette.Color).ConfigureAwait(true);
        if (color is null)
        {
            return;
        }

        if (color.Length > 0 && !IsHexColor(color.Trim()))
        {
            Message = $"Couleur invalide : « {color} » (attendu #RRGGBB).";
            return;
        }

        Replace(button.Palette with { Color = color.Length == 0 ? null : color.Trim().ToUpperInvariant() });
    }

    /// <summary>Déplace une palette dans sa grille (PAL-007).</summary>
    internal void Move(PaletteButtonViewModel button, int delta)
    {
        var list = _runtime.Project.Palettes.Palettes.ToList();
        var sameKind = list.Where(p => p.Kind == button.Palette.Kind).ToList();
        var position = sameKind.FindIndex(p => p.Id == button.Palette.Id);
        var target = position + delta;
        if (position < 0 || target < 0 || target >= sameKind.Count)
        {
            return;
        }

        var a = list.FindIndex(p => p.Id == sameKind[position].Id);
        var b = list.FindIndex(p => p.Id == sameKind[target].Id);
        (list[a], list[b]) = (list[b], list[a]);
        Save(list);
    }

    /// <summary>Supprime une palette : rapport des utilisations et proposition de figer les valeurs (PAL-006).</summary>
    internal async Task DeleteAsync(PaletteButtonViewModel button)
    {
        var scenes = _runtime.Project.Scenes;
        var usages = SceneUsage.PaletteUsages(scenes, button.Palette.Id);
        if (usages.Count == 0)
        {
            if (await _dialogs.ConfirmAsync("Supprimer la palette", $"Supprimer « {button.Palette.Name} » ? Aucune scène ne l'utilise.").ConfigureAwait(true))
            {
                Save([.. _runtime.Project.Palettes.Palettes.Where(p => p.Id != button.Palette.Id)]);
            }

            return;
        }

        var text = string.Create(
            CultureInfo.CurrentCulture,
            $"« {button.Palette.Name} » est utilisée par :{Environment.NewLine}{string.Join(Environment.NewLine, usages)}{Environment.NewLine}{Environment.NewLine}Figer ses valeurs actuelles dans ces scènes, puis la supprimer ?");
        if (!await _dialogs.ConfirmAsync("Supprimer une palette utilisée", text).ConfigureAwait(true))
        {
            return;
        }

        var frozen = SceneUsage.FreezePalette(scenes, button.Palette, _runtime.Show.Patch);
        _runtime.Project.SaveScenes(frozen);
        Save([.. _runtime.Project.Palettes.Palettes.Where(p => p.Id != button.Palette.Id)]);
        Message = $"Palette « {button.Palette.Name} » supprimée, valeurs figées dans {usages.Count} étape(s).";
    }

    partial void OnUpdateArmedChanged(bool value) =>
        Message = value ? "Cliquez la palette à mettre à jour avec les réglages actuels du programmeur (appareils sélectionnés)." : null;

    private void RefreshAutoPalettes()
    {
        Automatic.Clear();
        foreach (var palette in AutoPalettes.For(_programmer.SelectedFixtures))
        {
            Automatic.Add(new AutoPaletteButtonViewModel(_programmer, palette));
        }

        HasAutomatic = Automatic.Count > 0;
    }

    /// <summary>Remplit une palette avec les réglages actuels de la sélection (programmeur, sinon sortie).</summary>
    private Palette? Capture(Palette palette)
    {
        var snapshot = _programmer.TargetEngine.Snapshot;
        var selected = _programmer.SelectedFixtures;
        switch (palette.Kind)
        {
            case PaletteKind.Color:
                var first = selected.FirstOrDefault(f => ValueResolver.Keys(f, 0, AttributeKind.Red).Any());
                var programmed = _programmer.Values.LastOrDefault(v => v.Color is not null)?.Color;
                if (programmed is null && first is null)
                {
                    return null;
                }

                return palette with
                {
                    Light = programmed ?? new LogicalColor
                    {
                        R = Level(snapshot, first!, AttributeKind.Red),
                        G = Level(snapshot, first!, AttributeKind.Green),
                        B = Level(snapshot, first!, AttributeKind.Blue),
                    },
                };

            case PaletteKind.Intensity:
                var dimmed = selected.FirstOrDefault(f => ValueResolver.Keys(f, 0, AttributeKind.Intensity).Any());
                return dimmed is null ? null : palette with { Level = Level(snapshot, dimmed, AttributeKind.Intensity) };

            case PaletteKind.Position:
                var positions = selected
                    .SelectMany(f => new[] { AttributeKind.Pan, AttributeKind.Tilt }
                        .Where(a => ValueResolver.Keys(f, 0, a).Any())
                        .Select(a => new PaletteValue { FixtureId = f.Fixture.Id, Attribute = a, Level = Level(snapshot, f, a) }))
                    .ToList();
                return positions.Count == 0 ? null : palette with { Values = positions };

            case PaletteKind.Beam:
                var beams = selected
                    .DistinctBy(f => f.Type.Id)
                    .SelectMany(f => f.Channels
                        .Where(c => AttributeCatalog.Get(c.Attribute).Family == AttributeFamily.Beam && c.Attribute != AttributeKind.Shutter)
                        .Select(c => new PaletteValue { FixtureTypeId = f.Type.Id, Channel = c.Key, Level = LevelOf(snapshot, f.ReferenceId, c.Key) }))
                    .ToList();
                return beams.Count == 0 ? null : palette with { Values = beams };

            default:
                return null;
        }
    }

    private void Replace(Palette palette) =>
        Save([.. _runtime.Project.Palettes.Palettes.Select(p => p.Id == palette.Id ? palette : p)]);

    private void Save(IReadOnlyList<Palette> palettes)
    {
        _runtime.Project.SavePalettes(new PaletteSet { Palettes = palettes });
        Reload();
    }

    private static double Level(EngineSnapshot snapshot, FixtureInfo fixture, AttributeKind attribute) =>
        ValueResolver.Keys(fixture, 0, attribute).FirstOrDefault() is { } key ? LevelOf(snapshot, fixture.ReferenceId, key) : 0;

    private static double LevelOf(EngineSnapshot snapshot, Guid fixtureId, string key)
    {
        var index = snapshot.Show.IndexOf(fixtureId, key);
        return index >= 0 && index < snapshot.Values.Length ? snapshot.Values[index] : 0;
    }

    private static bool IsHexColor(string text) =>
        text.Length == 7 && text[0] == '#' && int.TryParse(text.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
}
