namespace Panther.Infrastructure.Models;

public readonly struct ChannelHandle : IEquatable<ChannelHandle>, IEquatable<int>
{
    private readonly int _handle;

    public ChannelHandle()
    {
        _handle = 0;
    }

    public ChannelHandle(int handle)
    {
        _handle = handle;
    }

    public readonly bool IsEmpty => _handle == 0;

    public readonly bool Equals(ChannelHandle other) => _handle == other._handle;

    public readonly bool Equals(int other) => _handle == other;

    public override readonly bool Equals(object? obj) => obj is ChannelHandle other && Equals(other);

    public override readonly int GetHashCode() => _handle.GetHashCode();

    public static implicit operator int(ChannelHandle handle) => handle._handle;
    public static implicit operator ChannelHandle(int handle) => new(handle);
    public static bool operator ==(ChannelHandle left, ChannelHandle right) => left.Equals(right);
    public static bool operator !=(ChannelHandle left, ChannelHandle right) => !(left == right);
}
