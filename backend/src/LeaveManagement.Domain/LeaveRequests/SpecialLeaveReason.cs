namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>Personal occasions that entitle to paid special leave (see <see cref="SpecialLeavePolicy"/>).</summary>
public enum SpecialLeaveReason
{
    OwnWedding,
    BirthOfChild,

    /// <summary>Death of the spouse or partner, a child or a parent.</summary>
    DeathOfCloseRelative,

    /// <summary>Death of a sibling, parent-in-law or grandparent.</summary>
    DeathOfRelative,

    WeddingOfChildOrSibling,

    /// <summary>Moving to a new home (own household).</summary>
    Relocation,

    /// <summary>Care leave for a sick close relative (Pflegefreistellung, § 16 UrlG).</summary>
    CareLeave,
}
