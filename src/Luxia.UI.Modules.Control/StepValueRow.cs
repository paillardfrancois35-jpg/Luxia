namespace Luxia.UI.Modules.Control;

/// <summary>Une valeur de l'étape éditée, lisible : qui, quoi.</summary>
/// <param name="Who">Appareil ou sélection visé.</param>
/// <param name="What">Ce qui est réglé (« Couleur : #FF0000 », « Intensité : 50 % »…).</param>
/// <param name="Color">Couleur de la pastille (mode d'édition).</param>
public sealed record StepValueRow(string Who, string What, string Color);
