using Panther.Infrastructure.BassWrapper.Models;

namespace Panther.Infrastructure.BassWrapper;

public interface IBassChannel : IDisposable
{
    event EventHandler? PlaybackEnded;

    BassHandle Handle { get; }

    bool Play();
    bool Pause();
    bool Stop();

    double GetChannelLengthInSeconds();

    double GetPositionInSeconds();
    bool SetPositionInSeconds(double seconds);

    bool Free();
}
