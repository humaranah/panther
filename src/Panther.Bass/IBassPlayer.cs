namespace Panther.Bass
{
    public interface IBassPlayer : IDisposable
    {
        long ChannelGetLength(int handle);
        long ChannelGetPosition(int handle);
        bool ChannelPause(int handle);
        bool ChannelPlay(int handle);
        bool ChannelStop(int handle);

        int CreateStream(string fileName);
    }
}
