using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmx.Fixtures;
using Dmx.Fixtures.Model;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Library;

/// <summary>Roue de couleur ou de gobos dans l'éditeur (BIB-025).</summary>
public sealed partial class WheelViewModel : ViewModelBase
{
    private readonly FixtureEditorViewModel _editor;
    private bool _loading;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private Choice<WheelKind> _kind = Choices.WheelKinds[0];

    /// <summary>Crée la roue.</summary>
    public WheelViewModel(FixtureEditorViewModel editor, int index)
    {
        _editor = editor;
        Index = index;
    }

    /// <summary>Rang de la roue.</summary>
    public int Index { get; }

    /// <summary>Choix du type.</summary>
    public IReadOnlyList<Choice<WheelKind>> Kinds => Choices.WheelKinds;

    /// <summary>Emplacements : nom et couleurs (#RRGGBB séparés par des espaces).</summary>
    public ObservableCollection<WheelSlotRowViewModel> Slots { get; } = [];

    /// <summary>Recharge depuis le modèle.</summary>
    public void Load(Wheel wheel)
    {
        ArgumentNullException.ThrowIfNull(wheel);
        _loading = true;
        Name = wheel.Name;
        Kind = Choices.WheelKinds.First(k => k.Value == wheel.Kind);
        if (Slots.Count == wheel.Slots.Count)
        {
            for (var i = 0; i < wheel.Slots.Count; i++)
            {
                Slots[i].Load(wheel.Slots[i]);
            }
        }
        else
        {
            Slots.Clear();
            for (var i = 0; i < wheel.Slots.Count; i++)
            {
                Slots.Add(new WheelSlotRowViewModel(this, i, wheel.Slots[i]));
            }
        }

        _loading = false;
    }

    /// <summary>Modifie un emplacement.</summary>
    public void ReplaceSlot(int index, WheelSlot slot) =>
        Edit("Emplacement de roue", w => w with { Slots = [.. w.Slots.Select((s, i) => i == index ? slot : s)] });

    /// <summary>Supprime un emplacement.</summary>
    public void RemoveSlot(int index) =>
        Edit("Supprimer un emplacement", w => w with { Slots = [.. w.Slots.Where((_, i) => i != index)] });

    partial void OnNameChanged(string value) => Edit("Nom de roue", w => w with { Name = value.Trim() });

    partial void OnKindChanged(Choice<WheelKind> value) => Edit("Type de roue", w => w with { Kind = value.Value });

    [RelayCommand]
    private void AddSlot() => Edit("Ajouter un emplacement", w => w with { Slots = [.. w.Slots, new WheelSlot($"Emplacement {w.Slots.Count + 1}", [])] });

    [RelayCommand]
    private void Remove() => _editor.Apply("Supprimer une roue", f => FixtureEdits.RemoveWheel(f, Index));

    private void Edit(string description, Func<Wheel, Wheel> change)
    {
        if (!_loading)
        {
            _editor.Apply(description, f => FixtureEdits.UpdateWheel(f, Index, change));
        }
    }
}
