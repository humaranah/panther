namespace Panther.Core.Models;

public record PlayerQueueItem(
    string Source,
    string Title,
    string Album,
    string Artist)
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
