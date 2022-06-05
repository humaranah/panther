using Panther.Core.Models;
using Panther.Core.Services;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Panther.Core.UnitTests;

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
        var trackList = new List<TrackInfo>
            {
                new TrackInfo("a.wav"),
                new TrackInfo("b.wav")
            };

        _playerQueue.Load(trackList);

        Assert.True(_playerQueue.IsLoaded);
        Assert.Equal(2, _playerQueue.Count);
        Assert.Equal(trackList.First().FileName, _playerQueue.Current?.FileName);
    }

    [Fact]
    public void Add_GivenTrack_WithEmptyQueue_ShouldAddItToEnd()
    {
        var trackInfo = new TrackInfo("a.wav");

        _playerQueue.Add(trackInfo);

        Assert.True(_playerQueue.IsLoaded);
        Assert.Equal(1, _playerQueue.Count);
    }

    [Fact]
    public void Add_GivenTrack_WithNonEmptyQueue_ShouldAddItToEnd()
    {
        var initialTrackList = new List<TrackInfo>()
            {
                new TrackInfo("a.wav")
            };
        var trackInfo = new TrackInfo("b.wav");

        _playerQueue.Load(initialTrackList);
        _playerQueue.Add(trackInfo);

        Assert.Equal(2, _playerQueue.Count);
    }
}
