namespace Panther.Infrastructure.BassWrapper.Models;

public readonly struct BassHandle : IEquatable<BassHandle>, IEquatable<int>
{
    private readonly int _handle;

    public BassHandle()
    {
        _handle = Empty;
    }

    public BassHandle(int handle)
    {
        _handle = handle;
    }

    public static BassHandle Empty => 0;

    public readonly bool IsEmpty => _handle == Empty;

    public readonly bool Equals(BassHandle other) => _handle == other._handle;

    public readonly bool Equals(int other) => _handle == other;

    public override readonly bool Equals(object? obj) => obj is BassHandle other && Equals(other);

    public override readonly int GetHashCode() => _handle.GetHashCode();

    public static implicit operator int(BassHandle handle) => handle._handle;
    public static implicit operator BassHandle(int handle) => new(handle);
    public static bool operator ==(BassHandle left, BassHandle right) => left.Equals(right);
    public static bool operator !=(BassHandle left, BassHandle right) => !(left == right);
}
