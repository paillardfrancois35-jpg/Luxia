using Avalonia.Controls;
using Avalonia.Input;

namespace Dmx.UI.Modules.Console;

/// <summary>Écran « Console ».</summary>
public partial class ConsoleView : UserControl
{
    private ConsoleViewModel? _viewModel;

    /// <summary>Crée la vue.</summary>
    public ConsoleView()
    {
        InitializeComponent();
        FaderArea.SizeChanged += (_, e) => _viewModel?.AdaptPageSize(e.NewSize.Width - 12);
        Monitor.ChannelHovered += (_, channel) => _viewModel?.OnMonitorHover(channel);
        Monitor.ChannelClicked += (_, channel) => _viewModel?.GoToChannel(channel);
        SnapshotList.DoubleTapped += (_, _) => _viewModel?.RecallSnapshotCommand.Execute(_viewModel.SelectedSnapshot);
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_viewModel is not null)
        {
            _viewModel.Refreshed -= OnRefreshed;
        }

        _viewModel = DataContext as ConsoleViewModel;
        if (_viewModel is not null)
        {
            _viewModel.Refreshed += OnRefreshed;
            _viewModel.AdaptPageSize(FaderArea.Bounds.Width - 12);
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || _viewModel is null)
        {
            return;
        }

        // Échap : désélectionner.
        if (e.Key == Key.Escape)
        {
            _viewModel.ClearSelectionCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnRefreshed(object? sender, EventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.Frame.CopyTo(Monitor.Values);
        var overrides = _viewModel.Overrides;
        for (var i = 0; i < Monitor.Overridden.Length; i++)
        {
            Monitor.Overridden[i] = overrides[i] >= 0;
        }

        Monitor.Highlight = (_viewModel.FirstChannel, _viewModel.LastChannel);
        Monitor.Revision++;
    }
}
