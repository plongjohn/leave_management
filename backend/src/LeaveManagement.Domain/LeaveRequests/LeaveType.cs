namespace LeaveManagement.Domain.LeaveRequests;

public enum LeaveType
{
    /// <summary>Annual leave; uses the leave entitlement and needs approval.</summary>
    Vacation,

    /// <summary>Time off in lieu of overtime; needs approval. There is no time account, so no balance check.</summary>
    TimeOffInLieu,

    /// <summary>Sick leave is reported, not requested: approved immediately, may lie in the past.</summary>
    SickLeave,

    /// <summary>
    /// Paid leave for a personal occasion (Dienstverhinderung); needs approval and a reason,
    /// does not use the entitlement and is limited per occasion (<see cref="SpecialLeavePolicy"/>).
    /// </summary>
    SpecialLeave,
}
