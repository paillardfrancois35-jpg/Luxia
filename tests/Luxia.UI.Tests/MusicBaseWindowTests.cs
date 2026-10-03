using Luxia.Music.Base;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Fenêtre « Base musicale » (MUS-027, MUS-028, MUS-029) : liste + fiche, À classer, propositions, export et import JSON.</summary>
public sealed class MusicBaseWindowTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private MusicBaseViewModel _vm = null!;

    public ValueTask InitializeAsync()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
        foreach (var file in Directory.EnumerateFiles(source, "*.json", SearchOption.AllDirectories))
        {
            var target = Path.Combine(_host.ProjectFolder, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        _host.Runtime.Project.Open(_host.ProjectFolder).ShouldBeTrue();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DisposeAsync();
    }

    private MusicBaseViewModel Open() => _vm = new MusicBaseViewModel(_host.Runtime, _host.Dialogs);

    private MusicBase Base => _host.Runtime.Music.Base;

    private void Select(string name)
    {
        _vm.Search = name.ToLowerInvariant();
        _vm.SelectedArtist = _vm.Artists.First(a => a.Name == name);
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Base_ListsTheArtists_AndTheSearchFilters()
    {
        Open();

        _vm.Artists.Count.ShouldBeGreaterThan(300);
        _vm.Summary.ShouldContain("artiste(s)");

        _vm.Search = "queen";

        _vm.Artists.Select(a => a.Name).ShouldContain("Queen");
        _vm.Artists.Count.ShouldBeLessThan(10);
        _vm.Search = "zzzzzz";
        _vm.Artists.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void SelectingAnArtist_FillsTheForm_WithoutMarkingItModified()
    {
        Base.SaveArtist(Base.FindArtist("queen")!.Code, Base.FindArtist("queen")! with { Aliases = ["Les Reines"] }, [new TitleEntry { Title = "Love of My Life", Style = "slow" }, new TitleEntry { Title = "Radio Ga Ga" }]).ShouldBeNull();
        Open();

        Select("Queen");

        _vm.HasForm.ShouldBeTrue();
        _vm.FormName.ShouldBe("Queen");
        _vm.FormStyle!.Id.ShouldBe("rock");
        _vm.Aliases.Select(a => a.Text).ShouldBe(["Les Reines"]);
        _vm.TitleRows.Select(t => (t.Title, t.Style.Id)).ShouldBe([("Love of My Life", "slow"), ("Radio Ga Ga", string.Empty)]);
        _vm.IsModified.ShouldBeFalse();
        _vm.SaveFormCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Editing_MarksModified_NothingIsWrittenBeforeSave_AndCancelRestores()
    {
        Open();
        Select("Queen");

        _vm.FormStyle = _vm.Families.First(f => f.Id == "festif");
        _vm.AddAliasRowCommand.Execute(null);
        _vm.Aliases[^1].Text = "Les Reines";

        _vm.IsModified.ShouldBeTrue();
        _vm.SaveFormCommand.CanExecute(null).ShouldBeTrue();
        Base.FindArtist("queen")!.Style.ShouldBe("rock", "rien n'est enregistré avant « Enregistrer »");
        Base.FindArtist("les reines").ShouldBeNull();

        _vm.CancelFormCommand.Execute(null);

        _vm.IsModified.ShouldBeFalse();
        _vm.FormStyle!.Id.ShouldBe("rock");
        _vm.Aliases.ShouldBeEmpty();

        _vm.FormStyle = _vm.Families.First(f => f.Id == "festif");
        _vm.AddAliasRowCommand.Execute(null);
        _vm.Aliases[^1].Text = "Les Reines";
        _vm.SaveFormCommand.Execute(null);

        _vm.IsModified.ShouldBeFalse();
        Base.FindArtist("queen")!.Style.ShouldBe("festif");
        Base.FindArtist("les reines")!.Name.ShouldBe("Queen");
        _vm.Artists.First(a => a.Name == "Queen").Style.ShouldBe("Festif / Tubes de soirée");

        // L'enregistrement est différé de 1,5 s ; la fermeture du service l'écrit tout de suite.
        _host.Runtime.Music.Dispose();
        File.ReadAllText(Path.Combine(_host.ProjectFolder, "artistes.json")).ShouldContain("festif");
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void PlusButton_AddsARow_AndReusesTheEmptyOneInsteadOfAddingAnother()
    {
        Open();
        Select("Queen");
        var focused = new List<object>();
        _vm.FocusRequested += (_, row) => focused.Add(row);

        _vm.AddAliasRowCommand.Execute(null);
        _vm.AddAliasRowCommand.Execute(null);
        _vm.AddTitleRowCommand.Execute(null);
        _vm.AddTitleRowCommand.Execute(null);

        _vm.Aliases.Count.ShouldBe(1);
        _vm.TitleRows.Count.ShouldBe(1);
        focused.Count.ShouldBe(4);
        focused[0].ShouldBeSameAs(focused[1]);
        focused[2].ShouldBeSameAs(focused[3]);
        _vm.IsModified.ShouldBeTrue();

        _vm.Aliases[0].Text = "Les Reines";
        _vm.AddAliasRowCommand.Execute(null);
        _vm.Aliases.Count.ShouldBe(2, "plus de ligne vide : on en ajoute une");
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void EmptyRows_AreNotSaved_AndTitlesKeepTheirOwnStyle()
    {
        Open();
        Select("Queen");
        _vm.AddAliasRowCommand.Execute(null);
        _vm.AddTitleRowCommand.Execute(null);
        _vm.TitleRows[0].Title = "Love of My Life";
        _vm.TitleRows[0].Style = _vm.TitleStyles.First(f => f.Id == "slow");
        _vm.AddTitleRowCommand.Execute(null);

        _vm.SaveFormCommand.Execute(null);

        Base.FindArtist("queen")!.Aliases.ShouldBeEmpty();
        Base.TitlesOf("Queen").ShouldHaveSingleItem().Style.ShouldBe("slow");
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Save_IsRefused_WithTheReason_WhenTheAliasBelongsToAnotherArtist()
    {
        Open();
        Select("Queen");
        _vm.AddAliasRowCommand.Execute(null);
        _vm.Aliases[0].Text = "ABBA";

        _vm.SaveFormCommand.Execute(null);

        _vm.Message.ShouldContain("refusé");
        _vm.Message.ShouldContain("ABBA");
        _vm.IsModified.ShouldBeTrue("la fiche reste ouverte, rien n'est perdu");
        Base.FindArtist("queen")!.Aliases.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Add_StartsABlankForm_AndSaveCreatesTheArtist()
    {
        Open();

        _vm.AddArtistCommand.Execute(null);

        _vm.HasForm.ShouldBeTrue();
        _vm.FormStyle!.Id.ShouldBe("inconnu");
        _vm.SaveFormCommand.Execute(null);
        _vm.Message.ShouldContain("refusé", Case.Insensitive);

        _vm.FormName = "Mon Groupe Local";
        _vm.FormStyle = _vm.Families.First(f => f.Id == "rock");
        _vm.SaveFormCommand.Execute(null);

        Base.FindArtist("mon groupe local")!.Style.ShouldBe("rock");
        _vm.SelectedArtist!.Name.ShouldBe("Mon Groupe Local");
        _vm.IsModified.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public void Duplicate_CopiesStyleAndTitles_ButNotTheAliases()
    {
        Base.SaveArtist(Base.FindArtist("queen")!.Code, Base.FindArtist("queen")! with { Aliases = ["Les Reines"] }, [new TitleEntry { Title = "Radio Ga Ga" }]).ShouldBeNull();
        Open();
        Select("Queen");

        _vm.DuplicateArtistCommand.Execute(null);

        _vm.FormName.ShouldBe("Queen (copie)");
        _vm.FormStyle!.Id.ShouldBe("rock");
        _vm.Aliases.ShouldBeEmpty();
        _vm.TitleRows.ShouldHaveSingleItem().Title.ShouldBe("Radio Ga Ga");
        _vm.IsModified.ShouldBeTrue();
        _vm.SelectedArtist.ShouldBeNull();

        _vm.FormName = "Queen Cover";
        _vm.SaveFormCommand.Execute(null);

        Base.FindArtist("queen cover")!.Style.ShouldBe("rock");
        Base.FindArtist("queen")!.Aliases.ShouldBe(["Les Reines"]);
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public async Task ChangingArtistWithAModifiedForm_AsksYesNoCancel()
    {
        Open();
        Select("Queen");
        _vm.FormStyle = _vm.Families.First(f => f.Id == "festif");
        _vm.Search = string.Empty;
        var stromae = _vm.Artists.First(a => a.Name == "ABBA");

        // Annuler : on reste sur la fiche, rien n'est changé.
        _host.Dialogs.SaveAnswer = SaveChoice.Cancel;
        _vm.SelectedArtist = stromae;
        await _vm.Navigation;

        _vm.FormName.ShouldBe("Queen");
        _vm.SelectedArtist!.Name.ShouldBe("Queen");
        _vm.IsModified.ShouldBeTrue();
        _host.Dialogs.SaveQuestions.ShouldHaveSingleItem().ShouldContain("Queen");

        // Non : les modifications sont abandonnées, on change d'artiste.
        _host.Dialogs.SaveAnswer = SaveChoice.Discard;
        _vm.SelectedArtist = stromae;
        await _vm.Navigation;

        _vm.FormName.ShouldBe("ABBA");
        _vm.IsModified.ShouldBeFalse();
        Base.FindArtist("queen")!.Style.ShouldBe("rock");

        // Oui : enregistré, puis on change d'artiste.
        _vm.Search = "queen";
        _vm.SelectedArtist = _vm.Artists.First(a => a.Name == "Queen");
        _vm.FormStyle = _vm.Families.First(f => f.Id == "festif");
        _host.Dialogs.SaveAnswer = SaveChoice.Save;
        _vm.Search = string.Empty;
        _vm.SelectedArtist = _vm.Artists.First(a => a.Name == "ABBA");
        await _vm.Navigation;

        _vm.FormName.ShouldBe("ABBA");
        Base.FindArtist("queen")!.Style.ShouldBe("festif");
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public async Task CanLeave_IsImmediateWithoutModification_AndAsksOtherwise()
    {
        Open();
        Select("Queen");

        (await _vm.CanLeaveAsync()).ShouldBeTrue();
        _host.Dialogs.SaveQuestions.ShouldBeEmpty();

        _vm.FormName = "Queen 2";
        _host.Dialogs.SaveAnswer = SaveChoice.Cancel;
        (await _vm.CanLeaveAsync()).ShouldBeFalse();
        _host.Dialogs.SaveAnswer = SaveChoice.Discard;
        (await _vm.CanLeaveAsync()).ShouldBeTrue();
        _vm.IsModified.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public async Task Remove_AsksConfirmation_AndStaysIfTheUserRefuses()
    {
        Open();
        Base.SetArtistStyle("Mon Groupe Local", "rock");
        _vm.RefreshArtists();
        Select("Mon Groupe Local");
        _host.Dialogs.ConfirmAnswer = false;

        await _vm.RemoveArtistCommand.ExecuteAsync(null);

        Base.FindArtist("mon groupe local").ShouldNotBeNull();

        _host.Dialogs.ConfirmAnswer = true;
        await _vm.RemoveArtistCommand.ExecuteAsync(null);

        _host.Dialogs.Confirmations.Count.ShouldBe(2);
        Base.FindArtist("mon groupe local").ShouldBeNull();
        _vm.HasForm.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "MUS-027")]
    public async Task Duplicates_NoneGivesAMessage_SomeShowThePanelAndMerge()
    {
        Open();

        _vm.FindDuplicatesCommand.Execute(null);

        _vm.ShowDuplicates.ShouldBeFalse();
        _vm.Message.ShouldContain("Aucun doublon");

        Base.SetArtistStyle("Beatles Doublon", "rock");
        Base.SetArtistStyle("Doublon Beatles", "rock");

        _vm.FindDuplicatesCommand.Execute(null);

        _vm.ShowDuplicates.ShouldBeTrue();
        var row = _vm.Duplicates.ShouldHaveSingleItem();
        _vm.SelectedDuplicate = row;
        await _vm.MergeCommand.ExecuteAsync("first");

        _vm.Duplicates.ShouldBeEmpty();
        _vm.ShowDuplicates.ShouldBeFalse();
        Base.FindArtist("beatles doublon")!.Name.ShouldBe(Base.FindArtist("doublon beatles")!.Name, "les deux écritures désignent la même fiche");
    }

    [Fact]
    [Trait("Exigence", "MUS-028")]
    public void UnknownFilter_AndTheClassifyTab_ListTheUnknownArtists_AndAClickClassifies()
    {
        Base.InjectUnknownArtist("Artiste Alpha");
        Base.InjectUnknownArtist("Artiste Beta");
        Open();

        _vm.UnknownOnly = true;
        _vm.Artists.Select(a => a.Name).ShouldBe(["Artiste Alpha", "Artiste Beta"]);
        _vm.UnknownOnly = false;

        _vm.ToClassify.Select(r => r.Name).ShouldBe(["Artiste Alpha", "Artiste Beta"]);
        _vm.ClassifySummary.ShouldContain("2 artiste(s)");
        _vm.SelectedToClassify!.Name.ShouldBe("Artiste Alpha");

        _vm.ClassifyCommand.Execute("latino");

        _vm.ToClassify.ShouldHaveSingleItem().Name.ShouldBe("Artiste Beta");
        _vm.SelectedToClassify!.Name.ShouldBe("Artiste Beta");
        Base.FindArtist("artiste alpha")!.Style.ShouldBe("latino");

        _vm.ClassifyCommand.Execute("pop");

        _vm.ToClassify.ShouldBeEmpty();
        _vm.ClassifySummary.ShouldContain("Rien à classer");
    }

    [Fact]
    [Trait("Exigence", "MUS-028")]
    public void Classify_RefusesTheUnknownStyle_AndSkipMovesOn()
    {
        Base.InjectUnknownArtist("Artiste Alpha");
        Base.InjectUnknownArtist("Artiste Beta");
        Open();

        _vm.ClassifyCommand.Execute("inconnu");
        _vm.ToClassify.Count.ShouldBe(2);

        _vm.SkipCommand.Execute(null);

        _vm.SelectedToClassify!.Name.ShouldBe("Artiste Beta");
        _vm.ToClassify.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Exigence", "MUS-029")]
    public void ExportThenImport_TravelsStylesArtistsAndAliases()
    {
        Base.AddAlias("Queen", "Les Reines");
        Open();
        var file = Path.Combine(_host.ProjectFolder, "echange.json");

        _vm.ExportFile(file);

        File.ReadAllText(file).ShouldContain("\"aliases\"");
        Base.RemoveArtist("Queen");
        Base.FindArtist("les reines").ShouldBeNull();

        _vm.ImportFile(file);

        _vm.Message.ShouldContain("Import");
        Base.FindArtist("les reines")!.Name.ShouldBe("Queen");
        _vm.Artists.ShouldContain(a => a.Name == "Queen");
    }

    private void WriteProposals(params Luxia.Music.Classification.Proposal[] items) =>
        MusicStore.SaveProposals(_host.ProjectFolder, new Luxia.Music.Classification.ProposalSet { Items = items });

    private static Luxia.Music.Classification.Proposal Proposal(string artist, string style, double confidence) =>
        new() { Artist = artist, Style = style, Confidence = confidence, Source = "MusicBrainz", Tags = ["hardstyle", "electronic"] };

    [Fact]
    [Trait("Exigence", "MUS-041")]
    public void Proposals_AreListedStrongestFirst_AndNothingEntersTheBaseBeforeAcceptance()
    {
        WriteProposals(Proposal("Artiste Faible", "pop", 0.55), Proposal("Artiste Sûr", "electro", 0.85));

        Open();

        _vm.Proposals.Select(p => p.Proposal.Artist).ShouldBe(["Artiste Sûr", "Artiste Faible"]);
        _vm.Proposals[1].Weak.ShouldBeTrue();
        _vm.SelectedProposal!.Proposal.Artist.ShouldBe("Artiste Sûr");
        _vm.ProposalSummary.ShouldContain("2 proposition(s)");
        Base.FindArtist("artiste sur").ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "MUS-041")]
    public void AcceptingAProposal_ClassifiesTheArtist_AndRemovesIt()
    {
        WriteProposals(Proposal("Artiste Sûr", "electro", 0.85), Proposal("Autre Artiste", "pop", 0.8));
        Open();

        _vm.AcceptProposalCommand.Execute(null);

        Base.FindArtist("artiste sur")!.Style.ShouldBe("electro");
        _vm.Proposals.ShouldHaveSingleItem().Proposal.Artist.ShouldBe("Autre Artiste");
        MusicStore.LoadProposals(_host.ProjectFolder).Items.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Exigence", "MUS-041")]
    public void AcceptingWithAnotherStyle_Modifies_AndRejecting_RemembersTheRefusal()
    {
        WriteProposals(Proposal("Artiste Un", "electro", 0.85), Proposal("Artiste Deux", "pop", 0.8));
        Open();

        _vm.AcceptProposalAsCommand.Execute("latino");
        Base.FindArtist("artiste un")!.Style.ShouldBe("latino");

        _vm.RejectProposalCommand.Execute(null);

        _vm.Proposals.ShouldBeEmpty();
        Base.FindArtist("artiste deux").ShouldBeNull();
        MusicStore.LoadProposals(_host.ProjectFolder).Rejected.ShouldBe(["Artiste Deux"]);
    }

    [Fact]
    [Trait("Exigence", "MUS-041")]
    public async Task AcceptStrong_TakesOnlyTheConfidentOnes_AfterConfirmation_AndRejectAllClears()
    {
        WriteProposals(Proposal("Artiste Sûr", "electro", 0.85), Proposal("Artiste Faible", "pop", 0.55));
        Open();

        _host.Dialogs.ConfirmAnswer = false;
        await _vm.AcceptStrongCommand.ExecuteAsync(null);
        Base.FindArtist("artiste sur").ShouldBeNull("refus : rien n'entre");

        _host.Dialogs.ConfirmAnswer = true;
        await _vm.AcceptStrongCommand.ExecuteAsync(null);

        Base.FindArtist("artiste sur").ShouldNotBeNull();
        Base.FindArtist("artiste faible").ShouldBeNull("sous 70 % : à examiner à la main");
        _vm.Proposals.ShouldHaveSingleItem().Proposal.Artist.ShouldBe("Artiste Faible");

        await _vm.RejectAllCommand.ExecuteAsync(null);

        _vm.Proposals.ShouldBeEmpty();
        _vm.ProposalSummary.ShouldContain("Aucune proposition");
    }
}
