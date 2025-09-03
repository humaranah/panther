using Un4seen.Bass;

namespace Panther.Infrastructure.Exceptions;

public class BassException(
    BASSError error = BASSError.BASS_ERROR_UNKNOWN,
    Exception? innerException = null)
    : Exception($"BASS Error: {error}", innerException)
{
    public BASSError Error { get; } = error;
}
