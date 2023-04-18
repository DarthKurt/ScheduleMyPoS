using Google.OrTools.Sat;
using ScheduleMyPoS.Extensions;
using ScheduleMyPoS.Models;
using Spectre.Console;

namespace ScheduleMyPoS;

/// <summary>
/// Scheduler using Google OR tools
/// </summary>
internal sealed class ScheduleSolver
{
    private readonly IReadOnlyList<PointOfService> _points;
    private readonly DateOnly[] _dateRange;
    private readonly IReadOnlyDictionary<string, int> _visitsLeft;
    private readonly int _visitsPerDay;


    public ScheduleSolver(
        IReadOnlyList<PointOfService> points,
        DateOnly[] dateRange,
        IReadOnlyDictionary<string, int> visitsLeft,
        int visitsPerDay
        )
    {
        _points = points;
        _dateRange = dateRange;
        _visitsLeft = visitsLeft;
        _visitsPerDay = visitsPerDay;
    }

    public async Task GenerateSchedule(CancellationToken cancellation)
    {
        var solver = new CpSolver();
        var status = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("[green]Generating solution[/]",
                _ => RunSolver(cancellation, solver));

        if (cancellation.IsCancellationRequested)
            return;

        AddConsoleRuleWithTitle("[green]Solve status[/]");
        AnsiConsole.Write(
            new FigletText(status.ToString())
                .LeftJustified()
                .Color(Color.Red));
        AddConsoleRuleWithTitle("[green]Statistics[/]");

        var table = new Table();

        // Add some columns
        table.AddColumn("Metric");
        table.AddColumn("Value");

        // Add some rows
        table.AddRow("Conflicts", $"{solver.NumConflicts()}");
        table.AddRow("Branches", $"{solver.NumBranches()}");
        table.AddRow("Wall time", $"{solver.WallTime()}");

        // Render the table to the console
        AnsiConsole.Write(table);
    }

    private async Task<CpSolverStatus> RunSolver(CancellationToken cancellation, CpSolver solver)
    {
        CpModel? model;
        IntVar[,,]? visits;
        try
        {
            (model, visits) = Modeling(cancellation);
        }
        catch (OperationCanceledException)
        {
            return CpSolverStatus.Unknown;
        }

        // Tell the solver to enumerate all solutions.
        solver.StringParameters += "linearization_level:0 enumerate_all_solutions:true ";

        // Define tasks
        const int solutionLimit = 5;
        var cb = new SolutionPrinter(_points, _dateRange, visits, solutionLimit, _visitsPerDay, () => { });

        if (cancellation.IsCancellationRequested)
            return CpSolverStatus.Unknown;

        // Solve
        cancellation.Register(solver.StopSearch);
        return await Task.Factory.StartNew(
            () => solver.Solve(model, cb),
            cancellation,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private (CpModel model, IntVar[,,] visits) Modeling(CancellationToken cancellationToken)
    {
        // Creates the model.
        var model = new CpModel();

        // Define variables
        var nDays = _dateRange.Length;
        var nPoints = _points.Count;

        // Creates visits variables.
        // visits[(p, d, v)]: point 'p' visit 'v' on day 'd'.
        var visits = new IntVar[nDays, nPoints, _visitsPerDay];
        for (var d = 0; d < nDays; d++)
        {
            for (var p = 0; p < nPoints; p++)
            {
                for (var v = 0; v < _visitsPerDay; v++)
                {
                    visits[d, p, v] = model.NewIntVar(0, 1, $"visits[{d},{p},{v}]");
                }
            }
        }

        var dayIndices = new int[nDays];
        for (var i = 0; i < nDays; i++)
        {
            // Suppose here no year change
            dayIndices[i] = _dateRange[i].DayOfYear - _dateRange[0].DayOfYear;
        }

        var dates = new IntVar[nDays];
        for (var d = 0; d < nDays; d++)
        {
            // create a variable for each day, bounded by the min and max possible dates
            var maxDayIndex = Math.Min(dayIndices.Length - 1, d + 1);
            dates[d] = model.NewIntVar(dayIndices[d], dayIndices[maxDayIndex], $"date_{d}");
        }

        // 1. Each point of service can only be visited once per day.
        for (var p = 0; p < nPoints; p++)
        {
            for (var d = 0; d < nDays; d++)
            {
                var x = new IntVar[_visitsPerDay];
                for (var v = 0; v < _visitsPerDay; v++)
                {
                    x[v] = visits[d, p, v];
                }

                model.Add(LinearExpr.Sum(x) <= 1);
            }
        }

        // 2. Each point of service must be visited exactly predefined number of times.
        for (var p = 0; p < nPoints; p++)
        {
            var x = new IntVar[nDays * _visitsPerDay];
            var point = _points[p];
            var numVisits = _visitsLeft[point.Name];
            for (var d = 0; d < nDays; d++)
            {
                for (var v = 0; v < _visitsPerDay; v++)
                {
                    x[d * _visitsPerDay + v] = visits[d, p, v];
                }
            }

            model.Add(LinearExpr.Sum(x) == numVisits);
        }

        // 3. All visits per day should be done.
        for (var d = 0; d < nDays; d++)
        {
            for (var v = 0; v < _visitsPerDay; v++)
            {
                var x = new IntVar[nPoints];
                for (var p = 0; p < nPoints; p++)
                {
                    x[p] = visits[d, p, v];
                }

                model.Add(LinearExpr.Sum(x) == 1);
            }
        }

        // 4. Add constraint that enforces allowed interval between visits for each selling point
        for (var p = 0; p < nPoints; p++)
        {
            var point = _points[p];
            var interval = point.Category.GetMinimalInterval();

            for (var d1 = 0; d1 < nDays; d1++)
            {
                for (var d2 = d1 + 1; d2 < nDays; d2++)
                {
                    for (var v1 = 0; v1 < _visitsPerDay; v1++)
                    {
                        for (var v2 = 0; v2 < _visitsPerDay; v2++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (v1 == v2)
                                continue;

                            var b = model.NewBoolVar("b");
                            var x = visits[d1, p, v1];
                            var y = visits[d2, p, v2];
                            model.Add(x == 1).OnlyEnforceIf(b);
                            model.Add(y == 1).OnlyEnforceIf(b);
                            model.Add(dates[d2] - dates[d1] >= interval).OnlyEnforceIf(b);
                        }
                    }
                }
            }
        }

        return (model, visits);
    }

    private static void AddConsoleRuleWithTitle(string title)
    {
        var rule = new Rule(title);
        rule.LeftJustified();
        AnsiConsole.Write(rule);
    }
}