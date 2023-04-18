namespace ScheduleMyPoS.Extensions;

internal static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list, Random rand)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rand.Next(i + 1);
            (list[j], list[i]) = (list[i], list[j]);
        }
    }
}