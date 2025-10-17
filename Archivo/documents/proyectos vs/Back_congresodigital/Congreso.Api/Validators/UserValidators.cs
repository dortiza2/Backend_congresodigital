using FluentValidation;
using Congreso.Api.DTOs;

namespace Congreso.Api.Validators;

/// <summary>
/// Validador para CreateUserDto
/// </summary>
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido")
            .EmailAddress().WithMessage("El formato del email no es válido")
            .MaximumLength(256).WithMessage("El email no puede exceder 256 caracteres");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre completo es requerido")
            .MaximumLength(200).WithMessage("El nombre completo no puede exceder 200 caracteres")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$").WithMessage("El nombre solo puede contener letras y espacios");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$")
            .WithMessage("La contraseña debe contener al menos una minúscula, una mayúscula y un número");

        RuleFor(x => x.OrgName)
            .MaximumLength(100).WithMessage("El nombre de la organización no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.OrgName));
    }
}

/// <summary>
/// Validador para UpdateUserDto
/// </summary>
public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(255).WithMessage("El nombre completo no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.AvatarUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("La URL del avatar debe ser válida")
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl));

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrEmpty(status) || 
                            status == "active" || 
                            status == "inactive" || 
                            status == "suspended")
            .WithMessage("El estado debe ser 'active', 'inactive' o 'suspended'")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.Roles)
            .Must(roles => roles == null || roles.All(role => !string.IsNullOrEmpty(role)))
            .WithMessage("Los roles no pueden contener valores vacíos")
            .When(x => x.Roles != null);
    }

    private bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}