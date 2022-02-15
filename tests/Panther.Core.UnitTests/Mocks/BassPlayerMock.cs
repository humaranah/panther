using Moq;
using Panther.Bass;

namespace Panther.Core.UnitTests.Mocks
{
    public static class BassPlayerMock
    {
        public static Mock<IBassPlayer> Create()
        {
            var bass = new Mock<IBassPlayer>();

            bass.Setup(x => x.ChannelGetPosition(It.IsAny<int>())).Returns(0);
            bass.Setup(x => x.ChannelGetLength(It.IsAny<int>())).Returns(10);
            bass.Setup(x => x.ChannelPause(It.IsAny<int>())).Returns(true);
            bass.Setup(x => x.ChannelPlay(It.IsAny<int>())).Returns(true);
            bass.Setup(x => x.ChannelStop(It.IsAny<int>())).Returns(true);

            bass.Setup(x => x.CreateStream(It.IsAny<string>())).Returns(0);

            bass.Setup(x => x.Dispose());

            return bass;
        }
    }
}
