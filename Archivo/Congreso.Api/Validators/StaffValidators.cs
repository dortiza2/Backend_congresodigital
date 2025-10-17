using FluentValidation;
using Congreso.Api.DTOs;

namespace Congreso.Api.Validators;

/// <summary>
/// Validador para StaffInviteDto
/// </summary>
public class StaffInviteDtoValidator : AbstractValidator<StaffInviteDto>
{
    public StaffInviteDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido")
            .EmailAddress().WithMessage("El email debe ser válido");

        RuleFor(x => x.Role)
            .NotNull().WithMessage("El rol es requerido");

        RuleFor(x => x.DisplayName)
            .MaximumLength(255).WithMessage("El nombre para mostrar no puede exceder 255 caracteres");

        RuleFor(x => x.AvatarUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("La URL del avatar debe ser válida");
    }
}

/// <summary>
/// Validador para UpdateRoleRequest
/// </summary>
public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("El ID del rol es requerido")
            .GreaterThan(0).WithMessage("El ID del rol debe ser mayor a 0");
    }
}

/// <summary>
/// Validador para UpdateStatusRequest
/// </summary>
public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        // No additional validation needed for boolean IsActive
    }
}