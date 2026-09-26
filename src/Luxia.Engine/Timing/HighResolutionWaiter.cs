using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace Luxia.Engine.Timing;

/// <summary>
/// Attente précise sous Windows : minuterie « haute résolution » (Windows 10 1803+), précision ≈ 0,5 ms,
/// là où <see cref="Thread.Sleep(int)"/> et les minuteries .NET ont une granularité de 15,6 ms. Nécessaire pour GEN-031.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed partial class HighResolutionWaiter : IDisposable
{
    private const uint CreateWaitableTimerHighResolution = 0x00000002;
    private const uint TimerAllAccess = 0x001F0003;
    private const uint Infinite = 0xFFFFFFFF;

    private readonly SafeWaitHandle _timer;

    public HighResolutionWaiter()
    {
        _timer = CreateWaitableTimerExW(IntPtr.Zero, null, CreateWaitableTimerHighResolution, TimerAllAccess);
        if (_timer.IsInvalid)
        {
            throw new InvalidOperationException("Minuterie haute résolution indisponible.");
        }
    }

    /// <summary>Attend la durée indiquée (retour immédiat si nulle ou négative).</summary>
    public void Wait(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        // Valeur négative = délai relatif, en unités de 100 ns.
        var dueTime = -duration.Ticks;
        if (!SetWaitableTimer(_timer, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, false))
        {
            throw new InvalidOperationException("Échec du réglage de la minuterie haute résolution.");
        }

        _ = WaitForSingleObject(_timer, Infinite);
    }

    public void Dispose() => _timer.Dispose();

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial SafeWaitHandle CreateWaitableTimerExW(IntPtr attributes, string? name, uint flags, uint access);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWaitableTimer(
        SafeWaitHandle timer,
        ref long dueTime,
        int period,
        IntPtr completionRoutine,
        IntPtr completionArgument,
        [MarshalAs(UnmanagedType.Bool)] bool resume);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint WaitForSingleObject(SafeWaitHandle handle, uint milliseconds);
}
