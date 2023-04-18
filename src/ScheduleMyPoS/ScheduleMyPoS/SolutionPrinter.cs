using Google.OrTools.Sat;
using ScheduleMyPoS.Models;
using Spectre.Console;

namespace ScheduleMyPoS;

internal sealed class SolutionPrinter : CpSolverSolutionCallback
{
    private readonly IReadOnlyList<PointOfService> _points;
    private readonly DateOnly[] _dateRange;

    private readonly IntVar[,,] _visits;
    private readonly int _solutionLimit;
    private readonly int _visitsPerDay;
    private readonly Action _feedback;

    private int _solutionCount;

    private readonly Table _table = new();

    public SolutionPrinter(
        IReadOnlyList<PointOfService> points,
        DateOnly[] dateRange,
        IntVar[,,] visits,
        int limit,
        int visitsPerDay,
        Action feedback)
    {
        _visits = visits;
        _points = points;
        _dateRange = dateRange;
        _solutionLimit = limit;
        _visitsPerDay = visitsPerDay;
        _feedback = feedback;

        _table
            .AddColumn("Day")
            .AddColumn("Visit")
            .AddColumn("Point Id")
            .AddColumn("Address")
            .AddColumn("District");
    }

    public override void OnSolutionCallback()
    {
        Console.WriteLine($"Solution #{_solutionCount}");
        var days = new Dictionary<DateOnly, LinkedList<PointOfService>>();

        for (var d = 0; d < _dateRange.Length; d++)
        {
            Console.WriteLine($"Day {d}");
            var results = new PointOfService?[_visitsPerDay];
            for (var p = 0; p < _points.Count; p++)
            {
                for (var v = 0; v < _visitsPerDay; v++)
                {
                    if (Value(_visits[d, p, v]) != 1L)
                        continue;

                    results[v] = _points[p];
                }
            }

            var day = _dateRange[d];
            var visits = new LinkedList<PointOfService>();

            for (var p = 0; p < results.Length; p++)
            {
                var point = results[p];

                if (point != null)
                {
                    visits.AddLast(point);
                }

                _table.AddRow(
                    $"{day}",
                    $"{p}",
                    $"{point?.Name ?? "-"}",
                    $"{point?.Address ?? "-"}",
                    $"{point?.District.Main ?? "-"} / {point?.District.Secondary ?? "-"}");
            }

            days.Add(day, visits);
        }

        var schedule = new VisitSchedule(days);
        VisitScheduleStorage.Solutions.Add(schedule);

        _feedback();
        _solutionCount++;
        if (_solutionCount < _solutionLimit)
            return;

        // Render the table to the console
        AnsiConsole.Write(_table);
        Console.WriteLine($"Stop search after {_solutionLimit} solutions");
        StopSearch();
    }
}