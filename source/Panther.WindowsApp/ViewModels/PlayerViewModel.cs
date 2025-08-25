using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Panther.Core;
using Panther.Core.Enums;
using Panther.Core.Models;
using System;
using System.Threading.Tasks;

namespace Panther.WindowsApp.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    const double PositionThreshold = 2;

    private readonly IMusicPlayer _musicPlayer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTrackLoaded))]
    private Track? _currentTrack;

    [ObservableProperty]
    private int _volume = 50; // Default volume set to 50%

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private double _totalDuration;

    [ObservableProperty]
    private double _currentPosition;

    [ObservableProperty]
    private bool _isShuffleActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRepeatActive))]
    private RepeatMode _repeatMode;

    public PlayerViewModel(IMusicPlayer musicPlayer)
    {
        _musicPlayer = musicPlayer;
        _musicPlayer.PositionChanged += OnPlayerPositionChanged;
        _musicPlayer.PlaybackStateChanged += OnPlayerStateChanged;
        _musicPlayer.TrackChanged += OnPlayerTrackChanged;
        _musicPlayer.PlaybackEnded += OnPlayerPlaybackEnded;
    }
    public bool IsTrackLoaded => CurrentTrack != null;
    public bool IsRepeatActive => RepeatMode != RepeatMode.None;

    [RelayCommand]
    private async Task LoadTrackAsync(Track track)
    {
        await _musicPlayer.LoadTrackAsync(track.Source);
        CurrentTrack = track;
        CurrentPosition = 0;
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (!IsTrackLoaded)
            return;
        if (IsPlaying)
            _musicPlayer.Pause();
        else
            _musicPlayer.Play();
    }

    [RelayCommand]
    private void StopTrack()
    {
        _musicPlayer.Stop();
    }

    [RelayCommand]
    private void SetVolume(int volume)
    {
        if (volume < 0 || volume > 100)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0 and 100.");
        _musicPlayer.Volume = volume;
        Volume = volume;
    }

    [RelayCommand]
    private void NextTrack()
    {
        if (!IsTrackLoaded) return;
        // Logic to play the next track can be added here
    }

    [RelayCommand]
    private void PreviousTrack()
    {
        if (!IsTrackLoaded) return;
        if (CurrentPosition > PositionThreshold)
        {
            _musicPlayer.Seek(0);
            CurrentPosition = 0;
            return;
        }
        // Logic to play the previous track can be added here
    }

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        _musicPlayer.Volume = IsMuted ? 0 : Volume;
    }

    [RelayCommand]
    private void ToggleRepeat()
    {
        RepeatMode = RepeatMode switch
        {
            RepeatMode.None => RepeatMode.All,
            RepeatMode.All => RepeatMode.Single,
            RepeatMode.Single => RepeatMode.None,
            _ => RepeatMode.None
        };
    }

    private void OnPlayerPositionChanged(object? sender, double position)
    {
        CurrentPosition = position;
    }

    private void OnPlayerStateChanged(object? sender, PlaybackState state)
    {
        IsPlaying = state == PlaybackState.Playing;
        if (state == PlaybackState.Stopped)
            CurrentPosition = 0;
    }

    private void OnPlayerTrackChanged(object? sender, Track? track)
    {
        CurrentTrack = track;
        TotalDuration = track?.Duration ?? 0;
        CurrentPosition = 0;
    }

    private void OnPlayerPlaybackEnded(object? sender, EventArgs e)
    {
        switch (RepeatMode, IsShuffleActive)
        {
            case (RepeatMode.Single, _):
                _musicPlayer.Play();
                break;
            case (RepeatMode.All, false):
                // Logic to play the next track in the playlist can be added here
                break;
            case (_, true):
                // Logic to play a random track can be added here
                break;
            default:
                _musicPlayer.Stop();
                break;
        }
    }
}
