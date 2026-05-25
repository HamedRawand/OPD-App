using System.Globalization;

namespace OPDClinic.Services;

public static class HijriService
{
    private static readonly PersianCalendar _calendar = new();

    public static string ToShamsi(DateTime gregorian)
    {
        int day   = _calendar.GetDayOfMonth(gregorian);
        int month = _calendar.GetMonth(gregorian);
        int year  = _calendar.GetYear(gregorian);
        return $"{day}/{month}/{year}";
    }

    public static DateTime? FromShamsi(string shamsiDate)
    {
        var parts = shamsiDate.Split('/', ',');
        if (parts.Length != 3) return null;
        if (!int.TryParse(parts[0].Trim(), out int day))   return null;
        if (!int.TryParse(parts[1].Trim(), out int month)) return null;
        if (!int.TryParse(parts[2].Trim(), out int year))  return null;
        try { return _calendar.ToDateTime(year, month, day, 0, 0, 0, 0); }
        catch { return null; }
    }
}
