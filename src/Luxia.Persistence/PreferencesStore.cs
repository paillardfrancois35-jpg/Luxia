using Luxia.Core.Settings;
using Luxia.Persistence.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Persistence;

/// <summary>
/// Préférences du poste (<c>%AppData%\LuXia\preferences.json</c>). Un fichier absent ou illisible donne les valeurs
/// par défaut, jamais une erreur bloquante (GEN-056).
/// </summary>
public sealed class PreferencesStore
{
    /// <summary>Type de document « préférences ».</summary>
    public static readonly DocumentType<Preferences> DocumentType = new("préférences", Preferences.CurrentFormatVersion, []);

    private readonly string _path;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private Preferences _current = new();

    /// <summary>Crée le magasin pour un fichier donné.</summary>
    public PreferencesStore(string path, ILogger<PreferencesStore>? logger = null)
    {
        _path = path;
        _logger = logger ?? NullLogger<PreferencesStore>.Instance;
    }

    /// <summary>Préférences courantes.</summary>
    public Preferences Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    /// <summary>Levé après chaque modification.</summary>
    public event EventHandler<Preferences>? Changed;

    /// <summary>Charge le fichier ; en cas de problème, garde les valeurs par défaut et le signale.</summary>
    public LoadResult<Preferences> Load()
    {
        var result = VersionedJsonFile.Load(_path, DocumentType);
        switch (result.Status)
        {
            case LoadStatus.Loaded:
            case LoadStatus.Migrated:
                lock (_lock)
                {
                    _current = result.Value!;
                }

                if (result.Message is not null)
                {
                    _logger.LogInformation("{Message}", result.Message);
                }

                break;
            case LoadStatus.Missing:
                _logger.LogInformation("Préférences absentes : valeurs par défaut ({Chemin})", _path);
                break;
            case LoadStatus.Invalid:
            case LoadStatus.TooRecent:
            default:
                _logger.LogWarning("{Message} ; valeurs par défaut utilisées", result.Message);
                break;
        }

        return result;
    }

    /// <summary>Modifie et enregistre les préférences.</summary>
    public void Update(Func<Preferences, Preferences> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        Preferences updated;
        lock (_lock)
        {
            updated = change(_current);
            _current = updated;
            try
            {
                VersionedJsonFile.Save(_path, updated, DocumentType);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Impossible d'enregistrer les préférences ({Chemin})", _path);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Impossible d'enregistrer les préférences ({Chemin})", _path);
            }
        }

        Changed?.Invoke(this, updated);
    }
}
