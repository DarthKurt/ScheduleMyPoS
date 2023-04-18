using ScheduleMyPoS.Models;

namespace ScheduleMyPoS.Extensions;

internal static class SellingPointCategoryExtensions
{
    public static DateOnly GetNextVisitDate(this SellingPointCategory category, DateOnly lastVisit)
    {
        var daysToAdd = category.GetMinimalInterval();
        var nextDate = lastVisit.AddDays(daysToAdd);
        while (nextDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            nextDate = nextDate.AddDays(1);
        }
        return nextDate;
    }

    public static int GetMinimalInterval(this SellingPointCategory category)
        => category switch
        {
            SellingPointCategory.HighPriority => 5,
            SellingPointCategory.MediumPriority => 9,
            SellingPointCategory.LowPriority => 14,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Invalid selling point category")
        };
}