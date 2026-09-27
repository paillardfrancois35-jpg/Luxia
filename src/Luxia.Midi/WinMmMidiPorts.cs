using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Luxia.Midi;

/// <summary>
/// Ports MIDI de Windows par l'API <c>winmm</c> appelée directement (Q31 : aucune dépendance). Les messages reçus arrivent
/// sur un fil du système : ils sont transmis tels quels, sans travail long.
/// </summary>
public sealed partial class WinMmMidiPorts : IMidiPorts
{
    private const uint CallbackFunction = 0x00030000;
    private const uint MimData = 0x3C3;

    private static readonly ConcurrentDictionary<nint, Action<MidiMessage>> Receivers = new();
    private static long _nextKey;

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs()
    {
        var names = new List<string>();
        var count = midiInGetNumDevs();
        for (uint i = 0; i < count; i++)
        {
            var caps = default(MidiInCaps);
            if (midiInGetDevCapsW(i, ref caps, (uint)Marshal.SizeOf<MidiInCaps>()) == 0)
            {
                names.Add(caps.Name);
            }
        }

        return names;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs()
    {
        var names = new List<string>();
        var count = midiOutGetNumDevs();
        for (uint i = 0; i < count; i++)
        {
            var caps = default(MidiOutCaps);
            if (midiOutGetDevCapsW(i, ref caps, (uint)Marshal.SizeOf<MidiOutCaps>()) == 0)
            {
                names.Add(caps.Name);
            }
        }

        return names;
    }

    /// <inheritdoc />
    public unsafe IDisposable OpenInput(string name, Action<MidiMessage> received)
    {
        ArgumentNullException.ThrowIfNull(received);
        var id = IndexOf(Inputs(), name);
        var key = (nint)Interlocked.Increment(ref _nextKey);
        Receivers[key] = received;
        delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nint, void> callback = &OnInput;
        var result = midiInOpen(out var handle, (uint)id, (nint)callback, key, CallbackFunction);
        if (result != 0)
        {
            Receivers.TryRemove(key, out _);
            throw new IOException($"Ouverture de l'entrée MIDI « {name} » impossible (code {result}).");
        }

        if (midiInStart(handle) != 0)
        {
            _ = midiInClose(handle);
            Receivers.TryRemove(key, out _);
            throw new IOException($"Démarrage de l'entrée MIDI « {name} » impossible.");
        }

        return new Input(handle, key);
    }

    /// <inheritdoc />
    public IMidiOutput OpenOutput(string name)
    {
        var id = IndexOf(Outputs(), name);
        var result = midiOutOpen(out var handle, (uint)id, 0, 0, 0);
        return result != 0
            ? throw new IOException($"Ouverture de la sortie MIDI « {name} » impossible (code {result}).")
            : new Output(handle);
    }

    private static int IndexOf(IReadOnlyList<string> names, string name)
    {
        for (var i = 0; i < names.Count; i++)
        {
            if (names[i] == name)
            {
                return i;
            }
        }

        throw new IOException($"Port MIDI « {name} » absent.");
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void OnInput(nint handle, uint message, nint instance, nint param1, nint param2)
    {
        if (message == MimData && Receivers.TryGetValue(instance, out var received))
        {
            try
            {
                received(MidiMessage.Unpack((uint)param1));
            }
            catch (Exception)
            {
                // Jamais d'exception vers le système depuis un rappel natif : le message est perdu, rien de plus.
            }
        }
    }

    [LibraryImport("winmm.dll")]
    private static partial uint midiInGetNumDevs();

    [LibraryImport("winmm.dll")]
    private static partial uint midiInGetDevCapsW(uint deviceId, ref MidiInCaps caps, uint size);

    [LibraryImport("winmm.dll")]
    private static partial uint midiInOpen(out nint handle, uint deviceId, nint callback, nint instance, uint flags);

    [LibraryImport("winmm.dll")]
    private static partial uint midiInStart(nint handle);

    [LibraryImport("winmm.dll")]
    private static partial uint midiInStop(nint handle);

    [LibraryImport("winmm.dll")]
    private static partial uint midiInReset(nint handle);

    [LibraryImport("winmm.dll")]
    private static partial uint midiInClose(nint handle);

    [LibraryImport("winmm.dll")]
    private static partial uint midiOutGetNumDevs();

    [LibraryImport("winmm.dll")]
    private static partial uint midiOutGetDevCapsW(uint deviceId, ref MidiOutCaps caps, uint size);

    [LibraryImport("winmm.dll")]
    private static partial uint midiOutOpen(out nint handle, uint deviceId, nint callback, nint instance, uint flags);

    [LibraryImport("winmm.dll")]
    private static partial uint midiOutShortMsg(nint handle, uint message);

    [LibraryImport("winmm.dll")]
    private static partial uint midiOutReset(nint handle);

    [LibraryImport("winmm.dll")]
    private static partial uint midiOutClose(nint handle);

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct MidiInCaps
    {
        public ushort Manufacturer;
        public ushort Product;
        public uint DriverVersion;
        public fixed char NameBuffer[32];
        public uint Support;

        public readonly string Name
        {
            get
            {
                fixed (char* name = NameBuffer)
                {
                    return new string(name);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct MidiOutCaps
    {
        public ushort Manufacturer;
        public ushort Product;
        public uint DriverVersion;
        public fixed char NameBuffer[32];
        public ushort Technology;
        public ushort Voices;
        public ushort Notes;
        public ushort ChannelMask;
        public uint Support;

        public readonly string Name
        {
            get
            {
                fixed (char* name = NameBuffer)
                {
                    return new string(name);
                }
            }
        }
    }

    private sealed class Input(nint handle, nint key) : IDisposable
    {
        private nint _handle = handle;

        public void Dispose()
        {
            var handle = Interlocked.Exchange(ref _handle, 0);
            if (handle == 0)
            {
                return;
            }

            // Codes de retour ignorés à la fermeture : un port débranché ne se ferme pas mieux en insistant.
            _ = midiInStop(handle);
            _ = midiInReset(handle);
            _ = midiInClose(handle);
            Receivers.TryRemove(key, out _);
        }
    }

    private sealed class Output(nint handle) : IMidiOutput
    {
        private nint _handle = handle;

        public void Send(MidiMessage message)
        {
            var handle = _handle;
            if (handle != 0 && midiOutShortMsg(handle, message.Pack()) != 0)
            {
                throw new IOException("Envoi MIDI impossible (contrôleur débranché ?).");
            }
        }

        public void Dispose()
        {
            var handle = Interlocked.Exchange(ref _handle, 0);
            if (handle != 0)
            {
                _ = midiOutReset(handle);
                _ = midiOutClose(handle);
            }
        }
    }
}
