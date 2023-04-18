using ScheduleMyPoS.Models;

namespace ScheduleMyPoS.Extensions;

internal static class DistrictExtensions
{
    public static int GetTransitionWeight(this District from, District to)
    {
        if (from.Main.Equals(to.Main, StringComparison.OrdinalIgnoreCase))
            return 0;

        if (from.Main.Equals(to.Secondary, StringComparison.OrdinalIgnoreCase))
            return 1;

        return from.Secondary.Equals(to.Main, StringComparison.OrdinalIgnoreCase)
            ? 1
            : 2;
    }
}