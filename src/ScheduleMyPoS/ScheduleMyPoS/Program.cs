using System.Collections.Immutable;
using ScheduleMyPoS;

var dateList = new DateOnly[]
{
    new(2023, 4, 18),
    new(2023, 4, 19),
    new(2023, 4, 20),
    new(2023, 5, 2),
    new(2023, 5, 3),
    new(2023, 5, 4),
    new(2023, 5, 5),
    new(2023, 5, 10),
    new(2023, 5, 11),
    new(2023, 5, 12),
    new(2023, 5, 15),
    new(2023, 5, 16),
    new(2023, 5, 17),
    new(2023, 5, 18),
    new(2023, 5, 19),
    new(2023, 5, 22),
    new(2023, 5, 23),
    new(2023, 5, 24),
    new(2023, 5, 25),
    new(2023, 5, 26),
    new(2023, 5, 29),
    new(2023, 5, 30),
    new(2023, 5, 31),
    new(2023, 6, 1),
    new(2023, 6, 2),
    new(2023, 6, 5),
    new(2023, 6, 6),
    new(2023, 6, 7),
    new(2023, 6, 8),
    new(2023, 6, 9),
    new(2023, 6, 13),
    new(2023, 6, 14),
    new(2023, 6, 15),
    new(2023, 6, 16),
};

const int visitsPerDay = 14;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    cts.Cancel();
    e.Cancel = true;
};

var input = PointOfServiceParser.Parse(args[0]);

var solver = new ScheduleSolver(
    input.PointsOfService.ToImmutableArray(),
    dateList,
    input.VisitsLeft.ToImmutableDictionary(),
    visitsPerDay
);
await solver.GenerateSchedule(cts.Token);
