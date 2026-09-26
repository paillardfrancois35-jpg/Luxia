using Avalonia.Input;

namespace Luxia.UI.Modules.Live;

/// <summary>Traduction des touches Avalonia en raccourcis du Live.</summary>
public static class LiveKeys
{
    /// <summary>Raccourci d'une touche (sans modificateur), ou <c>null</c>. B (blackout) reste traité par la fenêtre.</summary>
    public static LiveKey? From(Key key) => key switch
    {
        Key.F => LiveKey.Flash,
        Key.S => LiveKey.Strobe,
        Key.Z => LiveKey.Smoke,
        Key.G => LiveKey.Freeze,
        Key.Escape => LiveKey.Release,
        Key.Left => LiveKey.PreviousLayer,
        Key.Right => LiveKey.NextLayer,
        Key.PageUp => LiveKey.MasterUp,
        Key.PageDown => LiveKey.MasterDown,
        >= Key.D1 and <= Key.D9 => LiveKey.Scene1 + (key - Key.D1),
        >= Key.NumPad1 and <= Key.NumPad9 => LiveKey.Scene1 + (key - Key.NumPad1),
        _ => null,
    };
}
