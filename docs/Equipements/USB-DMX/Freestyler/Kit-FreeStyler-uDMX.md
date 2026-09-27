# Kit : piloter la clé « FreeStyler USB to DMX512 » (LIXADA HK-WDMX03C) en C#

Extrait d'un projet précédent où cette clé **fonctionne** (testé sur un vrai projecteur, sept. 2026).

## 1. Ce qu'est vraiment la clé

- Malgré le nom « FreeStyler », ce n'est **pas** une FTDI / Enttec Open DMX, et ce n'est **pas** un port COM.
- Windows la détecte comme une **Anyma uDMX** : **VID 0x16C0 / PID 0x05DC**, nom « uDMX ».
- Le firmware (obdev/Anyma) **régénère lui-même le signal DMX en continu (~44 Hz)**.
  Le PC lui envoie simplement les valeurs des canaux par un **transfert de contrôle USB vendeur**.

## 2. Pilote Windows (une seule fois par machine)

1. Télécharger **Zadig** (https://zadig.akeo.ie).
2. Brancher la clé → Options → *List All Devices* → choisir **« uDMX »** (vérifier USB ID `16C0 05DC`).
3. Pilote cible : **WinUSB** → *Install Driver* (ou *Replace Driver*).

Sans ça, libusb ne voit pas la clé (`Find` renvoie null).

## 3. Paquets NuGet (.NET 8)

```xml
<PackageReference Include="LibUsbDotNet" Version="3.0.224" />
<PackageReference Include="UsbDotNet.LibUsbNative" Version="1.4.1" />  <!-- fournit libusb-1.0.dll natif -->
```

## 4. Protocole (tout ce qu'il faut savoir)

| Champ setup USB | Valeur | Sens |
|---|---|---|
| bmRequestType | `0x40` | vendeur, hôte → périphérique, destinataire = device |
| bRequest | `2` | `SetChannelRange` |
| wValue | `n` | nombre de canaux envoyés |
| wIndex | `0` | canal de départ (0 = canal DMX 1) |
| data | `n` octets | valeurs des canaux 1..n |

(Il existe aussi `bRequest = 1` = SetSingleChannel, inutile ici.)

## 5. Pièges rencontrés (et corrigés)

- **Ne pas pousser 512 octets à chaque trame** : le firmware uDMX (USB logiciel sur AVR) sature → saccades.
  → N'envoyer que jusqu'au **dernier canal non nul** (minimum 24).
- **N'envoyer que si ça a changé**, + un **keepalive toutes les 1 s** même sans changement.
- Cadence d'envoi ~**30 Hz** (max 44).
- Envoi dans un **thread dédié** ; l'appli ne fait que déposer un instantané des 512 canaux (sous verrou).
- À la fermeture : envoyer une trame à **0 (blackout)** avant de libérer.
- Après ~20 erreurs USB consécutives → considérer la clé débranchée et lever un événement.

## 6. Code prêt à l'emploi (autonome)

```csharp
using System.Diagnostics;
using System.IO;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

/// <summary>
/// Sortie DMX via une interface USB compatible Anyma uDMX (VID 0x16C0 / PID 0x05DC),
/// c.-à-d. la clé « FreeStyler / LIXADA HK-WDMX03C ». Pilote WinUSB requis (Zadig).
/// Usage : var dmx = new UdmxOutput(); dmx.Open(); dmx.Submit(canaux512); ... dmx.Dispose();
/// </summary>
public sealed class UdmxOutput : IDisposable
{
    public const int ChannelCount = 512;
    private const int VendorId = 0x16C0;
    private const int ProductId = 0x05DC;
    private const byte RequestType = 0x40;      // vendeur, host→device
    private const byte CmdSetChannelRange = 2;
    private const int MinChannels = 24;
    private const int MaxConsecutiveErrors = 20;

    private readonly byte[] _snapshot = new byte[ChannelCount];
    private readonly byte[] _lastSent = new byte[ChannelCount];
    private readonly object _gate = new();
    private UsbContext? _context;
    private IUsbDevice? _device;
    private Thread? _pump;
    private volatile bool _running;
    private bool _everSent;
    private TimeSpan _lastSendAt;

    public UdmxOutput(int refreshHz = 30) => RefreshHz = Math.Clamp(refreshHz, 1, 44);

    public int RefreshHz { get; }
    public bool IsOpen => _device is { IsOpen: true };
    public long FramesSent { get; private set; }
    public string? LastError { get; private set; }
    public event Action<Exception>? Faulted;

    /// <summary>Vrai si la clé est branchée ET visible par libusb (pilote WinUSB installé).</summary>
    public static bool IsPresent()
    {
        try
        {
            using var ctx = new UsbContext();
            using var dev = ctx.Find(new UsbDeviceFinder { Vid = VendorId, Pid = ProductId });
            return dev is not null;
        }
        catch { return false; }
    }

    public void Open()
    {
        if (_running) return;
        _context = new UsbContext();
        _device = _context.Find(new UsbDeviceFinder { Vid = VendorId, Pid = ProductId })
                  ?? throw new InvalidOperationException(
                      "Interface uDMX introuvable : clé branchée ? pilote WinUSB installé via Zadig ?");
        _device.Open();
        try { _device.SetConfiguration(1); } catch { /* déjà configurée */ }
        try { _device.ClaimInterface(0); } catch { /* pas toujours d'interface à réclamer */ }

        _running = true;
        _pump = new Thread(Pump) { IsBackground = true, Name = "dmx-udmx-tx" };
        _pump.Start();
    }

    /// <summary>Dépose l'état des canaux (index 0 = canal DMX 1). Thread-safe, non bloquant.</summary>
    public void Submit(ReadOnlySpan<byte> channels)
    {
        lock (_gate)
        {
            Array.Clear(_snapshot);
            channels[..Math.Min(channels.Length, ChannelCount)].CopyTo(_snapshot);
        }
    }

    private void Pump()
    {
        var period = TimeSpan.FromSeconds(1.0 / RefreshHz);
        var clock = Stopwatch.StartNew();
        TimeSpan due = clock.Elapsed;
        var frame = new byte[ChannelCount];
        int consecutiveErrors = 0;

        while (_running)
        {
            try
            {
                lock (_gate) Array.Copy(_snapshot, frame, frame.Length);

                // N'envoyer que jusqu'au dernier canal non nul (le firmware sature à 512 octets/trame).
                int count = MinChannels;
                for (int i = frame.Length - 1; i >= count; i--)
                    if (frame[i] != 0) { count = i + 1; break; }

                bool changed = !_everSent;
                for (int i = 0; i < count && !changed; i++)
                    if (frame[i] != _lastSent[i]) changed = true;

                TimeSpan now = clock.Elapsed;
                bool keepaliveDue = now - _lastSendAt >= TimeSpan.FromSeconds(1);

                if (changed || keepaliveDue)
                {
                    var setup = new UsbSetupPacket(RequestType, CmdSetChannelRange, count, 0, count);
                    int result = _device!.ControlTransfer(setup, frame, 0, count);
                    if (result < 0) throw new IOException($"ControlTransfer uDMX a renvoyé {result}.");
                    Array.Copy(frame, _lastSent, count);
                    _everSent = true;
                    _lastSendAt = now;
                    FramesSent++;
                }
                consecutiveErrors = 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                if (++consecutiveErrors >= MaxConsecutiveErrors)
                {
                    _running = false;
                    Faulted?.Invoke(ex);
                    return;
                }
                Thread.Sleep(100);
                due = clock.Elapsed;
                continue;
            }

            due += period;
            TimeSpan wait = due - clock.Elapsed;
            if (wait > TimeSpan.Zero) Thread.Sleep(wait); else due = clock.Elapsed;
        }
    }

    public void Close()
    {
        _running = false;
        _pump?.Join(1000);
        _pump = null;
        try
        {
            if (_device is { IsOpen: true })   // blackout avant de lâcher la clé
            {
                var zeros = new byte[ChannelCount];
                var setup = new UsbSetupPacket(RequestType, CmdSetChannelRange, ChannelCount, 0, ChannelCount);
                _device.ControlTransfer(setup, zeros, 0, zeros.Length);
            }
        }
        catch { /* on ferme quand même */ }

        try { _device?.ReleaseInterface(0); } catch { }
        _device?.Dispose();
        _device = null;
        _context?.Dispose();
        _context = null;
    }

    public void Dispose() => Close();
}
```

## 7. Test minimal

```csharp
using var dmx = new UdmxOutput();
dmx.Open();
var ch = new byte[512];
ch[0] = 255;              // canal DMX 1 à fond (adapter à l'adresse du projecteur)
dmx.Submit(ch);
Thread.Sleep(3000);       // la clé maintient le signal toute seule
```

## 8. Déploiement

- `dotnet publish` self-contained : vérifier que **`libusb-1.0.dll`** est bien copié à côté de l'exe.
- Sur toute nouvelle machine : refaire l'étape Zadig (le pilote n'est pas embarqué).
