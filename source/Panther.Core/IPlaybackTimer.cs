namespace Panther.Core;

public interface IPlaybackTimer
{
    event EventHandler Elapsed;
    void Start();
    void Stop();
}
