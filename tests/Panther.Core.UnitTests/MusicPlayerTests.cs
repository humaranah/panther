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
        public void Load_GivenValidFile_ShouldLoadIt()
        {
            var fileName = "a.mp3";

            File.Create(fileName).Close();
            _player.Load(fileName);
            File.Delete(fileName);

            Assert.Equal(fileName, _player.FileName);
        }
    }
}
