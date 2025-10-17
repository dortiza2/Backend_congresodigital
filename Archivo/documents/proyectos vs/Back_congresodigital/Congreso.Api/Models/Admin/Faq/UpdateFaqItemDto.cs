namespace Congreso.Api.Models.Admin.Faq;

public class UpdateFaqItemDto
{
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public string? Category { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public class PutFaqItemDto
{
    public required string Question { get; set; }
    public required string Answer { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}