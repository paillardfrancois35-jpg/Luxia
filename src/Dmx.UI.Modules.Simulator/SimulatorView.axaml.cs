using Avalonia.Controls;

namespace Dmx.UI.Modules.Simulator;

/// <summary>Écran « Simulateur » (doc 14).</summary>
public partial class SimulatorView : UserControl
{
    private SimulatorViewModel? _viewModel;

    /// <summary>Crée la vue.</summary>
    public SimulatorView()
    {
        InitializeComponent();
        Canvas.FixtureHovered += (_, id) => _viewModel?.OnFixtureHovered(id);
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_viewModel is not null)
        {
            _viewModel.Refreshed -= OnRefreshed;
        }

        _viewModel = DataContext as SimulatorViewModel;
        if (_viewModel is not null)
        {
            _viewModel.Refreshed += OnRefreshed;
        }
    }

    private void OnRefreshed(object? sender, EventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        Canvas.Fixtures = [.. _viewModel.Fixtures];
        Canvas.InvalidateVisual();
    }
}
