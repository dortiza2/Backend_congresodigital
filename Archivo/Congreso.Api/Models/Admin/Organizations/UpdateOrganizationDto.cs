namespace Congreso.Api.Models.Admin.Organizations;

public class UpdateOrganizationDto
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Domain { get; set; }
}

public class PutOrganizationDto
{
    public required string Name { get; set; }
    public string? Type { get; set; }
    public string? Domain { get; set; }
}