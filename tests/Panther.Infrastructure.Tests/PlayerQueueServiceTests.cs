using Moq;
using Panther.Core.Models;
using Panther.Core.Util;
using Shouldly;

namespace Panther.Infrastructure.Tests;

public class PlayerQueueServiceTests
{
    private readonly Mock<IRandomProvider> _randomMock = new();
    private readonly PlayerQueueService _queueService;

    private static readonly TrackInfo TestItem =
        new() { Source = "source", Title = "title" };

    private static readonly TrackInfo[] TestItems =
    [
        new() { Source = "source1", Title = "title1" },
        new() { Source = "source2", Title = "title2" },
        new() { Source = "source3", Title = "title3" }
    ];

    private readonly HashSet<string> _propertiesChanged = [];

    public PlayerQueueServiceTests()
    {
        _queueService = new(_randomMock.Object);
    }

    #region GetNext Tests
    [Fact]
    public void GetNext_ShouldReturnNullIfQueueIsEmpty()
    {
        // Arrange
        AttachPropertyChangedEventHandler();
        // Act
        var result = _queueService.GetNext();
        // Assert
        result.ShouldBeNull();
        _propertiesChanged.ShouldNotContain(nameof(_queueService.Current));
    }

    [Fact]
    public void GetNext_ShouldReturnNextWhenHistoryIsNotAtEnd()
    {
        // Arrange
        _queueService.AddToSource(TestItems);
        AttachPropertyChangedEventHandler();
        var expected = TestItems[1];
        // Act
        var result = _queueService.GetNext();
        // Assert
        result.ShouldBe(expected);
        _propertiesChanged.ShouldContain(nameof(_queueService.Current));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void GetNext_ShouldReturnNextWhenHistoryIsAtEnd(bool isShuffle, int expectedIndex)
    {
        // Arrange
        _queueService.AddToSource(TestItems); // This will pop the first remaining item
        AttachPropertyChangedEventHandler();
        _queueService.IsShuffle = isShuffle;
        _randomMock.Setup(r => r.Next(It.IsAny<int>()))
            .Returns(expectedIndex - 1); // -1 because the first item is already popped
        var expected = TestItems[expectedIndex];
        // Act
        var result = _queueService.GetNext();
        // Assert
        result.ShouldBe(expected);
        _propertiesChanged.ShouldContain(nameof(_queueService.Current));
        if (isShuffle)
            _propertiesChanged.ShouldContain(nameof(_queueService.IsShuffle));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void GetNext_ShouldRefillRemainingWhenRepeat(bool isShuffle, int expectedIndex)
    {
        // Arrange
        _queueService.AddToSource(TestItems);
        AttachPropertyChangedEventHandler();
        _queueService.IsRepeat = true;
        _queueService.IsShuffle = isShuffle;
        _queueService.GetNext(); // Move to second track
        _queueService.GetNext(); // Move to third track (popping last remaining)
        _randomMock.Setup(r => r.Next(It.IsAny<int>()))
            .Returns(expectedIndex);
        var expected = TestItems[expectedIndex];
        // Act
        var result = _queueService.GetNext();
        // Assert
        result.ShouldBe(expected);
        _queueService.IsRepeat.ShouldBeTrue();
        _propertiesChanged.ShouldContain(nameof(_queueService.IsRepeat));
        _propertiesChanged.ShouldContain(nameof(_queueService.Current));
        if (isShuffle)
            _propertiesChanged.ShouldContain(nameof(_queueService.IsShuffle));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetNext_ShouldReturnNullWhenNoRemainingAndNoRepeat(bool isShuffle)
    {
        // Arrange
        _queueService.AddToSource(TestItem);
        AttachPropertyChangedEventHandler();
        _queueService.IsShuffle = isShuffle;
        // Act
        var result = _queueService.GetNext();
        // Assert
        _queueService.IsRepeat.ShouldBeFalse();
        result.ShouldBeNull();
        _propertiesChanged.ShouldNotContain(nameof(_queueService.Current));
        if (isShuffle)
            _propertiesChanged.ShouldContain(nameof(_queueService.IsShuffle));
    }
    #endregion

    #region GetPrevious Tests
    [Fact]
    public void GetPrevious_ShouldReturnNullIfNoHistory()
    {
        // Arrange
        AttachPropertyChangedEventHandler();
        // Act
        var result = _queueService.GetPrevious();
        // Assert
        result.ShouldBeNull();
        _propertiesChanged.ShouldNotContain(nameof(_queueService.Current));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetPrevious_ShouldReturnExpectedItem(bool isFirstElement)
    {
        // Arrange
        AttachPropertyChangedEventHandler();
        _queueService.AddToSource(TestItems);
        if (!isFirstElement)
            _queueService.GetNext(); // Move to second track
        var expected = isFirstElement ? null : TestItems[0];
        // Act
        var result = _queueService.GetPrevious();
        // Assert
        result.ShouldBe(expected);
        _propertiesChanged.ShouldContain(nameof(_queueService.Current));
    }
    #endregion

    #region Add Tests
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddToSource_ShouldAddSingleItem(bool sourceWasEmpty)
    {
        // Arrange
        AttachPropertyChangedEventHandler();
        if (!sourceWasEmpty)
            _queueService.AddToSource(TestItems[0]);
        var itemToAdd = TestItem;
        var initialSourceCount = _queueService.Source.Count;
        var expectedSourceCount = initialSourceCount + 1;
        var expectedCurrent = sourceWasEmpty ? itemToAdd : _queueService.Current;
        var expectedRemainingCount = sourceWasEmpty ? 0 : 1;
        // Act
        _queueService.AddToSource(itemToAdd);
        // Assert
        _queueService.ShouldSatisfyAllConditions(
            q => q.Source.Count.ShouldBe(expectedSourceCount),
            q => q.Current.ShouldBe(expectedCurrent),
            q => q.IsEmpty.ShouldBeFalse(),
            q => q.History.Count.ShouldBe(1),
            q => q.Remaining.Count.ShouldBe(expectedRemainingCount));
        _propertiesChanged.ShouldContain(nameof(_queueService.Current));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddToSource_ShouldAddMultipleItems(bool sourceWasEmpty)
    {
        // Arrange
        AttachPropertyChangedEventHandler();
        if (!sourceWasEmpty)
            _queueService.AddToSource(TestItem);
        var itemsToAdd = TestItems;
        var initialSourceCount = _queueService.Source.Count;
        var expectedCurrent = sourceWasEmpty ? TestItems[0] : _queueService.Current;
        var expectedSourceCount = initialSourceCount + itemsToAdd.Length;
        var expectedRemainingCount = sourceWasEmpty ? itemsToAdd.Length - 1 : itemsToAdd.Length;
        // Act
        var result = _queueService.AddToSource(itemsToAdd);
        // Assert
        result.ShouldBeTrue();
        _queueService.ShouldSatisfyAllConditions(
            x => x.Current.ShouldBe(expectedCurrent),
            x => x.IsEmpty.ShouldBeFalse(),
            x => x.Source.Count.ShouldBe(expectedSourceCount),
            x => x.History.Count.ShouldBe(1),
            x => x.Remaining.Count.ShouldBe(expectedRemainingCount));
        _propertiesChanged.ShouldContain(nameof(_queueService.Current));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddToSource_ShouldNotAddEmptyList(bool sourceWasEmpty)
    {
        // Arrange
        if (!sourceWasEmpty)
            _queueService.AddToSource(TestItems);
        AttachPropertyChangedEventHandler();
        var initialSourceCount = _queueService.Source.Count;
        var expectedCurrent = sourceWasEmpty ? null : _queueService.Current;
        var expectedIsEmpty = sourceWasEmpty;
        var expectedHistoryCount = sourceWasEmpty ? 0 : 1;
        var expectedRemainingCount = sourceWasEmpty ? 0 : initialSourceCount - 1;
        // Act
        var result = _queueService.AddToSource([]);
        // Assert
        result.ShouldBeFalse();
        _queueService.ShouldSatisfyAllConditions(
            x => x.Current.ShouldBe(expectedCurrent),
            x => x.IsEmpty.ShouldBe(expectedIsEmpty),
            x => x.Source.Count.ShouldBe(initialSourceCount),
            x => x.History.Count.ShouldBe(expectedHistoryCount),
            x => x.Remaining.Count.ShouldBe(expectedRemainingCount));
        _propertiesChanged.ShouldNotContain(nameof(_queueService.Current));
    }
    #endregion

    #region SetCurrent Tests
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetCurrent_ShouldReturnExpectedValue(bool itemInHistory)
    {
        // Arrange
        _queueService.AddToSource(TestItems);
        _queueService.GetNext(); // Move to second track
        AttachPropertyChangedEventHandler();
        var itemToSet = itemInHistory ? TestItems[0] : TestItem;
        var expected = itemInHistory ? itemToSet : TestItems[1];
        // Act
        var result = _queueService.SetCurrent(itemToSet);
        // Assert
        result.ShouldBe(expected);
        _queueService.Current.ShouldBe(expected);
        if (itemInHistory)
            _propertiesChanged.ShouldContain(nameof(_queueService.Current));
        else
            _propertiesChanged.ShouldNotContain(nameof(_queueService.Current));
    }

    [Fact]
    public void SetCurrent_ShouldReturnNullIfSourceIsEmpty()
    {
        // Arrange
        AttachPropertyChangedEventHandler();
        var itemToSet = TestItem;
        // Act
        var result = _queueService.SetCurrent(itemToSet);
        // Assert
        result.ShouldBeNull();
        _queueService.Current.ShouldBeNull();
        _propertiesChanged.ShouldNotContain(nameof(_queueService.Current));
    }
    #endregion

    #region RemoveFromSource Tests
    [Fact]
    public void RemoveFromSource_ShouldReturnFalseIfItemNotInSource()
    {
        // Arrange
        _queueService.AddToSource(TestItems);
        AttachPropertyChangedEventHandler();
        var itemToRemove = TestItem;
        // Act
        var result = _queueService.RemoveFromSource(itemToRemove);
        // Assert
        result.ShouldBeFalse();
        _propertiesChanged.ShouldNotContain(nameof(_queueService.Current));
    }

    [Fact]
    public void RemoveFromSource_ShouldReturnFalseIfSourceIsEmpty()
    {
        // Arrange
        AttachPropertyChangedEventHandler();
        var itemToRemove = TestItem;
        // Act
        var result = _queueService.RemoveFromSource(itemToRemove);
        // Assert
        result.ShouldBeFalse();
        _propertiesChanged.ShouldNotContain(nameof(_queueService.Current));
    }

    [Fact]
    public void RemoveFromSource_ShouldRemoveRemainingItemAndReturnTrue()
    {
        // Arrange
        _queueService.AddToSource(TestItems);
        AttachPropertyChangedEventHandler();
        var itemToRemove = TestItems[1];
        var initialSourceCount = _queueService.Source.Count;
        var expectedSourceCount = initialSourceCount - 1;
        var expectedCurrent = _queueService.Current;
        var expectedRemainingCount = _queueService.Remaining.Count - 1;
        // Act
        var result = _queueService.RemoveFromSource(itemToRemove);
        // Assert
        result.ShouldBeTrue();
        _queueService.ShouldSatisfyAllConditions(
            x => x.Current.ShouldBe(expectedCurrent),
            x => x.Source.Count.ShouldBe(expectedSourceCount),
            x => x.History.Count.ShouldBe(1),
            x => x.Remaining.Count.ShouldBe(expectedRemainingCount));
        _propertiesChanged.ShouldNotContain(nameof(_queueService.Current));
    }

    [Fact]
    public void RemoveFromSource_ShouldRemoveOnlyItemInHistory()
    {
        // Arrange
        _queueService.AddToSource(TestItem);
        AttachPropertyChangedEventHandler();
        var itemToRemove = TestItem;
        // Act
        var result = _queueService.RemoveFromSource(itemToRemove);
        // Assert
        result.ShouldBeTrue();
        _queueService.ShouldSatisfyAllConditions(
            x => x.Current.ShouldBeNull(),
            x => x.IsEmpty.ShouldBeTrue(),
            x => x.Source.Count.ShouldBe(0),
            x => x.History.Count.ShouldBe(0),
            x => x.Remaining.Count.ShouldBe(0));
        _propertiesChanged.ShouldContain(nameof(_queueService.Current));
    }

    [Fact]
    public void RemoveFromSource_ShouldRemoveItemPreviousToCurrent()
    {
        // Arrange
        _queueService.AddToSource(TestItems); // Add 3 items and pop the first one to History
        _queueService.GetNext(); // Move to second item
        AttachPropertyChangedEventHandler();
        var itemToRemove = TestItems[0]; // Will remove the first item in History
        var initialSourceCount = _queueService.Source.Count;
        var expectedSourceCount = initialSourceCount - 1;
        var expectedCurrent = _queueService.Current;
        var expectedRemainingCount = _queueService.Remaining.Count;
        var expectedHistoryCount = _queueService.History.Count - 1;
        // Act
        var result = _queueService.RemoveFromSource(itemToRemove);
        // Assert
        result.ShouldBeTrue();
        _queueService.ShouldSatisfyAllConditions(
            x => x.Current.ShouldBe(expectedCurrent),
            x => x.Source.Count.ShouldBe(expectedSourceCount),
            x => x.History.Count.ShouldBe(expectedHistoryCount),
            x => x.Remaining.Count.ShouldBe(expectedRemainingCount));
        _propertiesChanged.ShouldNotContain(nameof(_queueService.Current));
    }

    [Fact]
    public void RemoveFromSource_ShouldRemoveItemAfterCurrent()
    {
        // Arrange
        _queueService.AddToSource(TestItems); // Add 3 items and pop the first one to History
        _queueService.GetNext(); // Move to second item
        AttachPropertyChangedEventHandler();
        var itemToRemove = TestItems[2]; // Will remove the last item in Source
        var initialSourceCount = _queueService.Source.Count;
        var expectedSourceCount = initialSourceCount - 1;
        var expectedCurrent = _queueService.Current;
        var expectedRemainingCount = _queueService.Remaining.Count - 1;
        var expectedHistoryCount = _queueService.History.Count;
        // Act
        var result = _queueService.RemoveFromSource(itemToRemove);
        // Assert
        result.ShouldBeTrue();
        _queueService.ShouldSatisfyAllConditions(
            x => x.Current.ShouldBe(expectedCurrent),
            x => x.Source.Count.ShouldBe(expectedSourceCount),
            x => x.History.Count.ShouldBe(expectedHistoryCount),
            x => x.Remaining.Count.ShouldBe(expectedRemainingCount));
        _propertiesChanged.ShouldNotContain(nameof(_queueService.Current));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void RemoveFromSource_ShouldRemoveCurrentItem(int currentIndexToRemove, int expectedIndexFromTestData)
    {
        // Arrange
        _queueService.AddToSource(TestItems); // Add 3 items and pop the first one to History
        for (int i = 1; i <= TestItems.Length; i++)
            _queueService.GetNext(); // Push all items to History
        _queueService.SetCurrent(TestItems[currentIndexToRemove]); // Set the current item to the specified index
        AttachPropertyChangedEventHandler();
        var itemToRemove = _queueService.History[currentIndexToRemove];
        var expectedCurrent = TestItems[expectedIndexFromTestData];
        var initialSourceCount = _queueService.Source.Count;
        var expectedSourceCount = initialSourceCount - 1;
        var expectedHistoryCount = _queueService.History.Count - 1;
        // Act
        var result = _queueService.RemoveFromSource(itemToRemove);
        // Assert
        result.ShouldBeTrue();
        _queueService.ShouldSatisfyAllConditions(
            x => x.Current.ShouldBe(expectedCurrent),
            x => x.Source.Count.ShouldBe(expectedSourceCount),
            x => x.History.Count.ShouldBe(expectedHistoryCount));
        _propertiesChanged.ShouldContain(nameof(_queueService.Current));
    }
    #endregion

    #region ReplaceSource Tests
    [Fact]
    public void ReplaceSource_ShouldReplaceWithNewItems()
    {
        // Arrange
        _queueService.AddToSource(TestItems);
        var newItems = new[] { TestItem };
        // Act
        var result = _queueService.ReplaceSource(newItems);
        // Assert
        result.ShouldBeTrue();
        _queueService.ShouldSatisfyAllConditions(
            x => x.Current.ShouldBe(TestItem),
            x => x.IsEmpty.ShouldBeFalse(),
            x => x.Source.Count.ShouldBe(1),
            x => x.History.Count.ShouldBe(1),
            x => x.Remaining.Count.ShouldBe(0));
    }

    [Fact]
    public void ReplaceSource_ShouldReturnFalseIfNewItemsIsEmpty()
    {
        // Arrange
        _queueService.AddToSource(TestItems);
        var initialSourceCount = _queueService.Source.Count;
        var expectedCurrent = _queueService.Current;
        var expectedHistoryCount = _queueService.History.Count;
        var expectedRemainingCount = _queueService.Remaining.Count;
        // Act
        var result = _queueService.ReplaceSource([]);
        // Assert
        result.ShouldBeFalse();
        _queueService.ShouldSatisfyAllConditions(
            x => x.Current.ShouldBe(expectedCurrent),
            x => x.IsEmpty.ShouldBeFalse(),
            x => x.Source.Count.ShouldBe(initialSourceCount),
            x => x.History.Count.ShouldBe(expectedHistoryCount),
            x => x.Remaining.Count.ShouldBe(expectedRemainingCount));
    }
    #endregion

    #region Clear Tests
    [Fact]
    public void Clear_ShouldEmptyTheQueue()
    {
        // Arrange
        _queueService.AddToSource(TestItems);
        // Act
        _queueService.Clear();
        // Assert
        _queueService.ShouldSatisfyAllConditions(
            x => x.Current.ShouldBeNull(),
            x => x.IsEmpty.ShouldBeTrue(),
            x => x.Source.Count.ShouldBe(0),
            x => x.History.Count.ShouldBe(0),
            x => x.Remaining.Count.ShouldBe(0));
    }
    #endregion

    private void AttachPropertyChangedEventHandler()
    {
        _queueService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == null)
                return;
            if (!_propertiesChanged.Contains(e.PropertyName))
                _propertiesChanged.Add(e.PropertyName!);
        };
    }
}
