namespace Panther.Core.Util;

public class SharedRandomProvider : IRandomProvider
{
    public int Next(int maxValue) => Random.Shared.Next(maxValue);
}
