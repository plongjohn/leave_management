namespace LeaveManagement.Domain.Holidays;

public enum HolidayKind
{
    /// <summary>Statutory public holiday (Feiertagsruhegesetz) – not a working day.</summary>
    Statutory,

    /// <summary>Day off granted by the company or the collective agreement – not a working day.</summary>
    CompanyDayOff,

    /// <summary>Half working day granted by the company, e.g. 24 and 31 December.</summary>
    CompanyHalfDay,
}
