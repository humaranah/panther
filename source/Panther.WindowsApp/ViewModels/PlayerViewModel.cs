using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Panther.Core;
using Panther.Core.Enums;
using Panther.Core.Models;
using Panther.WindowsApp.Converters;
using Panther.WindowsApp.Services;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Panther.WindowsApp.ViewModels;

public sealed partial class PlayerViewModel : ObservableObject, IDisposable
{
    private readonly IMusicPlayer _musicPlayer;
    private readonly IPlaybackPositionTracker _playbackTracker;
    private readonly IPlayerQueueService _queueService;
    private readonly ITrackInfoProvider _trackProvider;
    private readonly IFilePickerService _filePickerService;
    private readonly DispatcherQueue _dispatcherQueue;

    private const double DefaultPreviousTrackThreshold = 2;

    public PlayerViewModel(
        IMusicPlayer musicPlayer,
        IPlaybackPositionTracker playbackTimer,
        IPlayerQueueService queueService,
        ITrackInfoProvider trackProvider,
        IFilePickerService filePickerService)
    {
        _musicPlayer = musicPlayer;
        _musicPlayer.PropertyChanged += OnPlayerPropertyChanged;
        _musicPlayer.PlaybackEnded += OnPlayerPlaybackEnded;
        _playbackTracker = playbackTimer;
        _playbackTracker.PositionUpdated += OnPlaybackPositionUpdated;
        _queueService = queueService;
        _trackProvider = trackProvider;
        _filePickerService = filePickerService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        VolumePercent = 100;
    }

    #region Observable properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTrackLoaded),
        nameof(TrackTitle), nameof(TrackArtist), nameof(IsPlaying))]
    private TrackInfo? _currentTrack;

    [ObservableProperty]
    private int _volumePercent;

    partial void OnVolumePercentChanged(int value)
    {
        _musicPlayer.Volume = (float)_volumePercent / 100f;
        IsMuted = false;
    }

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isMuted;

    partial void OnIsMutedChanged(bool value)
    {
        _musicPlayer.Volume = value ? 0f : VolumePercent / 100f;
    }

    [ObservableProperty]
    private double _durationInSeconds;

    [ObservableProperty]
    private double _positionInSeconds;

    [ObservableProperty]
    private bool _isSeeking;

    [ObservableProperty]
    private bool _isShuffleActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRepeatActive))]
    private RepeatMode _repeatMode;

    [ObservableProperty]
    private ImageSource? _albumArt;
    #endregion

    #region Calculated properties
    public IPlaybackPositionTracker PlaybackTracker => _playbackTracker;

    public bool IsTrackLoaded => _musicPlayer.HasTrackLoaded && CurrentTrack != null;

    public string TrackTitle => CurrentTrack?.Title ?? string.Empty;

    public string TrackArtist => CurrentTrack?.Artists.Length switch
    {
        null or 0 => string.Empty,
        1 => CurrentTrack.Artists[0],
        _ => string.Join(", ", CurrentTrack.Artists)
    };

    public bool IsRepeatActive => RepeatMode != RepeatMode.None;
    #endregion

    #region Commands
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
            await LoadTrackInternalAsync(file.Path, token);
        }
        if (IsPlaying)
            PauseInternal();
        else
            PlayInternal();
    }

    [RelayCommand]
    private void StopTrack()
    {
        _musicPlayer.Stop();
        _playbackTracker.Stop();
        SetPosition(0);
    }

    [RelayCommand]
    private async Task NextTrack(CancellationToken cancellationToken)
    {
        if (_queueService.IsEmpty) return;
        var next = _queueService.GetNext();
        if (next == null) return;
        await LoadTrackInternalAsync(next.Source, cancellationToken);
    }

    [RelayCommand]
    private async Task PreviousTrack(CancellationToken cancellationToken)
    {
        if (IsTrackLoaded && PositionInSeconds > DefaultPreviousTrackThreshold)
        {
            _musicPlayer.SeekTo(0);
            return;
        }
        if (_queueService.IsEmpty) return;
        var previous = _queueService.GetPrevious();
        if (previous == null) return;
        await LoadTrackInternalAsync(previous.Source, cancellationToken);
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

    [RelayCommand]
    private void SeekTo(double seconds)
    {
        _musicPlayer?.SeekTo(seconds);
        PositionInSeconds = seconds;
    }
    #endregion

    #region Event handlers
    private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Action? updateAction = e.PropertyName switch
        {
            nameof(IMusicPlayer.PlaybackState) => () => IsPlaying = _musicPlayer.PlaybackState == PlaybackState.Playing,
            nameof(IMusicPlayer.DurationInSeconds) => () => DurationInSeconds = _musicPlayer.DurationInSeconds,
            _ => null
        };
        if (updateAction == null) return;
        if (_dispatcherQueue.HasThreadAccess)
            updateAction.Invoke();
        else
            _ = _dispatcherQueue.TryEnqueue(() => updateAction());
    }

    private void OnPlaybackPositionUpdated(double seconds)
    {
        if (_dispatcherQueue.HasThreadAccess)
            PositionInSeconds = seconds;
        else
            _ = _dispatcherQueue?.TryEnqueue(() => PositionInSeconds = seconds);
    }

    private void OnPlayerPlaybackEnded(object? sender, EventArgs e)
    {
        StopTrack();
        if (_queueService.IsEmpty && RepeatMode == RepeatMode.None) return;
        if (RepeatMode == RepeatMode.Single)
        {
            PlayInternal();
            return;
        }




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
        }
    }
    #endregion

    private void PlayInternal()
    {
        if (!IsTrackLoaded) return;
        _musicPlayer.Play();
        _playbackTracker.Start();
    }

    private void PauseInternal()
    {
        if (!IsTrackLoaded) return;
        _musicPlayer.Pause();
        _playbackTracker.Stop();
    }

    private async Task<bool> LoadTrackInternalAsync(string source, CancellationToken token)
    {
        var trackLoaded = await _musicPlayer.LoadTrackAsync(source, token);
        if (!trackLoaded) return false;
        CurrentTrack = _trackProvider.LoadFrom(source);
        if (CurrentTrack == null) return false;
        PositionInSeconds = 0;
        AlbumArt = await BitmapConverters.ConvertBytesToImageSource(CurrentTrack.AlbumArt);
        return true;
    }

    private void SetPosition(double seconds)
    {
        if (_dispatcherQueue.HasThreadAccess)
            PositionInSeconds = seconds;
        else
            _ = _dispatcherQueue.TryEnqueue(() => PositionInSeconds = seconds);
    }

    public void Dispose()
    {
        _musicPlayer.Stop();
        _musicPlayer.PropertyChanged -= OnPlayerPropertyChanged;
        _musicPlayer.PlaybackEnded -= OnPlayerPlaybackEnded;
        GC.SuppressFinalize(this);
    }
}
