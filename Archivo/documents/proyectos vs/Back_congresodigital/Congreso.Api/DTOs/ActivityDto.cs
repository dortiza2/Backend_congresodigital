namespace Congreso.Api.DTOs;

public record ActivityDto(
    string Id,
    string Title,
    string ActivityType,
    string? Location,
    System.DateTime? StartTime,
    System.DateTime? EndTime,
    int? Capacity,
    bool Published,
    int? EnrolledCount,
    int? AvailableSpots,
    SpeakerDto? Speaker
 );