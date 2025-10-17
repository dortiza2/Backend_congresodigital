namespace Congreso.Api.DTOs;

public record PodiumDto(
    int Id,
    int Year,
    int Place,
    System.Guid? ActivityId,
    string? ActivityTitle,
    int? UserId,
    string? WinnerName,
    System.DateTime? AwardDate,
    int? TeamId,
    string? PrizeDescription
);