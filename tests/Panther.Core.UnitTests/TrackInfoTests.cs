using Panther.Core.Constants;
using Panther.Core.Models;
using System;
using Xunit;

namespace Panther.Core.UnitTests;

public class TrackInfoTests
{
    [Fact]
    public void Constructor_GivenValidFileName_ShouldAssignIt()
    {
        var trackInfo = new TrackInfo("somefile.wav");
        Assert.NotEmpty(trackInfo.FileName);
    }

    [Fact]
    public void Constructor_GiveNullOrEmptyFileName_ShouldThrowException()
    {
        var execution = () =>
        {
            var trackInfo = new TrackInfo("");
        };

        var exception = Assert.Throws<ArgumentException>(execution);
        Assert.StartsWith(ErrorMessages.ValueNotNullOrEmpty, exception.Message);
        Assert.Equal(nameof(TrackInfo.FileName), exception.ParamName);
    }
}
