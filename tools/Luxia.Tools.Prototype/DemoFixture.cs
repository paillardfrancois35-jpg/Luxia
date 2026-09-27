namespace Luxia.Tools.Prototype;

/// <summary>Appareil de démonstration (une lyre) : visée et sélection.</summary>
internal sealed class DemoFixture(string id, string label, double pan, double tilt, bool isSelected)
{
    public string Id { get; } = id;

    public string Label { get; } = label;

    public double Pan { get; set; } = pan;

    public double Tilt { get; set; } = tilt;

    public bool IsSelected { get; set; } = isSelected;
}
