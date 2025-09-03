using FluentAssertions;
using Panther.Core.Models;

namespace Panther.Infrastructure.Tests;

public class PlayerQueueServiceTests
{
    private readonly PlayerQueueService _queueService = new();

    private static readonly PlayerQueueItem[] TestItems =
    [
        new("source1", "title1", "album1", "artist1"),
        new("source2", "title2", "album2", "artist2"),
        new("source3", "title3", "album3", "artist3")
    ];

    [Fact]
    public void Constructor_ShouldInitializeEmptyQueue()
    {
        // Assert
        _queueService.Items.Should().BeEmpty();
        _queueService.Current.Should().BeNull();
        _queueService.IsEmpty.Should().BeTrue();
        _queueService.IsFirstPosition.Should().BeTrue();
        _queueService.IsLastPosition.Should().BeTrue();
    }

    [Fact]
    public void Add_SingleItem_ShouldAddToQueue()
    {
        // Arrange
        var testItem = TestItems[0];
        // Act
        var result = _queueService.Add(testItem);
        // Assert
        result.Should().BeTrue();
        _queueService.Items.Should().ContainSingle();
        _queueService.Current.Should().Be(testItem);
    }

    [Fact]
    public void AddRange_ShouldAddAllToQueue()
    {
        // Arrange
        var expected = TestItems[0];
        // Act
        var result = _queueService.AddRange(TestItems);
        // Assert
        result.Should().BeTrue();
        _queueService.Items.Count.Should().Be(3);
        _queueService.Current.Should().Be(expected);
    }

    [Fact]
    public void AddRange_ShouldReturnFalseIfEmpty()
    {
        // Act
        var result = _queueService.AddRange([]);
        // Assert
        result.Should().BeFalse();
        _queueService.Items.Should().BeEmpty();
        _queueService.Current.Should().BeNull();
    }

    [Fact]
    public void Clear_ShouldEmptyTheQueue()
    {
        // Arrange
        _queueService.AddRange(TestItems);
        // Act
        _queueService.Clear();
        // Assert
        _queueService.Items.Should().BeEmpty();
        _queueService.Current.Should().BeNull();
        _queueService.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void SetCurrentTo_ShouldSetItToCorrespondingItem()
    {
        // Arrange
        _queueService.AddRange(TestItems);
        var target = TestItems[^1];
        // Act
        var result = _queueService.SetCurrentTo(target);
        // Assert
        result.Should().Be(target);
    }

    [Fact]
    public void SetCurrentTo_ShouldReturnNullIfNotInQueue()
    {
        // Arrange
        _queueService.AddRange(TestItems);
        var target = new PlayerQueueItem("source4", "title", "album", "artist");
        // Act
        var result = _queueService.SetCurrentTo(target);
        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(0, false, 1)]
    [InlineData(0, true, 1)]
    [InlineData(null, false, null)]
    [InlineData(null, true, null)]
    [InlineData(-1, false, null)]
    [InlineData(-1, true, 0)]
    public void Next_ShouldSetItToCorrespondingItem(int? currentPosition, bool isLoop, int? expectedPosition)
    {
        // Arrange
        var current = GetTestItemByPosition(currentPosition);
        var expected = GetTestItemByPosition(expectedPosition);
        if (current != null)
        {
            _queueService.AddRange(TestItems);
            _queueService.SetCurrentTo(current);
        }
        // Act
        var result = _queueService.Next(isLoop);
        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, false, 0)]
    [InlineData(1, true, 0)]
    [InlineData(null, false, null)]
    [InlineData(null, true, null)]
    [InlineData(0, false, null)]
    [InlineData(0, true, -1)]
    public void Previous_ShouldSetItToCorrespondingItem(int? currentPosition, bool isLoop, int? expectedPosition)
    {
        // Arrange
        var current = GetTestItemByPosition(currentPosition);
        var expected = GetTestItemByPosition(expectedPosition);
        if (current != null)
        {
            _queueService.AddRange(TestItems);
            _queueService.SetCurrentTo(current);
        }
        // Act
        var result = _queueService.Previous(isLoop);
        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Remove_ShouldRemoveItem()
    {
        // Arrange
        _queueService.AddRange(TestItems);
        var toRemove = TestItems[1];
        // Act
        var result = _queueService.Remove(toRemove);
        // Assert
        result.Should().BeTrue();
        _queueService.Items.Count.Should().Be(2);
        _queueService.Items.Should().NotContain(toRemove);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(-1, -2)]
    public void Remove_ShouldRemoveCurrentItemAndAdjustCurrent(int currentPosition, int targetPosition)
    {
        // Arrange
        _queueService.AddRange(TestItems);
        var toRemove = GetTestItemByPosition(currentPosition)!;
        var expected = GetTestItemByPosition(targetPosition)!;
        var expectedCount = TestItems.Length - 1;
        _queueService.SetCurrentTo(toRemove);
        // Act
        var result = _queueService.Remove(toRemove);
        // Assert
        result.Should().BeTrue();
        _queueService.Items.Count.Should().Be(expectedCount);
        _queueService.Current.Should().Be(expected);
    }

    [Fact]
    public void Remove_ShouldReturnFalseIfItemNotInQueue()
    {
        // Arrange
        _queueService.AddRange(TestItems);
        var toRemove = new PlayerQueueItem("source4", "title", "album", "artist");
        // Act
        var result = _queueService.Remove(toRemove);
        // Assert
        result.Should().BeFalse();
        _queueService.Items.Count.Should().Be(3);
    }

    [Fact]
    public void Remove_ShouldSetCurrentToNullIfLastItemRemoved()
    {
        // Arrange
        var singleItem = TestItems[0];
        _queueService.Add(singleItem);
        _queueService.SetCurrentTo(singleItem);
        // Act
        var result = _queueService.Remove(singleItem);
        // Assert
        result.Should().BeTrue();
        _queueService.Items.Should().BeEmpty();
        _queueService.Current.Should().BeNull();
    }

    [Fact]
    public void Shuffle_ShouldRandomizeOrderAndKeepCurrentItem()
    {
        // Arrange
        _queueService.AddRange(TestItems);
        var originalOrder = _queueService.Items.ToList();
        // Act
        _queueService.Shuffle();
        var shuffledOrder = _queueService.Items.ToList();
        // Assert
        shuffledOrder.Should().HaveCount(originalOrder.Count);
        shuffledOrder.Should().NotEqual(originalOrder);
        shuffledOrder.Should().Contain(originalOrder);
        _queueService.Current.Should().Be(originalOrder[0]);
    }

    [Fact]
    public void Shuffle_ShouldDoNothingIfQueueIsEmpty()
    {
        // Act
        _queueService.Shuffle();
        // Assert
        _queueService.Items.Should().BeEmpty();
        _queueService.Current.Should().BeNull();
    }

    [Fact]
    public void Shuffle_ShouldDoNothingIfQueueHasOneItem()
    {
        // Arrange
        var singleItem = TestItems[0];
        _queueService.Add(singleItem);
        // Act
        _queueService.Shuffle();
        // Assert
        _queueService.Items.Should().ContainSingle().Which.Should().Be(singleItem);
        _queueService.Current.Should().Be(singleItem);
    }

    private static PlayerQueueItem? GetTestItemByPosition(int? position)
    {
        return position switch
        {
            null => null,
            < 0 => TestItems[^(-position.Value)],
            _ => TestItems[position.Value]
        };
    }
}
