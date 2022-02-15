using Panther.Core.Constants;
using Panther.Core.Enums;
using Panther.Core.Services;
using Panther.Core.UnitTests.Mocks;
using System;
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

        [Fact]
        public void Load_GivenInvalidFile_ShouldThrowException()
        {
            var events = 0;
            var fileName = "a.mp3";

            var action = () => _player.Load(fileName);
            var exception = Assert.Throws<ApplicationException>(action);

            Assert.Equal(ErrorMessages.CouldNotFind(fileName), exception.Message);
            Assert.Equal(PlayerStatus.Null, _player.Status);
            Assert.Equal(0, events);
        }
    }
}
