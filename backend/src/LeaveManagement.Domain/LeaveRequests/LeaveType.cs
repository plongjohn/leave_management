namespace LeaveManagement.Domain.LeaveRequests;

public enum LeaveType
{
    /// <summary>Annual leave; uses the leave entitlement and needs approval.</summary>
    Vacation,

    /// <summary>Time off in lieu of overtime; needs approval. There is no time account, so no balance check.</summary>
    TimeOffInLieu,

    /// <summary>Sick leave is reported, not requested: approved immediately, may lie in the past.</summary>
    SickLeave,
}
