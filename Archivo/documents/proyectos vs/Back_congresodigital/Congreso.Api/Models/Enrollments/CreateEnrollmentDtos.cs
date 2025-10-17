namespace Congreso.Api.Models.Enrollments;

public record CreateEnrollmentRequest(Guid UserId, List<Guid> ActivityIds);

public class CreateEnrollmentEnvelope
{
    public CreateEnrollmentRequest? Request { get; set; }
    public Guid? UserId { get; set; }
    public List<Guid>? ActivityIds { get; set; }
}