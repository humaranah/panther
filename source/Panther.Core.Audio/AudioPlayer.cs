using Panther.Core.Audio.Abstractions;
using Panther.Core.Audio.Enums;

namespace Panther.Core.Audio;

/// <summary>
/// Default <see cref="IAudioPlayer"/> implementation: decodes a track and feeds the raw decoded
/// frames straight to a playback device at the track's own native format. If the device's
/// negotiated format ends up different (e.g. shared mode locked to the OS mixer's rate/channel
/// count), miniaudio's own device-level converter bridges that transparently - this class does not
/// resample or channel-mix on top of that, since doing so would double-convert the signal. See
/// <see cref="EnsureDeviceForTrack"/> for how <see cref="PlaybackSettings.ExclusiveMode"/> is
/// resolved per track, and <see cref="PlaybackSettings.AllowResampling"/> for how a shared-mode
/// rate mismatch can be refused instead of silently accepted.
///
/// Threading: <see cref="PlayAsync"/>/<see cref="Pause"/>/<see cref="Resume"/>/<see cref="Stop"/>/
/// <see cref="Dispose"/> are serialized against each other via <c>_playGate</c> and are safe to call
/// from a UI thread. The device's data callback runs on miniaudio's own real-time audio thread and
/// never blocks on that gate - it only ever touches the current <c>_session</c> (published via
/// Interlocked/Volatile) and a short-lived <c>_decoderLock</c> shared with <see cref="Seek"/>,
/// since the underlying decoder is documented as not safe for concurrent read+seek.
/// </summary>
public sealed class AudioPlayer : IAudioPlayer
{
    private static readonly TimeSpan PositionTickInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>Length of the fade applied when starting, resuming, seeking, pausing or stopping,
    /// to avoid an audible click from an abrupt discontinuity in the waveform.</summary>
    private const double FadeSeconds = 0.015;

    /// <summary>Upper bound on how long <see cref="Pause"/>/<see cref="Stop"/> will block waiting
    /// for a fade-out to finish before giving up and stopping the device anyway. Comfortably above
    /// <see cref="FadeSeconds"/> to absorb scheduling jitter, while staying imperceptible to a user
    /// clicking a button.</summary>
    private static readonly TimeSpan FadeOutSafetyTimeout = TimeSpan.FromMilliseconds(200);

    /// <summary>Volume floor in decibels: how quiet <see cref="Volume"/> = 0 would be if it weren't
    /// special-cased to exact silence. Standard "audio taper" so the UI slider position matches
    /// perceived loudness (which is roughly logarithmic) instead of raw linear amplitude.</summary>
    private const double MinVolumeDecibels = -60.0;

    private readonly IAudioDecoderFactory _decoderFactory;
    private readonly IAudioDeviceFactory _deviceFactory;
    private readonly PlaybackSettings _settings;
    private readonly SemaphoreSlim _playGate = new(1, 1);
    private readonly object _decoderLock = new();

    private IAudioDevice? _device;
    private PlaybackSession? _session;
    private Timer? _positionTimer;
    private double _volumeSetting = 1.0;
    private volatile float _gain = 1f;
    private bool _disposed;

    public AudioPlayer(IAudioDecoderFactory decoderFactory, IAudioDeviceFactory deviceFactory, PlaybackSettings settings)
    {
        _decoderFactory = decoderFactory ?? throw new ArgumentNullException(nameof(decoderFactory));
        _deviceFactory = deviceFactory ?? throw new ArgumentNullException(nameof(deviceFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;

    public string? CurrentFilePath { get; private set; }

    public TimeSpan Position
    {
        get
        {
            var session = Volatile.Read(ref _session);
            if (session is null)
            {
                return TimeSpan.Zero;
            }

            long frame;
            lock (_decoderLock)
            {
                frame = session.Decoder.CurrentFrame;
            }

            return TimeSpan.FromSeconds((double)frame / session.SourceFormat.SampleRate);
        }
    }

    public TimeSpan? Duration
    {
        get
        {
            var session = Volatile.Read(ref _session);
            if (session is null || session.Decoder.TotalFrames <= 0)
            {
                return null;
            }

            return TimeSpan.FromSeconds((double)session.Decoder.TotalFrames / session.SourceFormat.SampleRate);
        }
    }

    /// <summary>
    /// UI-facing volume in [0.0, 1.0]. Mapped to an amplitude gain along a decibel taper (see
    /// <see cref="MinVolumeDecibels"/>) rather than applied as a raw linear multiplier, since
    /// perceived loudness is roughly logarithmic - a linear slider would spend most of its travel
    /// barely changing the perceived volume.
    /// </summary>
    public double Volume
    {
        get => _volumeSetting;
        set
        {
            _volumeSetting = Math.Clamp(value, 0.0, 1.0);
            _gain = VolumeToGain(_volumeSetting);
        }
    }

    public event EventHandler<PlaybackState>? StateChanged;
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler? PlaybackCompleted;
    public event EventHandler<Exception>? PlaybackError;
    public event EventHandler? DeviceDisconnected;

    public async Task PlayAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A file path is required.", nameof(filePath));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        await _playGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SetState(PlaybackState.Opening);

            PlaybackSession newSession;
            try
            {
                newSession = await Task.Run(() => OpenSession(filePath), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                SetState(Volatile.Read(ref _session) is null ? PlaybackState.Stopped : PlaybackState.Playing);
                throw;
            }

            StopAndClearSessionLocked();

            try
            {
                EnsureDeviceForTrack(newSession.SourceFormat);

                var deviceFormat = _device!.NegotiatedFormat!.Value;

                if (newSession.SourceFormat.SampleRate != deviceFormat.SampleRate && !_settings.AllowResampling)
                {
                    throw new InvalidOperationException(
                        $"'{filePath}' is {newSession.SourceFormat.SampleRate} Hz but the device is running at " +
                        $"{deviceFormat.SampleRate} Hz, and PlaybackSettings.AllowResampling is false. " +
                        "(Exclusive mode never resamples; this only applies to shared mode.)");
                }
            }
            catch
            {
                newSession.Dispose();
                CurrentFilePath = null;
                SetState(PlaybackState.Stopped);
                throw;
            }

            CurrentFilePath = filePath;
            newSession.RequestFade(FadeDirection.In, FadeFrameCount(newSession.SourceFormat.SampleRate));
            Volatile.Write(ref _session, newSession);
            _device!.Start();
            StartPositionTimer();
            SetState(PlaybackState.Playing);
        }
        finally
        {
            _playGate.Release();
        }
    }

    public void Pause()
    {
        _playGate.Wait();
        try
        {
            if (State != PlaybackState.Playing)
            {
                return;
            }

            FadeOutAndStopDeviceLocked();
            StopPositionTimer();
            SetState(PlaybackState.Paused);
        }
        finally
        {
            _playGate.Release();
        }
    }

    public void Resume()
    {
        _playGate.Wait();
        try
        {
            var session = Volatile.Read(ref _session);
            if (State != PlaybackState.Paused || session is null)
            {
                return;
            }

            session.RequestFade(FadeDirection.In, FadeFrameCount(session.SourceFormat.SampleRate));
            _device?.Start();
            StartPositionTimer();
            SetState(PlaybackState.Playing);
        }
        finally
        {
            _playGate.Release();
        }
    }

    public void Stop()
    {
        _playGate.Wait();
        try
        {
            StopAndClearSessionLocked();
            CurrentFilePath = null;
            SetState(PlaybackState.Stopped);
        }
        finally
        {
            _playGate.Release();
        }
    }

    public void Seek(TimeSpan position)
    {
        var session = Volatile.Read(ref _session)
            ?? throw new InvalidOperationException("No track is loaded.");

        if (!session.Decoder.SupportsSeek)
        {
            throw new NotSupportedException("This track does not support seeking.");
        }

        var frame = (long)(position.TotalSeconds * session.SourceFormat.SampleRate);
        frame = Math.Clamp(frame, 0, session.Decoder.TotalFrames);

        lock (_decoderLock)
        {
            session.Decoder.Seek(frame);
        }

        if (State == PlaybackState.Playing)
        {
            // Smooths the discontinuity at the seek point; no audio thread is running to hear a
            // click while paused, so Resume() (which requests its own fade-in) already covers it.
            session.RequestFade(FadeDirection.In, FadeFrameCount(session.SourceFormat.SampleRate));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _playGate.Wait();
        try
        {
            _disposed = true;
            StopAndClearSessionLocked();

            if (_device is not null)
            {
                _device.RequestData -= OnRequestData;
                _device.Disconnected -= OnDeviceDisconnected;
                _device.Dispose();
                _device = null;
            }
        }
        finally
        {
            _playGate.Release();
        }

        _positionTimer?.Dispose();
        _playGate.Dispose();
    }

    /// <summary>
    /// Resolves <see cref="PlaybackSettings.ExclusiveModePreference"/> for the given track and
    /// (re)configures <c>_device</c> accordingly:
    /// - Never: reuse an already-configured device as-is. Shared mode's negotiated rate is fixed
    ///   by the OS mixer, so it doesn't need to track each track's native format.
    /// - Always/PreferExclusive: always (re)open a fresh device targeting this track's exact
    ///   format, since a successfully-configured MiniAudioDevice can't be reconfigured in place.
    ///   PreferExclusive falls back to shared mode on failure; Always propagates the failure.
    /// </summary>
    private void EnsureDeviceForTrack(AudioFormat sourceFormat)
    {
        var preference = _settings.ExclusiveMode;

        if (preference != ExclusiveModePreference.Never)
        {
            RecreateDevice();
            try
            {
                _device!.Configure(sourceFormat, exclusiveMode: true);
                return;
            }
            catch (InvalidOperationException) when (preference == ExclusiveModePreference.PreferExclusive)
            {
                // The device already self-cleaned on failure, so it can be reconfigured directly.
                _device!.Configure(sourceFormat, exclusiveMode: false);
                return;
            }
        }

        if (_device is null)
        {
            RecreateDevice();
        }

        if (_device!.NegotiatedFormat is null)
        {
            _device.Configure(sourceFormat, exclusiveMode: false);
        }
    }

    private void RecreateDevice()
    {
        if (_device is not null)
        {
            _device.RequestData -= OnRequestData;
            _device.Disconnected -= OnDeviceDisconnected;
            _device.Dispose();
        }

        _device = _deviceFactory.CreateDevice();
        _device.RequestData += OnRequestData;
        _device.Disconnected += OnDeviceDisconnected;
    }

    /// <summary>Runs on a ThreadPool thread (see <see cref="MiniAudioDevice"/>'s notification
    /// callback), not the audio thread - safe to acquire <c>_playGate</c> here.</summary>
    private void OnDeviceDisconnected(object? sender, EventArgs e)
    {
        _playGate.Wait();
        try
        {
            if (State is PlaybackState.Stopped || !ReferenceEquals(sender, _device))
            {
                return; // already stopped, or this is a stale event from a device we've since replaced.
            }

            // The device is already gone - fading out to it would just wait out the safety
            // timeout for nothing, so skip straight to a hard stop.
            StopAndClearSessionLocked(fadeOut: false);
            CurrentFilePath = null;
            SetState(PlaybackState.Stopped);
        }
        finally
        {
            _playGate.Release();
        }

        DeviceDisconnected?.Invoke(this, EventArgs.Empty);
    }

    private PlaybackSession OpenSession(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var decoder = _decoderFactory.CreateFor(stream, Path.GetFileName(filePath));
        try
        {
            var format = decoder.Probe(stream);
            decoder.Open(stream, format);
            return new PlaybackSession(decoder, format);
        }
        catch
        {
            decoder.Dispose();
            throw;
        }
    }

    private void StopAndClearSessionLocked(bool fadeOut = true)
    {
        if (fadeOut)
        {
            FadeOutAndStopDeviceLocked();
        }
        else
        {
            _device?.Stop();
        }

        StopPositionTimer();
        Interlocked.Exchange(ref _session, null)?.Dispose();
    }

    /// <summary>
    /// Ramps the current session's gain down to silence before calling <see cref="IAudioDevice.Stop"/>,
    /// so playback doesn't cut off mid-waveform. Only called from within <c>_playGate</c>, so it's
    /// safe to block briefly here - <see cref="PlaybackSession.FadeOutCompleted"/> is signaled from
    /// the audio thread once the ramp reaches zero, bounded by <see cref="FadeOutSafetyTimeout"/> in
    /// case nothing is currently pulling frames (e.g. the device errored) to advance the fade.
    /// </summary>
    private void FadeOutAndStopDeviceLocked()
    {
        var session = Volatile.Read(ref _session);
        if (session is not null && State == PlaybackState.Playing)
        {
            session.RequestFade(FadeDirection.Out, FadeFrameCount(session.SourceFormat.SampleRate));
            session.FadeOutCompleted.Wait(FadeOutSafetyTimeout);
        }

        _device?.Stop();
    }

    private void SetState(PlaybackState newState)
    {
        if (State == newState)
        {
            return;
        }

        State = newState;
        StateChanged?.Invoke(this, newState);
    }

    private void StartPositionTimer()
    {
        _positionTimer ??= new Timer(_ => PositionChanged?.Invoke(this, Position));
        _positionTimer.Change(PositionTickInterval, PositionTickInterval);
    }

    private void StopPositionTimer()
    {
        _positionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>Runs on miniaudio's real-time audio thread. Must never block indefinitely.</summary>
    private void OnRequestData(Memory<float> buffer, int frameCount)
    {
        var session = Volatile.Read(ref _session);
        var span = buffer.Span;

        if (session is null)
        {
            span.Clear();
            return;
        }

        var channels = session.SourceFormat.Channels;
        int framesProduced;

        try
        {
            lock (_decoderLock)
            {
                framesProduced = session.Decoder.ReadFrames(span, frameCount);
            }
        }
        catch (Exception ex)
        {
            span.Clear();
            HandlePlaybackFailure(session, ex);
            return;
        }

        var validSampleCount = framesProduced * channels;
        session.ApplyGain(span, framesProduced, channels, _gain);

        if (framesProduced < frameCount)
        {
            span[validSampleCount..].Clear();
        }

        if (framesProduced == 0)
        {
            HandleTrackEnded(session);
        }
    }

    private static int FadeFrameCount(int sampleRate) => Math.Max(1, (int)(sampleRate * FadeSeconds));

    private static float VolumeToGain(double volume)
    {
        if (volume <= 0.0)
        {
            return 0f;
        }

        var decibels = MinVolumeDecibels * (1.0 - volume);
        return (float)Math.Pow(10.0, decibels / 20.0);
    }

    private enum FadeDirection
    {
        In = 1,
        Out = -1,
    }

    private void HandleTrackEnded(PlaybackSession session)
    {
        if (Interlocked.CompareExchange(ref _session, null, session) != session)
        {
            return;
        }

        ThreadPool.QueueUserWorkItem(_ =>
        {
            _playGate.Wait();
            try
            {
                session.Dispose();

                if (Volatile.Read(ref _session) is not null)
                {
                    return; // a new track already started before we got here; stale, drop it.
                }

                _device?.Stop();
                StopPositionTimer();
                CurrentFilePath = null;
                SetState(PlaybackState.Stopped);
            }
            finally
            {
                _playGate.Release();
            }

            PlaybackCompleted?.Invoke(this, EventArgs.Empty);
        });
    }

    private void HandlePlaybackFailure(PlaybackSession session, Exception ex)
    {
        if (Interlocked.CompareExchange(ref _session, null, session) != session)
        {
            return;
        }

        ThreadPool.QueueUserWorkItem(_ =>
        {
            _playGate.Wait();
            try
            {
                session.Dispose();

                if (Volatile.Read(ref _session) is not null)
                {
                    return;
                }

                _device?.Stop();
                StopPositionTimer();
                CurrentFilePath = null;
                SetState(PlaybackState.Stopped);
            }
            finally
            {
                _playGate.Release();
            }

            PlaybackError?.Invoke(this, ex);
        });
    }

    private sealed class PlaybackSession(IAudioDecoder decoder, AudioFormat sourceFormat)
    {
        public IAudioDecoder Decoder { get; } = decoder;
        public AudioFormat SourceFormat { get; } = sourceFormat;

        /// <summary>Signaled by the audio thread once a requested fade-out reaches silence. Waited
        /// on (with a timeout) by control threads via <see cref="FadeOutAndStopDeviceLocked"/>.</summary>
        public ManualResetEventSlim FadeOutCompleted { get; } = new(initialState: false);

        // Fade envelope. _pendingFadeDirection/_pendingFadeFrameCount are written from control
        // threads to request a new fade (Volatile.Write publishes the frame count written just
        // before it); everything else is only ever touched from the audio thread inside
        // ApplyGain, which runs the ramp and applies it in the same pass as the constant volume
        // gain so there's only one multiply loop over the buffer.
        private int _pendingFadeDirection;
        private int _pendingFadeFrameCount;
        private float _currentGain;
        private int _fadeFramesRemaining;
        private float _fadeStep;

        public void RequestFade(FadeDirection direction, int frameCount)
        {
            _pendingFadeFrameCount = frameCount;
            if (direction == FadeDirection.Out)
            {
                FadeOutCompleted.Reset();
            }

            Volatile.Write(ref _pendingFadeDirection, (int)direction);
        }

        /// <summary>Audio-thread only. Applies the fade envelope and constant volume gain to the
        /// first <paramref name="frameCount"/> frames of <paramref name="audio"/> in place.</summary>
        public void ApplyGain(Span<float> audio, int frameCount, int channels, float volumeGain)
        {
            var direction = Interlocked.Exchange(ref _pendingFadeDirection, 0);
            if (direction != 0)
            {
                _fadeFramesRemaining = Math.Max(_pendingFadeFrameCount, 1);

                // Fading in always restarts from silence (rather than ramping from whatever the
                // current gain happens to be) so it also works as a de-click "duck" right after a
                // seek, where playback doesn't actually pass through silence first.
                if (direction > 0)
                {
                    _currentGain = 0f;
                    _fadeStep = 1f / _fadeFramesRemaining;
                }
                else
                {
                    _fadeStep = -_currentGain / _fadeFramesRemaining;
                }
            }

            var rampFrames = Math.Min(_fadeFramesRemaining, frameCount);
            for (var frame = 0; frame < rampFrames; frame++)
            {
                _currentGain = Math.Clamp(_currentGain + _fadeStep, 0f, 1f);
                var sampleGain = _currentGain * volumeGain;
                for (var ch = 0; ch < channels; ch++)
                {
                    audio[(frame * channels) + ch] *= sampleGain;
                }
            }

            _fadeFramesRemaining -= rampFrames;
            if (rampFrames > 0 && _fadeFramesRemaining == 0 && _currentGain <= 0f)
            {
                FadeOutCompleted.Set();
            }

            if (volumeGain != 1f || _currentGain != 1f)
            {
                var steadyGain = _currentGain * volumeGain;
                for (var i = rampFrames * channels; i < frameCount * channels; i++)
                {
                    audio[i] *= steadyGain;
                }
            }
        }

        public void Dispose()
        {
            Decoder.Dispose();
            FadeOutCompleted.Dispose();
        }
    }
}
