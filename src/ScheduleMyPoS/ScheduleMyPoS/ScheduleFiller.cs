using ScheduleMyPoS.Extensions;
using ScheduleMyPoS.Models;

namespace ScheduleMyPoS;

/// <summary>
/// Naive scheduler that greedily tries to set the visits.
/// </summary>
/// <remarks>
/// Not used, keep for tests.
/// </remarks>
internal sealed class ScheduleFiller
{
    private static Dictionary<DateOnly, LinkedList<PointOfService>> _theBest = new(0);

    private readonly IReadOnlyList<PointOfService> _points;
    private readonly DateOnly[] _dateRange;
    private readonly IReadOnlyDictionary<string, int> _visitsLeft;
    private const int VisitsPerDay = 14;

    public ScheduleFiller(IReadOnlyList<PointOfService> points, DateOnly[] dateRange, IReadOnlyDictionary<string, int> visitsLeft)
    {
        _points = points;
        _dateRange = dateRange;
        _visitsLeft = visitsLeft;
    }

    public VisitSchedule? GenerateSchedule()
    {
        var workingDays = new Dictionary<DateOnly, LinkedList<PointOfService>>(_dateRange.Length);
        var lastVisits = _points.ToDictionary(p => p.Name, _ => DateOnly.MinValue);
        var visitsLeft = _visitsLeft.ToDictionary(c => c.Key, c => c.Value);
        var rnd = new Random();
        foreach (var day in _dateRange)
        {
            var validPoints = _points
                .Where(p => p.Category.GetNextVisitDate(lastVisits[p.Name]) <= day
                && visitsLeft[p.Name] > 0).ToArray();

            if (validPoints.Length < VisitsPerDay)
            {
                if (_theBest.Count < workingDays.Count)
                {
                    _theBest = workingDays;
                }
                return null;
            }

            validPoints.Shuffle(rnd);
            var plannedVisits = validPoints.Take(VisitsPerDay).ToArray();

            foreach (var plannedVisit in plannedVisits)
            {
                lastVisits[plannedVisit.Name] = day;
                visitsLeft[plannedVisit.Name]--;
            }

            workingDays.Add(day, new LinkedList<PointOfService>(plannedVisits));
        }

        return new VisitSchedule(workingDays);
    }
}