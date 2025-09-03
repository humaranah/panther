using Panther.Infrastructure.Models;

namespace Panther.Infrastructure.BassWrapper;

public interface IBassChannel : IDisposable
{
    ChannelHandle Handle { get; }

    void Play();
    void Pause();
    void Stop();

    double GetPositionInSeconds();
    void SetPositionInSeconds(double seconds);

    void Free();
}
