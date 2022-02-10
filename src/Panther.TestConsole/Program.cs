﻿// See https://aka.ms/new-console-template for more information
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
    Console.WriteLine($"Status: {musicPlayer.Status} ");

    musicPlayer.StatusChanged += (s, e) =>
    {
        Console.SetCursorPosition(position.Left, position.Top);
        Console.WriteLine($"Status: {e.CurrentStatus} ");
    };

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
    Bass.Free();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
