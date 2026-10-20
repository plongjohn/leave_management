using FluentValidation;
using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Api.Auth;

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    // Only limits the work for absurd input; the password policy applies when passwords are set.
    private const int PasswordMaxLength = 256;

    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().MaximumLength(Employee.EmailMaxLength);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(PasswordMaxLength);
    }
}
