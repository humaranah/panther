namespace Panther.Core.Audio.Tests;

public class MiniAudioNativeTests
{
    [Fact]
    public unsafe void DeviceInfoNative_GetName_ReadsTheNullTerminatedUtf8Name()
    {
        // Arrange
        var info = new MiniAudioNative.DeviceInfoNative();
        var bytes = "Test Device"u8;
        for (var i = 0; i < bytes.Length; i++)
        {
            info.name[i] = bytes[i];
        }

        info.name[bytes.Length] = 0;

        // Act
        var name = info.GetName();

        // Assert
        name.ShouldBe("Test Device");
    }

    [Fact]
    public unsafe void DeviceInfoNative_GetName_ReturnsEmptyForAnUnsetBuffer()
    {
        // Arrange
        var info = new MiniAudioNative.DeviceInfoNative(); // fixed buffers are zero-initialized

        // Act
        var name = info.GetName();

        // Assert
        name.ShouldBe(string.Empty);
    }
}
