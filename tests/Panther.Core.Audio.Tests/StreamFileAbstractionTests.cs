namespace Panther.Core.Audio.Tests;

public class StreamFileAbstractionTests
{
    [Fact]
    public void WriteStream_Throws()
    {
        // Arrange
        using var stream = new MemoryStream();
        var abstraction = new StreamFileAbstraction("track.wav", stream);

        // Act & Assert
        Should.Throw<NotSupportedException>(() => abstraction.WriteStream);
    }

    [Fact]
    public void CloseStream_DoesNotThrow_AndDoesNotCloseTheUnderlyingStream()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        var abstraction = new StreamFileAbstraction("track.wav", stream);

        // Act
        Should.NotThrow(() => abstraction.CloseStream(stream));

        // Assert
        stream.CanRead.ShouldBeTrue(); // the underlying stream is managed externally, not by this call
    }
}
