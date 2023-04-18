using System.Collections.Concurrent;
using ScheduleMyPoS.Models;

namespace ScheduleMyPoS;

internal static class VisitScheduleStorage
{
    public static ConcurrentBag<VisitSchedule> Solutions { get; } = new();
}