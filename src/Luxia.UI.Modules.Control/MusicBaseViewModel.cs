using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Music.Base;
using Luxia.Music.Classification;
using Luxia.Music.Normalization;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>Une ligne de la liste des artistes de la fenêtre « Base musicale ».</summary>
/// <param name="Code">Code stable de l'artiste.</param>
/// <param name="Name">Nom de l'artiste.</param>
/// <param name="Style">Nom du style.</param>
/// <param name="Aliases">Alias, séparés par « ; ».</param>
/// <param name="Titles">Nombre de titres connus.</param>
public sealed record ArtistRow(string Code, string Name, string Style, string Aliases, int Titles)
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

/// <summary>Une ligne de la liste des propositions de l'outil d'enrichissement (MUS-041).</summary>
/// <param name="Proposal">Proposition.</param>
/// <param name="FamilyName">Nom de la famille proposée.</param>
/// <param name="Weak">Confiance faible (sous 70 %).</param>
public sealed record ProposalRow(Proposal Proposal, string FamilyName, bool Weak)
{
    /// <summary>Texte de la ligne.</summary>
    public string Label => string.Create(CultureInfo.CurrentCulture, $"{Proposal.Artist}  →  {FamilyName}  ·  {Proposal.Confidence:P0}  ·  {Proposal.Source}");

    /// <summary>Étiquettes de la source, pour juger.</summary>
    public string TagsText => "Étiquettes : " + string.Join(", ", Proposal.Tags);
}

/// <summary>Une cellule de texte modifiable de la fiche (un alias) : toute frappe marque la fiche « modifiée ».</summary>
public sealed partial class EditableText : ObservableObject
{
    private readonly Action _changed;

    [ObservableProperty]
    private string _text;

    /// <summary>Crée la cellule.</summary>
    /// <param name="text">Texte de départ.</param>
    /// <param name="changed">Appelé à chaque modification.</param>
    public EditableText(string text, Action changed)
    {
        ArgumentNullException.ThrowIfNull(changed);
        _text = text;
        _changed = changed;
    }

    partial void OnTextChanged(string value) => _changed();
}

/// <summary>Une ligne de la liste des titres de la fiche : titre, version et style propre (facultatif).</summary>
public sealed partial class TitleRow : ObservableObject
{
    private readonly Action _changed;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _version;

    [ObservableProperty]
    private MusicFamily _style;

    /// <summary>Crée la ligne.</summary>
    /// <param name="title">Titre.</param>
    /// <param name="version">Version (« extended mix »), ou vide.</param>
    /// <param name="style">Style propre au titre, ou le choix « comme l'artiste ».</param>
    /// <param name="changed">Appelé à chaque modification.</param>
    public TitleRow(string title, string version, MusicFamily style, Action changed)
    {
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(changed);
        _title = title;
        _version = version;
        _style = style;
        _changed = changed;
    }

    /// <summary>La ligne est vide (titre non saisi) : elle n'est pas enregistrée.</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Title);

    partial void OnTitleChanged(string value) => _changed();

    partial void OnVersionChanged(string value) => _changed();

    partial void OnStyleChanged(MusicFamily value) => _changed();
}

/// <summary>
/// Fenêtre « Base musicale » (MUS-027, MUS-028, MUS-029), charte « liste + fiche » (doc 60). <b>Base</b> : la liste des artistes à gauche
/// (boutons Ajouter, Dupliquer, Supprimer, Chercher les doublons toujours au même endroit, filtre « Inconnu »), la fiche à droite (nom,
/// style, alias et titres saisis directement dans leurs listes). Rien n'est enregistré avant « Enregistrer » ; changer d'élément avec une
/// fiche modifiée pose la question Oui / Non / Annuler. <b>À classer</b> : les artistes « Inconnu » (morceaux joués d'artistes absents de
/// la base), classés d'un clic sur la pastille de leur style. <b>Propositions</b> : l'enrichissement en ligne, à valider.
/// </summary>
public sealed partial class MusicBaseViewModel : ViewModelBase
{
    /// <summary>Confiance à partir de laquelle une proposition est jugée sûre (acceptation en lot).</summary>
    public const double StrongProposal = 0.7;

    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;
    private readonly MusicFamily _sameAsArtist = new() { Id = string.Empty, Name = "(style de l'artiste)" };
    private bool _loading;
    private bool _loadingForm;
    private bool _reverting;
    private ArtistRow? _formRow;

    [ObservableProperty]
    private string _search = string.Empty;

    [ObservableProperty]
    private bool _unknownOnly;

    [ObservableProperty]
    private ArtistRow? _selectedArtist;

    [ObservableProperty]
    private string _formName = string.Empty;

    [ObservableProperty]
    private MusicFamily? _formStyle;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private bool _hasForm;

    [ObservableProperty]
    private DuplicateRow? _selectedDuplicate;

    [ObservableProperty]
    private bool _showDuplicates;

    [ObservableProperty]
    private ArtistRow? _selectedToClassify;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private string _classifySummary = string.Empty;

    [ObservableProperty]
    private ProposalRow? _selectedProposal;

    [ObservableProperty]
    private string _proposalSummary = string.Empty;

    [ObservableProperty]
    private int _selectedTab;

    /// <summary>Crée la fenêtre sur le moteur en service.</summary>
    /// <param name="runtime">Moteur en service.</param>
    /// <param name="dialogs">Boîtes de dialogue.</param>
    public MusicBaseViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _dialogs = dialogs;
        Families = [.. runtime.Music.Families];
        TitleStyles = [_sameAsArtist, .. Families];
        Aliases.CollectionChanged += (_, _) => MarkModified();
        TitleRows.CollectionChanged += (_, _) => MarkModified();
        RefreshArtists();
        RefreshToClassify();
        RefreshProposals();
    }

    /// <summary>Levé quand la fenêtre demande à choisir le fichier JSON à importer ; la vue répond en appelant <see cref="ImportFile"/>.</summary>
    public event EventHandler? ImportRequested;

    /// <summary>Levé quand la fenêtre demande où enregistrer l'export JSON ; la vue répond en appelant <see cref="ExportFile"/>.</summary>
    public event EventHandler? ExportRequested;

    /// <summary>Levé quand une ligne vient d'être ajoutée (ou retrouvée vide) : la vue place le curseur dans sa première cellule.</summary>
    public event EventHandler<object>? FocusRequested;

    /// <summary>Styles de la taxonomie.</summary>
    public IReadOnlyList<MusicFamily> Families { get; }

    /// <summary>Styles qu'on peut donner à un artiste « Inconnu » ou à une proposition : tous sauf « Inconnu » lui-même.</summary>
    public IReadOnlyList<MusicFamily> AssignableFamilies => [.. Families.Where(f => f.Id != Taxonomy.UnknownId)];

    /// <summary>Choix de style d'un titre : « style de l'artiste », puis les styles.</summary>
    public IReadOnlyList<MusicFamily> TitleStyles { get; }

    /// <summary>Artistes correspondant à la recherche et au filtre.</summary>
    public ObservableCollection<ArtistRow> Artists { get; } = [];

    /// <summary>Alias de l'artiste de la fiche (une ligne vide à la fin sert à en ajouter, bouton +).</summary>
    public ObservableCollection<EditableText> Aliases { get; } = [];

    /// <summary>Titres de l'artiste de la fiche.</summary>
    public ObservableCollection<TitleRow> TitleRows { get; } = [];

    /// <summary>Doublons probables (liste montrée seulement s'il y en a).</summary>
    public ObservableCollection<DuplicateRow> Duplicates { get; } = [];

    /// <summary>Artistes « Inconnu », à classer.</summary>
    public ObservableCollection<ArtistRow> ToClassify { get; } = [];

    /// <summary>Propositions de l'outil d'enrichissement, à valider.</summary>
    public ObservableCollection<ProposalRow> Proposals { get; } = [];

    /// <summary>Une proposition est choisie.</summary>
    public bool HasProposalSelection => SelectedProposal is not null;

    /// <summary>Un artiste est choisi dans la liste.</summary>
    public bool HasSelection => SelectedArtist is not null;

    /// <summary>Un artiste « Inconnu » est choisi dans « À classer ».</summary>
    public bool HasToClassifySelection => SelectedToClassify is not null;

    /// <summary>
    /// Tâche de la dernière navigation différée (changement d'artiste avec une fiche modifiée : la question Oui / Non / Annuler est posée
    /// avant de changer) ; les tests l'attendent.
    /// </summary>
    public Task Navigation { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// À appeler avant de quitter la fiche (autre artiste, autre onglet, fermeture) : sans modification, rend <c>true</c> ; sinon pose la
    /// question Oui (enregistrer) / Non (abandonner) / Annuler (rester).
    /// </summary>
    /// <returns><c>true</c> si l'on peut continuer.</returns>
    public async Task<bool> CanLeaveAsync()
    {
        if (!IsModified)
        {
            return true;
        }

        var name = string.IsNullOrWhiteSpace(FormName) ? "(nouvel artiste)" : FormName.Trim();
        switch (await _dialogs.AskSaveAsync("Modifications non enregistrées", $"Enregistrer les modifications de « {name} » ?").ConfigureAwait(true))
        {
            case SaveChoice.Save:
                return TrySave();
            case SaveChoice.Discard:
                IsModified = false;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Recalcule la liste des artistes, avec la recherche et le filtre courants.</summary>
    public void RefreshArtists()
    {
        _loading = true;
        var musicBase = _runtime.Music.Base;
        var keep = _formRow?.Code ?? SelectedArtist?.Code;
        Artists.Clear();
        foreach (var artist in musicBase.SearchArtists(Search, 400, UnknownOnly ? Taxonomy.UnknownId : null))
        {
            Artists.Add(RowOf(artist));
        }

        SelectedArtist = keep is null ? null : Artists.FirstOrDefault(a => a.Code == keep);
        Summary = string.Create(CultureInfo.CurrentCulture, $"{musicBase.ArtistCount} artiste(s), {musicBase.TitleCount} titre(s) connus{(Artists.Count < musicBase.ArtistCount ? $" · {Artists.Count} affiché(s)" : string.Empty)}");
        _loading = false;
        OnPropertyChanged(nameof(HasSelection));
    }

    /// <summary>Recalcule la liste « À classer » : les artistes dont le style est « Inconnu ».</summary>
    public void RefreshToClassify()
    {
        var musicBase = _runtime.Music.Base;
        var keep = SelectedToClassify?.Code;
        ToClassify.Clear();
        foreach (var artist in musicBase.SearchArtists(null, int.MaxValue, Taxonomy.UnknownId))
        {
            ToClassify.Add(RowOf(artist));
        }

        SelectedToClassify = keep is null ? ToClassify.FirstOrDefault() : ToClassify.FirstOrDefault(r => r.Code == keep) ?? ToClassify.FirstOrDefault();
        ClassifySummary = ToClassify.Count == 0
            ? "Rien à classer : tous les artistes de la base ont un style."
            : string.Create(CultureInfo.CurrentCulture, $"{ToClassify.Count} artiste(s) « Inconnu » à classer (morceaux joués d'artistes absents de la base)");
    }

    /// <summary>Relit les propositions de l'outil d'enrichissement (<c>propositions.json</c> du projet).</summary>
    public void RefreshProposals()
    {
        var keep = SelectedProposal?.Proposal.Artist;
        Proposals.Clear();
        if (_runtime.Project.Folder is { } folder)
        {
            foreach (var proposal in MusicStore.LoadProposals(folder).Items.OrderByDescending(p => p.Confidence).ThenBy(p => p.Artist, StringComparer.OrdinalIgnoreCase))
            {
                var name = _runtime.Music.Base.FamilyById(proposal.Style)?.Name ?? proposal.Style;
                Proposals.Add(new ProposalRow(proposal, name, proposal.Confidence < StrongProposal));
            }
        }

        SelectedProposal = keep is null ? Proposals.FirstOrDefault() : Proposals.FirstOrDefault(p => p.Proposal.Artist == keep) ?? Proposals.FirstOrDefault();
        ProposalSummary = Proposals.Count == 0
            ? "Aucune proposition. L'outil luxia-enrich, lancé à la maison, en prépare pour les artistes « Inconnu » ; rien n'entre dans la base sans votre accord."
            : string.Create(CultureInfo.CurrentCulture, $"{Proposals.Count} proposition(s) à valider, dont {Proposals.Count(p => !p.Weak)} de confiance d'au moins {StrongProposal:P0}");
    }

    /// <summary>Enregistre la fiche dans la base (nom, style, alias, titres) ; le message dit pourquoi si elle est refusée.</summary>
    /// <returns><c>true</c> si la fiche est enregistrée.</returns>
    public bool TrySave()
    {
        if (!HasForm)
        {
            return true;
        }

        var style = FormStyle ?? Families.First(f => f.Id == Taxonomy.UnknownId);
        var entry = new ArtistEntry
        {
            Name = FormName.Trim(),
            Style = style.Id,
            Aliases = [.. Aliases.Select(a => a.Text.Trim()).Where(a => a.Length > 0)],
        };
        var titles = TitleRows.Where(t => !t.IsEmpty).Select(t => new TitleEntry
        {
            Title = t.Title.Trim(),
            Version = TextKey.Of(t.Version) is { Length: > 0 } version ? version : null,
            Style = t.Style.Id,
        }).ToList();
        var musicBase = _runtime.Music.Base;
        var error = musicBase.SaveArtist(_formRow?.Code, entry, titles);
        if (error is not null)
        {
            Message = $"Enregistrement refusé : {error}.";
            return false;
        }

        var saved = musicBase.OwnerOf(entry.Name)!;
        Message = $"{saved.Name} enregistré.";
        _formRow = null;
        IsModified = false;
        UnknownOnly = UnknownOnly && saved.Style == Taxonomy.UnknownId;
        if (!string.IsNullOrEmpty(Search) && !musicBase.SearchArtists(Search, int.MaxValue).Any(a => a.Code == saved.Code))
        {
            Search = string.Empty;
        }

        RefreshArtists();
        RefreshToClassify();
        _reverting = true;
        SelectedArtist = Artists.FirstOrDefault(a => a.Code == saved.Code);
        _reverting = false;
        LoadForm(SelectedArtist);
        return true;
    }

    partial void OnSearchChanged(string value)
    {
        if (!_loading)
        {
            RefreshArtists();
        }
    }

    partial void OnUnknownOnlyChanged(bool value)
    {
        if (!_loading)
        {
            RefreshArtists();
        }
    }

    partial void OnSelectedArtistChanged(ArtistRow? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        if (_loading || _reverting)
        {
            return;
        }

        if (!IsModified)
        {
            LoadForm(value);
            return;
        }

        // Fiche modifiée : la sélection revient à l'artiste de la fiche, le temps de poser la question.
        _reverting = true;
        SelectedArtist = _formRow is null ? null : Artists.FirstOrDefault(a => a.Code == _formRow.Code);
        _reverting = false;
        Navigation = GoToAsync(value);
    }

    partial void OnFormNameChanged(string value) => MarkModified();

    partial void OnFormStyleChanged(MusicFamily? value) => MarkModified();

    partial void OnSelectedProposalChanged(ProposalRow? value) => OnPropertyChanged(nameof(HasProposalSelection));

    partial void OnSelectedToClassifyChanged(ArtistRow? value) => OnPropertyChanged(nameof(HasToClassifySelection));

    private ArtistRow RowOf(ArtistEntry artist) =>
        new(artist.Code, artist.Name, _runtime.Music.Base.FamilyById(artist.Style)?.Name ?? "—", string.Join("; ", artist.Aliases), _runtime.Music.Base.TitlesOf(artist.Name).Count);

    private void MarkModified()
    {
        if (!_loadingForm && HasForm)
        {
            IsModified = true;
        }
    }

    partial void OnIsModifiedChanged(bool value)
    {
        SaveFormCommand.NotifyCanExecuteChanged();
        CancelFormCommand.NotifyCanExecuteChanged();
    }

    private async Task GoToAsync(ArtistRow? target)
    {
        if (!await CanLeaveAsync().ConfigureAwait(true))
        {
            return;
        }

        _reverting = true;
        SelectedArtist = target is null ? null : Artists.FirstOrDefault(a => a.Code == target.Code);
        _reverting = false;
        LoadForm(SelectedArtist);
    }

    /// <summary>Remplit la fiche avec un artiste de la base (ou la vide) ; la fiche n'est alors pas « modifiée ».</summary>
    private void LoadForm(ArtistRow? row)
    {
        _loadingForm = true;
        try
        {
            Aliases.Clear();
            TitleRows.Clear();
            _formRow = row;
            var musicBase = _runtime.Music.Base;
            var artist = row is null ? null : musicBase.FindArtistByCode(row.Code);
            HasForm = artist is not null;
            FormName = artist?.Name ?? string.Empty;
            FormStyle = artist is null ? null : musicBase.FamilyById(artist.Style);
            if (artist is not null)
            {
                foreach (var alias in artist.Aliases)
                {
                    Aliases.Add(new EditableText(alias, MarkModified));
                }

                foreach (var title in musicBase.TitlesOf(artist.Name))
                {
                    TitleRows.Add(new TitleRow(title.Title, title.Version ?? string.Empty, TitleStyleOf(title.Style), MarkModified));
                }
            }

            IsModified = false;
        }
        finally
        {
            _loadingForm = false;
        }
    }

    private MusicFamily TitleStyleOf(string styleId) =>
        TitleStyles.FirstOrDefault(f => string.Equals(f.Id, styleId, StringComparison.OrdinalIgnoreCase)) ?? _sameAsArtist;

    /// <summary>Commence la fiche d'un nouvel artiste (style « Inconnu »).</summary>
    [RelayCommand]
    private async Task AddArtistAsync()
    {
        if (!await CanLeaveAsync().ConfigureAwait(true))
        {
            return;
        }

        StartNewForm(string.Empty, Taxonomy.UnknownId, [], []);
        Message = "Nouvel artiste : saisissez son nom, son style, puis Enregistrer.";
    }

    /// <summary>
    /// Commence la fiche d'un nouvel artiste à partir de celui qui est choisi : même style, mêmes titres, mais sans alias (un alias n'appartient
    /// qu'à un seul artiste).
    /// </summary>
    [RelayCommand]
    private async Task DuplicateArtistAsync()
    {
        if (!HasForm || _formRow is null)
        {
            Message = "Choisissez d'abord l'artiste à dupliquer.";
            return;
        }

        if (!await CanLeaveAsync().ConfigureAwait(true))
        {
            return;
        }

        var source = _runtime.Music.Base.FindArtistByCode(_formRow.Code);
        if (source is null)
        {
            return;
        }

        StartNewForm($"{source.Name} (copie)", source.Style, [], [.. _runtime.Music.Base.TitlesOf(source.Name)]);
        Message = "Copie : changez le nom, puis Enregistrer. Les alias ne sont pas copiés.";
    }

    private void StartNewForm(string name, string styleId, IReadOnlyList<string> aliases, IReadOnlyList<TitleEntry> titles)
    {
        _reverting = true;
        SelectedArtist = null;
        _reverting = false;
        LoadForm(null);
        _loadingForm = true;
        HasForm = true;
        FormName = name;
        FormStyle = _runtime.Music.Base.FamilyById(styleId);
        foreach (var alias in aliases)
        {
            Aliases.Add(new EditableText(alias, MarkModified));
        }

        foreach (var title in titles)
        {
            TitleRows.Add(new TitleRow(title.Title, title.Version ?? string.Empty, TitleStyleOf(title.Style), MarkModified));
        }

        _loadingForm = false;
        IsModified = true;
    }

    /// <summary>Supprime l'artiste choisi après confirmation.</summary>
    [RelayCommand]
    private async Task RemoveArtistAsync()
    {
        if (_formRow is null || SelectedArtist is not { } row)
        {
            Message = "Choisissez d'abord l'artiste à supprimer.";
            return;
        }

        if (!await _dialogs.ConfirmAsync("Supprimer l'artiste", $"Supprimer « {row.Name} » et ses {row.Titles} titre(s) de la base musicale ?\n\nSes passages futurs seront « Inconnu » tant qu'il n'est pas reclassé.").ConfigureAwait(true))
        {
            return;
        }

        _runtime.Music.Base.RemoveArtist(row.Name);
        Message = $"{row.Name} supprimé.";
        _reverting = true;
        SelectedArtist = null;
        _reverting = false;
        LoadForm(null);
        RefreshArtists();
        RefreshToClassify();
    }

    /// <summary>Abandonne les modifications de la fiche : elle reprend l'état de la base.</summary>
    [RelayCommand(CanExecute = nameof(IsModified))]
    private void CancelForm()
    {
        LoadForm(_formRow);
        if (_formRow is null)
        {
            Message = "Modifications abandonnées.";
        }
    }

    /// <summary>Enregistre la fiche.</summary>
    [RelayCommand(CanExecute = nameof(IsModified))]
    private void SaveForm() => TrySave();

    /// <summary>Ajoute une ligne d'alias (ou place le curseur sur la ligne vide qui existe déjà).</summary>
    [RelayCommand]
    private void AddAliasRow()
    {
        if (!HasForm)
        {
            return;
        }

        var row = Aliases.FirstOrDefault(a => a.Text.Length == 0);
        if (row is null)
        {
            row = new EditableText(string.Empty, MarkModified);
            Aliases.Add(row);
        }

        FocusRequested?.Invoke(this, row);
    }

    /// <summary>Retire une ligne d'alias.</summary>
    /// <param name="row">Ligne.</param>
    [RelayCommand]
    private void RemoveAliasRow(EditableText? row)
    {
        if (row is not null)
        {
            Aliases.Remove(row);
        }
    }

    /// <summary>Ajoute une ligne de titre (ou place le curseur sur la ligne vide qui existe déjà).</summary>
    [RelayCommand]
    private void AddTitleRow()
    {
        if (!HasForm)
        {
            return;
        }

        var row = TitleRows.FirstOrDefault(t => t.IsEmpty);
        if (row is null)
        {
            row = new TitleRow(string.Empty, string.Empty, _sameAsArtist, MarkModified);
            TitleRows.Add(row);
        }

        FocusRequested?.Invoke(this, row);
    }

    /// <summary>Retire une ligne de titre.</summary>
    /// <param name="row">Ligne.</param>
    [RelayCommand]
    private void RemoveTitleRow(TitleRow? row)
    {
        if (row is not null)
        {
            TitleRows.Remove(row);
        }
    }

    /// <summary>Cherche les doublons probables ; la liste n'apparaît que s'il y en a.</summary>
    [RelayCommand]
    private void FindDuplicates()
    {
        Duplicates.Clear();
        foreach (var (first, second, score) in _runtime.Music.Base.FindDuplicates())
        {
            Duplicates.Add(new DuplicateRow(first.Name, second.Name, score));
        }

        ShowDuplicates = Duplicates.Count > 0;
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
        ShowDuplicates = Duplicates.Count > 0;
        _reverting = true;
        SelectedArtist = null;
        _reverting = false;
        LoadForm(null);
        RefreshArtists();
        RefreshToClassify();
    }

    /// <summary>Demande le fichier JSON à importer.</summary>
    [RelayCommand]
    private void ImportJson() => ImportRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Demande où enregistrer l'export JSON.</summary>
    [RelayCommand]
    private void ExportJson() => ExportRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Importe un fichier JSON de styles, d'artistes et d'alias (ajoute et met à jour, ne supprime rien).</summary>
    /// <param name="path">Fichier.</param>
    public void ImportFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var report = MusicExchange.ImportFromFile(_runtime.Music.Base, path);
        Message = report.Problems.Count > 0 ? $"Import : {report.Summary} Première remarque : {report.Problems[0]}" : $"Import : {report.Summary}";
        _reverting = true;
        SelectedArtist = null;
        _reverting = false;
        LoadForm(null);
        RefreshArtists();
        RefreshToClassify();
    }

    /// <summary>Écrit l'export JSON des styles, artistes et alias.</summary>
    /// <param name="path">Fichier.</param>
    public void ExportFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        MusicExchange.ExportToFile(_runtime.Music.Base, path);
        Message = $"{_runtime.Music.Base.ArtistCount} artiste(s) exporté(s) dans {Path.GetFileName(path)}.";
    }

    /// <summary>Classe l'artiste choisi dans « À classer » avec un style, puis passe au suivant.</summary>
    /// <param name="familyId">Style.</param>
    [RelayCommand]
    private void Classify(string? familyId)
    {
        if (SelectedToClassify is not { } row || string.IsNullOrWhiteSpace(familyId))
        {
            return;
        }

        var musicBase = _runtime.Music.Base;
        var family = musicBase.FamilyById(familyId);
        if (family is null || family.Id == Taxonomy.UnknownId)
        {
            return;
        }

        var index = ToClassify.IndexOf(row);
        musicBase.SetArtistStyle(row.Name, family.Id);
        Message = $"{row.Name} → {family.Name}";
        RefreshToClassify();
        SelectedToClassify = ToClassify.Count == 0 ? null : ToClassify[Math.Min(index, ToClassify.Count - 1)];
        if (_formRow?.Code == row.Code && !IsModified)
        {
            LoadForm(Artists.FirstOrDefault(a => a.Code == row.Code) ?? _formRow);
        }

        RefreshArtists();
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

    /// <summary>Accepte la proposition choisie telle quelle : l'artiste entre dans la base.</summary>
    [RelayCommand]
    private void AcceptProposal() => Accept(SelectedProposal, null);

    /// <summary>Accepte la proposition choisie avec un autre style (modifier).</summary>
    /// <param name="familyId">Style choisi.</param>
    [RelayCommand]
    private void AcceptProposalAs(string? familyId) => Accept(SelectedProposal, familyId);

    /// <summary>Rejette la proposition choisie : elle disparaît et l'outil ne la repropose plus.</summary>
    [RelayCommand]
    private void RejectProposal()
    {
        if (SelectedProposal is not { } row || _runtime.Project.Folder is not { } folder)
        {
            return;
        }

        var set = MusicStore.LoadProposals(folder);
        MusicStore.SaveProposals(folder, set with { Items = [.. set.Items.Where(p => p.Artist != row.Proposal.Artist)], Rejected = [.. set.Rejected, row.Proposal.Artist] });
        Message = $"Proposition pour {row.Proposal.Artist} rejetée.";
        RefreshProposals();
    }

    /// <summary>Accepte en lot toutes les propositions de confiance suffisante, après confirmation.</summary>
    [RelayCommand]
    private async Task AcceptStrongAsync()
    {
        var strong = Proposals.Where(p => !p.Weak).ToList();
        if (strong.Count == 0)
        {
            Message = $"Aucune proposition de confiance d'au moins {StrongProposal:P0}.";
            return;
        }

        if (!await _dialogs.ConfirmAsync("Accepter les propositions sûres", $"Faire entrer dans la base les {strong.Count} proposition(s) de confiance d'au moins {StrongProposal:P0} ?\n\nLes autres restent à examiner une par une.").ConfigureAwait(true))
        {
            return;
        }

        foreach (var row in strong)
        {
            Apply(row, null);
        }

        Message = $"{strong.Count} artiste(s) classé(s) d'après l'enrichissement.";
        RefreshProposals();
        RefreshArtists();
        RefreshToClassify();
    }

    /// <summary>Rejette en lot toutes les propositions, après confirmation.</summary>
    [RelayCommand]
    private async Task RejectAllAsync()
    {
        if (Proposals.Count == 0 || _runtime.Project.Folder is not { } folder)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync("Rejeter toutes les propositions", $"Rejeter les {Proposals.Count} proposition(s) ? L'outil ne les reproposera pas.").ConfigureAwait(true))
        {
            return;
        }

        var set = MusicStore.LoadProposals(folder);
        MusicStore.SaveProposals(folder, set with { Items = [], Rejected = [.. set.Rejected, .. set.Items.Select(p => p.Artist)] });
        Message = "Propositions rejetées.";
        RefreshProposals();
    }

    private void Accept(ProposalRow? row, string? familyId)
    {
        if (row is null)
        {
            return;
        }

        if (familyId is not null && _runtime.Music.Base.FamilyById(familyId) is null)
        {
            return;
        }

        Apply(row, familyId);
        Message = $"{row.Proposal.Artist} classé : {_runtime.Music.Base.FamilyById(familyId ?? row.Proposal.Style)?.Name ?? row.FamilyName}";
        RefreshProposals();
        RefreshArtists();
        RefreshToClassify();
    }

    private void Apply(ProposalRow row, string? familyId)
    {
        _runtime.Music.Base.SetArtistStyle(row.Proposal.Artist, familyId ?? row.Proposal.Style);
        if (_runtime.Project.Folder is { } folder)
        {
            var set = MusicStore.LoadProposals(folder);
            MusicStore.SaveProposals(folder, set with { Items = [.. set.Items.Where(p => p.Artist != row.Proposal.Artist)] });
        }
    }
}
