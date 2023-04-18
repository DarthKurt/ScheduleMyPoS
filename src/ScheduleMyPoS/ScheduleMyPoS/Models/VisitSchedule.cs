using ScheduleMyPoS.Extensions;

namespace ScheduleMyPoS.Models;

internal sealed class VisitSchedule
{
    public VisitSchedule(IDictionary<DateOnly, LinkedList<PointOfService>> workingDays)
    {
        WorkingDays = workingDays;

    }

    public IDictionary<DateOnly, LinkedList<PointOfService>> WorkingDays { get; }

    /// <summary>
    /// Metric of the schedule to compare
    /// </summary>
    /// <returns></returns>
    public double EvaluateDistance() => WorkingDays.Values.Average(AggregateLinkedList);

    /// <summary>
    /// Metric of the schedule to compare
    /// </summary>
    /// <returns></returns>
    public IDictionary<SellingPointCategory, int> EvaluateMinimalIntervals()
    {
        return WorkingDays.SelectMany(
                kvp => kvp.Value.Select(p => (kvp.Key, p.Name, p.Category)))
            // Group points
            .GroupBy(i => i.Name)
            .Where(g => g.Count() > 1)
            // For each point group look for minimal interval between visits
            .Select(g =>
            {
                var sortedItems = g.OrderBy(i => i.Key).ToArray();
                var minDuration = sortedItems
                    .Skip(1)
                    // Assume here no year changes
                    .Zip(sortedItems, (a, b) => a.Key.DayOfYear - b.Key.DayOfYear)
                    .Min();
                return (sortedItems[0].Category, minDuration);
            })
            // Group by category
            .GroupBy(g => g.Category)
            // In each category get the minimal interval
            .ToDictionary(g => g.Key, g => g.Min(i => i.minDuration));
    }

    private static int AggregateLinkedList(LinkedList<PointOfService> visits)
    {
        var sum = 0;
        var currentNode = visits.First;

        while (currentNode is { Next: not null })
        {
            sum += currentNode.Value.District.GetTransitionWeight(currentNode.Next.Value.District);
            currentNode = currentNode.Next;
        }

        return sum;
    }
}