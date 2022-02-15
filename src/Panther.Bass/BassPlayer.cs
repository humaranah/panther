using ManagedBass;

namespace Panther.Bass
{
    public class BassPlayer : IBassPlayer
    {
        public BassPlayer()
        {
            if (!ManagedBass.Bass.Init() && ManagedBass.Bass.LastError != Errors.Already)
            {
                throw new ApplicationException($"An error ocurred initializing Bass: {ManagedBass.Bass.LastError}");
            }
        }

        public long ChannelGetLength(int handle) => ManagedBass.Bass.ChannelGetLength(handle);
        public long ChannelGetPosition(int handle) => ManagedBass.Bass.ChannelGetPosition(handle);
        public bool ChannelPause(int handle) => ManagedBass.Bass.ChannelPause(handle);
        public bool ChannelPlay(int handle) => ManagedBass.Bass.ChannelPlay(handle);
        public bool ChannelStop(int handle) => ManagedBass.Bass.ChannelStop(handle);

        public int CreateStream(string fileName) => ManagedBass.Bass.CreateStream(fileName);

        public void Dispose()
        {
            if (ManagedBass.Bass.Init() || ManagedBass.Bass.LastError == Errors.Already)
            {
                ManagedBass.Bass.Free();
            }
        }
    }
}
