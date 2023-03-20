// See https://aka.ms/new-console-template for more information
using Panther.Core.Enums;
using Panther.Core.Models;
using Panther.Core.Services;
using Serilog;
using TagLib;

Console.Title = "Panther Test Console";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .CreateLogger();

try
{
    using var musicPlayer = new MusicPlayer();

    Console.WriteLine("Enter a music path...");
    var path = Console.ReadLine();
    if (string.IsNullOrEmpty(path))
    {
        Log.Warning("No music path was provided");
        return;
    }

    var tracks = new DirectoryInfo(path).EnumerateFiles()
        .Select(x =>
        {
            try { return TagLib.File.Create(x.FullName); }
            catch (UnsupportedFormatException) { return null; }
            catch (Exception ex) { Log.Warning(ex, ex.ToString()); return null; }
        })
        .Where(x => x != null && x.Properties.Duration > TimeSpan.Zero)
        .Select(x => new TrackInfo(x!));
    var playlist = new Playlist("Default", tracks);

    musicPlayer.Load(tracks.First().FileName);
    Log.Information("Loaded: {@Track}",
        new { playlist[0].Title, playlist[0].Composer, playlist[0].Duration });
    Console.WriteLine();
    Console.CursorVisible = false;

    var (left, top) = Console.GetCursorPosition();
    Console.WriteLine($"{musicPlayer.Status,10}  {musicPlayer.ElapsedTime:mm':'ss}/{musicPlayer.TotalTime:mm':'ss}");
    Console.WriteLine("\n[Spacebar] Play/Pause    [ESC] Exit");

    var totalLength = musicPlayer.TrackLength;
    var timer = new Timer(x =>
    {
        Console.SetCursorPosition(left, top);
        Console.WriteLine($"{musicPlayer.Status,10}  {musicPlayer.ElapsedTime:mm':'ss}/{musicPlayer.TotalTime:mm':'ss}");
    }, null, 0, 250);

    musicPlayer.StatusChanged += (s, e) =>
    {
        Console.SetCursorPosition(left, top);
        Console.WriteLine($"{musicPlayer.Status,10}  {musicPlayer.ElapsedTime:mm':'ss}/{musicPlayer.TotalTime:mm':'ss}");
    };

    musicPlayer.Play();

    var key = Console.ReadKey(true).Key;
    while (key != ConsoleKey.Escape)
    {
        if (key != ConsoleKey.Spacebar)
        {
            key = Console.ReadKey(true).Key;
            continue;
        }

        if (musicPlayer.Status == PlayerStatus.Playing)
        {
            musicPlayer.Pause();
        }
        else
        {
            musicPlayer.Play();
        }

        key = Console.ReadKey(true).Key;
    }

    musicPlayer.Stop();
}
catch (Exception ex)
{
    Log.Error(ex, ex.Message);
}
finally
{
    Log.CloseAndFlush();
}
