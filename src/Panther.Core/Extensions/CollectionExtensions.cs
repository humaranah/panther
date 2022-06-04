namespace Panther.Core.Extensions;

public static class CollectionExtensions
{
    private static readonly Random _rng = new();

    public static IEnumerable<T> Randomize<T>(this IEnumerable<T> collection)
    {
        var array = collection.ToArray();
        int currentPosition = array.Length;
        while (currentPosition > 1)
        {
            currentPosition--;
            var newPosition = _rng.Next(currentPosition + 1);
            var value = array[newPosition];
            array[newPosition] = array[currentPosition];
            array[currentPosition] = value;
        }

        return array;
    }
}
