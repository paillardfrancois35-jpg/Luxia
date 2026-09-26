namespace Luxia.UI.Controls;

/// <summary>
/// Écran rafraîchi périodiquement par la coquille : l'interface lit l'état du moteur à son propre rythme (doc 02 §6.1).
/// </summary>
public interface IRefreshable
{
    /// <summary>Met à jour l'affichage (appelé sur le fil de l'interface, environ 20 fois par seconde).</summary>
    void Refresh();

    /// <summary>
    /// Vrai si l'écran pilote une surcharge de canaux réels qui doit continuer même quand il n'est pas affiché
    /// (ex. Identifier, CMD-023) : la coquille l'appelle alors en plus de l'écran affiché, pour qu'un appareil
    /// identifié ne se fige pas au dernier état si l'utilisateur change d'écran (ex. pour regarder le simulateur,
    /// SIM-009) pendant que « Identifier » clignote.
    /// </summary>
    bool NeedsBackgroundRefresh => false;
}
