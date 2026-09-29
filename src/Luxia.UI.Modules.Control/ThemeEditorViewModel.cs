using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Fenêtre « Thème de couleurs » (PAL-010, essai P6) : nom, crans de couleur dans l'ordre de l'alternance, couleur de
/// chaque cran au sélecteur ou en saisie libre « #RRGGBB », ajouter / supprimer / déplacer un cran. Sans interface :
/// le panneau Effets l'ouvre et lit le résultat (<see cref="Saved"/>, <see cref="Colors"/>).
/// </summary>
public sealed partial class ThemeEditorViewModel : ViewModelBase
{
    /// <summary>Nombre minimal de crans d'un thème (une alternance d'une seule couleur n'en est pas une).</summary>
    public const int MinSteps = 2;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private ThemeStepViewModel? _selectedStep;

    [ObservableProperty]
    private LightColor _selectedColor = LightColor.White;

    [ObservableProperty]
    private string _hexText = string.Empty;

    [ObservableProperty]
    private string? _message;

    private bool _loading;

    /// <summary>Crée l'éditeur.</summary>
    /// <param name="title">Titre de la fenêtre (« Nouveau thème », « Modifier le thème »).</param>
    /// <param name="name">Nom proposé.</param>
    /// <param name="colors">Couleurs de départ (au moins deux sont ajoutées si besoin).</param>
    public ThemeEditorViewModel(string title, string name, IReadOnlyList<LogicalColor> colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        Title = title;
        _name = name;
        foreach (var color in colors)
        {
            Steps.Add(new ThemeStepViewModel(color));
        }

        while (Steps.Count < MinSteps)
        {
            Steps.Add(new ThemeStepViewModel(Steps.Count == 0 ? LogicalColor.FromHex("#FF0000") : LogicalColor.FromHex("#0000FF")));
        }

        Renumber();
        SelectedStep = Steps[0];
    }

    /// <summary>Levé quand la fenêtre doit se fermer (Enregistrer ou Annuler).</summary>
    public event EventHandler? Closed;

    /// <summary>Titre de la fenêtre.</summary>
    public string Title { get; }

    /// <summary>Crans, dans l'ordre de l'alternance.</summary>
    public ObservableCollection<ThemeStepViewModel> Steps { get; } = [];

    /// <summary>Enregistré (sinon annulé).</summary>
    public bool Saved { get; private set; }

    /// <summary>Couleurs du thème, dans l'ordre.</summary>
    public IReadOnlyList<LogicalColor> Colors => [.. Steps.Select(s => s.Color)];

    /// <summary>Le sélecteur de couleur demande une couleur pour le cran choisi.</summary>
    public void RequestColor(LightColor color)
    {
        if (SelectedStep is not { } step)
        {
            return;
        }

        var (r, g, b) = color.ToRgb();
        step.TrySet(string.Create(CultureInfo.InvariantCulture, $"#{r:X2}{g:X2}{b:X2}"));
        _loading = true;
        SelectedColor = color;
        HexText = step.Hex;
        _loading = false;
    }

    /// <summary>Ajoute un cran après le cran choisi (même couleur, à changer).</summary>
    [RelayCommand]
    private void AddStep()
    {
        var index = SelectedStep is { } step ? Steps.IndexOf(step) + 1 : Steps.Count;
        var added = new ThemeStepViewModel(SelectedStep?.Color ?? LogicalColor.FromHex("#FFFFFF"));
        Steps.Insert(index, added);
        Renumber();
        SelectedStep = added;
    }

    /// <summary>Supprime le cran choisi (il en reste toujours deux).</summary>
    [RelayCommand]
    private void RemoveStep()
    {
        if (SelectedStep is not { } step || Steps.Count <= MinSteps)
        {
            Message = string.Create(CultureInfo.CurrentCulture, $"Un thème garde au moins {MinSteps} couleurs.");
            return;
        }

        var index = Steps.IndexOf(step);
        Steps.Remove(step);
        Renumber();
        SelectedStep = Steps[Math.Min(index, Steps.Count - 1)];
    }

    /// <summary>Monte le cran choisi d'un rang.</summary>
    [RelayCommand]
    private void MoveUp() => Move(-1);

    /// <summary>Descend le cran choisi d'un rang.</summary>
    [RelayCommand]
    private void MoveDown() => Move(+1);

    /// <summary>Enregistre (nom non vide, au moins deux crans).</summary>
    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Message = "Donnez un nom au thème.";
            return;
        }

        Saved = true;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Ferme sans rien changer.</summary>
    [RelayCommand]
    private void Cancel() => Closed?.Invoke(this, EventArgs.Empty);

    partial void OnSelectedStepChanged(ThemeStepViewModel? value)
    {
        if (value is null)
        {
            return;
        }

        _loading = true;
        SelectedColor = value.Light;
        HexText = value.Hex;
        _loading = false;
    }

    // Saisie libre « #RRGGBB » (ou « RRGGBB ») : appliquée dès qu'elle est complète.
    partial void OnHexTextChanged(string value)
    {
        if (_loading || SelectedStep is not { } step)
        {
            return;
        }

        if (step.TrySet(value))
        {
            _loading = true;
            SelectedColor = step.Light;
            _loading = false;
            Message = null;
        }
    }

    private void Move(int delta)
    {
        if (SelectedStep is not { } step)
        {
            return;
        }

        var index = Steps.IndexOf(step);
        var target = index + delta;
        if (target < 0 || target >= Steps.Count)
        {
            return;
        }

        Steps.Move(index, target);
        Renumber();
        SelectedStep = step;
    }

    private void Renumber()
    {
        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].Label = string.Create(CultureInfo.CurrentCulture, $"{i + 1}");
        }
    }
}
