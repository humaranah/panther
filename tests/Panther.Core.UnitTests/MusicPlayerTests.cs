using Moq;
using NAudio.Wave;
using Panther.Core.Constants;
using Panther.Core.Enums;
using Panther.Core.Services;
using System.IO;
using Xunit;

namespace Panther.Core.UnitTests;
public class MusicPlayerTests
{
    private readonly Mock<WaveOutEvent> _wavePlayer;
    private readonly MusicPlayer _musicPlayer;

    public MusicPlayerTests()
    {
        _wavePlayer = new Mock<WaveOutEvent>();
        _musicPlayer = new MusicPlayer(_wavePlayer.Object);
    }

    [Fact]
    public void Load_GivenValidFile_ShouldLoadItAndRaiseEvent()
    {
        var fileName = @"..\..\..\..\..\samples\empty_1c.wav";

        _musicPlayer.Load(fileName);

        Assert.Equal(fileName, _musicPlayer.FileName);
        Assert.Equal(PlayerStatus.Stopped, _musicPlayer.Status);
    }

    [Fact]
    public void Load_GivenInvalidFile_ShouldThrowException()
    {
        var events = 0;
        var fileName = "notFound.mp3";

        var action = () => _musicPlayer.Load(fileName);
        var exception = Assert.Throws<FileNotFoundException>(action);

        Assert.Equal(ErrorMessages.CouldNotFind(fileName), exception.Message);
        Assert.Equal(PlayerStatus.Stopped, _musicPlayer.Status);
        Assert.Equal(0, events);
    }
}
