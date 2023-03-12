using Panther.Core.Models;
using Panther.Core.Services;
using System.Linq;
using Xunit;

namespace Panther.Core.UnitTests.Services;

public class PlayerQueueTests
{
    private readonly PlayerQueue _playerQueue;

    public PlayerQueueTests()
    {
        _playerQueue = new();
    }

    [Fact]
    public void Load_GivenTrackList_ShouldLoadIt()
    {
        var trackList = new Playlist
            {
                new TrackInfo("a.wav"),
                new TrackInfo("b.wav")
            };

        _playerQueue.Load(trackList);

        Assert.True(_playerQueue.IsLoaded);
        Assert.Equal(trackList.First().FileName, _playerQueue.Current?.FileName);
    }

    [Fact]
    public void Add_GivenTrack_WithEmptyQueue_ShouldAddIt()
    {
        var trackInfo = new TrackInfo("a.wav");

        _playerQueue.Add(trackInfo);

        Assert.True(_playerQueue.IsLoaded);
    }

    [Fact]
    public void Add_GivenTrack_WithNonEmptyQueue_ShouldAddItNext()
    {
        var initialTrackList = new Playlist
        {
            new TrackInfo("a.wav"),
            new TrackInfo("b.wav")
        };
        var trackInfo = new TrackInfo("test.wav");

        _playerQueue.Load(initialTrackList);
        _playerQueue.Add(trackInfo);

        Assert.NotNull(_playerQueue.Current);
        Assert.Single(_playerQueue.Played);
        Assert.Single(_playerQueue.Remaining);
        Assert.Equal("test.wav", _playerQueue.Current.FileName);
    }
}
