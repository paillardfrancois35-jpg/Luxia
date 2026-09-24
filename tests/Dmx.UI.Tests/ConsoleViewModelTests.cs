using Dmx.UI.Controls;
using Dmx.UI.Modules.Console;

namespace Dmx.UI.Tests;

/// <summary>Console en mode canaux : logique des faders, de la sélection et des instantanés (doc 11 §3).</summary>
public sealed class ConsoleViewModelTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private ConsoleViewModel _console = null!;

    public ValueTask InitializeAsync()
    {
        _console = new ConsoleViewModel(_host.Runtime, _host.Dialogs);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "CONS-001")]
    public void PageSize_AdaptsToWidth_AndPagesCoverAll512Channels()
    {
        _console.AdaptPageSize(32 * ConsoleViewModel.StripWidth);
        _console.Channels.Count.ShouldBe(32);

        var firsts = new List<int>();
        for (var i = 0; i < 16; i++)
        {
            firsts.Add(_console.FirstChannel);
            _console.NextPageCommand.Execute(null);
        }

        firsts.ShouldBe(Enumerable.Range(0, 16).Select(i => (i * 32) + 1));
        _console.FirstChannel.ShouldBe(1); // retour au début après la dernière page
    }

    [Fact]
    [Trait("Exigence", "CONS-003")]
    [Trait("Exigence", "CONS-005")]
    public void FaderRequest_OverridesChannel_AndFaderShowsEmittedValue()
    {
        var ch1 = _console.Channels[0];

        _console.OnFaderRequest(ch1, new FaderValueRequest(200, 200));
        _host.Tick();
        _console.Refresh();

        _host.Frame()[0].ShouldBe((byte)200);
        ch1.IsOverridden.ShouldBeTrue();
        ch1.Value.ShouldBe(200);
    }

    [Fact]
    [Trait("Exigence", "CONS-006")]
    public void MultiSelection_Relative_MovesAllByTheSameDelta()
    {
        SetAndTick((1, 100), (2, 50));
        _console.OnFaderPressed(_console.Channels[0], control: false, shift: false);
        _console.OnFaderPressed(_console.Channels[1], control: true, shift: false);

        _console.OnFaderRequest(_console.Channels[0], new FaderValueRequest(120, 20));
        _host.Tick();

        _host.Frame()[0].ShouldBe((byte)120);
        _host.Frame()[1].ShouldBe((byte)70);
    }

    [Fact]
    [Trait("Exigence", "CONS-006")]
    public void MultiSelection_Absolute_SetsSameValue()
    {
        SetAndTick((1, 100), (2, 50));
        _console.IsRelative = false;
        _console.OnFaderPressed(_console.Channels[0], control: false, shift: false);
        _console.OnFaderPressed(_console.Channels[2], control: false, shift: true);

        _console.OnFaderRequest(_console.Channels[1], new FaderValueRequest(10, -40));
        _host.Tick();

        _host.Frame()[..3].ShouldBe(new byte[] { 10, 10, 10 });
    }

    [Fact]
    [Trait("Exigence", "CONS-006")]
    public void ClickWithoutModifier_OutsideSelection_SelectsOnlyThatFader()
    {
        _console.OnFaderPressed(_console.Channels[0], control: false, shift: false);
        _console.OnFaderPressed(_console.Channels[1], control: true, shift: false);

        _console.OnFaderPressed(_console.Channels[5], control: false, shift: false);

        _console.Channels.Count(c => c.IsSelected).ShouldBe(1);
        _console.Channels[5].IsSelected.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "CONS-004")]
    public void PageToFull_ThenReleasePage()
    {
        _console.AdaptPageSize(16 * ConsoleViewModel.StripWidth);
        _console.PageToFullCommand.Execute(null);
        _host.Tick();
        _host.Frame()[..16].ShouldAllBe(v => v == 255);
        _host.Frame()[16].ShouldBe((byte)0);

        _console.ReleasePageCommand.Execute(null);
        _host.Tick();
        _host.Frame().ShouldAllBe(v => v == 0);
        _host.Runtime.Engine.OverrideCount(1).ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "CONS-004")]
    public void ReleaseSelection_OnlyReleasesSelectedChannels()
    {
        SetAndTick((1, 10), (2, 20), (3, 30));
        _console.OnFaderPressed(_console.Channels[1], control: false, shift: false);

        _console.ReleaseSelectionCommand.Execute(null);
        _host.Tick();

        _host.Frame()[..3].ShouldBe(new byte[] { 10, 0, 30 });
    }

    [Fact]
    [Trait("Exigence", "CONS-002")]
    public void TypedValue_Valid_IsApplied_Invalid_IsRejected()
    {
        _console.OnValueTyped(_console.Channels[3], "128");
        _console.OnValueTyped(_console.Channels[4], "300");
        _host.Tick();

        _host.Frame()[3].ShouldBe((byte)128);
        _host.Frame()[4].ShouldBe((byte)0);
        _console.Message!.ShouldContain("Valeur invalide");
    }

    [Fact]
    [Trait("Exigence", "CONS-041")]
    public void MonitorHover_DescribesChannel()
    {
        SetAndTick((5, 255));
        _console.Refresh();

        _console.OnMonitorHover(5);

        _console.HoverText.ShouldBe("Canal 5 – valeur 255 (100 %) – pris à la console");
    }

    [Fact]
    [Trait("Exigence", "CONS-010")]
    public async Task Snapshot_SaveReleaseRecall_RestoresOverrides()
    {
        _host.Runtime.Project.Create(_host.ProjectFolder, "Essai");
        SetAndTick((1, 255), (2, 128));
        _console.Refresh();

        _host.Dialogs.TextAnswers.Enqueue("PAR 1 en blanc");
        await _console.SaveSnapshotCommand.ExecuteAsync(null);
        _console.Snapshots.Single().Name.ShouldBe("PAR 1 en blanc");
        File.Exists(Path.Combine(_host.ProjectFolder, "console.json")).ShouldBeTrue();

        _console.ReleaseAllCommand.Execute(null);
        _host.Tick();
        _host.Frame()[0].ShouldBe((byte)0);

        _console.RecallSnapshotCommand.Execute(_console.Snapshots[0]);
        _host.Tick();
        _host.Frame()[..3].ShouldBe(new byte[] { 255, 128, 0 });
    }

    [Fact]
    [Trait("Exigence", "GEN-103")]
    public async Task Snapshot_Delete_AsksConfirmation()
    {
        _host.Runtime.Project.Create(_host.ProjectFolder, "Essai");
        SetAndTick((1, 1));
        _console.Refresh();
        _host.Dialogs.TextAnswers.Enqueue("A");
        await _console.SaveSnapshotCommand.ExecuteAsync(null);
        _console.SelectedSnapshot = _console.Snapshots[0];

        _host.Dialogs.ConfirmAnswer = false;
        await _console.DeleteSnapshotCommand.ExecuteAsync(null);
        _console.Snapshots.Count.ShouldBe(1);

        _host.Dialogs.ConfirmAnswer = true;
        await _console.DeleteSnapshotCommand.ExecuteAsync(null);
        _console.Snapshots.ShouldBeEmpty();
        _host.Dialogs.Confirmations.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Snapshot_WithoutProject_ExplainsWhy()
    {
        SetAndTick((1, 1));
        _console.Refresh();

        await _console.SaveSnapshotCommand.ExecuteAsync(null);

        _console.Message!.ShouldContain("projet");
    }

    private void SetAndTick(params (int Channel, byte Value)[] values)
    {
        _host.Runtime.SetChannels(1, [.. values.Select(v => new Messaging.Commands.ChannelValue(v.Channel, v.Value))]);
        _host.Tick();
        _console.Refresh();
    }
}
