using System.Reflection;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace Luxia.App.Services;

/// <summary>Libellé lisible d'un bouton ou d'un élément de liste, pour la trace des actions (SORT-066).</summary>
internal static class UiLabel
{
    /// <summary>Texte du bouton, sinon son infobulle, sinon ce qu'il représente.</summary>
    public static string Of(Button button)
    {
        var text = button.Content switch
        {
            string s => s,
            TextBlock block => block.Text,
            Control control => string.Join(" ", control.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text).Where(t => !string.IsNullOrWhiteSpace(t))),
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(text))
        {
            text = ToolTip.GetTip(button) as string;
        }

        if (string.IsNullOrWhiteSpace(text) && button.DataContext is { } context && NameOf(context) is { } name)
        {
            text = name;
        }

        return string.IsNullOrWhiteSpace(text) ? button.Name ?? button.GetType().Name : text.Trim();
    }

    /// <summary>Nom ou titre d'un élément de liste, sinon son texte.</summary>
    public static string Describe(object? item) => item switch
    {
        null => "rien",
        string s => s,
        ContentControl { Content: string s } => s,
        _ => NameOf(item) ?? item.ToString() ?? item.GetType().Name,
    };

    private static string? NameOf(object item)
    {
        foreach (var property in new[] { "Title", "Name", "Label", "Text" })
        {
            if (item.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance) is { PropertyType: var type } info && type == typeof(string)
                && info.GetValue(item) is string value && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
