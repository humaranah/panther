// See https://aka.ms/new-console-template for more information
using ManagedBass;
using Panther.Core.Enums;
using Panther.Core.Services;

Console.Title = "Panther Test Console";

try
{
    var musicPlayer = new MusicPlayer();
    musicPlayer.Load("song.mp3");
    Console.WriteLine($"Loaded: {musicPlayer.FileName}\n");

    musicPlayer.Play();
    var position = Console.GetCursorPosition();
    Console.WriteLine("Status: Playing");

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
            Console.SetCursorPosition(position.Left, position.Top);
            Console.WriteLine("Status: Paused ");
        }
        else
        {
            musicPlayer.Play();
            Console.SetCursorPosition(position.Left, position.Top);
            Console.WriteLine("Status: Playing");
        }

        key = Console.ReadKey(true).Key;
    }

    musicPlayer.Stop();
    Bass.Free();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
