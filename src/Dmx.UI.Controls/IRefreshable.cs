namespace Dmx.UI.Controls;

/// <summary>
/// Écran rafraîchi périodiquement par la coquille : l'interface lit l'état du moteur à son propre rythme (doc 02 §6.1).
/// </summary>
public interface IRefreshable
{
    /// <summary>Met à jour l'affichage (appelé sur le fil de l'interface, environ 20 fois par seconde).</summary>
    void Refresh();
}
