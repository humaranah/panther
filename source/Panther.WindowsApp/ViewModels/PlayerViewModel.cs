using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Panther.Core.Enums;
using Panther.Core.Models;
using System;
using System.Threading.Tasks;

namespace Panther.WindowsApp.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTrackLoaded))]
    private Track? _currentTrack;

    [ObservableProperty]
    private double _volume = 50.0; // Default volume set to 50%

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalDurationInSeconds))]
    private TimeSpan _totalDuration;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPositionInSeconds))]
    private TimeSpan _currentPosition;

    [ObservableProperty]
    private bool _isShuffleActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRepeatActive))]
    private RepeatMode _repeatMode;

    public PlayerViewModel()
    {
        // Initialize properties or load data if necessary
    }
    public bool IsTrackLoaded => CurrentTrack != null;
    public bool IsRepeatActive => RepeatMode != RepeatMode.None;
    public int TotalDurationInSeconds => (int)TotalDuration.TotalSeconds;
    public int CurrentPositionInSeconds
    {
        get => (int)CurrentPosition.TotalSeconds;
        set => CurrentPosition = TimeSpan.FromSeconds(value);
    }

    [RelayCommand]
    private async Task LoadTrackAsync(Track track)
    {
        CurrentTrack = track;
        CurrentPosition = TimeSpan.Zero; // Reset position when loading a new track
    }

    [RelayCommand]
    private void PlayTrack()
    {
        IsPlaying = true;
        // Logic to play the track can be added here
    }

    [RelayCommand]
    private void PauseTrack()
    {
        IsPlaying = false;
        // Logic to pause the track can be added here
    }

    [RelayCommand]
    private void StopTrack()
    {
        IsPlaying = false;
        CurrentPosition = TimeSpan.Zero; // Reset position when stopping the track
        // Logic to stop the track can be added here
    }

    [RelayCommand]
    private void SetVolume(double volume)
    {
        if (volume < 0 || volume > 100)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0 and 100.");

        Volume = volume;
        // Logic to set the volume can be added here
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
        // Logic to play the previous track can be added here
    }

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        // Logic to mute/unmute the player can be added here
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
}
