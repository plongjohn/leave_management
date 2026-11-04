namespace LeaveManagement.Api.LeaveRequests;

/// <param name="Version">The <c>version</c> of the request as the client last loaded it.</param>
public sealed record CancelLeaveRequestRequest(uint Version);
