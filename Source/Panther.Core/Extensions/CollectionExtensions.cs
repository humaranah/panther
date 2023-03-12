namespace Panther.Core.Extensions;

public static class CollectionExtensions
{
    public static IEnumerable<T> Randomize<T>(this IEnumerable<T> collection)
    {
        var random = new Random(DateTime.Now.Millisecond);
        var array = collection.ToArray();
        int currentPosition = array.Length;
        while (currentPosition > 1)
        {
            currentPosition--;
            var newPosition = random.Next(currentPosition + 1);
            (array[currentPosition], array[newPosition]) = (array[newPosition], array[currentPosition]);
        }

        return array;
    }

    public static T PopFirst<T>(this LinkedList<T> list)
    {
        var item = list.First();
        list.RemoveFirst();
        return item;
    }

    public static T PopLast<T>(this LinkedList<T> list)
    {
        var item = list.Last();
        list.RemoveLast();
        return item;
    }
}
