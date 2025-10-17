namespace Congreso.Api.Models.Admin;

public class UpdateActivityDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public string? Location { get; set; }
    public int? Capacity { get; set; }
    public bool? IsActive { get; set; }
    public bool? RequiresEnrollment { get; set; }
    public string? ActivityType { get; set; }
}

public class PutActivityDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public string? Location { get; set; }
    public int Capacity { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool RequiresEnrollment { get; set; } = true;
    public string ActivityType { get; set; } = "Workshop";
}