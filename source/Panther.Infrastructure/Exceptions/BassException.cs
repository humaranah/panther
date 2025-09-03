using System.Runtime.CompilerServices;
using Un4seen.Bass;

namespace Panther.Infrastructure.Exceptions;

public class BassException(
    string message,
    BASSError error,
    Exception? innerException = null,
    [CallerMemberName] string? operation = null) : Exception(message, innerException)
{
    public BASSError Error { get; } = error;
    public string Operation { get; } = operation ?? string.Empty;

    public override string ToString() => $"{Message}, Error: {Error}, Operation: {Operation}";
}
