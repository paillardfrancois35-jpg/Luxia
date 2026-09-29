using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Console;

/// <summary>Un canal affiché dans la page courante de la console (tranche de fader).</summary>
public sealed partial class ChannelViewModel : ViewModelBase
{
    /// <summary>Après une action locale, on garde la valeur demandée le temps que le moteur l'applique (≈ 1 tick).</summary>
    private static readonly TimeSpan PendingDuration = TimeSpan.FromMilliseconds(150);

    private long _pendingSince;

    [ObservableProperty]
    private int _value;

    [ObservableProperty]
    private bool _isOverridden;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _valueText = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Detail))]
    private string _percentText = "0 %";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Detail))]
    private string _caption = string.Empty;

    /// <summary>Crée la tranche.</summary>
    public ChannelViewModel(int channel) => Channel = channel;

    /// <summary>Numéro du canal (1 à 512).</summary>
    public int Channel { get; }

    /// <summary>
    /// Texte complet de la tranche (infobulle et ligne d'information de la Console) : la tranche, étroite, coupe le nom
    /// de l'appareil et celui de la plage (essai P6).
    /// </summary>
    public string Detail => string.Create(
        CultureInfo.CurrentCulture,
        $"Canal {Channel}{(Caption.Length > 0 ? " · " + Caption : " · non patché")} · {Value} = {PercentText}");

    /// <summary>Numéro affiché.</summary>
    public string ChannelText => Channel.ToString(CultureInfo.CurrentCulture);

    /// <summary>Valeur saisie localement (affichée tout de suite).</summary>
    public void SetLocal(int value)
    {
        _pendingSince = Stopwatch.GetTimestamp();
        Apply(value);
    }

    /// <summary>Mise à jour depuis l'état du moteur (CONS-005).</summary>
    public void Update(byte emitted, bool overridden)
    {
        IsOverridden = overridden;
        if (_pendingSince != 0 && Stopwatch.GetElapsedTime(_pendingSince) < PendingDuration)
        {
            return;
        }

        _pendingSince = 0;
        Apply(emitted);
    }

    partial void OnValueChanged(int value)
    {
        OnPropertyChanged(nameof(Detail));
        ValueText = value.ToString(CultureInfo.CurrentCulture);
        PercentText = string.Create(CultureInfo.CurrentCulture, $"{Math.Round(value * 100 / 255.0)} %");
    }

    private void Apply(int value)
    {
        if (Value != value)
        {
            Value = value;
        }
    }
}
