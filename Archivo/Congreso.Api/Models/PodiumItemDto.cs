namespace Congreso.Api.Models;

public record PodiumItemDto(
    int Year,
    Guid? ActivityId,
    string? Title,
    int Position,
    Guid? UserId,
    string? UserName
);