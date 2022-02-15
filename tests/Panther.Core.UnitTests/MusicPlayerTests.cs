using Panther.Core.Enums;
using Panther.Core.Services;
using Panther.Core.UnitTests.Mocks;
using System.IO;
using Xunit;

namespace Panther.Core.UnitTests
{
    public class MusicPlayerTests
    {
        private readonly MusicPlayer _player;

        public MusicPlayerTests()
        {
            var bass = BassPlayerMock.Create();
            _player = new MusicPlayer(bass.Object);
        }

        [Fact]
        public void Load_GivenValidFile_ShouldLoadItAndRaiseEvent()
        {
            var events = 0;
            var fileName = "a.mp3";

            _player.StatusChanged += (s, e) => events++;
            File.Create(fileName).Close();
            _player.Load(fileName);
            File.Delete(fileName);

            Assert.Equal(fileName, _player.FileName);
            Assert.Equal(PlayerStatus.Stopped, _player.Status);
            Assert.Equal(1, events);
        }
    }
}
