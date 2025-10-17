using FluentValidation;
using Congreso.Api.DTOs;

namespace Congreso.Api.Validators;

/// <summary>
/// Validador para CreateActivityAdminDto
/// </summary>
public class CreateActivityAdminDtoValidator : AbstractValidator<CreateActivityAdminDto>
{
    public CreateActivityAdminDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título es requerido")
            .MaximumLength(255).WithMessage("El título no puede exceder 255 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción no puede exceder 2000 caracteres");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("La fecha de inicio es requerida")
            .Must(date => date > DateTime.UtcNow).WithMessage("La fecha de inicio debe ser futura");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("La fecha de fin es requerida")
            .Must((dto, endTime) => endTime > dto.StartTime)
            .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio");

        RuleFor(x => x.Location)
            .MaximumLength(255).WithMessage("La ubicación no puede exceder 255 caracteres");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0).WithMessage("La capacidad máxima debe ser mayor a 0")
            .LessThanOrEqualTo(1000).WithMessage("La capacidad máxima no puede exceder 1000");

        When(x => x.RequiresSpeaker, () =>
        {
            RuleFor(x => x.SpeakerName)
                .NotEmpty().WithMessage("El nombre del ponente es requerido cuando se requiere ponente")
                .MaximumLength(255).WithMessage("El nombre del ponente no puede exceder 255 caracteres");

            RuleFor(x => x.SpeakerBio)
                .MaximumLength(2000).WithMessage("La biografía del ponente no puede exceder 2000 caracteres");

            RuleFor(x => x.SpeakerAvatarUrl)
                .Must(url => string.IsNullOrEmpty(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("La URL del avatar debe ser válida");
        });
    }
}

/// <summary>
/// Validador para UpdateActivityAdminDto
/// </summary>
public class UpdateActivityAdminDtoValidator : AbstractValidator<UpdateActivityAdminDto>
{
    public UpdateActivityAdminDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(255).WithMessage("El título no puede exceder 255 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción no puede exceder 2000 caracteres");

        RuleFor(x => x.StartTime)
            .Must(date => !date.HasValue || date.Value > DateTime.UtcNow)
            .WithMessage("La fecha de inicio debe ser futura");

        RuleFor(x => x.EndTime)
            .Must((dto, endTime) => !endTime.HasValue || !dto.StartTime.HasValue || endTime.Value > dto.StartTime.Value)
            .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio");

        RuleFor(x => x.Location)
            .MaximumLength(255).WithMessage("La ubicación no puede exceder 255 caracteres");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0).WithMessage("La capacidad máxima debe ser mayor a 0")
            .LessThanOrEqualTo(1000).WithMessage("La capacidad máxima no puede exceder 1000");

        RuleFor(x => x.SpeakerName)
            .MaximumLength(255).WithMessage("El nombre del ponente no puede exceder 255 caracteres");

        RuleFor(x => x.SpeakerBio)
            .MaximumLength(2000).WithMessage("La biografía del ponente no puede exceder 2000 caracteres");

        RuleFor(x => x.SpeakerAvatarUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("La URL del avatar debe ser válida");

        RuleFor(x => x.IsPublished)
            .NotNull().WithMessage("El estado de publicación es requerido");
    }
}

/// <summary>
/// Validador para PublishActivityDto
/// </summary>
public class PublishActivityDtoValidator : AbstractValidator<PublishActivityDto>
{
    public PublishActivityDtoValidator()
    {
        // No additional validation needed for boolean property
    }
}