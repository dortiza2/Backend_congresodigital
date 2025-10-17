namespace Congreso.Api.DTOs;

public record SpeakerDto(
    string Id,
    string Name,
    string? Bio,
    string? Company,
    string? RoleTitle,
    string? AvatarUrl,
    System.Collections.Generic.Dictionary<string, object>? Links
);