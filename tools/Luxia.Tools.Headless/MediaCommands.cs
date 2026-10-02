using System.Globalization;
using System.Text;
using Luxia.Media;
using Luxia.Media.Windows;

namespace Luxia.Tools.Headless;

/// <summary>
/// Commande <c>media</c> : la sonde de la lecture en cours (preuve de concept PoC-3, doc 21 §2.3). Affiche ce que Windows sait des
/// sessions média (Deezer, YouTube Music, VLC…) et ce que LuXia en retient : session suivie, changement de morceau stabilisé,
/// position estimée. Ne touche à aucun matériel.
/// </summary>
internal static class MediaCommands
{
    public static int Probe(Arguments args)
    {
        var seconds = args.GetDouble("duree", 120);
        var path = args.Get("rapport");
        var report = new StringBuilder();

        void Say(string text)
        {
            var line = string.Create(CultureInfo.InvariantCulture, $"{DateTime.Now:HH:mm:ss}  {text}");
            Console.WriteLine(line);
            report.AppendLine(line);
        }

        Console.WriteLine("Sonde de la lecture en cours (PoC-3). Lancez un titre dans Deezer, YouTube Music ou VLC ; changez de titre, mettez en pause,");
        Console.WriteLine("avancez dans le morceau, changez d'application. Ctrl+C ou fin de la durée pour terminer.");
        Console.WriteLine();
        using var tracker = new NowPlayingTracker(new WindowsMediaSessionSource(new ConsoleLogger()), new ConsoleLogger());
        tracker.TrackChanged += (_, change) => Say(change.Track is { } t
            ? $"► MORCEAU : « {t.Title} » — {t.Artist} · album « {t.Album} » · {t.App} · durée {Format(t.Duration)}"
            : "► PLUS DE MORCEAU (aucune session ne fournit de titre)");
        tracker.PlaybackChanged += (_, playing) => Say(playing ? "► LECTURE démarrée" : "► LECTURE arrêtée ou en pause");
        tracker.Start();

        var last = string.Empty;
        var end = DateTime.UtcNow.AddSeconds(seconds);
        var tick = 0;
        while (DateTime.UtcNow < end)
        {
            Thread.Sleep(1000);
            tick++;
            var sessions = tracker.Sessions;
            var snapshot = string.Join('|', sessions.Select(s => $"{s.Id};{s.Playback};{s.Title};{s.Artist};{s.Album}"));

            // Une ligne par session dès que quelque chose change, puis toutes les 5 s : l'évolution de la position montre la précision du lecteur.
            if (snapshot != last || tick % 5 == 0)
            {
                if (sessions.Count == 0)
                {
                    Say("(aucune session média)");
                }

                foreach (var s in sessions)
                {
                    var followed = s.Id == tracker.SelectedSessionId ? "★" : " ";
                    var position = s.Position is { } p ? Format(p) : "—";
                    var age = s.PositionUpdatedUtc is { } u ? $"relevée il y a {(DateTimeOffset.UtcNow - u).TotalSeconds:0.0} s" : "pas de date";
                    Say($"{followed} {s.App,-12} {s.Playback,-8} « {s.Title} » — {s.Artist} · album « {s.Album} » · position {position} ({age}) · durée {Format(s.Duration)} · vitesse {s.Rate:0.##}");
                }

                if (tracker.Current is { Track: not null } now)
                {
                    Say($"    suivi : « {now.Track.Title} » · position estimée {(now.Position is { } e ? Format(e) : "—")} · {(now.Playing ? "en lecture" : "à l'arrêt")}");
                }

                last = snapshot;
            }
        }

        if (path is not null)
        {
            File.WriteAllText(path, report.ToString(), Encoding.UTF8);
            Console.WriteLine($"Rapport écrit : {Path.GetFullPath(path)}");
        }

        return 0;
    }

    private static string Format(TimeSpan span) => span.TotalHours >= 1 ? span.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture) : span.ToString(@"m\:ss", CultureInfo.InvariantCulture);
}
