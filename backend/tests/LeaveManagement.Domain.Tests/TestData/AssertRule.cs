using LeaveManagement.Domain.Common;

namespace LeaveManagement.Domain.Tests.TestData;

internal static class AssertRule
{
    public static void Violated(string expectedCode, Action action)
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(action);
        Assert.Equal(expectedCode, exception.Code);
    }
}
