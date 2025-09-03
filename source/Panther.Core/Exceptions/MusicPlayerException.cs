using System.Runtime.CompilerServices;

namespace Panther.Core.Exceptions;

public class MusicPlayerException(
    string message,
    string? trackSource = null,
    double? position = null,
    Exception? innerException = null,
    [CallerMemberName] string? operation = null) : Exception(message, innerException)
{
    public string? Operation { get; } = operation;
    public string? TrackSource { get; } = trackSource;
    public double? Position { get; } = position;

    public override string ToString()
    {
        return $"{Message}, Operation: {Operation}, TrackSource: {TrackSource}, Position: {Position}";
    }
}
