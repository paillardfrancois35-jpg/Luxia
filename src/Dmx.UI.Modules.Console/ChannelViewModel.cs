using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Console;

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
    private string _percentText = "0 %";

    /// <summary>Crée la tranche.</summary>
    public ChannelViewModel(int channel) => Channel = channel;

    /// <summary>Numéro du canal (1 à 512).</summary>
    public int Channel { get; }

    /// <summary>Numéro affiché.</summary>
    public string ChannelText => Channel.ToString(CultureInfo.CurrentCulture);

    /// <summary>Appareil et attribut, dès que le patch existera (CONS-007, P3).</summary>
    public string Caption { get; set; } = string.Empty;

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
