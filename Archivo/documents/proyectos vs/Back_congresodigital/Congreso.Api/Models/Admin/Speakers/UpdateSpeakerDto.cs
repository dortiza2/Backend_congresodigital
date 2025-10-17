namespace Congreso.Api.Models.Admin.Speakers;

public class UpdateSpeakerDto
{
    public string? FullName { get; set; }
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Links { get; set; }
}

public class PutSpeakerDto
{
    public required string FullName { get; set; }
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Links { get; set; }
}