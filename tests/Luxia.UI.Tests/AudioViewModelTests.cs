using Luxia.Audio;
using Luxia.Core.Time;
using Luxia.Hosting;
using Luxia.Persistence;
using Luxia.UI.Modules.Audio;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.UI.Tests;

/// <summary>Écran Audio (AUD-080, AUD-081) : lecture de l'écoute, réglages mémorisés, calibration.</summary>
public sealed class AudioViewModelTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dmx-ui-tests", Guid.NewGuid().ToString("N"));
    private readonly FakeSources _sources = new();
    private readonly VirtualClock _clock = new();
    private LuxiaRuntime _runtime = null!;

    public ValueTask InitializeAsync()
    {
        var paths = new DataPaths(Path.Combine(_root, "Documents"), Path.Combine(_root, "AppData"));
        _runtime = new LuxiaRuntime(paths, NullLoggerFactory.Instance, null, _clock, null, _sources);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await _runtime.DisposeAsync();

    private sealed class FakeSource : IAudioSource
    {
        public string Name => "Haut-parleurs simulés";

        public int SampleRate => 44100;

        public event EventHandler<AudioBlock>? BlockAvailable
        {
            add { }
            remove { }
        }

        public event EventHandler<Exception?>? Stopped
        {
            add { }
            remove { }
        }

        public void StartCapture()
        {
        }

        public void StopCapture()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeSources : IAudioSourceFactory
    {
        public List<string?> Requested { get; } = [];

        public IAudioSource CreateLoopback() => Create(null);

        public IAudioSource Create(string? deviceId)
        {
            Requested.Add(deviceId);
            return new FakeSource();
        }

        public IReadOnlyList<AudioDeviceInfo> Devices() => [new("hp", "Haut-parleurs", false), new("micro", "Micro USB", true)];
    }

    [Fact]
    [Trait("Exigence", "AUD-080")]
    public void Screen_ListsTheDevices_AndStartsStopped()
    {
        var vm = new AudioViewModel(_runtime);

        vm.Available.ShouldBeTrue();
        vm.Listening.ShouldBeFalse();
        vm.Devices.Select(d => d.Label).ShouldBe(["Le son joué par le PC (sortie par défaut)", "Sortie : Haut-parleurs", "Entrée : Micro USB"]);
        vm.SelectedDevice!.Id.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "AUD-003")]
    public void ChoosingTheMicrophone_IsRememberedAndUsedByTheListening()
    {
        var vm = new AudioViewModel(_runtime);
        vm.Listening = true;

        vm.SelectedDevice = vm.Devices.Single(d => d.Id == "micro");

        _runtime.Preferences.Current.Audio.DeviceId.ShouldBe("micro");
        _sources.Requested.ShouldBe([null, "micro"]);
        new AudioViewModel(_runtime).SelectedDevice!.Id.ShouldBe("micro");
    }

    [Fact]
    [Trait("Exigence", "AUD-081")]
    public void Settings_AreAppliedAfterAShortWhile_AndRemembered()
    {
        var vm = new AudioViewModel(_runtime)
        {
            Sensitivity = 80,
            LatencyMilliseconds = -40,
            PreferredBpm = 100,
        };

        for (var i = 0; i < 12; i++)
        {
            vm.Refresh();
        }

        var saved = _runtime.Preferences.Current.Audio;
        saved.PulseSensitivity.ShouldBe(0.8, 1e-9);
        saved.LatencySeconds.ShouldBe(-0.04, 1e-9);
        saved.PreferredBpm.ShouldBe(100);
        _runtime.Audio!.Tuning.PulseSensitivity.ShouldBe(0.8, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "AUD-027")]
    public void Calibration_WithoutTheScene_ExplainsWhere()
    {
        var vm = new AudioViewModel(_runtime);

        vm.StartCalibrationCommand.Execute(null);

        vm.Message!.ShouldContain(AudioViewModel.CalibrationSceneName);
        vm.Calibrating.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "AUD-080")]
    public void Refresh_WithoutSound_ShowsNothingHeard()
    {
        var vm = new AudioViewModel(_runtime);
        vm.Refresh();

        vm.BpmText.ShouldBe("—");
        vm.Level.ShouldBe(0);
        vm.EnergyText.ShouldBe("—");
    }
}
