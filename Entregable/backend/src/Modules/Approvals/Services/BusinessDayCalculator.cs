namespace MileageClaims.Modules.Approvals.Services;

/// <summary>Cuenta días hábiles (lunes a viernes) — RN-14 no considera feriados.</summary>
public static class BusinessDayCalculator
{
    public static int BusinessDaysBetween(DateTimeOffset from, DateTimeOffset to)
    {
        if (to <= from) return 0;

        var days = 0;
        var cursor = from.Date;
        while (cursor < to.Date)
        {
            cursor = cursor.AddDays(1);
            if (cursor.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                days++;
            }
        }
        return days;
    }
}
