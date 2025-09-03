using Un4seen.Bass;

namespace Panther.Infrastructure.BassWrapper.Models;

public record BassOperationError(
    BASSError Error,
    string Description,
    string? Method = null,
    object? Source = null
)
{
    public string? SourceName => Source?.GetType().Name;

    public override string ToString()
    {
        var sourcePart = string.IsNullOrWhiteSpace(SourceName) ? "" : $" in {SourceName}";
        var methodPart = string.IsNullOrWhiteSpace(Method) ? "" : $" during {Method}";
        return $"Bass Operation Error: {Description} (Error Code: {Error}{methodPart}{sourcePart})";
    }
}
