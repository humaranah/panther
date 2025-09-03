using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Panther.Core;
using Panther.Core.Enums;
using Panther.Core.Models;
using Panther.WindowsApp.Converters;
using Panther.WindowsApp.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Panther.WindowsApp.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    private const double DefaultPositionThreshold = 2;

    private readonly IMusicPlayer _musicPlayer;
    private readonly ITrackInfoProvider _trackProvider;
    private readonly IFilePickerService _filePickerService;

    public PlayerViewModel(
        IMusicPlayer musicPlayer,
        ITrackInfoProvider trackProvider,
        IFilePickerService filePickerService)
    {
        _musicPlayer = musicPlayer;
        _musicPlayer.PositionChanged += OnPlayerPositionChanged;
        _musicPlayer.PlaybackStateChanged += OnPlayerStateChanged;
        _musicPlayer.TrackChanged += OnPlayerTrackChanged;
        _musicPlayer.PlaybackEnded += OnPlayerPlaybackEnded;
        _trackProvider = trackProvider;
        _filePickerService = filePickerService;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTrackLoaded), nameof(TrackTitle), nameof(TrackArtist))]
    private TrackInfo? _currentTrack;

    [ObservableProperty]
    private int _volumePercent = 50;

    partial void OnVolumePercentChanged(int value)
    {
        _musicPlayer.Volume = _volumePercent / 100f;
        IsMuted = false;
    }

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

    [ObservableProperty]
    private ImageSource? _albumArt;

    public bool IsTrackLoaded => _musicPlayer.HasTrackLoaded && CurrentTrack != null;

    public string TrackTitle => CurrentTrack?.Title ?? string.Empty;

    public string TrackArtist => CurrentTrack?.Artists.Length switch
    {
        null or 0 => string.Empty,
        1 => CurrentTrack.Artists[0],
        _ => string.Join(", ", CurrentTrack.Artists)
    };

    public bool IsRepeatActive => RepeatMode != RepeatMode.None;

    [RelayCommand]
    private async Task LoadTrackAsync(string source, CancellationToken token)
    {
        await LoadTrackInternalAsync(source, token);
    }

    [RelayCommand]
    private async Task PlayPause(CancellationToken token)
    {
        if (!IsTrackLoaded)
        {
            var file = await _filePickerService.PickSingleFileAsync();
            if (file == null) return;
            var trackLoaded = await LoadTrackInternalAsync(file.Path, token);
            if (!trackLoaded) return;
        }
        if (IsPlaying)
            _musicPlayer.Pause();
        else
            _musicPlayer.Play();
        IsPlaying = !IsPlaying;
    }

    [RelayCommand]
    private void StopTrack()
    {
        _musicPlayer.Stop();
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
        if (CurrentPosition > DefaultPositionThreshold)
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
        _musicPlayer.Volume = IsMuted ? 0f : VolumePercent / 100f;
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

    private async Task<bool> LoadTrackInternalAsync(string source, CancellationToken token)
    {
        var trackLoaded = await _musicPlayer.LoadTrackAsync(source, token);
        if (!trackLoaded) return false;
        CurrentTrack = _trackProvider.LoadFrom(source);
        if (CurrentTrack == null) return false;
        CurrentPosition = 0;
        AlbumArt = await BitmapConverters.ConvertBytesToImageSource(CurrentTrack.AlbumArt);
        return true;
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

    private void OnPlayerTrackChanged(object? sender, TrackChange change)
    {
        // Load track here
        TotalDuration = _musicPlayer.GetDurationInSeconds();
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
