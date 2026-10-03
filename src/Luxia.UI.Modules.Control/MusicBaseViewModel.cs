using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Music.Base;
using Luxia.Music.Classification;
using Luxia.Music.Identification;
using Luxia.Music.Normalization;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>Une ligne de la liste des artistes de la fenêtre « Base musicale ».</summary>
/// <param name="Name">Nom de l'artiste.</param>
/// <param name="Style">Nom de la famille dominante.</param>
/// <param name="Aliases">Alias, séparés par « ; ».</param>
/// <param name="Source">Origine (initial, manuel, correction, import).</param>
/// <param name="Titles">Nombre de titres connus.</param>
public sealed record ArtistRow(string Name, string Style, string Aliases, string Source, int Titles)
{
    /// <summary>Texte de la ligne.</summary>
    public string Label => Titles > 0 ? $"{Name}  ·  {Style}  ·  {Titles} titre(s)" : $"{Name}  ·  {Style}";
}

/// <summary>Une ligne de la liste des doublons probables.</summary>
/// <param name="First">Premier artiste.</param>
/// <param name="Second">Second artiste.</param>
/// <param name="Score">Ressemblance (0 à 1).</param>
public sealed record DuplicateRow(string First, string Second, double Score)
{
    /// <summary>Texte de la ligne.</summary>
    public string Label => string.Create(CultureInfo.CurrentCulture, $"{First}  =  {Second}   ({Score:P0})");
}

/// <summary>Une ligne de la liste « À classer ».</summary>
/// <param name="Item">Artiste (ou titre sans artiste) à classer.</param>
public sealed record ClassifyRow(ClassifyItem Item)
{
    /// <summary>Texte de la ligne : artiste, passages et supposition.</summary>
    public string Label => string.Create(
        CultureInfo.CurrentCulture,
        $"{(Item.HasArtist ? Item.Artist : "(sans artiste)")}  ·  {Item.Plays} passage(s)  ·  {Item.Titles.Count} titre(s)");

    /// <summary>Titres de l'artiste, pour reconnaître de qui il s'agit.</summary>
    public string TitlesText => string.Join("  ·  ", Item.Titles.Take(8).Select(t => t.Title)) + (Item.Titles.Count > 8 ? "  …" : string.Empty);
}

/// <summary>
/// Fenêtre « Base musicale » (MUS-027, MUS-028, MUS-029, Q50) : deux onglets. <b>Base</b> : chercher un artiste, changer son style,
/// ajouter ou retirer un artiste, lui donner un alias, voir ses titres, fusionner des doublons, importer et exporter en CSV.
/// <b>À classer</b> : les titres joués en soirée (journaux) ou importés (playlists) que la base ne sait pas classer, regroupés par
/// artiste ; classer d'un geste (touches 1 à 9, 0 et Q, W, E, R pour les 14 familles ; Entrée = suivant). Les changements sont
/// enregistrés dans le projet tout de suite (comme les corrections en Live).
/// </summary>
public sealed partial class MusicBaseViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;
    private readonly TrackNormalizer _normalizer = new();
    private IReadOnlyList<ClassifyItem> _queue = [];
    private bool _loading;

    [ObservableProperty]
    private string _search = string.Empty;

    [ObservableProperty]
    private ArtistRow? _selectedArtist;

    [ObservableProperty]
    private string _newArtist = string.Empty;

    [ObservableProperty]
    private string _newAlias = string.Empty;

    [ObservableProperty]
    private MusicFamily? _selectedFamily;

    [ObservableProperty]
    private DuplicateRow? _selectedDuplicate;

    [ObservableProperty]
    private ClassifyRow? _selectedToClassify;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private string _classifySummary = string.Empty;

    [ObservableProperty]
    private int _selectedTab;

    /// <summary>Crée la fenêtre sur le moteur en service.</summary>
    public MusicBaseViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _dialogs = dialogs;
        Families = [.. runtime.Music.Families];
        RefreshArtists();
        RefreshToClassify();
    }

    /// <summary>Levé quand la fenêtre demande à choisir un fichier à importer ; la vue répond en appelant <see cref="ImportFile"/>.</summary>
    public event EventHandler? ImportRequested;

    /// <summary>Levé quand la fenêtre demande à enregistrer l'export ; la vue enregistre alors <see cref="ExportText"/>.</summary>
    public event EventHandler? ExportRequested;

    /// <summary>Familles de la taxonomie.</summary>
    public IReadOnlyList<MusicFamily> Families { get; }

    /// <summary>Artistes correspondant à la recherche.</summary>
    public ObservableCollection<ArtistRow> Artists { get; } = [];

    /// <summary>Titres de l'artiste choisi.</summary>
    public ObservableCollection<string> Titles { get; } = [];

    /// <summary>Doublons probables.</summary>
    public ObservableCollection<DuplicateRow> Duplicates { get; } = [];

    /// <summary>Titres à classer, regroupés par artiste.</summary>
    public ObservableCollection<ClassifyRow> ToClassify { get; } = [];

    /// <summary>Un artiste est choisi.</summary>
    public bool HasSelection => SelectedArtist is not null;

    /// <summary>Un titre à classer est choisi.</summary>
    public bool HasToClassifySelection => SelectedToClassify is not null;

    /// <summary>Recalcule la liste des artistes, avec la recherche courante.</summary>
    public void RefreshArtists()
    {
        _loading = true;
        var musicBase = _runtime.Music.Base;
        var keep = SelectedArtist?.Name;
        Artists.Clear();
        foreach (var artist in musicBase.SearchArtists(Search, 400))
        {
            var top = artist.Styles.OrderByDescending(s => s.Value).FirstOrDefault();
            Artists.Add(new ArtistRow(artist.Name, musicBase.FamilyById(top.Key)?.Name ?? "—", string.Join("; ", artist.Aliases), artist.Source, musicBase.TitlesOf(artist.Name).Count));
        }

        SelectedArtist = keep is null ? null : Artists.FirstOrDefault(a => a.Name == keep);
        Summary = string.Create(CultureInfo.CurrentCulture, $"{musicBase.ArtistCount} artiste(s), {musicBase.TitleCount} titre(s) connus{(Artists.Count < musicBase.ArtistCount ? $" · {Artists.Count} affiché(s)" : string.Empty)}");
        _loading = false;
        ShowTitles();
    }

    /// <summary>Recalcule la liste « À classer » avec la base à jour.</summary>
    public void RefreshToClassify()
    {
        var folder = _runtime.Project.Folder;
        var musicBase = _runtime.Music.Base;
        var entries = ReadJournals();
        var pending = folder is null ? [] : MusicStore.LoadPending(folder).Items;
        var identifier = new StyleIdentifier(musicBase);
        _queue = ClassifyList.Build(entries, pending, musicBase, _normalizer, identifier);
        var keep = SelectedToClassify?.Item.ArtistKey;
        ToClassify.Clear();
        foreach (var item in _queue)
        {
            ToClassify.Add(new ClassifyRow(item));
        }

        SelectedToClassify = keep is null ? ToClassify.FirstOrDefault() : ToClassify.FirstOrDefault(r => r.Item.ArtistKey == keep) ?? ToClassify.FirstOrDefault();
        ClassifySummary = _queue.Count == 0
            ? "Rien à classer : tous les morceaux joués ou importés sont identifiés."
            : string.Create(CultureInfo.CurrentCulture, $"{_queue.Count} artiste(s) à classer, {_queue.Sum(i => i.Titles.Count)} titre(s)");
    }

    /// <summary>Change le style de l'artiste choisi.</summary>
    /// <param name="familyId">Famille.</param>
    [RelayCommand]
    private void SetStyle(string? familyId)
    {
        if (SelectedArtist is not { } row || string.IsNullOrWhiteSpace(familyId))
        {
            return;
        }

        _runtime.Music.Base.SetArtistStyle(row.Name, familyId);
        Message = $"{row.Name} : {Families.FirstOrDefault(f => f.Id == familyId)?.Name ?? familyId}";
        RefreshArtists();
    }

    /// <summary>Ajoute un artiste avec la famille choisie dans la liste.</summary>
    [RelayCommand]
    private void AddArtist()
    {
        if (string.IsNullOrWhiteSpace(NewArtist) || SelectedFamily is null)
        {
            Message = "Saisissez le nom de l'artiste et choisissez sa famille.";
            return;
        }

        _runtime.Music.Base.SetArtistStyle(NewArtist.Trim(), SelectedFamily.Id);
        Message = $"{NewArtist.Trim()} ajouté : {SelectedFamily.Name}";
        Search = NewArtist.Trim();
        NewArtist = string.Empty;
        RefreshArtists();
    }

    /// <summary>Retire l'artiste choisi après confirmation.</summary>
    [RelayCommand]
    private async Task RemoveArtistAsync()
    {
        if (SelectedArtist is not { } row)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync("Retirer l'artiste", $"Retirer « {row.Name} » et ses {row.Titles} titre(s) de la base musicale ?\n\nSes passages futurs seront « Inconnu » tant qu'il n'est pas reclassé.").ConfigureAwait(true))
        {
            return;
        }

        _runtime.Music.Base.RemoveArtist(row.Name);
        Message = $"{row.Name} retiré.";
        SelectedArtist = null;
        RefreshArtists();
    }

    /// <summary>Ajoute un alias à l'artiste choisi.</summary>
    [RelayCommand]
    private void AddAlias()
    {
        if (SelectedArtist is not { } row || string.IsNullOrWhiteSpace(NewAlias))
        {
            Message = "Choisissez un artiste et saisissez l'alias.";
            return;
        }

        Message = _runtime.Music.Base.AddAlias(row.Name, NewAlias)
            ? $"Alias « {NewAlias.Trim()} » ajouté à {row.Name}."
            : $"Alias refusé : « {NewAlias.Trim()} » désigne déjà un autre artiste.";
        NewAlias = string.Empty;
        RefreshArtists();
    }

    /// <summary>Cherche les doublons probables.</summary>
    [RelayCommand]
    private void FindDuplicates()
    {
        Duplicates.Clear();
        foreach (var (first, second, score) in _runtime.Music.Base.FindDuplicates())
        {
            Duplicates.Add(new DuplicateRow(first.Name, second.Name, score));
        }

        Message = Duplicates.Count == 0 ? "Aucun doublon probable." : $"{Duplicates.Count} doublon(s) probable(s) : choisissez-en un puis la fiche à garder.";
    }

    /// <summary>Fusionne le doublon choisi en gardant la première ou la seconde fiche.</summary>
    /// <param name="keep"><c>first</c> ou <c>second</c> : la fiche gardée.</param>
    [RelayCommand]
    private async Task MergeAsync(string? keep)
    {
        if (SelectedDuplicate is not { } row)
        {
            return;
        }

        var (kept, removed) = keep == "second" ? (row.Second, row.First) : (row.First, row.Second);
        if (!await _dialogs.ConfirmAsync("Fusionner deux fiches", $"Garder « {kept} » et y fondre « {removed} » (nom en alias, titres repris) ?").ConfigureAwait(true))
        {
            return;
        }

        Message = _runtime.Music.Base.Merge(kept, removed) ? $"« {removed} » fondu dans « {kept} »." : "Fusion impossible.";
        Duplicates.Remove(row);
        RefreshArtists();
    }

    /// <summary>Demande le fichier CSV à importer.</summary>
    [RelayCommand]
    private void ImportCsv() => ImportRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Demande où enregistrer l'export CSV.</summary>
    [RelayCommand]
    private void ExportCsv() => ExportRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Importe le contenu d'un fichier CSV (artistes, titres ou playlist) ; les lignes sans style vont dans « À classer ».</summary>
    /// <param name="csv">Texte du fichier.</param>
    public void ImportFile(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        var musicBase = _runtime.Music.Base;
        var report = MusicCsv.Import(csv, musicBase, _normalizer, new StyleIdentifier(musicBase));
        if (report.ToClassify.Count > 0 && _runtime.Project.Folder is { } folder)
        {
            var known = MusicStore.LoadPending(folder).Items.ToList();
            known.AddRange(report.ToClassify.Where(n => !known.Any(k => k.Artist == n.Artist && k.Title == n.Title)));
            MusicStore.SavePending(folder, new PendingSet { Items = known });
        }

        Message = string.Create(
            CultureInfo.CurrentCulture,
            $"Import : {report.Rows} ligne(s), {report.TitlesSet} titre(s) et {report.ArtistsSet} artiste(s) classés, {report.AlreadyKnown} déjà connus, {report.ToClassify.Count} à classer{(report.Problems.Count > 0 ? $", {report.Problems.Count} remarque(s) : {report.Problems[0]}" : string.Empty)}.");
        RefreshArtists();
        RefreshToClassify();
        if (report.ToClassify.Count > 0)
        {
            SelectedTab = 1;
        }
    }

    /// <summary>Texte CSV de l'export des artistes.</summary>
    /// <returns>Le texte.</returns>
    public string ExportText() => MusicCsv.ExportArtists(_runtime.Music.Base);

    /// <summary>Classe l'artiste (ou le titre sans artiste) choisi dans « À classer » avec une famille, puis passe au suivant.</summary>
    /// <param name="familyId">Famille.</param>
    [RelayCommand]
    private void Classify(string? familyId)
    {
        if (SelectedToClassify is not { } row || string.IsNullOrWhiteSpace(familyId))
        {
            return;
        }

        var item = row.Item;
        var musicBase = _runtime.Music.Base;
        var family = musicBase.FamilyById(familyId);
        if (family is null)
        {
            return;
        }

        var at = DateTimeOffset.Now;
        if (item.HasArtist)
        {
            musicBase.Correct("artist", item.Artist, null, null, family.Id, null, at);
        }
        else
        {
            // Sans artiste, le classement ne peut pas retrouver le titre ; il est noté à part sous un artiste vide interdit : on le saute.
            Message = "Ce titre n'a pas d'artiste : saisissez-le d'abord avec « Saisir… » en soirée, ou ajoutez l'artiste dans le CSV.";
            return;
        }

        Message = $"{item.Artist} → {family.Name}";
        AdvanceAfter(row);
    }

    /// <summary>Passe au suivant sans classer.</summary>
    [RelayCommand]
    private void Skip()
    {
        if (SelectedToClassify is { } row)
        {
            var index = ToClassify.IndexOf(row);
            SelectedToClassify = ToClassify.Count > index + 1 ? ToClassify[index + 1] : null;
        }
    }

    /// <summary>Famille d'un raccourci clavier (1 à 9, 0, puis Q, W, E, R).</summary>
    /// <param name="key">Touche : « 1 » à « 9 », « 0 », « Q », « W », « E », « R ».</param>
    /// <returns>La famille, ou <c>null</c>.</returns>
    public MusicFamily? FamilyOfKey(string key)
    {
        var index = "1234567890QWER".IndexOf(key.ToUpperInvariant(), StringComparison.Ordinal);
        return index >= 0 && index < Families.Count ? Families[index] : null;
    }

    /// <summary>Numéro ou lettre du raccourci d'une famille, pour les boutons.</summary>
    /// <param name="family">Famille.</param>
    /// <returns>Le raccourci.</returns>
    public string KeyOf(MusicFamily family)
    {
        var index = Families.ToList().IndexOf(family);
        return index is >= 0 and < 14 ? "1234567890QWER"[index].ToString() : string.Empty;
    }

    partial void OnSearchChanged(string value)
    {
        if (!_loading)
        {
            RefreshArtists();
        }
    }

    partial void OnSelectedArtistChanged(ArtistRow? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        if (!_loading)
        {
            ShowTitles();
        }
    }

    partial void OnSelectedToClassifyChanged(ClassifyRow? value) => OnPropertyChanged(nameof(HasToClassifySelection));

    private void ShowTitles()
    {
        Titles.Clear();
        if (SelectedArtist is { } row)
        {
            foreach (var title in _runtime.Music.Base.TitlesOf(row.Name))
            {
                var family = _runtime.Music.Base.FamilyById(title.Style)?.Name ?? title.Style;
                Titles.Add($"{title.Title}{(title.Version is { Length: > 0 } v ? $" ({v})" : string.Empty)}  ·  {family}");
            }
        }
    }

    private void AdvanceAfter(ClassifyRow row)
    {
        var index = ToClassify.IndexOf(row);
        RefreshToClassify();
        SelectedToClassify = ToClassify.Count == 0 ? null : ToClassify[Math.Min(index, ToClassify.Count - 1)];
        RefreshArtists();
    }

    private List<EveningEntry> ReadJournals()
    {
        var entries = new List<EveningEntry>();
        var folder = _runtime.Paths.Logs;
        if (!Directory.Exists(folder))
        {
            return entries;
        }

        foreach (var file in Directory.EnumerateFiles(folder, "soiree-*.csv").Order(StringComparer.Ordinal))
        {
            try
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                entries.AddRange(EveningJournal.Parse(reader.ReadToEnd()));
            }
            catch (IOException)
            {
                // Un journal verrouillé (en cours d'écriture) est lu à la prochaine ouverture.
            }
        }

        return entries;
    }
}
