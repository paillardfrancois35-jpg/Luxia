namespace Luxia.Messaging.Commands;

/// <summary>CMD-041 <c>ChoisirSourceTempo</c> : choisit la source du tempo ; pour Fixe, avec le BPM à appliquer.</summary>
/// <param name="Origin">Origine.</param>
/// <param name="Source">Source.</param>
/// <param name="Bpm">BPM fixe (source Fixe) ; vide = on garde le tempo courant.</param>
public sealed record SetTempoSourceCommand(CommandOrigin Origin, TempoSourceKind Source, double? Bpm = null) : Command(Origin);
