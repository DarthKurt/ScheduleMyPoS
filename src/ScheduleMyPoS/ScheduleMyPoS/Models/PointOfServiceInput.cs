namespace ScheduleMyPoS.Models;

internal sealed record PointOfServiceInput(IList<PointOfService> PointsOfService, IDictionary<string, int> VisitsLeft);