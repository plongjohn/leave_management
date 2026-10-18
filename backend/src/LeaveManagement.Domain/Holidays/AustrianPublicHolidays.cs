namespace LeaveManagement.Domain.Holidays;

/// <summary>
/// The statutory public holidays in Austria (§ 7 Arbeitsruhegesetz). Easter Sunday and
/// Whit Sunday are left out because they always fall on a Sunday; Good Friday has not been a
/// public holiday since 2019.
/// </summary>
public static class AustrianPublicHolidays
{
    // Offsets of the movable holidays from Easter Sunday.
    private const int EasterMondayOffset = 1;
    private const int AscensionDayOffset = 39;
    private const int WhitMondayOffset = 50;
    private const int CorpusChristiOffset = 60;

    public static IReadOnlyList<Holiday> For(int year)
    {
        var easterSunday = EasterSunday(year);

        return
        [
            Statutory(new DateOnly(year, 1, 1), "Neujahr"),
            Statutory(new DateOnly(year, 1, 6), "Heilige Drei Könige"),
            Statutory(easterSunday.AddDays(EasterMondayOffset), "Ostermontag"),
            Statutory(new DateOnly(year, 5, 1), "Staatsfeiertag"),
            Statutory(easterSunday.AddDays(AscensionDayOffset), "Christi Himmelfahrt"),
            Statutory(easterSunday.AddDays(WhitMondayOffset), "Pfingstmontag"),
            Statutory(easterSunday.AddDays(CorpusChristiOffset), "Fronleichnam"),
            Statutory(new DateOnly(year, 8, 15), "Mariä Himmelfahrt"),
            Statutory(new DateOnly(year, 10, 26), "Nationalfeiertag"),
            Statutory(new DateOnly(year, 11, 1), "Allerheiligen"),
            Statutory(new DateOnly(year, 12, 8), "Mariä Empfängnis"),
            Statutory(new DateOnly(year, 12, 25), "Christtag"),
            Statutory(new DateOnly(year, 12, 26), "Stefanitag"),
        ];
    }

    /// <summary>
    /// Easter Sunday in the Gregorian calendar, computed with the anonymous Gregorian algorithm
    /// (Meeus/Jones/Butcher). The variable names follow the published algorithm.
    /// </summary>
    public static DateOnly EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var month = (h + l - (7 * m) + 114) / 31;
        var day = ((h + l - (7 * m) + 114) % 31) + 1;

        return new DateOnly(year, month, day);
    }

    private static Holiday Statutory(DateOnly date, string name) => Holiday.Create(date, name, HolidayKind.Statutory);
}
